using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace RuleAssembler.Tests;

// P2 组合内核等价回归：compatibilityCases 声明的每个旧 Profile，
// 组合路径（CompositionPlanner + RuleRenderer）与兼容路径（Profile 渲染）必须逐字节一致。
// 默认能力集与显式能力子集都要等价，能力关闭时根导航同样不得引用不存在的子规则。
public class CompositionEquivalenceTests
{
    private readonly Manifest _manifest;
    private readonly RuleRenderer _renderer;
    private readonly CompositionPlanner _planner;
    private readonly ITestOutputHelper _output;

    public CompositionEquivalenceTests(ITestOutputHelper output)
    {
        _output = output;
        var resources = new ResourceStore();
        _manifest = new ManifestLoader(resources).Load();
        _renderer = new RuleRenderer(resources);
        _planner = new CompositionPlanner(_manifest);
    }

    public static IEnumerable<object[]> CompatibilityCases()
    {
        var manifest = new ManifestLoader(new ResourceStore()).Load();
        foreach (var item in manifest.CompatibilityCases)
            yield return new object[] { item.Profile, item.Selection };
    }

    [Theory]
    [MemberData(nameof(CompatibilityCases))]
    public void 组合路径与旧Profile默认产物逐字节一致(string profileId, Dictionary<string, string> selection)
    {
        var profile = _manifest.Profiles.First(item => item.Id.Equals(profileId, StringComparison.OrdinalIgnoreCase));
        var plan = _planner.Plan(new Selection { Dimensions = selection });

        var legacyFiles = _renderer.Plan(_manifest, profile);
        var composeFiles = _renderer.Plan(_manifest, plan, FeatureSelection.Defaults(_manifest, plan));

        AssertPlannedFilesEqual(profileId, legacyFiles, composeFiles);
    }

    [Theory]
    [MemberData(nameof(CompatibilityCases))]
    public void 组合路径与旧Profile能力子集产物逐字节一致(string profileId, Dictionary<string, string> selection)
    {
        var profile = _manifest.Profiles.First(item => item.Id.Equals(profileId, StringComparison.OrdinalIgnoreCase));
        var plan = _planner.Plan(new Selection { Dimensions = selection });

        // 能力子集逐个关闭再组合关闭，覆盖根导航联动与 design-system 门控
        var defaultFeatures = FeatureSelection.Defaults(_manifest, plan);
        var subsets = new List<HashSet<string>> { new(StringComparer.OrdinalIgnoreCase) };
        foreach (var feature in defaultFeatures)
        {
            var without = new HashSet<string>(defaultFeatures, StringComparer.OrdinalIgnoreCase);
            without.Remove(feature);
            subsets.Add(without);
        }

        foreach (var features in subsets)
        {
            var legacyFiles = _renderer.Plan(_manifest, profile, features);
            var composeFiles = _renderer.Plan(_manifest, plan, features);
            AssertPlannedFilesEqual($"{profileId}（features=[{string.Join(",", features.OrderBy(item => item))}]）", legacyFiles, composeFiles);
        }
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

    private void AssertPlannedFilesEqual(
        string caseName, List<PlannedFile> legacyFiles, List<PlannedFile> composeFiles)
    {
        Assert.True(legacyFiles.Count == composeFiles.Count,
            $"{caseName}：文件数量不一致 旧={legacyFiles.Count} 组合={composeFiles.Count}");

        for (var i = 0; i < legacyFiles.Count; i++)
        {
            Assert.True(legacyFiles[i].Output.Equals(composeFiles[i].Output, StringComparison.OrdinalIgnoreCase),
                $"{caseName}：第 {i + 1} 个文件顺序不一致 旧={legacyFiles[i].Output} 组合={composeFiles[i].Output}");
            if (!legacyFiles[i].Content.Equals(composeFiles[i].Content, StringComparison.Ordinal))
                ReportContentDiff(caseName, legacyFiles[i], composeFiles[i]);
        }
    }

    // 内容不一致时输出首个差异行，让回归失败可以直接定位到根变量或片段来源
    private void ReportContentDiff(string caseName, PlannedFile legacy, PlannedFile compose)
    {
        var legacyLines = legacy.Content.Split('\n');
        var composeLines = compose.Content.Split('\n');
        _output.WriteLine($"{caseName}：{legacy.Output} 内容不一致（旧 {legacyLines.Length} 行 / 组合 {composeLines.Length} 行）");
        for (var i = 0; i < Math.Max(legacyLines.Length, composeLines.Length); i++)
        {
            var left = i < legacyLines.Length ? legacyLines[i] : "<缺行>";
            var right = i < composeLines.Length ? composeLines[i] : "<缺行>";
            if (!left.Equals(right, StringComparison.Ordinal))
            {
                _output.WriteLine($"  首个差异在第 {i + 1} 行：");
                _output.WriteLine($"  旧：{left}");
                _output.WriteLine($"  组合：{right}");
                break;
            }
        }

        Assert.Fail($"{caseName}：{legacy.Output} 组合路径与旧 Profile 产物不一致");
    }

    private static void AssertRootEqual(RootVariables first, RootVariables second)
    {
        // RootLine 未重写 Equals（两份计划持有各自的深拷贝实例），按值投影比较
        Assert.Equal(DescribeLines(first.TargetList), DescribeLines(second.TargetList));
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
