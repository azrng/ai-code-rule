namespace RuleAssembler.Tests;

// P1-3 Q5 能力开关：按 features 过滤输出与根导航联动（组合口径）
public sealed class FeatureToggleTests
{
    private readonly Manifest _manifest;
    private readonly RuleRenderer _renderer;

    public FeatureToggleTests()
    {
        _manifest = new ManifestLoader(new ResourceStore()).Load();
        _renderer = new RuleRenderer(new ResourceStore());
    }

    private CompositionPlan PlanOf(string caseId)
    {
        var caseItem = _manifest.CompatibilityCases.Single(item =>
            item.Profile.Equals(caseId, StringComparison.OrdinalIgnoreCase));
        return new CompositionPlanner(_manifest).Plan(new Selection
        {
            Dimensions = new Dictionary<string, string>(caseItem.Selection, StringComparer.Ordinal)
        });
    }

    [Fact]
    public void Toggle_DocOff_RemovesDocFileAndNavigation()
    {
        var plan = PlanOf("reactLinkdotNet");
        var features = FeatureSelection.Defaults(_manifest, plan);
        features.Remove("doc");

        var files = _renderer.Plan(_manifest, plan, features);

        Assert.DoesNotContain(files, file => file.Output == "doc-AGENTS.md");
        var root = Assert.Single(files, file => file.Output == "AGENTS.md");
        Assert.DoesNotContain("doc-AGENTS.md", root.Content, StringComparison.Ordinal);
        // doc 关闭后子规则索引中的 doc 行整体移除
        Assert.DoesNotContain("| `doc-AGENTS.md` |", root.Content, StringComparison.Ordinal);
        // 其余能力不受影响
        Assert.Contains(files, file => file.Output == "infra-AGENTS.md");
        Assert.Contains(files, file => file.Output == "design-system.yaml");
    }

    [Fact]
    public void Toggle_InfraOff_RemovesInfraFileAndNavigation()
    {
        var plan = PlanOf("reactLinkdotNet");
        var features = FeatureSelection.Defaults(_manifest, plan);
        features.Remove("infra");

        var files = _renderer.Plan(_manifest, plan, features);

        Assert.DoesNotContain(files, file => file.Output == "infra-AGENTS.md");
        var root = Assert.Single(files, file => file.Output == "AGENTS.md");
        Assert.DoesNotContain("infra-AGENTS.md", root.Content, StringComparison.Ordinal);
        // 根模板的行级条件也同步移除
        Assert.DoesNotContain("按 `infra-AGENTS.md`「提交前最小回归」执行", root.Content, StringComparison.Ordinal);
        Assert.Contains(files, file => file.Output == "doc-AGENTS.md");
    }

    [Fact]
    public void Toggle_DesignSystemOff_RemovesFileAndRenumbersReadingOrder()
    {
        var plan = PlanOf("reactLinkdotNet");
        var features = FeatureSelection.Defaults(_manifest, plan);
        features.Remove("design-system");

        var files = _renderer.Plan(_manifest, plan, features);

        Assert.DoesNotContain(files, file => file.Output == "design-system.yaml");
        var root = Assert.Single(files, file => file.Output == "AGENTS.md");
        Assert.DoesNotContain("design-system.yaml", root.Content, StringComparison.Ordinal);
        // reading-order 移除 design-system 条目后重新编号：原第 4 条变为第 3 条
        Assert.Contains("3. 再阅读相关模块与现有实现，确认复用点后再动手。", root.Content, StringComparison.Ordinal);
        Assert.DoesNotContain("4. 再阅读相关模块", root.Content, StringComparison.Ordinal);
        // doc 行落到无 design-system 的 doc 档，doc 文件本身保留
        Assert.Contains("| `doc-AGENTS.md` | 设计文档、契约说明 | 涉及阶段 0、文档维护或契约说明时 |", root.Content, StringComparison.Ordinal);
        Assert.Contains(files, file => file.Output == "doc-AGENTS.md");
    }

