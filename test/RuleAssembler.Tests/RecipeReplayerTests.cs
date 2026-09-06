using System.Text.Json;
using Xunit;

namespace RuleAssembler.Tests;

// RecipeReplayer（P2 阶段 4 配方重放）行为回归：
// 新配方逐字节还原、旧配方（仅 profile_id）经兼容样例反查、组件集合不一致显式报错、
// 能力子集按配方重放、非法组合拒绝。
public sealed class RecipeReplayerTests : IDisposable
{
    private readonly Manifest _manifest;
    private readonly RuleRenderer _renderer;
    private readonly RecipeReplayer _replayer;
    private readonly string _tempDir;

    public RecipeReplayerTests()
    {
        var resources = new ResourceStore();
        _manifest = new ManifestLoader(resources).Load();
        _renderer = new RuleRenderer(resources);
        _replayer = new RecipeReplayer(_manifest, _renderer);
        _tempDir = Path.Combine(Path.GetTempPath(), "ruleassembler-replay-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private (RecipeRecord Record, string Json) BuildRecipe(string profileId, IReadOnlySet<string>? features = null)
    {
        var caseItem = _manifest.CompatibilityCases.First(item => item.Profile == profileId);
        var profile = _manifest.Profiles.First(item => item.Id == profileId);
        var selection = new Selection
        {
            Dimensions = new Dictionary<string, string>(caseItem.Selection, StringComparer.Ordinal)
        };
        var plan = new CompositionPlanner(_manifest).Plan(selection);
        var effectiveFeatures = features ?? FeatureSelection.Defaults(_manifest, plan);
        var files = _renderer.Plan(_manifest, profile, effectiveFeatures);
        var record = RecipeRecorder.BuildComposition(
            _manifest, selection, plan.ComponentIds, "wizard",
            new Dictionary<string, string>(), new Dictionary<string, string>(), effectiveFeatures, files);
        var json = JsonSerializer.Serialize(record, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
        return (record, json);
    }

    [Fact]
    public void 新配方重放_产物与旧Profile默认渲染逐字节一致()
    {
        var (record, json) = BuildRecipe("vueWailsExe");
        var result = _replayer.Replay(json, _tempDir);

        var legacyFiles = _renderer.Plan(_manifest, _manifest.Profiles.First(item => item.Id == "vueWailsExe"));
        Assert.Equal(legacyFiles.Count, result.Files.Count);
        foreach (var (expected, actual) in legacyFiles.Zip(result.Files))
        {
            Assert.Equal(expected.Output, actual.Output);
            Assert.Equal(expected.Content, actual.Content);
        }

        Assert.Equal("vueWailsExe", result.Record.ProfileId);
        Assert.Equal(record.ComponentIds, result.Record.ComponentIds);
        Assert.Equal(_manifest.Vars.Version, result.Record.ManifestVersion);
    }

    [Fact]
    public void 旧配方仅有profileId_经兼容样例反查完成重放()
    {
        var json = """
            {
              "schema_version": 1,
              "manifest_version": "1.40.0",
              "generated_at": "2026-09-01T10:00:00+08:00",
              "mode": "classic",
              "profile_id": "reactOnly",
              "features": ["doc", "infra", "design-system"],
              "files": []
            }
            """;
        var result = _replayer.Replay(json, _tempDir);

        var legacyFiles = _renderer.Plan(_manifest, _manifest.Profiles.First(item => item.Id == "reactOnly"));
        Assert.Equal(legacyFiles.Select(file => file.Output), result.Files.Select(file => file.Output));
        Assert.Equal("reactOnly", result.Record.ProfileId);
        // 版本差异以提示说明，不阻断重放
        Assert.Contains(result.Notes, note => note.Contains("1.40.0"));
    }

    [Fact]
    public void 组件集合与当前manifest不一致时报错_不静默生成()
    {
        var (_, json) = BuildRecipe("reactLinkdotNet");
        var tampered = json.Replace(
            "\"component_ids\": [",
            "\"component_ids\": [\n    \"ghost-component\",",
            StringComparison.Ordinal);

        var ex = Assert.Throws<InvalidOperationException>(() => _replayer.Replay(tampered, _tempDir));
        Assert.Contains("配方与当前规则版本不一致", ex.Message);
        Assert.Contains("ghost-component", ex.Message);
    }

    [Fact]
    public void 能力子集按配方重放_关闭doc后无doc文件()
    {
        var (_, json) = BuildRecipe("reactElectron", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "infra" });
        var result = _replayer.Replay(json, _tempDir);

        Assert.DoesNotContain(result.Files, file => file.Output == "doc-AGENTS.md");
        Assert.DoesNotContain(result.Files, file => file.Output == "design-system.yaml");
        var legacyFiles = _renderer.Plan(
            _manifest,
            _manifest.Profiles.First(item => item.Id == "reactElectron"),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "infra" });
        Assert.Equal(legacyFiles.Select(file => file.Output), result.Files.Select(file => file.Output));
        // 配方中无 doc，根导航也不得引用
        var root = result.Files.First(file => file.Output == "AGENTS.md").Content;
        Assert.DoesNotContain("doc-AGENTS.md", root);
    }

    [Fact]
    public void 非法组合配方被组合器拒绝()
    {
        var json = """
            {
              "schema_version": 1,
              "manifest_version": "1.51.0",
              "mode": "wizard",
              "profile_id": "",
              "selection": { "form": "web", "shell": "electron", "frontend": "react", "backend": "none" },
              "features": [],
              "files": []
            }
            """;

        var ex = Assert.Throws<InvalidOperationException>(() => _replayer.Replay(json, _tempDir));
        Assert.Contains("违反约束", ex.Message);
    }

    [Fact]
    public void 无selection且无profileId的配方报错()
    {
        var json = """
            {
              "schema_version": 1,
              "manifest_version": "1.51.0",
              "mode": "wizard",
              "features": [],
              "files": []
            }
            """;

        var ex = Assert.Throws<InvalidOperationException>(() => _replayer.Replay(json, _tempDir));
        Assert.Contains("无法重建组合选择", ex.Message);
    }
}
