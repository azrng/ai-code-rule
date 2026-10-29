namespace RuleAssembler;

// 纯文本编号输入实现：用于 stdin 重定向（echo 管道冒烟、CI）等非交互场景，
// 行为与 Spectre 引入前的手写交互完全一致，保证管道输入兼容性
internal sealed class PlainConsoleUi : IConsoleUi
{
    public int? Choose(string title, string zeroHint, IReadOnlyList<UiChoice> choices)
    {
        Console.WriteLine(title);
        for (var i = 0; i < choices.Count; i++)
        {
            var suffix = string.IsNullOrWhiteSpace(choices[i].Desc) ? "" : $" — {choices[i].Desc}";
            Console.WriteLine($"  {i + 1}. {choices[i].Label}{suffix}");
        }

        Console.Write($"输入编号 (1-{choices.Count})，0 {zeroHint}：");
        while (true)
        {
            var input = Console.ReadLine()?.Trim();
            if (input is null || input == "0")
                return null;
            if (int.TryParse(input, out var index) && index >= 1 && index <= choices.Count)
                return index - 1;

            Console.Write($"输入无效，请输入 1-{choices.Count}，0 {zeroHint}：");
        }
    }

    public IReadOnlyList<int> ChooseMany(string title, IReadOnlyList<UiChoice> choices, IReadOnlyList<int> selected)
    {
        var state = choices.Select((_, index) => selected.Contains(index)).ToArray();
        while (true)
        {
            Console.WriteLine(title);
            for (var i = 0; i < choices.Count; i++)
            {
                var suffix = string.IsNullOrWhiteSpace(choices[i].Desc) ? "" : $" — {choices[i].Desc}";
                Console.WriteLine($"  {i + 1}. [{(state[i] ? "x" : " ")}] {choices[i].Label}{suffix}");
            }

            Console.Write("输入编号切换勾选（可多个，如 1,3），直接回车确认：");
            var input = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(input))
                return Enumerable.Range(0, state.Length).Where(index => state[index]).ToList();
            foreach (var token in input.Split(',', ' ', '，', '、', ';', '；'))
            {
                if (int.TryParse(token.Trim(), out var number) && number >= 1 && number <= state.Length)
                    state[number - 1] = !state[number - 1];
            }
        }
    }

    // 分组多选：按组打印编号（跨组连续），输入编号切换勾选、直接回车确认，与 ChooseMany 同一套交互
    public IReadOnlyList<int> ChooseManyGrouped(
        string title,
        IReadOnlyList<UiChoice> choices,
        IReadOnlyList<SkillGroupChoice> groups,
        IReadOnlyList<int> selected)
    {
        var state = choices.Select((_, index) => selected.Contains(index)).ToArray();
        var flat = groups.SelectMany(group => group.SkillIndices).ToList();
        while (true)
        {
            Console.WriteLine(title);
            var cursor = 1;
            foreach (var group in groups)
            {
                Console.WriteLine($"  [{group.Title}]");
                foreach (var index in group.SkillIndices)
                {
                    var suffix = string.IsNullOrWhiteSpace(choices[index].Desc) ? "" : $" — {choices[index].Desc}";
                    Console.WriteLine($"  {cursor}. [{(state[index] ? "x" : " ")}] {choices[index].Label}{suffix}");
                    cursor++;
                }
            }

            Console.Write("输入编号切换勾选（可多个，如 1,3），a 全选/反选，直接回车确认：");
            var input = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(input))
                return Enumerable.Range(0, state.Length).Where(index => state[index]).ToList();
            if (input.Equals("a", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var index in flat)
                    state[index] = !state[index];
                continue;
            }

            foreach (var token in input.Split(',', ' ', '，', '、', ';', '；'))
            {
                if (int.TryParse(token.Trim(), out var number) && number >= 1 && number <= flat.Count)
                    state[flat[number - 1]] = !state[flat[number - 1]];
            }
        }
    }

    public void Head(string text)
    {
        Console.WriteLine(text);
    }

    public void Line(string text)
    {
        Console.WriteLine(text);
    }

    public void Bullets(IEnumerable<string> items)
    {
        foreach (var item in items)
            Console.WriteLine($"  - {item}");
    }

    public void FileTable(string title, IEnumerable<string> files)
    {
        Console.WriteLine(title);
        foreach (var file in files)
            Console.WriteLine($"  - {file}");
    }

    // 纯文本 / 管道场景不做动画：直接执行（stdout 是日志与编号交互流，插帧只会污染它）
    public T WithProgress<T>(string label, Func<T> action)
        => action();
}
