using System.Text.Json;

namespace RuleAssembler;

// P2 阶段 4：按配方记录（.ai-rule-profile.json）重放生成。
// 配方是可复现输入的唯一事实来源：selection 重建组合选择（旧配方仅有 profile_id 时经兼容样例反查），
// 组件集合必须与当前 manifest 解析结果一致（不一致说明规则演进过，需人工确认而不是静默生成），
// 能力开关按配方重放（空 = 当前默认集）；design-system 的「已定制保留」语义与交互路径一致。
internal sealed class RecipeReplayer
{
    private readonly Manifest _manifest;
    private readonly RuleRenderer _renderer;

    public RecipeReplayer(Manifest manifest, RuleRenderer renderer)
    {
        _manifest = manifest;
        _renderer = renderer;
    }

    public sealed record ReplayResult(
        CompositionPlan Plan,
        HashSet<string> Features,
        List<PlannedFile> Files,
        RecipeRecord Record,
        List<string> Notes);

    public ReplayResult Replay(string recipeJson, string targetDirectory)
    {
        RecipeRecord? recipe;
        try
        {
            recipe = JsonSerializer.Deserialize<RecipeRecord>(recipeJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("配方文件不是有效的 JSON。", ex);
        }

        if (recipe is null)
            throw new InvalidOperationException("配方文件内容为空。");

        var dimensions = recipe.Selection is { Count: > 0 }
            ? recipe.Selection
            : FromLegacyProfile(recipe);
        var selection = new Selection
        {
            Dimensions = new Dictionary<string, string>(dimensions, StringComparer.Ordinal)
        };
        var plan = new CompositionPlanner(_manifest).Plan(selection);

        // 组件集合是配方结构一致性的锚点：当前 manifest 解析不出同一组件集，说明组合已演进
        if (recipe.ComponentIds is { Count: > 0 }
            && !plan.ComponentIds.SequenceEqual(recipe.ComponentIds, StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                $"配方与当前规则版本不一致：配方组件 [{string.Join("、", recipe.ComponentIds)}]，"
                + $"当前解析 [{string.Join("、", plan.ComponentIds)}]。请重新运行生成器确认组合后再生成。");
        }

        var features = recipe.Features is { Count: > 0 }
            ? FeatureSelection.Normalize(_manifest, plan, recipe.Features)
            : FeatureSelection.Defaults(_manifest, plan);
        var files = _renderer.Plan(_manifest, plan, features);

        var notes = new List<string>();
        if (!string.IsNullOrWhiteSpace(recipe.ManifestVersion)
            && !recipe.ManifestVersion.Equals(_manifest.Vars.Version, StringComparison.Ordinal))
        {
            notes.Add($"配方记录于规则版本 {recipe.ManifestVersion}，当前为 {_manifest.Vars.Version}：组合结构一致，正文按当前规则内容生成");
        }

        // 已定制的设计系统文件属于产品资产：重放同样保留本地版本
        if (files.Any(file => file.Output.Equals("design-system.yaml", StringComparison.OrdinalIgnoreCase))
            && DesignSystemGuard.ShouldPreserve(targetDirectory))
        {
            files = files
                .Where(file => !file.Output.Equals("design-system.yaml", StringComparison.OrdinalIgnoreCase))
                .ToList();
            notes.Add("检测到已定制的 design-system.yaml：保留本地版本，本次不覆盖");
        }

        // 回写配方：版本与时间刷新为本次重放，selection / component_ids / 能力随之更新
        var record = RecipeRecorder.BuildComposition(
            _manifest, selection, plan.ComponentIds, recipe.Mode,
            recipe.Answers, recipe.AnswerSources, features, files);

        return new ReplayResult(plan, features, files, record, notes);
    }

    // 旧配方兼容：早期记录只有 profile_id，经兼容样例反查完整维度组合
    private Dictionary<string, string> FromLegacyProfile(RecipeRecord recipe)
    {
        if (string.IsNullOrWhiteSpace(recipe.ProfileId))
            throw new InvalidOperationException("配方缺少 selection 与 profile_id，无法重建组合选择。");

        var caseItem = _manifest.CompatibilityCases.FirstOrDefault(item =>
            item.Profile.Equals(recipe.ProfileId, StringComparison.OrdinalIgnoreCase));
        if (caseItem is null)
            throw new InvalidOperationException(
                $"配方仅有 profile_id={recipe.ProfileId}，当前规则未注册对应兼容样例，无法重放。");

        return caseItem.Selection;
    }
}
