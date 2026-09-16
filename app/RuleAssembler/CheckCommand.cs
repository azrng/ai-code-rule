namespace RuleAssembler;

// --check：manifest 装载后的产物级守门校验，只读不写入；依次跑输出契约、
// 组件问答决策树、内置技能、组合等价四段，任何一段失败即非零退出
internal static class CheckCommand
{
    internal static int Run(Manifest manifest, ResourceStore resources, SkillsStore skillsStore)
    {
        var renderer = new RuleRenderer(resources);
        var validator = new RootConsistencyValidator();
        var planner = new CompositionPlanner(manifest);

        // 契约校验走组合口径：每个 Profile 经其兼容样例反查组合渲染产物（装载校验保证每个
        // Profile 恰好一个样例）；同一份 plan 也供下方等价比对消费，避免双重解析。
        // 旧 Profile 渲染路径在本命令里只剩等价比对一项职责
        var contractErrors = new List<string>();
        var plansByProfile = new Dictionary<string, (CompositionPlan Plan, HashSet<string> Features)>(
            StringComparer.OrdinalIgnoreCase);
        foreach (var profile in manifest.Profiles)
        {
            var caseItem = manifest.CompatibilityCases.FirstOrDefault(item =>
                item.Profile.Equals(profile.Id, StringComparison.OrdinalIgnoreCase));
            if (caseItem is null)
            {
                contractErrors.Add($"Profile {profile.Id} 缺少兼容样例，无法走组合路径校验");
                continue;
            }

            var selection = new Selection
            {
                Dimensions = new Dictionary<string, string>(caseItem.Selection, StringComparer.Ordinal)
            };
            var plan = planner.Plan(selection);
            var features = FeatureSelection.Defaults(manifest, plan);
            plansByProfile[profile.Id] = (plan, features);

            var files = renderer.Plan(manifest, plan, features);
            var errors = validator.Validate(manifest, renderer, plan, features, profile.Id);
            // 产物级重复标题：同文件内 ## / ### 标题重复即 include 叠加或骨架残留的症状
            foreach (var file in files)
            {
                var duplicatedHeadings = HeadingDuplicationValidator.FindDuplicateHeadings(file.Content);
                foreach (var heading in duplicatedHeadings)
                    errors.Add($"{profile.Id}/{file.Output} 存在重复标题：{heading}");
            }

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

        // 组合内核兼容样例：组合路径与旧 Profile 产物逐字节等价，等价被破坏时 --check 失败
        // （这是旧 Profile 渲染路径仅存的生产职责；plan 已在契约段解析，这里直接复用）
        if (manifest.CompatibilityCases.Count > 0)
        {
            var compositionErrors = new List<string>();
            foreach (var item in manifest.CompatibilityCases)
            {
                var profile = manifest.Profiles.First(candidate =>
                    candidate.Id.Equals(item.Profile, StringComparison.OrdinalIgnoreCase));
                try
                {
                    var (plan, features) = plansByProfile[profile.Id];
                    var legacyFiles = renderer.Plan(manifest, profile);
                    var composeFiles = renderer.Plan(manifest, plan, features);
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
