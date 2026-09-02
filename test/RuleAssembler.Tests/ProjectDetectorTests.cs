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
        Assert.Equal("reactWailsExe", result.ProfileId);
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
        Assert.Equal("wpfExe", result.ProfileId);
    }

    [Fact]
    public void Detect_FastapiProject_RecommendsPython()
    {
        File.WriteAllText(Path.Combine(_root, "requirements.txt"), "fastapi==0.110.0\nuvicorn\n");

        var result = new ProjectDetector(_manifest).Detect(_root);

        Assert.NotNull(result);
        Assert.Equal("python", result.ProfileId);
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
        Assert.Equal("reactElectronDotNet", result.ProfileId);
        // 配方回答必须带上渲染层框架，保证按配方可还原到同一变体
        Assert.Equal("react", result.Answers["frontend"]);
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
        Assert.Equal("vueElectronDotNet", result.ProfileId);
        Assert.Equal("vue", result.Answers["frontend"]);
    }

    [Fact]
    public void Detect_ElectronWithoutRendererDeps_DefaultsToReactVariant()
    {
        // Electron + .NET 但识别不出渲染层框架（无 react/vue 依赖）：
        // 整体打分在两个 Electron 变体间同分，按 manifest 顺序稳定落到 React 变体，
        // frontend 维度在 Notes 中提示未逐一对应
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
        Assert.Equal("reactElectronDotNet", result.ProfileId);
        // 配方回答取自推荐 Profile 的具体 trait，React 变体应带 frontend=react 以便还原
        Assert.Equal("react", result.Answers["frontend"]);
    }

    [Fact]
    public void Detect_ElectronWithoutDotnet_StillRecommendsElectronTemplate()
    {
        // 纯 Electron + React（无 .NET 工程）：候选空间里没有 electron-without-dotnet 的模板，
        // 应就近推荐 React Electron 变体，而不是被其他壳（Tauri/Wails）抢走
        File.WriteAllText(
            Path.Combine(_root, "package.json"),
            "{\"dependencies\":{\"react\":\"^18.0.0\"},\"devDependencies\":{\"electron\":\"^33.0.0\"}}");
        File.WriteAllText(Path.Combine(_root, "electron.vite.config.ts"), "export default {}");

        var result = new ProjectDetector(_manifest).Detect(_root);

        Assert.NotNull(result);
        Assert.Equal("reactElectronDotNet", result.ProfileId);
    }

    [Fact]
    public void Detect_EmptyDirectory_ReturnsNull()
    {
        var result = new ProjectDetector(_manifest).Detect(_root);

        Assert.Null(result);
    }
}
