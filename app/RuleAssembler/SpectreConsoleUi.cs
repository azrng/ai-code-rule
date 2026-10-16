using Spectre.Console;
using Spectre.Console.Rendering;

namespace RuleAssembler;

// Spectre.Console 实现：交互式终端用方向键 + 回车选择。
// 只在 stdin 未重定向时被启用；文本渲染统一 Markup.Escape，防止用户内容被当作控制标记解析
internal sealed class SpectreConsoleUi : IConsoleUi
{
    public int? Choose(string title, string zeroHint, IReadOnlyList<UiChoice> choices)
    {
        var display = new List<string>();
        for (var i = 0; i < choices.Count; i++)
        {
            var suffix = string.IsNullOrWhiteSpace(choices[i].Desc) ? "" : $" — {choices[i].Desc}";
            display.Add($"{i + 1}. {choices[i].Label}{suffix}");
        }

        display.Add($"0. {zeroHint}");

        var prompt = new SelectionPrompt<string>()
            .Title(Markup.Escape(title))
            .PageSize(PageBudget(display.Count))
            .AddChoices(display);

        var selected = AnsiConsole.Prompt(prompt);
        if (selected.StartsWith("0.", StringComparison.Ordinal))
            return null;
        return display.IndexOf(selected);
    }

    public IReadOnlyList<int> ChooseMany(string title, IReadOnlyList<UiChoice> choices, IReadOnlyList<int> selected)
    {
        var display = new List<string>();
        for (var i = 0; i < choices.Count; i++)
        {
            var suffix = string.IsNullOrWhiteSpace(choices[i].Desc) ? "" : $" — {choices[i].Desc}";
            display.Add($"{i + 1}. {choices[i].Label}{suffix}");
        }

        var prompt = new MultiSelectionPrompt<string>()
            .Title(Markup.Escape(title))
            .InstructionsText("[grey](空格切换勾选，回车确认)[/]")
            .PageSize(PageBudget(display.Count))
            .AddChoices(display);
        foreach (var index in selected)
            prompt.Select(display[index]);

        var chosen = AnsiConsole.Prompt(prompt);
        return chosen.Select(item => display.IndexOf(item)).ToList();
    }

    // 分组多选：MultiSelectionPrompt 树形组（AddChoiceGroup），组展开勾选父级即可全选组内技能；
    // 返回勾选项映射回全量下标
    public IReadOnlyList<int> ChooseManyGrouped(
        string title,
        IReadOnlyList<UiChoice> choices,
        IReadOnlyList<SkillGroupChoice> groups,
        IReadOnlyList<int> selected)
    {
        var prompt = new MultiSelectionPrompt<string>()
            .Title(Markup.Escape(title))
            .InstructionsText("[grey](方向键展开分组，空格切换勾选，回车确认)[/]")
            .PageSize(PageBudget(choices.Count + groups.Count))
            .NotRequired();

        foreach (var group in groups)
        {
            var items = group.SkillIndices
                .Select(index => (index, Label: GroupedLabel(choices[index])))
                .ToList();
            prompt.AddChoiceGroup(
                group.Title,
                items.Select(item => item.Label));
        }

        foreach (var index in selected)
            prompt.Select(GroupedLabel(choices[index]));

        var chosen = AnsiConsole.Prompt(prompt);
        var chosenSet = chosen.ToHashSet(StringComparer.Ordinal);
        return Enumerable.Range(0, choices.Count)
            .Where(index => chosenSet.Contains(GroupedLabel(choices[index])))
            .ToList();
    }

    private static string GroupedLabel(UiChoice choice)
    {
        var suffix = string.IsNullOrWhiteSpace(choice.Desc) ? "" : $" — {choice.Desc}";
        return $"{choice.Label}{suffix}";
    }

    // Spectre.Console 要求 PageSize >= 3：选项行数不足 3（如汇总确认只有 1 项 + 取消）时取下限，超长列表封顶 12
    internal static int PageBudget(int rowCount) => Math.Clamp(rowCount, 3, 12);

    public void Head(string text)
    {
        AnsiConsole.MarkupLine($"[bold cyan]{Markup.Escape(text)}[/]");
    }

    public void Line(string text)
    {
        AnsiConsole.WriteLine(text);
    }

    public void Bullets(IEnumerable<string> items)
    {
        foreach (var item in items)
            AnsiConsole.MarkupLine($"  [dim]-[/] {Markup.Escape(item)}");
    }

    public void FileTable(string title, IEnumerable<string> files)
    {
        var table = new Table().RoundedBorder();
        table.AddColumn(new TableColumn(Markup.Escape(title)).LeftAligned());
        foreach (var file in files)
            table.AddRow(Markup.Escape(file));
        AnsiConsole.Write(table);
    }
}
