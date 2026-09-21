namespace RuleAssembler;

// 设计系统文件的保留守卫：design-system.yaml 释放后归产品所有，
// 仅当本地不存在、或 meta.status 仍为「基线待定制」（从未定制）时才允许用新基线覆盖
internal static class DesignSystemGuard
{
    private const string FileName = "design-system.yaml";
    private const string BaselineStatus = "基线待定制";
    private const string StackCommentPrefix = "# 技术栈：";

    public static bool ShouldPreserve(string directory)
    {
        var path = Path.Combine(directory, FileName);
        if (!File.Exists(path))
            return false;
        return !IsUntouchedBaseline(path);
    }

    // meta 段位于文件头部：status 仍是「基线待定制」说明从未按产品定制，可安全刷新
    private static bool IsUntouchedBaseline(string path)
    {
        try
        {
            foreach (var line in File.ReadLines(path).Take(30))
            {
                var trimmed = line.Trim();
                if (!trimmed.StartsWith("status:", StringComparison.OrdinalIgnoreCase))
                    continue;
                return trimmed.Contains(BaselineStatus);
            }
        }
        catch (IOException)
        {
            // 状态读不出来时按已定制处理：宁可保留本地版本，也不用基线覆盖
        }

        return false;
    }

    // 技术栈不匹配告警：本地文件与本次基线的技术栈锚点（头部「# 技术栈：」注释）不一致时提示用户，
    // 典型场景是旧组合的基线（如 React）被保留后项目已切到新组合（如 Blazor）。
    // 只告警不覆盖——已定制文件仍是产品资产；任一侧读不出锚点时返回 null，避免误报。
    public static string? GetTechStackMismatchWarning(string directory, string baselineContent)
    {
        var path = Path.Combine(directory, FileName);
        if (!File.Exists(path))
            return null;

        string? local;
        try
        {
            local = ExtractStackComment(File.ReadLines(path));
        }
        catch (IOException)
        {
            // 本地锚点读不出来时按无不匹配处理，不阻塞生成本身
            return null;
        }

        var baseline = ExtractStackComment(baselineContent.Split('\n'));
        if (local is null || baseline is null || local.Equals(baseline, StringComparison.Ordinal))
            return null;

        return "警告：本地 design-system.yaml 的技术栈（" + local + "）与本次组合基线（" + baseline
            + "）不一致，本地文件疑似旧组合基线，其框架专属章节对当前项目不适用；"
            + "确认本地定制内容无需保留后，删除该文件再重新生成可切换到当前技术栈基线。";
    }

    // 锚点形如「# 技术栈：React + TypeScript + ...」，取冒号后的整行值；锚点固定在文件头部，前 20 行足够
    private static string? ExtractStackComment(IEnumerable<string> lines)
    {
        foreach (var line in lines.Take(20))
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith(StackCommentPrefix, StringComparison.Ordinal))
                continue;
            var value = trimmed[StackCommentPrefix.Length..].Trim();
            return value.Length == 0 ? null : value;
        }
        return null;
    }
}
