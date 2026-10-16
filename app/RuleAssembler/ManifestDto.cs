using System.Text.Json.Serialization;

namespace RuleAssembler;

// manifest.json 的反序列化模型：模块、家族、能力、向导维度空间与组合事实（components / constraints / cases）
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

    // 技能元数据：展示分组与推荐预选的事实来源；技能内容发现仍由 SkillsStore 负责，
    // 此处只登记 ID（与 SkillInfo.Id 同口径）、group 与 when（检测/问答组合 → 推荐预选）
    [JsonPropertyName("skills")]
    public List<SkillDefinition> Skills { get; set; } = new();
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
