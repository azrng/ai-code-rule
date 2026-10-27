namespace RuleAssembler.Tests;

public sealed class ManifestLoaderTests
{
    [Fact]
    public void Load_CurrentManifest_SucceedsAndIncludesGrokPointer()
    {
        var manifest = new ManifestLoader(new ResourceStore()).Load();

        Assert.Equal(3, manifest.SchemaVersion);
        Assert.Equal(36, manifest.CompatibilityCases.Count);
        Assert.Contains(
            manifest.Modules.SelectMany(module => module.Pointers),
            pointer => pointer.Equals("GROK.md", StringComparison.OrdinalIgnoreCase));

        // 样例携带展示元数据（label / family），经典入口与配方三件套按样例取值
        var reactOnly = Assert.Single(manifest.CompatibilityCases, item => item.Profile == "reactOnly");
        Assert.Equal("web", reactOnly.Selection["form"]);
        Assert.Equal("none", reactOnly.Selection["backend"]);
        Assert.Equal("react", reactOnly.Selection["frontend"]);
        Assert.Equal("frontend-static", reactOnly.Family);
        Assert.False(string.IsNullOrWhiteSpace(reactOnly.Label));
        var vueOnlyCase = Assert.Single(manifest.CompatibilityCases, item => item.Profile == "vueOnly").Selection;
        Assert.Equal("vue", vueOnlyCase["frontend"]);
        Assert.Equal("none", vueOnlyCase["backend"]);
        var generalFrontendCase = Assert.Single(manifest.CompatibilityCases, item => item.Profile == "generalFrontend").Selection;
        Assert.Equal("general", generalFrontendCase["frontend"]);
        Assert.Equal("none", generalFrontendCase["backend"]);

        var planner = new CompositionPlanner(manifest);

        // 纯前端 AI 变体：AI 能力模块的产物经组件文件贡献进入组合（ai-AGENTS.md 与骨架追加段）
        var reactOnlyAiSelection = Assert.Single(manifest.CompatibilityCases, item => item.Profile == "reactOnly-ai").Selection;
        Assert.Equal("yes", reactOnlyAiSelection["ai-capability"]);
        var reactOnlyAiPlan = planner.Plan(new Selection { Dimensions = reactOnlyAiSelection });
        Assert.Contains(reactOnlyAiPlan.Files, rule => rule.Output == "ai-AGENTS.md");
        Assert.Contains(
            reactOnlyAiPlan.Files,
            rule => rule.GetSources().Contains("shared/skeletons/frontend-ai/frontend-ai-appendix.md"));
        var vueOnlyAiSelection = Assert.Single(manifest.CompatibilityCases, item => item.Profile == "vueOnly-ai").Selection;
        Assert.Equal("yes", vueOnlyAiSelection["ai-capability"]);
        Assert.Contains(
            planner.Plan(new Selection { Dimensions = vueOnlyAiSelection }).Files,
            rule => rule.Output == "ai-AGENTS.md");

        // Electron + .NET 拆为 React / Vue 双变体，backend/doc/infra 共享骨架
        var reactElectronCase = Assert.Single(manifest.CompatibilityCases, item => item.Profile == "reactElectronDotNet").Selection;
        Assert.Equal("react", reactElectronCase["frontend"]);
        Assert.Equal("electron", reactElectronCase["shell"]);
        Assert.Contains(
            planner.Plan(new Selection { Dimensions = reactElectronCase }).Files,
            rule => rule.GetSources().Contains("shared/skeletons/electron-dotnet/backend-AGENTS.md"));
        var vueElectronCase = Assert.Single(manifest.CompatibilityCases, item => item.Profile == "vueElectronDotNet").Selection;
        Assert.Equal("vue", vueElectronCase["frontend"]);
        Assert.Equal("electron", vueElectronCase["shell"]);

        // 纯 Electron 变体：无 .NET 后端，本地逻辑由主进程承担，backend/doc/infra 走 electron 族骨架
        var reactElectronOnlyCase = Assert.Single(manifest.CompatibilityCases, item => item.Profile == "reactElectron").Selection;
        Assert.Equal("react", reactElectronOnlyCase["frontend"]);
        Assert.Equal("none", reactElectronOnlyCase["backend"]);
        Assert.Equal("electron", reactElectronOnlyCase["shell"]);
        Assert.Contains(
            planner.Plan(new Selection { Dimensions = reactElectronOnlyCase }).Files,
            rule => rule.GetSources().Contains("shared/skeletons/electron/backend-AGENTS.md"));
        var vueElectronOnlyCase = Assert.Single(manifest.CompatibilityCases, item => item.Profile == "vueElectron").Selection;
        Assert.Equal("vue", vueElectronOnlyCase["frontend"]);
        Assert.Equal("none", vueElectronOnlyCase["backend"]);
        Assert.Equal("electron", vueElectronOnlyCase["shell"]);

        var blazorServer = Assert.Single(manifest.CompatibilityCases, item => item.Profile == "dotnetBlazor");
        Assert.Contains("Blazor Server", blazorServer.Label);
        Assert.Contains(
            planner.Plan(new Selection { Dimensions = blazorServer.Selection }).Files,
            rule => rule.Source == "arch/dotnetBlazor/ui-AGENTS.md");

        var blazorWebAppPlan = planner.Plan(new Selection
        {
            Dimensions = Assert.Single(manifest.CompatibilityCases, item => item.Profile == "dotnetBlazorWebApp").Selection
        });
        Assert.Contains(blazorWebAppPlan.Files, rule => rule.Source == "arch/dotnetBlazorWebApp/ui-AGENTS.md");
        Assert.Contains(blazorWebAppPlan.Files, rule => rule.Source == "arch/dotnetBlazor/backend-AGENTS.md");
        Assert.Equal("arch/dotnetBlazor/design-system.yaml", blazorWebAppPlan.DesignSystemSource);

        var reactTauriPlan = planner.Plan(new Selection
        {
            Dimensions = Assert.Single(manifest.CompatibilityCases, item => item.Profile == "reactTauriExe").Selection
        });
        Assert.Equal("前后端或跨多个领域", reactTauriPlan.Root.ContextScopeNouns);
        Assert.Equal("前端、Rust 后端还是基础设施", reactTauriPlan.Root.TaskKindNouns);
    }

