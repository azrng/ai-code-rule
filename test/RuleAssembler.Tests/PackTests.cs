using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.IO.Compression;
using System.Text.Json;

namespace RuleAssembler.Tests;

// 内容包机制回归：
// 两个 provider 的来源差异屏蔽（内嵌逻辑名 → fragments//skills/ 统一前缀；目录直读）、
// pack.json 加载与校验（schema 拒绝、哈希不匹配检出）、PackResolver 的
// --pack 硬失败语义与 --offline 无缓存回退内嵌
public sealed class PackTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "ruleassembler-pack-tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // ── EmbeddedResourceProvider ──

    [Fact]
    public void Embedded_NamesUseUnifiedFragmentsSkillsPrefixes()
    {
        var provider = new EmbeddedResourceProvider();

        Assert.Contains(provider.Names, name => name.StartsWith("fragments/", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(provider.Names, name => name.StartsWith("skills/", StringComparison.OrdinalIgnoreCase));
        // 非 Fragments./Skills. 逻辑名（程序集自身资源）不进入
        Assert.DoesNotContain(provider.Names, name => !name.StartsWith("fragments/", StringComparison.OrdinalIgnoreCase)
            && !name.StartsWith("skills/", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Embedded_ReadManifestAndSkillMarkdown()
    {
        var provider = new EmbeddedResourceProvider();

        var manifest = provider.ReadBytes("fragments/manifest.json");
        Assert.NotNull(manifest);
        Assert.StartsWith("{", Encoding.UTF8.GetString(manifest!).Trim());

        var skillMd = provider.Names.FirstOrDefault(name => name.EndsWith("/SKILL.md", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(skillMd);
        Assert.NotNull(provider.ReadBytes(skillMd!));
    }

    [Fact]
    public void Embedded_ReadBytesMissingResourceReturnsNull()
    {
        var provider = new EmbeddedResourceProvider();
        Assert.Null(provider.ReadBytes("fragments/不存在.md"));
        Assert.Null(provider.ReadBytes("skills/不存在/SKILL.md"));
    }

    // ── FolderResourceProvider ──

    [Fact]
    public void Folder_EnumeratesAndReadsFiles_ExcludingPackJson()
    {
        var root = NewDir();
        WriteFile(root, "fragments/manifest.json", "{}");
        WriteFile(root, "skills/demo/SKILL.md", "---\ndescription: demo\n---\n# demo");
        File.WriteAllText(Path.Combine(root, "pack.json"), "{}");

        var provider = new FolderResourceProvider(root);

        Assert.Equal(2, provider.Names.Count);
        Assert.Equal("---\ndescription: demo\n---\n# demo", Encoding.UTF8.GetString(provider.ReadBytes("skills/demo/SKILL.md")!));
        Assert.Null(provider.ReadBytes("pack.json"));
    }

    [Fact]
    public void Folder_MissingResourceReturnsNull()
    {
        var root = NewDir();
        var provider = new FolderResourceProvider(root);
        Assert.Null(provider.ReadBytes("anything.txt"));
    }

    // ── ResourceStore / SkillsStore 经 provider 的一致性 ──

    [Fact]
    public void Stores_FromFolderProvider_BehaveLikeEmbedded()
    {
        // 用内嵌资源起一个内容包目录的形态，验证 Store 对两种来源行为一致
        var embedded = new EmbeddedResourceProvider();
        var root = NewDir();
        foreach (var name in embedded.Names)
        {
            var target = Path.Combine(root, name.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.WriteAllBytes(target, embedded.ReadBytes(name)!);
        }

        var folderProvider = new FolderResourceProvider(root);
        var embeddedStore = new SkillsStore(embedded);
        var folderStore = new SkillsStore(folderProvider);

        var embeddedSkills = embeddedStore.Discover();
        var folderSkills = folderStore.Discover();
        Assert.Equal(
            embeddedSkills.Select(skill => (skill.Id, skill.ResourceRoot, skill.FileCount)).ToList(),
            folderSkills.Select(skill => (skill.Id, skill.ResourceRoot, skill.FileCount)).ToList());
        Assert.True(embeddedSkills.Count >= 30);

        // ResourceStore：manifest 内容两种来源一致
        var fromEmbedded = new ResourceStore(embedded).Read("manifest.json");
        var fromFolder = new ResourceStore(folderProvider).Read("manifest.json");
        Assert.Equal(fromEmbedded, fromFolder);
    }

    // ── PackReader ──

    [Fact]
    public void PackReader_LoadsAndVerifiesValidPack()
    {
        var root = NewDir();
        WriteFile(root, "fragments/manifest.json", "{}");
        WriteFile(root, "skills/demo/SKILL.md", "demo");
        WritePackJson(root, "1.0.0", files: new Dictionary<string, string>
        {
            ["fragments/manifest.json"] = Sha256Of(root, "fragments/manifest.json"),
            ["skills/demo/SKILL.md"] = Sha256Of(root, "skills/demo/SKILL.md")
        });

        var manifest = PackReader.Load(root);
        Assert.Equal("1.0.0", manifest.Version);
        Assert.Equal("merged", manifest.Kind);
        Assert.Empty(PackReader.Verify(manifest, root));
    }

    [Fact]
    public void PackReader_VerifyDetectsTamperedAndMissingFiles()
    {
        var root = NewDir();
        WriteFile(root, "fragments/manifest.json", "{}");
        WriteFile(root, "skills/demo/SKILL.md", "demo");
        WritePackJson(root, "1.0.0", files: new Dictionary<string, string>
        {
            ["fragments/manifest.json"] = new string('0', 64),
            ["skills/demo/SKILL.md"] = Sha256Of(root, "skills/demo/SKILL.md"),
            ["skills/ghost/SKILL.md"] = new string('a', 64)
        });

        var mismatches = PackReader.Verify(PackReader.Load(root), root);
        Assert.Contains("fragments/manifest.json", mismatches);
        Assert.Contains("skills/ghost/SKILL.md", mismatches);
        Assert.DoesNotContain("skills/demo/SKILL.md", mismatches);
        Assert.Equal(2, mismatches.Count);
    }

    [Fact]
    public void PackReader_LoadRejectsMissingPackJson()
    {
        var root = NewDir();
        Assert.Throws<InvalidOperationException>(() => PackReader.Load(root));
    }

    [Fact]
    public void PackReader_LoadRejectsUnsupportedSchema()
    {
        var root = NewDir();
        WritePackJson(root, "1.0.0", schemaVersion: PackReader.SupportedSchemaVersion + 1, files: new Dictionary<string, string>());

        var ex = Assert.Throws<InvalidOperationException>(() => PackReader.Load(root));
        Assert.Contains("schema", ex.Message);
    }

    [Fact]
    public void PackReader_LoadRejectsUnknownKind()
    {
        var root = NewDir();
        WritePackJson(root, "1.0.0", kind: "rules-only", files: new Dictionary<string, string>());

        Assert.Throws<InvalidOperationException>(() => PackReader.Load(root));
    }

    // ── PackResolver ──

    [Fact]
    public void Resolve_PackWithZip_PrefersExplicitPack()
    {
        var root = NewDir();
        WriteFile(root, "fragments/manifest.json", "{}");
        WritePackJson(root, "9.9.9", files: new Dictionary<string, string>
        {
            ["fragments/manifest.json"] = Sha256Of(root, "fragments/manifest.json")
        });

        var resolution = PackResolver.Resolve(new[] { "--pack", root });

        Assert.Equal("9.9.9", resolution.Version);
        Assert.Equal("pack", resolution.Source);
        Assert.IsType<FolderResourceProvider>(resolution.Provider);
        Assert.NotNull(new ResourceStore(resolution.Provider).Read("manifest.json"));
    }

    [Fact]
    public void Resolve_PackWithMissingPath_Throws()
    {
        Assert.ThrowsAny<Exception>(() =>
            PackResolver.Resolve(new[] { "--pack", Path.Combine(_tempDir, "nope.zip") }));
    }

    [Fact]
    public void Resolve_OfflineWithoutCache_FallsBackToEmbedded()
    {
        // cacheRoot 注入隔离目录：测试不依赖也不污染本机 %LOCALAPPDATA% 真实缓存，
        // 保证「无缓存 → 内嵌兜底」分支在任何开发机上可复现
        var resolution = PackResolver.Resolve(new[] { "--offline" }, cacheRoot: NewDir());

        Assert.Null(resolution.Version);
        Assert.Equal("embedded", resolution.Source);
        Assert.IsType<EmbeddedResourceProvider>(resolution.Provider);
    }

    [Fact]
    public void Resolve_OfflineWithCache_PrefersCache()
    {
        // 夹具版本取远高于任何真实内嵌版本的值：该用例验证「缓存命中优先」，
        // 不应随仓库 manifest 版本增长而反复失效（缓存 ≥ 内嵌时仍走缓存）
        var cache = NewDir();
        WriteFile(cache, "999.0.0/pack.json", "{\"version\":\"999.0.0\",\"kind\":\"merged\",\"schema_version\":1,\"files\":{}}");
        WriteFile(cache, "999.0.0/fragments/manifest.json", "{}");
        File.WriteAllText(Path.Combine(cache, "current.json"), "{\"version\":\"999.0.0\"}");

        var resolution = PackResolver.Resolve(new[] { "--offline" }, cacheRoot: cache);

        Assert.Equal("999.0.0", resolution.Version);
        Assert.Equal("cache", resolution.Source);
    }

    [Fact]
    public void Resolve_CacheOlderThanEmbedded_UpgradesCache()
    {
        // 引擎换代先于内容包发布：缓存版本旧于 exe 内嵌内容时，把内嵌内容落位为
        // 新缓存并切指针（缓存收敛到内嵌版本），避免旧缓存压掉新特性（技能全归「其他」）
        var cache = NewDir();
        WriteFile(cache, "0.1.0/pack.json", "{\"version\":\"0.1.0\",\"kind\":\"merged\",\"schema_version\":1,\"files\":{}}");
        WriteFile(cache, "current.json", "{\"version\":\"0.1.0\"}");

        var embeddedVersion = new ManifestLoader(new ResourceStore()).Load().Vars.Version;

        var resolution = PackResolver.Resolve(new[] { "--offline" }, cacheRoot: cache);

        Assert.Equal("cache", resolution.Source);
        Assert.Equal(embeddedVersion, resolution.Version);
        Assert.Contains(Path.Combine(cache, embeddedVersion), Directory.GetDirectories(cache));
        Assert.True(File.Exists(Path.Combine(cache, embeddedVersion, "fragments", "manifest.json")));
        Assert.Equal(embeddedVersion, JsonDocument.Parse(File.ReadAllText(Path.Combine(cache, "current.json"))).RootElement.GetProperty("version").GetString());
        // 旧缓存目录保留（指针已切换，不再被选中）
        Assert.True(Directory.Exists(Path.Combine(cache, "0.1.0")));
    }

    [Fact]
    public void Resolve_CacheNewerOrEqualEmbedded_KeepsCache()
    {
        // 缓存版本高于内嵌（正常更新路径）时维持缓存优先，不被本修复改变
        var cache = NewDir();
        WriteFile(cache, "999.0.0/pack.json", "{\"version\":\"999.0.0\",\"kind\":\"merged\",\"schema_version\":1,\"files\":{}}");
        WriteFile(cache, "current.json", "{\"version\":\"999.0.0\"}");

        var resolution = PackResolver.Resolve(new[] { "--offline" }, cacheRoot: cache);

        Assert.Equal("cache", resolution.Source);
        Assert.Equal("999.0.0", resolution.Version);
    }

    [Fact]
    public void Resolve_BareCommandLineArgs_WithoutCache_FallsBackToEmbedded()
    {
        // 命令行启动不做网络检查：无缓存直接内嵌
        var resolution = PackResolver.Resolve(new[] { "--help" }, cacheRoot: NewDir());

        Assert.Equal("embedded", resolution.Source);
    }

    // ── 启动统一更新检查（TryFetchUpdateOffer + ApplyUpdates）──
    [Fact]
    public void 统一检查_确认更新_安装新包并切缓存指针()
    {
        // 本地 stub 发布源替代 GitHub Release：验证「一次检查给出统一版本对照结论 → 确认更新 →
        // 下载校验落位切指针 → 返回 true 要求调用方重启」这条消费者可见行为
        var cache = NewDir();
        var packRoot = NewDir();
        WriteFile(packRoot, "fragments/manifest.json", "{}");
        WritePackJson(packRoot, "9.8.7", files: new Dictionary<string, string>
        {
            ["fragments/manifest.json"] = Sha256Of(packRoot, "fragments/manifest.json")
        });
        var zipPath = Path.Combine(_tempDir, "ai-rule-pack.zip");
        ZipFile.CreateFromDirectory(packRoot, zipPath);

        using var server = new StubReleaseServer(
            ("latest.json", Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
            {
                version = "9.8.7",
                sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(zipPath))).ToLowerInvariant()
            }))),
            ("ai-rule-pack.zip", File.ReadAllBytes(zipPath)));
        WithPackUrl(server.BaseUrl, cache, () =>
        {
            var offer = PackResolver.TryFetchUpdateOffer("1.0.0", "1.0.0", new List<string>());
            Assert.NotNull(offer);
            Assert.True(offer.HasPackUpdate);
            Assert.False(offer.HasEngineUpdate); // latest.json 未携带 engine_sha256：引擎轨道不触发
            Assert.True(PackResolver.ApplyUpdates(offer, _ => true));

            var restarted = PackResolver.Resolve(new[] { "--offline" }, cacheRoot: cache);
            Assert.Equal("9.8.7", restarted.Version);
            Assert.Equal("cache", restarted.Source);
            Assert.NotNull(new ResourceStore(restarted.Provider).Read("manifest.json"));
            // 缓存已落位并切指针：重启后的 Resolve 离线即可拿到新版本（重启生效语义的事实基础）
            Assert.Equal(
                "9.8.7",
                JsonDocument.Parse(File.ReadAllText(Path.Combine(cache, "current.json")))
                    .RootElement.GetProperty("version").GetString());
            Assert.Equal("9.8.7", PackResolver.Resolve(new[] { "--offline" }, cacheRoot: cache).Version);
        });
    }

    [Fact]
    public void 统一检查_用户跳过_不安装不落缓存()
    {
        var cache = NewDir();
        using var server = new StubReleaseServer(
            ("latest.json", Encoding.UTF8.GetBytes($"{{\"version\":\"9.8.7\",\"sha256\":\"{new string('0', 64)}\"}}")));
        WithPackUrl(server.BaseUrl, cache, () =>
        {
            var offer = PackResolver.TryFetchUpdateOffer("1.0.0", "1.0.0", new List<string>());
            Assert.NotNull(offer);
            Assert.False(PackResolver.ApplyUpdates(offer, _ => false));
            Assert.False(File.Exists(Path.Combine(cache, "current.json")));
        });
    }

    [Fact]
    public void 统一检查_已是最新_不产生更新结论()
    {
        var cache = NewDir();
        using var server = new StubReleaseServer(
            ("latest.json", Encoding.UTF8.GetBytes($"{{\"version\":\"1.0.0\",\"sha256\":\"{new string('0', 64)}\"}}")));
        WithPackUrl(server.BaseUrl, cache, () =>
        {
            var offer = PackResolver.TryFetchUpdateOffer("1.0.0", "1.0.0", new List<string>());
            Assert.NotNull(offer);
            Assert.False(offer.HasAnyUpdate);
            Assert.False(PackResolver.ApplyUpdates(offer, _ => true));
        });
    }

    [Fact]
    public void 统一检查_落后版本双轨道同进同退()
    {
        // 统一版本语义：exe 与生效内容都落后于远端统一版本且哈希齐备 → 两条轨道同时命中，
        // 确认文案列出两部分的替换范围
        var cache = NewDir();
        using var server = new StubReleaseServer(
            ("latest.json", Encoding.UTF8.GetBytes(
                $"{{\"version\":\"9.8.7\",\"sha256\":\"{new string('0', 64)}\",\"engine_sha256\":\"ab\"}}")));
        WithPackUrl(server.BaseUrl, cache, () =>
        {
            var offer = PackResolver.TryFetchUpdateOffer("2.5", "2.5", new List<string>());
            Assert.NotNull(offer);
            Assert.True(offer.HasEngineUpdate);
            Assert.True(offer.HasPackUpdate);
            var description = offer.Describe();
            Assert.Contains("程序 2.5 → 9.8.7", description);
            Assert.Contains("内容包 2.5 → 9.8.7", description);
        });
    }

    [Fact]
    public void 统一检查_部分更新残留_只补齐落后轨道()
    {
        // 上次更新部分失败的残留（如引擎已换、内容包下载失败）：统一版本只对照各自基准，
        // 已追平的一方不再触发，仍落后的一方单独补齐
        var cache = NewDir();
        using var server = new StubReleaseServer(
            ("latest.json", Encoding.UTF8.GetBytes(
                $"{{\"version\":\"9.8.7\",\"sha256\":\"{new string('0', 64)}\",\"engine_sha256\":\"ab\"}}")));
        WithPackUrl(server.BaseUrl, cache, () =>
        {
            // exe 已最新、内容包落后：引擎轨道不触发，仅补齐内容包
            var packBehind = PackResolver.TryFetchUpdateOffer("9.8.7", "2.5", new List<string>());
            Assert.NotNull(packBehind);
            Assert.False(packBehind.HasEngineUpdate);
            Assert.True(packBehind.HasPackUpdate);

            // 反向残留：内容包已最新、exe 落后（engine_sha256 在场才允许替换二进制）
            var engineBehind = PackResolver.TryFetchUpdateOffer("2.5", "9.8.7", new List<string>());
            Assert.NotNull(engineBehind);
            Assert.True(engineBehind.HasEngineUpdate);
            Assert.False(engineBehind.HasPackUpdate);
            Assert.Contains("程序 2.5 → 9.8.7", engineBehind.Describe());
        });
    }

    // 检查期间把发布源指到本地 stub、缓存根指到测试隔离目录，结束后还原，避免污染本机缓存与同进程其他测试；
    // 隔离目录先清后用，保证断言不受历史残留影响
    private static void WithPackUrl(string baseUrl, string cacheRoot, Action action)
    {
        var original = Environment.GetEnvironmentVariable("USE_AI_RULE_PACK_URL");
        var originalCache = Environment.GetEnvironmentVariable("USE_AI_RULE_PACK_CACHE");
        Environment.SetEnvironmentVariable("USE_AI_RULE_PACK_URL", baseUrl);
        if (Directory.Exists(cacheRoot))
            Directory.Delete(cacheRoot, recursive: true);
        Environment.SetEnvironmentVariable("USE_AI_RULE_PACK_CACHE", cacheRoot);
        try
        {
            action();
        }
        finally
        {
            Environment.SetEnvironmentVariable("USE_AI_RULE_PACK_URL", original);
            Environment.SetEnvironmentVariable("USE_AI_RULE_PACK_CACHE", originalCache);
        }
    }

    // 只服务给定静态路径的最小 HTTP 源：TcpListener + 手写响应，避免 HttpListener 的 URLACL 依赖；
    // Connection: close + Content-Length 足够 HttpClient 拉取 latest.json 与 zip
    private sealed class StubReleaseServer : IDisposable
    {
        private readonly TcpListener _listener;
        private readonly Dictionary<string, byte[]> _assets;
        private readonly CancellationTokenSource _cts = new();

        public StubReleaseServer(params (string Path, byte[] Content)[] assets)
        {
            _assets = assets.ToDictionary(a => "/" + a.Path, a => a.Content, StringComparer.Ordinal);
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
            _ = Task.Run(AcceptLoop);
        }

        private int Port { get; }

        public string BaseUrl => $"http://127.0.0.1:{Port}";

        private async Task AcceptLoop()
        {
            try
            {
                while (!_cts.IsCancellationRequested)
                {
                    using var client = await _listener.AcceptTcpClientAsync(_cts.Token);
                    using var stream = client.GetStream();
                    using var reader = new StreamReader(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
                    var requestLine = await reader.ReadLineAsync(_cts.Token);
                    var path = requestLine?.Split(' ') is { Length: > 1 } parts ? parts[1] : "";
                    while (!string.IsNullOrEmpty(await reader.ReadLineAsync(_cts.Token)))
                    {
                    }

                    if (!_assets.TryGetValue(path, out var content))
                    {
                        await WriteAsync(stream, "404 Not Found", Array.Empty<byte>());
                        continue;
                    }

                    await WriteAsync(stream, "200 OK", content);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception)
            {
                // Dispose 后的套接字中断按测试结束处理
            }
        }

        private static async Task WriteAsync(NetworkStream stream, string status, byte[] content)
        {
            var header = Encoding.ASCII.GetBytes(
                $"HTTP/1.1 {status}\r\nContent-Length: {content.Length}\r\nConnection: close\r\n\r\n");
            await stream.WriteAsync(header);
            if (content.Length > 0)
                await stream.WriteAsync(content);
        }

        public void Dispose()
        {
            _cts.Cancel();
            _listener.Stop();
        }
    }

    // ── 辅助 ──

    private string NewDir()
    {
        var dir = Path.Combine(_tempDir, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static string Sha256Of(string root, string relativePath)
    {
        return Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)))));
    }

    // 写相对路径文件（自动建父目录）
    private static void WriteFile(string root, string relativePath, string content)
    {
        var target = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.WriteAllText(target, content);
    }

    private static void WritePackJson(string root, string version, string kind = "merged", int schemaVersion = 1, Dictionary<string, string>? files = null)
    {
        var manifest = new
        {
            kind,
            version,
            schema_version = schemaVersion,
            files = files ?? new Dictionary<string, string>()
        };
        File.WriteAllText(
            Path.Combine(root, "pack.json"),
            JsonSerializer.Serialize(manifest, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}
