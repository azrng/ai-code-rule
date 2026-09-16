namespace RuleAssembler;

// 组合内核域校验：维度空间（wizard steps / options）、组件结构、维度约束与兼容样例。
// 组件间的组合冲突（同名输出、槽位重复、证据不足）由 CompositionPlanner 在解析时拦截
internal static class CompositionValidator
{
    // 组件结构校验：kind 合法、when 维度与取值可被问答/检测表达、文件与根变量声明完整
    internal static void ValidateComponents(
        Manifest manifest, ResourceStore resources, ICollection<string> errors)
    {
        var shared = new ManifestValidator(resources);
        ValidateUniqueIds(manifest.Components.Select(component => component.Id), "component", errors);
        var moduleIds = manifest.Modules
            .Select(module => module.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var component in manifest.Components)
        {
            if (string.IsNullOrWhiteSpace(component.Id))
                continue;

            if (!ComponentDefinition.KnownKinds.Contains(component.Kind, StringComparer.Ordinal))
                errors.Add($"component {component.Id} 的 kind 非法：{component.Kind}");
            if (string.IsNullOrWhiteSpace(component.Label))
                errors.Add($"component {component.Id} 缺少 label");

            foreach (var pair in component.When)
                ValidateDimensionValue(manifest, $"component {component.Id} when.{pair.Key}", pair.Key, pair.Value, errors);

            foreach (var moduleId in component.ModuleIds)
            {
                if (string.IsNullOrWhiteSpace(moduleId) || !moduleIds.Contains(moduleId))
                    errors.Add($"component {component.Id} 引用了不存在的 module：{moduleId}");
            }

            if (!string.IsNullOrWhiteSpace(component.Family)
                && !manifest.Families.Any(family => family.Id.Equals(component.Family, StringComparison.OrdinalIgnoreCase)))
            {
                errors.Add($"component {component.Id} 引用了不存在的 family：{component.Family}");
            }

            foreach (var slot in component.Slots.Keys)
            {
                if (string.IsNullOrWhiteSpace(slot) || !ManifestValidator.SlotNamePattern.IsMatch(slot))
                    errors.Add($"component {component.Id} 的槽位名非法：{slot}");
            }

            foreach (var file in component.Files)
            {
                shared.ValidateRuleFile(file, $"component {component.Id}", errors);
                ManifestValidator.ValidateOutputMetadata(
                    file.Feature, file.Merge, $"component {component.Id} file {file.Output}",
                    FeatureIds(manifest), errors);
            }

            if (component.Root is not null)
                ManifestValidator.ValidateRootVariables(
                    component.Root, $"component {component.Id} root", FeatureIds(manifest), errors);

            if (!string.IsNullOrWhiteSpace(component.DesignSystemSource))
                shared.ValidateRequiredResource(component.DesignSystemSource, $"component {component.Id} designSystemSource", errors);
        }
    }

    // 维度约束校验：条件与要求都必须是可表达的 trait 取值
    internal static void ValidateConstraints(Manifest manifest, ICollection<string> errors)
    {
        foreach (var constraint in manifest.Constraints)
        {
            ValidateDimensionPairs(manifest, "constraint when", constraint.When, errors);
            ValidateDimensionPairs(manifest, "constraint require", constraint.Require, errors);
            ValidateDimensionPairs(manifest, "constraint forbid", constraint.Forbid, errors);
        }
    }

