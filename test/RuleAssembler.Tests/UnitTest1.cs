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
        var reactOnly = Assert.Single(manifest.Profiles, profile => profile.Id == "reactOnly");
        Assert.Equal("frontend-static", reactOnly.Family);
        Assert.Equal("web", reactOnly.Traits!.Form);
        Assert.Equal("none", reactOnly.Traits.Backend);
        Assert.Equal("react", reactOnly.Traits.Frontend);
        var vueOnly = Assert.Single(manifest.Profiles, profile => profile.Id == "vueOnly");
        Assert.Equal("vue", vueOnly.Traits!.Frontend);
        Assert.Equal("none", vueOnly.Traits.Backend);
        var generalFrontend = Assert.Single(manifest.Profiles, profile => profile.Id == "generalFrontend");
        Assert.Equal("general", generalFrontend.Traits!.Frontend);
        Assert.Equal("none", generalFrontend.Traits.Backend);

        // 纯前端 AI 变体：挂前端口径的 AI 模块，文档 / 基础设施骨架追加 AI 补充段
        var reactOnlyAi = Assert.Single(manifest.Profiles, profile => profile.Id == "reactOnly-ai");
        Assert.Equal("yes", reactOnlyAi.Traits!.AiCapability);
        Assert.Equal("none", reactOnlyAi.Traits.Backend);
        Assert.Contains(reactOnlyAi.ModuleIds, id => id == "ai-capability-frontend");
        Assert.Contains(
            reactOnlyAi.Subrules,
            rule => rule.GetSources().Contains("shared/skeletons/frontend-ai/frontend-ai-appendix.md"));
        var vueOnlyAi = Assert.Single(manifest.Profiles, profile => profile.Id == "vueOnly-ai");
        Assert.Equal("yes", vueOnlyAi.Traits!.AiCapability);
        Assert.Contains(vueOnlyAi.ModuleIds, id => id == "ai-capability-frontend");

        // Electron + .NET 拆为 React / Vue 双变体，backend/doc/infra 共享骨架
        var reactElectron = Assert.Single(manifest.Profiles, profile => profile.Id == "reactElectronDotNet");
        Assert.Equal("react", reactElectron.Traits!.Frontend);
        Assert.Equal("electron", reactElectron.Traits!.Shell);
        Assert.Contains(
            reactElectron.Subrules,
            rule => rule.GetSources().Contains("shared/skeletons/electron-dotnet/backend-AGENTS.md"));
        var vueElectron = Assert.Single(manifest.Profiles, profile => profile.Id == "vueElectronDotNet");
        Assert.Equal("vue", vueElectron.Traits!.Frontend);
        Assert.Equal("electron", vueElectron.Traits!.Shell);

        // 纯 Electron 变体：无 .NET 后端，本地逻辑由主进程承担，backend/doc/infra 走 electron 族骨架
        var reactElectronOnly = Assert.Single(manifest.Profiles, profile => profile.Id == "reactElectron");
        Assert.Equal("react", reactElectronOnly.Traits!.Frontend);
        Assert.Equal("none", reactElectronOnly.Traits!.Backend);
        Assert.Equal("electron", reactElectronOnly.Traits!.Shell);
        Assert.Contains(
            reactElectronOnly.Subrules,
            rule => rule.GetSources().Contains("shared/skeletons/electron/backend-AGENTS.md"));
        var vueElectronOnly = Assert.Single(manifest.Profiles, profile => profile.Id == "vueElectron");
        Assert.Equal("vue", vueElectronOnly.Traits!.Frontend);
        Assert.Equal("none", vueElectronOnly.Traits!.Backend);
        Assert.Equal("electron", vueElectronOnly.Traits!.Shell);

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
        var loader = new ManifestLoader(new ResourceStore());
        var manifest = loader.Load();
        var module = Assert.Single(manifest.Modules, item => item.Pointers.Count > 0);
        module.Pointers.Add(module.Pointers[0].ToUpperInvariant());

        var exception = Assert.Throws<InvalidOperationException>(() => loader.Validate(manifest));

        Assert.Contains("pointer 输出重复", exception.Message);
    }

    [Fact]
    public void Validate_DuplicateProfilePointer_Throws()
    {
        var loader = new ManifestLoader(new ResourceStore());
        var manifest = loader.Load();
        var profile = manifest.Profiles[0];
        profile.Pointers.Add("GROK.md");
        profile.Pointers.Add("grok.md");

        var exception = Assert.Throws<InvalidOperationException>(() => loader.Validate(manifest));

        Assert.Contains("pointer 输出重复", exception.Message);
    }

    [Fact]
    public void Validate_SubruleConflictingWithRootOutput_Throws()
    {
        var loader = new ManifestLoader(new ResourceStore());
        var manifest = loader.Load();
        var profile = manifest.Profiles[0];
        var source = manifest.Modules.SelectMany(module => module.Files).First().Source;
        profile.Subrules.Add(new Subrule { Source = source, Output = "agents.md" });

        var exception = Assert.Throws<InvalidOperationException>(() => loader.Validate(manifest));

        Assert.Contains("Profile", exception.Message);
        Assert.Contains("输出重复", exception.Message);
        Assert.Contains("agents.md", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_ProfileIdWithTraversal_Throws()
    {
        var loader = new ManifestLoader(new ResourceStore());
        var manifest = loader.Load();
        manifest.Profiles[0].Id = "../outside";

        var exception = Assert.Throws<InvalidOperationException>(() => loader.Validate(manifest));

        Assert.Contains("安全的单级目录名", exception.Message);
    }

    [Fact]
    public void Validate_OutputPathAliases_Throws()
    {
        var loader = new ManifestLoader(new ResourceStore());
        var manifest = loader.Load();
        var profile = manifest.Profiles[0];
        var source = manifest.Modules.SelectMany(module => module.Files).First().Source;
        profile.Subrules.Add(new Subrule { Source = source, Output = "nested/./rule.md" });
        profile.Subrules.Add(new Subrule { Source = source, Output = "nested\\rule.md" });

        var exception = Assert.Throws<InvalidOperationException>(() => loader.Validate(manifest));

        Assert.Contains("nested/rule.md", exception.Message);
    }
}
