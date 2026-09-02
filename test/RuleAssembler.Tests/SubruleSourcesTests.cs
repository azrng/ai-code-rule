namespace RuleAssembler.Tests;

public sealed class SubruleSourcesTests
{
    private static ManifestLoader CreateLoader() => new(new ResourceStore());

    [Fact]
    public void GetSources_SingleFallsBackToSource()
    {
        var subrule = new Subrule { Source = "arch/a.md" };

        Assert.Equal(new[] { "arch/a.md" }, subrule.GetSources());
    }

    [Fact]
    public void GetSources_MultiKeepsOrder()
    {
        var subrule = new Subrule { Sources = { "shared/x.md", "shared/y.md" } };

        Assert.Equal(new[] { "shared/x.md", "shared/y.md" }, subrule.GetSources());
    }

    [Fact]
    public void CombineSources_JoinsWithBlankLine()
    {
        var manifest = new Manifest { Vars = new ManifestVars { Version = "1.37.0", LastUpdated = "2026-09-02" } };
        var renderer = new RuleRenderer(new ResourceStore());
        var subrule = new Subrule
        {
            Output = "backend-AGENTS.md",
            Sources = { "shared/skeletons/wails/doc-AGENTS.md", "shared/no-test-fallback.md" }
        };

        var combined = renderer.CombineSources(subrule, manifest);

        // 两个源的内容都在，且第二个源以空行分隔拼接在后
        Assert.Contains("# 文档规则", combined);
        Assert.Contains("### 无法执行测试时", combined);
        var fallbackIndex = combined.IndexOf("### 无法执行测试时", StringComparison.Ordinal);
        Assert.True(fallbackIndex > combined.IndexOf("# 文档规则", StringComparison.Ordinal));
        Assert.DoesNotContain("\n\n\n", combined);
    }

    [Fact]
    public void ValidateRuleFile_SourceAndSourcesMutuallyExclusive()
    {
        var errors = new List<string>();
        var subrule = new Subrule
        {
            Source = "shared/no-test-fallback.md",
            Sources = { "shared/skeletons/wails/doc-AGENTS.md" },
            Output = "x.md"
        };

        CreateLoader().ValidateRuleFile(subrule, "测试 Profile", errors);

        Assert.Contains(errors, error => error.Contains("不能同时配置"));
    }

    [Fact]
    public void ValidateRuleFile_NonFirstSourceWithFrontMatterRejected()
    {
        var errors = new List<string>();
        var subrule = new Subrule
        {
            Sources = { "shared/skeletons/wails/doc-AGENTS.md", "shared/skeletons/wails/backend-AGENTS.md" },
            Output = "x.md"
        };

        CreateLoader().ValidateRuleFile(subrule, "测试 Profile", errors);

        // 第二个源带 front matter，必须报错
        Assert.Contains(errors, error => error.Contains("front matter"));
    }

    [Fact]
    public void ValidateRuleFile_ValidMultiSourcePasses()
    {
        var errors = new List<string>();
        var subrule = new Subrule
        {
            Sources = { "shared/skeletons/wails/doc-AGENTS.md", "shared/no-test-fallback.md" },
            Output = "x.md"
        };

        CreateLoader().ValidateRuleFile(subrule, "测试 Profile", errors);

        Assert.Empty(errors);
    }
}
