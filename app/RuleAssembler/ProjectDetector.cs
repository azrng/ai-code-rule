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
    private readonly WizardEngine _engine;

    public ProjectDetector(Manifest manifest)
    {
        _manifest = manifest;
        _engine = new WizardEngine(manifest);
    }

    public DetectionResult? Detect(string directory)
    {
        var signals = Scan(directory);
        var evidence = CollectEvidence(signals);
        if (evidence.Count == 0)
            return null;

        var answers = new Dictionary<string, string>(StringComparer.Ordinal);
        var reasons = new List<string>();
        var notes = new List<string>();
        var candidates = _manifest.Profiles;

        foreach (var step in _manifest.Wizard.Steps)
        {
            if (candidates.Count <= 1)
                break;
            if (!WizardEngine.IsApplicable(candidates, step))
                continue;

            var question = _engine.NextQuestion(candidates);
            if (question is null)
                break;

            var optionValues = question.Options.Select(option => option.Value).ToHashSet(StringComparer.Ordinal);
            var chosen = evidence.FirstOrDefault(item => item.Trait == step.Trait && optionValues.Contains(item.Value));
            string value;
            if (chosen is not null)
            {
                value = chosen.Value;
                reasons.Add(chosen.Reason);
            }
            else
            {
                // 本 trait 无直接证据时，看哪个选项能保住与整体证据匹配最多的候选，避免早期提问把正确组合挤出局
                var supported = ChooseByEvidenceSupport(question, candidates, evidence);
                if (supported is not null)
                {
                    value = supported;
                    notes.Add($"依据整体特征组合推断为 {Describe(step.Trait, value)}");
                }
                else if (!string.IsNullOrWhiteSpace(step.DetectDefault) && optionValues.Contains(step.DetectDefault))
                {
                    value = step.DetectDefault;
                    notes.Add($"未发现 {step.Trait} 相关特征，按默认倾向处理：{Describe(step.Trait, value)}");
                }
                else
                {
                    value = question.Options[0].Value;
                    notes.Add($"未发现 {step.Trait} 相关特征，按最常见的 {Describe(step.Trait, value)} 处理");
                }
            }

            answers[step.Trait] = value;
            candidates = _engine.Filter(candidates, step.Trait, value);
        }

        if (candidates.Count != 1)
            return null;

        var profile = candidates[0];
        return new DetectionResult(profile.Id, profile.Label, answers, reasons, notes);
    }

    private string? ChooseByEvidenceSupport(
        WizardQuestion question,
        List<Profile> candidates,
        List<DetectionEvidence> evidence)
    {
        string? bestValue = null;
        var bestScore = 0;
        foreach (var option in question.Options)
        {
            var remaining = _engine.Filter(candidates, option.Trait, option.Value);
            if (remaining.Count == 0)
                continue;
            var score = remaining.Max(profile => ScoreProfile(profile, evidence));
            if (score > bestScore)
            {
                bestScore = score;
                bestValue = option.Value;
            }
        }

        return bestValue;
    }

    // Profile 与证据的匹配数；只统计精确匹配，* 通配不参与打分
    private static int ScoreProfile(Profile profile, List<DetectionEvidence> evidence)
    {
        var score = 0;
        foreach (var item in evidence)
        {
            if (profile.Traits?.Get(item.Trait) == item.Value)
                score++;
        }

        return score;
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

        if (signals.ElectronInPackageJson)
        {
            evidence.Add(new("form", "desktop", "package.json 依赖 electron"));
            evidence.Add(new("shell", "electron", "package.json 依赖 electron（Electron 壳）"));
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
                evidence.Add(new("frontend", "electron", "csproj 引用 ElectronNET.API（Electron 界面）"));
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

        if (signals.HasFastapiDependency)
            evidence.Add(new("backend", "python", "Python 依赖中包含 fastapi"));
        if (signals.HasFastapiDependency || (signals.HasPyproject && !signals.DotNetOrNodePresent()))
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
