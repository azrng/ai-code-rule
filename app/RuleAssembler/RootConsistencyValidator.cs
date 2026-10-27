namespace RuleAssembler;

// 结构化输出契约与 RuleRenderer.Plan 实际产物的一致性校验。
// 契约的事实来源是 manifest 的结构化声明（片段 output、指针、designSystem、显式 outputs、全局 referenced-existing），
// 根文件中的 subrule-index Markdown 仅作展示，本类不做任何自然语言解析。
// 声明推导的遍历顺序与 RuleRenderer.Plan 保持一致：根文件 → 模块文件 → 子规则/组件文件 → 指针 → design-system。
internal sealed class RootConsistencyValidator
{
    // design-system 输出由 designSystem 声明派生，能力归属固定为该 ID
    public const string DesignSystemFeature = "design-system";

    // 组合口径（--check 契约校验与 --emit 的生产路径）：契约事实来源是 CompositionPlan
    // （模块、组件文件贡献、design-system 来源），不依赖 Profile 声明；label 用于错误定位
    public List<string> Validate(
        Manifest manifest, RuleRenderer renderer, CompositionPlan plan, IReadOnlySet<string> features, string label)
    {
        var errors = new List<string>();
        var sink = new DeclarationSink(manifest, $"组合 {label}", features, errors);

        // 根文件始终生成，归属核心（无 feature）
        sink.AddGenerated("AGENTS.md", "", OutputDeclaration.MergeReplace);

        var modules = ResolveModulesByIds(manifest, plan.ModuleIds);
        foreach (var module in modules)
        {
            foreach (var file in module.Files)
                sink.AddGenerated(file.Output, file.Feature, file.Merge);
        }
        foreach (var pointer in modules.SelectMany(module => module.Pointers)
            .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            sink.AddGenerated(pointer, "", OutputDeclaration.MergeReplace);
        }

        // 组件文件贡献：与渲染器同序（FileContributions 优先，退回 Files 扁平视图）
        var planFiles = plan.FileContributions.Count > 0
            ? plan.FileContributions
            : plan.Files.Select(file => new FileContribution
            {
                OwnerId = label,
                File = file
            }).ToList();
        foreach (var contribution in planFiles)
            sink.AddGenerated(contribution.File.Output, contribution.File.Feature, contribution.File.Merge);

        if (plan.DesignSystemSource.Length > 0)
            sink.AddGenerated("design-system.yaml", DesignSystemFeature, OutputDeclaration.MergeReplace);

        foreach (var path in manifest.Outputs.ReferencedExisting)
            sink.AddReferenced(path);

        sink.ValidateConflicts();
        if (errors.Count > 0)
            return errors;

        return CompareWithActual(sink, renderer.Plan(manifest, plan, features), $"组合 {label}", errors);
    }

    private static List<string> CompareWithActual(
        DeclarationSink sink, List<PlannedFile> planned, string label, List<string> errors)
    {
        var actual = planned
            .Select(file => file.Output)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var path in sink.Generated)
        {
            if (!actual.Contains(path))
                errors.Add(Inconsistency(label, path, OutputDeclaration.KindGenerated, "未生成"));
        }

        foreach (var path in sink.Referenced)
        {
            if (actual.Contains(path))
                errors.Add(Inconsistency(label, path, OutputDeclaration.KindReferencedExisting, "已生成"));
        }

        foreach (var path in actual)
        {
            if (!sink.Generated.Contains(path) && !sink.Referenced.Contains(path))
                errors.Add(Inconsistency(label, path, "缺失声明", "已生成"));
        }

