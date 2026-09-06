using System.Text.Json;
using System.Text.Json.Serialization;

namespace RuleAssembler;

internal sealed class Manifest
{
    [JsonPropertyName("schema_version")]
    public int SchemaVersion { get; set; }

    [JsonPropertyName("line_ending")]
    public string LineEnding { get; set; } = "LF";

    [JsonPropertyName("vars")]
    public ManifestVars Vars { get; set; } = new();

    [JsonPropertyName("modules")]
    public List<ModuleDefinition> Modules { get; set; } = new();

    [JsonPropertyName("families")]
    public List<Family> Families { get; set; } = new();

    [JsonPropertyName("profiles")]
    public List<Profile> Profiles { get; set; } = new();

    // 能力注册表：Q5 能力开关与输出声明的能力归属都引用这里的 ID
    [JsonPropertyName("features")]
    public List<FeatureDefinition> Features { get; set; } = new();

    // 全局输出契约默认值：所有 Profile 共用的“引用现有文件”清单
    [JsonPropertyName("outputs")]
    public OutputsConfig Outputs { get; set; } = new();

    [JsonPropertyName("wizard")]
    public WizardConfig Wizard { get; set; } = new();

    // P2 组合内核：组件、约束与兼容样例。components 是 Selection -> CompositionPlan 的事实来源，
    // constraints 声明维度间的合法关系，compatibilityCases 把旧 Profile 固定为等价回归样例
    [JsonPropertyName("components")]
    public List<ComponentDefinition> Components { get; set; } = new();

    [JsonPropertyName("constraints")]
    public List<CompositionConstraint> Constraints { get; set; } = new();

    [JsonPropertyName("compatibilityCases")]
    public List<CompatibilityCase> CompatibilityCases { get; set; } = new();
}

// ---------- P2 组合内核：Selection -> Component -> CompositionPlan ----------

// 用户选择：维度用字典表达（不扩展固定字段清单），Q5 能力用 ID 集合表达；
// 字段缺省表示尚未确认，组合校验通过且必要维度齐全后才能生成
internal sealed class Selection
{
    // 可整体替换：问答返回上一步时恢复历史快照
    public Dictionary<string, string> Dimensions { get; set; } = new(StringComparer.Ordinal);

