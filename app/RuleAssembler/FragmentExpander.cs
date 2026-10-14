using System.Text.RegularExpressions;

namespace RuleAssembler;

/// <summary>
/// 展开片段中的 include 指令：独占一行的 &lt;!-- include: 路径 --&gt;，路径相对 fragments 根目录。
/// 共享小节集中维护在 fragments/shared/ 下，由各架构子规则引用，避免逐份复制产生漂移。
/// </summary>
internal sealed class FragmentExpander
{
    private static readonly Regex IncludePattern = new(
        @"^[ \t]*<!--\s*include:\s*(?<path>[^>\r\n]+?)\s*-->[ \t]*\r?$",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private readonly ResourceStore _resources;

    public FragmentExpander(ResourceStore resources)
    {
        _resources = resources;
    }

    public string Expand(string path)
    {
        return ExpandCore(path, [path]);
    }

    private string ExpandCore(string path, List<string> chain)
    {
        var content = _resources.Read(path)
            ?? throw new InvalidOperationException($"缺失片段：{path}");

        return IncludePattern.Replace(content, match =>
        {
            var includePath = match.Groups["path"].Value.Trim().Replace('\\', '/');

            if (chain.Contains(includePath, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException($"片段存在循环引用：{string.Join(" -> ", chain)} -> {includePath}");

            chain.Add(includePath);
            var expanded = ExpandCore(includePath, chain);
            chain.RemoveAt(chain.Count - 1);
            return expanded.TrimEnd('\r', '\n');
        });
    }
}
