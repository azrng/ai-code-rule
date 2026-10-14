using System.Text.Json.Serialization;

namespace RuleAssembler;

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

    // 生成时的文件内容 SHA256（路径 → 十六进制摘要）：--upgrade 用它识别目标项目本地修改过的文件。
    // 早期配方无此字段，升级时退化为不可检测语义（按 --recipe 直接重放）
    [JsonPropertyName("file_hashes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? FileHashes { get; init; }

    // 导入的技能 ID 与目标工具 ID（交互路径勾选产生，技能目标见 SkillTargets）：
    // --upgrade / --recipe 按配方同步重放；未导入技能时为 null 且不序列化，旧配方形状不变
    [JsonPropertyName("skills")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Skills { get; init; }

    [JsonPropertyName("skill_targets")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? SkillTargets { get; init; }

    [JsonPropertyName("files")]
    public List<string> Files { get; init; } = new();
}
