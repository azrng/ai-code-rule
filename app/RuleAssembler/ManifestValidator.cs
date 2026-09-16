using System.Text.RegularExpressions;

namespace RuleAssembler;

// manifest 结构校验（旧 Profile / 模块 / family 域 + 共享低层规则）：
// schema 与 ID 唯一性、模块与 Profile 的资源引用、根变量能力引用、输出声明与合并冲突；
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

        var familyIds = manifest.Families
            .Select(f => f.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var moduleMap = manifest.Modules
            .Where(m => !string.IsNullOrWhiteSpace(m.Id))
            .GroupBy(m => m.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        // 根变量条目的能力引用必须已注册，防止导航过滤因拼写错误静默失效
        foreach (var family in manifest.Families)
            ValidateRootVariables(family.RootDefaults, $"family {family.Id} rootDefaults", featureIds, errors);
        foreach (var profile in manifest.Profiles)
            ValidateRootVariables(profile.Root, $"Profile {profile.Id} root", featureIds, errors);

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

        foreach (var profile in manifest.Profiles)
        {
            if (string.IsNullOrWhiteSpace(profile.Id))
                errors.Add("Profile ID 不能为空");
            else if (!PathGuard.IsSafeProfileId(profile.Id))
                errors.Add($"Profile {profile.Id} ID 必须是安全的单级目录名");

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
            {
                ValidateRuleFile(file, $"Profile {profile.Id}", errors);
                ValidateOutputMetadata(file.Feature, file.Merge, $"Profile {profile.Id} file {file.Output}", featureIds, errors);
            }
            foreach (var slot in profile.Slots.Keys)
            {
                if (string.IsNullOrWhiteSpace(slot) || !SlotNamePattern.IsMatch(slot))
                    errors.Add($"Profile {profile.Id} 的槽位名非法：{slot}");
            }
            ValidateUniqueOutputs(profile.Pointers, $"Profile {profile.Id} pointer", errors);
            foreach (var pointer in profile.Pointers)
                ValidateRequiredResource($"pointers/{pointer}", $"Profile {profile.Id} pointer", errors);

            ValidateProfileOutputDeclarations(profile, featureIds, errors);

            if (profile.DesignSystem)
            {
                var source = ManifestLoader.GetDesignSystemSource(profile);
                ValidateRequiredResource(source, $"Profile {profile.Id} design-system", errors);
            }

            ValidateProfileOutputConflicts(profile, moduleMap, errors);
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
            ("target-list", root.TargetList),
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

    // Profile 显式输出声明的结构校验；与派生清单的交叉一致性由 RootConsistencyValidator 负责
    private void ValidateProfileOutputDeclarations(
        Profile profile,
        IReadOnlySet<string> featureIds,
        ICollection<string> errors)
    {
        foreach (var declaration in profile.Outputs)
        {
            var field = $"Profile {profile.Id} outputs[{declaration.Path}]";
            ValidateResourcePath(declaration.Path, field, errors);
            if (declaration.Kind != OutputDeclaration.KindGenerated
                && declaration.Kind != OutputDeclaration.KindReferencedExisting)
                errors.Add($"{field} 的 kind 只能是 generated 或 referenced-existing，当前为 {declaration.Kind}");
            ValidateOutputMetadata(declaration.Feature, declaration.Merge, field, featureIds, errors);
        }

        ValidateUniqueOutputs(
            profile.Outputs.Select(declaration => declaration.Path),
            $"Profile {profile.Id} outputs",
            errors);
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

    private static void ValidateProfileOutputConflicts(
        Profile profile,
        IReadOnlyDictionary<string, ModuleDefinition> moduleMap,
        ICollection<string> errors)
    {
        var outputs = new List<(string Path, string Merge)>
        {
            ("AGENTS.md", OutputDeclaration.MergeReplace)
        };
        var moduleIds = profile.ModuleIds.Count > 0
            ? profile.ModuleIds
            : new List<string> { "core" };
        var modules = moduleIds
            .Where(moduleMap.ContainsKey)
            .Select(id => moduleMap[id])
            .ToList();

        outputs.AddRange(modules
            .SelectMany(module => module.Files)
            .Select(file => (file.Output, file.Merge)));
        outputs.AddRange(profile.Subrules.Select(file => (file.Output, file.Merge)));
        outputs.AddRange(modules
            .SelectMany(module => module.Pointers)
            .Concat(profile.Pointers)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(pointer => (pointer, OutputDeclaration.MergeReplace)));

        if (profile.DesignSystem)
            outputs.Add(("design-system.yaml", OutputDeclaration.MergeReplace));

        ValidateOutputMergeConflicts(outputs, $"Profile {profile.Id}", errors);
    }

    private static void ValidateOutputMergeConflicts(
        IEnumerable<(string Path, string Merge)> declarations,
        string kind,
        ICollection<string> errors)
    {
        var groups = declarations
            .Where(item => !string.IsNullOrWhiteSpace(item.Path))
            .GroupBy(item => PathGuard.TryNormalizeRelative(item.Path, out var canonical)
                ? canonical
                : item.Path.Trim().Replace('\\', '/'), StringComparer.OrdinalIgnoreCase);

        foreach (var group in groups)
        {
            var replaceCount = group.Count(item => item.Merge == OutputDeclaration.MergeReplace);
            if (replaceCount > 1)
                errors.Add($"{kind} 输出重复：{group.Key} 存在多个 replace 提供者");
            else if (replaceCount == 0
                && group.Any(item => item.Merge == OutputDeclaration.MergeAppend))
                errors.Add($"{kind} 输出重复：{group.Key} 只有 append，缺少 replace 基底");
        }
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
