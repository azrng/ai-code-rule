namespace RuleAssembler.Tests;

public sealed class ManifestLoaderTests
{
    [Fact]
    public void Load_CurrentManifest_SucceedsAndIncludesGrokPointer()
    {
        var manifest = new ManifestLoader(new ResourceStore()).Load();

        Assert.Equal(34, manifest.Profiles.Count);
        Assert.Contains(
            manifest.Modules.SelectMany(module => module.Pointers),
            pointer => pointer.Equals("GROK.md", StringComparison.OrdinalIgnoreCase));

        // 纯前端组合：form=web + backend=none，问答 Q2 才能推出「无后端」选项
        // （维度取值唯一事实来源是兼容样例，Profile 不再声明 traits）
        var reactOnlyCase = Assert.Single(manifest.CompatibilityCases, item => item.Profile == "reactOnly").Selection;
        Assert.Equal("web", reactOnlyCase["form"]);
        Assert.Equal("none", reactOnlyCase["backend"]);
        Assert.Equal("react", reactOnlyCase["frontend"]);
        Assert.Equal("frontend-static", Assert.Single(manifest.Profiles, profile => profile.Id == "reactOnly").Family);
        var vueOnlyCase = Assert.Single(manifest.CompatibilityCases, item => item.Profile == "vueOnly").Selection;
        Assert.Equal("vue", vueOnlyCase["frontend"]);
        Assert.Equal("none", vueOnlyCase["backend"]);
        var generalFrontendCase = Assert.Single(manifest.CompatibilityCases, item => item.Profile == "generalFrontend").Selection;
        Assert.Equal("general", generalFrontendCase["frontend"]);
        Assert.Equal("none", generalFrontendCase["backend"]);

        // 纯前端 AI 变体：挂前端口径的 AI 模块，文档 / 基础设施骨架追加 AI 补充段
        var reactOnlyAi = Assert.Single(manifest.Profiles, profile => profile.Id == "reactOnly-ai");
        Assert.Equal("yes", Assert.Single(manifest.CompatibilityCases, item => item.Profile == "reactOnly-ai").Selection["ai-capability"]);
        Assert.Contains(reactOnlyAi.ModuleIds, id => id == "ai-capability-frontend");
        Assert.Contains(
            reactOnlyAi.Subrules,
            rule => rule.GetSources().Contains("shared/skeletons/frontend-ai/frontend-ai-appendix.md"));
        var vueOnlyAi = Assert.Single(manifest.Profiles, profile => profile.Id == "vueOnly-ai");
        Assert.Equal("yes", Assert.Single(manifest.CompatibilityCases, item => item.Profile == "vueOnly-ai").Selection["ai-capability"]);
        Assert.Contains(vueOnlyAi.ModuleIds, id => id == "ai-capability-frontend");

        // Electron + .NET 拆为 React / Vue 双变体，backend/doc/infra 共享骨架
        var reactElectron = Assert.Single(manifest.Profiles, profile => profile.Id == "reactElectronDotNet");
        var reactElectronCase = Assert.Single(manifest.CompatibilityCases, item => item.Profile == "reactElectronDotNet").Selection;
        Assert.Equal("react", reactElectronCase["frontend"]);
        Assert.Equal("electron", reactElectronCase["shell"]);
        Assert.Contains(
            reactElectron.Subrules,
            rule => rule.GetSources().Contains("shared/skeletons/electron-dotnet/backend-AGENTS.md"));
        var vueElectron = Assert.Single(manifest.Profiles, profile => profile.Id == "vueElectronDotNet");
        var vueElectronCase = Assert.Single(manifest.CompatibilityCases, item => item.Profile == "vueElectronDotNet").Selection;
        Assert.Equal("vue", vueElectronCase["frontend"]);
        Assert.Equal("electron", vueElectronCase["shell"]);

        // 纯 Electron 变体：无 .NET 后端，本地逻辑由主进程承担，backend/doc/infra 走 electron 族骨架
        var reactElectronOnly = Assert.Single(manifest.Profiles, profile => profile.Id == "reactElectron");
        var reactElectronOnlyCase = Assert.Single(manifest.CompatibilityCases, item => item.Profile == "reactElectron").Selection;
        Assert.Equal("react", reactElectronOnlyCase["frontend"]);
        Assert.Equal("none", reactElectronOnlyCase["backend"]);
        Assert.Equal("electron", reactElectronOnlyCase["shell"]);
        Assert.Contains(
            reactElectronOnly.Subrules,
            rule => rule.GetSources().Contains("shared/skeletons/electron/backend-AGENTS.md"));
        var vueElectronOnly = Assert.Single(manifest.Profiles, profile => profile.Id == "vueElectron");
        var vueElectronOnlyCase = Assert.Single(manifest.CompatibilityCases, item => item.Profile == "vueElectron").Selection;
        Assert.Equal("vue", vueElectronOnlyCase["frontend"]);
        Assert.Equal("none", vueElectronOnlyCase["backend"]);
        Assert.Equal("electron", vueElectronOnlyCase["shell"]);

        var blazorServer = Assert.Single(manifest.Profiles, profile => profile.Id == "dotnetBlazor");
        Assert.Contains("Blazor Server", blazorServer.Label);
        Assert.Contains(blazorServer.Subrules, rule => rule.Source == "arch/dotnetBlazor/ui-AGENTS.md");

        var blazorWebApp = Assert.Single(manifest.Profiles, profile => profile.Id == "dotnetBlazorWebApp");
        Assert.Contains(blazorWebApp.Subrules, rule => rule.Source == "arch/dotnetBlazorWebApp/ui-AGENTS.md");
        Assert.Contains(blazorWebApp.Subrules, rule => rule.Source == "arch/dotnetBlazor/backend-AGENTS.md");
        Assert.Equal("arch/dotnetBlazor/design-system.yaml", blazorWebApp.DesignSystemSource);

        var reactTauri = Assert.Single(manifest.Profiles, profile => profile.Id == "reactTauriExe");
        Assert.Equal("前后端或跨多个领域", reactTauri.Root.ContextScopeNouns);
        Assert.Equal("前端、Rust 后端还是基础设施", reactTauri.Root.TaskKindNouns);
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
    public void Validate_DuplicateProfilePointer_Throws()
    {
        var validator = new ManifestValidator(new ResourceStore());
        var manifest = new ManifestLoader(new ResourceStore()).Load();
        var profile = manifest.Profiles[0];
        profile.Pointers.Add("GROK.md");
        profile.Pointers.Add("grok.md");

        var exception = Assert.Throws<InvalidOperationException>(() => validator.Validate(manifest));

        Assert.Contains("pointer 输出重复", exception.Message);
    }

    [Fact]
    public void Validate_SubruleConflictingWithRootOutput_Throws()
    {
        var validator = new ManifestValidator(new ResourceStore());
        var manifest = new ManifestLoader(new ResourceStore()).Load();
        var profile = manifest.Profiles[0];
        var source = manifest.Modules.SelectMany(module => module.Files).First().Source;
        profile.Subrules.Add(new Subrule { Source = source, Output = "agents.md" });

        var exception = Assert.Throws<InvalidOperationException>(() => validator.Validate(manifest));

        Assert.Contains("Profile", exception.Message);
        Assert.Contains("输出重复", exception.Message);
        Assert.Contains("agents.md", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_ProfileIdWithTraversal_Throws()
    {
        var validator = new ManifestValidator(new ResourceStore());
        var manifest = new ManifestLoader(new ResourceStore()).Load();
        manifest.Profiles[0].Id = "../outside";

        var exception = Assert.Throws<InvalidOperationException>(() => validator.Validate(manifest));

        Assert.Contains("安全的单级目录名", exception.Message);
    }

    [Fact]
    public void Validate_OutputPathAliases_Throws()
    {
        var validator = new ManifestValidator(new ResourceStore());
        var manifest = new ManifestLoader(new ResourceStore()).Load();
        var profile = manifest.Profiles[0];
        var source = manifest.Modules.SelectMany(module => module.Files).First().Source;
        profile.Subrules.Add(new Subrule { Source = source, Output = "nested/./rule.md" });
        profile.Subrules.Add(new Subrule { Source = source, Output = "nested\\rule.md" });

        var exception = Assert.Throws<InvalidOperationException>(() => validator.Validate(manifest));

        Assert.Contains("nested/rule.md", exception.Message);
    }
}
