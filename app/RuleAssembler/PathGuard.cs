namespace RuleAssembler;

internal static class PathGuard
{
    public static bool TryNormalizeRelative(string path, out string canonical)
    {
        canonical = "";
        if (string.IsNullOrWhiteSpace(path))
            return false;

        var normalized = path.Trim().Replace('\\', '/');
        if (normalized[0] == '/'
            || IsDrivePath(normalized))
            return false;

        try
        {
            var root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), ".ruleassembler-path-root"));
            var fullPath = Path.GetFullPath(Path.Combine(root, normalized));
            var relative = Path.GetRelativePath(root, fullPath).Replace('\\', '/');
            if (relative == "."
                || relative == ".."
                || relative.StartsWith("../", StringComparison.Ordinal))
                return false;

            canonical = relative;
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public static bool IsSafeProfileId(string id)
    {
        if (string.IsNullOrWhiteSpace(id)
            || id != id.Trim()
            || id is "." or ".."
            || id.Contains('/')
            || id.Contains('\\')
            || id.EndsWith('.')
            || id.EndsWith(' '))
            return false;

        return id.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
    }

    public static string GetPathWithin(string root, string relativePath)
    {
        if (!TryNormalizeRelative(relativePath, out var canonical))
            throw new IOException($"输出路径越界或无效：{relativePath}");

        var fullRoot = Path.GetFullPath(root);
        return Path.Combine(fullRoot, canonical.Replace('/', Path.DirectorySeparatorChar));
    }

    private static bool IsDrivePath(string path)
    {
        return path.Length >= 2
            && char.IsLetter(path[0])
            && path[1] == ':';
    }
}
