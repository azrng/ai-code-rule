namespace RuleAssembler;

// 显式维度选择预览：--plan form=web frontend=react backend=dotnet ...
// 与问答同一套推导：唯一取值自动补全，仍有分歧的维度报错并列出可选值；只打印不写入
internal static class PlanCommand
{
    internal static int Run(string[] args, Manifest manifest, ResourceStore resources)
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
}
