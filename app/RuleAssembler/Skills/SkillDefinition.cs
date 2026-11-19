using System.Text.Json;
using System.Text.Json.Serialization;

namespace RuleAssembler;


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
