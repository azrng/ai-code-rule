using System.Text.Json.Serialization;

namespace RuleAssembler;

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

    // 组件可附带公共模块；组合器始终先加载 core，再按选中组件声明顺序去重合并模块
    [JsonPropertyName("moduleIds")]
    public List<string> ModuleIds { get; set; } = new();

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

    // 组合文件的逐项贡献，保留组件归属与声明顺序；Files 是兼容旧调用方的扁平视图
    [JsonIgnore]
    public List<FileContribution> FileContributions { get; init; } = new();

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
