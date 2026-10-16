using System.Text.Json;
using System.Text.Json.Serialization;

namespace RuleAssembler;

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

// 根变量条目：普通文本始终渲染；带 features 的条目仅在所列能力全部启用时渲染；
