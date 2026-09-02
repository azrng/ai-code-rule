namespace RuleAssembler.Tests;

// P1-4 兼容回归：必覆盖组合的「检测 → 能力开关 → 渲染 → 契约校验」全链路样例。
// 覆盖：.NET + React、.NET 分层、React + Wails、React + Electron + .NET、纯静态 HTML、纯后端 Python
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

        // Web 工程 + 前端依赖 → form=web；单层 .NET + React 同时匹配 AI 与非 AI 两个 Profile，
        // ai 维度无证据 → 部分结果交由补答，不得隐式二选一
        Assert.False(result.Converged);
        Assert.Null(result.LegacyProfileId);
        Assert.Contains("ai-capability", result.Unconfirmed);
        Assert.Contains(result.Candidates, profile => profile.Id == "reactLinkdotNet");
        Assert.Contains(result.Candidates, profile => profile.Id == "reactLinkdotNet-ai");
        var profile = Assert.Single(result.Candidates, item => item.Id == "reactLinkdotNet");
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
        Assert.Equal(7, files.Count);
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
        Assert.Equal(9, files.Count);
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
        Assert.Equal(7, files.Count);
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
        Assert.Equal(7, files.Count);
        AssertContractPasses(result.LegacyProfile);
    }

    [Fact]
    public void Combo_AllProfiles_DefaultFeatureRenderIsStable()
    {
        // 21 个旧 Profile 继续作为兼容样例：默认能力集渲染、契约校验全部通过，
        // 产物级零漂移由 --emit 基线对比与 Defaults_MatchLegacyPlan 共同保证
        Assert.Equal(21, _manifest.Profiles.Count);
        foreach (var profile in _manifest.Profiles)
        {
            var files = _renderer.Plan(_manifest, profile, FeatureSelection.Defaults(_manifest, profile));
            Assert.Contains(files, file => file.Output == "AGENTS.md");
            AssertContractPasses(profile);
        }
    }
}
