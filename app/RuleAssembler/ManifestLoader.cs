using System.Text.Json;

namespace RuleAssembler;

// manifest 装载：JSON 读取、family rootDefaults 继承（旧 Profile 域）与组件根字段存在性标注；
// 结构校验委托给 ManifestValidator / CompositionValidator
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
            // 先在 JSON 层做 family rootDefaults 继承：Profile 的 root 中未显式出现的字段
            // 从所属 family 的 rootDefaults 复制，保证「未写则继承、写了则覆盖」的语义
            json = MergeFamilyDefaults(json);
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

        AnnotateComponentRootPresence(manifest, json);
        new ManifestValidator(_resources).Validate(manifest);
        return manifest;
    }

    // 标注每个组件根标量字段在 JSON 中的真实存在性，供组合器区分伪默认与刻意声明
    private static void AnnotateComponentRootPresence(Manifest manifest, string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty("components", out var componentsEl)
            || componentsEl.ValueKind != JsonValueKind.Array)
            return;

        var byId = manifest.Components
            .Where(component => !string.IsNullOrWhiteSpace(component.Id))
            .GroupBy(component => component.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var componentEl in componentsEl.EnumerateArray())
        {
            if (componentEl.ValueKind != JsonValueKind.Object
                || !componentEl.TryGetProperty("id", out var idEl)
                || idEl.ValueKind != JsonValueKind.String
                || !byId.TryGetValue(idEl.GetString() ?? "", out var component))
                continue;
            if (!componentEl.TryGetProperty("root", out var rootEl)
                || rootEl.ValueKind != JsonValueKind.Object)
                continue;

            var scalars = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in rootEl.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.String)
                    scalars.Add(property.Name);
            }

            component.DeclaredScalarFields = scalars;
        }
    }

    private static string MergeFamilyDefaults(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty("families", out var familiesEl)
            || familiesEl.ValueKind != JsonValueKind.Array
            || !root.TryGetProperty("profiles", out var profilesEl)
            || profilesEl.ValueKind != JsonValueKind.Array)
            return json;

        var defaultsByFamily = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        foreach (var family in familiesEl.EnumerateArray())
        {
            if (family.ValueKind != JsonValueKind.Object
                || !family.TryGetProperty("id", out var idEl)
                || idEl.ValueKind != JsonValueKind.String)
                continue;
            if (family.TryGetProperty("rootDefaults", out var defaults)
                && defaults.ValueKind == JsonValueKind.Object)
                defaultsByFamily[idEl.GetString() ?? ""] = defaults.Clone();
        }

        if (defaultsByFamily.Count == 0)
            return json;

        var rootObj = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
        var profiles = new List<JsonElement>();
        foreach (var profile in profilesEl.EnumerateArray())
        {
            if (profile.ValueKind != JsonValueKind.Object
                || !profile.TryGetProperty("family", out var famEl)
                || famEl.ValueKind != JsonValueKind.String
                || !defaultsByFamily.TryGetValue(famEl.GetString() ?? "", out var defaults))
            {
                profiles.Add(profile.Clone());
                continue;
            }

            var profileObj = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(profile.GetRawText())!;
            if (!profileObj.TryGetValue("root", out var rootVarEl)
                || rootVarEl.ValueKind != JsonValueKind.Object)
                rootVarEl = JsonDocument.Parse("{}").RootElement.Clone();

            var rootVars = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(rootVarEl.GetRawText())!;
            foreach (var pair in defaults.EnumerateObject())
            {
                if (!rootVars.ContainsKey(pair.Name))
                    rootVars[pair.Name] = pair.Value.Clone();
            }

            profileObj["root"] = JsonDocument.Parse(JsonSerializer.Serialize(rootVars)).RootElement.Clone();
            profiles.Add(JsonDocument.Parse(JsonSerializer.Serialize(profileObj)).RootElement.Clone());
        }

        rootObj!["profiles"] = JsonDocument.Parse(JsonSerializer.Serialize(profiles)).RootElement.Clone();
        return JsonSerializer.Serialize(rootObj);
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