    [Fact]
    public void Toggle_AllToggleableOff_KeepsCoreFilesOnly()
    {
        var plan = PlanOf("reactLinkdotNet");
        var features = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var files = _renderer.Plan(_manifest, plan, features);

        // 核心（AGENTS.md、collaboration、frontend、backend、CLAUDE/GEMINI/GROK）保留，能力文件全部移除
        Assert.Equal(
            new[] { "AGENTS.md", "collaboration-AGENTS.md", "frontend-AGENTS.md", "backend-AGENTS.md", "CLAUDE.md", "GEMINI.md", "GROK.md" },
            files.Select(file => file.Output));
        var root = Assert.Single(files, file => file.Output == "AGENTS.md");
        Assert.DoesNotContain("doc-AGENTS.md", root.Content, StringComparison.Ordinal);
        Assert.DoesNotContain("infra-AGENTS.md", root.Content, StringComparison.Ordinal);
        Assert.DoesNotContain("design-system.yaml", root.Content, StringComparison.Ordinal);
    }

    [Fact]
    public void Availability_UnavailableFeaturesHidden()
    {
        // dotnet-layered 没有界面与设计系统资源，也没有 AI 模块：Q5 只能出现 doc / infra
        var plan = PlanOf("dotnet-layered");
        var available = FeatureSelection.Available(_manifest, plan);

        Assert.Equal(new[] { "doc", "infra" }, available.Select(feature => feature.Id));
        Assert.False(FeatureSelection.IsAvailable(_manifest, plan, "design-system"));
        Assert.False(FeatureSelection.IsAvailable(_manifest, plan, "ai-capability"));
    }

    [Fact]
    public void Availability_FrontendOnlyCaseExposesDocInfraDesignSystem()
    {
        // 纯前端组合：Q5 可勾选 doc / infra / design-system，无 AI 模块不可选
        var plan = PlanOf("reactOnly");
        var available = FeatureSelection.Available(_manifest, plan);

        Assert.Equal(new[] { "doc", "infra", "design-system" }, available.Select(feature => feature.Id));
        Assert.False(FeatureSelection.IsAvailable(_manifest, plan, "ai-capability"));
    }

    [Fact]
    public void Availability_FrontendOnlyAiCaseKeepsAiByDefault()
    {
        // 纯前端 AI 变体：AI 由问答决定且默认保留（前端 AI 模块），doc / infra / design-system 可勾选
        var plan = PlanOf("reactOnly-ai");
        var defaults = FeatureSelection.Defaults(_manifest, plan);

        Assert.Equal(
            new[] { "doc", "infra", "ai-capability", "design-system" },
            FeatureSelection.Available(_manifest, plan).Select(feature => feature.Id));
        Assert.Contains("ai-capability", defaults);
        var files = _renderer.Plan(_manifest, plan, defaults);
        Assert.Contains(files, file => file.Output == "ai-AGENTS.md");
    }

    [Fact]
    public void Availability_AiCaseKeepsAiByDefault()
    {
        // AI 能力由向导问答这一唯一事实来源决定；含 AI 模块的组合默认启用
        var plan = PlanOf("reactLinkdotNet-ai");
        var defaults = FeatureSelection.Defaults(_manifest, plan);

        Assert.Contains("ai-capability", defaults);
        Assert.True(FeatureSelection.IsAvailable(_manifest, plan, "ai-capability"));
        var files = _renderer.Plan(_manifest, plan, defaults);
        Assert.Contains(files, file => file.Output == "ai-AGENTS.md");
    }

    [Fact]
    public void Normalize_DropsUnavailableSelection()
    {
        // 不可用能力不得因默认值或误选被启用
        var plan = PlanOf("dotnet-layered");
        var normalized = FeatureSelection.Normalize(
            _manifest, plan, new[] { "doc", "design-system", "ai-capability", "not-a-feature" });

        Assert.Equal(new[] { "doc" }, normalized.OrderBy(id => id, StringComparer.Ordinal));
    }

    [Fact]
    public void Validate_FeatureAwareContractPasses()
    {
        var validator = new RootConsistencyValidator();
        var plan = PlanOf("reactLinkdotNet");
        var features = FeatureSelection.Defaults(_manifest, plan);
        features.Remove("doc");
        features.Remove("design-system");

        Assert.Empty(validator.Validate(_manifest, _renderer, plan, features, "reactLinkdotNet"));
    }

    [Fact]
    public void Plan_FeatureMarkersAlwaysStripped()
    {
        // 根模板的 if-feature 标记是渲染指令，任何能力状态下都不得残留在产物中
        foreach (var caseItem in _manifest.CompatibilityCases)
        {
            var plan = PlanOf(caseItem.Profile);
            var files = _renderer.Plan(_manifest, plan, FeatureSelection.Defaults(_manifest, plan));
            Assert.All(files, file => Assert.DoesNotContain("if-feature", file.Content, StringComparison.Ordinal));
        }
    }
}
