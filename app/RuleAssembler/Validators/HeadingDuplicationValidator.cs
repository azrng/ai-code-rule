using System.Text.RegularExpressions;

namespace RuleAssembler;

// 产物级重复标题校验：同一输出文件内 ## / ### 标题重复即缺陷，是 include 叠加（双份内容 / 双份标题）的直接症状。
// 只查二、三级标题：# 是文件主标题天然唯一，四级及以下是跨节复用的细粒度小节。
// fenced code block（``` 围栏）内的 # 行是文本不是标题，跳过。
internal static class HeadingDuplicationValidator
{
    private static readonly Regex HeadingPattern =
        new(@"^(?<level>#{2,3}) (?<text>.+?)[ \t]*$", RegexOptions.Multiline | RegexOptions.Compiled);

    public static List<string> FindDuplicateHeadings(string content)
    {
        var fences = MarkdownFences.FindSpans(content);

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var duplicates = new List<string>();
        foreach (Match heading in HeadingPattern.Matches(content))
        {
            if (MarkdownFences.IsInside(fences, heading.Index))
                continue;

            var key = heading.Groups["level"].Value + " " + heading.Groups["text"].Value.Trim();
            if (!seen.Add(key))
                duplicates.Add(key);
        }

        return duplicates;
    }
}
