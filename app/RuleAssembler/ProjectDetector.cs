using System.Text.Json;

namespace RuleAssembler;

internal sealed record DetectionEvidence(string Trait, string Value, string Reason);

internal sealed record DetectionResult(
    string ProfileId,
    string ProfileLabel,
    Dictionary<string, string> Answers,
    List<string> Reasons,
    List<string> Notes);

// 目标项目技术栈检测：扫描目录中的特征文件（sln/csproj/package.json/wails.json/tauri/pubspec 等），
// 产出 trait 证据，再沿决策树贪心走到底，保证推荐的一定是可生成的 Profile。
// 只做浅层扫描（深度 ≤2，跳过 node_modules/bin/obj 等），双击启动时也要保持瞬时完成。
internal sealed class ProjectDetector
{
    private static readonly string[] SkipDirectories =
    {
        "node_modules", "bin", "obj", ".git", "dist", ".vs", ".idea", "build", ".dart_tool"
    };

    private static readonly string[] StrongMarkers =
    {
        "wails.json", "go.mod", "pubspec.yaml", "pyproject.toml", "requirements.txt", "package.json"
    };

    private readonly Manifest _manifest;

    public ProjectDetector(Manifest manifest)
    {
        _manifest = manifest;
    }

    public DetectionResult? Detect(string directory)
    {
        var signals = Scan(directory);
        var evidence = CollectEvidence(signals);
        if (evidence.Count == 0)
            return null;

        // 全局打分而非逐问贪心：证据强弱不同，逐问过滤会让先问到的弱证据
        // 提前淘汰强证据指向的候选（如 Electron+React+.NET 项目被推成 Tauri）
        Profile? best = null;
        double bestScore = double.NegativeInfinity;
        foreach (var profile in _manifest.Profiles)
        {
            var score = ScoreProfile(profile, evidence);
            if (score > bestScore)
            {
                bestScore = score;
                best = profile;
            }
        }

        if (best is null || bestScore <= 0)
            return null;

        var reasons = evidence
            .Where(item => best.Traits?.Get(item.Trait) == item.Value)
            .Select(item => item.Reason)
            .ToList();
        var notes = evidence
            .Where(item => best.Traits?.Get(item.Trait) != item.Value)
            .Select(item => $"特征 {Describe(item.Trait, item.Value)} 未被推荐组合逐一对应，按整体最接近处理")
            .ToList();

        // 回答组合取 Profile 的全部具体 trait，保证按配方能还原到同一 Profile
        var answers = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var trait in ProfileTraits.KnownTraits)
        {
            var value = best.Traits?.Get(trait);
            if (!string.IsNullOrEmpty(value) && value != ProfileTraits.Wildcard)
                answers[trait] = value;
        }