        return errors;
    }

    private static List<ModuleDefinition> ResolveModulesByIds(Manifest manifest, List<string> moduleIds)
    {
        var modules = new List<ModuleDefinition>();
        foreach (var id in moduleIds.Count > 0 ? moduleIds : new List<string> { "core" })
        {
            var module = manifest.Modules.FirstOrDefault(item =>
                item.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            // 模块缺失由 ManifestLoader 与 CompositionPlanner 负责报错，这里跳过即可
            if (module is not null)
                modules.Add(module);
        }

        return modules;
    }

    private static string Inconsistency(string label, string path, string declared, string actual)
    {
        return $"输出契约不一致：{label} 路径 {path} 声明={declared} 实际={actual}";
    }

    // 声明收集器：组合口径与 Profile 口径共用的规范化、能力过滤与冲突检查
    private sealed class DeclarationSink
    {
        private readonly Manifest _manifest;
        private readonly string _label;
        private readonly IReadOnlySet<string> _features;
        private readonly ICollection<string> _errors;
        private readonly Dictionary<string, List<(string Feature, string Merge)>> _generatedContributions =
            new(StringComparer.OrdinalIgnoreCase);

        public HashSet<string> Generated { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> Referenced { get; } = new(StringComparer.OrdinalIgnoreCase);

        public DeclarationSink(Manifest manifest, string label, IReadOnlySet<string> features, ICollection<string> errors)
        {
            _manifest = manifest;
            _label = label;
            _features = features;
            _errors = errors;
        }

        public void AddGenerated(string path, string feature, string merge)
        {
            if (!IsFeatureEnabled(_features, feature))
                return;
            if (!PathGuard.TryNormalizeRelative(path, out var canonical))
            {
                _errors.Add(Illegal(path, "路径无效或越界"));
                return;
            }

            ValidateFeature(canonical, feature);
            ValidateMerge(canonical, merge);
            Generated.Add(canonical);
            if (!_generatedContributions.TryGetValue(canonical, out var contributions))
            {
                contributions = new List<(string Feature, string Merge)>();
                _generatedContributions[canonical] = contributions;
            }

            contributions.Add((feature, merge));
        }

        public void AddReferenced(string path)
        {
            if (!PathGuard.TryNormalizeRelative(path, out var canonical))
            {
                _errors.Add(Illegal(path, "路径无效或越界"));
                return;
            }

            if (!Referenced.Add(canonical))
                _errors.Add(Illegal(canonical, "重复声明 referenced-existing 输出"));
        }

        // Profile 显式输出声明的结构校验；组合口径没有显式声明，不走这里
        public void AddExplicit(OutputDeclaration declaration)
        {
            if (!PathGuard.TryNormalizeRelative(declaration.Path, out var canonical))
            {
                _errors.Add(Illegal(declaration.Path, "路径无效或越界"));
                return;
            }

            if (declaration.Kind != OutputDeclaration.KindGenerated
                && declaration.Kind != OutputDeclaration.KindReferencedExisting)
            {
                _errors.Add(Illegal(canonical, $"kind 值无效：{declaration.Kind}"));
                return;
            }

            if (declaration.Kind == OutputDeclaration.KindGenerated)
            {
                ValidateFeature(canonical, declaration.Feature);
                ValidateMerge(canonical, declaration.Merge);
                Generated.Add(canonical);
                if (!_generatedContributions.TryGetValue(canonical, out var contributions))
                {
                    contributions = new List<(string Feature, string Merge)>();
                    _generatedContributions[canonical] = contributions;
                }

                contributions.Add((declaration.Feature, declaration.Merge));
            }
            else
            {
                if (declaration.Feature.Length > 0 || declaration.Merge != OutputDeclaration.MergeReplace)
                    _errors.Add(Illegal(canonical, "referenced-existing 声明不支持 feature / merge 字段"));
                if (!Referenced.Add(canonical))
                    _errors.Add(Illegal(canonical, "重复声明 referenced-existing 输出"));
            }
        }

        public void ValidateConflicts()
        {
            // 声明本身非法时跳过冲突检查，避免错误噪音掩盖根因（与旧实现行为一致）
            if (_errors.Count > 0)
                return;

            // 同一路径不能既是生成目标又是引用现有文件
            foreach (var canonical in Generated.Intersect(Referenced))
                _errors.Add(Illegal(canonical, "同时声明为 generated 与 referenced-existing"));

            foreach (var pair in _generatedContributions)
            {
                var replacements = pair.Value.Count(item => item.Merge == OutputDeclaration.MergeReplace);
                if (replacements > 1)
                    _errors.Add(Illegal(pair.Key, "存在多个 replace 提供者"));
                else if (replacements == 0)
                    _errors.Add(Illegal(pair.Key, "只有 append，缺少 replace 基底"));
            }
        }

        private static bool IsFeatureEnabled(IReadOnlySet<string> features, string feature)
        {
            return string.IsNullOrWhiteSpace(feature) || features.Contains(feature);
        }

        private void ValidateFeature(string path, string feature)
        {
            if (feature.Length == 0)
                return;
            if (!_manifest.Features.Any(item => item.Id.Equals(feature, StringComparison.OrdinalIgnoreCase)))
                _errors.Add(Illegal(path, $"feature 未在 manifest.features 注册：{feature}"));
        }

        private void ValidateMerge(string path, string merge)
        {
            if (merge != OutputDeclaration.MergeReplace && merge != OutputDeclaration.MergeAppend)
                _errors.Add(Illegal(path, $"merge 值无效：{merge}"));
        }

        private string Illegal(string path, string reason)
        {
            return $"输出声明非法：{_label} 路径 {path}：{reason}";
        }
    }
}
