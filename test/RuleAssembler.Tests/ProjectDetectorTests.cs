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
    public void Detect_WebReactLayeredBackend_RecommendsReactLinkdotNet()
    {
        File.WriteAllText(
            Path.Combine(_root, "package.json"),
            "{\"dependencies\":{\"react\":\"^18.0.0\"}}");
        // csproj 须落在检测扫描深度（≤2）以内
        Directory.CreateDirectory(Path.Combine(_root, "backend", "src"));
        foreach (var name in new[] { "Api", "Application", "Infrastructure" })
        {
            File.WriteAllText(
                Path.Combine(_root, "backend", "src", name + ".csproj"),
                "<Project Sdk=\"Microsoft.NET.Sdk.Web\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");
        }

        var result = new ProjectDetector(_manifest).Detect(_root);

        Assert.NotNull(result);
        // 3 个 csproj 的 Web 工程按分层收敛；arch-style 证据在检测阶段即确认
        Assert.Equal("reactLinkdotNet", result!.LegacyProfileId);
        Assert.Equal("layered", result.BuildLegacyAnswers()["arch-style"]);
    }

    [Fact]
    public void Detect_WebReactSingleBackend_ConfirmsArchStyleSingle()
    {
        File.WriteAllText(
            Path.Combine(_root, "package.json"),
            "{\"dependencies\":{\"react\":\"^18.0.0\"}}");
        Directory.CreateDirectory(Path.Combine(_root, "backend", "src"));
        File.WriteAllText(
            Path.Combine(_root, "backend", "src", "Demo.csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk.Web\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");

        var result = new ProjectDetector(_manifest).Detect(_root);

        Assert.NotNull(result);
        // 单项目后端：arch-style=single 证据确认，候选含单项目变体（AI 与否留待补答）
        Assert.Equal("single", result!.Confirmed["arch-style"]);
        Assert.Contains(result.Candidates, profile => profile.Id == "reactLinkdotNetSingle");
    }

    [Fact]
    public void Detect_ReactSpaWithFastapi_RecommendsReactLinkPython()
    {
        File.WriteAllText(
            Path.Combine(_root, "package.json"),
            "{\"dependencies\":{\"react\":\"^18.0.0\"}}");
        File.WriteAllText(
            Path.Combine(_root, "pyproject.toml"),
            "[project]\ndependencies = [\"fastapi\", \"uvicorn\"]\n");

        var result = new ProjectDetector(_manifest).Detect(_root);

        Assert.NotNull(result);
        // 前端工程 + Python 后端工程：web+python 组合此前无法收敛，现直接命中分离组合
        Assert.Equal("reactLinkPython", result!.LegacyProfileId);
        Assert.Equal("python", result.BuildLegacyAnswers()["backend"]);
    }

    [Fact]
    public void Detect_NuxtProject_RecommendsNuxtFullstack()
    {
        File.WriteAllText(
            Path.Combine(_root, "package.json"),
            "{\"dependencies\":{\"nuxt\":\"^3.13.0\"}}");
        File.WriteAllText(Path.Combine(_root, "nuxt.config.ts"), "export default defineNuxtConfig({})");

        var result = new ProjectDetector(_manifest).Detect(_root);

        Assert.NotNull(result);
        // Nuxt 工程：nuxt.config / 依赖直接给出 web + vue + node 证据，收敛到 Nuxt 全栈
        Assert.Equal("nuxtFullstack", result!.LegacyProfileId);
        Assert.Equal("node", result.BuildLegacyAnswers()["backend"]);
    }

    [Fact]
    public void Detect_TauriReactProject_RecommendsReactTauriExe()
    {
        Directory.CreateDirectory(Path.Combine(_root, "src-tauri"));
        File.WriteAllText(Path.Combine(_root, "src-tauri", "tauri.conf.json"), "{\"productName\":\"demo\"}");
        File.WriteAllText(
            Path.Combine(_root, "package.json"),
            "{\"dependencies\":{\"react\":\"^18.0.0\"}}");

        var result = new ProjectDetector(_manifest).Detect(_root);

        Assert.NotNull(result);
        Assert.Equal("reactTauriExe", result!.LegacyProfileId);
        // Tauri 壳即 Rust 后端证据，回答组合必须带 backend=rust 才能重放回同一组合
        Assert.Equal("rust", result.BuildLegacyAnswers()["backend"]);
    }

    [Fact]
    public void Detect_TauriVueProject_RecommendsVueTauri()
    {
        Directory.CreateDirectory(Path.Combine(_root, "src-tauri"));
        File.WriteAllText(Path.Combine(_root, "src-tauri", "tauri.conf.json"), "{\"productName\":\"demo\"}");
        File.WriteAllText(
            Path.Combine(_root, "package.json"),
            "{\"dependencies\":{\"vue\":\"^3.4.0\"}}");

        var result = new ProjectDetector(_manifest).Detect(_root);

        Assert.NotNull(result);
        Assert.Equal("vueTauri", result!.LegacyProfileId);
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
        Assert.Equal(7, result.Candidates.Count);
        Assert.Contains(result.Candidates, profile => profile.Id == "reactOnly");
        Assert.Contains(result.Candidates, profile => profile.Id == "reactOnly-ai");
        Assert.Contains(result.Candidates, profile => profile.Id == "reactLinkdotNet");
        Assert.Contains(result.Candidates, profile => profile.Id == "reactLinkdotNetSingle");
        Assert.Contains(result.Candidates, profile => profile.Id == "reactLinkPython");
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
    public void Detect_AvaloniaAppWithWebTestApi_RecommendsAvaloniaExe()
    {
        // ApixPress 真实布局：src 下 Avalonia 主工程 + Core，tools 下 Sdk.Web 测试 API。
        // 测试 API 属辅助工程，不得用 form=backend-only 与桌面形态打擂台，
        // 否则候选只剩通配的通用 Profile，推荐会收敛到「通用项目」
        File.WriteAllText(Path.Combine(_root, "ApixPress.slnx"), "");
        Directory.CreateDirectory(Path.Combine(_root, "src", "ApixPress.App"));
        Directory.CreateDirectory(Path.Combine(_root, "src", "ApixPress.Core"));
        Directory.CreateDirectory(Path.Combine(_root, "tools", "ApixPress.TestApi"));
        File.WriteAllText(
            Path.Combine(_root, "src", "ApixPress.App", "ApixPress.App.csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><OutputType>WinExe</OutputType></PropertyGroup><ItemGroup><PackageReference Include=\"Avalonia\" Version=\"12.1.2\" /></ItemGroup></Project>");
        File.WriteAllText(
            Path.Combine(_root, "src", "ApixPress.Core", "ApixPress.Core.csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
        File.WriteAllText(
            Path.Combine(_root, "tools", "ApixPress.TestApi", "ApixPress.TestApi.csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk.Web\"></Project>");

        var result = new ProjectDetector(_manifest).Detect(_root);

        Assert.NotNull(result);
        Assert.True(result!.Converged);
        Assert.Equal("avaloniaExe", result.LegacyProfileId);
        Assert.Equal("desktop", result.Confirmed["form"]);
        Assert.Equal("avalonia", result.Confirmed["frontend"]);
        Assert.DoesNotContain(result.Notes, note => note.Contains("证据冲突"));
    }

    [Fact]
    public void Detect_WpfAppWithWebTestApi_RecommendsWpfExe()
    {
        // 与 Avalonia 场景对称：WPF 方案携带 Sdk.Web 测试 API 时同样收敛到桌面 Profile
        Directory.CreateDirectory(Path.Combine(_root, "src"));
        Directory.CreateDirectory(Path.Combine(_root, "tools"));
        File.WriteAllText(
            Path.Combine(_root, "src", "Demo.App.csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><UseWPF>true</UseWPF></PropertyGroup></Project>");
        File.WriteAllText(
            Path.Combine(_root, "tools", "Demo.TestApi.csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk.Web\"></Project>");

        var result = new ProjectDetector(_manifest).Detect(_root);

        Assert.NotNull(result);
        Assert.True(result!.Converged);
        Assert.Equal("wpfExe", result.LegacyProfileId);
        Assert.DoesNotContain(result.Notes, note => note.Contains("证据冲突"));
    }

    [Fact]
    public void Detect_EmptyDirectory_ReturnsNull()
    {
        var result = new ProjectDetector(_manifest).Detect(_root);

        Assert.Null(result);
    }
}
