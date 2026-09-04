namespace RuleAssembler;

// 设计系统文件的保留守卫：design-system.yaml 释放后归产品所有，
// 仅当本地不存在、或 meta.status 仍为「基线待定制」（从未定制）时才允许用新基线覆盖
internal static class DesignSystemGuard
{
    private const string FileName = "design-system.yaml";
    private const string BaselineStatus = "基线待定制";

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
}
