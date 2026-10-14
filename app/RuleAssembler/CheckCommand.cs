namespace RuleAssembler;

// --check：manifest 装载后的产物级守门校验，只读不写入；依次跑输出契约（组合口径）、
// 组件问答决策树、内置技能三段，任何一段失败即非零退出。
// 旧 Profile 渲染路径与新旧等价比对已随 profiles 节点退役，组合内核是唯一渲染路径
internal static class CheckCommand
{
    internal static int Run(Manifest manifest, ResourceStore resources, SkillsStore skillsStore)
    {
        var renderer = new RuleRenderer(resources);
        var validator = new RootConsistencyValidator();
        var planner = new CompositionPlanner(manifest);

        // 契约校验（组合口径）：每个兼容样例经组合内核解析并渲染产物，
        // 声明（模块 + 组件文件贡献 + 指针 + design-system）与实际产物必须一致
        var contractErrors = new List<string>();
        foreach (var caseItem in manifest.CompatibilityCases)
        {
            var selection = new Selection
            {
                Dimensions = new Dictionary<string, string>(caseItem.Selection, StringComparer.Ordinal)
            };
            var plan = planner.Plan(selection);
            var features = FeatureSelection.Defaults(manifest, plan);
            var files = renderer.Plan(manifest, plan, features);
            var errors = validator.Validate(manifest, renderer, plan, features, caseItem.Profile);
            // 产物级重复标题：同文件内 ## / ### 标题重复即 include 叠加或骨架残留的症状
            foreach (var file in files)
            {
                var duplicatedHeadings = HeadingDuplicationValidator.FindDuplicateHeadings(file.Content);
                foreach (var heading in duplicatedHeadings)
                    errors.Add($"{caseItem.Profile}/{file.Output} 存在重复标题：{heading}");
            }

            if (errors.Count == 0)
            {
                Console.WriteLine($"通过：{caseItem.Profile}（{files.Count} 个文件）");
                continue;
            }

            contractErrors.AddRange(errors);
            Console.WriteLine($"失败：{caseItem.Profile}（输出契约不一致）");
        }

        if (contractErrors.Count > 0)
        {
            Console.Error.WriteLine("输出契约校验失败：");
            foreach (var error in contractErrors)
                Console.Error.WriteLine($"- {error}");
            return 1;
        }

        // 组件问答决策树仿真：每条交互可达路径必须结构完备可解析（约束 + 组件锚定），
        // 映射兼容样例时必须唯一（无样例映射的样例外组合合法），全部样例可达
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

        Console.WriteLine($"校验完成：{manifest.CompatibilityCases.Count} 个兼容样例全部通过。");
        return 0;
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
}
