namespace RuleAssembler;

// 结构化输出契约与 RuleRenderer.Plan 实际产物的一致性校验。
// 契约的事实来源是 manifest 的结构化声明（片段 output、指针、designSystem、显式 outputs、全局 referenced-existing），
// 根文件中的 target-list / subrule-index Markdown 仅作展示，本类不做任何自然语言解析。
// 声明推导的遍历顺序与 RuleRenderer.Plan 保持一致：根文件 → 模块文件 → Profile 子规则 → 指针 → design-system。
internal sealed class RootConsistencyValidator
{
    // design-system 输出由 profile.DesignSystem 开关派生，能力归属固定为该 ID
    public const string DesignSystemFeature = "design-system";

    public List<string> Validate(Manifest manifest, RuleRenderer renderer, Profile profile)
    {
        // 兼容路径：按默认能力集校验（与旧调用方行为一致）
        return Validate(manifest, renderer, profile, FeatureSelection.Defaults(manifest, profile));
    }

    public List<string> Validate(Manifest manifest, RuleRenderer renderer, Profile profile, IReadOnlySet<string> features)
    {
        var errors = new List<string>();
        var (generated, referenced) = CollectDeclarations(manifest, profile, features, errors);

        // 声明本身非法时不再比对产物，避免错误噪音掩盖根因
        if (errors.Count > 0)
            return errors;

        var actual = renderer
            .Plan(manifest, profile, features)
            .Select(file => file.Output)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var path in generated)
        {
            if (!actual.Contains(path))
                errors.Add(Inconsistency(profile, path, OutputDeclaration.KindGenerated, "未生成"));
        }

        foreach (var path in referenced)
        {
            if (actual.Contains(path))
                errors.Add(Inconsistency(profile, path, OutputDeclaration.KindReferencedExisting, "已生成"));
        }

        foreach (var path in actual)
        {
            if (!generated.Contains(path) && !referenced.Contains(path))
                errors.Add(Inconsistency(profile, path, "缺失声明", "已生成"));
        }

