using System.Text.RegularExpressions;

namespace RuleAssembler;

// markdown 围栏区间（``` 开闭对）：围栏内是示例文本不是真实结构，标题校验、引用提取等
// 都应跳过；未闭合围栏按延伸到文末处理，宁可多跳过也不把围栏内容当正文误判
internal static class MarkdownFences
{
    private static readonly Regex FencePattern = new(@"^```", RegexOptions.Multiline | RegexOptions.Compiled);

    // 全部围栏的 [start,end) 区间
    internal static List<(int Start, int End)> FindSpans(string content)
    {
        var spans = new List<(int Start, int End)>();
        var openAt = -1;
        foreach (Match fence in FencePattern.Matches(content))
        {
            if (openAt < 0)
            {
                openAt = fence.Index;
            }
            else
            {
                spans.Add((openAt, fence.Index + fence.Length));
                openAt = -1;
            }
        }

        if (openAt >= 0)
            spans.Add((openAt, content.Length));

        return spans;
    }

    internal static bool IsInside(List<(int Start, int End)> spans, int position) =>
        spans.Any(span => position >= span.Start && position < span.End);
}
