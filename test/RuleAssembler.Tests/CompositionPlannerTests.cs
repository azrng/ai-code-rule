using Xunit;

namespace RuleAssembler.Tests;

// CompositionPlanner 行为回归：证据不足不隐式补组件、约束与冲突显式报错、
// 槽位唯一提供者与 family 基线继承等组合语义都要有测试钉住
public class CompositionPlannerTests
{
    private static Manifest LoadManifest()
    {
        return new ManifestLoader(new ResourceStore()).Load();
    }

    private static Selection FromDimensions(Dictionary<string, string> dimensions)
    {
        return new Selection { Dimensions = new Dictionary<string, string>(dimensions, StringComparer.Ordinal) };
    }

    [Fact]
    public void 违反约束的组合报错_tauri壳_要求rust后端()
    {
        var planner = new CompositionPlanner(LoadManifest());
        var selection = FromDimensions(new Dictionary<string, string>
        {
            ["form"] = "desktop",
            ["frontend"] = "react",
            ["shell"] = "tauri",
            ["backend"] = "go"
        });

        var ex = Assert.Throws<InvalidOperationException>(() => planner.Plan(selection));
        Assert.Contains("组合违反约束", ex.Message);
        Assert.Contains("backend=rust", ex.Message);
    }

    [Fact]
    public void 违反约束的组合报错_纯后端形态_要求无前端()
    {
        var planner = new CompositionPlanner(LoadManifest());
        var selection = FromDimensions(new Dictionary<string, string>
        {
            ["form"] = "backend-only",
            ["frontend"] = "react",
            ["backend"] = "dotnet",
            ["arch-style"] = "layered"
        });

        var ex = Assert.Throws<InvalidOperationException>(() => planner.Plan(selection));
        Assert.Contains("组合违反约束", ex.Message);
        Assert.Contains("frontend=none", ex.Message);
    }

    [Fact]
    public void 违反约束的组合报错_静态形态_要求html前端与无后端()
    {
        var planner = new CompositionPlanner(LoadManifest());
        var selection = FromDimensions(new Dictionary<string, string>
        {
            ["form"] = "static",
            ["frontend"] = "react",
            ["backend"] = "none",
            ["shell"] = "none"
        });

        var ex = Assert.Throws<InvalidOperationException>(() => planner.Plan(selection));
        Assert.Contains("组合违反约束", ex.Message);
        Assert.Contains("frontend=html", ex.Message);
    }

    [Fact]
    public void 维度缺失时报错并列出待确认维度_不隐式补全()
    {
        var planner = new CompositionPlanner(LoadManifest());
        // 只有形态与壳，缺少 frontend / backend：任何组件都不允许隐式补位
        var selection = FromDimensions(new Dictionary<string, string>
        {
            ["form"] = "desktop",
            ["shell"] = "electron"
        });

        var ex = Assert.Throws<InvalidOperationException>(() => planner.Plan(selection));
        Assert.Contains("组合证据不足", ex.Message);
        Assert.Contains("frontend", ex.Message);
        Assert.Contains("backend", ex.Message);
    }

    [Fact]
    public void 违反约束的组合报错_shell_electron_要求_form_desktop()
    {
        var planner = new CompositionPlanner(LoadManifest());
        var selection = FromDimensions(new Dictionary<string, string>
        {
            ["form"] = "web",
            ["shell"] = "electron",
            ["frontend"] = "react",
            ["backend"] = "none"
        });

        var ex = Assert.Throws<InvalidOperationException>(() => planner.Plan(selection));
        Assert.Contains("违反约束", ex.Message);
        Assert.Contains("form=desktop", ex.Message);
    }

    [Fact]
    public void 未知维度或缺少文案的取值报错()
    {
        var planner = new CompositionPlanner(LoadManifest());
        var selection = FromDimensions(new Dictionary<string, string>
        {
            ["form"] = "desktop",
            ["shell"] = "electron",
            ["frontend"] = "react",
            ["backend"] = "not-a-backend"
        });

        var ex = Assert.Throws<InvalidOperationException>(() => planner.Plan(selection));
        Assert.Contains("缺少 wizard.options 文案", ex.Message);
    }

