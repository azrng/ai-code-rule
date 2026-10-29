using System.Reflection;
using System.Text;
using Spectre.Console;

namespace RuleAssembler;

internal static class Program
{
    private const string Banner = "AI Rule 拼接器 — 把一套完整 AI rule 释放到当前目录";

    private static int Main(string[] args)
    {
        try
        {
            // 编码设置纳入兜底：个别宿主没有标准输入句柄时 setter 会抛，裸崩不如走友好错误处理
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            // 引擎自更新残留清理：上一次「.old + 新 exe 落位」留下的旧版本文件，
            // 运行中的新 exe 不锁它，正常都能删掉；删不掉（仍被占用）静默跳过下次再试
            if (Environment.ProcessPath is { } currentExe)
                PackResolver.CleanupEngineUpdateResidue(currentExe);


            // 内容包解析只决定来源（--pack / 缓存 / 内嵌兜底，缓存与内嵌谁新用谁），绝不产生网络副作用；
            // 更新检查在交互模式启动时统一做一次（见 TryCheckUpdates）
            var packArgs = args;
            var resolution = PackResolver.Resolve(packArgs);
            foreach (var line in resolution.Logs)
                Console.WriteLine(line);

            var resources = new ResourceStore(resolution.Provider);
            var manifest = new ManifestLoader(resources).Load();

            // 统一版本口径：exe 版本与内容包版本同源（manifest.vars.version，CI 构建经
            // InformationalVersion 注入）。程序内读回：CI 产物 = 发布版本；本地开发构建未注入
            // 时为空显示 dev（dev 解析失败视为不比任何远端版本新，不触发自更新）；
            // USE_AI_RULE_ENGINE_VERSION 仅供本地调试覆盖显示
            var engineVersion = Environment.GetEnvironmentVariable("USE_AI_RULE_ENGINE_VERSION");
            if (string.IsNullOrWhiteSpace(engineVersion))
            {
                var informational = Assembly.GetExecutingAssembly()
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                engineVersion = string.IsNullOrWhiteSpace(informational) ? "dev" : informational;
            }

            // 正常状态两个版本相等只显一个；不等说明上次更新只部分生效（如引擎已换、内容包
            // 下载失败），此时如实展示双版本——启动更新检查会自动把落后的一方补齐
            Console.WriteLine(engineVersion == manifest.Vars.Version
                ? $"use-ai-rule {engineVersion}（{manifest.Vars.LastUpdated}）"
                : $"use-ai-rule {engineVersion}（内容包 {manifest.Vars.Version}，{manifest.Vars.LastUpdated}，版本不一致：启动更新检查将尝试补齐）");

            // 带参模式走命令行分发（--check / --emit / --plan / --detect / --upgrade / --recipe 等），
            // 不做交互与更新检查，避免脚本调用出网络副作用
            if (StripPackArgs(ref packArgs))
            {
                var skills = new SkillsStore(resolution.Provider);
                return RunCommand(packArgs, manifest, resources, skills);
            }

            // 启动统一更新检查（仅交互模式；带参命令与 --offline / --pack 模式绝不联网）：
            // 一次 latest.json 拉取，统一版本同时对照 exe 与生效内容两个基准，弹一次确认（默认更新）。
            // 安装成功后自动重启生成器——exe 已原子替换落位、内容包已切缓存指针，
            // 重启拉起的进程即刻用上新程序与新内容，避免继续按启动时加载的旧内容生成、
            // 把配方版本回写降级；重启后版本一致不再提示，天然防循环。
            // stdin 重定向（管道冒烟 / CI）只报告不更新：自动化场景不弹窗、不下载、不重启
            if (!args.Contains("--offline", StringComparer.OrdinalIgnoreCase) && resolution.Source != "pack")
            {
                // 检查包一层转圈：latest.json 拉取最多阻塞 3 秒，无反馈时启动看起来像卡死；
                // 降级说明（[pack] 更新检查超时…）在动画收尾后输出，纯文本场景不插帧
                IConsoleUi startupUi = CreateUi();
                var updateLogs = new List<string>();
                var offer = startupUi.WithProgress(
                    "检查更新中…",
                    () => PackResolver.TryFetchUpdateOffer(engineVersion, manifest.Vars.Version, updateLogs));
                foreach (var line in updateLogs)
                    Console.WriteLine(line);

                if (offer is { HasAnyUpdate: true })
                {
                    if (Console.IsInputRedirected)
                    {
                        Console.WriteLine($"[update] 检测到更新：{offer.Describe()}（非交互输入，跳过更新）");
                    }
                    else
                    {
                        var installed = PackResolver.ApplyUpdates(
                            offer,
                            o => AnsiConsole.Confirm(
                                $"发现更新：{o.Describe()}，是否更新？",
                                defaultValue: true));
                        if (installed)
                        {
                            Console.WriteLine("[update] 更新完成，即将自动重启生成器（重启后直接继续操作）…");
                            RestartSelf();
                        }
                    }
                }
            }


            var interactiveSkills = new SkillsStore(resolution.Provider);
            return RunInteractive(manifest, resources, interactiveSkills);
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

    // 剥离内容包开关（--pack <路径> / --offline），返回剩余业务参数；
    // 业务命令不接受这两个开关，避免拼错被静默吞掉
    private static bool StripPackArgs(ref string[] args)
    {
        if (args.Length == 0)
            return false;

        var remaining = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].Equals("--offline", StringComparison.OrdinalIgnoreCase))
                continue;
            if (args[i].Equals("--pack", StringComparison.OrdinalIgnoreCase))
            {
                i++; // 跳过路径参数
                continue;
            }
            remaining.Add(args[i]);
        }

