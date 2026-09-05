using System.Text;
using Spectre.Console;

namespace RuleAssembler;

internal static class Program
{
    private const string Banner = "AI Rule 拼接器 — 把一套完整 AI rule 释放到当前目录";

    private static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        try
        {
            var resources = new ResourceStore();
            var manifest = new ManifestLoader(resources).Load();

            // 版本以 manifest.vars 为唯一事实来源，随规则内容一起演进，避免 exe 与规则版本漂移
            Console.WriteLine($"use-ai-rule {manifest.Vars.Version}（{manifest.Vars.LastUpdated}）");

            if (args.Length > 0)
                return RunCommand(args, manifest, resources);

            return RunInteractive(manifest, resources);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"错误：{ex.Message}");
            if (ex.InnerException is not null)
                Console.Error.WriteLine($"详细信息：{ex.InnerException.Message}");

            if (!Console.IsInputRedirected)
            {
                Console.WriteLine("按 Enter 键退出。");
                Console.ReadLine();
            }

            return 1;
        }
    }

    // 交互主流程：先检测当前目录给出推荐，确认不了再落到问答决策树 / 经典选择
    private static int RunInteractive(Manifest manifest, ResourceStore resources)
    {
        Console.WriteLine(Banner);
        Console.WriteLine($"当前目录：{Environment.CurrentDirectory}");
        Console.WriteLine();

        // 交互实现只在入口处决定一次：stdin 重定向（管道冒烟/CI）强制走纯文本编号输入；
        // 终端不支持 ANSI 光标控制时 SelectionPrompt 无法擦除上一帧（旧版 conhost 会叠屏），同样回退编号输入
        IConsoleUi ui = CreateUi();
        var renderer = new RuleRenderer(resources);
        var wizard = new ConsoleWizard(ui);

        var selection = OfferEntry(manifest, renderer, wizard, ui);
        if (selection is null)
        {
            Console.WriteLine("已取消。");
            return 0;
        }

        // Q5 能力开关：所有入口统一在选择之后、汇总确认之前执行
        var features = ChooseFeatures(manifest, ui, selection.Profile);
        var files = renderer.Plan(manifest, selection.Profile, features);

        // 已定制的设计系统文件属于产品资产：本次计划包含 design-system.yaml 且本地已是定制版时，保留本地不覆盖
        if (files.Any(file => file.Output.Equals("design-system.yaml", StringComparison.OrdinalIgnoreCase))
            && DesignSystemGuard.ShouldPreserve(Environment.CurrentDirectory))
        {
            files = files
                .Where(file => !file.Output.Equals("design-system.yaml", StringComparison.OrdinalIgnoreCase))
                .ToList();
            Console.WriteLine("检测到已定制的 design-system.yaml：保留本地版本，本次不覆盖（如需重置基线，删除该文件后再生成）");
        }

        Console.WriteLine();
        Console.WriteLine($"已选择：{selection.Profile.Label}");
        Console.WriteLine("将释放以下文件：");
        foreach (var file in files)
            Console.WriteLine($"  - {file.Output}");
        Console.WriteLine("（不释放 TASK.md / rulelog / devlog）");
        Console.WriteLine();

        // 汇总确认：能力开关可能增减产物，所有路径在此统一确认一次
        var confirm = ui.Choose(
            $"确认在当前目录生成 {files.Count} 个文件？",
            "取消",
            new List<UiChoice> { new("开始生成", "") });
        if (confirm is null)
        {
            Console.WriteLine("已取消。");
            return 0;
        }

        var written = new OutputWriter().WriteFiles(files);

        var recorder = new RecipeRecorder();
        var record = RecipeRecorder.Build(manifest, selection.Profile, selection.Mode, selection.Answers, selection.AnswerSources, features, files);
        recorder.Write(Environment.CurrentDirectory, record);
        var gitignoreMessage = recorder.EnsureGitignored(
            Environment.CurrentDirectory,
            RecipeRecorder.StackKeyFor(selection.Profile));

        Console.WriteLine($"完成：已释放 {written} 个文件到 {Environment.CurrentDirectory}");
        Console.WriteLine($"已写入配方记录 {RecipeRecorder.RecordFileName}（{gitignoreMessage}）");
        return 0;
    }

    private static ProfileSelection? OfferEntry(Manifest manifest, RuleRenderer renderer, ConsoleWizard wizard, IConsoleUi ui)
    {
        var detection = TryDetect(manifest, Environment.CurrentDirectory);
        if (detection is null)
        {
            while (true)
            {
                var choice = ui.Choose(
                    "请选择确定规则组合的方式：",
                    "取消",
                    new List<UiChoice>
                    {
                        new("问答选择（推荐）", "回答项目形态、前后端技术栈等几个问题"),
                        new("经典选择", "技术大类 → Profile")
                    });
                if (choice is null)
                    return null;
                if (choice == 0)
                    return wizard.SelectByWizard(manifest);
                if (choice == 1)
                    return wizard.SelectProfile(manifest);
            }
        }

        ui.Head("已检测当前目录，判断依据：");
        ui.Bullets(detection.Reasons.Take(4));
        ui.Bullets(detection.Notes.Take(2));
        ui.Line("");

        // 部分检测结果：证据不足以锁定唯一组合，不伪造推荐，进入补答流程
        if (!detection.Converged)
            return OfferSupplement(manifest, wizard, ui, detection);

        var profile = detection.LegacyProfile;
        ui.Line($"推荐组合：{profile.Label}");

        while (true)
        {
            var choice = ui.Choose(
                "请确认：",
                "取消",
                new List<UiChoice>
                {
                    new("使用推荐组合", "随后可选择能力开关并确认生成"),
                    new("问答选择（重新回答）", ""),
                    new("经典选择", "技术大类 → Profile")
                });
            if (choice is null)
                return null;
            if (choice == 0)
            {
                var answers = detection.BuildLegacyAnswers();
                return new ProfileSelection(
                    profile,
                    "detected",
                    answers,
                    answers.Keys.ToDictionary(key => key, _ => "detected", StringComparer.Ordinal));
            }
            if (choice == 1)
                return wizard.SelectByWizard(manifest);
            if (choice == 2)
                return wizard.SelectProfile(manifest);
        }
    }

    // Q5 能力开关：只列出可用能力（不可用能力隐藏）；
    // ai-capability 的启用由向导 AI 问答这一唯一事实来源决定，这里仅展示不调整
    private static HashSet<string> ChooseFeatures(Manifest manifest, IConsoleUi ui, Profile profile)
    {
        var defaults = FeatureSelection.Defaults(manifest, profile);
        var aiFeature = manifest.Features
            .FirstOrDefault(feature => feature.Id.Equals("ai-capability", StringComparison.OrdinalIgnoreCase));
        if (aiFeature is not null && defaults.Contains(aiFeature.Id))
            ui.Line($"AI 能力：已启用（{aiFeature.Desc}）；是否启用由 AI 问答决定，此处不调整");

        // 能力选择只覆盖 doc / infra / design-system 等可开关能力；AI 保持向导回答的状态
        var toggleable = FeatureSelection.Available(manifest, profile)
            .Where(feature => !feature.Id.Equals("ai-capability", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var final = toggleable.Count == 0
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : ui.ChooseMany(
                    "选择本次生成的能力：",
                    toggleable.Select(feature => new UiChoice(feature.Label, feature.Desc)).ToList(),
                    toggleable
                        .Select((feature, index) => (feature, index))
                        .Where(pair => defaults.Contains(pair.feature.Id))
                        .Select(pair => pair.index)
                        .ToList())
                .Select(index => toggleable[index].Id)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // 用勾选结果覆盖可开关能力的默认值，AI 能力保留向导回答的状态，再按可用性归一化
        foreach (var id in defaults)
        {
            if (id.Equals("ai-capability", StringComparison.OrdinalIgnoreCase))
                final.Add(id);
        }

        return FeatureSelection.Normalize(manifest, profile, final);
    }

    // 检测部分结果的补答入口：展示已确认维度与候选组合，回答只针对未收敛的维度；
    // 选择完成后交回主流程，与其它入口一样经过 Q5 能力开关与统一汇总确认
    private static ProfileSelection? OfferSupplement(
        Manifest manifest,
        ConsoleWizard wizard,
        IConsoleUi ui,
        DetectionResult detection)
    {
        var questionByTrait = manifest.Wizard.Steps
            .GroupBy(step => step.Trait, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().Question, StringComparer.Ordinal);

        ui.Line("已确认维度：" + string.Join(
            "，",
            detection.Confirmed.Select(pair => DescribeTraitValue(manifest, pair.Key, pair.Value))));
        if (detection.Unconfirmed.Count > 0)
        {
            ui.Bullets(detection.Unconfirmed.Select(trait =>
                $"{trait} 证据不足，待确认（{questionByTrait.GetValueOrDefault(trait, trait)}）"));
        }

        ui.Line("候选组合：");
        ui.Bullets(detection.Candidates.Select(profile => $"{profile.Label}（{profile.Id}）"));
        ui.Line("");

        var choice = ui.Choose(
            "请选择：",
            "取消",
            new List<UiChoice>
            {
                new("补答确认（保留已确认结果，只回答缺失项）", ""),
                new("问答选择（重新回答）", ""),
                new("经典选择", "技术大类 → Profile")
            });
        if (choice is null)
            return null;

        return choice switch
        {
            0 => wizard.SupplementByWizard(manifest, detection),
            1 => wizard.SelectByWizard(manifest),
            _ => wizard.SelectProfile(manifest)
        };
    }

    private static string DescribeTraitValue(Manifest manifest, string trait, string value)
    {
        var label = manifest.Wizard.Options.TryGetValue(trait, out var options)
            && options.TryGetValue(value, out var option)
            ? option.Label
            : value;
        return $"{label}（{trait}={value}）";
    }

    private static IConsoleUi CreateUi()
    {
        if (Console.IsInputRedirected)
            return new PlainConsoleUi();
        try
        {
            // Ansi != true 的终端（旧版 conhost 等）无法做光标回退重绘，SelectionPrompt 会叠屏
            if (AnsiConsole.Profile.Capabilities.Ansi == true)
                return new SpectreConsoleUi();
        }
        catch (Exception)
        {
            // 能力探测失败时保守回退到纯文本交互
        }

        return new PlainConsoleUi();
    }

    private static DetectionResult? TryDetect(Manifest manifest, string directory)
    {
        try
        {
            return new ProjectDetector(manifest).Detect(directory);
        }
        catch (Exception)
        {
            // 检测失败不阻塞交互，直接落到问答选择
            return null;
        }
    }

    private static int RunCommand(string[] args, Manifest manifest, ResourceStore resources)
    {
        if (args.Length == 1 && args[0].Equals("--check", StringComparison.OrdinalIgnoreCase))
        {
            var renderer = new RuleRenderer(resources);
            var validator = new RootConsistencyValidator();
            var contractErrors = new List<string>();
            foreach (var profile in manifest.Profiles)
            {
                var files = renderer.Plan(manifest, profile);
                var errors = validator.Validate(manifest, renderer, profile);
                if (errors.Count == 0)
                {
                    Console.WriteLine($"通过：{profile.Id}（{files.Count} 个文件）");
                    continue;
                }

                contractErrors.AddRange(errors);
                Console.WriteLine($"失败：{profile.Id}（输出契约不一致）");
            }

            if (contractErrors.Count > 0)
            {
                Console.Error.WriteLine("输出契约校验失败：");
                foreach (var error in contractErrors)
                    Console.Error.WriteLine($"- {error}");
                return 1;
            }

            var wizardErrors = new WizardEngine(manifest).SimulateAll();
            if (wizardErrors.Count > 0)
            {
                Console.Error.WriteLine("决策树校验失败：");
                foreach (var error in wizardErrors)
                    Console.Error.WriteLine($"- {error}");
                return 1;
            }

            Console.WriteLine($"决策树校验通过：全部 Profile 可达且无歧义路径。");
            Console.WriteLine($"校验完成：{manifest.Profiles.Count} 个 Profile 全部通过。");
            return 0;
        }

        if (args.Length is 1 or 2 && args[0].Equals("--detect", StringComparison.OrdinalIgnoreCase))
        {
            var dir = args.Length == 2 ? args[1] : Environment.CurrentDirectory;
            var result = new ProjectDetector(manifest).Detect(dir);
            if (result is null)
            {
                Console.WriteLine($"未能在 {dir} 识别出已接入的技术栈。");
                return 1;
            }

            Console.WriteLine($"判断依据：");
            foreach (var reason in result.Reasons)
                Console.WriteLine($"  - {reason}");
            foreach (var note in result.Notes)
                Console.WriteLine($"  - {note}");

            if (result.Converged)
            {
                Console.WriteLine($"推荐组合：{result.LegacyProfileLabel}（{result.LegacyProfileId}）");
                Console.WriteLine("回答组合：" + string.Join(
                    "，",
                    result.BuildLegacyAnswers().Select(pair => $"{pair.Key}={pair.Value}")));
                return 0;
            }

            // 部分有效但未收敛：打印待确认维度与候选，不伪造完整推荐
            Console.WriteLine("检测未收敛，以下维度已确认：" + string.Join(
                "，",
                result.Confirmed.Select(pair => $"{pair.Key}={pair.Value}")));
            foreach (var trait in result.Unconfirmed)
                Console.WriteLine($"待确认维度：{trait}");
            Console.WriteLine("候选组合：");
            foreach (var candidate in result.Candidates)
                Console.WriteLine($"  - {candidate.Label}（{candidate.Id}）");
            return 0;
        }

        if (args.Length == 2 && args[0].Equals("--emit", StringComparison.OrdinalIgnoreCase))
        {
            var outDir = Path.GetFullPath(args[1]);
            var renderer = new RuleRenderer(resources);
            var plannedProfiles = new List<(Profile Profile, List<PlannedFile> Files)>();
            foreach (var profile in manifest.Profiles)
            {
                var files = renderer.Plan(manifest, profile);
                plannedProfiles.Add((profile, files));
            }

            Directory.CreateDirectory(outDir);
            var allFiles = plannedProfiles
                .SelectMany(item => item.Files.Select(file => new PlannedFile
                {
                    Output = $"{item.Profile.Id}/{file.Output}",
                    Content = file.Content
                }))
                .ToList();
            new OutputWriter().WriteFiles(outDir, allFiles);
            foreach (var item in plannedProfiles)
                Console.WriteLine($"已输出：{item.Profile.Id}（{item.Files.Count} 个文件）");

            Console.WriteLine($"输出完成：{manifest.Profiles.Count} 个 Profile → {outDir}");
            return 0;
        }

        if (args.Length == 1 && (args[0].Equals("--help", StringComparison.OrdinalIgnoreCase)
            || args[0].Equals("-h", StringComparison.OrdinalIgnoreCase)))
        {
            Console.WriteLine(Banner);
            Console.WriteLine("直接双击或不带参数运行：自动检测当前项目给出推荐，也可进入问答选择并释放规则。");
            Console.WriteLine("--check：只校验 manifest、片段、全部 Profile 与决策树，不写入文件。");
            Console.WriteLine("--detect [目录]：只做项目检测，打印推荐组合；证据不足时打印待确认维度与候选组合，不写入文件。");
            Console.WriteLine("--emit <目录>：把全部 Profile 的生成结果输出到指定目录，仅用于批量预览与对比，不覆盖工作目录。");
            Console.WriteLine("--help：显示帮助。");
            return 0;
        }

        throw new ArgumentException($"不支持的参数：{string.Join(" ", args)}。请直接运行，或使用 --check。", nameof(args));
    }
}
