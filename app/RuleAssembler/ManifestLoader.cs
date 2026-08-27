using System.Text.Json;

namespace RuleAssembler;

internal sealed class ManifestLoader
{
    private readonly ResourceStore _resources;

    public ManifestLoader(ResourceStore resources)
    {
        _resources = resources;
    }

    public Manifest Load()
    {
        var json = _resources.Read("manifest.json");
        if (json is null)
            throw new InvalidOperationException("无法加载嵌入的 manifest.json。");

        Manifest? manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<Manifest>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("manifest.json 不是有效的 JSON。", ex);
        }

        if (manifest is null)
            throw new InvalidOperationException("manifest.json 内容为空。");

        Validate(manifest);
        return manifest;
    }

    private void Validate(Manifest manifest)
    {
        var errors = new List<string>();

        if (manifest.SchemaVersion != 2)
            errors.Add($"schema_version 必须为 2，当前为 {manifest.SchemaVersion}");
        if (string.IsNullOrWhiteSpace(manifest.Vars.Version))
            errors.Add("vars.version 不能为空");
        if (string.IsNullOrWhiteSpace(manifest.Vars.LastUpdated))
            errors.Add("vars.last_updated 不能为空");
        if (!manifest.LineEnding.Equals("LF", StringComparison.OrdinalIgnoreCase)
            && !manifest.LineEnding.Equals("CRLF", StringComparison.OrdinalIgnoreCase))
            errors.Add("line_ending 只能是 LF 或 CRLF");

        ValidateUniqueIds(manifest.Families.Select(f => f.Id), "family", errors);
        ValidateUniqueIds(manifest.Modules.Select(m => m.Id), "module", errors);
        ValidateUniqueIds(manifest.Profiles.Select(p => p.Id), "Profile", errors);

        var familyIds = manifest.Families
            .Select(f => f.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var moduleMap = manifest.Modules
            .Where(m => !string.IsNullOrWhiteSpace(m.Id))
            .GroupBy(m => m.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        if (!moduleMap.Values.Any(m => !string.IsNullOrWhiteSpace(m.RootSource)))
            errors.Add("至少需要一个提供 rootSource 的公共模块");

        foreach (var module in manifest.Modules)
        {
            ValidateResourcePath(module.RootSource, $"module {module.Id} rootSource", errors, allowEmpty: true);
            foreach (var pointer in module.Pointers)
                ValidateResourcePath($"pointers/{pointer}", $"module {module.Id} pointer", errors);
            foreach (var file in module.Files)
                ValidateRuleFile(file, $"module {module.Id}", errors);
        }

        foreach (var profile in manifest.Profiles)
        {
            if (!familyIds.Contains(profile.Family))
                errors.Add($"Profile {profile.Id} 引用了不存在的 family：{profile.Family}");

            foreach (var moduleId in profile.ModuleIds)
            {
                if (!moduleMap.ContainsKey(moduleId))
                    errors.Add($"Profile {profile.Id} 引用了不存在的 module：{moduleId}");
            }

            ValidateResourcePath(profile.SourceRoot, $"Profile {profile.Id} sourceRoot", errors, allowEmpty: true);
            ValidateResourcePath(profile.DesignSystemSource, $"Profile {profile.Id} designSystemSource", errors, allowEmpty: true);

            foreach (var file in profile.Subrules)
                ValidateRuleFile(file, $"Profile {profile.Id}", errors);
            foreach (var pointer in profile.Pointers)
                ValidateResourcePath($"pointers/{pointer}", $"Profile {profile.Id} pointer", errors);

            if (profile.DesignSystem)
            {
                var source = GetDesignSystemSource(profile);
                ValidateResourcePath(source, $"Profile {profile.Id} design-system", errors);
            }
        }

        if (errors.Count > 0)
            throw new InvalidOperationException("manifest 校验失败：\n- " + string.Join("\n- ", errors));
    }

    private void ValidateRuleFile(Subrule file, string owner, ICollection<string> errors)
    {
        ValidateResourcePath(file.Source, $"{owner} source", errors);
        ValidateOutputPath(file.Output, $"{owner} output", errors);
        if (!string.IsNullOrWhiteSpace(file.Source) && _resources.Read(file.Source) is null)
            errors.Add($"{owner} 找不到片段：{file.Source}");
    }

    private void ValidateResourcePath(string path, string field, ICollection<string> errors, bool allowEmpty = false)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            if (!allowEmpty)
                errors.Add($"{field} 不能为空");
            return;
        }

        if (Path.IsPathRooted(path) || path.Replace('\\', '/').Split('/').Any(part => part == ".."))
            errors.Add($"{field} 不能包含绝对路径或 ..：{path}");
    }

    private static void ValidateOutputPath(string path, string field, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            errors.Add($"{field} 不能为空");
            return;
        }

        if (Path.IsPathRooted(path) || path.Replace('\\', '/').Split('/').Any(part => part == ".."))
            errors.Add($"{field} 不能包含绝对路径或 ..：{path}");
    }

    private static void ValidateUniqueIds(IEnumerable<string> ids, string kind, ICollection<string> errors)
    {
        var duplicates = ids
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .GroupBy(id => id, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicates.Count > 0)
            errors.Add($"{kind} ID 重复：{string.Join("、", duplicates)}");
    }

    internal static string GetDesignSystemSource(Profile profile)
    {
        if (!string.IsNullOrWhiteSpace(profile.DesignSystemSource))
            return profile.DesignSystemSource;
        if (!string.IsNullOrWhiteSpace(profile.SourceRoot))
            return profile.SourceRoot.TrimEnd('/', '\\') + "/design-system.yaml";
        return $"arch/{profile.Id}/design-system.yaml";
    }
}
