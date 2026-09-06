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

    // 交互主流程（P2 阶段 3）：问答 / 检测入口产出 Selection 走组合内核；
    // 经典大类 → Profile 保留为兼容入口，仍走旧渲染路径
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
        var planner = new CompositionPlanner(manifest);

        while (true)
        {
            var entry = OfferEntry(manifest, wizard, ui);
            if (entry is null)
            {
                Console.WriteLine("已取消。");
                return 0;
            }

            List<PlannedFile> files;
            string summaryLabel;
            RecipeRecord record;
            string stackKey;
            if (entry.Selection is not null)
            {
                CompositionPlan plan;
                try
                {
                    plan = planner.Plan(entry.Selection);
                }
                catch (InvalidOperationException ex)
                {
                    // 组合无法解析时回到入口菜单，不让用户从头再来
                    Console.WriteLine($"组合无法解析：{ex.Message}");
                    Console.WriteLine();
                    continue;
                }

                var features = ChooseFeaturesForPlan(manifest, ui, plan);
                files = renderer.Plan(manifest, plan, features);
                summaryLabel = DescribeCompositionLabel(manifest, plan);
                stackKey = RecipeRecorder.StackKeyForComposition(manifest, plan);
                record = RecipeRecorder.BuildComposition(
                    manifest, entry.Selection, plan.ComponentIds, entry.Mode,
                    entry.Answers, entry.AnswerSources, features, files);
            }
            else
            {
                var legacy = entry.Legacy!;
                var features = ChooseFeatures(manifest, ui, legacy.Profile);
                files = renderer.Plan(manifest, legacy.Profile, features);
                summaryLabel = legacy.Profile.Label;
                stackKey = RecipeRecorder.StackKeyFor(legacy.Profile);
                record = RecipeRecorder.Build(
                    manifest, legacy.Profile, legacy.Mode, legacy.Answers, legacy.AnswerSources, features, files);
            }

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
            Console.WriteLine($"已选择：{summaryLabel}");
            if (entry.Selection is not null)
                Console.WriteLine($"命中组件：{string.Join("、", record.ComponentIds ?? new List<string>())}");
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
            recorder.Write(Environment.CurrentDirectory, record);
            var gitignoreMessage = recorder.EnsureGitignored(Environment.CurrentDirectory, stackKey);

            Console.WriteLine($"完成：已释放 {written} 个文件到 {Environment.CurrentDirectory}");
            Console.WriteLine($"已写入配方记录 {RecipeRecorder.RecordFileName}（{gitignoreMessage}）");
            return 0;
        }
    }

    // 入口结果：Selection 非空走组合内核；否则走 Legacy 的经典 Profile 路径
    private sealed record EntryOutcome(
        Selection? Selection,
        ProfileSelection? Legacy,
        string Mode,
        Dictionary<string, string> Answers,
        Dictionary<string, string> AnswerSources);

    private static EntryOutcome? LegacyOutcome(ProfileSelection selection)
    {
        return new EntryOutcome(null, selection, selection.Mode, selection.Answers, selection.AnswerSources);
    }

    private static EntryOutcome? SelectionOutcome(
        Selection selection, string mode, Dictionary<string, string> answers, Dictionary<string, string> sources)
    {
        return new EntryOutcome(selection, null, mode, answers, sources);
    }

    // 逐字节比较两条渲染路径的产物：路径集合、顺序与内容都必须一致，任何差异都是回归
    private static List<string> ComparePlannedFiles(
        string caseName, List<PlannedFile> legacyFiles, List<PlannedFile> composeFiles)
    {
        var errors = new List<string>();
        if (legacyFiles.Count != composeFiles.Count)
        {
            errors.Add($"文件数量不一致：旧路径 {legacyFiles.Count} 个，组合路径 {composeFiles.Count} 个");
        }

        var legacyByName = legacyFiles.ToDictionary(
            file => file.Output, file => file, StringComparer.OrdinalIgnoreCase);
        var composeByName = composeFiles.ToDictionary(
            file => file.Output, file => file, StringComparer.OrdinalIgnoreCase);

        foreach (var pair in legacyByName)
        {
            if (!composeByName.TryGetValue(pair.Key, out var composeFile))
            {
                errors.Add($"组合路径缺少文件：{pair.Key}");
                continue;
            }

            if (!string.Equals(pair.Value.Content, composeFile.Content, StringComparison.Ordinal))
                errors.Add($"文件内容不一致：{pair.Key}");
        }

        foreach (var pair in composeByName)
        {
            if (!legacyByName.ContainsKey(pair.Key))
                errors.Add($"组合路径多出文件：{pair.Key}");
        }

        var orderMismatch = legacyFiles
            .Select((file, index) => (file, index))
            .Where(item => composeFiles.Count > item.index
                && !composeFiles[item.index].Output.Equals(item.file.Output, StringComparison.OrdinalIgnoreCase))
            .Select(item => item.file.Output)
            .ToList();
        if (orderMismatch.Count > 0)
            errors.Add($"文件顺序不一致：{string.Join("、", orderMismatch)}（样例 {caseName}）");

        return errors;
    }

    private static EntryOutcome? OfferEntry(Manifest manifest, ConsoleWizard wizard, IConsoleUi ui)
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
                        new("经典选择（兼容）", "技术大类 → Profile")
                    });
                if (choice is null)
                    return null;
                if (choice == 0)
                    return RunSelectionWizard(manifest, ui, "wizard");
                if (choice == 1)
                    return wizard.SelectProfile(manifest) is { } legacyPick ? LegacyOutcome(legacyPick) : null;
            }
        }

        ui.Head("已检测当前目录，判断依据：");
        ui.Bullets(detection.Reasons.Take(4));
        ui.Bullets(detection.Notes.Take(2));
        ui.Line("");

        // 部分检测结果：证据不足以锁定唯一组合，不伪造推荐，进入补答流程
        if (!detection.Converged)
            return OfferSupplement(manifest, ui, detection);

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
                    new("经典选择（兼容）", "技术大类 → Profile")
                });
            if (choice is null)
                return null;
            if (choice == 0)
            {
                // 收敛的检测证据直接映射为 Selection：取推荐 Profile 的全部具体 trait
                var answers = detection.BuildLegacyAnswers();
                var selection = new Selection { Dimensions = new Dictionary<string, string>(answers, StringComparer.Ordinal) };
                return SelectionOutcome(
                    selection, "detected", answers,
                    answers.Keys.ToDictionary(key => key, _ => "detected", StringComparer.Ordinal));
            }
            if (choice == 1)
                return RunSelectionWizard(manifest, ui, "wizard");
            if (choice == 2)
                return wizard.SelectProfile(manifest) is { } classicPick ? LegacyOutcome(classicPick) : null;
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

    // Q5 能力开关（组合路径）：可用性由 CompositionPlan 的文件贡献与 design-system 来源推导，
    // 关闭能力不会生成对应文件，根导航同步收敛
    private static HashSet<string> ChooseFeaturesForPlan(Manifest manifest, IConsoleUi ui, CompositionPlan plan)
    {
        var defaults = FeatureSelection.Defaults(manifest, plan);
        var aiFeature = manifest.Features
            .FirstOrDefault(feature => feature.Id.Equals("ai-capability", StringComparison.OrdinalIgnoreCase));
        if (aiFeature is not null && defaults.Contains(aiFeature.Id))
            ui.Line($"AI 能力：已启用（{aiFeature.Desc}）；是否启用由问答回答决定，此处不调整");

        var toggleable = FeatureSelection.Available(manifest, plan)
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

        // 用勾选结果覆盖可开关能力的默认值，AI 能力保留问答回答的状态，再按可用性归一化
        foreach (var id in defaults)
        {
            if (id.Equals("ai-capability", StringComparison.OrdinalIgnoreCase))
                final.Add(id);
        }

        return FeatureSelection.Normalize(manifest, plan, final);
    }

    // 组件问答主循环：检测补答与全新问答共用；已确认（detected）维度保留且不再提问，
    // 唯一取值自动推导（来源 derived），0 返回上一步，第一问返回交还入口菜单
    private static EntryOutcome? RunSelectionWizard(
        Manifest manifest, IConsoleUi ui, string mode, Dictionary<string, string>? seeds = null)
    {
        var engine = new SelectionEngine(manifest);
        var seedDims = seeds ?? new Dictionary<string, string>();
        var selection = new Selection
        {
            Dimensions = new Dictionary<string, string>(seedDims, StringComparer.Ordinal)
        };
        var answers = new Dictionary<string, string>(seedDims, StringComparer.Ordinal);
        var sources = seedDims.Keys
            .ToDictionary(key => key, _ => "detected", StringComparer.Ordinal);
        var history = new List<(Dictionary<string, string> Dims, Dictionary<string, string> Answers, Dictionary<string, string> Sources)>();

        while (true)
        {
            var derived = new List<(string Trait, string Value)>();
            var question = engine.NextQuestion(selection, derived);
            foreach (var pair in derived)
            {
                answers[pair.Trait] = pair.Value;
                sources[pair.Trait] = "derived";
                ui.Line($"已自动确定 {DescribeTraitValue(manifest, pair.Trait, pair.Value)}（当前组合下唯一取值）");
            }

            if (question is null)
            {
                return SelectionOutcome(selection, mode, answers, sources);
            }

            var dimsBefore = new Dictionary<string, string>(selection.Dimensions, StringComparer.Ordinal);
            var answersBefore = new Dictionary<string, string>(answers, StringComparer.Ordinal);
            var sourcesBefore = new Dictionary<string, string>(sources, StringComparer.Ordinal);
            var choice = ui.Choose(
                question.Question,
                "返回上一步",
                question.Options.Select(option => new UiChoice(option.Label, option.Desc)).ToList());
            if (choice is null)
            {
                if (history.Count == 0)
                    return null;
                (var dims, answers, sources) = history[^1];
                history.RemoveAt(history.Count - 1);
                selection.Dimensions = dims;
                continue;
            }

            history.Add((dimsBefore, answersBefore, sourcesBefore));
            var selected = question.Options[choice.Value];
            selection.Dimensions[selected.Trait] = selected.Value;
            answers[selected.Trait] = selected.Value;
            sources[selected.Trait] = "manual";
        }
    }

    // 检测部分结果的补答入口（组合路径）：展示已确认维度与候选组合，
    // 已确认维度作为种子带入组件问答，只回答仍有分歧的维度
    private static EntryOutcome? OfferSupplement(Manifest manifest, IConsoleUi ui, DetectionResult detection)
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
                new("经典选择（兼容）", "技术大类 → Profile")
            });
        if (choice is null)
            return null;

        if (choice == 0)
            return RunSelectionWizard(manifest, ui, "detected", new Dictionary<string, string>(detection.Confirmed, StringComparer.Ordinal));
        if (choice == 1)
            return RunSelectionWizard(manifest, ui, "wizard");
        var legacy = new ConsoleWizard(ui).SelectProfile(manifest);
        return legacy is null ? null : LegacyOutcome(legacy);
    }

    // 组合路径的展示标签：唯一映射兼容样例时用其 Profile 名称，否则列组件
    private static string DescribeCompositionLabel(Manifest manifest, CompositionPlan plan)
    {
        var matches = SelectionEngine.MatchCompatibilityCases(manifest, plan.Selection);
        var profile = matches.Count == 1
            ? manifest.Profiles.FirstOrDefault(item => item.Id.Equals(matches[0], StringComparison.OrdinalIgnoreCase))
            : null;
        return profile is not null ? profile.Label : $"组件组合（{string.Join("、", plan.ComponentIds)}）";
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

            // 组件问答决策树仿真：每条交互可达路径必须可解析且唯一映射兼容样例，全部样例可达
            var selectionErrors = new SelectionEngine(manifest).SimulateAll(new CompositionPlanner(manifest));
            if (selectionErrors.Count > 0)
            {
                Console.Error.WriteLine("组件问答决策树校验失败：");
                foreach (var error in selectionErrors)
                    Console.Error.WriteLine($"- {error}");
                return 1;
            }

            Console.WriteLine("组件问答校验通过：全部交互路径可解析，28 个兼容样例可达且无歧义。");

            // 组合内核兼容样例：组合路径与旧 Profile 产物逐字节等价，等价被破坏时 --check 失败
            if (manifest.CompatibilityCases.Count > 0)
            {
                var planner = new CompositionPlanner(manifest);
                var compositionErrors = new List<string>();
                foreach (var item in manifest.CompatibilityCases)
                {
                    var profile = manifest.Profiles.First(candidate =>
                        candidate.Id.Equals(item.Profile, StringComparison.OrdinalIgnoreCase));
                    var selection = new Selection { Dimensions = new Dictionary<string, string>(item.Selection, StringComparer.Ordinal) };
                    try
                    {
                        var plan = planner.Plan(selection);
                        var legacyFiles = renderer.Plan(manifest, profile);
                        var composeFiles = renderer.Plan(manifest, plan, FeatureSelection.Defaults(manifest, plan));
                        var caseErrors = ComparePlannedFiles(profile.Id, legacyFiles, composeFiles);
                        if (caseErrors.Count == 0)
                        {
                            Console.WriteLine($"组合等价：{profile.Id} ← [{string.Join("、", plan.ComponentIds)}]");
                            continue;
                        }

                        compositionErrors.AddRange(caseErrors);
                        Console.WriteLine($"组合不等价：{profile.Id}");
                    }
                    catch (InvalidOperationException ex)
                    {
                        compositionErrors.Add($"组合解析失败：{profile.Id}：{ex.Message}");
                        Console.WriteLine($"组合解析失败：{profile.Id}");
                    }
                }

                if (compositionErrors.Count > 0)
                {
                    Console.Error.WriteLine("组合内核兼容样例校验失败：");
                    foreach (var error in compositionErrors)
                        Console.Error.WriteLine($"- {error}");
                    return 1;
                }
            }

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

        // 按配方重放生成：--recipe <配方.json> [目标目录]（缺省为当前目录）。
        // 与交互生成同一套组合解析与 design-system 保留语义；配方与当前规则不一致时显式报错
        if (args.Length is 2 or 3 && args[0].Equals("--recipe", StringComparison.OrdinalIgnoreCase))
        {
            var recipePath = Path.GetFullPath(args[1]);
            if (!File.Exists(recipePath))
                throw new IOException($"找不到配方文件：{recipePath}");
            var targetDirectory = args.Length == 3 ? Path.GetFullPath(args[2]) : Environment.CurrentDirectory;

            var replayer = new RecipeReplayer(manifest, new RuleRenderer(resources));
            var result = replayer.Replay(File.ReadAllText(recipePath), targetDirectory);

            foreach (var note in result.Notes)
                Console.WriteLine(note);
            Console.WriteLine($"命中组件：{string.Join("、", result.Plan.ComponentIds)}");
            Console.WriteLine($"能力开关：{string.Join("、", result.Features.OrderBy(id => id, StringComparer.Ordinal))}");
            Console.WriteLine($"将释放以下文件到 {targetDirectory}：");
            foreach (var file in result.Files)
                Console.WriteLine($"  - {file.Output}");

            var written = new OutputWriter().WriteFiles(targetDirectory, result.Files);
            var recorder = new RecipeRecorder();
            recorder.Write(targetDirectory, result.Record);
            var gitignoreMessage = recorder.EnsureGitignored(
                targetDirectory,
                RecipeRecorder.StackKeyForComposition(manifest, result.Plan));

            Console.WriteLine($"完成：已重放 {written} 个文件到 {targetDirectory}");
            Console.WriteLine($"已写入配方记录 {RecipeRecorder.RecordFileName}（{gitignoreMessage}）");
            return 0;
        }

        if (args.Length == 2 && (args[0].Equals("--emit", StringComparison.OrdinalIgnoreCase)
            || args[0].Equals("--emit-all-legacy", StringComparison.OrdinalIgnoreCase)))
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

        // 显式维度选择预览：--plan form=web frontend=react backend=dotnet ...
        // 与问答同一套推导：唯一取值自动补全，仍有分歧的维度报错并列出可选值；只打印不写入
        if (args.Length >= 1 && args[0].Equals("--plan", StringComparison.OrdinalIgnoreCase))
        {
            var dimensions = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var arg in args.Skip(1))
            {
                var separator = arg.IndexOf('=', StringComparison.Ordinal);
                if (separator <= 0)
                    throw new ArgumentException($"--plan 参数必须是 维度=取值 形式：{arg}", nameof(args));
                dimensions[arg[..separator]] = arg[(separator + 1)..];
            }

            var selection = new Selection { Dimensions = dimensions };
            var engine = new SelectionEngine(manifest);
            while (engine.NextQuestion(selection) is { } question)
            {
                var values = string.Join(" / ", question.Options.Select(option => option.Value));
                throw new ArgumentException(
                    $"--plan 缺少仍有分歧的维度：{question.Trait}（可选值：{values}），不隐式补全", nameof(args));
            }

            var plan = new CompositionPlanner(manifest).Plan(selection);
            Console.WriteLine($"命中组件：{string.Join("、", plan.ComponentIds)}");
            var features = FeatureSelection.Defaults(manifest, plan);
            Console.WriteLine($"默认能力：{string.Join("、", features.OrderBy(id => id, StringComparer.Ordinal))}");
            foreach (var warning in plan.Warnings)
                Console.WriteLine($"告警：{warning}");
            Console.WriteLine("将生成以下文件：");
            foreach (var file in new RuleRenderer(resources).Plan(manifest, plan, features))
                Console.WriteLine($"  - {file.Output}");
            return 0;
        }

        if (args.Length == 1 && (args[0].Equals("--help", StringComparison.OrdinalIgnoreCase)
            || args[0].Equals("-h", StringComparison.OrdinalIgnoreCase)))
        {
            Console.WriteLine(Banner);
            Console.WriteLine("直接双击或不带参数运行：自动检测当前项目给出推荐，也可进入问答选择并释放规则。");
            Console.WriteLine("--check：只校验 manifest、片段、全部 Profile、输出契约一致性、问答决策树与组件问答决策树，不写入文件。");
            Console.WriteLine("--detect [目录]：只做项目检测，打印推荐组合；证据不足时打印待确认维度与候选组合，不写入文件。");
            Console.WriteLine("--plan 维度=取值 ...：按显式维度选择解析组合内核，打印命中组件与文件清单，不写入文件。");
            Console.WriteLine("--recipe <配方.json> [目标目录]：按配方记录重放生成（缺省写入当前目录），写入配方与 .gitignore。");
            Console.WriteLine("--emit <目录> / --emit-all-legacy <目录>：把全部 Profile 的旧路径生成结果输出到指定目录，仅用于批量预览与历史产物回归。");
            Console.WriteLine("--help：显示帮助。");
            return 0;
        }

        throw new ArgumentException($"不支持的参数：{string.Join(" ", args)}。请直接运行，或使用 --check。", nameof(args));
    }
}
