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

        Validate(manifest);
        return manifest;
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
            ValidateUniqueOutputs(profile.Pointers, $"Profile {profile.Id} pointer", errors);
            foreach (var pointer in profile.Pointers)
                ValidateRequiredResource($"pointers/{pointer}", $"Profile {profile.Id} pointer", errors);

            ValidateProfileOutputDeclarations(profile, featureIds, errors);

            if (profile.DesignSystem)
            {
                var source = GetDesignSystemSource(profile);
                ValidateRequiredResource(source, $"Profile {profile.Id} design-system", errors);
            }

            ValidateProfileOutputConflicts(profile, moduleMap, errors);
        }

        ValidateWizard(manifest, errors);

        if (errors.Count > 0)
            throw new InvalidOperationException("manifest 校验失败：\n- " + string.Join("\n- ", errors));
    }

    // 决策树与 traits 的结构校验：trait 键合法、每个 Profile 的 trait 值都有展示文案，
    // 保证问答向导与检测器在任何候选集上都能渲染出可读选项
    private void ValidateWizard(Manifest manifest, ICollection<string> errors)
    {
        var wizard = manifest.Wizard;
        if (wizard.Steps.Count == 0)
        {
            errors.Add("wizard.steps 不能为空");
            return;
        }

        var knownTraits = ProfileTraits.KnownTraits.ToHashSet(StringComparer.Ordinal);
        var seenTraits = new HashSet<string>(StringComparer.Ordinal);
        foreach (var step in wizard.Steps)
        {
            if (string.IsNullOrWhiteSpace(step.Trait) || !knownTraits.Contains(step.Trait))
            {
                errors.Add($"wizard step trait 非法：{step.Trait}");
            }
            else if (!seenTraits.Add(step.Trait))
            {
                errors.Add($"wizard step trait 重复：{step.Trait}");
            }

            if (string.IsNullOrWhiteSpace(step.Question))
                errors.Add($"wizard step {step.Trait} 缺少 question");

            if (!string.IsNullOrWhiteSpace(step.AppliesWhenTrait)
                && (!knownTraits.Contains(step.AppliesWhenTrait) || step.AppliesWhenValues.Count == 0))
                errors.Add($"wizard step {step.Trait} 的先决条件配置不完整");

            if (!string.IsNullOrWhiteSpace(step.DetectDefault)
                && (!wizard.Options.TryGetValue(step.Trait, out var defaults)
                    || !defaults.ContainsKey(step.DetectDefault)))
                errors.Add($"wizard step {step.Trait} 的 detect-default 不在 options 中：{step.DetectDefault}");
        }

        foreach (var trait in wizard.Options.Keys)
        {
            if (!knownTraits.Contains(trait))
                errors.Add($"wizard options 出现未知 trait：{trait}");
        }

        foreach (var profile in manifest.Profiles)
        {
            if (profile.Traits is null)
            {
                errors.Add($"Profile {profile.Id} 缺少 traits");
                continue;
            }

            foreach (var trait in ProfileTraits.KnownTraits)
            {
                var value = profile.Traits.Get(trait);
                if (string.IsNullOrWhiteSpace(value))
                {
                    errors.Add($"Profile {profile.Id} traits 缺少 {trait}");
                }
                else if (value != ProfileTraits.Wildcard
                    && (!wizard.Options.TryGetValue(trait, out var options) || !options.ContainsKey(value)))
                {
                    errors.Add($"Profile {profile.Id} traits.{trait} 值 {value} 缺少 wizard.options 文案");
                }
            }
        }
    }

    // 输出声明元数据的合法性：feature 必须已注册，merge 只允许 replace / append
    private static void ValidateOutputMetadata(
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

    private void ValidateRequiredResource(string path, string field, ICollection<string> errors)
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

        if (!PathGuard.TryNormalizeRelative(path, out _))
            errors.Add($"{field} 不能包含绝对路径或 ..：{path}");
    }

    private static void ValidateProfileOutputConflicts(
        Profile profile,
        IReadOnlyDictionary<string, ModuleDefinition> moduleMap,
        ICollection<string> errors)
    {
        var outputs = new List<string> { "AGENTS.md" };
        var moduleIds = profile.ModuleIds.Count > 0
            ? profile.ModuleIds
            : new List<string> { "core" };
        var modules = moduleIds
            .Where(moduleMap.ContainsKey)
            .Select(id => moduleMap[id])
            .ToList();

        outputs.AddRange(modules.SelectMany(module => module.Files).Select(file => file.Output));
        outputs.AddRange(profile.Subrules.Select(file => file.Output));
        outputs.AddRange(modules
            .SelectMany(module => module.Pointers)
            .Concat(profile.Pointers)
            .Distinct(StringComparer.OrdinalIgnoreCase));

        if (profile.DesignSystem)
            outputs.Add("design-system.yaml");

        ValidateUniqueOutputs(outputs, $"Profile {profile.Id}", errors);
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

    internal static string GetDesignSystemSource(Profile profile)
    {
        if (!string.IsNullOrWhiteSpace(profile.DesignSystemSource))
            return profile.DesignSystemSource;
        if (!string.IsNullOrWhiteSpace(profile.SourceRoot))
            return profile.SourceRoot.TrimEnd('/', '\\') + "/design-system.yaml";
        return $"arch/{profile.Id}/design-system.yaml";
    }
}
