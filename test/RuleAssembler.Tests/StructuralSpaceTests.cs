namespace RuleAssembler.Tests;

// 结构完备性空间守卫：全维度组合中，「约束满足 + Plan 可解析 + 组件非空 +
// 非标记（absent）维度被选中组件锚定」的幸存空间必须与兼容样例互为充要——
// 每个样例都能到达（可达性下界），每个幸存者都映射样例（无样例外组合，新增组件时
// 该断言即失败，强制为新组合显式补样例并验证渲染，防止未验证组合悄悄可达）
public sealed class StructuralSpaceTests
{
    [Fact]
    public void 结构完备空间_与兼容样例互为充要()
    {
        var manifest = new ManifestLoader(new ResourceStore()).Load();
        var planner = new CompositionPlanner(manifest);
        var dims = ProfileTraits.KnownTraits;
        var values = dims
            .Select(trait => manifest.Wizard.Options[trait].Keys.Order(StringComparer.Ordinal).ToList())
            .ToList();

        var survivors = new List<string>();
        void Rec(Dictionary<string, string> current, int index)
        {
            if (index == dims.Length)
            {
                var selection = new Selection { Dimensions = new Dictionary<string, string>(current, StringComparer.Ordinal) };
                if (!CompositionPlanner.SatisfiesConstraints(manifest, selection))
                    return;
                try
                {
                    var plan = planner.Plan(selection);
                    var selected = plan.ComponentIds
                        .Select(id => manifest.Components.First(c => c.Id == id))
                        .ToList();
                    if (selected.Count == 0)
                        return;
                    var pinned = current.All(pair =>
                        CompositionPlanner.IsAbsentValue(manifest, pair.Key, pair.Value)
                        || selected.Any(component =>
                            component.When.TryGetValue(pair.Key, out var value) && value == pair.Value));
                    if (pinned)
                        survivors.Add(string.Join(",", current.Select(pair => $"{pair.Key}={pair.Value}")));
                }
                catch (InvalidOperationException)
                {
                    // 根变量合并 / 槽位 / 输出冲突等渲染期错误同样视为不可生成
                }

                return;
            }

            foreach (var value in values[index])
            {
                current[dims[index]] = value;
                Rec(current, index + 1);
            }

            current.Remove(dims[index]);
        }

        Rec(new Dictionary<string, string>(StringComparer.Ordinal), 0);

        // 下界：每个样例用 absent 填充到全维度后必须是幸存者
        var absent = dims
            .Where(trait => manifest.Wizard.Steps.First(step => step.Trait == trait).Absent.Length > 0)
            .ToDictionary(
                trait => trait,
                trait => manifest.Wizard.Steps.First(step => step.Trait == trait).Absent,
                StringComparer.Ordinal);
        var unreachable = new List<string>();
        foreach (var item in manifest.CompatibilityCases)
        {
            var padded = dims.ToDictionary(
                trait => trait,
                trait => item.Selection.TryGetValue(trait, out var value) ? value : absent[trait],
                StringComparer.Ordinal);
            var key = string.Join(",", padded.Select(pair => $"{pair.Key}={pair.Value}"));
            if (!survivors.Contains(key))
                unreachable.Add($"{item.Profile}（{key}）");
        }

        // 上界：每个幸存者必须映射至少一个样例（样例维度与幸存者共有维度全等，多出的维度取 absent）
        var unmapped = survivors
            .Where(key => !manifest.CompatibilityCases.Any(item =>
            {
                var parts = key.Split(',').Select(kv => kv.Split('=')).ToDictionary(kv => kv[0], kv => kv[1]);
                return item.Selection.All(pair => parts.TryGetValue(pair.Key, out var value) && value == pair.Value)
                    && parts.Where(pair => !item.Selection.ContainsKey(pair.Key))
                        .All(pair => absent.TryGetValue(pair.Key, out var absentValue) && absentValue == pair.Value);
            }))
            .ToList();

        Assert.True(unreachable.Count == 0 && unmapped.Count == 0,
            $"结构空间与样例空间不一致。不可达样例：{string.Join("；", unreachable)}。" +
            $"样例外幸存组合：{string.Join("；", unmapped)}——新组件接入产生了样例外可达组合，" +
            "须先补兼容样例与渲染回归，或收紧约束。");
    }

    [Fact]
    public void Plan_维度组合无组件命中_显式报错()
    {
        var manifest = new ManifestLoader(new ResourceStore()).Load();
        var planner = new CompositionPlanner(manifest);

        // 桌面 + Python：没有任何组件表达该组合
        var ex = Assert.Throws<InvalidOperationException>(() => planner.Plan(new Selection
        {
            Dimensions = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["form"] = "desktop",
                ["frontend"] = "none",
                ["shell"] = "none",
                ["backend"] = "python",
                ["arch-style"] = "single",
                ["blazor-flavor"] = "none",
                ["ai-capability"] = "no"
            }
        }));
        Assert.Contains("没有命中任何组件", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Plan_桌面壳无渲染层_违反钳制约束()
    {
        var manifest = new ManifestLoader(new ResourceStore()).Load();
        var planner = new CompositionPlanner(manifest);

        var ex = Assert.Throws<InvalidOperationException>(() => planner.Plan(new Selection
        {
            Dimensions = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["form"] = "desktop",
                ["frontend"] = "none",
                ["shell"] = "electron",
                ["backend"] = "none",
                ["arch-style"] = "single",
                ["blazor-flavor"] = "none",
                ["ai-capability"] = "no"
            }
        }));
        Assert.Contains("违反约束", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Plan_未锚定的正向维度取值_显式报错()
    {
        var manifest = new ManifestLoader(new ResourceStore()).Load();
        var planner = new CompositionPlanner(manifest);

        // Electron + AI：ai-capability=yes 没有任何选中组件表达（AI capability 只接入 Web 家族）
        var ex = Assert.Throws<InvalidOperationException>(() => planner.Plan(new Selection
        {
            Dimensions = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["form"] = "desktop",
                ["frontend"] = "react",
                ["shell"] = "electron",
                ["backend"] = "dotnet",
                ["arch-style"] = "single",
                ["blazor-flavor"] = "none",
                ["ai-capability"] = "yes"
            }
        }));
        Assert.Contains("未被任何选中组件表达", ex.Message, StringComparison.Ordinal);
    }
}
