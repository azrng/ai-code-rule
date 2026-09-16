namespace RuleAssembler.Tests;

// 经典入口的组合口径转换：选中 Profile 经其兼容样例反查维度选择，
// mode=classic、回答来源整体 classic，转换结果可被组合内核解析
public sealed class ClassicEntryTests
{
    [Fact]
    public void ClassicOutcome_ConvertsProfileToCaseSelection()
    {
        var manifest = new ManifestLoader(new ResourceStore()).Load();
        var profile = manifest.Profiles.Single(item => item.Id == "wpfExe");

        var outcome = Program.ClassicOutcome(manifest, profile);

        Assert.NotNull(outcome);
        var caseSelection = manifest.CompatibilityCases
            .Single(item => item.Profile.Equals("wpfExe", StringComparison.OrdinalIgnoreCase)).Selection;
        Assert.Equal(caseSelection, outcome!.Selection.Dimensions);
        Assert.Equal("classic", outcome.Mode);
        Assert.NotEmpty(outcome.AnswerSources);
        Assert.All(outcome.AnswerSources.Values, source => Assert.Equal("classic", source));

        // 转换结果走组合内核解析，组件集与该 Profile 兼容样例等价回归一致
        var plan = new CompositionPlanner(manifest).Plan(outcome.Selection);
        Assert.Contains(plan.ComponentIds, id => id == "wpfExe");
    }
}
