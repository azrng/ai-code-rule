using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RuleAssembler;

// 内容资源抽象：屏蔽「程序集内嵌」与「内容包目录」两种来源差异，
// ResourceStore / SkillsStore 只认本接口，不再直读 Assembly
internal interface IResourceProvider
{
    // 归一化相对路径（'\' → '/'，去开头 '/'）→ 全部资源相对路径集合
    IReadOnlyCollection<string> Names { get; }

    // 读取字节内容；资源不存在时返回 null（调用方决定报错还是跳过）
    byte[]? ReadBytes(string relativePath);
}

// 程序集内嵌资源（EmbeddedResource，逻辑名 Fragments.* / Skills.*）：
// 编译期固化，单 exe 离线兜底；逻辑名前缀转相对路径后与目录实现共用一套归一化口径
internal sealed class EmbeddedResourceProvider : IResourceProvider
{
    private readonly Assembly _assembly = Assembly.GetExecutingAssembly();

    // 原始逻辑名 → 归一化相对路径（如 Fragments/shared/common/x.md → shared/common/x.md）
    private readonly Dictionary<string, string> _names;

    public EmbeddedResourceProvider()
    {
        _names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var logicalName in _assembly.GetManifestResourceNames())
        {
            var relative = TryToRelative(logicalName);
            if (relative is not null)
                _names[relative] = logicalName;
        }
    }

    public IReadOnlyCollection<string> Names => _names.Keys;

    public byte[]? ReadBytes(string relativePath)
    {
        var normalized = Normalize(relativePath);
        return _names.TryGetValue(normalized, out var logicalName)
            ? ReadStream(logicalName)
            : null;
    }

    // 内嵌逻辑名 → 统一相对路径：Fragments.xxx → fragments/xxx，Skills.xxx → skills/xxx。
    // 两种来源（内嵌 / 内容包目录）共用同一口径，ResourceStore 与 SkillsStore 无需感知来源差异
    internal static string? TryToRelative(string logicalName)
    {
        foreach (var (prefix, unified) in new[] { ("Fragments.", "fragments/"), ("Skills.", "skills/") })
        {
            if (!logicalName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;
            return Normalize(unified + logicalName[prefix.Length..]);
        }

        return null;
    }

    private byte[] ReadStream(string logicalName)
    {
        using var stream = _assembly.GetManifestResourceStream(logicalName);
        if (stream is null)
            throw new InvalidOperationException($"嵌入资源缺失：{logicalName}");
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    internal static string Normalize(string path)
    {
        return path.Replace('\\', '/').TrimStart('/');
    }
}

// 内容包目录来源：已解包的内容包根目录（内含 pack.json + manifest.json + fragments/ + skills/）
internal sealed class FolderResourceProvider : IResourceProvider
{
    private readonly string _root;

    // 相对路径 → 绝对路径缓存；构造时一次性枚举，避免每次查找都扫盘
    private readonly Dictionary<string, string> _files;

    public FolderResourceProvider(string root)
    {
        _root = Path.GetFullPath(root);
        _files = Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories)
            .Select(path => new
            {
                Relative = EmbeddedResourceProvider.Normalize(Path.GetRelativePath(_root, path)),
                Full = path
            })
            .Where(item => !item.Relative.Equals("pack.json", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(item => item.Relative, item => item.Full, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<string> Names => _files.Keys.ToList();

    public byte[]? ReadBytes(string relativePath)
    {
        return _files.TryGetValue(EmbeddedResourceProvider.Normalize(relativePath), out var full)
            ? File.ReadAllBytes(full)
            : null;
    }
}

// 内容包清单：发布包内的 pack.json（kind/version/schema_version/files 哈希）；
// JSON 字段按发布脚本口径用 snake_case，与 manifest.json / RecipeRecord 同约定
internal sealed record PackManifest(
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("schema_version")] int SchemaVersion,
    [property: JsonPropertyName("files")] Dictionary<string, string> Files);

// 内容包读取与校验：解包目录 → pack.json → 逐文件 SHA-256 比对
internal static class PackReader
{
    // exe 支持的内容包结构版本：新包 schema 超出时拒绝使用并提示升 exe
    internal const int SupportedSchemaVersion = 1;

    public static PackManifest Load(string root)
    {
        var path = Path.Combine(root, "pack.json");
        if (!File.Exists(path))
            throw new InvalidOperationException($"内容包缺少 pack.json：{root}");

        PackManifest? manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<PackManifest>(
                File.ReadAllText(path),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"pack.json 不是有效的 JSON：{path}", ex);
        }

        if (manifest is null)
            throw new InvalidOperationException($"pack.json 内容为空：{path}");
        if (!manifest.Kind.Equals("merged", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"不支持的内容包类型：{manifest.Kind}（期望 merged）");
        if (manifest.SchemaVersion > SupportedSchemaVersion)
            throw new InvalidOperationException(
                $"内容包 schema {manifest.SchemaVersion} 高于本程序支持的 {SupportedSchemaVersion}，请升级 use-ai-rule.exe");
        return manifest;
    }

    // 逐文件校验 SHA-256；返回不匹配/缺失清单（空即全部通过）；pack.json 自身不在 files 清单内
    public static List<string> Verify(PackManifest manifest, string root)
    {
        var mismatches = new List<string>();
        foreach (var pair in manifest.Files)
        {
            var path = Path.Combine(root, pair.Key);
            if (!File.Exists(path))
            {
                mismatches.Add(pair.Key);
                continue;
            }
            using var stream = File.OpenRead(path);
            var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(stream));
            if (!hash.Equals(pair.Value, StringComparison.OrdinalIgnoreCase))
                mismatches.Add(pair.Key);
        }
        return mismatches;
    }
}
