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

// P1-2 检测补答：部分检测结果保留已确认维度，只对未收敛维度补答
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

        // frontend 问题的选项从候选推导：React / Vue 各一，按 label 序 React 在前，选 1 = Vue
        var ui = new ScriptedConsoleUi(1);
        var selection = new ConsoleWizard(ui).SupplementByWizard(_manifest, detection);

        Assert.NotNull(selection);
        Assert.Equal("vueElectronDotNet", selection!.Profile.Id);
        // 补答完成的配方保留 mode=detected
        Assert.Equal("detected", selection.Mode);
        Assert.Equal("vue", selection.Answers["frontend"]);
        // 已确认维度保留且来源为 detected，不被覆盖
        Assert.Equal("desktop", selection.Answers["form"]);
        Assert.Equal("dotnet", selection.Answers["backend"]);
        Assert.Equal("electron", selection.Answers["shell"]);
        Assert.Equal("detected", selection.AnswerSources["form"]);
        Assert.Equal("detected", selection.AnswerSources["backend"]);
        Assert.Equal("detected", selection.AnswerSources["shell"]);
        Assert.Equal("manual", selection.AnswerSources["frontend"]);
    }

    [Fact]
    public void Supplement_ConfirmedTraitsAreNotAskedAgain()
    {
        var detection = DetectElectronWithoutRenderer();

        var ui = new ScriptedConsoleUi(1);
        _ = new ConsoleWizard(ui).SupplementByWizard(_manifest, detection);

        // 整个补答只提问 frontend 一问，form/backend/shell 不重复提问
        Assert.Single(ui.Titles);
        Assert.Contains("前端", ui.Titles[0], StringComparison.Ordinal);
    }

    [Fact]
    public void Supplement_CancelAtFirstQuestion_ReturnsNull()
    {
        var detection = DetectElectronWithoutRenderer();

        var ui = new ScriptedConsoleUi((int?)null);
        var selection = new ConsoleWizard(ui).SupplementByWizard(_manifest, detection);

        Assert.Null(selection);
    }

    [Fact]
    public void Supplement_PartialResultDoesNotRequireProfileId()
    {
        // 部分 DetectionResult 不要求存在可反查的 Profile ID
        var detection = DetectElectronWithoutRenderer();

        Assert.Null(detection.LegacyProfileId);
        Assert.Null(detection.LegacyProfileLabel);
        Assert.Empty(detection.BuildLegacyAnswers());
        Assert.Equal(2, detection.Candidates.Count);
    }
}
