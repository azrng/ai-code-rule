namespace RuleAssembler.Tests;

public sealed class RuleRendererVersionTests
{
    private static Manifest ManifestWith(string version, string lastUpdated) => new()
    {
        Vars = new ManifestVars { Version = version, LastUpdated = lastUpdated }
    };

    [Fact]
    public void AlignVersion_ReplacesKeysInLfFrontmatter()
    {
        var content = "---\nrule_id: ui-agents\nversion: 1.0.0\nlast_updated: 2020-01-01\ndependencies: [agents-root]\n---\n\n正文";

        var result = RuleRenderer.AlignVersion(content, ManifestWith("1.24.0", "2026-08-31"));

        Assert.StartsWith("---\nrule_id: ui-agents\nversion: 1.24.0\nlast_updated: 2026-08-31\n", result);
        Assert.EndsWith("正文", result);
    }

    [Fact]
    public void AlignVersion_ReplacesKeysInCrlfFrontmatter()
    {
        var content = "---\r\nrule_id: ui-agents\r\nversion: 1.0.0\r\nlast_updated: 2020-01-01\r\n---\r\n\r\n正文";

        var result = RuleRenderer.AlignVersion(content, ManifestWith("1.24.0", "2026-08-31"));

        Assert.Contains("version: 1.24.0", result);
        Assert.Contains("last_updated: 2026-08-31", result);
        Assert.DoesNotContain("1.0.0", result);
    }

    [Fact]
    public void AlignVersion_LeavesContentWithoutFrontmatterUntouched()
    {
        var content = "# 标题\n\nversion: 1.0.0\n";

        var result = RuleRenderer.AlignVersion(content, ManifestWith("1.24.0", "2026-08-31"));

        Assert.Equal(content, result);
    }

    [Fact]
    public void AlignVersion_MissingKeyLeavesFrontmatterUntouched()
    {
        var content = "---\nrule_id: ui-agents\n---\n正文";

        var result = RuleRenderer.AlignVersion(content, ManifestWith("1.24.0", "2026-08-31"));

        Assert.Equal(content, result);
    }

    [Fact]
    public void AlignVersion_OnlyFrontmatterOccurrenceIsReplaced()
    {
        var content = "---\nversion: 1.0.0\n---\nversion: 9.9.9\n";

        var result = RuleRenderer.AlignVersion(content, ManifestWith("1.24.0", "2026-08-31"));

        Assert.Contains("---\nversion: 1.24.0\n---\nversion: 9.9.9\n", result);
    }
}

public sealed class RuleRendererPlanTests
{
    [Fact]
    public void Plan_ReactTauri_ExpandsSharedFragmentsAndPointers()
    {
        var resources = new ResourceStore();
        var manifest = new ManifestLoader(resources).Load();
        var profile = Assert.Single(manifest.Profiles, item => item.Id == "reactTauriExe");

        var files = new RuleRenderer(resources).Plan(manifest, profile);

        Assert.Equal(10, files.Count);
        Assert.All(files, file => Assert.DoesNotContain("<!-- include:", file.Content));
        Assert.Contains(files, file => file.Output == "collaboration-AGENTS.md"
            && file.Content.Contains("需求澄清与计划关卡", StringComparison.Ordinal));
        Assert.Contains(files, file => file.Output == "frontend-AGENTS.md"
            && file.Content.Contains("所有 Tauri 调用统一收敛", StringComparison.Ordinal));
        Assert.Contains(files, file => file.Output == "backend-AGENTS.md"
            && file.Content.Contains("Tauri Command / event / plugin 体系", StringComparison.Ordinal));
        var doc = Assert.Single(files, file => file.Output == "doc-AGENTS.md");
        Assert.Contains("doc/requirements/<module>/<feature>.md", doc.Content, StringComparison.Ordinal);
        Assert.Contains("需求追溯", doc.Content, StringComparison.Ordinal);
        Assert.DoesNotContain("全项目一份", doc.Content, StringComparison.Ordinal);
        Assert.Contains(files, file => file.Output == "GROK.md"
            && file.Content.Contains("version: 1.3.1", StringComparison.Ordinal));
    }
}
