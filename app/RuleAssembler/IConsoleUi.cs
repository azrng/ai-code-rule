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

    // 能力多选：selected 为默认勾选的下标集合；确认后返回勾选的下标集合
    IReadOnlyList<int> ChooseMany(string title, IReadOnlyList<UiChoice> choices, IReadOnlyList<int> selected);

    // 分组多选：choices 为全量技能（下标与全量清单对齐），groups 为树形分组视图。
    // 返回勾选的全量下标集合；选择 0 项（取消）返回空集合，由调用方按「跳过导入」处置
    IReadOnlyList<int> ChooseManyGrouped(
        string title,
        IReadOnlyList<UiChoice> choices,
        IReadOnlyList<SkillGroupChoice> groups,
        IReadOnlyList<int> selected);

    // 包裹一段耗时操作并显示加载指示（转圈 + 文案）：交互终端渲染动画，纯文本实现直接执行。
    // 当前用于启动更新检查（latest.json 拉取最多阻塞 3 秒，不给反馈看起来像卡死）
    T WithProgress<T>(string label, Func<T> action);
}
