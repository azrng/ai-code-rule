namespace RuleAssembler.Tests;

// 常驻规则预算守卫：兑现《规则退役清单》「测试对固定段设置上限，避免每次只加一行持续膨胀」。
// 只统计根 AGENTS.md 的固定行为规则段（根文件原则 / 最小充分上下文 / 全局工作规则 / 编码硬约束 /
// 触发式协作规则），不统计技术栈总览、子规则索引、阶段变量与能力导航等动态内容。
// 上调上限需给出充分理由（合并口径后净减少者优先下调），不允许无解释加行。
public sealed class RootBudgetTests
{
    private static readonly string[] FixedSections =
    [
        "## 根文件原则",
        "## 最小充分上下文",
        "## 全局工作规则",
        "## 编码硬约束",
        "## 触发式协作规则",
    ];

    private const int MaxFixedSectionLines = 48;

    [Fact]
    public void EveryCase_RootAgents_FixedSections_WithinBudget()
    {
        var manifest = new ManifestLoader(new ResourceStore()).Load();
        var renderer = new RuleRenderer(new ResourceStore());
        var planner = new CompositionPlanner(manifest);

        foreach (var caseItem in manifest.CompatibilityCases)
        {
            var plan = planner.Plan(new Selection { Dimensions = new Dictionary<string, string>(caseItem.Selection, StringComparer.Ordinal) });
            var files = renderer.Plan(manifest, plan, FeatureSelection.Defaults(manifest, plan));
            var root = Assert.Single(files, file => file.Output == "AGENTS.md");
            var lines = root.Content.Split('\n');

            var counted = 0;
            foreach (var section in FixedSections)
            {
                var start = Array.FindIndex(lines, line => line.TrimStart().StartsWith(section, StringComparison.Ordinal));
                if (start < 0)
                {
                    continue; // 动态裁剪的段不计（如最小充分上下文仅部分组合存在）
                }

                var end = lines.Length;
                for (var i = start + 1; i < lines.Length; i++)
                {
                    if (lines[i].StartsWith("## ", StringComparison.Ordinal))
                    {
                        end = i;
                        break;
                    }
                }

                for (var i = start; i < end; i++)
                {
                    var text = lines[i].Trim();
                    if (text.Length > 0 && text != "---")
                    {
                        counted++;
                    }
                }
            }

            Assert.True(
                counted <= MaxFixedSectionLines,
                $"{caseItem.Profile} 根 AGENTS.md 固定行为规则段共 {counted} 行，超出预算 {MaxFixedSectionLines} 行；请合并重复口径、下沉细则或退役行为补丁后再加行");
        }
    }
}