        args = remaining.ToArray();
        return args.Length > 0;
    }

    // 内容包更新安装成功后的进程自重启：以原 exe 路径、无参数重新拉起交互入口，
    // 新进程从缓存重新 Resolve（加载刚更新的内容包），随后退出当前进程。
    // 仅交互模式可达此处（带参模式不联网检查），无需透传命令行参数；
    // 拉起失败降级为提示用户手动重启，不静默继续旧内容
    private static void RestartSelf()
    {
        var exe = Environment.ProcessPath;
        if (exe is null || !File.Exists(exe))
        {
            Console.WriteLine("[pack] 告警：无法定位生成器可执行文件，请手动重新启动以启用新内容包。");
            return;
        }

        try
        {
            using var process = System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo
                {
                    FileName = exe,
                    UseShellExecute = false,
                    WorkingDirectory = Environment.CurrentDirectory
                });
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[pack] 告警：自动重启失败（{ex.Message}），请手动重新启动以启用新内容包。");
        }
    }

    // 交互主流程（组件口径）：问答 / 检测 / 经典大类全部产出 Selection 走组合内核渲染；
    // 旧 Profile 渲染路径与 profiles 节点已整体退役，组合内核是唯一渲染路径。
    // 更新检查已在 Main 启动阶段统一完成（一次 latest.json 对照 exe 与生效内容两个基准），
    // 本流程不再产生网络请求
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

        // 顶层模式选择：常用路径仍是生成规则文件；已有自维护规则的项目可只拿内置 Skills
        var mode = ui.Choose(
            "请选择要执行的操作：",
            "取消",
            new List<UiChoice>
            {
                new("生成规则文件（含可选 Skills 导入）", "检测 / 问答选择组合并释放 AGENTS.md 等规则文件"),
                new("检测当前目录组合（仅查看）", "只检测项目技术栈并展示推荐或待确认组合，不写入文件"),
                new("仅导入 Skills", "不生成规则文件，只把内置 Skills 导入到 Codex / ZCode / Claude Code / pi")
            });
        if (mode is null)
        {
            Console.WriteLine("已取消。");
            return 0;
        }

        if (mode == 1)
            return DetectInteractive(manifest, ui, Environment.CurrentDirectory);


        if (mode == 2)
            return ImportSkillsOnly(ui, skillsStore, manifest);

        // 交互模式同样尊重已有配方：当前目录存在 .ai-rule-profile.json 时优先提供按配方重放，
        // 避免第二次使用仍走目录检测误判（嵌套子目录检测不到特征文件时尤其明显）
        var recipePath = Path.Combine(Environment.CurrentDirectory, RecipeRecorder.RecordFileName);
        if (File.Exists(recipePath))
        {
            var replayChoice = ui.Choose(
                $"检测到已有配方记录 {RecipeRecorder.RecordFileName}（上次生成的选择留档）：",
                "取消",
                new List<UiChoice>
                {
                    new("按配方重放（推荐）", "沿用上次选择的组合与能力升级规则，可识别本地修改"),
                    new("重新选择组合", "忽略配方，重新检测 / 问答选择后生成（会覆盖配方）")
                });
            if (replayChoice is null)
            {
                Console.WriteLine("已取消。");
                return 0;
            }
            if (replayChoice == 0)
                return RunUpgrade(manifest, resources, skillsStore, Environment.CurrentDirectory, force: false, interactive: true);
        }

        while (true)
        {
            var entry = InteractiveFlow.OfferEntry(manifest, wizard, ui);
            if (entry is null)
            {
                Console.WriteLine("已取消。");
                return 0;
            }

            List<PlannedFile> files;
            string summaryLabel;
            RecipeRecord record;
            string stackKey;
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

            var features = InteractiveFlow.ChooseFeaturesForPlan(manifest, ui, plan);
            files = renderer.Plan(manifest, plan, features);
            summaryLabel = InteractiveFlow.DescribeCompositionLabel(manifest, plan);
            stackKey = RecipeRecorder.StackKeyForComposition(manifest, plan);
            record = RecipeRecorder.BuildComposition(
                manifest, entry.Selection, plan.ComponentIds, entry.Mode,
                entry.Answers, entry.AnswerSources, features, files);

            // 已定制的设计系统文件属于产品资产：本次计划包含 design-system.yaml 且本地已是定制版时，保留本地不覆盖
            if (files.Any(file => file.Output.Equals("design-system.yaml", StringComparison.OrdinalIgnoreCase))
                && DesignSystemGuard.ShouldPreserve(Environment.CurrentDirectory))
            {
                // 保留前比对技术栈锚点：本地文件疑似旧组合基线时告警提示重置，不静默保留
                var baselineContent = files
                    .First(file => file.Output.Equals("design-system.yaml", StringComparison.OrdinalIgnoreCase))
                    .Content;
                var stackWarning = DesignSystemGuard.GetTechStackMismatchWarning(Environment.CurrentDirectory, baselineContent);
                files = files
                    .Where(file => !file.Output.Equals("design-system.yaml", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                Console.WriteLine("检测到已定制的 design-system.yaml：保留本地版本，本次不覆盖（如需重置基线，删除该文件后再生成）");
                if (stackWarning is not null)
                    Console.WriteLine(stackWarning);
            }

            Console.WriteLine();
            Console.WriteLine($"已选择：{summaryLabel}");
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
            var skillImport = SkillsImportFlow.OfferSkillsImport(ui, skillsStore, manifest, entry.Selection);
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

    // 配方升级共享实现：--upgrade 命令行与交互模式「按配方重放」周同一口径，
    // 避免两条入口各自演化导致行为漂移；interactive=true 时写入前再确认一次
    private static int RunUpgrade(
        Manifest manifest, ResourceStore resources, SkillsStore skillsStore,
        string targetDirectory, bool force, bool interactive)
    {
        var recipePath = Path.Combine(targetDirectory, RecipeRecorder.RecordFileName);
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

        if (plan.ModifiedFiles.Count > 0 && plan.HashDetectable && force)
        {
            // --force / 交互选择覆盖：被保留的修改文件重新纳入写入集合，配方按全新写入重算哈希
            plan = new RecipeReplayer.UpgradePlan(
                replay, plan.ModifiedFiles, new List<string>(), replay.Files, plan.HashDetectable, replay.Record);
        }

        Console.WriteLine($"将写入 {plan.FilesToWrite.Count} 个文件到 {targetDirectory}" +
                          (plan.SkippedFiles.Count > 0 ? $"（跳过本地修改的 {plan.SkippedFiles.Count} 个）" : "") + "：");
        foreach (var file in plan.FilesToWrite)
            Console.WriteLine($"  - {file.Output}");

        if (interactive)
        {
            if (plan.SkippedFiles.Count > 0)
            {
                // 有被跳过的本地修改文件时给用户二次选择：目标项目有 git 兜底时可直接覆盖，不行再撤回
                var overwrite = new PlainConsoleUi().Choose(
                    $"检测到 {plan.SkippedFiles.Count} 个本地修改过的文件将被保留（跳过可能导致规则停留在旧版）：",
                    "取消，不写入",
                    new List<UiChoice>
                    {
                        new("保留本地修改并升级其余文件", "跳过的文件不动，只更新未修改文件"),
                        new("全部覆盖为最新版", "本地修改的文件也重写为当前版本规则；如有本地定制请先自行备份或依赖 git 撤回")
                    });
                if (overwrite is null)
                {
                    Console.WriteLine("已取消。");
                    return 0;
                }
                if (overwrite == 1)
                {
                    plan = new RecipeReplayer.UpgradePlan(
                        replay, plan.ModifiedFiles, new List<string>(), replay.Files, plan.HashDetectable, replay.Record);
                }
            }
            else
            {
                var confirm = new PlainConsoleUi().Choose(
                    $"确认写入 {plan.FilesToWrite.Count} 个文件到 {targetDirectory}？",
                    "取消",
                    new List<UiChoice> { new("开始升级", "按配方重放并写入最新版规则") });
                if (confirm is null)
                {
                    Console.WriteLine("已取消。");
                    return 0;
                }
            }
        }

        var written = new OutputWriter().WriteFiles(targetDirectory, plan.FilesToWrite);
        var skillWritten = SkillsImportFlow.ImportRecordedSkills(targetDirectory, plan.Record, skillsStore);
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

    // 仅导入 Skills：不生成规则文件、不写配方记录（配方是组合口径，空组合会让 --upgrade / --recipe
    // 尝试渲染规则文件，违背只要技能的意图）；更新技能重新运行导入即可（同名技能先删后写）。
    // gitignore 按 minimal 处理：仅导入模式不知道技术栈，取最保守模板；已存在时幂等追加 AI 工具目录忽略
    private static int ImportSkillsOnly(IConsoleUi ui, SkillsStore skillsStore, Manifest manifest)
    {
        var skillImport = SkillsImportFlow.OfferSkillsImport(ui, skillsStore, manifest, context: null, confirmEntry: false);
        if (skillImport is null)
        {
            Console.WriteLine("未导入任何 Skills。");
            return 0;
        }

        Console.WriteLine($"导入 Skills 到 {Environment.CurrentDirectory}：");
        var skillWritten = new SkillImporter().Import(
            Environment.CurrentDirectory, skillImport.Targets, skillImport.Skills, skillsStore, skillImport.SkipKeys);
        var gitignoreMessage = new RecipeRecorder().EnsureGitignored(Environment.CurrentDirectory, "minimal");

        Console.WriteLine($"完成：已导入 Skills {skillWritten} 个文件" +
            "（仅导入模式不生成规则文件、不写配方记录，更新技能时重新运行导入即可，同名技能先删后写）");
        Console.WriteLine(gitignoreMessage);
        return 0;
    }

    // 启动菜单中的只读检测：与 --detect 共用同一输出逻辑，避免交互入口和命令行入口口径漂移
    private static int DetectInteractive(Manifest manifest, IConsoleUi ui, string directory)
    {
        var result = new ProjectDetector(manifest).Detect(directory);
        if (result is null)
        {
            ui.Line($"未能在 {directory} 识别出已接入的技术栈。");
            return 1;
        }

        ui.Head("已检测当前目录，判断依据：");
        ui.Bullets(result.Reasons);
        ui.Bullets(result.Notes);
        ui.Line("");

        if (result.Converged)
        {
            ui.Line($"推荐组合：{result.SampleLabel}（{result.SampleProfileId}）");
            ui.Line("回答组合：" + string.Join(
                "，",
                result.BuildSampleAnswers().Select(pair => $"{pair.Key}={pair.Value}")));
            return 0;
        }

        ui.Line("检测未收敛，以下维度已确认：" + string.Join(
            "，",
            result.Confirmed.Select(pair => $"{pair.Key}={pair.Value}")));
        foreach (var trait in result.Unconfirmed)
            ui.Line($"待确认维度：{trait}");
        ui.Line("候选组合：");
        foreach (var candidate in result.Candidates)
            ui.Line($"  - {candidate.Label}（{candidate.SampleProfileId}）");
        return 0;
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

    private static int RunCommand(string[] args, Manifest manifest, ResourceStore resources, SkillsStore skillsStore)
    {
        if (args.Length == 1 && args[0].Equals("--check", StringComparison.OrdinalIgnoreCase))
            return CheckCommand.Run(manifest, resources, skillsStore);

        if (args.Length is 1 or 2 && args[0].Equals("--detect", StringComparison.OrdinalIgnoreCase))
        {
            var dir = args.Length == 2 ? args[1] : Environment.CurrentDirectory;
            return DetectInteractive(manifest, new PlainConsoleUi(), dir);
        }

        // 按配方升级已有规则：--upgrade [目录] [--force]。
        // 读取目标目录的 .ai-rule-profile.json 重放生成；配方带内容哈希时识别本地修改过的文件，
        // 默认保留本地版本只更新未修改文件（--force 全部覆盖），回写配方刷新版本与哈希
        if (args.Length >= 1 && args[0].Equals("--upgrade", StringComparison.OrdinalIgnoreCase))
        {
            var force = args.Contains("--force", StringComparer.Ordinal);
            var dirArg = args.Skip(1).FirstOrDefault(arg => !arg.Equals("--force", StringComparison.Ordinal));
            var targetDirectory = dirArg is null ? Environment.CurrentDirectory : Path.GetFullPath(dirArg);

            // 除 --force 与目录外不接受其他参数：拼错的开关被静默吞掉会让用户误以为已启用覆盖
            var extras = args.Skip(1)
                .Where(arg => !arg.Equals("--force", StringComparison.Ordinal)
                    && !string.Equals(arg, dirArg, StringComparison.Ordinal))
                .ToList();
            if (extras.Count > 0)
                throw new ArgumentException(
                    $"--upgrade 存在无法识别的参数：{string.Join(" ", extras)}（用法：--upgrade [目录] [--force]）",
                    nameof(args));
            var recipeUpgradePath = Path.Combine(targetDirectory, RecipeRecorder.RecordFileName);
            if (!File.Exists(recipeUpgradePath))
                throw new IOException($"未找到配方记录 {recipeUpgradePath}：升级需要此前由生成器写入的配方文件，首次生成请直接运行生成器或使用 --recipe。");
            return RunUpgrade(manifest, resources, skillsStore, targetDirectory, force, interactive: false);
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
            var skillWritten = SkillsImportFlow.ImportRecordedSkills(targetDirectory, result.Record, skillsStore);
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

        if (args.Length == 2 && args[0].Equals("--emit", StringComparison.OrdinalIgnoreCase))
        {
            // emit 走组合路径：每个兼容样例经组合内核渲染（目录名沿用样例 ID，即旧 Profile ID）
            var outDir = Path.GetFullPath(args[1]);
            var renderer = new RuleRenderer(resources);
            var planner = new CompositionPlanner(manifest);
            var plannedCases = new List<(CompatibilityCase Case, List<PlannedFile> Files)>();
            foreach (var caseItem in manifest.CompatibilityCases)
            {
                var selection = new Selection
                {
                    Dimensions = new Dictionary<string, string>(caseItem.Selection, StringComparer.Ordinal)
                };
                var plan = planner.Plan(selection);
                var files = renderer.Plan(manifest, plan, FeatureSelection.Defaults(manifest, plan));
                plannedCases.Add((caseItem, files));
            }

            Directory.CreateDirectory(outDir);
            var allFiles = plannedCases
                .SelectMany(item => item.Files.Select(file => new PlannedFile
                {
                    Output = $"{item.Case.Profile}/{file.Output}",
                    Content = file.Content
                }))
                .ToList();
            new OutputWriter().WriteFiles(outDir, allFiles);
            foreach (var item in plannedCases)
                Console.WriteLine($"已输出：{item.Case.Profile}（{item.Files.Count} 个文件）");

            Console.WriteLine($"输出完成：{manifest.CompatibilityCases.Count} 个兼容样例 → {outDir}");
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

            // 先点名未知维度名：直接放行会被结构锚定校验拦下，但报错会甩锅给不相干的维度
            var knownTraits = manifest.Wizard.Steps.Select(step => step.Trait).ToList();
            var unknownTraits = dimensions.Keys
                .Where(key => !knownTraits.Contains(key, StringComparer.Ordinal))
                .ToList();
            if (unknownTraits.Count > 0)
                throw new ArgumentException(
                    $"--plan 未知维度名：{string.Join("、", unknownTraits)}（有效维度：{string.Join("、", knownTraits)}）",
                    nameof(args));

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
            Console.WriteLine("直接双击或不带参数运行：先选模式——生成规则文件（自动检测推荐 / 问答选择）、检测当前目录组合，或仅导入 Skills；已存在配方记录时优先生成「按配方重放」入口。");
            Console.WriteLine("规则文件落盘后可选择把内置 Skills 导入到 Codex（.codex/skills）/ ZCode（.zcode/skills）/ Claude Code（.claude/skills）/ pi（.pi/skills），追加导入、同名技能可覆盖或跳过；--recipe / --upgrade 按配方同步已记录的 Skills。");
            Console.WriteLine("--check：只校验 manifest、片段、全部兼容样例的输出契约一致性、问答决策树、组件问答决策树与内置技能，不写入文件。");
            Console.WriteLine("--detect [目录]：只做项目检测，打印推荐组合；证据不足时打印待确认维度与候选组合，不写入文件。");
            Console.WriteLine("--plan 维度=取值 ...：按显式维度选择解析组合内核，打印命中组件与文件清单，不写入文件。");
            Console.WriteLine("--recipe <配方.json> [目标目录]：按配方记录重放生成（缺省写入当前目录），写入配方与 .gitignore。");
            Console.WriteLine("--upgrade [目录] [--force]：按目标目录的配方记录升级规则；识别本地修改默认保留，--force 全部覆盖。");
            Console.WriteLine("--emit <目录>：把全部兼容样例的组合路径生成结果输出到指定目录，用于批量预览与产物回归。");
            Console.WriteLine("--pack <内容包.zip|目录>：使用指定内容包（zip 或已解包目录），校验失败直接报错退出。");
            Console.WriteLine("--offline：跳过网络更新检查，使用本地缓存或内嵌资源。");
            Console.WriteLine("--help：显示帮助。");
            return 0;
        }

        throw new ArgumentException($"不支持的参数：{string.Join(" ", args)}。请直接运行，或使用 --check。", nameof(args));
    }
}
