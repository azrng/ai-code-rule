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
