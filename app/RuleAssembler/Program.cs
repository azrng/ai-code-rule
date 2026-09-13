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

            var skills = new SkillsStore();
            if (args.Length > 0)
                return RunCommand(args, manifest, resources, skills);

            return RunInteractive(manifest, resources, skills);
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
    private static int RunInteractive(Manifest manifest, ResourceStore resources, SkillsStore skillsStore)
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

            // Skills 导入：规则文件落盘后的可选追加步骤，勾选结果随配方记录（--upgrade / --recipe 按配方同步）
            var skillImport = OfferSkillsImport(ui, skillsStore);
            var skillWritten = 0;
            if (skillImport is not null)
            {
                Console.WriteLine($"导入 Skills 到 {Environment.CurrentDirectory}：");
                skillWritten = new SkillImporter().Import(
                    Environment.CurrentDirectory, skillImport.Targets, skillImport.Skills, skillsStore, skillImport.SkipKeys);
            }

            // 配方只记实际写入的技能：被跳过的同名技能保留本地版本，也不进入后续升级的同步范围
            record = RecipeRecorder.WithSkills(
                record,
                skillImport?.Skills
                    .Where(skill => skillImport.Targets.Any(target =>
                        skillImport.SkipKeys?.Contains($"{target.Id}/{skill.Id}") != true))
                    .Select(skill => skill.Id)
                    .ToList(),
                skillImport?.Targets.Select(target => target.Id).ToList());

            var recorder = new RecipeRecorder();
            recorder.Write(Environment.CurrentDirectory, record);
            var gitignoreMessage = recorder.EnsureGitignored(Environment.CurrentDirectory, stackKey);

            Console.WriteLine($"完成：已释放 {written} 个文件到 {Environment.CurrentDirectory}" +
                (skillWritten > 0 ? $"，另导入 Skills {skillWritten} 个文件" : ""));
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

    // Skills 导入勾选结果：技能与目标都是多选；SkipKeys 是「<targetId>/<skillId>」的跳过集合（保留本地同名技能）；
    // null 表示用户跳过导入（不写入配方）
    private sealed record SkillImportSelection(
        IReadOnlyList<SkillInfo> Skills,
        IReadOnlyList<SkillTarget> Targets,
        IReadOnlySet<string>? SkipKeys);

    // Skills 导入（可选步骤）：规则文件落盘后追加；技能多选（默认全不勾）→ 目标工具多选（默认全勾）。
    // 导入是追加语义：目标目录已有同名技能（含 SKILL.md）时先问覆盖还是跳过，未选中的技能一律不动；
    // 覆盖 = 同名技能目录先删后写、按内置内容整体重写；任何一步取消或零勾选都视为跳过导入，配方不记录技能字段
    private static SkillImportSelection? OfferSkillsImport(IConsoleUi ui, SkillsStore skillsStore)
    {
        var skills = skillsStore.Discover();
        if (skills.Count == 0)
            return null;

        var entry = ui.Choose(
            "是否导入 AI Skills（内置于生成器，免手动拷贝）？",
            "跳过，不导入",
            new List<UiChoice>
            {
                new("选择 Skills 并导入", $"{skills.Count} 个内置技能，可选导入到 Codex / ZCode / Claude Code")
            });
        if (entry is null)
            return null;

        var picked = ui.ChooseMany(
            "选择要导入的 Skills（可多选）：",
            skills.Select(skill => new UiChoice(skill.Id, TruncateDescription(skill.Description))).ToList(),
            new List<int>());
        if (picked.Count == 0)
        {
            Console.WriteLine("未勾选任何 Skill，跳过导入。");
            return null;
        }

        var targetPicked = ui.ChooseMany(
            "选择导入到哪些工具（项目级目录，可多选）：",
            SkillTargets.All.Select(target => new UiChoice(target.Label, $"{target.RelativeRoot}/<skill>/")).ToList(),
            Enumerable.Range(0, SkillTargets.All.Count).ToList());
        if (targetPicked.Count == 0)
        {
            Console.WriteLine("未选择任何目标工具，跳过导入。");
            return null;
        }

        var selectedSkills = picked.Select(index => skills[index]).ToList();
        var selectedTargets = targetPicked.Select(index => SkillTargets.All[index]).ToList();

        // 同名冲突处置：追加导入的前提是同名技能显式确认；取消则整体放弃导入
        var conflicts = new SkillImporter().DetectConflicts(Environment.CurrentDirectory, selectedTargets, selectedSkills);
        IReadOnlySet<string>? skipKeys = null;
        if (conflicts.Count > 0)
        {
            Console.WriteLine($"检测到 {conflicts.Count} 个同名 Skills 已存在于目标目录：");
            foreach (var conflict in conflicts.Take(8))
            {
                var separator = conflict.IndexOf('/');
                Console.WriteLine($"  - [{conflict[..separator]}] {conflict[(separator + 1)..]}");
            }

            if (conflicts.Count > 8)
                Console.WriteLine($"  …等共 {conflicts.Count} 个");
            var overwrite = ui.Choose(
                "同名 Skills 如何处理？（未命中的新增技能照常导入）",
                "取消导入",
                new List<UiChoice>
                {
                    new("覆盖已存在的", "删除本地同名技能后按内置版本整体重写（清理上游已删改的残留文件）"),
                    new("跳过已存在的", "保留本地版本不动，只导入新增技能")
                });
            if (overwrite is null)
                return null;
            if (overwrite == 1)
                skipKeys = conflicts;
        }

        return new SkillImportSelection(selectedSkills, selectedTargets, skipKeys);
    }

    private static string TruncateDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return "（SKILL.md 未提供 description）";
        return description.Length <= 60 ? description : description[..60] + "…";
    }

    // 重放 / 升级路径的技能同步：配方记录了 skills + skill_targets 时按内置内容覆盖重放，
    // 技能或目标已不在当前版本生成器中时记告警跳过，不阻塞规则文件升级
    private static int ImportRecordedSkills(string targetDirectory, RecipeRecord record, SkillsStore skillsStore)
    {
        if (record.Skills is not { Count: > 0 } || record.SkillTargets is not { Count: > 0 })
            return 0;

        var discovered = skillsStore.Discover();
        var skills = discovered
            .Where(skill => record.Skills.Contains(skill.Id, StringComparer.OrdinalIgnoreCase))
            .ToList();
        foreach (var missing in record.Skills.Where(skillId =>
            !discovered.Any(candidate => candidate.Id.Equals(skillId, StringComparison.OrdinalIgnoreCase))))
        {
            Console.WriteLine($"告警：配方记录的技能 {missing} 已不在当前版本生成器中，跳过");
        }

        var targets = record.SkillTargets
            .Select(SkillTargets.Find)
            .Where(target => target is not null)
            .Cast<SkillTarget>()
            .ToList();
        if (skills.Count == 0 || targets.Count == 0)
            return 0;

        Console.WriteLine($"按配方同步 Skills 到 {targetDirectory}（同名技能先删后写，按内置内容整体重写）：");
        return new SkillImporter().Import(targetDirectory, targets, skills, skillsStore);
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

    // --check 的技能资源校验：发现数 > 0、ID 不冲突（忽略大小写，目录名跨平台大小写不敏感）、
    // 每个 SKILL.md 有 frontmatter description（选择列表与导入说明依赖它展示）
    private static List<string> ValidateSkills(SkillsStore skillsStore)
    {
        var errors = new List<string>();
        var skills = skillsStore.Discover();
        if (skills.Count == 0)
        {
            errors.Add("未发现任何内置技能（skills/ 下没有含 SKILL.md 的目录）");
            return errors;
        }

        var duplicateIds = skills
            .GroupBy(skill => skill.Id, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key}（{string.Join("、", group.Select(skill => skill.ResourceRoot))}）")
            .ToList();
        errors.AddRange(duplicateIds.Select(duplicate => $"技能 ID 冲突：{duplicate}"));

        errors.AddRange(skills
            .Where(skill => string.IsNullOrWhiteSpace(skill.Description))
            .Select(skill => $"技能 {skill.Id}（{skill.ResourceRoot}）的 SKILL.md 缺少 description"));

        return errors;
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

    private static int RunCommand(string[] args, Manifest manifest, ResourceStore resources, SkillsStore skillsStore)
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

            Console.WriteLine($"组件问答校验通过：全部交互路径可解析，{manifest.CompatibilityCases.Count} 个兼容样例可达且无歧义。");

            // 内置技能校验：资源里必须能发现技能、ID 全局唯一、每个 SKILL.md 带 description 供选择列表展示
            var skillErrors = ValidateSkills(skillsStore);
            if (skillErrors.Count > 0)
            {
                Console.Error.WriteLine("内置技能校验失败：");
                foreach (var error in skillErrors)
                    Console.Error.WriteLine($"- {error}");
                return 1;
            }

            Console.WriteLine($"内置技能校验通过：{skillsStore.Discover().Count} 个技能，SKILL.md 齐全且 ID 无冲突。");

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

        // 按配方升级已有规则：--upgrade [目录] [--force]。
        // 读取目标目录的 .ai-rule-profile.json 重放生成；配方带内容哈希时识别本地修改过的文件，
        // 默认保留本地版本只更新未修改文件（--force 全部覆盖），回写配方刷新版本与哈希
        if (args.Length >= 1 && args[0].Equals("--upgrade", StringComparison.OrdinalIgnoreCase))
        {
            var force = args.Contains("--force", StringComparer.Ordinal);
            var dirArg = args.Skip(1).FirstOrDefault(arg => !arg.Equals("--force", StringComparison.Ordinal));
            var targetDirectory = dirArg is null ? Environment.CurrentDirectory : Path.GetFullPath(dirArg);
            var recipePath = Path.Combine(targetDirectory, RecipeRecorder.RecordFileName);
            if (!File.Exists(recipePath))
                throw new IOException($"未找到配方记录 {recipePath}：升级需要此前由生成器写入的配方文件，首次生成请直接运行生成器或使用 --recipe。");

            Console.WriteLine($"use-ai-rule {manifest.Vars.Version}（{manifest.Vars.LastUpdated}）");
            var replayer = new RecipeReplayer(manifest, new RuleRenderer(resources));
            var plan = replayer.PlanUpgrade(File.ReadAllText(recipePath), targetDirectory);
            var replay = plan.Replay;

            foreach (var note in replay.Notes)
                Console.WriteLine(note);
            Console.WriteLine($"命中组件：{string.Join("、", replay.Plan.ComponentIds)}");

            if (plan.ModifiedFiles.Count > 0 && plan.HashDetectable)
            {
                Console.WriteLine($"检测到 {plan.ModifiedFiles.Count} 个本地修改过的文件：");
                foreach (var file in plan.ModifiedFiles)
                    Console.WriteLine($"  - {file}" + (plan.SkippedFiles.Contains(file, StringComparer.OrdinalIgnoreCase) ? "（保留本地版本）" : ""));
                if (!force)
                    Console.WriteLine("默认保留本地修改；确认全部覆盖请追加 --force 重新运行。");
            }
            else if (plan.HashDetectable)
            {
                Console.WriteLine("未检测到本地修改过的文件。");
            }
            else
            {
                Console.WriteLine("配方未记录内容哈希（早期版本生成），无法识别本地修改，将按配方直接重放覆盖。");
            }

            if (force && plan.ModifiedFiles.Count > 0)
            {
                // --force：被保留的修改文件重新纳入写入集合，配方按全新写入重算哈希
                plan = new RecipeReplayer.UpgradePlan(
                    replay, plan.ModifiedFiles, new List<string>(), replay.Files, plan.HashDetectable, replay.Record);
            }

            Console.WriteLine($"将写入 {plan.FilesToWrite.Count} 个文件到 {targetDirectory}" +
                              (plan.SkippedFiles.Count > 0 ? $"（跳过本地修改的 {plan.SkippedFiles.Count} 个）" : "") + "：");
            foreach (var file in plan.FilesToWrite)
                Console.WriteLine($"  - {file.Output}");

            var written = new OutputWriter().WriteFiles(targetDirectory, plan.FilesToWrite);
            var skillWritten = ImportRecordedSkills(targetDirectory, plan.Record, skillsStore);
            var recorder = new RecipeRecorder();
            recorder.Write(targetDirectory, plan.Record);
            var gitignoreMessage = recorder.EnsureGitignored(
                targetDirectory,
                RecipeRecorder.StackKeyForComposition(manifest, replay.Plan));

            Console.WriteLine($"完成：已升级 {written} 个文件（规则版本 {manifest.Vars.Version}）" +
                (skillWritten > 0 ? $"，另同步 Skills {skillWritten} 个文件" : ""));
            Console.WriteLine($"已写入配方记录 {RecipeRecorder.RecordFileName}（{gitignoreMessage}）");
            return 0;
        }

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
            var skillWritten = ImportRecordedSkills(targetDirectory, result.Record, skillsStore);
            var recorder = new RecipeRecorder();
            recorder.Write(targetDirectory, result.Record);
            var gitignoreMessage = recorder.EnsureGitignored(
                targetDirectory,
                RecipeRecorder.StackKeyForComposition(manifest, result.Plan));

            Console.WriteLine($"完成：已重放 {written} 个文件到 {targetDirectory}" +
                (skillWritten > 0 ? $"，另同步 Skills {skillWritten} 个文件" : ""));
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
            Console.WriteLine("规则文件落盘后可选择把内置 Skills 导入到 Codex（.codex/skills）/ ZCode（.zcode/skills）/ Claude Code（.claude/skills），追加导入、同名技能可覆盖或跳过；--recipe / --upgrade 按配方同步已记录的 Skills。");
            Console.WriteLine("--check：只校验 manifest、片段、全部 Profile、输出契约一致性、问答决策树、组件问答决策树与内置技能，不写入文件。");
            Console.WriteLine("--detect [目录]：只做项目检测，打印推荐组合；证据不足时打印待确认维度与候选组合，不写入文件。");
            Console.WriteLine("--plan 维度=取值 ...：按显式维度选择解析组合内核，打印命中组件与文件清单，不写入文件。");
            Console.WriteLine("--recipe <配方.json> [目标目录]：按配方记录重放生成（缺省写入当前目录），写入配方与 .gitignore。");
            Console.WriteLine("--upgrade [目录] [--force]：按目标目录的配方记录升级规则；识别本地修改默认保留，--force 全部覆盖。");
            Console.WriteLine("--emit <目录> / --emit-all-legacy <目录>：把全部 Profile 的旧路径生成结果输出到指定目录，仅用于批量预览与历史产物回归。");
            Console.WriteLine("--help：显示帮助。");
            return 0;
        }

        throw new ArgumentException($"不支持的参数：{string.Join(" ", args)}。请直接运行，或使用 --check。", nameof(args));
    }
}
