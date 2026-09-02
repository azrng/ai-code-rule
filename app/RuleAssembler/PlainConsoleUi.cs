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
}
