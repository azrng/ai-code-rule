using System.Security.Cryptography;
using System.Text;
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
        var resolution = PackResolver.Resolve(new[] { "--offline" });

        Assert.Null(resolution.Version);
        Assert.Equal("embedded", resolution.Source);
        Assert.IsType<EmbeddedResourceProvider>(resolution.Provider);
    }

    [Fact]
    public void Resolve_BareCommandLineArgs_WithoutCache_FallsBackToEmbedded()
    {
        // 命令行启动不做网络检查：无缓存直接内嵌
        var resolution = PackResolver.Resolve(new[] { "--help" });

        Assert.Equal("embedded", resolution.Source);
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
