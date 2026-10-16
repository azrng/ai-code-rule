using System.Text.Json;
using System.Text.Json.Serialization;

namespace RuleAssembler;

// skills 元数据：manifest 是技能展示分组与推荐预选的事实来源。
// ID 与 SkillInfo.Id 同一口径（含 SKILL.md 的最外层目录名，不含归档目录前缀）；
// when 缺省 = 任何组合都推荐；条件支持单组（全部维度命中）或多组（任一组命中）
internal sealed class SkillDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    // 展示分组文案；分组在技能多选中以树形组呈现
    [JsonPropertyName("group")]
    public string Group { get; set; } = "";

    // 推荐条件：维度 -> 任一命中取值清单；组内全部维度命中才算命中（AND + any-of）。
    // 单个条件对象或条件对象数组均可；数组语义为「任一条件命中即推荐」（OR）
    [JsonPropertyName("when")]
    [JsonConverter(typeof(SkillWhenConverter))]
    public List<Dictionary<string, List<string>>>? When { get; set; }
}

// skills.when 的多态解析：单条件对象或条件对象数组都归一化为条件清单；
// 条件内取值支持标量字符串或字符串数组（任一命中）
internal sealed class SkillWhenConverter : JsonConverter<List<Dictionary<string, List<string>>>>
{
    public override List<Dictionary<string, List<string>>>? Read(
        ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.StartObject:
                return [ReadCondition(ref reader)];
            case JsonTokenType.StartArray:
                var conditions = new List<Dictionary<string, List<string>>>();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    if (reader.TokenType != JsonTokenType.StartObject)
                        throw new JsonException("skills.when 数组的元素必须是条件对象");
                    conditions.Add(ReadCondition(ref reader));
                }

                return conditions;
            default:
                throw new JsonException("skills.when 必须是条件对象或条件对象数组");
        }
    }

    public override void Write(
        Utf8JsonWriter writer, List<Dictionary<string, List<string>>> value, JsonSerializerOptions options)
    {
        throw new NotSupportedException("manifest 只读，不需要序列化 skills.when");
    }

    private static Dictionary<string, List<string>> ReadCondition(ref Utf8JsonReader reader)
    {
        var condition = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            var trait = reader.GetString() ?? "";
            if (!reader.Read())
                throw new JsonException($"skills.when.{trait} 缺少取值");

            var values = new List<string>();
            if (reader.TokenType == JsonTokenType.String)
            {
                values.Add(reader.GetString() ?? "");
            }
            else if (reader.TokenType == JsonTokenType.StartArray)
            {
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    if (reader.TokenType != JsonTokenType.String)
                        throw new JsonException($"skills.when.{trait} 的取值必须是字符串");
                    values.Add(reader.GetString() ?? "");
                }
            }
            else
            {
                throw new JsonException($"skills.when.{trait} 的取值必须是字符串或字符串数组");
            }

            condition[trait] = values;
        }

        return condition;
    }
}

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
