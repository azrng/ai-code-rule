namespace RuleAssembler.Tests;

// P1-4 兼容回归：必覆盖组合的「检测 → 能力开关 → 渲染 → 契约校验」全链路样例。
// 覆盖：.NET + React、.NET 分层、React + Wails、React + Electron + .NET、React / Vue + 纯 Electron、纯静态 HTML、纯后端 Python
public sealed class ComboRegressionTests : IDisposable
{
    private readonly string _root;
    private readonly Manifest _manifest;
    private readonly RuleRenderer _renderer;
    private readonly RootConsistencyValidator _validator = new();

    public ComboRegressionTests()
    {
        _manifest = new ManifestLoader(new ResourceStore()).Load();
        _renderer = new RuleRenderer(new ResourceStore());
        _root = Path.Combine(Path.GetTempPath(), $"rule-combo-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
    }

    private DetectionResult Detect(Action<string> seed)
    {
        seed(_root);
        var result = new ProjectDetector(_manifest).Detect(_root);
        Assert.NotNull(result);
        return result!;
    }

    private void AssertContractPasses(Profile profile, IReadOnlySet<string>? features = null)
    {
        var errors = features is null
            ? _validator.Validate(_manifest, _renderer, profile)
            : _validator.Validate(_manifest, _renderer, profile, features);
        Assert.True(errors.Count == 0, $"{profile.Id}: {string.Join("；", errors)}");
    }

    [Fact]
    public void Combo_DotnetReact_DetectsAndRenders()
    {
        // reactLinkdotNet 真实布局：根 Sdk.Web 工程 + frontend/package.json react
        var result = Detect(root =>
        {
            File.WriteAllText(
                Path.Combine(root, "Api.csproj"),
                "<Project Sdk=\"Microsoft.NET.Sdk.Web\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");
            Directory.CreateDirectory(Path.Combine(root, "frontend"));
            File.WriteAllText(
                Path.Combine(root, "frontend", "package.json"),
                "{\"dependencies\":{\"react\":\"^18.0.0\"}}");
        });

        // Web 工程 + 前端依赖 → form=web；单 csproj → arch-style=single，
        // 单项目 .NET + React 同时匹配 AI 与非 AI 两个 Profile，
        // ai 维度无证据 → 部分结果交由补答，不得隐式二选一
        Assert.False(result.Converged);
        Assert.Null(result.LegacyProfileId);
        Assert.Contains("ai-capability", result.Unconfirmed);
        Assert.Contains(result.Candidates, profile => profile.Id == "reactLinkdotNetSingle");
        Assert.Contains(result.Candidates, profile => profile.Id == "reactLinkdotNet-ai");
        var profile = Assert.Single(result.Candidates, item => item.Id == "reactLinkdotNetSingle");
        var files = _renderer.Plan(_manifest, profile, FeatureSelection.Defaults(_manifest, profile));
        Assert.Contains(files, file => file.Output == "frontend-AGENTS.md");
        Assert.Contains(files, file => file.Output == "backend-AGENTS.md");
        AssertContractPasses(profile);
    }

    [Fact]
    public void Combo_DotnetLayeredWeb_DetectsAndRenders()
    {
        // .NET 分层 + Web：多项目（Web 启动层 + 类库），无前端工程
        var result = Detect(root =>
        {
            File.WriteAllText(
                Path.Combine(root, "Web.csproj"),
                "<Project Sdk=\"Microsoft.NET.Sdk.Web\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");
            File.WriteAllText(
                Path.Combine(root, "Application.csproj"),
                "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");
            File.WriteAllText(
                Path.Combine(root, "Domain.csproj"),
                "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");
        });

        Assert.True(result.Converged);
        Assert.Equal("dotnet-layered", result.LegacyProfileId);
        Assert.Equal("layered", result.Confirmed["arch-style"]);
        var files = _renderer.Plan(_manifest, result.LegacyProfile);
        Assert.Equal(8, files.Count);
        var doc = Assert.Single(files, file => file.Output == "doc-AGENTS.md");
        Assert.Contains("doc/requirements/<module>/<feature>.md", doc.Content, StringComparison.Ordinal);
        Assert.Contains("契约引用", doc.Content, StringComparison.Ordinal);
        Assert.DoesNotContain("| 功能需求 |", doc.Content, StringComparison.Ordinal);
        AssertContractPasses(result.LegacyProfile);
    }

    [Fact]
    public void Combo_ReactWails_DetectsAndRenders()
    {
        var result = Detect(root =>
        {
            File.WriteAllText(Path.Combine(root, "wails.json"), "{\"name\":\"demo\"}");
            File.WriteAllText(Path.Combine(root, "go.mod"), "module demo\n\ngo 1.22\n");
            File.WriteAllText(
                Path.Combine(root, "package.json"),
                "{\"dependencies\":{\"react\":\"^18.0.0\"}}");
        });

        Assert.True(result.Converged);
        Assert.Equal("reactWailsExe", result.LegacyProfileId);
        Assert.Equal("react", result.Confirmed["frontend"]);
        Assert.Equal("wails", result.Confirmed["shell"]);
        var files = _renderer.Plan(_manifest, result.LegacyProfile);
        Assert.Equal(10, files.Count);
        AssertContractPasses(result.LegacyProfile);
    }

    [Fact]
    public void Combo_ReactElectronDotNet_DetectsAndRendersWithFeatureToggle()
    {
        var result = Detect(root =>
        {
            File.WriteAllText(
                Path.Combine(root, "package.json"),
                "{\"dependencies\":{\"react\":\"^18.0.0\"},\"devDependencies\":{\"electron\":\"^33.0.0\"}}");
            File.WriteAllText(Path.Combine(root, "electron.vite.config.ts"), "export default {}");
            Directory.CreateDirectory(Path.Combine(root, "src-dotnet"));
            File.WriteAllText(
                Path.Combine(root, "src-dotnet", "Sidecar.csproj"),
                "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");
        });

        Assert.True(result.Converged);
        Assert.Equal("reactElectronDotNet", result.LegacyProfileId);
        var profile = result.LegacyProfile;

        // 能力开关回归：取消文档与设计系统后仅剩核心 + 基础设施，契约仍一致
        var features = FeatureSelection.Defaults(_manifest, profile);
        features.Remove("doc");
        features.Remove("design-system");
        var files = _renderer.Plan(_manifest, profile, features);
        Assert.DoesNotContain(files, file => file.Output == "doc-AGENTS.md");
        Assert.DoesNotContain(files, file => file.Output == "design-system.yaml");
        Assert.Contains(files, file => file.Output == "infra-AGENTS.md");
        AssertContractPasses(profile, features);
    }

    [Fact]
    public void Combo_ReactElectronPure_DetectsAndRenders()
    {
        // 纯 Electron + React（无 .NET 工程）：收敛到 reactElectron，
        // backend-AGENTS.md 来自 electron 族骨架（主进程口径），不再出现 src-dotnet
        var result = Detect(root =>
        {
            File.WriteAllText(
                Path.Combine(root, "package.json"),
                "{\"dependencies\":{\"react\":\"^18.0.0\"},\"devDependencies\":{\"electron\":\"^33.0.0\"}}");
            File.WriteAllText(Path.Combine(root, "electron.vite.config.ts"), "export default {}");
        });

        Assert.True(result.Converged);
        Assert.Equal("reactElectron", result.LegacyProfileId);
        Assert.Equal("none", result.Confirmed["backend"]);
        var profile = result.LegacyProfile;

        var files = _renderer.Plan(_manifest, profile, FeatureSelection.Defaults(_manifest, profile));
        var backend = Assert.Single(files, file => file.Output == "backend-AGENTS.md");
        Assert.Contains("安全基线规则", backend.Content, StringComparison.Ordinal);
        Assert.DoesNotContain("src-dotnet", backend.Content, StringComparison.Ordinal);
        AssertContractPasses(profile);
    }

    [Fact]
    public void Combo_VueElectronPure_DetectsAndRenders()
    {
        // 纯 Electron + Vue：检测收敛到 vueElectron，契约校验通过
        var result = Detect(root =>
        {
            File.WriteAllText(
                Path.Combine(root, "package.json"),
                "{\"dependencies\":{\"vue\":\"^3.5.0\"},\"devDependencies\":{\"electron\":\"^33.0.0\"}}");
            File.WriteAllText(Path.Combine(root, "electron-builder.yml"), "appId: demo");
        });

        Assert.True(result.Converged);
        Assert.Equal("vueElectron", result.LegacyProfileId);
        AssertContractPasses(result.LegacyProfile);
    }

    [Fact]
    public void Combo_StaticHtml_DetectsAndRenders()
    {
        var result = Detect(root =>
        {
            File.WriteAllText(Path.Combine(root, "index.html"), "<!DOCTYPE html><html><body></body></html>");
            File.WriteAllText(Path.Combine(root, "about.html"), "<!DOCTYPE html><html><body></body></html>");
        });

        Assert.True(result.Converged);
        Assert.Equal("staticHtml", result.LegacyProfileId);
        var files = _renderer.Plan(_manifest, result.LegacyProfile);
        Assert.Equal(8, files.Count);
        AssertContractPasses(result.LegacyProfile);
    }

    [Fact]
    public void Combo_PythonFastapi_DetectsAndRenders()
    {
        var result = Detect(root =>
        {
            File.WriteAllText(Path.Combine(root, "requirements.txt"), "fastapi==0.110.0\nuvicorn\n");
        });

        Assert.True(result.Converged);
        Assert.Equal("python", result.LegacyProfileId);
        Assert.Equal("python", result.Confirmed["backend"]);
        var files = _renderer.Plan(_manifest, result.LegacyProfile);
        Assert.Equal(8, files.Count);
        AssertContractPasses(result.LegacyProfile);
    }

    [Fact]
    public void Combo_BareReactSpa_FrontendOnlyRenders()
    {
        // 纯 React SPA：form/frontend 已确认、backend 未确认，候选含纯前端组合，
        // 补答「无后端」后落到 reactOnly，生成物不含 backend-AGENTS.md
        var result = Detect(root =>
            File.WriteAllText(
                Path.Combine(root, "package.json"),
                "{\"dependencies\":{\"react\":\"^18.0.0\"}}"));

        Assert.False(result.Converged);
        Assert.Contains("backend", result.Unconfirmed);
        Assert.Contains(result.Candidates, profile => profile.Id == "reactOnly");

        var engine = new WizardEngine(_manifest);
        var profile = Assert.Single(engine.Resolve(new Dictionary<string, string>
        {
            ["form"] = "web",
            ["backend"] = "none",
            ["frontend"] = "react"
        }), item => item.Id == "reactOnly");
        var files = _renderer.Plan(_manifest, profile, FeatureSelection.Defaults(_manifest, profile));
        Assert.Contains(files, file => file.Output == "frontend-AGENTS.md");
        Assert.Contains(files, file => file.Output == "doc-AGENTS.md");
        Assert.Contains(files, file => file.Output == "infra-AGENTS.md");
        Assert.DoesNotContain(files, file => file.Output == "backend-AGENTS.md");
        AssertContractPasses(profile);
    }

    [Fact]
    public void Combo_VueOnlyAndGeneralFrontend_RenderWithContract()
    {
        // 纯前端家族的另外两个组合：默认能力集渲染与契约校验通过
        var engine = new WizardEngine(_manifest);
        var vueOnly = Assert.Single(
            engine.Resolve(new Dictionary<string, string>
            {
                ["form"] = "web",
                ["backend"] = "none",
                ["frontend"] = "vue"
            }), item => item.Id == "vueOnly");
        var generalFrontend = Assert.Single(
            engine.Resolve(new Dictionary<string, string>
            {
                ["form"] = "web",
                ["backend"] = "none",
                ["frontend"] = "general"
            }), item => item.Id == "generalFrontend");

        foreach (var profile in new[] { vueOnly, generalFrontend })
        {
            var files = _renderer.Plan(_manifest, profile, FeatureSelection.Defaults(_manifest, profile));
            Assert.Contains(files, file => file.Output == "frontend-AGENTS.md");
            Assert.DoesNotContain(files, file => file.Output == "backend-AGENTS.md");
            AssertContractPasses(profile);
        }
    }

    [Fact]
    public void Combo_ReactOnlyAi_RendersWithFrontendAiModule()
    {
        // 纯前端 + AI：ai-AGENTS.md 来自前端 AI 模块，frontend/doc/infra 为骨架追加 AI 补充段，
        // 关闭文档能力后契约仍一致
        var engine = new WizardEngine(_manifest);
        var profile = Assert.Single(engine.Resolve(new Dictionary<string, string>
        {
            ["form"] = "web",
            ["backend"] = "none",
            ["frontend"] = "react",
            ["ai-capability"] = "yes"
        }), item => item.Id == "reactOnly-ai");

        var defaults = FeatureSelection.Defaults(_manifest, profile);
        Assert.Contains("ai-capability", defaults);
        var files = _renderer.Plan(_manifest, profile, defaults);
        Assert.Contains(files, file => file.Output == "ai-AGENTS.md");
        var frontend = Assert.Single(files, file => file.Output == "frontend-AGENTS.md");
        Assert.Contains("AI 交互展示补充", frontend.Content, StringComparison.Ordinal);
        Assert.DoesNotContain(files, file => file.Output == "backend-AGENTS.md");
        AssertContractPasses(profile);

        var features = new HashSet<string>(defaults, StringComparer.OrdinalIgnoreCase);
        features.Remove("doc");
        var trimmed = _renderer.Plan(_manifest, profile, features);
        Assert.DoesNotContain(trimmed, file => file.Output == "doc-AGENTS.md");
        Assert.Contains(trimmed, file => file.Output == "ai-AGENTS.md");
        AssertContractPasses(profile, features);
    }

    [Fact]
    public void Combo_VueOnlyAi_RendersWithContract()
    {
        var engine = new WizardEngine(_manifest);
        var profile = Assert.Single(engine.Resolve(new Dictionary<string, string>
        {
            ["form"] = "web",
            ["backend"] = "none",
            ["frontend"] = "vue",
            ["ai-capability"] = "yes"
        }), item => item.Id == "vueOnly-ai");

        var files = _renderer.Plan(_manifest, profile, FeatureSelection.Defaults(_manifest, profile));
        Assert.Contains(files, file => file.Output == "ai-AGENTS.md");
        Assert.Contains(files, file => file.Output == "frontend-AGENTS.md");
        AssertContractPasses(profile);
    }

    [Fact]
    public void Combo_AllProfiles_DefaultFeatureRenderIsStable()
    {
        // 29 个 Profile 全量样例：默认能力集渲染、契约校验全部通过，
        // 旧 Profile 产物级零漂移由 --emit 基线对比与 Defaults_MatchLegacyPlan 共同保证
        Assert.Equal(33, _manifest.Profiles.Count);
        foreach (var profile in _manifest.Profiles)
        {
            var files = _renderer.Plan(_manifest, profile, FeatureSelection.Defaults(_manifest, profile));
            Assert.Contains(files, file => file.Output == "AGENTS.md");
            AssertContractPasses(profile);
        }
    }
}
