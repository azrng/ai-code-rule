namespace RuleAssembler;

// Skills 导入流：规则文件落盘后的可选导入勾选（交互路径）与配方技能同步（重放 / 升级路径）
internal static class SkillsImportFlow
{
    // Skills 导入勾选结果：技能与目标都是多选；SkipKeys 是「<targetId>/<skillId>」的跳过集合（保留本地同名技能）；
    // null 表示用户跳过导入（不写入配方）
    internal sealed record SkillImportSelection(
        IReadOnlyList<SkillInfo> Skills,
        IReadOnlyList<SkillTarget> Targets,
        IReadOnlySet<string>? SkipKeys);

    // Skills 导入（可选步骤）：规则文件落盘后追加；技能分组多选（按本次组合预选推荐）→ 目标工具多选（默认全勾）。
    // 导入是追加语义：目标目录已有同名技能（含 SKILL.md）时先问覆盖还是跳过，未选中的技能一律不动；
    // 覆盖 = 同名技能目录先删后写、按内置内容整体重写；任何一步取消或零勾选都视为跳过导入，配方不记录技能字段。
    // confirmEntry = false 供「仅导入 Skills」入口使用：用户已在顶层表达导入意图，跳过入口确认直接进技能多选
    // context = null（仅导入 Skills 入口）时不做推荐预选：入口菜单没走组合流程，没有可依据的维度选择
    internal static SkillImportSelection? OfferSkillsImport(
        IConsoleUi ui,
        SkillsStore skillsStore,
        Manifest manifest,
        Selection? context = null,
        bool confirmEntry = true)
    {
        var skills = skillsStore.Discover();
        if (skills.Count == 0)
            return null;

        if (confirmEntry)
        {
            var entry = ui.Choose(
                "是否导入 AI Skills（内置于生成器，免手动拷贝）？",
                "跳过，不导入",
                new List<UiChoice>
                {
                    new("选择 Skills 并导入", $"{skills.Count} 个内置技能，可选导入到 Codex / ZCode / Claude Code / pi")
                });
            if (entry is null)
                return null;
        }

        // 推荐预选：按本次组合的维度选择推导（检测 / 问答 / 经典入口一致口径），无上下文时全不勾
        var recommended = SkillCatalog.Recommend(manifest, context, skills);
        var groups = SkillCatalog.BuildGroups(manifest, skills);
        if (groups.Count == 1 && groups[0].Title == SkillCatalog.FallbackGroup)
            Console.WriteLine($"[pack] 告警：内容包缺少技能登记，全部技能归入「其他」且无推荐预选；请升级内容包后重试（当前 {manifest.Vars.Version}）");
        var picked = ui.ChooseManyGrouped(
            recommended.Count > 0
                ? $"选择要导入的 Skills（已按技术栈预选 {recommended.Count} 个，可调整，可多选）："
                : "选择要导入的 Skills（可多选）：",
            skills.Select(skill => new UiChoice(skill.Id, TruncateDescription(skill.Description))).ToList(),
            groups,
            recommended);
        if (picked.Count == 0)
        {
            Console.WriteLine("未勾选任何 Skill，跳过导入。");
            return null;
        }

        var targetPicked = ui.ChooseMany(
            "选择导入到哪些工具（项目级目录，可多选）：",
            SkillTargets.All.Select(target => new UiChoice(target.Label, $"{target.RelativeRoot}/<skill>/")).ToList(),
            Enumerable.Range(0, SkillTargets.All.Count).ToList());
        if (targetPicked.Count == 0)
        {
            Console.WriteLine("未选择任何目标工具，跳过导入。");
            return null;
        }

        var selectedSkills = picked.Select(index => skills[index]).ToList();
        var selectedTargets = targetPicked.Select(index => SkillTargets.All[index]).ToList();

        // 同名冲突处置：追加导入的前提是同名技能显式确认；取消则整体放弃导入
        var conflicts = new SkillImporter().DetectConflicts(Environment.CurrentDirectory, selectedTargets, selectedSkills);
        IReadOnlySet<string>? skipKeys = null;
        if (conflicts.Count > 0)
        {
            Console.WriteLine($"检测到 {conflicts.Count} 个同名 Skills 已存在于目标目录：");
            foreach (var conflict in conflicts.Take(8))
            {
                var separator = conflict.IndexOf('/');
                Console.WriteLine($"  - [{conflict[..separator]}] {conflict[(separator + 1)..]}");
            }

            if (conflicts.Count > 8)
                Console.WriteLine($"  …等共 {conflicts.Count} 个");
            var overwrite = ui.Choose(
                "同名 Skills 如何处理？（未命中的新增技能照常导入）",
                "取消导入",
                new List<UiChoice>
                {
                    new("覆盖已存在的", "删除本地同名技能后按内置版本整体重写（清理上游已删改的残留文件）"),
                    new("跳过已存在的", "保留本地版本不动，只导入新增技能")
                });
            if (overwrite is null)
                return null;
            if (overwrite == 1)
                skipKeys = conflicts;
        }

        return new SkillImportSelection(selectedSkills, selectedTargets, skipKeys);
    }

    private static string TruncateDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return "（SKILL.md 未提供 description）";
        return description.Length <= 60 ? description : description[..60] + "…";
    }

    // 重放 / 升级路径的技能同步：配方记录了 skills + skill_targets 时按内置内容覆盖重放，
    // 技能或目标已不在当前版本生成器中时记告警跳过，不阻塞规则文件升级
    internal static int ImportRecordedSkills(string targetDirectory, RecipeRecord record, SkillsStore skillsStore)
    {
        if (record.Skills is not { Count: > 0 } || record.SkillTargets is not { Count: > 0 })
            return 0;

        var discovered = skillsStore.Discover();
        var skills = discovered
            .Where(skill => record.Skills.Contains(skill.Id, StringComparer.OrdinalIgnoreCase))
            .ToList();
        foreach (var missing in record.Skills.Where(skillId =>
            !discovered.Any(candidate => candidate.Id.Equals(skillId, StringComparison.OrdinalIgnoreCase))))
        {
            Console.WriteLine($"告警：配方记录的技能 {missing} 已不在当前版本生成器中，跳过");
        }

        var targets = record.SkillTargets
            .Select(SkillTargets.Find)
            .Where(target => target is not null)
            .Cast<SkillTarget>()
            .ToList();
        if (skills.Count == 0 || targets.Count == 0)
            return 0;

        Console.WriteLine($"按配方同步 Skills 到 {targetDirectory}（同名技能先删后写，按内置内容整体重写）：");
        return new SkillImporter().Import(targetDirectory, targets, skills, skillsStore);
    }
}
