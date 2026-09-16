using System.Text.Json;

namespace RuleAssembler;

// manifest 装载：JSON 读取与组件根字段存在性标注；结构校验委托给
// ManifestValidator / CompositionValidator
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
}
