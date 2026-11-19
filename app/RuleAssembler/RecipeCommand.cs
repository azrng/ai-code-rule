namespace RuleAssembler;

// --recipe <配方.json> [目标目录]：按配方记录重放生成（缺省写入当前目录），写入配方与 .gitignore
internal static class RecipeCommand
{
    internal static int Run(string[] args, Manifest manifest, ResourceStore resources, SkillsStore skillsStore)
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
}
