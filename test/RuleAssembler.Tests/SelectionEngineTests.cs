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
    public void 桌面React路径_壳显式可选_后端区分主进程与sidecar()
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

        // Q3 壳：混合壳桌面必须显式出现 Electron / Tauri / Wails 选项
        var shell = engine.NextQuestion(selection);
        Assert.Equal("shell", shell!.Trait);
        Assert.Contains(shell.Options, option => option.Value == "electron");
        Assert.Contains(shell.Options, option => option.Value == "tauri");
        Assert.Contains(shell.Options, option => option.Value == "wails");
        selection.Dimensions["shell"] = "electron";

        // Q4 后端：Electron 壳下只剩 主进程(none) 与 .NET sidecar(dotnet) 之分
        var backend = engine.NextQuestion(selection);
        Assert.Equal("backend", backend!.Trait);
        Assert.Contains(backend.Options, option => option.Value == "none");
        Assert.Contains(backend.Options, option => option.Value == "dotnet");
        Assert.DoesNotContain(backend.Options, option => option.Value is "go" or "rust");
        selection.Dimensions["backend"] = "dotnet";

        // 此后 arch-style、blazor-flavor、ai-capability 全部不引用，不问不猜，选择已完备
        var derived = new List<(string Trait, string Value)>();
        Assert.Null(engine.NextQuestion(selection, derived));

        // electron 组件的 when 不引用 arch-style / ai-capability：既不提问也不推导（不问不猜），
        // 兼容样例映射按共有维度比较，不要求选择包含样例的全部维度
        Assert.DoesNotContain(selection.Dimensions, pair => pair.Key == "arch-style");
        Assert.DoesNotContain(selection.Dimensions, pair => pair.Key == "ai-capability");
    }

    [Fact]
    public void 桌面VueTauri壳路径_自动推导rust后端_唯一映射vueTauri()
    {
        var manifest = LoadManifest();
        var engine = CreateEngine(manifest);
        var selection = new Selection();
        selection.Dimensions["form"] = "desktop";

        var frontend = engine.NextQuestion(selection);
        Assert.Equal("frontend", frontend!.Trait);
        selection.Dimensions["frontend"] = "vue";

        // 壳在 electron / tauri / wails 之间仍有分歧，显式提问
        var shell = engine.NextQuestion(selection);
        Assert.Equal("shell", shell!.Trait);
        Assert.Contains(shell.Options, option => option.Value == "tauri");
        selection.Dimensions["shell"] = "tauri";

        // tauri 壳下 backend=rust 唯一，自动推导；arch-style 等 tauri 组件不引用，不问不猜
        var derived = new List<(string Trait, string Value)>();
        Assert.Null(engine.NextQuestion(selection, derived));
        Assert.Equal("rust", selection.Dimensions["backend"]);

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
    public void WebReactDotNet路径_先问arch_style_单项目下aidim分岔落到AI变体()
    {
        var manifest = LoadManifest();
        var engine = CreateEngine(manifest);
        var selection = new Selection();
        selection.Dimensions["form"] = "web";
        selection.Dimensions["frontend"] = "react";
        selection.Dimensions["backend"] = "dotnet";

        // 多项目（layered）与单项目（single，含 reactLinkdotNet-ai）仍有分歧，先问架构
        var arch = engine.NextQuestion(selection);
        Assert.Equal("arch-style", arch!.Trait);
        Assert.Contains(arch.Options, option => option.Value == "layered");
        Assert.Contains(arch.Options, option => option.Value == "single");
        selection.Dimensions["arch-style"] = "single";

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
    public void WebReactDotNetSingle路径_单项目落reactLinkdotNetSingle_复用前端组件()
    {
        var manifest = LoadManifest();
        var engine = CreateEngine(manifest);
        var selection = new Selection();
        selection.Dimensions["form"] = "web";
        selection.Dimensions["frontend"] = "react";
        selection.Dimensions["backend"] = "dotnet";

        var arch = engine.NextQuestion(selection);
        Assert.Equal("arch-style", arch!.Trait);
        selection.Dimensions["arch-style"] = "single";

        var ai = engine.NextQuestion(selection);
        Assert.Equal("ai-capability", ai!.Trait);
        selection.Dimensions["ai-capability"] = "no";

        Assert.Null(engine.NextQuestion(selection));
        var matches = SelectionEngine.MatchCompatibilityCases(manifest, selection);
        Assert.Equal(new[] { "reactLinkdotNetSingle" }, matches);
        var plan = new CompositionPlanner(manifest).Plan(selection);
        // 前端组件跨 arch-style 复用，只有后端组件按架构分叉
        Assert.Contains(plan.ComponentIds, id => id == "web-dotnet-shell");
        Assert.Contains(plan.ComponentIds, id => id == "web-frontend-react");
        Assert.Contains(plan.ComponentIds, id => id == "web-backend-react-single");
        Assert.DoesNotContain(plan.ComponentIds, id => id == "web-backend-react");
    }

    [Fact]
    public void WebReactPython路径_后端可选python_自动推导剩余维度_唯一映射reactLinkPython()
    {
        var manifest = LoadManifest();
        var engine = CreateEngine(manifest);
        var selection = new Selection();
        selection.Dimensions["form"] = "web";

        var frontend = engine.NextQuestion(selection);
        Assert.Equal("frontend", frontend!.Trait);
        selection.Dimensions["frontend"] = "react";

        // python 在 web 形态下成为可行后端选项（此前被样例剪枝掉）
        var backend = engine.NextQuestion(selection);
        Assert.Equal("backend", backend!.Trait);
        Assert.Contains(backend.Options, option => option.Value == "python");
        selection.Dimensions["backend"] = "python";

        // shell / blazor-flavor / ai-capability 唯一，自动推导；arch-style 无组件引用，不问不猜
        var derived = new List<(string Trait, string Value)>();
        Assert.Null(engine.NextQuestion(selection, derived));
        Assert.Equal("none", selection.Dimensions["shell"]);
        Assert.DoesNotContain(selection.Dimensions, pair => pair.Key == "arch-style");

        var matches = SelectionEngine.MatchCompatibilityCases(manifest, selection);
        Assert.Equal(new[] { "reactLinkPython" }, matches);
        var plan = new CompositionPlanner(manifest).Plan(selection);
        Assert.Contains(plan.ComponentIds, id => id == "web-python-shell");
        Assert.Contains(plan.ComponentIds, id => id == "web-frontend-python-react");
        Assert.Contains(plan.ComponentIds, id => id == "web-backend-python-react");
    }

    [Fact]
    public void WebVueNode路径_后端可选node_唯一映射nuxtFullstack()
    {
        var manifest = LoadManifest();
        var engine = CreateEngine(manifest);
        var selection = new Selection();
        selection.Dimensions["form"] = "web";

        var frontend = engine.NextQuestion(selection);
        Assert.Equal("frontend", frontend!.Trait);
        selection.Dimensions["frontend"] = "vue";

        // node 在 vue 分支下由 Nuxt 全栈承接（Next.js 只服务 react 分支）
        var backend = engine.NextQuestion(selection);
        Assert.Equal("backend", backend!.Trait);
        Assert.Contains(backend.Options, option => option.Value == "node");
        selection.Dimensions["backend"] = "node";

        var derived = new List<(string Trait, string Value)>();
        Assert.Null(engine.NextQuestion(selection, derived));
        Assert.Equal("none", selection.Dimensions["shell"]);

        var matches = SelectionEngine.MatchCompatibilityCases(manifest, selection);
        Assert.Equal(new[] { "nuxtFullstack" }, matches);
        var plan = new CompositionPlanner(manifest).Plan(selection);
        Assert.Contains(plan.ComponentIds, id => id == "nuxtFullstack");
    }

    [Fact]
    public void 全路径仿真_每条交互路径可解析且唯一映射_34样例可达()
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
