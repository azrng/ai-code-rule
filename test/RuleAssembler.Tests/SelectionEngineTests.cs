using Xunit;

namespace RuleAssembler.Tests;

// SelectionEngine（P2 阶段 3 组件问答）行为回归：
// 条件跳题、唯一值自动推导、可行性剪枝（不提供死组合选项）、全路径可解析且唯一映射兼容样例。
public class SelectionEngineTests
{
    private static Manifest LoadManifest()
    {
        return new ManifestLoader(new ResourceStore()).Load();
    }

    private static SelectionEngine CreateEngine(Manifest manifest)
    {
        return new SelectionEngine(manifest);
    }

    [Fact]
    public void 桌面React路径_唯一取值自动推导_不追问arch与blazor与ai()
    {
        var manifest = LoadManifest();
        var engine = CreateEngine(manifest);
        var selection = new Selection();

        // Q1 形态
        var form = engine.NextQuestion(selection);
        Assert.Equal("form", form!.Trait);
        selection.Dimensions["form"] = "desktop";

        // Q2 界面（前端先于后端）
        var frontend = engine.NextQuestion(selection);
        Assert.Equal("frontend", frontend!.Trait);
        selection.Dimensions["frontend"] = "react";

        // backend 仍有分歧（electron dotnet/none、wails go、tauri rust），提问
        var backend = engine.NextQuestion(selection);
        Assert.Equal("backend", backend!.Trait);
        Assert.Contains(backend.Options, option => option.Value == "go");
        Assert.Contains(backend.Options, option => option.Value == "rust");
        selection.Dimensions["backend"] = "dotnet";

        // 此后 shell=electron、arch-style、blazor-flavor、ai-capability 全部唯一，自动推导到完备
        var derived = new List<(string Trait, string Value)>();
        Assert.Null(engine.NextQuestion(selection, derived));

        Assert.Equal("electron", selection.Dimensions["shell"]);
        // electron 组件的 when 不引用 arch-style / ai-capability：既不提问也不推导（不问不猜），
        // 兼容样例映射按共有维度比较，不要求选择包含样例的全部维度
        Assert.DoesNotContain(selection.Dimensions, pair => pair.Key == "arch-style");
        Assert.DoesNotContain(selection.Dimensions, pair => pair.Key == "ai-capability");
    }

    [Fact]
    public void 桌面VueRust路径_自动推导tauri壳_唯一映射vueTauri()
    {
        var manifest = LoadManifest();
        var engine = CreateEngine(manifest);
        var selection = new Selection();
        selection.Dimensions["form"] = "desktop";

        var frontend = engine.NextQuestion(selection);
        Assert.Equal("frontend", frontend!.Trait);
        selection.Dimensions["frontend"] = "vue";

        // backend 在 electron dotnet/none、wails go、tauri rust 之间仍有分歧
        var backend = engine.NextQuestion(selection);
        Assert.Equal("backend", backend!.Trait);
        Assert.Contains(backend.Options, option => option.Value == "rust");
        selection.Dimensions["backend"] = "rust";

        // rust 后端下 shell=tauri 唯一，自动推导；arch-style 等维度 tauri 组件不引用，不问不猜
        var derived = new List<(string Trait, string Value)>();
        Assert.Null(engine.NextQuestion(selection, derived));
        Assert.Equal("tauri", selection.Dimensions["shell"]);

        var matches = SelectionEngine.MatchCompatibilityCases(manifest, selection);
        Assert.Equal(new[] { "vueTauri" }, matches);
        var plan = new CompositionPlanner(manifest).Plan(selection);
        Assert.Contains(plan.ComponentIds, id => id == "tauri-frontend-vue");
    }
    [Fact]
    public void 纯后端Dotnet路径_提问arch_style维度_唯一映射dotnetLayered()
    {
        var manifest = LoadManifest();
        var engine = CreateEngine(manifest);
        var selection = new Selection();
        selection.Dimensions["form"] = "backend-only";

        // python 组件引用 frontend=none，唯一取值自动推导；backend 在 dotnet / python 之间提问
        var derived0 = new List<(string Trait, string Value)>();
        var backend = engine.NextQuestion(selection, derived0);
        Assert.Equal("backend", backend!.Trait);
        selection.Dimensions["backend"] = "dotnet";

        // 分层 / 单层仍有分歧，arch-style 提问；其余维度唯一取值自动推导
        var arch = engine.NextQuestion(selection);
        Assert.Equal("arch-style", arch!.Trait);
        selection.Dimensions["arch-style"] = "layered";

        var derived = new List<(string Trait, string Value)>();
        Assert.Null(engine.NextQuestion(selection, derived));
        // shell 维度两种状态都合法：未被存续组件引用时不问不猜（不进入选择），
        // 被 python 等组件引用且取值唯一时自动推导为 none——断言与问答步骤顺序解耦
        Assert.True(!selection.Dimensions.ContainsKey("shell") || selection.Dimensions["shell"] == "none",
            "shell 应缺席或为 none，实际：" + string.Join("、", selection.Dimensions.Select(pair => $"{pair.Key}={pair.Value}")));

        var matches = SelectionEngine.MatchCompatibilityCases(manifest, selection);
        Assert.Equal(new[] { "dotnet-layered" }, matches);
        var plan = new CompositionPlanner(manifest).Plan(selection);
        Assert.Equal(new[] { "dotnet-backend-layered" }, plan.ComponentIds);
    }

