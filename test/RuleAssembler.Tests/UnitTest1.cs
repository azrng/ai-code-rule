namespace RuleAssembler.Tests;

public sealed class ManifestLoaderTests
{
    [Fact]
    public void Load_CurrentManifest_SucceedsAndIncludesGrokPointer()
    {
        var manifest = new ManifestLoader(new ResourceStore()).Load();

        Assert.Equal(20, manifest.Profiles.Count);
        Assert.Contains(
            manifest.Modules.SelectMany(module => module.Pointers),
            pointer => pointer.Equals("GROK.md", StringComparison.OrdinalIgnoreCase));

        var blazorServer = Assert.Single(manifest.Profiles, profile => profile.Id == "dotnetBlazor");
        Assert.Contains("Blazor Server", blazorServer.Label);
        Assert.Contains(blazorServer.Subrules, rule => rule.Source == "arch/dotnetBlazor/ui-AGENTS.md");

        var blazorWebApp = Assert.Single(manifest.Profiles, profile => profile.Id == "dotnetBlazorWebApp");
        Assert.Contains(blazorWebApp.Subrules, rule => rule.Source == "arch/dotnetBlazorWebApp/ui-AGENTS.md");
        Assert.Contains(blazorWebApp.Subrules, rule => rule.Source == "arch/dotnetBlazor/backend-AGENTS.md");
        Assert.Equal("arch/dotnetBlazor/design-system.yaml", blazorWebApp.DesignSystemSource);
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
}
