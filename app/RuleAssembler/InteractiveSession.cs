namespace RuleAssembler;

// 交互主流程（组件口径）：问答 / 检测 / 经典大类全部产出 Selection 走组合内核渲染（唯一渲染路径）；
// 更新检查已在 Program 启动阶段统一完成，本流程不再产生网络请求
internal static class InteractiveSession
{
    internal const string Banner = "AI Rule 拼接器 — 把一套完整 AI rule 释放到当前目录";

    internal static int Run(Manifest manifest, ResourceStore resources, SkillsStore skillsStore)
    {
        Console.WriteLine(Banner);
        Console.WriteLine($"当前目录：{Environment.CurrentDirectory}");
        Console.WriteLine("（菜单为键盘交互：方向键 / 编号 + 回车，不支持鼠标点击）");
        Console.WriteLine();

        // 交互实现只在入口处决定一次：stdin 重定向（管道冒烟/CI）强制走纯文本编号输入；
        // 终端不支持 ANSI 光标控制时 SelectionPrompt 无法擦除上一帧（旧版 conhost 会叠屏），同样回退编号输入
        IConsoleUi ui = Program.CreateUi();
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
            return DetectCommand.Run(manifest, ui, Environment.CurrentDirectory);


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
                return UpgradeFlow.Run(
                    manifest, resources, skillsStore, ui, Environment.CurrentDirectory, force: false, interactive: true);
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
}
