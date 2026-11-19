namespace RuleAssembler;

// 技能信息：ID 取含 SKILL.md 的最外层目录名；ResourceRoot 是该技能在资源来源中的相对根
// （如 dotnet-skills/aspnet-core，分组目录只是维护侧归档，不参与技能身份）
internal sealed record SkillInfo(string Id, string ResourceRoot, string Description, int FileCount);

// 技能文件：字节级内容，保证脚本、schema 等非 UTF8 文本原样释放
internal sealed record SkillFile(string RelativePath, byte[] Content);

// 技能仓库：技能内容可来自程序集内嵌（Skills.* 逻辑名）或内容包目录，由 IResourceProvider 屏蔽差异；
// 按「含 SKILL.md 的最外层目录」发现技能；嵌套在其它技能根内的 SKILL.md 属于外层技能，随外层整体拷贝
internal sealed class SkillsStore
{
    private const string Prefix = "skills/";

    private readonly IResourceProvider _provider;

    // 归一化相对路径（skills/ 前缀 + 相对技能文件路径）缓存，构造时一次枚举
    private readonly IReadOnlyList<string> _relativeNames;

    // 缺省走程序集内嵌（兼容既有调用与单测）；内容包模式下由 Program 注入 FolderResourceProvider
    public SkillsStore(IResourceProvider? provider = null)
    {
        _provider = provider ?? new EmbeddedResourceProvider();
        _relativeNames = _provider.Names
            .Select(name => EmbeddedResourceProvider.Normalize(name))
            .Where(name => name.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            .Select(name => name[Prefix.Length..])
            .ToList();
    }

    // 发现结果缓存：内容来源（内嵌 / 内容包目录）在进程内不可变，Discover 可安全复用
    private IReadOnlyList<SkillInfo>? _discovered;

    public IReadOnlyList<SkillInfo> Discover()
    {
        // 技能资源来自内嵌程序集或内容包目录，进程内不可变；发现结果含 49 个 SKILL.md 的
        // frontmatter 解析，--check 与交互流程会多次调用，这里缓存避免重复枚举解析
        _discovered ??= DiscoverCore();
        return _discovered;
    }

    private IReadOnlyList<SkillInfo> DiscoverCore()
    {
        var manifestTargets = _relativeNames
            .Where(relative => relative.EndsWith("/SKILL.md", StringComparison.OrdinalIgnoreCase))
            .OrderBy(relative => relative.Count(ch => ch == '/'))
            .ThenBy(relative => relative, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var claimedRoots = new List<string>();
        var skills = new List<SkillInfo>();
        foreach (var manifest in manifestTargets)
        {
            var root = manifest[..^"/SKILL.md".Length];
            // 嵌套候选：外层技能已认领时跳过，内层 SKILL.md 作为普通文件随外层拷贝
            if (claimedRoots.Any(claimed => root.StartsWith(claimed + "/", StringComparison.Ordinal)))
                continue;

            claimedRoots.Add(root);
            var fileCount = _relativeNames.Count(relative => relative.StartsWith(root + "/", StringComparison.Ordinal));
            var content = ReadText(root + "/SKILL.md");
            skills.Add(new SkillInfo(
                root[(root.LastIndexOf('/') + 1)..],
                root,
                ParseDescription(content),
                fileCount));
        }

        return skills.OrderBy(skill => skill.Id, StringComparer.OrdinalIgnoreCase).ToList();
    }


    public SkillInfo? Find(string skillId)
    {
        return Discover().FirstOrDefault(skill => skill.Id.Equals(skillId, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<SkillFile> ReadFiles(SkillInfo skill)
    {
        var prefix = skill.ResourceRoot + "/";
        return _relativeNames
            .Where(name => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(name => new SkillFile(name[prefix.Length..], ReadBytes(name)))
            .OrderBy(file => file.RelativePath, StringComparer.Ordinal)
            .ToList();
    }

    // SKILL.md frontmatter 的 description 值：支持单行与 YAML 块写法（值在下一行缩进续写，取首个非空行）；
    // 缺失时返回空串，--check 会拦截
    internal static string ParseDescription(string skillMarkdown)
    {
        var lines = skillMarkdown.Replace("\r\n", "\n").Split('\n');
        if (lines.Length == 0 || !lines[0].Trim().Equals("---", StringComparison.Ordinal))
            return "";

        for (var i = 1; i < lines.Length && i < 64; i++)
        {
            var line = lines[i];
            if (line.Trim().Equals("---", StringComparison.Ordinal))
                return "";
            if (!line.StartsWith("description:", StringComparison.Ordinal))
                continue;

            var inline = line["description:".Length..].Trim().Trim('"', '\'');
            // YAML 块写法：值在下一行缩进续写，或折叠/字面量指示符（>-、|）挂在同一行
            if (inline.Length == 0 || inline.All(ch => ch is '>' or '|' or '-' or '+'))
            {
                for (var j = i + 1; j < lines.Length && j < 64; j++)
                {
                    var continued = lines[j];
                    if (!continued.StartsWith(' ') && !continued.StartsWith('\t'))
                        return "";
                    var value = continued.Trim().Trim('"', '\'');
                    if (value.Length > 0)
                        return value;
                }

                return "";
            }

            return inline;
        }

        return "";
    }

    private byte[] ReadBytes(string relativePath)
    {
        var bytes = _provider.ReadBytes(Prefix + relativePath);
        if (bytes is null)
            throw new InvalidOperationException($"技能资源缺失：skills/{relativePath}");
        return bytes;
    }

    private string ReadText(string relativePath)
    {
        var bytes = _provider.ReadBytes(Prefix + relativePath);
        if (bytes is null)
            throw new InvalidOperationException($"技能资源缺失：skills/{relativePath}");

        using var reader = new StreamReader(new MemoryStream(bytes), System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return ResourceStore.NormalizeLineEndings(reader.ReadToEnd());
    }
}
