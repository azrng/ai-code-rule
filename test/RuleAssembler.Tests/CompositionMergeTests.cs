using Xunit;

namespace RuleAssembler.Tests;

public sealed class CompositionMergeTests
{
    private static Manifest LoadManifest()
    {
        return new ManifestLoader(new ResourceStore()).Load();
    }

    private static Selection GeneralProjectSelection()
    {
        return new Selection
        {
            Dimensions = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["form"] = "general"
            }
        };
    }

    private static ComponentDefinition AppendComponent(
        string id,
        string source,
        string feature = "")
    {
        return new ComponentDefinition
        {
            Id = id,
            Kind = ComponentDefinition.KindCapability,
            Label = id,
            When = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["form"] = "general"
            },
            Files =
            {
                new Subrule
                {
                    Source = source,
                    Output = "frontend-AGENTS.md",
                    Feature = feature,
                    Merge = OutputDeclaration.MergeAppend
                }
            }
        };
    }

    private static ComponentDefinition ReplaceComponent(string id, string source)
    {
        var component = AppendComponent(id, source);
        component.Files[0].Merge = OutputDeclaration.MergeReplace;
        return component;
    }

    [Fact]
    public void 组件模块按组件顺序并入且core始终在首位()
    {
        var manifest = LoadManifest();
        manifest.Components.Add(new ComponentDefinition
        {
            Id = "test-module-capability",
            Kind = ComponentDefinition.KindCapability,
            Label = "测试模块能力",
            When = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["form"] = "general"
            },
            ModuleIds = { "ai-capability", "core" }
        });

        var plan = new CompositionPlanner(manifest).Plan(GeneralProjectSelection());

        Assert.Equal(new[] { "core", "ai-capability" }, plan.ModuleIds);
    }

    [Fact]
    public void 组件引用未知模块时清单校验报错()
    {
        var validator = new ManifestValidator(new ResourceStore());
        var manifest = LoadManifest();
        manifest.Components.Add(new ComponentDefinition
        {
            Id = "test-missing-module",
            Kind = ComponentDefinition.KindCapability,
            Label = "测试未知模块",
            When = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["form"] = "general"
            },
            ModuleIds = { "missing-module" }
        });

        var ex = Assert.Throws<InvalidOperationException>(() => validator.Validate(manifest));

        Assert.Contains("component test-missing-module 引用了不存在的 module：missing-module", ex.Message);
    }

    [Fact]
    public void 同一输出按replace基底和append声明顺序合并且逐项过滤能力()
    {
        var manifest = LoadManifest();
        manifest.Components.Add(AppendComponent(
            "test-append-testing",
            "shared/frontend/testing.md"));
        manifest.Components.Add(AppendComponent(
            "test-append-form",
            "shared/frontend/form-rules.md",
            "doc"));

        var planner = new CompositionPlanner(manifest);
        var plan = planner.Plan(GeneralProjectSelection());
        var renderer = new RuleRenderer(new ResourceStore());

        var defaultFiles = renderer.Plan(manifest, plan, FeatureSelection.Defaults(manifest, plan));
        var defaultFrontend = defaultFiles.Single(file => file.Output == "frontend-AGENTS.md").Content;
        Assert.Contains("MSW（仅在需要模拟外部请求时使用）", defaultFrontend);
        Assert.Contains("React Hook Form + Zod", defaultFrontend);
        Assert.True(
            defaultFrontend.IndexOf("MSW（仅在需要模拟外部请求时使用）", StringComparison.Ordinal)
            < defaultFrontend.IndexOf("React Hook Form + Zod", StringComparison.Ordinal));

        var withoutDoc = FeatureSelection.Defaults(manifest, plan);
        withoutDoc.Remove("doc");
        var filteredFiles = renderer.Plan(manifest, plan, withoutDoc);
        var filteredFrontend = filteredFiles.Single(file => file.Output == "frontend-AGENTS.md").Content;
        Assert.Contains("MSW（仅在需要模拟外部请求时使用）", filteredFrontend);
        Assert.DoesNotContain("React Hook Form + Zod", filteredFrontend);
    }

    [Fact]
    public void 同一输出存在多个replace提供者时报错()
    {
        var manifest = LoadManifest();
        manifest.Components.Add(ReplaceComponent("test-replace-one", "shared/frontend/testing.md"));
        manifest.Components.Add(ReplaceComponent("test-replace-two", "shared/frontend/form-rules.md"));

        var ex = Assert.Throws<InvalidOperationException>(() =>
            new CompositionPlanner(manifest).Plan(GeneralProjectSelection()));

        Assert.Contains("多个 replace", ex.Message);
        Assert.Contains("test-replace-one", ex.Message);
        Assert.Contains("test-replace-two", ex.Message);
    }

    [Fact]
    public void 文件贡献独立执行槽位替换且文件级值覆盖计划级值()
    {
        var content = RuleRenderer.ApplyFileSlots(
            "{{slot:stage}} / {{slot:scope}}",
            new Subrule
            {
                Output = "frontend-AGENTS.md",
                Slots = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["stage"] = "文件级阶段"
                }
            },
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["stage"] = "计划级阶段",
                ["scope"] = "计划级范围"
            });

        Assert.Equal("文件级阶段 / 计划级范围", content);
    }

    [Fact]
    public void append贡献携带frontMatter时拒绝渲染()
    {
        var manifest = LoadManifest();
        manifest.Components.Add(AppendComponent(
            "test-append-front-matter",
            "arch/generalProject/frontend-AGENTS.md"));
        var plan = new CompositionPlanner(manifest).Plan(GeneralProjectSelection());

        var ex = Assert.Throws<InvalidOperationException>(() =>
            new RuleRenderer(new ResourceStore()).Plan(
                manifest,
                plan,
                FeatureSelection.Defaults(manifest, plan)));

        Assert.Contains("append", ex.Message);
        Assert.Contains("front matter", ex.Message);
    }
}
