namespace RuleAssembler;

// Q5 能力开关的状态推导：能力归属挂在输出声明上（片段 feature 标签 / designSystem 来源），
// 不靠输出文件名或字符串标签反推。
// 默认启用 = 组合计划已包含的能力，保证默认生成结果与历史组合基线一致；
// ai-capability 的启用与否由向导 AI 问答这一唯一事实来源决定，Q5 只展示不调整。
internal static class FeatureSelection
{
    // 可用能力：按 manifest.features 注册顺序返回，供 Q5 渲染；不可用能力必须隐藏
    public static List<FeatureDefinition> Available(Manifest manifest, CompositionPlan plan)
    {
        return manifest.Features
            .Where(feature => IsAvailable(manifest, plan, feature.Id))
            .ToList();
    }

    // design-system 可用性由计划是否携带来源决定，其余看文件贡献声明的能力归属
    public static bool IsAvailable(Manifest manifest, CompositionPlan plan, string featureId)
    {
        if (featureId == RootConsistencyValidator.DesignSystemFeature)
            return plan.DesignSystemSource.Length > 0;
        return DeclaredFeatures(manifest, plan).Contains(featureId, StringComparer.OrdinalIgnoreCase);
    }

    // 组合计划的默认能力集：可用的能力全部默认勾选
    public static HashSet<string> Defaults(Manifest manifest, CompositionPlan plan)
    {
        return manifest.Features
            .Where(feature => IsAvailable(manifest, plan, feature.Id))
            .Select(feature => feature.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    // 归一化用户选择：剔除不可用能力与未注册 ID，防止不可用的能力因默认值被选中；
    // 同时保留 Selection 中已确认的能力（如向导 AI 问答结果）
    public static HashSet<string> Normalize(Manifest manifest, CompositionPlan plan, IEnumerable<string> selected)
    {
        return selected
            .Where(featureId => IsAvailable(manifest, plan, featureId))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    // 组合计划的能力归属（核心模块文件 + 组件文件贡献；design-system 走 plan.DesignSystemSource）
    private static HashSet<string> DeclaredFeatures(Manifest manifest, CompositionPlan plan)
    {
        var features = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var moduleId in plan.ModuleIds.Count > 0 ? plan.ModuleIds : new List<string> { "core" })
        {
            var module = manifest.Modules.FirstOrDefault(item =>
                item.Id.Equals(moduleId, StringComparison.OrdinalIgnoreCase));
            if (module is null)
                continue;
            foreach (var file in module.Files)
            {
                if (!string.IsNullOrWhiteSpace(file.Feature))
                    features.Add(file.Feature);
            }
        }

        foreach (var file in plan.Files)
        {
            if (!string.IsNullOrWhiteSpace(file.Feature))
                features.Add(file.Feature);
        }

        return features;
    }
}
