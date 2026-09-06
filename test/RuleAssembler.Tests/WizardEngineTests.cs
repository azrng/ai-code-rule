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
    public void NextQuestion_WebCandidates_AskFrontendThenBackendWithNoneOption()
    {
        // Web 候选先问前端，回答 React 后后端问题必须出现「无后端」选项（纯前端组合可达）
        var manifest = LoadManifest();
        var engine = new WizardEngine(manifest);
        var candidates = engine.Filter(manifest.Profiles, "form", "web");

        var frontendQuestion = engine.NextQuestion(candidates);

        Assert.NotNull(frontendQuestion);
        Assert.Equal("frontend", frontendQuestion.Trait);

        var reactCandidates = engine.Filter(candidates, "frontend", "react");
        var backendQuestion = engine.NextQuestion(reactCandidates);

        Assert.NotNull(backendQuestion);
        Assert.Equal("backend", backendQuestion.Trait);
        Assert.Contains(backendQuestion.Options, option => option.Value == "none");
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
    public void NextQuestion_DesktopCandidates_AskFrontendBeforeShell()
    {
        // 问答顺序对齐 Q1 形态 → Q2 前端/界面 → Q3 壳 → Q4 后端；
        // 桌面先问界面：原生桌面（Avalonia/WPF/控制台）与混合壳的分歧在 frontend 维度，
        // 若先问后端，「.NET」回答会进入 Electron 分支导致 Avalonia/WPF 选项永不出现
        var manifest = LoadManifest();
        var engine = new WizardEngine(manifest);
        var candidates = engine.Filter(manifest.Profiles, "form", "desktop");

        var question = engine.NextQuestion(candidates);

        Assert.NotNull(question);
        Assert.Equal("frontend", question.Trait);
        Assert.Contains(question.Options, option => option.Value == "avalonia");
        Assert.Contains(question.Options, option => option.Value == "wpf");
        Assert.Contains(question.Options, option => option.Value == "console");
    }

    [Theory]
    [InlineData("avalonia", "avaloniaExe")]
    [InlineData("wpf", "wpfExe")]
    public void NextQuestion_DesktopNativeFrontend_ConvergesWithoutBackend(string frontend, string expectedProfileId)
    {
        // 桌面 + Avalonia/WPF 回答后候选唯一收敛，后端问题不再出现（backend=none 由唯一候选补全）
        var manifest = LoadManifest();
        var engine = new WizardEngine(manifest);
        var candidates = engine.Filter(engine.Filter(manifest.Profiles, "form", "desktop"), "frontend", frontend);

        var question = engine.NextQuestion(candidates);
        var profile = Assert.Single(candidates);

        Assert.Null(question);
        Assert.Equal(expectedProfileId, profile.Id);
    }

    [Fact]
    public void Resolve_DesktopWpfWithoutBackend_ResolvesWpfExe()
    {
        // 用户视角的回答组合：只回答形态 + 界面即可落到 WPF，无需声明「无后端」
        var engine = new WizardEngine(LoadManifest());
        var answers = new Dictionary<string, string>
        {
            ["form"] = "desktop",
            ["frontend"] = "wpf"
        };

        var result = engine.Resolve(answers);

        var profile = Assert.Single(result);
        Assert.Equal("wpfExe", profile.Id);
    }

    [Fact]
    public void NextQuestion_DesktopReactFrontend_AskShellThenBackend()
    {
        // 混合壳桌面（React 界面）先问壳：Electron / Tauri / Wails 必须作为显式选项出现，
        // 不能只靠「无后端」之类的后端回答隐式落进去（否则用户在问答里永远看不到 Electron）
        var manifest = LoadManifest();
        var engine = new WizardEngine(manifest);
        var candidates = engine.Filter(engine.Filter(manifest.Profiles, "form", "desktop"), "frontend", "react");

        var shellQuestion = engine.NextQuestion(candidates);

        Assert.NotNull(shellQuestion);
        Assert.Equal("shell", shellQuestion.Trait);
        Assert.Contains(shellQuestion.Options, option => option.Value == "electron");
        Assert.Contains(shellQuestion.Options, option => option.Value == "tauri");
        Assert.Contains(shellQuestion.Options, option => option.Value == "wails");

        var electronCandidates = engine.Filter(candidates, "shell", "electron");
        var backendQuestion = engine.NextQuestion(electronCandidates);

        // 选 Electron 后后端问题只剩 主进程(none) 与 .NET sidecar(dotnet) 之分
        Assert.NotNull(backendQuestion);
        Assert.Equal("backend", backendQuestion.Trait);
        Assert.Contains(backendQuestion.Options, option => option.Value == "none");
        Assert.Contains(backendQuestion.Options, option => option.Value == "dotnet");
        Assert.DoesNotContain(backendQuestion.Options, option => option.Value == "go" || option.Value == "rust");
    }
}
