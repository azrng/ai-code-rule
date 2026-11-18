using System.Security.Cryptography;
using System.Text;

namespace RuleAssembler.Tests;

// 引擎共享缓存回归：多目录副本同机复用一份已校验 exe。
// 落盘 → 同哈希命中回读；缺失 / 内容损坏（写入中断、被改动）按未命中处理并清掉坏文件；
// 重复落盘覆盖旧内容（temp + 原子改名）
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
}
