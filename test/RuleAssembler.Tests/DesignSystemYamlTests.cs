using YamlDotNet.Serialization;

namespace RuleAssembler.Tests;

// design-system.yaml 语法守卫：产物按 YAML 契约消费（目标项目脚本与规则层都会解析），
// 片段里引号未闭合、值后裸文本、行粘连等损坏此前能一路渲染到产物不被发现
// （staticHtml separator 全角说明、vueLinkdotNet el_size 行粘连）。
// 骨架模板（shared/skeletons 下含 {{slot:}} 占位）不直接发布，只校验渲染后的产物。
public sealed class DesignSystemYamlTests
{
    [Fact]
    public void 全部样例设计系统产物可被YAML解析()
    {
        var manifest = new ManifestLoader(new ResourceStore()).Load();
        var renderer = new RuleRenderer(new ResourceStore());
        var planner = new CompositionPlanner(manifest);
        var deserializer = new DeserializerBuilder().Build();

        var failures = new List<string>();
        foreach (var caseItem in manifest.CompatibilityCases)
        {
            var plan = planner.Plan(new Selection
            {
                Dimensions = new Dictionary<string, string>(caseItem.Selection, StringComparer.Ordinal)
            });
            foreach (var file in renderer.Plan(manifest, plan, FeatureSelection.Defaults(manifest, plan)))
            {
                if (!file.Output.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase))
                    continue;
                try
                {
                    deserializer.Deserialize<object>(file.Content);
                }
                catch (Exception ex)
                {
                    failures.Add($"{caseItem.Profile}/{file.Output}: {ex.Message.Split('\n')[0]}");
                }
            }
        }

        Assert.True(failures.Count == 0,
            "以下 design-system 产物不是合法 YAML：\n- " + string.Join("\n- ", failures));
    }
}
