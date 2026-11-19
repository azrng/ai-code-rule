namespace RuleAssembler;

// --emit <目录>：把全部兼容样例的组合路径生成结果输出到指定目录，用于批量预览与产物回归
internal static class EmitCommand
{
    internal static int Run(string outDirArg, Manifest manifest, ResourceStore resources)
    {
        // emit 走组合路径：每个兼容样例经组合内核渲染（目录名沿用样例 ID，即旧 Profile ID）
        var outDir = Path.GetFullPath(outDirArg);
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
}
