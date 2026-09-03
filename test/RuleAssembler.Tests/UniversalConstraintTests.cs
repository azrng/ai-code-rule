namespace RuleAssembler.Tests;

// 通用执行约束回归：根 AGENTS.md 声明的跨模板口径必须在每个 Profile 的根文件中真实落地，
// 防止约束只落在个别架构片段造成口径缺口（如 && 禁令曾仅存在于 staticHtml 本地段）
public sealed class UniversalConstraintTests
{
    [Fact]
    public void EveryProfile_RootAgents_ContainsPowerShellChainBan()
    {
        var manifest = new ManifestLoader(new ResourceStore()).Load();
        var renderer = new RuleRenderer(new ResourceStore());

        foreach (var profile in manifest.Profiles)
        {
            var files = renderer.Plan(manifest, profile);
            var root = Assert.Single(files, file => file.Output == "AGENTS.md");
            Assert.Contains(
                "PowerShell 命令不接受 `&&`，禁止使用；需要连续执行时改为分步执行或使用 PowerShell 兼容写法。",
                root.Content);
        }
    }

    [Fact]
    public void EveryProfile_Output_ContainsChineseCommentRule()
    {
        var manifest = new ManifestLoader(new ResourceStore()).Load();
        var renderer = new RuleRenderer(new ResourceStore());

        foreach (var profile in manifest.Profiles)
        {
            var files = renderer.Plan(manifest, profile);
            Assert.True(
                files.Any(file => file.Content.Contains("统一使用中文，不使用纯英文注释")),
                $"{profile.Id} 的生成产物缺少中文注释口径");
        }
    }
}
