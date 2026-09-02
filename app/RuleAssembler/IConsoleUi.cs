namespace RuleAssembler;

// 交互层契约：向导与主流程只依赖本接口，不直接操作控制台。
// 交互式终端走 SpectreConsoleUi（方向键选择），stdin 重定向（管道/冒烟）走 PlainConsoleUi（编号输入）。
internal sealed record UiChoice(string Label, string Desc);

internal interface IConsoleUi
{
    // 显示标题与选项，返回被选下标；选择 0 项（取消/返回上一步）时返回 null
    int? Choose(string title, string zeroHint, IReadOnlyList<UiChoice> choices);

    void Head(string text);

    void Line(string text);

    void Bullets(IEnumerable<string> items);

    void FileTable(string title, IEnumerable<string> files);
}