        return errors;
    }

    // 汇总声明：返回（generated 规范化路径集合, referenced-existing 规范化路径集合）；
    // 声明被关闭能力过滤，与按同一能力集渲染的实际产物对比
    private static (HashSet<string> Generated, HashSet<string> Referenced) CollectDeclarations(
        Manifest manifest,
        Profile profile,
        IReadOnlySet<string> features,
        ICollection<string> errors)
    {
        var generated = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var referenced = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var generatedContributions = new Dictionary<
            string, List<(string Feature, string Merge)>>(StringComparer.OrdinalIgnoreCase);

        void AddGenerated(string path, string feature, string merge)
        {
            if (!IsFeatureEnabled(features, feature))
                return;
            if (!PathGuard.TryNormalizeRelative(path, out var canonical))
            {
                errors.Add(Illegal(profile, path, "路径无效或越界"));
                return;
            }

            ValidateFeature(manifest, profile, canonical, feature, errors);
            ValidateMerge(profile, canonical, merge, errors);
            generated.Add(canonical);
            if (!generatedContributions.TryGetValue(canonical, out var contributions))
            {
                contributions = new List<(string Feature, string Merge)>();
                generatedContributions[canonical] = contributions;
            }

            contributions.Add((feature, merge));
        }

        // 根文件始终生成，归属核心（无 feature）
        AddGenerated("AGENTS.md", "", OutputDeclaration.MergeReplace);

        var modules = ResolveModules(manifest, profile);
        foreach (var module in modules)
        {
            foreach (var file in module.Files)
                AddGenerated(file.Output, file.Feature, file.Merge);
        }

        foreach (var file in profile.Subrules)
            AddGenerated(file.Output, file.Feature, file.Merge);

        var pointers = modules
            .SelectMany(module => module.Pointers)
            .Concat(profile.Pointers)
            .Distinct(StringComparer.OrdinalIgnoreCase);
        foreach (var pointer in pointers)
            AddGenerated(pointer, "", OutputDeclaration.MergeReplace);

        if (profile.DesignSystem && features.Contains(DesignSystemFeature))
            AddGenerated("design-system.yaml", DesignSystemFeature, OutputDeclaration.MergeReplace);

        // 全局“引用现有文件”默认清单，作用于所有 Profile
        foreach (var path in manifest.Outputs.ReferencedExisting)
        {
            if (!PathGuard.TryNormalizeRelative(path, out var canonical))
            {
                errors.Add(Illegal(profile, path, "路径无效或越界"));
                continue;
            }

            if (!referenced.Add(canonical))
                errors.Add(Illegal(profile, canonical, "重复声明 referenced-existing 输出"));
        }

        // Profile 显式声明：可补充 generated 或 referenced-existing 条目
        foreach (var declaration in profile.Outputs)
        {
            if (!PathGuard.TryNormalizeRelative(declaration.Path, out var canonical))
            {
                errors.Add(Illegal(profile, declaration.Path, "路径无效或越界"));
                continue;
            }

            if (declaration.Kind != OutputDeclaration.KindGenerated
                && declaration.Kind != OutputDeclaration.KindReferencedExisting)
            {
                errors.Add(Illegal(profile, canonical, $"kind 值无效：{declaration.Kind}"));
                continue;
            }

            if (declaration.Kind == OutputDeclaration.KindGenerated)
            {
                ValidateFeature(manifest, profile, canonical, declaration.Feature, errors);
                ValidateMerge(profile, canonical, declaration.Merge, errors);
                generated.Add(canonical);
                if (!generatedContributions.TryGetValue(canonical, out var contributions))
                {
                    contributions = new List<(string Feature, string Merge)>();
                    generatedContributions[canonical] = contributions;
                }

                contributions.Add((declaration.Feature, declaration.Merge));
            }
            else
            {
                if (declaration.Feature.Length > 0 || declaration.Merge != OutputDeclaration.MergeReplace)
                    errors.Add(Illegal(profile, canonical, "referenced-existing 声明不支持 feature / merge 字段"));
                if (!referenced.Add(canonical))
                    errors.Add(Illegal(profile, canonical, "重复声明 referenced-existing 输出"));
            }
        }

        // 同一路径不能既是生成目标又是引用现有文件
        foreach (var canonical in generated.Intersect(referenced))
            errors.Add(Illegal(profile, canonical, "同时声明为 generated 与 referenced-existing"));

        if (errors.Count == 0)
        {
            foreach (var pair in generatedContributions)
            {
                var replacements = pair.Value.Count(item => item.Merge == OutputDeclaration.MergeReplace);
                if (replacements > 1)
                    errors.Add(Illegal(profile, pair.Key, "存在多个 replace 提供者"));
                else if (replacements == 0)
                    errors.Add(Illegal(profile, pair.Key, "只有 append，缺少 replace 基底"));
            }
        }

        return (generated, referenced);
    }

    private static bool IsFeatureEnabled(IReadOnlySet<string> features, string feature)
    {
        return string.IsNullOrWhiteSpace(feature) || features.Contains(feature);
    }

    private static List<ModuleDefinition> ResolveModules(Manifest manifest, Profile profile)
    {
        var modules = new List<ModuleDefinition>();
        foreach (var id in profile.ModuleIds.Count > 0 ? profile.ModuleIds : new List<string> { "core" })
        {
            var module = manifest.Modules.FirstOrDefault(item =>
                item.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            // 模块缺失由 ManifestLoader 与 Plan 负责报错，这里跳过即可
            if (module is not null)
                modules.Add(module);
        }

        return modules;
    }

    private static void ValidateFeature(
        Manifest manifest, Profile profile, string path, string feature, ICollection<string> errors)
    {
        if (feature.Length == 0)
            return;
        if (!manifest.Features.Any(item => item.Id.Equals(feature, StringComparison.OrdinalIgnoreCase)))
            errors.Add(Illegal(profile, path, $"feature 未在 manifest.features 注册：{feature}"));
    }

    private static void ValidateMerge(Profile profile, string path, string merge, ICollection<string> errors)
    {
        if (merge != OutputDeclaration.MergeReplace && merge != OutputDeclaration.MergeAppend)
            errors.Add(Illegal(profile, path, $"merge 值无效：{merge}"));
    }

    private static string Inconsistency(Profile profile, string path, string declared, string actual)
    {
        return $"输出契约不一致：Profile {profile.Id} 路径 {path} 声明={declared} 实际={actual}";
    }

    private static string Illegal(Profile profile, string path, string reason)
    {
        return $"输出声明非法：Profile {profile.Id} 路径 {path}：{reason}";
    }
}
