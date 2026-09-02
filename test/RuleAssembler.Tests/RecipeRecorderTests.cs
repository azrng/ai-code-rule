using System.Text.Json;

namespace RuleAssembler.Tests;

public sealed class RecipeRecorderTests : IDisposable
{
    private readonly string _root;

    public RecipeRecorderTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"rule-recipe-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void Write_ProducesJsonRecordWithProfile()
    {
        var manifest = new Manifest { Vars = new ManifestVars { Version = "1.36.0", LastUpdated = "2026-09-02" } };
        var profile = new Profile { Id = "reactWailsExe", Label = "React + Wails 桌面应用", Family = "hybrid-app" };
        var answers = new Dictionary<string, string> { ["form"] = "desktop" };
        var sources = new Dictionary<string, string> { ["form"] = "manual" };
        var files = new List<PlannedFile> { new() { Output = "AGENTS.md", Content = "x" } };

        var record = RecipeRecorder.Build(manifest, profile, "wizard", answers, sources, new[] { "doc", "infra" }, files);
        new RecipeRecorder().Write(_root, record);

        var path = Path.Combine(_root, RecipeRecorder.RecordFileName);
        Assert.True(File.Exists(path));
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        Assert.Equal("reactWailsExe", doc.RootElement.GetProperty("profile_id").GetString());
        Assert.Equal("wizard", doc.RootElement.GetProperty("mode").GetString());
        Assert.Equal("desktop", doc.RootElement.GetProperty("answers").GetProperty("form").GetString());
        Assert.Equal("manual", doc.RootElement.GetProperty("answer_sources").GetProperty("form").GetString());
        // 仓库约定生成文件统一 LF，配方记录不得带 CRLF
        Assert.DoesNotContain("\r\n", File.ReadAllText(path));
    }

    [Fact]
    public void Write_RecordDetectedAnswerSources()
    {
        // 补答完成的配方保留 mode=detected，回答来源区分检测确认与人工补答
        var manifest = new Manifest { Vars = new ManifestVars { Version = "1.39.0", LastUpdated = "2026-09-02" } };
        var profile = new Profile { Id = "vueElectronDotNet", Label = "Vue + Electron + .NET 桌面应用", Family = "hybrid-app" };
        var answers = new Dictionary<string, string>
        {
            ["form"] = "desktop", ["backend"] = "dotnet", ["shell"] = "electron", ["frontend"] = "vue"
        };
        var sources = new Dictionary<string, string>
        {
            ["form"] = "detected", ["backend"] = "detected", ["shell"] = "detected", ["frontend"] = "manual"
        };
        var files = new List<PlannedFile> { new() { Output = "AGENTS.md", Content = "x" } };

        var record = RecipeRecorder.Build(manifest, profile, "detected", answers, sources, new[] { "doc", "infra", "design-system" }, files);
        new RecipeRecorder().Write(_root, record);

        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(_root, RecipeRecorder.RecordFileName)));
        Assert.Equal("detected", doc.RootElement.GetProperty("mode").GetString());
        Assert.Equal("detected", doc.RootElement.GetProperty("answer_sources").GetProperty("shell").GetString());
        Assert.Equal("manual", doc.RootElement.GetProperty("answer_sources").GetProperty("frontend").GetString());
    }

    [Fact]
    public void EnsureGitignored_CreatesFileWhenMissing()
    {
        var message = new RecipeRecorder().EnsureGitignored(_root, "hybrid");

        var content = File.ReadAllText(Path.Combine(_root, ".gitignore"));
        Assert.Contains(".ai-rule-profile.json", content);
        Assert.Contains("node_modules/", content);
        Assert.Contains("已创建", message);
    }

    [Fact]
    public void EnsureGitignored_AppendsEntryToExistingFile()
    {
        File.WriteAllText(Path.Combine(_root, ".gitignore"), "bin/\nobj/\n");

        new RecipeRecorder().EnsureGitignored(_root, "dotnet");

        var content = File.ReadAllText(Path.Combine(_root, ".gitignore"));
        Assert.StartsWith("bin/\nobj/\n", content);
        Assert.Contains(".ai-rule-profile.json", content);
        Assert.DoesNotContain("node_modules/", content);
    }

    [Fact]
    public void EnsureGitignored_IsIdempotent()
    {
        var recorder = new RecipeRecorder();
        recorder.EnsureGitignored(_root, "dotnet");
        var first = File.ReadAllText(Path.Combine(_root, ".gitignore"));

        var message = recorder.EnsureGitignored(_root, "dotnet");
        var second = File.ReadAllText(Path.Combine(_root, ".gitignore"));

        Assert.Equal(first, second);
        Assert.Contains("已包含", message);
    }

    [Fact]
    public void StackKeyFor_MapsFamiliesToTemplates()
    {
        var hybrid = new Profile { Id = "reactWailsExe", Family = "hybrid-app" };
        var flutter = new Profile { Id = "flutterMobile", Family = "mobile" };
        var pythonProfile = new Profile { Id = "python", Family = "python-backend" };
        var fullstack = new Profile { Id = "reactLinkdotNet-ai", Family = "web-fullstack" };

        Assert.Equal("hybrid", RecipeRecorder.StackKeyFor(hybrid));
        Assert.Equal("flutter", RecipeRecorder.StackKeyFor(flutter));
        Assert.Equal("python", RecipeRecorder.StackKeyFor(pythonProfile));
        // Web 前后端分离同时包含 .NET 后端，不能只给 Node 模板
        Assert.Equal("webstack", RecipeRecorder.StackKeyFor(fullstack));
        var template = GitignoreTemplates.Get("webstack");
        Assert.Contains("node_modules/", template);
        Assert.Contains("[Bb]in/", template);
    }
}