    public HashSet<string> Features { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

// 组件定义：单一技术职责（形态 / 壳 / 前端 / 后端 / 能力），可声明根变量贡献、输出文件与适用条件。
// 根变量文本可携带 {{slot:x}} 槽位占位，由其他选中组件的 slots 提供取值（如 backend 决定的服务名词），
// 保证骨架顺序单一、差异内容归属对应组件
internal sealed class ComponentDefinition
{
    // 组件分类；组合器按 kind 决定解析顺序（form -> shell -> frontend -> backend -> capability）
    public const string KindForm = "form";
    public const string KindShell = "shell";
    public const string KindFrontend = "frontend";
    public const string KindBackend = "backend";
    public const string KindCapability = "capability";

    public static readonly string[] KnownKinds =
    {
        KindForm, KindShell, KindFrontend, KindBackend, KindCapability
    };

    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "";

    [JsonPropertyName("label")]
    public string Label { get; set; } = "";

    [JsonPropertyName("desc")]
    public string Desc { get; set; } = "";

    // 适用条件：全部维度命中才选中；引用了选择中不存在的维度时视为证据不足（部分匹配）
    [JsonPropertyName("when")]
    public Dictionary<string, string> When { get; set; } = new(StringComparer.Ordinal);

    // 骨架组件声明：根变量未覆盖字段继承该 family 的 rootDefaults（与旧 Profile 行为一致）
    [JsonPropertyName("family")]
    public string Family { get; set; } = "";

    // 根变量贡献：列表字段按解析顺序拼接，标量字段禁止无提示覆盖；文本可携带 {{slot:x}}
    [JsonPropertyName("root")]
    public RootVariables? Root { get; set; }

    // 槽位取值：替换骨架根变量中的 {{slot:x}}；同一槽位被多个选中组件提供时校验失败
    [JsonPropertyName("slots")]
    public Dictionary<string, string> Slots { get; set; } = new(StringComparer.Ordinal);

    // 输出文件贡献：与 Profile 子规则同构（source / sources / output / feature / merge）
    [JsonPropertyName("files")]
    public List<Subrule> Files { get; set; } = new();

    // design-system.yaml 来源；同一组合内最多允许一个选中组件声明
    [JsonPropertyName("designSystemSource")]
    public string DesignSystemSource { get; set; } = "";

    // 根标量字段在组件 JSON 中的真实存在性（ManifestLoader 加载时标注）：
    // 类默认值比对无法区分「未声明的伪默认」与「刻意声明且恰好等于类默认值」，
    // 后者（如覆盖 family 基线的通用措辞）必须保留
    [JsonIgnore]
    public HashSet<string>? DeclaredScalarFields { get; set; }
}

// 维度约束：when 全部命中时，require 列出的维度取值必须成立，forbid 列出的取值必须不成立
internal sealed class CompositionConstraint
{
    [JsonPropertyName("when")]
    public Dictionary<string, string> When { get; set; } = new(StringComparer.Ordinal);

    [JsonPropertyName("require")]
    public Dictionary<string, string> Require { get; set; } = new(StringComparer.Ordinal);

    [JsonPropertyName("forbid")]
    public Dictionary<string, string> Forbid { get; set; } = new(StringComparer.Ordinal);
}

// 兼容样例：声明旧 Profile 与组合维度的等价关系，供 --check 与回归测试逐字节比对
internal sealed class CompatibilityCase
{
    [JsonPropertyName("profile")]
    public string Profile { get; set; } = "";

    [JsonPropertyName("selection")]
    public Dictionary<string, string> Selection { get; set; } = new(StringComparer.Ordinal);
}

// 组合解析结果：确定性 —— 同一 Selection 与同一版本 manifest 必须得到相同的组件顺序与文件列表
internal sealed class CompositionPlan
{
    [JsonPropertyName("selection")]
    public Selection Selection { get; init; } = new();

    // 组合始终基于公共核心模块渲染（根模板、协作流程、指针）；后续阶段允许组件扩展模块集
    [JsonPropertyName("module_ids")]
    public List<string> ModuleIds { get; init; } = new() { "core" };

    [JsonPropertyName("component_ids")]
    public List<string> ComponentIds { get; init; } = new();

    // 已合并 family 默认值、组件根变量并完成槽位替换的最终根变量
    [JsonPropertyName("root")]
    public RootVariables Root { get; init; } = new();

    [JsonPropertyName("files")]
    public List<Subrule> Files { get; init; } = new();

    // 空 = 本组合不提供 design-system.yaml
    [JsonPropertyName("design_system_source")]
    public string DesignSystemSource { get; init; } = "";

    // 组件提供的槽位取值（根骨架与子规则骨架共用同一命名空间），渲染子规则正文时消费
    [JsonPropertyName("slots")]
    public Dictionary<string, string> Slots { get; init; } = new(StringComparer.Ordinal);

    // 非致命说明（如声明了未被消费的槽位）；致命问题在解析时直接抛错
    [JsonPropertyName("warnings")]
    public List<string> Warnings { get; init; } = new();
}

// 能力定义：仅注册 ID 与展示文案；能力是否可用、默认是否勾选由后续阶段按 Profile/模块声明判断
internal sealed class FeatureDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("label")]
    public string Label { get; set; } = "";

    [JsonPropertyName("desc")]
    public string Desc { get; set; } = "";
}

internal sealed class OutputsConfig
{
    // 根规则文案会提到、但生成器不负责创建的目标项目现有文件（如 TASK.md）
    [JsonPropertyName("referenced-existing")]
    public List<string> ReferencedExisting { get; set; } = new();
}

internal sealed class WizardConfig
{
    [JsonPropertyName("steps")]
    public List<WizardStep> Steps { get; set; } = new();

    // trait -> 选项值 -> 展示文案；Profile traits 中出现的每个非 * 值都必须在这里有文案
    [JsonPropertyName("options")]
    public Dictionary<string, Dictionary<string, WizardOption>> Options { get; set; } = new();
}

internal sealed class WizardStep
{
    [JsonPropertyName("trait")]
    public string Trait { get; set; } = "";

    [JsonPropertyName("question")]
    public string Question { get; set; } = "";

    [JsonPropertyName("hint")]
    public string Hint { get; set; } = "";

    // 只在先决回答命中时才提问；留空表示始终适用（仍受“候选间存在差异才问”的约束）
    [JsonPropertyName("applies-when-trait")]
    public string? AppliesWhenTrait { get; set; }

    [JsonPropertyName("applies-when-values")]
    public List<string> AppliesWhenValues { get; set; } = new();

