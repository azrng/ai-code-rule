namespace RuleAssembler.Tests;

// 脚本化 UI：按预设顺序返回选择，用于无终端环境的向导流程测试
internal sealed class ScriptedConsoleUi : IConsoleUi
{
    private readonly Queue<int?> _choices;

    public ScriptedConsoleUi(params int?[] choices)
    {
        _choices = new Queue<int?>(choices);
    }

    public List<string> Lines { get; } = new();

    public List<string> Titles { get; } = new();

    public int? Choose(string title, string zeroHint, IReadOnlyList<UiChoice> choices)
    {
        Titles.Add(title);
        return _choices.Count > 0 ? _choices.Dequeue() : null;
    }

    public IReadOnlyList<int> ChooseMany(string title, IReadOnlyList<UiChoice> choices, IReadOnlyList<int> selected)
    {
        Titles.Add(title);
        // 脚本化多选：预设的正整数按序切换勾选，遇 null 或队列耗尽即按当前状态确认
        var state = choices.Select((_, index) => selected.Contains(index)).ToArray();
        while (_choices.Count > 0 && _choices.Peek() is { } number)
        {
            _choices.Dequeue();
            if (number >= 1 && number <= state.Length)
                state[number - 1] = !state[number - 1];
        }

        return Enumerable.Range(0, state.Length).Where(index => state[index]).ToList();
    }

    // 脚本化分组多选：与 ChooseMany 同一套脚本语义，直接确认返回预选集合
    public IReadOnlyList<int> ChooseManyGrouped(
        string title,
        IReadOnlyList<UiChoice> choices,
        IReadOnlyList<SkillGroupChoice> groups,
        IReadOnlyList<int> selected)
    {
        Titles.Add(title);
        return selected.ToList();
    }

    public void Head(string text) => Lines.Add(text);

    public void Line(string text) => Lines.Add(text);

    public void Bullets(IEnumerable<string> items)
    {
        foreach (var item in items)
            Lines.Add(item);
    }

    public void FileTable(string title, IEnumerable<string> files)
    {
        Lines.Add(title);
        foreach (var file in files)
            Lines.Add(file);
    }
}

// P1-2 检测补答（组件路径）：部分检测结果保留已确认维度，只对未收敛维度补答。
// 生产补答入口是 InteractiveFlow.RunSelectionWizard(seeds=detection.Confirmed)，测试直接驱动它
public sealed class SupplementWizardTests : IDisposable
{
    private readonly string _root;
    private readonly Manifest _manifest;

