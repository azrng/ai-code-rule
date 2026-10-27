using System.Text.RegularExpressions;

namespace RuleAssembler;

// manifest 结构校验（schema / 模块 / family 域 + 共享低层规则）：
// schema 与 ID 唯一性、模块的资源引用、根变量能力引用；
// 维度空间 / 组件 / 约束 / 兼容样例域委托给 CompositionValidator
internal sealed class ManifestValidator
{
    // 组件槽位名的合法形态：小写字母、数字与连字符
    internal static readonly Regex SlotNamePattern = new(@"^[\w-]+$", RegexOptions.Compiled);

    private readonly ResourceStore _resources;

    public ManifestValidator(ResourceStore resources)
    {
        _resources = resources;
    }

    internal void Validate(Manifest manifest)
    {
        var errors = new List<string>();

        if (manifest.SchemaVersion != 3)
            errors.Add($"schema_version 必须为 3，当前为 {manifest.SchemaVersion}");
        if (string.IsNullOrWhiteSpace(manifest.Vars.Version))
            errors.Add("vars.version 不能为空");
        if (string.IsNullOrWhiteSpace(manifest.Vars.LastUpdated))
            errors.Add("vars.last_updated 不能为空");
        if (!manifest.LineEnding.Equals("LF", StringComparison.OrdinalIgnoreCase)
            && !manifest.LineEnding.Equals("CRLF", StringComparison.OrdinalIgnoreCase))
            errors.Add("line_ending 只能是 LF 或 CRLF");

        ValidateUniqueIds(manifest.Families.Select(f => f.Id), "family", errors);
        ValidateUniqueIds(manifest.Modules.Select(m => m.Id), "module", errors);
        ValidateUniqueIds(manifest.Features.Select(f => f.Id), "feature", errors);

        var featureIds = manifest.Features
            .Select(f => f.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var feature in manifest.Features)
        {
            if (string.IsNullOrWhiteSpace(feature.Id))
                errors.Add("feature ID 不能为空");
            if (string.IsNullOrWhiteSpace(feature.Label))
                errors.Add($"feature {feature.Id} 缺少 label");
        }

        foreach (var path in manifest.Outputs.ReferencedExisting)
            ValidateResourcePath(path, $"outputs.referenced-existing 条目 {path}", errors);

        var moduleMap = manifest.Modules
            .Where(m => !string.IsNullOrWhiteSpace(m.Id))
            .GroupBy(m => m.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        // 根变量条目的能力引用必须已注册，防止导航过滤因拼写错误静默失效
        foreach (var family in manifest.Families)
            ValidateRootVariables(family.RootDefaults, $"family {family.Id} rootDefaults", featureIds, errors);

        if (!moduleMap.Values.Any(m => !string.IsNullOrWhiteSpace(m.RootSource)))
            errors.Add("至少需要一个提供 rootSource 的公共模块");

        foreach (var module in manifest.Modules)
        {
            ValidateResourcePath(module.RootSource, $"module {module.Id} rootSource", errors, allowEmpty: true);
            if (!string.IsNullOrWhiteSpace(module.RootSource) && _resources.Read(module.RootSource) is null)
                errors.Add($"module {module.Id} 找不到根模板：{module.RootSource}");

            ValidateUniqueOutputs(module.Pointers, $"module {module.Id} pointer", errors);
            foreach (var pointer in module.Pointers)
                ValidateRequiredResource($"pointers/{pointer}", $"module {module.Id} pointer", errors);
            foreach (var file in module.Files)
            {
                ValidateRuleFile(file, $"module {module.Id}", errors);
                ValidateOutputMetadata(file.Feature, file.Merge, $"module {module.Id} file {file.Output}", featureIds, errors);
            }
        }

        CompositionValidator.ValidateWizard(manifest, errors);
        CompositionValidator.ValidateComponents(manifest, _resources, errors);
        CompositionValidator.ValidateConstraints(manifest, errors);
        CompositionValidator.ValidateCompatibilityCases(manifest, errors);

        if (errors.Count > 0)
            throw new InvalidOperationException("manifest 校验失败：\n- " + string.Join("\n- ", errors));
    }

    // 根变量条目的能力引用校验：带 features 的条目必须引用已注册能力；when 多档每档都必须声明条件
    internal static void ValidateRootVariables(
        RootVariables? root,
        string owner,
        IReadOnlySet<string> featureIds,
        ICollection<string> errors)
    {
        if (root is null)
            return;

        var lists = new (string Name, List<RootLine> Lines)[]
        {
            ("subrule-index", root.SubruleIndex),
            ("dev-stage", root.DevStage),
            ("understanding-checklist", root.UnderstandingChecklist),
            ("reading-order", root.ReadingOrder),
            ("extra-sections", root.ExtraSections)
        };

        foreach (var (name, lines) in lists)
        {
            foreach (var entry in lines)
            {
                ValidateRootLineFeatures(entry.Features, $"{owner} {name}", featureIds, errors);
                if (entry.When is { Count: > 0 })
                {
                    foreach (var variant in entry.When)
                        ValidateRootLineFeatures(variant.Features, $"{owner} {name} when", featureIds, errors);
                }
            }
        }
    }

    private static void ValidateRootLineFeatures(
        List<string> features,
        string field,
        IReadOnlySet<string> featureIds,
        ICollection<string> errors)
    {
        foreach (var feature in features)
        {
            if (!featureIds.Contains(feature))
                errors.Add($"{field} 的能力引用未在 manifest.features 注册：{feature}");
        }
    }

    // 输出声明元数据的合法性：feature 必须已注册，merge 只允许 replace / append
    internal static void ValidateOutputMetadata(
        string feature,
        string merge,
        string field,
        IReadOnlySet<string> featureIds,
        ICollection<string> errors)
    {
        if (!string.IsNullOrWhiteSpace(feature) && !featureIds.Contains(feature))
            errors.Add($"{field} 的 feature 未在 manifest.features 注册：{feature}");
        if (merge != OutputDeclaration.MergeReplace && merge != OutputDeclaration.MergeAppend)
            errors.Add($"{field} 的 merge 只能是 replace 或 append，当前为 {merge}");
    }

    internal void ValidateRequiredResource(string path, string field, ICollection<string> errors)
    {
        ValidateResourcePath(path, field, errors);
        if (!string.IsNullOrWhiteSpace(path) && _resources.Read(path) is null)
            errors.Add($"{field} 找不到资源：{path}");
    }

    internal void ValidateRuleFile(Subrule file, string owner, ICollection<string> errors)
    {
        if (file.Sources.Count > 0 && !string.IsNullOrWhiteSpace(file.Source))
            errors.Add($"{owner} 的 source 与 sources 不能同时配置");
        if (file.Sources.Count == 0 && string.IsNullOrWhiteSpace(file.Source))
            errors.Add($"{owner} 的 source 不能为空");

        foreach (var slot in file.Slots.Keys)
        {
            if (string.IsNullOrWhiteSpace(slot) || !SlotNamePattern.IsMatch(slot))
                errors.Add($"{owner} 的子规则槽位名非法：{slot}");
        }

        var sources = file.GetSources();
        for (var i = 0; i < sources.Count; i++)
        {
            ValidateResourcePath(sources[i], $"{owner} source[{i}]", errors);
            var raw = _resources.Read(sources[i]);
            if (raw is null)
            {
                errors.Add($"{owner} 找不到片段：{sources[i]}");
                continue;
            }

            // 多源拼接时 front matter 只能出现在首个源，否则版本盖写与正文结构都会被破坏
            if (i > 0 && raw.TrimStart().StartsWith("---", StringComparison.Ordinal))
                errors.Add($"{owner} 多源拼接的第 {i + 1} 个源不允许携带 front matter：{sources[i]}");
        }

        ValidateOutputPath(file.Output, $"{owner} output", errors);
    }

    private static void ValidateResourcePath(string path, string field, ICollection<string> errors, bool allowEmpty = false)
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

        if (!PathGuard.TryNormalizeRelative(path, out _))
            errors.Add($"{field} 不能包含绝对路径或 ..：{path}");
    }

    private static void ValidateUniqueOutputs(IEnumerable<string> outputs, string kind, ICollection<string> errors)
    {
        var duplicates = outputs
            .Where(output => !string.IsNullOrWhiteSpace(output))
            .GroupBy(output => PathGuard.TryNormalizeRelative(output, out var canonical)
                ? canonical
                : output.Trim().Replace('\\', '/'), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicates.Count > 0)
            errors.Add($"{kind} 输出重复：{string.Join("、", duplicates)}");
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
}
