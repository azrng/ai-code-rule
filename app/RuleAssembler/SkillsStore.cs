using System.Reflection;

namespace RuleAssembler;

// 技能信息：ID 取含 SKILL.md 的最外层目录名；ResourceRoot 是该技能在嵌入资源中的相对根
// （如 dotnet-skills/aspnet-core，分组目录只是维护侧归档，不参与技能身份）
internal sealed record SkillInfo(string Id, string ResourceRoot, string Description, int FileCount);

// 技能文件：字节级内容，保证脚本、schema 等非 UTF8 文本原样释放
internal sealed record SkillFile(string RelativePath, byte[] Content);

// 嵌入式技能仓库：skills/ 以 Skills.* 逻辑名打进程序集（见 csproj），
// 按「含 SKILL.md 的最外层目录」发现技能；嵌套在其它技能根内的 SKILL.md 属于外层技能，随外层整体拷贝
internal sealed class SkillsStore
{
    private const string Prefix = "Skills.";

    private readonly Assembly _assembly = Assembly.GetExecutingAssembly();

    // 归一化名（反斜杠统一为斜杠，抹平 Windows / Linux 构建机的 RecursiveDir 分隔符差异）→ 程序集原始资源名
    private readonly Dictionary<string, string> _normalizedNames;

    public SkillsStore()
    {
        _normalizedNames = _assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(
                name => name.Replace('\\', '/'),
                name => name,
                StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<SkillInfo> Discover()
    {
        var relatives = _normalizedNames.Keys.Select(name => name[Prefix.Length..]).ToList();
        var manifestTargets = relatives
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
            var fileCount = relatives.Count(relative => relative.StartsWith(root + "/", StringComparison.Ordinal));
            var content = ReadText(Prefix + root + "/SKILL.md");
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
        var prefix = Prefix + skill.ResourceRoot + "/";
        return _normalizedNames.Keys
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

    private byte[] ReadBytes(string normalizedResourceName)
    {
        using var stream = _assembly.GetManifestResourceStream(_normalizedNames[normalizedResourceName]);
        if (stream is null)
            throw new InvalidOperationException($"嵌入资源缺失：{normalizedResourceName}");

        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    private string ReadText(string normalizedResourceName)
    {
        using var stream = _assembly.GetManifestResourceStream(_normalizedNames[normalizedResourceName]);
        if (stream is null)
            throw new InvalidOperationException($"嵌入资源缺失：{normalizedResourceName}");

        using var reader = new StreamReader(stream, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return ResourceStore.NormalizeLineEndings(reader.ReadToEnd());
    }
}