    // 检测无证据时的默认倾向值（如分层 vs 单层默认单层）
    [JsonPropertyName("detect-default")]
    public string DetectDefault { get; set; } = "";
}

internal sealed class WizardOption
{
    [JsonPropertyName("label")]
    public string Label { get; set; } = "";

    [JsonPropertyName("desc")]
    public string Desc { get; set; } = "";
}

internal sealed class ManifestVars
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    [JsonPropertyName("last_updated")]
    public string LastUpdated { get; set; } = "";
}

internal sealed class Family
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("label")]
    public string Label { get; set; } = "";

    [JsonPropertyName("desc")]
    public string Desc { get; set; } = "";

    // 家族级根变量默认值：Profile 的 root 中未显式出现的字段继承这里的值
    [JsonPropertyName("rootDefaults")]
    public RootVariables? RootDefaults { get; set; }
}

internal sealed class ModuleDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("label")]
    public string Label { get; set; } = "";

    [JsonPropertyName("rootSource")]
    public string RootSource { get; set; } = "";

    [JsonPropertyName("pointers")]
    public List<string> Pointers { get; set; } = new();

    [JsonPropertyName("files")]
    public List<Subrule> Files { get; set; } = new();
}

internal sealed class Profile
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("label")]
    public string Label { get; set; } = "";

    [JsonPropertyName("family")]
    public string Family { get; set; } = "";

    [JsonPropertyName("desc")]
    public string Desc { get; set; } = "";

    [JsonPropertyName("sourceRoot")]
    public string SourceRoot { get; set; } = "";

    [JsonPropertyName("moduleIds")]
    public List<string> ModuleIds { get; set; } = new() { "core" };

    [JsonPropertyName("root")]
    public RootVariables Root { get; set; } = new();

    [JsonPropertyName("subrules")]
    public List<Subrule> Subrules { get; set; } = new();

    // 兼容旧配方：当 Profile 没有模块指针时才使用这里的指针列表。
    [JsonPropertyName("pointers")]
    public List<string> Pointers { get; set; } = new();

    [JsonPropertyName("designSystem")]
    public bool DesignSystem { get; set; }

    [JsonPropertyName("designSystemSource")]
    public string DesignSystemSource { get; set; } = "";

    // 组合画像：问答向导与项目检测按 trait 过滤 Profile；* 表示该项不适用（匹配任意回答）
    [JsonPropertyName("traits")]
    public ProfileTraits? Traits { get; set; }

    // 显式输出声明：补充派生清单之外的条目（如把某文件标记为 referenced-existing），
    // 与片段结构派生的声明合并后构成该 Profile 的完整输出契约
    [JsonPropertyName("outputs")]
    public List<OutputDeclaration> Outputs { get; set; } = new();

    // Profile 级槽位：旧路径渲染骨架化子规则与 design-system 时提供取值（组合路径走组件并集）
    [JsonPropertyName("slots")]
    public Dictionary<string, string> Slots { get; set; } = new(StringComparer.Ordinal);
}

// 结构化输出声明：输出契约的事实来源是本结构与片段声明，不通过解析根文件 Markdown 反向推断。
// kind：generated = 本次确实生成；referenced-existing = 根规则提到但不由生成器创建的目标项目现有文件
internal sealed class OutputDeclaration
{
    // kind 与 merge 的合法取值；校验与比较都以这里为准
    public const string KindGenerated = "generated";
    public const string KindReferencedExisting = "referenced-existing";
    public const string MergeReplace = "replace";
    public const string MergeAppend = "append";

    [JsonPropertyName("path")]
    public string Path { get; set; } = "";

    [JsonPropertyName("kind")]
    public string Kind { get; set; } = KindGenerated;

    // 能力归属：doc / infra / ai-capability / design-system 等，空表示核心输出
    [JsonPropertyName("feature")]
    public string Feature { get; set; } = "";

    [JsonPropertyName("merge")]
    public string Merge { get; set; } = MergeReplace;
}

internal sealed class ProfileTraits
{
    public const string Wildcard = "*";

    public static readonly string[] KnownTraits =
    {
        "form", "backend", "frontend", "shell", "arch-style", "blazor-flavor", "ai-capability"
    };

    [JsonPropertyName("form")]
    public string Form { get; set; } = "";

    [JsonPropertyName("backend")]
    public string Backend { get; set; } = "";

