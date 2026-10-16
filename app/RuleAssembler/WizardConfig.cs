using System.Text.Json.Serialization;

namespace RuleAssembler;

// 向导维度空间：steps 是维度空间唯一事实来源，options 提供维度取值的展示文案；
// 组件 when、约束、兼容样例与技能推荐引用的维度都必须在这里声明
internal sealed class WizardConfig
{
    [JsonPropertyName("steps")]
    public List<WizardStep> Steps { get; set; } = new();

    // trait -> 选项值 -> 展示文案；维度取值（组件 when、约束、兼容样例）都必须在这里有文案
    [JsonPropertyName("options")]
    public Dictionary<string, Dictionary<string, WizardOption>> Options { get; set; } = new();
}

// 向导步骤：steps 是维度空间的唯一事实来源（组件 when / 约束 / 兼容样例引用的维度
// 都必须在此声明），不再由 C# 硬编码维度清单
internal sealed class WizardStep
{
    [JsonPropertyName("trait")]
    public string Trait { get; set; } = "";

    [JsonPropertyName("question")]
    public string Question { get; set; } = "";

    [JsonPropertyName("hint")]
    public string Hint { get; set; } = "";

    // 该维度的「不适用」标记值（如 frontend/backend 的 none、arch-style 的 single）：
    // 结构完备性校验只要求非标记取值被选中组件的 when 锚定，标记取值允许无组件消费
    [JsonPropertyName("absent")]
    public string Absent { get; set; } = "";
}

internal sealed class WizardOption
{
    [JsonPropertyName("label")]
    public string Label { get; set; } = "";

    [JsonPropertyName("desc")]
    public string Desc { get; set; } = "";
}
