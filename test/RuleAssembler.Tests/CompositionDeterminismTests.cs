namespace RuleAssembler.Tests;

// P2 组合内核确定性回归：同一选择在同一版本 manifest 上必须解析出相同的组件顺序、
// 文件列表、根变量与渲染产物（profiles 节点与旧渲染路径退役后，组合内核是唯一渲染路径）
public class CompositionDeterminismTests
{
    private readonly Manifest _manifest;
    private readonly RuleRenderer _renderer;
    private readonly CompositionPlanner _planner;

    public CompositionDeterminismTests()
    {
        var resources = new ResourceStore();
        _manifest = new ManifestLoader(resources).Load();
        _renderer = new RuleRenderer(resources);
        _planner = new CompositionPlanner(_manifest);
    }

    [Fact]
    public void 计划是确定性的_同一选择两次解析结果一致()
    {
        var selection = _manifest.CompatibilityCases[0].Selection;
        var first = _planner.Plan(new Selection { Dimensions = selection });
        var second = _planner.Plan(new Selection { Dimensions = selection });

        Assert.Equal(first.ComponentIds, second.ComponentIds);
        Assert.Equal(
            first.Files.Select(file => (file.Output, file.GetSources())),
            second.Files.Select(file => (file.Output, file.GetSources())));
        Assert.Equal(first.DesignSystemSource, second.DesignSystemSource);
        AssertRootEqual(first.Root, second.Root);
    }

    [Fact]
    public void 渲染是确定性的_全部样例两次渲染逐字节一致()
    {
        foreach (var caseItem in _manifest.CompatibilityCases)
        {
            var selection = new Selection
            {
                Dimensions = new Dictionary<string, string>(caseItem.Selection, StringComparer.Ordinal)
            };
            var plan = _planner.Plan(selection);
            var features = FeatureSelection.Defaults(_manifest, plan);
            var first = _renderer.Plan(_manifest, plan, features);
            var second = _renderer.Plan(_manifest, plan, features);
            Assert.Equal(
                first.Select(file => file.Output),
                second.Select(file => file.Output));
            Assert.Equal(
                first.Select(file => file.Content),
                second.Select(file => file.Content));
        }
    }

    private static void AssertRootEqual(RootVariables first, RootVariables second)
    {
        // RootLine 未重写 Equals（两份计划持有各自的深拷贝实例），按值投影比较
        Assert.Equal(DescribeLines(first.SubruleIndex), DescribeLines(second.SubruleIndex));
        Assert.Equal(DescribeLines(first.DevStage), DescribeLines(second.DevStage));
        Assert.Equal(DescribeLines(first.UnderstandingChecklist), DescribeLines(second.UnderstandingChecklist));
        Assert.Equal(DescribeLines(first.ReadingOrder), DescribeLines(second.ReadingOrder));
        Assert.Equal(DescribeLines(first.ExtraSections), DescribeLines(second.ExtraSections));
        Assert.Equal(first.SearchNouns, second.SearchNouns);
        Assert.Equal(first.ContractErrorLine, second.ContractErrorLine);
        Assert.Equal(first.ContextScopeNouns, second.ContextScopeNouns);
        Assert.Equal(first.HighRiskNouns, second.HighRiskNouns);
        Assert.Equal(first.PersistenceLine, second.PersistenceLine);
    }

    private static List<string> DescribeLines(IEnumerable<RootLine> lines)
    {
        return lines.Select(line =>
        {
            var variants = line.When is null
                ? ""
                : "|when=" + string.Join(";", line.When.Select(variant =>
                    $"{variant.Text}[{string.Join(",", variant.Features)}]"));
            return $"{line.Text}[{string.Join(",", line.Features)}]{variants}";
        }).ToList();
    }
}
