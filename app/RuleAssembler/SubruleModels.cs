using System.Text.Json;
using System.Text.Json.Serialization;


namespace RuleAssembler;

// 子规则文件声明与渲染产物：Subrule 是 manifest 的文件声明，FileContribution 记录组件归属，
// PlannedFile 是最终落盘的输出文件
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

// 一个输出文件的单项贡献；同一路径允许一个 replace 基底和多个 append 片段
internal sealed class FileContribution
{
    public string OwnerId { get; init; } = "";
    public Subrule File { get; init; } = new();
}

internal sealed class PlannedFile
{
    public string Output { get; init; } = "";
    public string Content { get; init; } = "";
}
