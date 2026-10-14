namespace RuleAssembler.Tests;

// 经典入口的组合口径转换：选中样例的维度选择直接进入组合内核，
// mode=classic、回答来源整体 classic，转换结果可被组合内核解析
public sealed class ClassicEntryTests
{
    [Fact]
    public void ClassicOutcome_ConvertsCaseSelection()
    {
        var manifest = new ManifestLoader(new ResourceStore()).Load();
        var caseItem = manifest.CompatibilityCases
            .Single(item => item.Profile.Equals("wpfExe", StringComparison.OrdinalIgnoreCase));

        var outcome = InteractiveFlow.ClassicOutcome(caseItem);

        Assert.NotNull(outcome);
        Assert.Equal(caseItem.Selection, outcome!.Selection.Dimensions);
        Assert.Equal("classic", outcome.Mode);
        Assert.NotEmpty(outcome.AnswerSources);
        Assert.All(outcome.AnswerSources.Values, source => Assert.Equal("classic", source));

        // 转换结果走组合内核解析，组件集与该样例等价回归一致
        var plan = new CompositionPlanner(manifest).Plan(outcome.Selection);
        Assert.Contains(plan.ComponentIds, id => id == "wpfExe");
    }
}
