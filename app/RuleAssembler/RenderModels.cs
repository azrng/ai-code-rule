using System.Text.Json;
using System.Text.Json.Serialization;

namespace RuleAssembler;

// 根变量条目：普通文本始终渲染；带 features 的条目仅在所列能力全部启用时渲染；
// when 提供按启用能力的多档文案，取第一个满足条件的档位，全部不满足时整条丢弃
[JsonConverter(typeof(RootLineJsonConverter))]
internal sealed class RootLine
{
    [JsonPropertyName("line")]
    public string Text { get; set; } = "";

    // 需要全部启用的能力 ID；空表示无条件渲染
    [JsonPropertyName("features")]
    public List<string> Features { get; set; } = new();

    [JsonPropertyName("when")]
    public List<RootLineVariant>? When { get; set; }
}

// 多档文案的一档：features 全部启用时采用该档文案
internal sealed class RootLineVariant
{
    [JsonPropertyName("line")]
    public string Text { get; set; } = "";

    [JsonPropertyName("features")]
    public List<string> Features { get; set; } = new();
}

// 根变量条目的 JSON 读取兼容：字符串等价于无条件的 RootLine
internal sealed class RootLineJsonConverter : JsonConverter<RootLine>
{
    public override RootLine? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                return new RootLine { Text = reader.GetString() ?? "" };
            case JsonTokenType.StartObject:
            {
                // 手工解析对象：options 仍含本转换器，直接反序列化 RootLine 会无限递归
                var line = new RootLine();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    if (reader.TokenType != JsonTokenType.PropertyName)
                        continue;
                    var property = reader.GetString();
                    reader.Read();
                    switch (property)
                    {
                        case "line":
                            line.Text = reader.GetString() ?? "";
                            break;
                        case "features":
                            if (reader.TokenType == JsonTokenType.StartArray)
                            {
                                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                                    line.Features.Add(reader.GetString() ?? "");
                            }
                            break;
                        case "when":
                            if (reader.TokenType == JsonTokenType.StartArray)
                                line.When = JsonSerializer.Deserialize<List<RootLineVariant>>(ref reader, options);
                            break;
                        default:
                            reader.Skip();
                            break;
                    }
                }

                return line;
            }
            default:
                throw new JsonException("根变量条目必须是字符串或 { line, features } 对象。");
        }
    }

    // 手工写对象：options 仍携带本转换器，委托 JsonSerializer.Serialize(RootLine) 会无限递归
    public override void Write(Utf8JsonWriter writer, RootLine value, JsonSerializerOptions options)
    {
        if (value.When is null && value.Features.Count == 0)
        {
            writer.WriteStringValue(value.Text);
            return;
        }

        writer.WriteStartObject();
        writer.WriteString("line", value.Text);
        if (value.Features.Count > 0)
        {
            writer.WriteStartArray("features");
            foreach (var feature in value.Features)
                writer.WriteStringValue(feature);
            writer.WriteEndArray();
        }

        if (value.When is not null)
        {
            writer.WriteStartArray("when");
            foreach (var variant in value.When)
            {
                writer.WriteStartObject();
                writer.WriteString("line", variant.Text);
                if (variant.Features.Count > 0)
                {
                    writer.WriteStartArray("features");
                    foreach (var feature in variant.Features)
                        writer.WriteStringValue(feature);
                    writer.WriteEndArray();
                }
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }
        writer.WriteEndObject();
    }
}

// 根变量条目拷贝：列表字段合并与槽位替换都在副本上进行，避免污染 family 默认值
internal static class RootLineExtensions
{
    public static RootLine Clone(this RootLine line)
    {
        return new RootLine
        {
            Text = line.Text,
            Features = new List<string>(line.Features),
            When = line.When is null
                ? null
                : line.When.Select(variant => new RootLineVariant
                {
                    Text = variant.Text,
                    Features = new List<string>(variant.Features)
                }).ToList()
        };
    }
}

internal sealed class RootVariables
{
    [JsonPropertyName("target-list")]
    public List<RootLine> TargetList { get; set; } = new();

    [JsonPropertyName("subrule-index")]
    public List<RootLine> SubruleIndex { get; set; } = new();

    [JsonPropertyName("dev-stage")]
    public List<RootLine> DevStage { get; set; } = new();

    [JsonPropertyName("understanding-checklist")]
    public List<RootLine> UnderstandingChecklist { get; set; } = new();

