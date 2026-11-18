using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace RuleAssembler.Tests;

// 内容机制回归：两个 provider 的来源差异屏蔽（内嵌逻辑名 → fragments//skills/ 统一前缀；目录直读）、
// pack.json 加载与校验（schema 拒绝、哈希不匹配检出）、PackResolver 的 --pack 硬失败语义与
// 内嵌兜底、启动统一更新检查的引擎单轨道闸门
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
    public void Resolve_PackWithDirectory_PrefersExplicitPack()
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
    public void Resolve_WithoutPack_FallsBackToEmbedded()
    {
        // 常态唯一来源：exe 内嵌内容（--offline 只影响是否联网检查，不影响解析来源）
        var resolution = PackResolver.Resolve(new[] { "--offline" });

        Assert.Null(resolution.Version);
        Assert.Equal("embedded", resolution.Source);
        Assert.IsType<EmbeddedResourceProvider>(resolution.Provider);
        Assert.NotNull(new ResourceStore(resolution.Provider).Read("manifest.json"));
    }

    // ── 启动统一更新检查（TryFetchUpdateOffer + ApplyUpdates，唯一轨道：引擎自更新）──
    [Fact]
    public void 统一检查_引擎落后且带哈希_命中更新()
    {
        // 单轨道语义：统一版本对照 exe 版本，engine_sha256 在场才允许替换二进制
        var cacheRoot = NewDir();
        using var server = new StubReleaseServer(
            ("latest.json", Encoding.UTF8.GetBytes(
                $"{{\"version\":\"9.8.7\",\"engine_sha256\":\"{new string('a', 64)}\"}}")));
        WithReleaseUrl(server.BaseUrl, () =>
        {
            var offer = PackResolver.TryFetchUpdateOffer("2.5", new List<string>());
            Assert.NotNull(offer);
            Assert.True(offer.HasEngineUpdate);
            Assert.True(offer.HasAnyUpdate);
            Assert.Contains("程序 2.5 → 9.8.7", offer.Describe());
            Assert.Contains("重新运行后生效", offer.Describe());

            // 用户拒绝：不安装（TryUpdateEngine 会替换运行中进程自身，测试只驱动确认闸门，
            // 引擎落位路径由 EngineCacheTests 与人工冒烟覆盖）
            Assert.False(PackResolver.ApplyUpdates(offer, _ => false));
        });
    }

    [Fact]
    public void 统一检查_缺引擎哈希_视为无更新()
    {
        // 旧发布源兼容：latest.json 未携带 engine_sha256 时拒绝静默落位二进制，
        // 即使版本落后也不触发更新（落位的字节绝不接受未校验）
        using var server = new StubReleaseServer(
            ("latest.json", Encoding.UTF8.GetBytes("{\"version\":\"9.8.7\"}")));
        WithReleaseUrl(server.BaseUrl, () =>
        {
            var offer = PackResolver.TryFetchUpdateOffer("2.5", new List<string>());
            Assert.NotNull(offer);
            Assert.False(offer.HasEngineUpdate);
            Assert.False(offer.HasAnyUpdate);
        });
    }

    [Fact]
    public void 统一检查_已是最新_不产生更新结论()
    {
        using var server = new StubReleaseServer(
            ("latest.json", Encoding.UTF8.GetBytes(
                $"{{\"version\":\"1.0.0\",\"engine_sha256\":\"{new string('b', 64)}\"}}")));
        WithReleaseUrl(server.BaseUrl, () =>
        {
            var offer = PackResolver.TryFetchUpdateOffer("1.0.0", new List<string>());
            Assert.NotNull(offer);
            Assert.False(offer.HasAnyUpdate);
            Assert.False(PackResolver.ApplyUpdates(offer, _ => true));
        });
    }

    // 检查期间把发布源指到本地 stub，结束后还原，避免污染同进程其他测试
    private static void WithReleaseUrl(string baseUrl, Action action)
    {
        var original = Environment.GetEnvironmentVariable("USE_AI_RULE_PACK_URL");
        Environment.SetEnvironmentVariable("USE_AI_RULE_PACK_URL", baseUrl);
        try
        {
            action();
        }
        finally
        {
            Environment.SetEnvironmentVariable("USE_AI_RULE_PACK_URL", original);
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
