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

    // 供去重会话解析片段标题使用（与展开同一资源视图）
    internal ResourceStore Resources => _resources;

    public FragmentExpander(ResourceStore resources)
    {
        _resources = resources;
    }

    public string Expand(string path)
    {
        return ExpandCore(path, [path], dedup: null);
    }

    // 带同组合去重会话的展开：同一共享片段在同一组合内第二次被引用时以指针行替代，
    // 首次展开的宿主输出文件负责承载全文（上下文唯一权威来源，避免一条执行路径重复加载）
    public string Expand(string path, IncludeDeduplication dedup, string displayOutput)
    {
        return ExpandCore(path, [path], dedup, displayOutput);
    }

    private string ExpandCore(string path, List<string> chain, IncludeDeduplication? dedup = null, string? displayOutput = null)
    {
        var content = _resources.Read(path)
            ?? throw new InvalidOperationException($"缺失片段：{path}");

        return IncludePattern.Replace(content, match =>
        {
            var includePath = match.Groups["path"].Value.Trim().Replace('\\', '/');

            if (chain.Contains(includePath, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException($"片段存在循环引用：{string.Join(" -> ", chain)} -> {includePath}");

            // 同组合去重：已被本组合更早的子规则展开过 → 指针行替代（嵌套 include 同样受管）
            if (dedup is not null && dedup.TryClaim(includePath, displayOutput ?? path, out var pointerLine))
                return pointerLine;

            chain.Add(includePath);
            var expanded = ExpandCore(includePath, chain, dedup, displayOutput);
            chain.RemoveAt(chain.Count - 1);
            return expanded.TrimEnd('\r', '\n');
        });
    }
}

// 同一组合（一次 Plan / 一次渲染）内的共享片段去重会话：
// 记录每个 include 片段的首次宿主输出文件，后续引用替换为指针行。
// 生命周期 = 单次渲染，跨组合不复用（各组合独立裁决首载宿主）
internal sealed class IncludeDeduplication
{
    private readonly Dictionary<string, string> _firstOwner = new(StringComparer.OrdinalIgnoreCase);
    private readonly ResourceStore _resources;

    public IncludeDeduplication(ResourceStore resources)
    {
        _resources = resources;
    }

    // 首次声明返回 false（正常展开）；重复声明返回 true 并给出指针行文本
    public bool TryClaim(string includePath, string currentOutput, out string pointerLine)
    {
        if (_firstOwner.TryGetValue(includePath, out var firstOwner))
        {
            var title = ResolveFragmentTitle(includePath);
            var titlePart = title.Length > 0 ? $"「{title}」" : "同节内容";
            pointerLine = $"> {titlePart}已在 `{firstOwner}` 展开，本文件不重复维护，需要时按需阅读。";
            return true;
        }

        _firstOwner[includePath] = currentOutput;
        pointerLine = "";
        return false;
    }

    // 片段首个 Markdown 标题（供指针行指路）；无标题时返回空
    private string ResolveFragmentTitle(string includePath)
    {
        var content = _resources.Read(includePath);
        if (content is null)
            return "";
        foreach (var line in content.Split('\n'))
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith('#') && trimmed.Length > 1)
                return trimmed.TrimStart('#').Trim();
        }

        return "";
    }
}