    [Fact]
    public void Validate_DuplicateModulePointer_Throws()
    {
        var validator = new ManifestValidator(new ResourceStore());
        var manifest = new ManifestLoader(new ResourceStore()).Load();
        var module = Assert.Single(manifest.Modules, item => item.Pointers.Count > 0);
        module.Pointers.Add(module.Pointers[0].ToUpperInvariant());

        var exception = Assert.Throws<InvalidOperationException>(() => validator.Validate(manifest));

        Assert.Contains("pointer 输出重复", exception.Message);
    }

    [Fact]
    public void Validate_CaseIdWithTraversal_Throws()
    {
        var validator = new ManifestValidator(new ResourceStore());
        var manifest = new ManifestLoader(new ResourceStore()).Load();
        manifest.CompatibilityCases[0].Profile = "../outside";

        var exception = Assert.Throws<InvalidOperationException>(() => validator.Validate(manifest));

        Assert.Contains("安全的单级目录名", exception.Message);
    }

    [Fact]
    public void Validate_CaseWithoutLabel_Throws()
    {
        var validator = new ManifestValidator(new ResourceStore());
        var manifest = new ManifestLoader(new ResourceStore()).Load();
        manifest.CompatibilityCases[0].Label = "";

        var exception = Assert.Throws<InvalidOperationException>(() => validator.Validate(manifest));

        Assert.Contains("缺少 label", exception.Message);
    }

    [Fact]
    public void Validate_CaseWithUnknownFamily_Throws()
    {
        var validator = new ManifestValidator(new ResourceStore());
        var manifest = new ManifestLoader(new ResourceStore()).Load();
        manifest.CompatibilityCases[0].Family = "no-such-family";

        var exception = Assert.Throws<InvalidOperationException>(() => validator.Validate(manifest));

        Assert.Contains("引用了不存在的 family", exception.Message);
    }

    [Fact]
    public void Validate_DuplicateCaseId_Throws()
    {
        var validator = new ManifestValidator(new ResourceStore());
        var manifest = new ManifestLoader(new ResourceStore()).Load();
        manifest.CompatibilityCases.Add(manifest.CompatibilityCases[0]);

        var exception = Assert.Throws<InvalidOperationException>(() => validator.Validate(manifest));

        Assert.Contains("样例 ID 重复", exception.Message);
    }
}
