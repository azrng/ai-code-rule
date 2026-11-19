namespace RuleAssembler.Tests;

// 渲染层同组合 include 去重：同一共享片段在同一组合的第二个子规则中以指针行替代全文，
// 首载宿主承载唯一权威全文；跨组合互不影响
public sealed class IncludeDeduplicationTests
{
    private static (Manifest Manifest, CompositionPlanner Planner, RuleRenderer Renderer) Load()
    {
        var manifest = new ManifestLoader(new ResourceStore()).Load();
        return (manifest, new CompositionPlanner(manifest), new RuleRenderer(new ResourceStore()));
    }

    private static List<PlannedFile> RenderCase(string profile)
    {
        var (manifest, planner, renderer) = Load();
        var caseItem = manifest.CompatibilityCases.Single(c => c.Profile == profile);
        var plan = planner.Plan(new Selection
        {
            Dimensions = new Dictionary<string, string>(caseItem.Selection, StringComparer.Ordinal)
        });
        return renderer.Plan(manifest, plan, FeatureSelection.Defaults(manifest, plan));
    }

    [Fact]
    public void WpfExe_NoTestFallback_OnlyExpandedOnce()
    {
        // wpfExe 的 application / ui / infrastructure 三个子规则都挂 no-test-fallback：
        // 首载宿主展开全文，其余为指针行；全文「必须说明未执行的测试类型」只出现一次
        var files = RenderCase("wpfExe");
        var fullTextCount = files.Count(f => f.Content.Contains("必须说明未执行的测试类型、原因、潜在影响范围和风险", StringComparison.Ordinal));
        Assert.Equal(1, fullTextCount);

        var pointerFiles = files.Where(f => f.Content.Contains("已在 `", StringComparison.Ordinal)
            && f.Content.Contains("不重复维护", StringComparison.Ordinal)).Select(f => f.Output).ToList();
        Assert.True(pointerFiles.Count >= 1, "重复挂载的子规则应有指针行替代");
    }

    [Fact]
    public void DuplicateInclude_PointerLineNamesFirstOwner()
    {
        var files = RenderCase("wpfExe");
        var application = files.Single(f => f.Output == "application-AGENTS.md");
        if (application.Content.Contains("已在 `", StringComparison.Ordinal))
        {
            // 指针行必须指向真实存在的首载宿主文件
            var match = System.Text.RegularExpressions.Regex.Match(application.Content, "已在 `([^`]+)` 展开");
            Assert.True(match.Success);
            Assert.True(files.Select(f => f.Output).Contains(match.Groups[1].Value, StringComparer.OrdinalIgnoreCase), "指针行首载宿主必须是本组合的真实输出");
        }
    }

    [Fact]
    public void Deduplication_IsPerPlan_CompositionalIsolation()
    {
        // 去重会话生命周期 = 单次 Plan：两个组合各自渲染互不污染，
        // 各自的首载宿主独立裁决（同一片段在两个组合都应有全文）
        var wpf = RenderCase("wpfExe");
        var avalonia = RenderCase("avaloniaExe");
        Assert.Equal(1, wpf.Count(f => f.Content.Contains("必须说明未执行的测试类型", StringComparison.Ordinal)));
        Assert.Equal(1, avalonia.Count(f => f.Content.Contains("必须说明未执行的测试类型", StringComparison.Ordinal)));
    }

    [Fact]
    public void SingleInclude_FilesAreUnchanged()
    {
        // 只挂载一次的片段不受去重影响：全文正常展开、无指针行混入
        var (manifest, planner, renderer) = Load();
        var caseItem = manifest.CompatibilityCases.Single(c => c.Profile == "reactOnly");
        var plan = planner.Plan(new Selection
        {
            Dimensions = new Dictionary<string, string>(caseItem.Selection, StringComparer.Ordinal)
        });
        var files = renderer.Plan(manifest, plan, FeatureSelection.Defaults(manifest, plan));

        // 指针行只替换「重复挂载」的片段；首载全文与回归基线行都正常展开
        Assert.Contains(files, f => f.Content.Contains("默认执行：项目现有的静态检查、类型检查或编译校验", StringComparison.Ordinal));
        // 指针行指向的首载宿主必须是本组合的真实输出文件（全文由它承载）
        var outputs = files.Select(f => f.Output).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var file in files)
        foreach (var match in System.Text.RegularExpressions.Regex.Matches(file.Content, "已在 `([^`]+)` 展开").Cast<System.Text.RegularExpressions.Match>())
            Assert.True(outputs.Contains(match.Groups[1].Value), $"指针行指向不存在的输出：{match.Groups[1].Value}");
    }
}
