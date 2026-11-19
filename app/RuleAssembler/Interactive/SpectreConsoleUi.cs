using Spectre.Console;
using Spectre.Console.Rendering;

namespace RuleAssembler;

// Spectre.Console 实现：交互式终端用方向键 + 回车选择。
// 只在 stdin 未重定向时被启用；文本渲染统一 Markup.Escape，防止用户内容被当作控制标记解析
internal sealed class SpectreConsoleUi : IConsoleUi
{
    private readonly IAnsiConsole _console;

    // 生产路径绑定全局控制台；测试注入 TestConsole 做按键回归
    public SpectreConsoleUi() : this(AnsiConsole.Console)
    {
    }

    internal SpectreConsoleUi(IAnsiConsole console)
    {
        _console = console;
    }

    public int? Choose(string title, string zeroHint, IReadOnlyList<UiChoice> choices)
    {
        var display = new List<string>();
        for (var i = 0; i < choices.Count; i++)
        {
            var suffix = string.IsNullOrWhiteSpace(choices[i].Desc) ? "" : $" — {Markup.Escape(choices[i].Desc)}";
            display.Add($"{i + 1}. {Markup.Escape(choices[i].Label)}{suffix}");
        }

        display.Add($"0. {Markup.Escape(zeroHint)}");

        var prompt = new SelectionPrompt<string>()
            .Title(Markup.Escape(title))
            .PageSize(PageBudget(display.Count))
            .AddChoices(display);

        var selected = _console.Prompt(prompt);
        if (selected.StartsWith("0.", StringComparison.Ordinal))
            return null;
        return display.IndexOf(selected);
    }

    public IReadOnlyList<int> ChooseMany(string title, IReadOnlyList<UiChoice> choices, IReadOnlyList<int> selected)
    {
        var display = new List<string>();
        for (var i = 0; i < choices.Count; i++)
        {
            var suffix = string.IsNullOrWhiteSpace(choices[i].Desc) ? "" : $" — {Markup.Escape(choices[i].Desc)}";
            display.Add($"{i + 1}. {Markup.Escape(choices[i].Label)}{suffix}");
        }

        // NotRequired：与 PlainConsoleUi「空回车 = 确认当前（可为空）勾选」对齐；缺省 Required=true 会让
        // 反选全部后无法回车确认（能力开关 / 目标工具屏没有 0 项出口，交互用户只能 Ctrl+C）
        var prompt = new MultiSelectionPrompt<string>()
            .Title(Markup.Escape(title))
            .InstructionsText("[grey](空格切换勾选，回车确认)[/]")
            .PageSize(PageBudget(display.Count))
            .NotRequired()
            .AddChoices(display);
        foreach (var index in selected)
            prompt.Select(display[index]);

        var chosen = _console.Prompt(prompt);
        return chosen.Select(item => display.IndexOf(item)).ToList();
    }

    // 分组多选：MultiSelectionPrompt 树形组（AddChoiceGroup），组不可折叠，勾选组标题即全选组内技能；
    // 返回勾选项映射回全量下标
    public IReadOnlyList<int> ChooseManyGrouped(
        string title,
        IReadOnlyList<UiChoice> choices,
        IReadOnlyList<SkillGroupChoice> groups,
        IReadOnlyList<int> selected)
    {
        // 选项值带全局编号前缀（与 ChooseMany 的 N. 风格一致）：desc 为空的技能值退化为纯 ID，
        // 编号保证组标题与技能值永不相撞，Spectre 按值勾选不会串选
        var prompt = new MultiSelectionPrompt<string>()
            .Title(Markup.Escape(title))
            .InstructionsText("[grey](勾选分组即全选组内技能，空格切换勾选，回车确认)[/]")
            .PageSize(PageBudget(choices.Count + groups.Count))
            .NotRequired();

        var display = new List<string>(choices.Count);
        for (var i = 0; i < choices.Count; i++)
        {
            var suffix = string.IsNullOrWhiteSpace(choices[i].Desc) ? "" : $" — {Markup.Escape(choices[i].Desc)}";
            display.Add($"{i + 1}. {Markup.Escape(choices[i].Label)}{suffix}");
        }

        foreach (var group in groups)
        {
            prompt.AddChoiceGroup(
                Markup.Escape(group.Title),
                group.SkillIndices.Select(index => display[index]));
        }

        foreach (var index in selected)
            prompt.Select(display[index]);

        var chosen = _console.Prompt(prompt);
        var chosenSet = chosen.ToHashSet(StringComparer.Ordinal);
        return display
            .Select((label, index) => (label, index))
            .Where(item => chosenSet.Contains(item.label))
            .Select(item => item.index)
            .ToList();
    }

    // Spectre.Console 要求 PageSize >= 3：选项行数不足 3（如汇总确认只有 1 项 + 取消）时取下限，超长列表封顶 12
    internal static int PageBudget(int rowCount) => Math.Clamp(rowCount, 3, 12);

    // 是非确认走 Spectre 的 y/n 按键提示（回车取默认值）
    public bool Confirm(string prompt, bool defaultValue)
        => _console.Confirm(Markup.Escape(prompt), defaultValue);

    // Status 动画（转圈）包裹阻塞操作；网络失败等异常直接向外传播，动画由 using 正常收尾
    public T WithProgress<T>(string label, Func<T> action)
        => AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("grey"))
            .Start(label, _ => action());

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
