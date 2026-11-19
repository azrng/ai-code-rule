namespace RuleAssembler;

// 配方升级共享实现：--upgrade 命令行与交互模式「按配方重放」走同一口径，
// 避免两条入口各自演化导致行为漂移；ui 由调用方传入（交互模式共享方向键 UI，
// 命令行模式传纯文本实现——interactive=false 不触发确认，仅作兜底）；interactive=true 时写入前再确认一次
internal static class UpgradeFlow
{
    internal static int Run(
        Manifest manifest, ResourceStore resources, SkillsStore skillsStore, IConsoleUi ui,
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
                var overwrite = ui.Choose(
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
                var confirm = ui.Choose(
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
}
