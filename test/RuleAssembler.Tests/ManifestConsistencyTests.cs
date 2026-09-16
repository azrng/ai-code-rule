namespace RuleAssembler.Tests;

// traits 与兼容样例一致性守卫：检测候选切换为样例口径后，
// 两侧取值漂移会让经典入口与检测 / 组件问答推荐不同组合（先例：python Web 组合的 arch-style），
// 必须在装载期拦截
public sealed class ManifestConsistencyTests
{
    [Fact]
    public void RealManifest_TraitsMatchCompatibilityCaseSelections()
    {
        var manifest = new ManifestLoader(new ResourceStore()).Load();
        var errors = new List<string>();
        ManifestLoader.ValidateCompatibilityCases(manifest, errors);
        Assert.Empty(errors);
    }

    [Fact]
    public void TraitDrift_IsRejected()
    {
        var manifest = new ManifestLoader(new ResourceStore()).Load();
        var profile = manifest.Profiles.Single(item => item.Id == "wpfExe");
        profile.Traits!.ArchStyle = "layered";

        var errors = new List<string>();
        ManifestLoader.ValidateCompatibilityCases(manifest, errors);
        Assert.Contains(errors, error => error.Contains("与兼容样例 selection", StringComparison.Ordinal));
    }

    [Fact]
    public void MissingCase_IsRejected()
    {
        var manifest = new ManifestLoader(new ResourceStore()).Load();
        manifest.CompatibilityCases.RemoveAll(item => item.Profile.Equals("wpfExe", StringComparison.OrdinalIgnoreCase));

        var errors = new List<string>();
        ManifestLoader.ValidateCompatibilityCases(manifest, errors);
        Assert.Contains(errors, error => error.Contains("缺少兼容样例", StringComparison.Ordinal));
    }
}