    [JsonPropertyName("search-nouns")]
    public string SearchNouns { get; set; } = "";

    [JsonPropertyName("extra-sections")]
    public List<RootLine> ExtraSections { get; set; } = new();

    [JsonPropertyName("root-scope-nouns")]
    public string RootScopeNouns { get; set; } = "后端、文档、基础设施";

    [JsonPropertyName("context-scope-nouns")]
    public string ContextScopeNouns { get; set; } = "服务端实现 / 文档 / 发布链路";

    [JsonPropertyName("high-risk-nouns")]
    public string HighRiskNouns { get; set; } = "数据库、缓存、鉴权、发布、环境变量";

    [JsonPropertyName("high-risk-prefix")]
    public string HighRiskPrefix { get; set; } = "";

    [JsonPropertyName("task-kind-nouns")]
    public string TaskKindNouns { get; set; } = "后端还是基础设施";

    [JsonPropertyName("evidence-nouns")]
    public string EvidenceNouns { get; set; } = "日志、数据、断点、测试";

    [JsonPropertyName("reuse-noun")]
    public string ReuseNoun { get; set; } = "配置";

    [JsonPropertyName("persistence-suffix")]
    public string PersistenceSuffix { get; set; } = "";

    [JsonPropertyName("destructive-nouns")]
    public string DestructiveNouns { get; set; } = "数据破坏性变更、环境清理";

    [JsonPropertyName("contract-error-line")]
    public string ContractErrorLine { get; set; } = "阶段 0 输出的接口契约、DTO 或字段说明在阶段 1 发现缺口时，应先回到契约变更流程，修改 DTO / 接口定义后同步 Application、Domain、EntityFramework 映射、测试和相关文档，再继续实现；不得绕过契约直接补临时字段";

    [JsonPropertyName("reading-order")]
    public List<RootLine> ReadingOrder { get; set; } = new();

    [JsonPropertyName("intro-quote")]
    public string IntroQuote { get; set; } = "";

    [JsonPropertyName("checklist-infra")]
    public string ChecklistInfra { get; set; } = "- 涉及基础设施时，已查看 Docker、环境变量、端口、启动命令、发布脚本和部署文档";

    [JsonPropertyName("model-behavior-extra")]
    public string ModelBehaviorExtra { get; set; } = "";

    [JsonPropertyName("verify-nouns")]
    public string VerifyNouns { get; set; } = "";

    [JsonPropertyName("verify-quote")]
    public string VerifyQuote { get; set; } = "";

    [JsonPropertyName("rollback-scope")]
    public string RollbackScope { get; set; } = "发布产物、容器";

    [JsonPropertyName("boundary-nouns")]
    public string BoundaryNouns { get; set; } = "跨层职责";

    [JsonPropertyName("persistence-line")]
    public string PersistenceLine { get; set; } = "";

    // 深拷贝：组合器以 family rootDefaults 为起点叠加组件根变量时，不能改动原清单对象
    public RootVariables Clone()
    {
        return new RootVariables
        {
            TargetList = TargetList.Select(line => line.Clone()).ToList(),
            SubruleIndex = SubruleIndex.Select(line => line.Clone()).ToList(),
            DevStage = DevStage.Select(line => line.Clone()).ToList(),
            UnderstandingChecklist = UnderstandingChecklist.Select(line => line.Clone()).ToList(),
            SearchNouns = SearchNouns,
            ExtraSections = ExtraSections.Select(line => line.Clone()).ToList(),
            RootScopeNouns = RootScopeNouns,
            ContextScopeNouns = ContextScopeNouns,
            HighRiskNouns = HighRiskNouns,
            HighRiskPrefix = HighRiskPrefix,
            TaskKindNouns = TaskKindNouns,
            EvidenceNouns = EvidenceNouns,
            ReuseNoun = ReuseNoun,
            PersistenceSuffix = PersistenceSuffix,
            DestructiveNouns = DestructiveNouns,
            ContractErrorLine = ContractErrorLine,
            ReadingOrder = ReadingOrder.Select(line => line.Clone()).ToList(),
            IntroQuote = IntroQuote,
            ChecklistInfra = ChecklistInfra,
            ModelBehaviorExtra = ModelBehaviorExtra,
            VerifyNouns = VerifyNouns,
            VerifyQuote = VerifyQuote,
            RollbackScope = RollbackScope,
            BoundaryNouns = BoundaryNouns,
            PersistenceLine = PersistenceLine
        };
    }
}
