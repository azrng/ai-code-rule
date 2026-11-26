using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace RuleAssembler.Tests;

// 引擎共享缓存回归：多目录副本同机复用一份已校验 exe。
// 落盘 → 同哈希命中回读；缺失 / 内容损坏（写入中断、被改动）按未命中处理并清掉坏文件；
// 重复落盘覆盖旧内容（temp + 原子改名）。
// 另覆盖引擎本地登记三件套：N1 启动自登记（first-wins + 规模清理）、
// N2 本地回退（sidecar 完整 + exe 哈希回验，漂移自愈）、N3 sidecar 补写
public sealed class EngineCacheTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "ruleassembler-engine-cache-tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private static byte[] Payload(string content) => Encoding.UTF8.GetBytes(content);

    private static string Sha256(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));

    private string NewDir()
    {
        var dir = Path.Combine(_tempDir, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    // 检查期间把缓存根指到隔离目录，结束后还原，避免污染同进程其他测试
    private static void WithCacheRoot(string cacheRoot, Action action)
    {
        var original = Environment.GetEnvironmentVariable("USE_AI_RULE_CACHE");
        Environment.SetEnvironmentVariable("USE_AI_RULE_CACHE", cacheRoot);
        try
        {
            action();
        }
        finally
        {
            Environment.SetEnvironmentVariable("USE_AI_RULE_CACHE", original);
        }
    }

    // 按生产路径造一个完整引擎缓存条目：exe 字节（TryStoreCachedEngine）+ sidecar（N3）
    private static void StoreEngineEntry(string cacheRoot, string version, byte[] exeBytes)
    {
        PackResolver.TryStoreCachedEngine(cacheRoot, version, exeBytes);
        EngineCache.TryStoreEngineSidecar(cacheRoot, version, Sha256(exeBytes));
    }

    // 写一个假「当前 exe」文件，供 RegisterCurrentEngine 注入路径（避免读写 testhost.exe）
    private string WriteFakeExe(byte[] bytes)
    {
        var path = Path.Combine(NewDir(), "use-ai-rule.exe");
        File.WriteAllBytes(path, bytes);
        return path;
    }

    [Fact]
    public void MissingCacheReturnsNull()
    {
        Assert.Null(PackResolver.TryReadCachedEngine(_tempDir, "3.8.5", Sha256(Payload("anything"))));
    }

    [Fact]
    public void StoredBytesRoundTripWithMatchingHash()
    {
        var bytes = Payload("use-ai-rule engine payload");

        PackResolver.TryStoreCachedEngine(_tempDir, "3.8.5", bytes);

        var cached = PackResolver.TryReadCachedEngine(_tempDir, "3.8.5", Sha256(bytes));
        Assert.NotNull(cached);
        Assert.Equal(bytes, cached);
        // 落在引擎缓存约定路径：engine\<版本>\use-ai-rule.exe，与内容包版本目录互不干扰
        Assert.Equal(
            Path.Combine(_tempDir, "engine", "3.8.5", "use-ai-rule.exe"),
            PackResolver.EngineCachePath(_tempDir, "3.8.5"));
        Assert.True(File.Exists(PackResolver.EngineCachePath(_tempDir, "3.8.5")));
    }

    [Fact]
    public void CorruptedCacheIsDeletedAndTreatedAsMiss()
    {
        var bytes = Payload("good engine bytes");
        PackResolver.TryStoreCachedEngine(_tempDir, "3.8.5", bytes);

        // 模拟上次写入中断 / 被改动：缓存文件内容与登记哈希不一致
        File.WriteAllText(PackResolver.EngineCachePath(_tempDir, "3.8.5"), "tampered");

        Assert.Null(PackResolver.TryReadCachedEngine(_tempDir, "3.8.5", Sha256(bytes)));
        Assert.False(File.Exists(PackResolver.EngineCachePath(_tempDir, "3.8.5")));
    }

    [Fact]
    public void RestoringSameVersionOverwritesPreviousContent()
    {
        PackResolver.TryStoreCachedEngine(_tempDir, "3.8.5", Payload("old bytes"));
        var bytes = Payload("new bytes");

        PackResolver.TryStoreCachedEngine(_tempDir, "3.8.5", bytes);

        var cached = PackResolver.TryReadCachedEngine(_tempDir, "3.8.5", Sha256(bytes));
        Assert.NotNull(cached);
        Assert.Equal(bytes, cached);
    }

    [Fact]
    public void HashMismatchDoesNotHitDifferentVersionPayload()
    {
        // 缓存里的字节与 latest.json 登记哈希不一致（哪怕只是版本装错）绝不当命中处理
        var stored = Payload("other version payload");
        PackResolver.TryStoreCachedEngine(_tempDir, "3.8.5", stored);

        Assert.Null(PackResolver.TryReadCachedEngine(_tempDir, "3.8.5", Sha256(Payload("expected payload"))));
    }

    // ── N1 启动自登记 ──

    [Fact]
    public void RegisterCurrentEngine_WritesExeAndSidecar()
    {
        var cacheRoot = NewDir();
        var exeBytes = Payload("my engine bytes 3.8.5");
        var exePath = WriteFakeExe(exeBytes);

        WithCacheRoot(cacheRoot, () =>
        {
            Assert.True(EngineCache.RegisterCurrentEngine("3.8.5", exePath));

            var engineDir = Path.Combine(cacheRoot, "engine", "3.8.5");
            Assert.Equal(exeBytes, File.ReadAllBytes(Path.Combine(engineDir, "use-ai-rule.exe")));

            // sidecar 登记哈希与落盘字节一致，可供 N2 / 免下载命中
            var meta = JsonDocument.Parse(File.ReadAllText(Path.Combine(engineDir, "meta.json")));
            Assert.Equal("3.8.5", meta.RootElement.GetProperty("version").GetString());
            Assert.Equal(Sha256(exeBytes), meta.RootElement.GetProperty("sha256").GetString());
        });
    }

    [Fact]
    public void RegisterCurrentEngine_DevVersionNotRegistered()
    {
        var cacheRoot = NewDir();

        WithCacheRoot(cacheRoot, () =>
        {
            // dev / 不可解析版本不登记、不参与比较（与 IsNewer 口径一致）
            Assert.False(EngineCache.RegisterCurrentEngine("dev"));
            Assert.False(Directory.Exists(Path.Combine(cacheRoot, "engine")));
        });
    }

    [Fact]
    public void RegisterCurrentEngine_FirstWinsSkipsCompleteDir()
    {
        var cacheRoot = NewDir();
        var exePath = WriteFakeExe(Payload("first bytes"));

        WithCacheRoot(cacheRoot, () =>
        {
            Assert.True(EngineCache.RegisterCurrentEngine("3.8.5", exePath));

            // 第二次登记（自身字节已变）：目录完整即跳过不覆盖，避免每次启动重写大文件
            File.WriteAllBytes(exePath, Payload("second bytes"));
            Assert.True(EngineCache.RegisterCurrentEngine("3.8.5", exePath));
            Assert.Equal(
                Payload("first bytes"),
                File.ReadAllBytes(Path.Combine(cacheRoot, "engine", "3.8.5", "use-ai-rule.exe")));
        });
    }

    [Fact]
    public void RegisterCurrentEngine_RewritesIncompleteDirWithOwnBytes()
    {
        var cacheRoot = NewDir();
        // 旧版程序留下的半截登记：exe 在而 sidecar 缺失，视为登记不完整
        PackResolver.TryStoreCachedEngine(cacheRoot, "3.8.5", Payload("stale bytes"));
        var exeBytes = Payload("current running bytes");
        var exePath = WriteFakeExe(exeBytes);

        WithCacheRoot(cacheRoot, () =>
        {
            Assert.True(EngineCache.RegisterCurrentEngine("3.8.5", exePath));

            var engineDir = Path.Combine(cacheRoot, "engine", "3.8.5");
            Assert.Equal(exeBytes, File.ReadAllBytes(Path.Combine(engineDir, "use-ai-rule.exe")));
            var meta = JsonDocument.Parse(File.ReadAllText(Path.Combine(engineDir, "meta.json")));
            Assert.Equal(Sha256(exeBytes), meta.RootElement.GetProperty("sha256").GetString());
        });
    }

    [Fact]
    public void RegisterCurrentEngine_PrunesOldVersionsKeepingCurrentAndTopThree()
    {
        var cacheRoot = NewDir();
        for (var minor = 1; minor <= 5; minor++)
            StoreEngineEntry(cacheRoot, $"3.8.{minor}", Payload($"engine 3.8.{minor}"));
        var exePath = WriteFakeExe(Payload("current exe"));

        WithCacheRoot(cacheRoot, () =>
        {
            Assert.True(EngineCache.RegisterCurrentEngine("3.8.1", exePath));

            var engineRoot = Path.Combine(cacheRoot, "engine");
            // 保留：当前 3.8.1 + 最高三版 3.8.5 / 3.8.4 / 3.8.3；剔除 3.8.2
            Assert.True(Directory.Exists(Path.Combine(engineRoot, "3.8.1")));
            Assert.True(Directory.Exists(Path.Combine(engineRoot, "3.8.3")));
            Assert.True(Directory.Exists(Path.Combine(engineRoot, "3.8.4")));
            Assert.True(Directory.Exists(Path.Combine(engineRoot, "3.8.5")));
            Assert.False(Directory.Exists(Path.Combine(engineRoot, "3.8.2")));
        });
    }

    // ── N2 本地回退 ──

    [Fact]
    public void TryFetchLocalOffer_FindsHigherVerifiedVersion()
    {
        var cacheRoot = NewDir();
        var exeBytes = Payload("engine 3.8.5");
        StoreEngineEntry(cacheRoot, "3.8.5", exeBytes);

        WithCacheRoot(cacheRoot, () =>
        {
            var offer = EngineCache.TryFetchLocalOffer("3.8.0");
            Assert.NotNull(offer);
            Assert.Equal("3.8.5", offer!.Version);
            Assert.Equal("3.8.0", offer.EngineCurrent);
            Assert.Equal(Sha256(exeBytes), offer.EngineSha256);
            Assert.True(offer.FromLocalCache);
            Assert.True(offer.HasEngineUpdate);
            Assert.True(offer.HasAnyUpdate);
            Assert.Contains("来自本机缓存", offer.Describe());
            Assert.Contains("程序 3.8.0 → 3.8.5", offer.Describe());
        });
    }

    [Fact]
    public void TryFetchLocalOffer_NoHigherVersionReturnsNull()
    {
        var cacheRoot = NewDir();
        StoreEngineEntry(cacheRoot, "3.8.0", Payload("engine 3.8.0"));

        WithCacheRoot(cacheRoot, () =>
        {
            // 当前已是缓存里的最高版本
            Assert.Null(EngineCache.TryFetchLocalOffer("3.8.0"));
            // 缓存里只有更旧版本
            Assert.Null(EngineCache.TryFetchLocalOffer("3.9.0"));
        });
    }

    [Fact]
    public void TryFetchLocalOffer_SidecarExeDriftPrunesDir()
    {
        var cacheRoot = NewDir();
        StoreEngineEntry(cacheRoot, "3.8.5", Payload("engine 3.8.5"));
        // 模拟写中断 / 被改动：sidecar 哈希与 exe 实际字节漂移
        File.WriteAllText(Path.Combine(cacheRoot, "engine", "3.8.5", "use-ai-rule.exe"), "tampered");

        WithCacheRoot(cacheRoot, () =>
        {
            Assert.Null(EngineCache.TryFetchLocalOffer("3.8.0"));
            // 漂移目录被自愈整目录剔除，不留「sidecar 在而 exe 坏」的僵尸条目
            Assert.False(Directory.Exists(Path.Combine(cacheRoot, "engine", "3.8.5")));
        });
    }

    [Fact]
    public void TryFetchLocalOffer_DriftedHighestFallsBackToLowerVerified()
    {
        var cacheRoot = NewDir();
        StoreEngineEntry(cacheRoot, "3.9.0", Payload("engine 3.9.0"));
        StoreEngineEntry(cacheRoot, "3.8.5", Payload("engine 3.8.5"));
        File.WriteAllText(Path.Combine(cacheRoot, "engine", "3.9.0", "use-ai-rule.exe"), "tampered");

        WithCacheRoot(cacheRoot, () =>
        {
            var offer = EngineCache.TryFetchLocalOffer("3.8.0");
            Assert.NotNull(offer);
            Assert.Equal("3.8.5", offer!.Version);
            // 最高版本漂移剔除后继续找次高完整版本
            Assert.False(Directory.Exists(Path.Combine(cacheRoot, "engine", "3.9.0")));
        });
    }

    [Fact]
    public void TryFetchLocalOffer_RequiresSidecar()
    {
        var cacheRoot = NewDir();
        // 旧版程序落的缓存：只有 exe 没有 sidecar，N2 不可发现（须先经 N3 补登记）
        PackResolver.TryStoreCachedEngine(cacheRoot, "3.8.5", Payload("engine 3.8.5"));

        WithCacheRoot(cacheRoot, () => Assert.Null(EngineCache.TryFetchLocalOffer("3.8.0")));
    }

    [Fact]
    public void TryFetchLocalOffer_SidecarVersionMismatchIgnored()
    {
        var cacheRoot = NewDir();
        var exeBytes = Payload("engine 3.8.5");
        StoreEngineEntry(cacheRoot, "3.8.5", exeBytes);
        // sidecar 的 version 与目录名不一致：登记口径不满足，不参与候选
        File.WriteAllText(
            Path.Combine(cacheRoot, "engine", "3.8.5", "meta.json"),
            $"{{\"version\":\"9.9.9\",\"sha256\":\"{Sha256(exeBytes)}\"}}");

        WithCacheRoot(cacheRoot, () => Assert.Null(EngineCache.TryFetchLocalOffer("3.8.0")));
    }

    [Fact]
    public void TryFetchLocalOffer_IgnoresNonVersionDirs()
    {
        var cacheRoot = NewDir();
        var junkDir = Path.Combine(cacheRoot, "engine", "not-a-version");
        Directory.CreateDirectory(junkDir);
        File.WriteAllText(Path.Combine(junkDir, "use-ai-rule.exe"), "junk");
        File.WriteAllText(Path.Combine(junkDir, "meta.json"), "{\"version\":\"9.9.9\",\"sha256\":\"abc\"}");

        WithCacheRoot(cacheRoot, () =>
        {
            Assert.Null(EngineCache.TryFetchLocalOffer("3.8.0"));
            // 非版本目录不参与比较也不被误删（可能是用户文件或未来格式）
            Assert.True(File.Exists(Path.Combine(junkDir, "use-ai-rule.exe")));
            Assert.True(File.Exists(Path.Combine(junkDir, "meta.json")));
        });
    }

    // ── N3 sidecar 补写 ──

    [Fact]
    public void TryStoreEngineSidecar_FillsMissingSidecar()
    {
        var cacheRoot = NewDir();
        var bytes = Payload("engine 3.8.5");
        // 先按旧版程序的形态只落 exe
        PackResolver.TryStoreCachedEngine(cacheRoot, "3.8.5", bytes);

        EngineCache.TryStoreEngineSidecar(cacheRoot, "3.8.5", Sha256(bytes));

        // 补登记后该条目可被 N2 发现（对应 TryUpdateEngine 缓存命中分支的自愈路径）
        WithCacheRoot(cacheRoot, () =>
        {
            var offer = EngineCache.TryFetchLocalOffer("3.8.0");
            Assert.NotNull(offer);
            Assert.Equal("3.8.5", offer!.Version);
        });
    }
}
