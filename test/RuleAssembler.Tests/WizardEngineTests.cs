namespace RuleAssembler.Tests;

public sealed class WizardEngineTests
{
    private static Manifest LoadManifest()
    {
        return new ManifestLoader(new ResourceStore()).Load();
    }

    [Fact]
    public void SimulateAll_NoErrorsAndAllProfilesReachable()
    {
        var engine = new WizardEngine(LoadManifest());

        var errors = engine.SimulateAll();

        Assert.Empty(errors);
    }

    [Fact]
    public void Resolve_WebDotnetReactNoAi_ResolvesReactLinkdotNet()
    {
        var engine = new WizardEngine(LoadManifest());
        var answers = new Dictionary<string, string>
        {
            ["form"] = "web",
            ["backend"] = "dotnet",
            ["frontend"] = "react",
            ["ai-capability"] = "no"
        };

        var result = engine.Resolve(answers);

        var profile = Assert.Single(result);
        Assert.Equal("reactLinkdotNet", profile.Id);
    }

    [Fact]
    public void Resolve_DesktopGoReactWails_ResolvesReactWailsExe()
    {
        var engine = new WizardEngine(LoadManifest());
        var answers = new Dictionary<string, string>
        {
            ["form"] = "desktop",
            ["backend"] = "go",
            ["frontend"] = "react",
            ["shell"] = "wails"
        };

        var result = engine.Resolve(answers);

        var profile = Assert.Single(result);
        Assert.Equal("reactWailsExe", profile.Id);
    }

    [Fact]
    public void Resolve_DesktopDotnetReactElectron_ResolvesReactElectronDotNet()
    {
        var engine = new WizardEngine(LoadManifest());
        var answers = new Dictionary<string, string>
        {
            ["form"] = "desktop",
            ["backend"] = "dotnet",
            ["frontend"] = "react",
            ["shell"] = "electron"
        };

        var result = engine.Resolve(answers);

        var profile = Assert.Single(result);
        Assert.Equal("reactElectronDotNet", profile.Id);
    }

    [Fact]
    public void Resolve_DesktopDotnetVueElectron_ResolvesVueElectronDotNet()
    {
        var engine = new WizardEngine(LoadManifest());
        var answers = new Dictionary<string, string>
        {
            ["form"] = "desktop",
            ["backend"] = "dotnet",
            ["frontend"] = "vue",
            ["shell"] = "electron"
        };

        var result = engine.Resolve(answers);

        var profile = Assert.Single(result);
        Assert.Equal("vueElectronDotNet", profile.Id);
    }

    [Fact]
    public void Resolve_WebNoneReact_ResolvesReactOnly()
    {
        var engine = new WizardEngine(LoadManifest());
        var answers = new Dictionary<string, string>
        {
            ["form"] = "web",
            ["backend"] = "none",
            ["frontend"] = "react",
            ["ai-capability"] = "no"
        };

        var result = engine.Resolve(answers);

        var profile = Assert.Single(result);
        Assert.Equal("reactOnly", profile.Id);
    }

    [Fact]
    public void Resolve_WebNoneVue_ResolvesVueOnly()
    {
        var engine = new WizardEngine(LoadManifest());
        var answers = new Dictionary<string, string>
        {
            ["form"] = "web",
            ["backend"] = "none",
            ["frontend"] = "vue",
            ["ai-capability"] = "no"
        };

        var result = engine.Resolve(answers);

        var profile = Assert.Single(result);
        Assert.Equal("vueOnly", profile.Id);
    }

    [Fact]
    public void Resolve_WebNoneGeneral_ResolvesGeneralFrontend()
    {
        var engine = new WizardEngine(LoadManifest());
        var answers = new Dictionary<string, string>
        {
            ["form"] = "web",
            ["backend"] = "none",
            ["frontend"] = "general"
        };

        var result = engine.Resolve(answers);

        var profile = Assert.Single(result);
        Assert.Equal("generalFrontend", profile.Id);
    }