    [JsonPropertyName("frontend")]
    public string Frontend { get; set; } = "";

    [JsonPropertyName("shell")]
    public string Shell { get; set; } = "";

    [JsonPropertyName("arch-style")]
    public string ArchStyle { get; set; } = "";

    [JsonPropertyName("blazor-flavor")]
    public string BlazorFlavor { get; set; } = "";

    [JsonPropertyName("ai-capability")]
    public string AiCapability { get; set; } = "";

    public string? Get(string trait)
    {
        return trait switch
        {
            "form" => Form,
            "backend" => Backend,
            "frontend" => Frontend,
            "shell" => Shell,
            "arch-style" => ArchStyle,
            "blazor-flavor" => BlazorFlavor,
            "ai-capability" => AiCapability,
            _ => null
        };
    }
}

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

    public override void Write(Utf8JsonWriter writer, RootLine value, JsonSerializerOptions options)
    {
        if (value.When is null && value.Features.Count == 0)
        {
            writer.WriteStringValue(value.Text);
            return;
        }

        JsonSerializer.Serialize(writer, value, options);
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

internal sealed class Subrule
{
    [JsonPropertyName("source")]
    public string Source { get; set; } = "";

    // B 档骨架继承：多源按序拼接为一个输出文件，与 source 互斥；
    // front matter 只允许出现在首个源，其余源是纯内容段
    [JsonPropertyName("sources")]
    public List<string> Sources { get; set; } = new();

    [JsonPropertyName("output")]
    public string Output { get; set; } = "";

    // 能力归属（doc / infra / ai-capability 等），空表示核心输出；归属必须挂在声明上而非靠文件名推断
    [JsonPropertyName("feature")]
    public string Feature { get; set; } = "";

    // 合并方式：replace（默认）或显式 append；当前渲染器全部按 replace 落盘，append 供后续阶段消费
    [JsonPropertyName("merge")]
    public string Merge { get; set; } = OutputDeclaration.MergeReplace;

    // 文件级槽位：骨架正文中的 {{slot:x}} 在渲染时以这里提供的值替换（P2 阶段 5 内容收敛）。
    // 旧 Profile 路径用静态值；组合路径由 CompositionPlan.Slots（组件并集）提供，文件级可覆盖
    [JsonPropertyName("slots")]
    public Dictionary<string, string> Slots { get; set; } = new(StringComparer.Ordinal);

    public IReadOnlyList<string> GetSources()
    {
        return Sources.Count > 0 ? Sources : new List<string> { Source };
    }
}

internal sealed class PlannedFile
{
    public string Output { get; init; } = "";
    public string Content { get; init; } = "";
}

// 写入目标项目的 .ai-rule-profile.json：记录本次生成的选择配方，供人工核对与后续升级
internal sealed class RecipeRecord
{
    [JsonPropertyName("schema_version")]
    public int SchemaVersion { get; init; } = 1;

    [JsonPropertyName("manifest_version")]
    public string ManifestVersion { get; init; } = "";

    [JsonPropertyName("generated_at")]
    public string GeneratedAt { get; init; } = "";

    // classic = 经典大类选择；wizard = 问答决策树；detected = 检测推荐直接确认
    [JsonPropertyName("mode")]
    public string Mode { get; init; } = "";

    [JsonPropertyName("profile_id")]
    public string ProfileId { get; init; } = "";

    [JsonPropertyName("profile_label")]
    public string ProfileLabel { get; init; } = "";

    [JsonPropertyName("family")]
    public string Family { get; init; } = "";

    [JsonPropertyName("answers")]
    public Dictionary<string, string> Answers { get; init; } = new();

    // 回答来源：detected = 检测证据确认；manual = 人工补答。无检测参与时为空，字段为增量兼容
    [JsonPropertyName("answer_sources")]
    public Dictionary<string, string> AnswerSources { get; init; } = new();

    // Q5 启用的能力 ID 集；旧 profile_id 保留为迁移兼容，能力状态以本字段为准
    [JsonPropertyName("features")]
    public List<string> Features { get; init; } = new();

    // 组合内核路径（P2 阶段 3）专属：维度选择与命中组件；经典 Profile 路径为 null 且不序列化
    [JsonPropertyName("selection")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? Selection { get; init; }

    [JsonPropertyName("component_ids")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? ComponentIds { get; init; }

    [JsonPropertyName("files")]
    public List<string> Files { get; init; } = new();
}
