namespace RuleAssembler;

// 技能多选的展示分组：Title 是组文案，SkillIndices 指向全量技能清单的下标；
// 每个技能恰好属于一个分组（未登记的归「其他」），组顺序 = manifest 首次出现顺序
internal sealed record SkillGroupChoice(string Title, IReadOnlyList<int> SkillIndices);

// 技能目录：分组构建与按组合推荐预选。全部口径只依赖 manifest.Skills 与本次维度选择，
// 检测 / 问答 / 经典入口产出的 Selection 都能直接作为推荐输入
internal static class SkillCatalog
{
    internal const string FallbackGroup = "其他";

    // 推荐预选：when 缺省的技能恒推荐；带 when 的技能按「任一条件命中」推荐。
    // 维度未确认（Selection 缺该维度）视为证据不足，不推荐
    public static IReadOnlyList<int> Recommend(
        Manifest manifest, Selection? selection, IReadOnlyList<SkillInfo> skills)
    {
        if (selection is null || manifest.Skills.Count == 0)
            return [];

        var dimensions = selection.Dimensions;
        return skills
            .Select((skill, index) => (skill, index))
            .Where(pair => manifest.Skills.Any(def =>
                def.Id.Equals(pair.skill.Id, StringComparison.OrdinalIgnoreCase)
                && Matches(def, dimensions)))
            .Select(pair => pair.index)
            .ToList();
    }

    // 条件命中：多组条件任一组命中即可（OR）；组内全部维度命中（AND），维度取值任一命中（any-of）
    public static bool Matches(SkillDefinition skill, IReadOnlyDictionary<string, string> dimensions)
    {
        if (skill.When is null || skill.When.Count == 0)
            return true;

        return skill.When.Any(condition => condition.All(pair =>
            dimensions.TryGetValue(pair.Key, out var value)
            && pair.Value.Contains(value, StringComparer.OrdinalIgnoreCase)));
    }

    // 分组构建：manifest 登记顺序优先（group 首次出现顺序），未登记技能归「其他」兜底，
    // 保证任何技能都会出现且只出现一次
    public static IReadOnlyList<SkillGroupChoice> BuildGroups(
        Manifest manifest, IReadOnlyList<SkillInfo> skills)
    {
        var defs = manifest.Skills
            .Where(def => !string.IsNullOrWhiteSpace(def.Id))
            .GroupBy(def => def.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var groupTitles = manifest.Skills
            .Select(def => def.Group)
            .Where(title => !string.IsNullOrWhiteSpace(title))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var buckets = groupTitles.ToDictionary(
            title => title, _ => new List<int>(), StringComparer.Ordinal);
        var fallback = new List<int>();

        for (var index = 0; index < skills.Count; index++)
        {
            if (defs.TryGetValue(skills[index].Id, out var def)
                && buckets.TryGetValue(def.Group, out var bucket))
                bucket.Add(index);
            else
                fallback.Add(index);
        }

        var groups = groupTitles
            .Where(buckets.ContainsKey)
            .Select(title => new SkillGroupChoice(title, buckets[title]))
            .ToList();
        if (fallback.Count > 0)
            groups.Add(new SkillGroupChoice(FallbackGroup, fallback));
        return groups;
    }
}