    [Fact]
    public void Resolve_WebNoneReactAi_ResolvesReactOnlyAi()
    {
        var engine = new WizardEngine(LoadManifest());
        var answers = new Dictionary<string, string>
        {
            ["form"] = "web",
            ["backend"] = "none",
            ["frontend"] = "react",
            ["ai-capability"] = "yes"
        };

        var result = engine.Resolve(answers);

        var profile = Assert.Single(result);
        Assert.Equal("reactOnly-ai", profile.Id);
    }

    [Fact]
    public void Resolve_WebNoneVueAi_ResolvesVueOnlyAi()
    {
        var engine = new WizardEngine(LoadManifest());
        var answers = new Dictionary<string, string>
        {
            ["form"] = "web",
            ["backend"] = "none",
            ["frontend"] = "vue",
            ["ai-capability"] = "yes"
        };

        var result = engine.Resolve(answers);

        var profile = Assert.Single(result);
        Assert.Equal("vueOnly-ai", profile.Id);
    }

    [Fact]
    public void NextQuestion_WebCandidates_OfferNoBackendOption()
    {
        // Web 候选加入纯前端组合后，Q2 后端问题必须出现「无后端」选项
        var manifest = LoadManifest();
        var engine = new WizardEngine(manifest);
        var candidates = engine.Filter(manifest.Profiles, "form", "web");

        var question = engine.NextQuestion(candidates);

        Assert.NotNull(question);
        Assert.Equal("backend", question.Trait);
        Assert.Contains(question.Options, option => option.Value == "none");
    }

    [Fact]
    public void NextQuestion_WebNoneReact_AskAiCapability()
    {
        // 纯前端 React 候选只剩 AI 维度分歧时，AI 问题在 Q5 之前出现（与全栈变体同构）
        var manifest = LoadManifest();
        var engine = new WizardEngine(manifest);
        var candidates = engine
            .Filter(engine.Filter(manifest.Profiles, "form", "web"), "backend", "none");
        candidates = engine.Filter(candidates, "frontend", "react");

        var question = engine.NextQuestion(candidates);

        Assert.NotNull(question);
        Assert.Equal("ai-capability", question.Trait);
        Assert.Equal(2, question.Options.Count);
    }

    [Fact]
    public void Matches_WildcardTraitMatchesAnyAnswer()
    {
        var manifest = LoadManifest();
        var general = manifest.Profiles.First(profile => profile.Id == "generalProject");

        Assert.True(WizardEngine.Matches(general, "backend", "dotnet"));
        Assert.True(WizardEngine.Matches(general, "frontend", "vue"));
    }

    [Fact]
    public void NextQuestion_SingleCandidateHasNoQuestion()
    {
        var manifest = LoadManifest();
        var engine = new WizardEngine(manifest);
        var python = manifest.Profiles.Where(profile => profile.Id == "python").ToList();

        Assert.Null(engine.NextQuestion(python));
    }

    [Fact]
    public void NextQuestion_BackendOnlyDotnet_AskArchStyle()
    {
        var manifest = LoadManifest();
        var engine = new WizardEngine(manifest);
        var candidates = engine.Filter(engine.Filter(manifest.Profiles, "form", "backend-only"), "backend", "dotnet");

        var question = engine.NextQuestion(candidates);

        Assert.NotNull(question);
        Assert.Equal("arch-style", question.Trait);
        Assert.Equal(2, question.Options.Count);
    }

    [Fact]
    public void NextQuestion_DesktopCandidates_AskBackendBeforeFrontend()
    {
        // 问答顺序对齐 Q1 形态 → Q2 后端 → Q3 前端 → Q4 壳
        var manifest = LoadManifest();
        var engine = new WizardEngine(manifest);
        var candidates = engine.Filter(manifest.Profiles, "form", "desktop");

        var question = engine.NextQuestion(candidates);

        Assert.NotNull(question);
        Assert.Equal("backend", question.Trait);
    }
}
