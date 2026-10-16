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

        // 内置技能校验：资源里必须能发现技能、ID 全局唯一、每个 SKILL.md 带 description 供选择列表展示；
        // manifest.Skills 登记的技能必须真实存在、group 非空、when 引用的维度与取值可被问答表达
        var skillErrors = ValidateSkills(manifest, skillsStore);
        if (skillErrors.Count > 0)
        {
            Console.Error.WriteLine("内置技能校验失败：");
            foreach (var error in skillErrors)
                Console.Error.WriteLine($"  - {error}");
            return 1;
        }

        Console.WriteLine($"内置技能校验通过：{skillsStore.Discover().Count} 个技能，分组与推荐登记齐全。");
        Console.WriteLine($"校验完成：{manifest.CompatibilityCases.Count} 个兼容样例全部通过。");
        return 0;
    }

    // --check 的技能资源校验：发现数 > 0、ID 不冲突（忽略大小写，目录名跨平台大小写不敏感）、
    // 每个 SKILL.md 有 frontmatter description；manifest.Skills 登记必须与可发现技能对得上：
    // 登记的 ID 必须存在、group 不能为空、when 引用的维度与取值必须可被问答/检测表达，
    // 保证分组多选与推荐预选在任何机器上都不会因登记漂移而静默失效
    private static List<string> ValidateSkills(Manifest manifest, SkillsStore skillsStore)
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

        var discoveredIds = skills
            .Select(skill => skill.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var registered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var def in manifest.Skills)
        {
            if (string.IsNullOrWhiteSpace(def.Id))
            {
                errors.Add("skills 条目缺少 id");
                continue;
            }

            if (string.IsNullOrWhiteSpace(def.Group))
                errors.Add($"技能 {def.Id} 的 skills.group 不能为空");
            if (def.Id.Contains('/'))
                errors.Add($"技能 {def.Id} 的 skills.id 必须是技能短 ID（不含目录前缀），目录前缀只用于维护归档");
            if (registered.Contains(def.Id))
                errors.Add($"技能 {def.Id} 在 skills 中重复登记");
            registered.Add(def.Id);

            if (!discoveredIds.Contains(def.Id))
                errors.Add($"skills 登记的技能 {def.Id} 不存在于内置技能（ID 取含 SKILL.md 的最外层目录名）");

            foreach (var condition in def.When ?? [])
            foreach (var (trait, values) in condition)
            {
                if (!manifest.Wizard.Steps.Any(step => step.Trait == trait))
                {
                    errors.Add($"技能 {def.Id} 的 when 引用了未知维度：{trait}");
                    continue;
                }

                foreach (var value in values.Where(value => string.IsNullOrWhiteSpace(value)))
                    errors.Add($"技能 {def.Id} 的 when.{trait} 含空取值");
                if (manifest.Wizard.Options.TryGetValue(trait, out var options))
                    foreach (var value in values.Where(value => !options.ContainsKey(value)))
                        errors.Add($"技能 {def.Id} 的 when.{trait}={value} 缺少 wizard.options 文案");
            }
        }

        return errors;
    }
}
