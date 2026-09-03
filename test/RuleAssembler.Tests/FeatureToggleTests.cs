namespace RuleAssembler.Tests;

// P1-3 Q5 能力开关：按 features 过滤输出与根导航联动
public sealed class FeatureToggleTests
{
    private readonly Manifest _manifest;
    private readonly RuleRenderer _renderer;

    public FeatureToggleTests()
    {
        _manifest = new ManifestLoader(new ResourceStore()).Load();
        _renderer = new RuleRenderer(new ResourceStore());
    }

    private Profile ProfileOf(string id)
    {
        return Assert.Single(_manifest.Profiles, profile => profile.Id == id);
    }

    [Fact]
    public void Defaults_MatchLegacyPlan()
    {
        // 默认能力集 = Profile 当前组合：带能力渲染与旧入口 Plan 输出必须完全一致（基线一致）
        foreach (var profile in _manifest.Profiles)
        {
            var legacy = _renderer.Plan(_manifest, profile);
            var byFeatures = _renderer.Plan(_manifest, profile, FeatureSelection.Defaults(_manifest, profile));
            Assert.Equal(legacy.Select(file => file.Output), byFeatures.Select(file => file.Output));
            Assert.Equal(legacy.Select(file => file.Content), byFeatures.Select(file => file.Content));
        }
    }

    [Fact]
    public void Toggle_DocOff_RemovesDocFileAndNavigation()
    {
        var profile = ProfileOf("reactLinkdotNet");
        var features = FeatureSelection.Defaults(_manifest, profile);
        features.Remove("doc");

        var files = _renderer.Plan(_manifest, profile, features);

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
        var profile = ProfileOf("reactLinkdotNet");
        var features = FeatureSelection.Defaults(_manifest, profile);
        features.Remove("infra");

        var files = _renderer.Plan(_manifest, profile, features);

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
        var profile = ProfileOf("reactLinkdotNet");
        var features = FeatureSelection.Defaults(_manifest, profile);
        features.Remove("design-system");

        var files = _renderer.Plan(_manifest, profile, features);

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
        var profile = ProfileOf("reactLinkdotNet");
        var features = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var files = _renderer.Plan(_manifest, profile, features);

        // 核心（AGENTS.md、frontend、backend、CLAUDE/GEMINI/GROK）保留，能力文件全部移除
        Assert.Equal(
            new[] { "AGENTS.md", "frontend-AGENTS.md", "backend-AGENTS.md", "CLAUDE.md", "GEMINI.md", "GROK.md" },
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
        var profile = ProfileOf("dotnet-layered");
        var available = FeatureSelection.Available(_manifest, profile);

        Assert.Equal(new[] { "doc", "infra" }, available.Select(feature => feature.Id));
        Assert.False(FeatureSelection.IsAvailable(_manifest, profile, "design-system"));
        Assert.False(FeatureSelection.IsAvailable(_manifest, profile, "ai-capability"));
    }

    [Fact]
    public void Availability_FrontendOnlyProfileExposesDocInfraDesignSystem()
    {
        // 纯前端组合：Q5 可勾选 doc / infra / design-system，无 AI 模块不可选
        var profile = ProfileOf("reactOnly");
        var available = FeatureSelection.Available(_manifest, profile);

        Assert.Equal(new[] { "doc", "infra", "design-system" }, available.Select(feature => feature.Id));
        Assert.False(FeatureSelection.IsAvailable(_manifest, profile, "ai-capability"));
    }

    [Fact]
    public void Availability_FrontendOnlyAiProfileKeepsAiByDefault()
    {
        // 纯前端 AI 变体：AI 由问答决定且默认保留（前端 AI 模块），doc / infra / design-system 可勾选
        var profile = ProfileOf("reactOnly-ai");
        var defaults = FeatureSelection.Defaults(_manifest, profile);

        Assert.Equal(
            new[] { "doc", "infra", "ai-capability", "design-system" },
            FeatureSelection.Available(_manifest, profile).Select(feature => feature.Id));
        Assert.Contains("ai-capability", defaults);
        var files = _renderer.Plan(_manifest, profile, defaults);
        Assert.Contains(files, file => file.Output == "ai-AGENTS.md");
    }

    [Fact]
    public void Availability_AiProfileKeepsAiByDefault()
    {
        // AI 能力由向导问答（Profile 选择）这一唯一事实来源决定；含 AI 模块的 Profile 默认启用
        var profile = ProfileOf("reactLinkdotNet-ai");
        var defaults = FeatureSelection.Defaults(_manifest, profile);

        Assert.Contains("ai-capability", defaults);
        Assert.True(FeatureSelection.IsAvailable(_manifest, profile, "ai-capability"));
        var files = _renderer.Plan(_manifest, profile, defaults);
        Assert.Contains(files, file => file.Output == "ai-AGENTS.md");
    }

    [Fact]
    public void Normalize_DropsUnavailableSelection()
    {
        // 不可用能力不得因默认值或误选被启用
        var profile = ProfileOf("dotnet-layered");
        var normalized = FeatureSelection.Normalize(
            _manifest, profile, new[] { "doc", "design-system", "ai-capability", "not-a-feature" });

        Assert.Equal(new[] { "doc" }, normalized.OrderBy(id => id, StringComparer.Ordinal));
    }

    [Fact]
    public void Validate_FeatureAwareContractPasses()
    {
        var validator = new RootConsistencyValidator();
        var profile = ProfileOf("reactLinkdotNet");
        var features = FeatureSelection.Defaults(_manifest, profile);
        features.Remove("doc");
        features.Remove("design-system");

        Assert.Empty(validator.Validate(_manifest, _renderer, profile, features));
    }

    [Fact]
    public void Plan_FeatureMarkersAlwaysStripped()
    {
        // 根模板的 if-feature 标记是渲染指令，任何能力状态下都不得残留在产物中
        foreach (var profile in _manifest.Profiles)
        {
            var files = _renderer.Plan(_manifest, profile);
            Assert.All(files, file => Assert.DoesNotContain("if-feature", file.Content, StringComparison.Ordinal));
        }
    }
}
