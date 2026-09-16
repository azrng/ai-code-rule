namespace RuleAssembler.Tests;

// 兼容样例完整性守卫：样例是组合空间的唯一注册表（profiles 节点已退役），
// 展示元数据与 ID 唯一性在装载期拦截
public sealed class ManifestConsistencyTests
{
    [Fact]
    public void RealManifest_AllCasesAreWellFormed()
    {
        var manifest = new ManifestLoader(new ResourceStore()).Load();
        var errors = new List<string>();
        CompositionValidator.ValidateCompatibilityCases(manifest, errors);
        Assert.Empty(errors);
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
        Assert.Contains(errors, error => error.Contains("样例 ID 重复", StringComparison.Ordinal));
    }
}