        return new DetectionResult(best.Id, best.Label, answers, reasons, notes);
    }

    // Profile 与证据的匹配分：匹配加权重、矛盾减权重；稀有证据权重更高（值越少见指向性越强）
    private double ScoreProfile(Profile profile, List<DetectionEvidence> evidence)
    {
        double score = 0;
        foreach (var item in evidence)
        {
            var value = profile.Traits?.Get(item.Trait);
            if (value == null || value == ProfileTraits.Wildcard)
                continue;
            var weight = TraitWeight(item.Trait, item.Value);
            score += value == item.Value ? weight : -weight;
        }

        return score;
    }

    private double TraitWeight(string trait, string value)
    {
        var holders = _manifest.Profiles.Count(p => p.Traits?.Get(trait) == value);
        return holders > 0 ? (double)_manifest.Profiles.Count / holders : 1;
    }

    private string Describe(string trait, string value)
    {
        return _manifest.Wizard.Options.TryGetValue(trait, out var options)
            && options.TryGetValue(value, out var option)
            ? option.Label
            : value;
    }

    private static List<DetectionEvidence> CollectEvidence(ScanSignals signals)
    {
        var evidence = new List<DetectionEvidence>();

        if (signals.HasWailsJson)
        {
            evidence.Add(new("form", "desktop", "发现 wails.json（Wails 桌面混合项目）"));
            evidence.Add(new("shell", "wails", "发现 wails.json（Wails 桌面壳）"));
            if (signals.HasGoMod)
                evidence.Add(new("backend", "go", "发现 go.mod（Go 后端）"));
        }

        if (signals.HasTauriConf)
        {
            evidence.Add(new("form", "desktop", "发现 src-tauri/tauri.conf.json（Tauri 桌面项目）"));
            evidence.Add(new("shell", "tauri", "发现 src-tauri/tauri.conf.json（Tauri 桌面壳）"));
        }

        var electronSignal = signals.ElectronInPackageJson || signals.HasElectronConfig;
        if (electronSignal)
        {
            evidence.Add(new("form", "desktop", "发现 Electron 工程（依赖或 electron.vite / electron-builder 配置）"));
            evidence.Add(new("shell", "electron", "发现 Electron 工程（依赖或 electron.vite / electron-builder 配置）"));
            // Electron 项目伴随 .NET 工程（sidecar / ElectronNET.API 宿主）时，.NET 是后端信号；
            // 普通 Sdk 工程没有其他特征分支，必须在这里兜底，否则会误推断成其他后端
            if (signals.CsprojCount > 0 || signals.HasSln)
                evidence.Add(new("backend", "dotnet", "Electron 项目伴随 .NET 工程（csproj/sln）"));
        }

        if (signals.HasPubspec)
        {
            evidence.Add(new("form", "mobile", "发现 pubspec.yaml（Flutter 项目）"));
            evidence.Add(new("frontend", "flutter", "发现 pubspec.yaml（Flutter 界面技术）"));
        }

        foreach (var content in signals.CsprojContents)
        {
            if (content.Contains("UseMaui", StringComparison.OrdinalIgnoreCase)
                || content.Contains("net8.0-android", StringComparison.OrdinalIgnoreCase)
                || content.Contains("net9.0-android", StringComparison.OrdinalIgnoreCase)
                || content.Contains("net10.0-android", StringComparison.OrdinalIgnoreCase))
            {
                evidence.Add(new("form", "mobile", "csproj 启用 MAUI / 移动目标框架"));
                evidence.Add(new("frontend", "maui", "csproj 启用 MAUI（.NET MAUI 界面）"));
                continue;
            }

            if (content.Contains("UseWPF", StringComparison.OrdinalIgnoreCase))
            {
                evidence.Add(new("form", "desktop", "csproj 启用 UseWPF"));
                evidence.Add(new("frontend", "wpf", "csproj 启用 UseWPF（WPF 界面）"));
                continue;
            }

            if (content.Contains("Avalonia", StringComparison.OrdinalIgnoreCase))
            {
                evidence.Add(new("form", "desktop", "csproj 引用 Avalonia"));
                evidence.Add(new("frontend", "avalonia", "csproj 引用 Avalonia（Avalonia UI）"));
                continue;
            }

            if (content.Contains("ElectronNET.API", StringComparison.OrdinalIgnoreCase))
            {
                evidence.Add(new("form", "desktop", "csproj 引用 ElectronNET.API"));
                evidence.Add(new("backend", "dotnet", "csproj 引用 ElectronNET.API（.NET 宿主）"));
                // ElectronNET.API 本身说明 Electron + .NET 宿主；渲染层框架（react/vue）
                // 交给 package.json 依赖证据判定，这里不再给 frontend 定值
                continue;
            }

            var isBlazor = content.Contains("Microsoft.NET.Sdk.Blazor", StringComparison.OrdinalIgnoreCase)
                || content.Contains("Microsoft.AspNetCore.Components.WebAssembly", StringComparison.OrdinalIgnoreCase);
            if (isBlazor || signals.HasRazorFiles)
            {
                evidence.Add(new("form", "web", isBlazor ? "csproj 使用 Blazor SDK" : "发现 .razor 组件文件"));
                evidence.Add(new("backend", "dotnet", "发现 .NET 工程（csproj）"));
                evidence.Add(new("frontend", "blazor", isBlazor ? "csproj 使用 Blazor SDK" : "发现 .razor 组件文件"));
                if (content.Contains("WebAssembly", StringComparison.OrdinalIgnoreCase))
                    evidence.Add(new("blazor-flavor", "webapp", "csproj 引用 Blazor WebAssembly"));
                // 传统 / Web App 之争证据不足时交给 wizard step 的 detect-default（traditional）
                continue;
            }

            if (content.Contains("Microsoft.NET.Sdk.Web", StringComparison.OrdinalIgnoreCase))
            {
                evidence.Add(new("form", "backend-only", "csproj 使用 Microsoft.NET.Sdk.Web（ASP.NET Core 服务）"));
                evidence.Add(new("backend", "dotnet", "发现 .NET 工程（csproj）"));
                if (signals.CsprojCount >= 3)
                    evidence.Add(new("arch-style", "layered", $"发现 {signals.CsprojCount} 个 csproj，符合多项目分层结构"));
                else
                    evidence.Add(new("arch-style", "single", $"仅发现 {signals.CsprojCount} 个 csproj，按单层结构处理"));
            }
        }

        // fastapi 是强信号（理由更具体）；仅有 pyproject/requirements 时是弱信号，也要给 python 指向，
        // 否则纯 Python 工程会因无后端证据被推到别的栈
        if (signals.HasPyproject || signals.HasRequirementsTxt)
            evidence.Add(new(
                "backend",
                "python",
                signals.HasFastapiDependency ? "Python 依赖中包含 fastapi" : "发现 Python 工程文件（pyproject/requirements）"));
        if ((signals.HasPyproject || signals.HasRequirementsTxt) && !signals.DotNetOrNodePresent())
            evidence.TryAddEvidence(new("form", "backend-only", "发现 Python 后端工程文件（pyproject/requirements）"));

        if (signals.NextInPackageJson)
        {
            evidence.Add(new("form", "web", "package.json 依赖 next（Next.js 全栈）"));
            evidence.Add(new("backend", "node", "package.json 依赖 next（Node 服务端）"));
            evidence.Add(new("frontend", "react", "package.json 依赖 next / react"));
        }
        else if (signals.ReactInPackageJson)
        {
            evidence.Add(new("frontend", "react", "package.json 依赖 react"));
        }
        else if (signals.VueInPackageJson)
        {
            evidence.Add(new("frontend", "vue", "package.json 依赖 vue"));
        }

        if (signals.PackageJsonWithoutBackend)
            evidence.Add(new("form", "web", "发现 package.json 前端工程"));

        if (signals.OnlyStaticHtml)
            evidence.Add(new("form", "static", "根目录只有 HTML 页面，未发现工程文件"));

        // 同一 (trait, value) 可能因多个工程文件重复出现，保留首条理由即可
        return evidence
            .GroupBy(item => (item.Trait, item.Value))
            .Select(group => group.First())
            .ToList();
    }

    private static ScanSignals Scan(string directory)
    {
        var signals = new ScanSignals();
        if (!Directory.Exists(directory))
            return signals;

        var root = Path.GetFullPath(directory);
        foreach (var name in StrongMarkers)
        {
            if (!File.Exists(Path.Combine(root, name)))
                continue;
            switch (name)
            {
                case "wails.json": signals.HasWailsJson = true; break;
                case "go.mod": signals.HasGoMod = true; break;
                case "pubspec.yaml": signals.HasPubspec = true; break;
                case "package.json": signals.HasPackageJson = true; break;
                case "pyproject.toml": signals.HasPyproject = true; break;
                case "requirements.txt": signals.HasRequirementsTxt = true; break;
            }
        }

        signals.HasTauriConf = File.Exists(Path.Combine(root, "src-tauri", "tauri.conf.json"))
            || File.Exists(Path.Combine(root, "tauri.conf.json"));

        foreach (var name in new[]
                 {
                     "electron.vite.config.ts", "electron.vite.config.js", "electron.vite.config.mjs",
                     "electron-builder.yml", "electron-builder.json"
                 })
        {
            if (File.Exists(Path.Combine(root, name)))
            {
                signals.HasElectronConfig = true;
                break;
            }
        }

        ParsePackageJson(root, signals);
        if (!signals.HasPackageJson)
            ParsePackageJson(Path.Combine(root, "frontend"), signals);

        ParsePythonDeps(root, signals);

        foreach (var file in EnumerateShallow(root, new[] { "*.sln", "*.slnx" }, 2))
            signals.HasSln = true;

        foreach (var file in EnumerateShallow(root, new[] { "*.csproj" }, 2))
        {
            signals.CsprojCount++;
            if (signals.CsprojContents.Count < 20)
            {
                try
                {
                    signals.CsprojContents.Add(File.ReadAllText(file));
                }
                catch (IOException)
                {
                    // 读不动的工程文件直接跳过，不影响整体检测
                }
            }
        }

        signals.HasRazorFiles = EnumerateShallow(root, new[] { "*.razor" }, 2).Any();
        signals.HasRootHtml = Directory
            .EnumerateFiles(root, "*.html", SearchOption.TopDirectoryOnly)
            .Any();

        signals.HasRootHtmlOnly = signals.HasRootHtml
            && signals.CsprojCount == 0
            && !signals.HasPackageJson
            && !signals.HasSln;
        return signals;
    }

    private static void ParsePackageJson(string directory, ScanSignals signals)
    {
        var path = Path.Combine(directory, "package.json");
        if (!File.Exists(path))
            return;

        signals.HasPackageJson = true;
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var deps = new List<string>();
            foreach (var section in new[] { "dependencies", "devDependencies" })
            {
                if (doc.RootElement.ValueKind == JsonValueKind.Object
                    && doc.RootElement.TryGetProperty(section, out var items)
                    && items.ValueKind == JsonValueKind.Object)
                {
                    deps.AddRange(items.EnumerateObject().Select(property => property.Name));
                }
            }

            signals.ReactInPackageJson |= deps.Contains("react");
            signals.VueInPackageJson |= deps.Contains("vue");
            signals.NextInPackageJson |= deps.Contains("next");
            signals.ElectronInPackageJson |= deps.Contains("electron");
        }
        catch (JsonException)
        {
            // package.json 解析失败时保留文件存在信号，忽略依赖细节
        }
    }

    private static void ParsePythonDeps(string directory, ScanSignals signals)
    {
        try
        {
            var requirements = Path.Combine(directory, "requirements.txt");
            if (File.Exists(requirements)
                && File.ReadLines(requirements).Any(line =>
                    line.TrimStart().StartsWith("fastapi", StringComparison.OrdinalIgnoreCase)))
            {
                signals.HasFastapiDependency = true;
            }

            var pyproject = Path.Combine(directory, "pyproject.toml");
            if (File.Exists(pyproject)
                && File.ReadAllText(pyproject).Contains("fastapi", StringComparison.OrdinalIgnoreCase))
            {
                signals.HasFastapiDependency = true;
            }
        }
        catch (IOException)
        {
            // 依赖文件读取失败不影响其余信号
        }
    }

    private static IEnumerable<string> EnumerateShallow(string root, string[] patterns, int maxDepth)
    {
        var stack = new Stack<(string Dir, int Depth)>();
        stack.Push((root, 0));
        while (stack.Count > 0)
        {
            var (dir, depth) = stack.Pop();
            foreach (var pattern in patterns)
            {
                string[] files = Array.Empty<string>();
                try
                {
                    files = Directory.GetFiles(dir, pattern, SearchOption.TopDirectoryOnly);
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }

                foreach (var file in files)
                    yield return file;
            }

            if (depth >= maxDepth)
                continue;
            string[] subDirs = Array.Empty<string>();
            try
            {
                subDirs = Directory.GetDirectories(dir);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }

            foreach (var sub in subDirs)
            {
                var name = Path.GetFileName(sub);
                if (SkipDirectories.Contains(name, StringComparer.OrdinalIgnoreCase))
                    continue;
                stack.Push((sub, depth + 1));
            }
        }
    }
}

