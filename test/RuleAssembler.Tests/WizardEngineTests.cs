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
}
