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

    [JsonPropertyName("wizard")]
    public WizardConfig Wizard { get; set; } = new();
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

internal sealed class RootVariables
{
    [JsonPropertyName("target-list")]
    public List<string> TargetList { get; set; } = new();

    [JsonPropertyName("subrule-index")]
    public List<string> SubruleIndex { get; set; } = new();

    [JsonPropertyName("dev-stage")]
    public List<string> DevStage { get; set; } = new();

    [JsonPropertyName("understanding-checklist")]
    public List<string> UnderstandingChecklist { get; set; } = new();

    [JsonPropertyName("search-nouns")]
    public string SearchNouns { get; set; } = "";

    [JsonPropertyName("extra-sections")]
    public List<string> ExtraSections { get; set; } = new();

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
    public List<string> ReadingOrder { get; set; } = new();

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
}

internal sealed class Subrule
{
    [JsonPropertyName("source")]
    public string Source { get; set; } = "";

    [JsonPropertyName("output")]
    public string Output { get; set; } = "";
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

    [JsonPropertyName("files")]
    public List<string> Files { get; init; } = new();
}
