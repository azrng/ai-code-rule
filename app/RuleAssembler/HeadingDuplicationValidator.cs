using System.Text.RegularExpressions;

namespace RuleAssembler;

// 产物级重复标题校验：同一输出文件内 ## / ### 标题重复出现即缺陷——它是 include 叠加
// （骨架与 arch 残留双份内容、本地标题与片段自带标题并用）的直接症状。
// 只查二、三级标题：# 是文件主标题天然唯一，四级及以下是跨节复用的细粒度小节。
// fenced code block（``` 围栏）内的 # 行是文本不是标题，跳过。
internal static class HeadingDuplicationValidator
{
    private static readonly Regex HeadingPattern =
        new(@"^(?<level>#{2,3}) (?<text>.+?)[ \t]*$", RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex FencePattern = new(@"^```", RegexOptions.Multiline | RegexOptions.Compiled);

    public static List<string> FindDuplicateHeadings(string content)
    {
        var fences = new List<(int Start, int End)>();
        var openAt = -1;
        foreach (Match fence in FencePattern.Matches(content))
        {
            if (openAt < 0)
            {
                openAt = fence.Index;
            }
            else
            {
                fences.Add((openAt, fence.Index + fence.Length));
                openAt = -1;
            }
        }

        // 未闭合围栏按延伸到文末处理，宁可多跳过也不把围栏内容误判为标题
        if (openAt >= 0)
            fences.Add((openAt, content.Length));

        bool InFence(int position) => fences.Any(span => position >= span.Start && position < span.End);

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var duplicates = new List<string>();
        foreach (Match heading in HeadingPattern.Matches(content))
        {
            if (InFence(heading.Index))
                continue;

            var key = heading.Groups["level"].Value + " " + heading.Groups["text"].Value.Trim();
            if (!seen.Add(key))
                duplicates.Add(key);
        }

        return duplicates;
    }
}
