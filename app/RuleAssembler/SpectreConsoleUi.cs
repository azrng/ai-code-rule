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
            .PageSize(Math.Min(display.Count, 12))
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
            .PageSize(Math.Min(display.Count, 12))
            .AddChoices(display);
        foreach (var index in selected)
            prompt.Select(display[index]);

        var chosen = AnsiConsole.Prompt(prompt);
        return chosen.Select(item => display.IndexOf(item)).ToList();
    }

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
