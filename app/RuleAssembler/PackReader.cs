using System.Text.Json;
using System.Text.Json.Serialization;

namespace RuleAssembler;

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