internal sealed class ScanSignals
{
    public bool HasWailsJson;
    public bool HasGoMod;
    public bool HasTauriConf;
    public bool HasElectronConfig;
    public bool HasPubspec;
    public bool HasPackageJson;
    public bool HasPyproject;
    public bool HasRequirementsTxt;
    public bool HasSln;
    public bool HasRazorFiles;
    public bool HasRootHtml;
    public bool HasRootHtmlOnly;
    public bool ReactInPackageJson;
    public bool VueInPackageJson;
    public bool NextInPackageJson;
    public bool ElectronInPackageJson;
    public bool HasFastapiDependency;
    public int CsprojCount;
    public List<string> CsprojContents { get; } = new();

    public bool DotNetOrNodePresent()
    {
        return CsprojCount > 0 || HasSln || HasPackageJson;
    }

    public bool PackageJsonWithoutBackend
    {
        get { return HasPackageJson && !HasWailsJson && !HasTauriConf && CsprojCount == 0 && !NextInPackageJson; }
    }

    public bool OnlyStaticHtml
    {
        get { return HasRootHtmlOnly && !HasGoMod && !HasPubspec && !HasPyproject; }
    }
}

internal static class DetectionEvidenceExtensions
{
    public static void TryAddEvidence(this List<DetectionEvidence> evidence, DetectionEvidence item)
    {
        if (!evidence.Any(existing => existing.Trait == item.Trait && existing.Value == item.Value))
            evidence.Add(item);
    }
}