    [Fact]
    public void Electron组合解析出骨架_前端_后端三类组件()
    {
        var planner = new CompositionPlanner(LoadManifest());
        var plan = planner.Plan(FromDimensions(new Dictionary<string, string>
        {
            ["form"] = "desktop",
            ["shell"] = "electron",
            ["frontend"] = "vue",
            ["backend"] = "dotnet"
        }));

        Assert.Equal(
            new[] { "electron-desktop-shell", "electron-frontend-vue-dotnet", "electron-backend-dotnet" },
            plan.ComponentIds);
        // 阶段 5 内容收敛后，vue 电子 design-system 指向共享骨架
        Assert.Equal("shared/skeletons/electron/design-system-vue.yaml", plan.DesignSystemSource);
        Assert.Contains(plan.Files, file => file.Output == "frontend-AGENTS.md");
        Assert.Contains(plan.Files, file => file.Output == "backend-AGENTS.md");
        // 槽位全部被骨架消费，不应有残留告警
        Assert.DoesNotContain(plan.Warnings, warning => warning.Contains("未被骨架消费"));
    }

    [Fact]
    public void 解析后的根变量不再携带槽位占位()
    {
        var planner = new CompositionPlanner(LoadManifest());
        var plan = planner.Plan(FromDimensions(new Dictionary<string, string>
        {
            ["form"] = "desktop",
            ["shell"] = "electron",
            ["frontend"] = "react",
            ["backend"] = "none"
        }));

        var texts = plan.Root.TargetList
            .Concat(plan.Root.SubruleIndex)
            .Concat(plan.Root.DevStage)
            .Concat(plan.Root.UnderstandingChecklist)
            .Select(line => line.Text)
            .Concat(new[] { plan.Root.SearchNouns, plan.Root.ContractErrorLine });
        foreach (var text in texts)
            Assert.DoesNotContain("{{slot:", text);

        // 槽位替换后应还原对应味道的文案
        Assert.Contains("类型 / schema", plan.Root.SearchNouns);
        Assert.DoesNotContain("DTO / schema", plan.Root.SearchNouns);
        Assert.Contains(plan.Root.TargetList.Select(line => line.Text), text => text.Contains("主进程服务"));
    }

    [Fact]
    public void Wails组合解析出骨架_界面_后端组件且vue覆盖family基线()
    {
        var planner = new CompositionPlanner(LoadManifest());
        var plan = planner.Plan(FromDimensions(new Dictionary<string, string>
        {
            ["form"] = "desktop",
            ["shell"] = "wails",
            ["backend"] = "go",
            ["frontend"] = "vue"
        }));

        Assert.Equal(
            new[] { "wails-desktop-shell", "wails-frontend-vue", "wails-backend-go" },
            plan.ComponentIds);
        // vueWailsExe 刻意声明与类默认值相同的 destructive-nouns 以覆盖 family 基线，不得被当作伪声明剥掉
        Assert.Equal("数据破坏性变更、环境清理", plan.Root.DestructiveNouns);
        Assert.DoesNotContain(plan.Warnings, warning => warning.Contains("未被骨架消费"));
    }

    [Fact]
    public void 纯前端AI组合的文件顺序与旧路径一致_ai在界面之前()
    {
        var manifest = LoadManifest();
        var planner = new CompositionPlanner(manifest);
        var traits = manifest.Profiles.First(profile => profile.Id == "reactOnly-ai").Traits!;
        var dimensions = ProfileTraits.KnownTraits.ToDictionary(
            trait => trait,
            trait => traits.Get(trait) ?? "",
            StringComparer.Ordinal);
        var plan = planner.Plan(FromDimensions(dimensions));

        var outputs = plan.Files.Select(file => file.Output).ToList();
        Assert.True(outputs.IndexOf("ai-AGENTS.md") < outputs.IndexOf("frontend-AGENTS.md"),
            "ai-AGENTS.md 必须先于 frontend-AGENTS.md（旧路径模块先于子规则渲染）");
        Assert.Equal("arch/reactLinkdotNet/design-system.yaml", plan.DesignSystemSource);
    }
}