    public SupplementWizardTests()
    {
        _manifest = new ManifestLoader(new ResourceStore()).Load();
        _root = Path.Combine(Path.GetTempPath(), $"rule-supplement-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
    }

    // Electron + .NET 但无 react/vue 依赖：form/shell/backend 已确认，frontend 未确认
    private DetectionResult DetectElectronWithoutRenderer()
    {
        File.WriteAllText(
            Path.Combine(_root, "package.json"),
            "{\"devDependencies\":{\"electron\":\"^33.0.0\"}}");
        File.WriteAllText(Path.Combine(_root, "electron.vite.config.ts"), "export default {}");
        Directory.CreateDirectory(Path.Combine(_root, "src-dotnet"));
        File.WriteAllText(
            Path.Combine(_root, "src-dotnet", "Sidecar.csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");

        var result = new ProjectDetector(_manifest).Detect(_root);
        Assert.NotNull(result);
        Assert.False(result!.Converged);
        return result;
    }

    [Fact]
    public void Supplement_VueAnswer_LandsOnVueElectronDotNet()
    {
        var detection = DetectElectronWithoutRenderer();
        Assert.Equal(new[] { "frontend" }, detection.Unconfirmed);

        // frontend 问题的选项从存续组件推导：React / Vue 各一，按 label 序 React 在前，选 1 = Vue
        var ui = new ScriptedConsoleUi(1);
        var result = InteractiveFlow.RunSelectionWizard(
            _manifest, ui, "detected", new Dictionary<string, string>(detection.Confirmed, StringComparer.Ordinal));

        Assert.True(result.Outcome is not null);
        var outcome = result.Outcome!;
        Assert.Equal("vue", outcome.Selection.Dimensions["frontend"]);
        // 补答完成的配方保留 mode=detected
        Assert.Equal("detected", outcome.Mode);
        Assert.Equal("vue", outcome.Answers["frontend"]);
        // 已确认维度保留且来源为 detected，不被覆盖
        Assert.Equal("desktop", outcome.Answers["form"]);
        Assert.Equal("dotnet", outcome.Answers["backend"]);
        Assert.Equal("electron", outcome.Answers["shell"]);
        Assert.Equal("detected", outcome.AnswerSources["form"]);
        Assert.Equal("detected", outcome.AnswerSources["backend"]);
        Assert.Equal("detected", outcome.AnswerSources["shell"]);
        Assert.Equal("manual", outcome.AnswerSources["frontend"]);
    }

    [Fact]
    public void Supplement_ConfirmedTraitsAreNotAskedAgain()
    {
        var detection = DetectElectronWithoutRenderer();

        var ui = new ScriptedConsoleUi(1);
        _ = InteractiveFlow.RunSelectionWizard(
            _manifest, ui, "detected", new Dictionary<string, string>(detection.Confirmed, StringComparer.Ordinal));

        // 整个补答只提问 frontend 一问，form/backend/shell 不重复提问
        Assert.Single(ui.Titles);
        Assert.Contains("前端", ui.Titles[0], StringComparison.Ordinal);
    }

    [Fact]
    public void Supplement_CancelAtFirstQuestion_ReturnsBackToEntry()
    {
        var detection = DetectElectronWithoutRenderer();

        var ui = new ScriptedConsoleUi((int?)null);
        var result = InteractiveFlow.RunSelectionWizard(
            _manifest, ui, "detected", new Dictionary<string, string>(detection.Confirmed, StringComparer.Ordinal));

        // 第一问按 0：交还入口菜单而不是取消生成器
        Assert.True(result.BackToEntry);
        Assert.Null(result.Outcome);
    }

    [Fact]
    public void Wizard_BackAfterAnswer_UndoThenBackToEntry()
    {
        // 全新问答：form=web 后 frontend 仍有分歧，构造「答一步 → 回退 → 再回退」路径
        var ui = new ScriptedConsoleUi(1, (int?)null, (int?)null);
        var result = InteractiveFlow.RunSelectionWizard(_manifest, ui, "wizard");

        // 第二次按 0 时已无更上一步：交还入口菜单而不是取消生成器
        Assert.True(result.BackToEntry);
        Assert.Null(result.Outcome);
        // 流程：form → frontend → 回退回 form → 再按 0 交还入口
        Assert.Equal(2, ui.Titles.Count(t => t.Contains("项目形态", StringComparison.Ordinal)));
        Assert.Single(ui.Titles, t => t.Contains("前端", StringComparison.Ordinal));
    }

    [Fact]
    public void Supplement_PartialResultDoesNotRequireProfileId()
    {
        // 部分 DetectionResult 不要求存在可反查的 Profile ID
        var detection = DetectElectronWithoutRenderer();

        Assert.Null(detection.SampleProfileId);
        Assert.Null(detection.SampleLabel);
        Assert.Empty(detection.BuildSampleAnswers());
        Assert.Equal(2, detection.Candidates.Count);
    }

    [Fact]
    public void Supplement_CompletedSelectionPlansToElectronFamily()
    {
        // 补答完成的选择必须能被组合内核解析为 Electron + Vue + .NET 组件集
        var detection = DetectElectronWithoutRenderer();
        var ui = new ScriptedConsoleUi(1);
        var result = InteractiveFlow.RunSelectionWizard(
            _manifest, ui, "detected", new Dictionary<string, string>(detection.Confirmed, StringComparer.Ordinal));

        Assert.True(result.Outcome is not null);
        var plan = new CompositionPlanner(_manifest).Plan(result.Outcome!.Selection);
        Assert.Contains(plan.ComponentIds, id => id == "electron-desktop-shell");
        Assert.Contains(plan.ComponentIds, id => id == "electron-frontend-vue-dotnet");
        Assert.Contains(plan.ComponentIds, id => id == "electron-backend-dotnet");
    }
}
