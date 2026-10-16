using System.Text.Json;

namespace RuleAssembler;

// 目标项目技术栈检测：扫描目录中的特征文件（sln/csproj/package.json/wails.json/tauri/pubspec 等），
// 产出维度证据（DetectionModels），再按兼容样例收敛候选（组件口径：候选来源是 compatibilityCases
// 而非 Profile traits，样例未携带的维度视为通配），保证推荐的一定是可生成的组合。
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

        // 证据按维度归并：同一维度出现多个不同值视为证据冲突，该维度不确认，留待人工补答；
        // 证据不足的维度不做默认推断（如 Electron + .NET 无 React/Vue 依赖时 frontend 保持未确认）
        var confirmed = new Dictionary<string, string>(StringComparer.Ordinal);
        var notes = new List<string>();
        foreach (var group in evidence.GroupBy(item => item.Trait, StringComparer.Ordinal))
        {
            var values = group.Select(item => item.Value).Distinct(StringComparer.Ordinal).ToList();
            if (values.Count == 1)
            {
                confirmed[group.Key] = values[0];
            }
            else
            {
                var descriptions = values.Select(value => Describe(group.Key, value));
                notes.Add($"维度 {DescribeTraitKey(group.Key)} 证据冲突（{string.Join(" / ", descriptions)}），交由人工确认");
            }
        }

        // 候选 = 与已确认维度相容的兼容样例（组件口径事实来源）：
        // 样例携带的维度须与证据相等，样例未携带的维度视为通配；Profile traits 不再参与检测收敛
        var matchedCases = MatchCases(confirmed);
        if (matchedCases.Count == 0)
        {
            // 已确认组合在样例空间没有精确匹配（跨族组合尚未接入）：按证据整体打分取最接近样例
            CompatibilityCase? best = null;
            double bestScore = double.NegativeInfinity;
            foreach (var item in _manifest.CompatibilityCases)
            {
                var score = ScoreCase(item, evidence);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = item;
                }
            }

            if (best is null || bestScore <= 0)
                return null;
            matchedCases = new List<CompatibilityCase> { best };
            notes.Add("已确认组合没有完全匹配的兼容样例，按整体最接近处理");
        }

        // 候选按 manifest 样例声明顺序展示；标签来自样例携带的展示元数据
        var candidates = matchedCases
            .Select(item => new DetectionCandidate(
                new Selection { Dimensions = new Dictionary<string, string>(item.Selection, StringComparer.Ordinal) },
                item.Profile,
                string.IsNullOrWhiteSpace(item.Label) ? DescribeSelection(item.Selection) : item.Label))
            .ToList();

        var reasons = evidence
            .Where(item => confirmed.TryGetValue(item.Trait, out var value) && value == item.Value)
            .Select(item => item.Reason)
            .ToList();

        // 未确认维度：按问答顺序取「证据未覆盖且候选样例之间仍有分歧」的维度；
        // 样例未携带的维度不参与分歧统计（等价于旧口径的通配 trait），
        // 候选之间无分歧的维度由唯一候选补全，不需要人工回答
        var unconfirmed = new List<string>();
        foreach (var step in _manifest.Wizard.Steps)
        {
            if (confirmed.ContainsKey(step.Trait))
                continue;
            var distinctValues = candidates
                .Select(candidate => candidate.Selection.Dimensions.TryGetValue(step.Trait, out var value) ? value : null)
                .Where(value => value is not null)
                .Distinct(StringComparer.Ordinal)
                .Count();
            if (distinctValues > 1)
                unconfirmed.Add(step.Trait);
        }

        return new DetectionResult(confirmed, unconfirmed, candidates, reasons, notes);
    }

    // 兼容样例匹配：样例维度与已确认证据在共有维度上全部相等即候选；
    // 样例未携带的维度视为通配（与旧 Profile traits 的 * 语义一致）
    private List<CompatibilityCase> MatchCases(Dictionary<string, string> confirmed)
    {
        return _manifest.CompatibilityCases
            .Where(item => confirmed.All(pair =>
                !item.Selection.TryGetValue(pair.Key, out var value) || value == pair.Value))
            .ToList();
    }

    // 样例与证据的匹配分：匹配加权重、矛盾减权重；稀有证据权重更高（值越少见指向性越强）。
    // 样例未携带的维度视为通配，不计分
    private double ScoreCase(CompatibilityCase item, List<DetectionEvidence> evidence)
    {
        double score = 0;
        foreach (var evidenceItem in evidence)
        {
            if (!item.Selection.TryGetValue(evidenceItem.Trait, out var value))
                continue;
            var weight = CaseWeight(evidenceItem.Trait, evidenceItem.Value);
            score += value == evidenceItem.Value ? weight : -weight;
        }

        return score;
    }

    private double CaseWeight(string trait, string value)
    {
        var holders = _manifest.CompatibilityCases.Count(item =>
            item.Selection.TryGetValue(trait, out var caseValue) && caseValue == value);
        return holders > 0 ? (double)_manifest.CompatibilityCases.Count / holders : 1;
    }

    // 无 Profile 标签可用时的候选描述兜底：按维度=取值列出样例选择
    private static string DescribeSelection(Dictionary<string, string> selection)
    {
        return "组合（" + string.Join("、", selection.Select(pair => $"{pair.Key}={pair.Value}")) + "）";
    }

    private string Describe(string trait, string value)
    {
        return _manifest.Wizard.Options.TryGetValue(trait, out var options)
            && options.TryGetValue(value, out var option)
            ? option.Label
            : value;
    }

    // 维度的展示名：优先取问答步骤的问题文案，找不到时退回 trait 键
    private string DescribeTraitKey(string trait)
    {
        return _manifest.Wizard.Steps.FirstOrDefault(step => step.Trait == trait)?.Question ?? trait;
    }

    // 单个 .NET 工程的类别判定，分支优先级与证据发放顺序一致；
    // 仅 Blazor 受全局 .razor 文件信号影响（工程本身无 SDK 标记时也按 Blazor 处理）
    private enum DotnetProjectKind
    {
        Plain,
        MauiMobile,
        WpfDesktop,
        AvaloniaDesktop,
        ElectronNetHost,
        Blazor,
        WebService
    }

    private static DotnetProjectKind ClassifyDotnetProject(string content, bool hasRazorFiles)
    {
        if (content.Contains("UseMaui", StringComparison.OrdinalIgnoreCase)
            || content.Contains("net8.0-android", StringComparison.OrdinalIgnoreCase)
            || content.Contains("net9.0-android", StringComparison.OrdinalIgnoreCase)
            || content.Contains("net10.0-android", StringComparison.OrdinalIgnoreCase))
        {
            return DotnetProjectKind.MauiMobile;
        }

        if (content.Contains("UseWPF", StringComparison.OrdinalIgnoreCase))
            return DotnetProjectKind.WpfDesktop;

        if (content.Contains("Avalonia", StringComparison.OrdinalIgnoreCase))
            return DotnetProjectKind.AvaloniaDesktop;

        if (content.Contains("ElectronNET.API", StringComparison.OrdinalIgnoreCase))
            return DotnetProjectKind.ElectronNetHost;

        if (content.Contains("Microsoft.NET.Sdk.Blazor", StringComparison.OrdinalIgnoreCase)
            || content.Contains("Microsoft.AspNetCore.Components.WebAssembly", StringComparison.OrdinalIgnoreCase)
            || hasRazorFiles)
        {
            return DotnetProjectKind.Blazor;
        }

        if (content.Contains("Microsoft.NET.Sdk.Web", StringComparison.OrdinalIgnoreCase))
            return DotnetProjectKind.WebService;

        return DotnetProjectKind.Plain;
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
            evidence.Add(new("backend", "rust", "发现 src-tauri/tauri.conf.json（Rust 后端）"));
        }

        var electronSignal = signals.ElectronInPackageJson || signals.HasElectronConfig;
        if (electronSignal)
        {
            evidence.Add(new("form", "desktop", "发现 Electron 工程（依赖或 electron.vite / electron-builder 配置）"));
            evidence.Add(new("shell", "electron", "发现 Electron 工程（依赖或 electron.vite / electron-builder 配置）"));
            // Electron 项目伴随 .NET 工程（sidecar / ElectronNET.API 宿主）时，.NET 是后端信号；
            // 普通 Sdk 工程没有其他特征分支，必须在这里兜底，否则会误推断成其他后端。
            // 无任何 .NET 工程时，本地逻辑由主进程（Node）承担，属于确定证据而非默认补值
            if (signals.CsprojCount > 0 || signals.HasSln)
                evidence.Add(new("backend", "dotnet", "Electron 项目伴随 .NET 工程（csproj/sln）"));
            else
                evidence.Add(new("backend", "none", "Electron 工程未伴随 .NET 工程，本地逻辑由主进程（Node）承担"));
        }

        if (signals.HasPubspec)
        {
            evidence.Add(new("form", "mobile", "发现 pubspec.yaml（Flutter 项目）"));
            evidence.Add(new("frontend", "flutter", "发现 pubspec.yaml（Flutter 界面技术）"));
        }

        // 先把每个工程归到唯一类别（分支优先级与旧 continue 链一致），再统一发证据。
        // MAUI / WPF / Avalonia / ElectronNET / Blazor 是能独立定义方案形态的 UI 工程；
        // 方案形态已被 UI 工程或桌面壳（Wails / Tauri / Electron）定义时，
        // Microsoft.NET.Sdk.Web 工程视为辅助工程（tests/tools 下的测试 API、Mock 服务等），
        // 不再声明 form / backend / arch-style 这类方案级证据——
        // 否则桌面方案携带测试 API 时形态证据互相冲突，候选只剩通配的通用 Profile 兜底
        var kinds = signals.CsprojContents
            .Select(content => ClassifyDotnetProject(content, signals.HasRazorFiles))
            .ToList();
        var uiFormDefined = kinds.Any(kind => kind is DotnetProjectKind.MauiMobile
            or DotnetProjectKind.WpfDesktop
            or DotnetProjectKind.AvaloniaDesktop
            or DotnetProjectKind.ElectronNetHost
            or DotnetProjectKind.Blazor)
            || signals.HasWailsJson
            || signals.HasTauriConf
            || electronSignal;

        foreach (var (content, kind) in signals.CsprojContents.Zip(kinds))
        {
            switch (kind)
            {
                case DotnetProjectKind.MauiMobile:
                    evidence.Add(new("form", "mobile", "csproj 启用 MAUI / 移动目标框架"));
                    evidence.Add(new("frontend", "maui", "csproj 启用 MAUI（.NET MAUI 界面）"));
                    continue;
                case DotnetProjectKind.WpfDesktop:
                    evidence.Add(new("form", "desktop", "csproj 启用 UseWPF"));
                    evidence.Add(new("frontend", "wpf", "csproj 启用 UseWPF（WPF 界面）"));
                    continue;
                case DotnetProjectKind.AvaloniaDesktop:
                    evidence.Add(new("form", "desktop", "csproj 引用 Avalonia"));
                    evidence.Add(new("frontend", "avalonia", "csproj 引用 Avalonia（Avalonia UI）"));
                    continue;
                case DotnetProjectKind.ElectronNetHost:
                    evidence.Add(new("form", "desktop", "csproj 引用 ElectronNET.API"));
                    evidence.Add(new("backend", "dotnet", "csproj 引用 ElectronNET.API（.NET 宿主）"));
                    // ElectronNET.API 本身说明 Electron + .NET 宿主；渲染层框架（react/vue）
                    // 交给 package.json 依赖证据判定，这里不再给 frontend 定值
                    continue;
                case DotnetProjectKind.Blazor:
                    var isBlazorSource = content.Contains("Microsoft.NET.Sdk.Blazor", StringComparison.OrdinalIgnoreCase)
                        || content.Contains("Microsoft.AspNetCore.Components.WebAssembly", StringComparison.OrdinalIgnoreCase);
                    evidence.Add(new("form", "web", isBlazorSource ? "csproj 使用 Blazor SDK" : "发现 .razor 组件文件"));
                    evidence.Add(new("backend", "dotnet", "发现 .NET 工程（csproj）"));
                    evidence.Add(new("frontend", "blazor", isBlazorSource ? "csproj 使用 Blazor SDK" : "发现 .razor 组件文件"));
                    if (content.Contains("WebAssembly", StringComparison.OrdinalIgnoreCase))
                        evidence.Add(new("blazor-flavor", "webapp", "csproj 引用 Blazor WebAssembly"));
                    // Components/ 目录下存在 .razor 文件是 .NET 8+ Blazor Web App 模板强特征，
                    // 与传统 Server 的 Pages 结构区分；证据不足时仍交给补答确认
                    if (signals.RazorUnderComponents)
                        evidence.Add(new("blazor-flavor", "webapp", "Components/ 目录下发现 .razor 组件（Blazor Web App 结构）"));
                    // 传统 / Web App 之争证据不足时交给补答确认（detect-default 已随旧引擎退役）
                    continue;
                case DotnetProjectKind.WebService:
                    if (uiFormDefined)
                        continue;
                    // Web 工程伴随前端框架依赖时按前后端分离处理，避免与 frontend 证据组合出无匹配候选
                    var hasJsFrontend = signals.ReactInPackageJson || signals.VueInPackageJson || signals.NextInPackageJson;
                    evidence.Add(new(
                        "form",
                        hasJsFrontend ? "web" : "backend-only",
                        hasJsFrontend
                            ? "Web 工程伴随前端框架依赖（前后端分离）"
                            : "csproj 使用 Microsoft.NET.Sdk.Web（ASP.NET Core 服务）"));
                    evidence.Add(new("backend", "dotnet", "发现 .NET 工程（csproj）"));
                    if (signals.CsprojCount >= 3)
                        evidence.Add(new("arch-style", "layered", $"发现 {signals.CsprojCount} 个 csproj，符合多项目分层结构"));
                    else
                        evidence.Add(new("arch-style", "single", $"仅发现 {signals.CsprojCount} 个 csproj，按单层结构处理"));
                    continue;
                default:
                    continue;
            }
        }

        // fastapi 是强信号（理由更具体）；仅有 pyproject/requirements 时是弱信号，也要给 python 指向，
        // 否则纯 Python 工程会因无后端证据被推到别的栈
        if (signals.HasPyproject || signals.HasRequirementsTxt)
            evidence.Add(new(
                "backend",
                "python",
                signals.HasFastapiDependency ? "Python 依赖中包含 fastapi" : "发现 Python 工程文件（pyproject/requirements）"));
        if ((signals.HasPyproject || signals.HasRequirementsTxt) && !signals.DotNetOrNodePresent()
            && !evidence.Any(item => item.Trait == "form" && item.Value == "backend-only"))
            evidence.Add(new("form", "backend-only", "发现 Python 后端工程文件（pyproject/requirements）"));

        if (signals.NextInPackageJson)
        {
            evidence.Add(new("form", "web", "package.json 依赖 next（Next.js 全栈）"));
            evidence.Add(new("backend", "node", "package.json 依赖 next（Node 服务端）"));
            evidence.Add(new("frontend", "react", "package.json 依赖 next / react"));
        }
        else if (signals.HasNuxtConfig || signals.NuxtInPackageJson)
        {
            evidence.Add(new("form", "web", "发现 Nuxt 工程（nuxt.config 或 package.json 依赖 nuxt）"));
            evidence.Add(new("frontend", "vue", "Nuxt 基于 Vue 3"));
            evidence.Add(new("backend", "node", "Nuxt 使用 Nitro（Node 服务端）"));
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

        signals.HasNuxtConfig = File.Exists(Path.Combine(root, "nuxt.config.ts"))
            || File.Exists(Path.Combine(root, "nuxt.config.js"));

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

        foreach (var file in EnumerateShallow(root, new[] { "*.sln", "*.slnx" }, 3))
            signals.HasSln = true;

        // csproj 深度取 4：常见解决方案布局（<方案>/<src>/<项目>/<项目>.csproj，如 CinemaHub/src/CinemaHub.Web）
        // 项目文件在深度 3，多仓子项目（根/<子项目>/src/...）更深一层；目录只降文件读，不影响双击启动的瞬时性
        foreach (var file in EnumerateShallow(root, new[] { "*.csproj" }, 4))
        {
            signals.CsprojCount++;
            if (signals.CsprojContents.Count < 20)
            {
                try
                {
                    signals.CsprojContents.Add(File.ReadAllText(file));
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    // 读不动的工程文件直接跳过，不影响整体检测
                }
            }
        }

        signals.HasRazorFiles = EnumerateShallow(root, new[] { "*.razor" }, 4).Any();
        // Razor 文件常藏在 Components/Pages 等更深目录（如 src/<项目>/Components），
        // 按 *.razor 浅扫探不到；这里专门找名为 Components 的目录（≤3 层），
        // 其子树下存在 .razor 即视为 Blazor Web App 模板结构
        signals.RazorUnderComponents = HasRazorUnderComponentsDirectory(root, 4);
        signals.HasRootHtml = Directory
            .EnumerateFiles(root, "*.html", SearchOption.TopDirectoryOnly)
            .Any();

        signals.HasRootHtmlOnly = signals.HasRootHtml
            && signals.CsprojCount == 0
            && !signals.HasPackageJson
            && !signals.HasSln;
        return signals;
    }

    // 找名为 Components 的目录（深度 ≤maxDepth，跳过 bin/obj 等），其子树存在 .razor 即命中。
    // .NET 8+ Blazor Web App 模板把组件收在 <项目>/Components 下，传统 Server 则在根 / Pages
    private static bool HasRazorUnderComponentsDirectory(string root, int maxDepth)
    {
        var stack = new Stack<(string Dir, int Depth)>();
        stack.Push((root, 0));
        while (stack.Count > 0)
        {
            var (dir, depth) = stack.Pop();
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
                if (name.Equals("Components", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        if (Directory.EnumerateFiles(sub, "*.razor", SearchOption.AllDirectories).Any())
                            return true;
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        // 读不动的目录跳过，不影响整体检测
                    }

                    continue;
                }

                if (depth + 1 < maxDepth)
                    stack.Push((sub, depth + 1));
            }
        }

        return false;
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
            signals.NuxtInPackageJson |= deps.Contains("nuxt");
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
