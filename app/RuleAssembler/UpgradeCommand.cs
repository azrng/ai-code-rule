namespace RuleAssembler;

// --upgrade [目录] [--force]：按目标目录的配方记录（.ai-rule-profile.json）重放升级。
// 配方带内容哈希时识别本地修改过的文件，默认保留本地版本只更新未修改文件
// （--force 全部覆盖），回写配方刷新版本与哈希
internal static class UpgradeCommand
{
    internal static int Run(string[] args, Manifest manifest, ResourceStore resources, SkillsStore skillsStore)
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
        var recipePath = Path.Combine(targetDirectory, RecipeRecorder.RecordFileName);
        if (!File.Exists(recipePath))
            throw new IOException($"未找到配方记录 {recipePath}：升级需要此前由生成器写入的配方文件，首次生成请直接运行生成器或使用 --recipe。");
        // 命令行模式不触发交互确认（interactive=false），传入纯文本实现仅作兜底
        return UpgradeFlow.Run(manifest, resources, skillsStore, new PlainConsoleUi(), targetDirectory, force, interactive: false);
    }
}
