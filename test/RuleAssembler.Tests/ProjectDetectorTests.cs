namespace RuleAssembler.Tests;

public sealed class ProjectDetectorTests : IDisposable
{
    private readonly string _root;
    private readonly Manifest _manifest;

    public ProjectDetectorTests()
    {
        _manifest = new ManifestLoader(new ResourceStore()).Load();
        _root = Path.Combine(Path.GetTempPath(), $"rule-detector-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void Detect_WailsProject_RecommendsReactWailsExe()
    {
        File.WriteAllText(Path.Combine(_root, "wails.json"), "{\"name\":\"demo\"}");
        File.WriteAllText(Path.Combine(_root, "go.mod"), "module demo\n\ngo 1.22\n");
        File.WriteAllText(
            Path.Combine(_root, "package.json"),
            "{\"dependencies\":{\"react\":\"^18.0.0\"}}");

        var result = new ProjectDetector(_manifest).Detect(_root);

        Assert.NotNull(result);
        Assert.Equal("reactWailsExe", result!.LegacyProfileId);
        Assert.Contains(result.Reasons, reason => reason.Contains("wails.json"));
    }

    [Fact]
    public void Detect_WpfProject_RecommendsWpfExe()
    {
        File.WriteAllText(
            Path.Combine(_root, "Demo.csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><OutputType>WinExe</OutputType><UseWPF>true</UseWPF></PropertyGroup></Project>");

        var result = new ProjectDetector(_manifest).Detect(_root);

        Assert.NotNull(result);
        Assert.Equal("wpfExe", result!.LegacyProfileId);
    }

    [Fact]
    public void Detect_FastapiProject_RecommendsPython()
    {
        File.WriteAllText(Path.Combine(_root, "requirements.txt"), "fastapi==0.110.0\nuvicorn\n");

        var result = new ProjectDetector(_manifest).Detect(_root);

        Assert.NotNull(result);
        Assert.Equal("python", result!.LegacyProfileId);
    }

    [Fact]
    public void Detect_ElectronReactNetSidecar_RecommendsReactElectronDotNet()
    {
        // SmartVault 真实布局：Electron + React 前端 + 普通 Sdk 的 .NET sidecar 工程
        File.WriteAllText(
            Path.Combine(_root, "package.json"),
            "{\"dependencies\":{\"react\":\"^18.0.0\"},\"devDependencies\":{\"electron\":\"^33.0.0\"}}");
        File.WriteAllText(Path.Combine(_root, "electron.vite.config.ts"), "export default {}");
        File.WriteAllText(Path.Combine(_root, "SmartVault.slnx"), "");
        Directory.CreateDirectory(Path.Combine(_root, "src-dotnet"));
        File.WriteAllText(
            Path.Combine(_root, "src-dotnet", "SmartVault.Sidecar.csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");

        var result = new ProjectDetector(_manifest).Detect(_root);

        Assert.NotNull(result);
        Assert.Equal("reactElectronDotNet", result!.LegacyProfileId);
        // 配方回答必须带上渲染层框架，保证按配方可还原到同一变体
        Assert.Equal("react", result.BuildLegacyAnswers()["frontend"]);
    }

    [Fact]
    public void Detect_ElectronVueNetSidecar_RecommendsVueElectronDotNet()
    {
        // Electron + Vue 渲染层 + .NET sidecar：frontend 证据来自 package.json 的 vue 依赖
        File.WriteAllText(
            Path.Combine(_root, "package.json"),
            "{\"dependencies\":{\"vue\":\"^3.5.0\"},\"devDependencies\":{\"electron\":\"^33.0.0\"}}");
        File.WriteAllText(Path.Combine(_root, "electron.vite.config.ts"), "export default {}");
        File.WriteAllText(Path.Combine(_root, "SmartVault.slnx"), "");
        Directory.CreateDirectory(Path.Combine(_root, "src-dotnet"));
        File.WriteAllText(
            Path.Combine(_root, "src-dotnet", "SmartVault.Sidecar.csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");

        var result = new ProjectDetector(_manifest).Detect(_root);

        Assert.NotNull(result);
        Assert.Equal("vueElectronDotNet", result!.LegacyProfileId);
        Assert.Equal("vue", result.BuildLegacyAnswers()["frontend"]);
    }

    [Fact]
    public void Detect_ElectronNetWithoutRenderer_FrontendStaysUnconfirmed()
    {
        // Electron + .NET 但识别不出渲染层框架（无 react/vue 依赖）：
        // 证据只确认 form/shell/backend，frontend 必须保持未确认，
        // 输出两个 Electron 变体候选，不得默认 React
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
        Assert.Null(result.LegacyProfileId);
        Assert.Contains("frontend", result.Unconfirmed);
        Assert.Equal("desktop", result.Confirmed["form"]);
        Assert.Equal("electron", result.Confirmed["shell"]);
        Assert.Equal("dotnet", result.Confirmed["backend"]);
        Assert.Equal(2, result.Candidates.Count);
        Assert.Contains(result.Candidates, profile => profile.Id == "reactElectronDotNet");
        Assert.Contains(result.Candidates, profile => profile.Id == "vueElectronDotNet");
    }

    [Fact]
    public void Detect_BareReactSpa_BackendStaysUnconfirmed()
    {
        // 只有 package.json react 依赖：form/frontend 已确认，
        // 后端证据不足时不得隐式补出 .NET / Node 后端，输出全部候选
        File.WriteAllText(
            Path.Combine(_root, "package.json"),
            "{\"dependencies\":{\"react\":\"^18.0.0\"}}");

        var result = new ProjectDetector(_manifest).Detect(_root);

        Assert.NotNull(result);
        Assert.False(result!.Converged);
        Assert.Contains("backend", result.Unconfirmed);
        Assert.Equal("web", result.Confirmed["form"]);
        Assert.Equal("react", result.Confirmed["frontend"]);
        // 候选包含纯前端组合（含 AI 变体）后，「无后端」才能作为补答选项出现
        Assert.Equal(5, result.Candidates.Count);
        Assert.Contains(result.Candidates, profile => profile.Id == "reactOnly");
        Assert.Contains(result.Candidates, profile => profile.Id == "reactOnly-ai");
        Assert.Contains(result.Candidates, profile => profile.Id == "reactLinkdotNet");
        Assert.Contains(result.Candidates, profile => profile.Id == "reactLinkdotNet-ai");
        Assert.Contains(result.Candidates, profile => profile.Id == "nextFullstack");
    }

    [Fact]
    public void Detect_ElectronReactWithoutDotnet_RecommendsReactElectron()
    {
        // 纯 Electron + React（无 .NET 工程）：backend=none 证据来自「未伴随 .NET 工程」，
        // 直接收敛到 reactElectron，不再就近推荐 Electron + .NET 变体
        File.WriteAllText(
            Path.Combine(_root, "package.json"),
            "{\"dependencies\":{\"react\":\"^18.0.0\"},\"devDependencies\":{\"electron\":\"^33.0.0\"}}");
        File.WriteAllText(Path.Combine(_root, "electron.vite.config.ts"), "export default {}");

        var result = new ProjectDetector(_manifest).Detect(_root);

        Assert.NotNull(result);
        Assert.True(result!.Converged);
        Assert.Equal("reactElectron", result.LegacyProfileId);
        Assert.Equal("none", result.Confirmed["backend"]);
        Assert.Contains(result.Reasons, reason => reason.Contains("主进程"));
    }

    [Fact]
    public void Detect_ElectronVueWithoutDotnet_RecommendsVueElectron()
    {
        // 纯 Electron + Vue（无 .NET 工程）：与 React 变体对称
        File.WriteAllText(
            Path.Combine(_root, "package.json"),
            "{\"dependencies\":{\"vue\":\"^3.5.0\"},\"devDependencies\":{\"electron\":\"^33.0.0\"}}");
        File.WriteAllText(Path.Combine(_root, "electron-builder.yml"), "appId: demo");

        var result = new ProjectDetector(_manifest).Detect(_root);

        Assert.NotNull(result);
        Assert.True(result!.Converged);
        Assert.Equal("vueElectron", result.LegacyProfileId);
        Assert.Equal("none", result.Confirmed["backend"]);
        Assert.Equal("vue", result.BuildLegacyAnswers()["frontend"]);
    }

    [Fact]
    public void Detect_ElectronWithoutRenderer_FrontendStaysUnconfirmedAmongPureVariants()
    {
        // 纯 Electron 但识别不出渲染层框架（无 react/vue 依赖）：
        // frontend 必须保持未确认，输出两个纯 Electron 变体候选，不得默认 React
        File.WriteAllText(
            Path.Combine(_root, "package.json"),
            "{\"devDependencies\":{\"electron\":\"^33.0.0\"}}");
        File.WriteAllText(Path.Combine(_root, "electron.vite.config.ts"), "export default {}");

        var result = new ProjectDetector(_manifest).Detect(_root);

        Assert.NotNull(result);
        Assert.False(result!.Converged);
        Assert.Null(result.LegacyProfileId);
        Assert.Contains("frontend", result.Unconfirmed);
        Assert.Equal("desktop", result.Confirmed["form"]);
        Assert.Equal("electron", result.Confirmed["shell"]);
        Assert.Equal("none", result.Confirmed["backend"]);
        Assert.Equal(2, result.Candidates.Count);
        Assert.Contains(result.Candidates, profile => profile.Id == "reactElectron");
        Assert.Contains(result.Candidates, profile => profile.Id == "vueElectron");
    }

    [Fact]
    public void Detect_EmptyDirectory_ReturnsNull()
    {
        var result = new ProjectDetector(_manifest).Detect(_root);

        Assert.Null(result);
    }
}
