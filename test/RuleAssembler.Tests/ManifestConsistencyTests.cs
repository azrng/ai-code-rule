namespace RuleAssembler.Tests;

// 兼容样例完整性守卫：检测候选、经典入口与配方重放都按样例收敛，
// 每个 Profile 必须恰好注册一个样例，缺失或多注册都在装载期拦截
public sealed class ManifestConsistencyTests
{
    [Fact]
    public void RealManifest_EveryProfileHasExactlyOneCase()
    {
        var manifest = new ManifestLoader(new ResourceStore()).Load();
        var errors = new List<string>();
        CompositionValidator.ValidateCompatibilityCases(manifest, errors);
        Assert.Empty(errors);
    }

    [Fact]
    public void MissingCase_IsRejected()
    {
        var manifest = new ManifestLoader(new ResourceStore()).Load();
        manifest.CompatibilityCases.RemoveAll(item => item.Profile.Equals("wpfExe", StringComparison.OrdinalIgnoreCase));

        var errors = new List<string>();
        CompositionValidator.ValidateCompatibilityCases(manifest, errors);
        Assert.Contains(errors, error => error.Contains("缺少兼容样例", StringComparison.Ordinal));
    }

    [Fact]
    public void DuplicateCase_IsRejected()
    {
        var manifest = new ManifestLoader(new ResourceStore()).Load();
        var original = manifest.CompatibilityCases.First(item => item.Profile == "wpfExe");
        manifest.CompatibilityCases.Add(new CompatibilityCase
        {
            Profile = "wpfExe",
            Selection = new Dictionary<string, string>(original.Selection, StringComparer.Ordinal)
        });

        var errors = new List<string>();
        CompositionValidator.ValidateCompatibilityCases(manifest, errors);
        Assert.Contains(errors, error => error.Contains("多个兼容样例", StringComparison.Ordinal));
    }
}