    // 兼容样例校验：样例是组合空间的唯一注册表（profiles 节点已退役）——ID 唯一且安全、
    // 展示元数据（label / family）完整且合法、维度取值可表达；检测候选、经典入口与配方重放都按样例收敛
    internal static void ValidateCompatibilityCases(Manifest manifest, ICollection<string> errors)
    {
        var familyIds = manifest.Families
            .Select(family => family.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var item in manifest.CompatibilityCases)
        {
            if (string.IsNullOrWhiteSpace(item.Profile))
                errors.Add("compatibilityCases 的 profile ID 不能为空");
            else if (!PathGuard.IsSafeProfileId(item.Profile))
                errors.Add($"compatibilityCases {item.Profile} ID 必须是安全的单级目录名");

            if (string.IsNullOrWhiteSpace(item.Label))
                errors.Add($"compatibilityCases {item.Profile} 缺少 label");

            if (!familyIds.Contains(item.Family))
                errors.Add($"compatibilityCases {item.Profile} 引用了不存在的 family：{item.Family}");

            ValidateDimensionPairs(manifest, $"compatibilityCases {item.Profile} selection", item.Selection, errors);
        }

        var duplicateProfiles = manifest.CompatibilityCases
            .GroupBy(item => item.Profile, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();
        foreach (var id in duplicateProfiles)
            errors.Add($"compatibilityCases 样例 ID 重复：{id}");
    }

    // 向导结构校验：steps 定义维度空间（无重复、问题文案齐全），options 的每个维度
    // 与取值都有展示文案，保证问答与检测在任何候选集上都能渲染出可读选项
    internal static void ValidateWizard(Manifest manifest, ICollection<string> errors)
    {
        var wizard = manifest.Wizard;
        if (wizard.Steps.Count == 0)
        {
            errors.Add("wizard.steps 不能为空");
            return;
        }

        var stepTraits = wizard.Steps
            .Select(step => step.Trait)
            .ToHashSet(StringComparer.Ordinal);
        var seenTraits = new HashSet<string>(StringComparer.Ordinal);
        foreach (var step in wizard.Steps)
        {
            if (string.IsNullOrWhiteSpace(step.Trait))
                errors.Add("wizard step trait 不能为空");
            else if (!seenTraits.Add(step.Trait))
                errors.Add($"wizard step trait 重复：{step.Trait}");

            if (string.IsNullOrWhiteSpace(step.Question))
                errors.Add($"wizard step {step.Trait} 缺少 question");

            if (!string.IsNullOrWhiteSpace(step.Absent)
                && (!wizard.Options.TryGetValue(step.Trait, out var absentOptions)
                    || !absentOptions.ContainsKey(step.Absent)))
                errors.Add($"wizard step {step.Trait} 的 absent 不在 options 中：{step.Absent}");
        }

        foreach (var trait in wizard.Options.Keys)
        {
            if (!stepTraits.Contains(trait))
                errors.Add($"wizard options 出现未知 trait：{trait}");
        }

        // 每个维度的取值都必须有展示文案：无 options 的维度在问答里渲染不出选项
        foreach (var trait in stepTraits)
        {
            if (!wizard.Options.TryGetValue(trait, out var options) || options.Count == 0)
                errors.Add($"维度 {trait} 缺少 wizard.options 取值文案");
        }
    }

    private static void ValidateDimensionPairs(
        Manifest manifest, string field, Dictionary<string, string> pairs, ICollection<string> errors)
    {
        foreach (var pair in pairs)
            ValidateDimensionValue(manifest, $"{field}.{pair.Key}", pair.Key, pair.Value, errors);
    }

    private static void ValidateDimensionValue(
        Manifest manifest, string field, string trait, string value, ICollection<string> errors)
    {
        // 维度空间的唯一事实来源是 wizard.steps：组件 when、约束与兼容样例引用的维度都必须在此声明
        if (!manifest.Wizard.Steps.Any(step => step.Trait == trait))
        {
            errors.Add($"{field} 引用了未知维度：{trait}");
            return;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{field} 的取值不能为空");
            return;
        }

        if (manifest.Wizard.Options.TryGetValue(trait, out var options)
            && !options.ContainsKey(value))
        {
            errors.Add($"{field} 的取值 {value} 缺少 wizard.options 文案");
        }
    }

    private static IReadOnlySet<string> FeatureIds(Manifest manifest)
    {
        return manifest.Features
            .Select(feature => feature.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
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
