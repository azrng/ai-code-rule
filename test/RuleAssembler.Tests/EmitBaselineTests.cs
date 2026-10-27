using System.Text.Json;

namespace RuleAssembler.Tests;

// 产物回归基线：全部兼容样例经组合路径渲染的产物 SHA-256 清单，
// 是旧路径等价比对退役后的跨版本漂移守卫——组合内核自洽由契约校验与确定性测试保证，
// 内容层的意外漂移（改错片段、include 叠加、传导面遗漏）由本基线拦截。
// 规则内容升版时刷新基线：UPDATE_EMIT_BASELINE=1 dotnet test --filter EmitBaseline，
// 基线文件的 diff 即本次内容变更的产物审查清单，随变更一起提交
public sealed class EmitBaselineTests
{
    private static string BaselinePath => Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "emit-baseline.json");

    [Fact]
    public void 全部样例产物与基线哈希一致()
    {
        var manifest = new ManifestLoader(new ResourceStore()).Load();
        var renderer = new RuleRenderer(new ResourceStore());
        var planner = new CompositionPlanner(manifest);

        var current = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var caseItem in manifest.CompatibilityCases)
        {
            var plan = planner.Plan(new Selection
            {
                Dimensions = new Dictionary<string, string>(caseItem.Selection, StringComparer.Ordinal)
            });
            foreach (var file in renderer.Plan(manifest, plan, FeatureSelection.Defaults(manifest, plan)))
                current[$"{caseItem.Profile}/{file.Output}"] = RecipeRecorder.HashContent(file.Content);
        }

        Assert.Equal(36, manifest.CompatibilityCases.Count);
        Assert.True(current.Count > 100, "产物清单异常：兼容样例渲染结果过少");

        var baselineJson = File.Exists(BaselinePath)
            ? File.ReadAllText(BaselinePath)
            : null;

        if (Environment.GetEnvironmentVariable("UPDATE_EMIT_BASELINE") == "1")
        {
            WriteBaseline(current);
            return;
        }

        if (baselineJson is null)
        {
            Assert.Fail(
                $"产物基线不存在：{BaselinePath}。首次生成或内容升版时运行 "
                + "UPDATE_EMIT_BASELINE=1 dotnet test --filter EmitBaseline 并提交基线变更。");
            return;
        }

        var baseline = JsonSerializer.Deserialize<Dictionary<string, string>>(
            baselineJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        var missing = baseline.Keys.Where(key => !current.ContainsKey(key)).ToList();
        var added = current.Keys.Where(key => !baseline.ContainsKey(key)).ToList();
        var changed = current.Keys
            .Where(key => baseline.TryGetValue(key, out var hash) && hash != current[key])
            .ToList();

        if (missing.Count == 0 && added.Count == 0 && changed.Count == 0)
            return;

        var details = new List<string>();
        foreach (var key in missing.Take(10))
            details.Add($"基线有而产物缺少：{key}");
        foreach (var key in added.Take(10))
            details.Add($"产物多出（基线没有）：{key}");
        foreach (var key in changed.Take(20))
            details.Add($"内容变化：{key}");
        if (missing.Count + added.Count + changed.Count > details.Count)
            details.Add($"…等共 {missing.Count + added.Count + changed.Count} 处差异");

        Assert.Fail(
            "产物与 emit-baseline.json 不一致（意外漂移或忘记刷新基线）。 diffs：\n- "
            + string.Join("\n- ", details)
            + "\n确认变更符合预期后运行 UPDATE_EMIT_BASELINE=1 dotnet test --filter EmitBaseline 刷新基线并随变更提交。");
    }

    private static void WriteBaseline(SortedDictionary<string, string> hashes)
    {
        var json = JsonSerializer.Serialize(hashes, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
        File.WriteAllText(BaselinePath, json.Replace("\r\n", "\n") + "\n", new System.Text.UTF8Encoding(false));
    }
}
