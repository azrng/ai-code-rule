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

    [Fact]
    public void EveryProfile_CollaborationRule_ContainsPlanGate()
    {
        var manifest = new ManifestLoader(new ResourceStore()).Load();
        var renderer = new RuleRenderer(new ResourceStore());

        foreach (var profile in manifest.Profiles)
        {
            var files = renderer.Plan(manifest, profile);
            var collaboration = Assert.Single(files, file => file.Output == "collaboration-AGENTS.md");
            Assert.Contains("需求澄清与计划关卡", collaboration.Content);
            Assert.Contains("每轮优先提出 1 至 3 个最关键问题", collaboration.Content);
        }
    }

    [Fact]
    public void EveryProfile_CollaborationRule_ContainsCandorAndRedundancyFeedback()
    {
        var manifest = new ManifestLoader(new ResourceStore()).Load();
        var renderer = new RuleRenderer(new ResourceStore());

        foreach (var profile in manifest.Profiles)
        {
            var files = renderer.Plan(manifest, profile);
            var collaboration = Assert.Single(files, file => file.Output == "collaboration-AGENTS.md");
            Assert.Contains("坦率反馈与过程沟通", collaboration.Content);
            Assert.Contains("常规细节自行判断", collaboration.Content);
            Assert.Contains("有用 / 冗余", collaboration.Content);

            var root = Assert.Single(files, file => file.Output == "AGENTS.md");
            Assert.Contains("不盲从用户前提", root.Content);
            Assert.Contains("坦率反馈与过程沟通", root.Content);
        }
    }

    [Fact]
    public void RootTemplate_FixedBehaviorSections_StayWithinResidentBudget()
    {
        var source = new ResourceStore().Read("root/AGENTS.md");
        Assert.NotNull(source);

        var sections = new[]
        {
            ExtractSection(source!, "## 根文件原则", "## 子规则索引"),
            ExtractSection(source!, "## 全局工作规则", "## 编码硬约束"),
            ExtractSection(source!, "## 编码硬约束", "## 触发式协作规则"),
            ExtractSection(source!, "## 触发式协作规则", null)
        };

        var bulletCount = sections.Sum(section => section.Split('\n').Count(line => line.StartsWith("- ", StringComparison.Ordinal)));

        Assert.InRange(bulletCount, 0, 24);
    }

    private static string ExtractSection(string content, string startHeading, string? endHeading)
    {
        var start = content.IndexOf(startHeading, StringComparison.Ordinal);
        Assert.True(start >= 0, $"缺少根模板章节：{startHeading}");
        var bodyStart = start + startHeading.Length;
        var end = endHeading is null
            ? content.Length
            : content.IndexOf(endHeading, bodyStart, StringComparison.Ordinal);
        Assert.True(end >= bodyStart, $"根模板章节顺序错误：{startHeading} -> {endHeading}");
        return content[bodyStart..end];
    }

    [Fact]
    public void EveryProfile_CollaborationRule_ContainsFeedbackLoop()
    {
        // 规则效果反馈闭环：所有 Profile 的协作子规则都带反馈登记口径，根文件触发提示覆盖该章节
        var manifest = new ManifestLoader(new ResourceStore()).Load();
        var renderer = new RuleRenderer(new ResourceStore());

        foreach (var profile in manifest.Profiles)
        {
            var files = renderer.Plan(manifest, profile);
            var collaboration = Assert.Single(files, file => file.Output == "collaboration-AGENTS.md");
            Assert.Contains("## 规则效果反馈", collaboration.Content);
            Assert.Contains(".ai-rule-feedback.md", collaboration.Content);
            var root = Assert.Single(files, file => file.Output == "AGENTS.md");
            Assert.Contains("重复问题回顾或规则效果反馈", root.Content);
        }
    }
}
