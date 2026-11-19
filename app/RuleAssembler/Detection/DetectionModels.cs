namespace RuleAssembler;

internal sealed record DetectionEvidence(string Trait, string Value, string Reason);

// 检测候选：一个候选对应一个兼容样例（维度组合 + 样例映射的 Profile ID 与标签，仅供旧展示路径）。
// 样例未携带的维度即通配维度；候选顺序与 manifest Profile 顺序对齐，与旧 traits 过滤口径的展示一致
internal sealed record DetectionCandidate(
    Selection Selection,
    string? SampleProfileId,
    string Label);

// 检测结果：既可能是收敛到唯一组合的完整结果，也可能是带待确认维度的部分结果。
// 部分结果不伪造完整推荐；Sample* 成员仅在候选唯一（收敛）时给出
internal sealed record DetectionResult(
    Dictionary<string, string> Confirmed,
    List<string> Unconfirmed,
    List<DetectionCandidate> Candidates,
    List<string> Reasons,
    List<string> Notes)
{
    public bool Converged => Candidates.Count == 1;

    // 收敛候选对应的兼容样例 ID 与标签
    public string? SampleProfileId => Converged ? Candidates[0].SampleProfileId : null;

    public string? SampleLabel => Converged ? Candidates[0].Label : null;

    // 完整回答组合取候选样例的全部维度选择，保证按配方能还原到同一组合
    public Dictionary<string, string> BuildSampleAnswers()
    {
        if (!Converged)
            return new Dictionary<string, string>(StringComparer.Ordinal);
        return new Dictionary<string, string>(Candidates[0].Selection.Dimensions, StringComparer.Ordinal);
    }
}