    [Fact]
    public void WebBlazor路径_提问blazor_flavor_映射dotnetBlazorWebApp()
    {
        var manifest = LoadManifest();
        var engine = CreateEngine(manifest);
        var selection = new Selection();
        selection.Dimensions["form"] = "web";

        var frontend = engine.NextQuestion(selection);
        Assert.Equal("frontend", frontend!.Trait);
        Assert.Contains(frontend.Options, option => option.Value == "blazor");
        selection.Dimensions["frontend"] = "blazor";

        // blazor 下 backend=dotnet、shell=none、ai-capability=no 唯一，自动推导；形态仍有分歧
        var flavor = engine.NextQuestion(selection);
        Assert.Equal("blazor-flavor", flavor!.Trait);
        Assert.Contains(flavor.Options, option => option.Value == "traditional");
        Assert.Contains(flavor.Options, option => option.Value == "webapp");
        selection.Dimensions["blazor-flavor"] = "webapp";

        var derived = new List<(string Trait, string Value)>();
        Assert.Null(engine.NextQuestion(selection, derived));
        Assert.Equal("dotnet", selection.Dimensions["backend"]);

        var matches = SelectionEngine.MatchCompatibilityCases(manifest, selection);
        Assert.Equal(new[] { "dotnetBlazorWebApp" }, matches);
        var plan = new CompositionPlanner(manifest).Plan(selection);
        Assert.Contains(plan.ComponentIds, id => id == "web-frontend-blazor-webapp");
        Assert.Contains(plan.ComponentIds, id => id == "web-backend-blazor");
    }

    [Fact]
    public void 纯控制台路径只问形态与界面_其余全部自动推导()
    {
        var manifest = LoadManifest();
        var engine = CreateEngine(manifest);
        var selection = new Selection();
        selection.Dimensions["form"] = "desktop";

        var frontend = engine.NextQuestion(selection);
        Assert.Equal("frontend", frontend!.Trait);
        selection.Dimensions["frontend"] = "console";

        var derived = new List<(string Trait, string Value)>();
        Assert.Null(engine.NextQuestion(selection, derived));
        // 后端/壳不再提问：console 组合下 backend=none、shell=none 是唯一可行取值
        Assert.Equal("none", selection.Dimensions["backend"]);
        Assert.Equal("none", selection.Dimensions["shell"]);
    }

    [Fact]
    public void 可行性剪枝_界面选项不出现未接入组合()
    {
        var manifest = LoadManifest();
        var engine = CreateEngine(manifest);
        var selection = new Selection();
        selection.Dimensions["form"] = "desktop";

        var frontend = engine.NextQuestion(selection);
        Assert.NotNull(frontend);
        // 桌面形态下不存在 flutter / maui / blazor / html 界面组合，不应出现在选项中
        Assert.DoesNotContain(frontend!.Options, option => option.Value is "flutter" or "maui" or "blazor" or "html");
        Assert.Contains(frontend.Options, option => option.Value == "avalonia");
    }

    [Fact]
    public void WebReactDotNet路径_aidim分岔_回答yes落到AI变体()
    {
        var manifest = LoadManifest();
        var engine = CreateEngine(manifest);
        var selection = new Selection();
        selection.Dimensions["form"] = "web";
        selection.Dimensions["frontend"] = "react";
        selection.Dimensions["backend"] = "dotnet";

        // shell=none 与 blazor-flavor=none 唯一，自动推导；ai-capability 有分岔，提问
        var ai = engine.NextQuestion(selection);
        Assert.Equal("ai-capability", ai!.Trait);
        selection.Dimensions["ai-capability"] = "yes";

        Assert.Null(engine.NextQuestion(selection));
        var planner = new CompositionPlanner(manifest);
        var plan = planner.Plan(selection);
        var matches = SelectionEngine.MatchCompatibilityCases(manifest, selection);
        Assert.Equal(new[] { "reactLinkdotNet-ai" }, matches);
        Assert.Contains(plan.ComponentIds, id => id == "reactLinkdotNet-ai");
    }

    [Fact]
    public void 全路径仿真_每条交互路径可解析且唯一映射_29样例可达()
    {
        var manifest = LoadManifest();
        var engine = CreateEngine(manifest);
        var errors = engine.SimulateAll(new CompositionPlanner(manifest));
        Assert.True(errors.Count == 0, "组件问答决策树校验失败：\n" + string.Join("\n", errors));
    }

    [Fact]
    public void 兼容样例映射_共有维度冲突的样例不入选()
    {
        var manifest = LoadManifest();
        var selection = new Selection
        {
            Dimensions = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["form"] = "web",
                ["frontend"] = "react",
                ["backend"] = "dotnet",
                ["shell"] = "none"
            }
        };

        var matches = SelectionEngine.MatchCompatibilityCases(manifest, selection);
        // reactLinkdotNet 与 reactLinkdotNet-ai 在共有维度上都不冲突（ai 未回答），
        // 唯一性由问答回答 ai 维度后达成
        Assert.Contains("reactLinkdotNet", matches);
        Assert.Contains("reactLinkdotNet-ai", matches);
        Assert.DoesNotContain("vueLinkdotNet", matches);
    }
}
