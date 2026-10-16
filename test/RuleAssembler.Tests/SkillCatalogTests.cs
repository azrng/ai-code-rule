namespace RuleAssembler.Tests;

// 技能目录测试：manifest.Skills 的推荐预选（when 命中语义）与分组构建（组序 / 兜底组）。
// 装载真实 manifest + 内嵌技能资源，顺带钉住「登记 ID 必须与可发现技能一致」的仓库不变量
public sealed class SkillCatalogTests
{
    private static Manifest LoadManifest()
    {
        return new ManifestLoader(new ResourceStore()).Load();
    }

    private static List<SkillInfo> DiscoverSkills()
    {
        return new SkillsStore().Discover().ToList();
    }

    private static Selection SelectionOf(params (string trait, string value)[] dims)
    {
        var selection = new Selection();
        foreach (var (trait, value) in dims)
            selection.Dimensions[trait] = value;
        return selection;
    }

    private static List<string> RecommendedIds(Manifest manifest, Selection selection)
    {
        var skills = DiscoverSkills();
        return SkillCatalog.Recommend(manifest, selection, skills)
            .Select(index => skills[index].Id)
            .ToList();
    }

    [Fact]
    public void 无上下文时不做任何推荐()
    {
        var manifest = LoadManifest();
        var skills = DiscoverSkills();

        var recommended = SkillCatalog.Recommend(manifest, selection: null, skills: skills);

        Assert.Empty(recommended);
    }

    [Fact]
    public void dotnet_后端组合预选后端与测试技能且不含前端技能()
    {
        var manifest = LoadManifest();
        var recommended = RecommendedIds(manifest, SelectionOf(
            ("form", "backend-only"),
            ("backend", "dotnet"),
            ("frontend", "none"),
            ("arch-style", "layered")));

        Assert.Contains("aspnet-core", recommended);
        Assert.Contains("dotnet-best-practices", recommended);
        Assert.Contains("onion-architecture", recommended);
        Assert.Contains("run-tests", recommended);
        Assert.DoesNotContain(recommended, id => id == "maf"); // 未开启 ai-capability 时 maf 不推荐
    }

    [Fact]
    public void maf_仅在_ai_能力开启时推荐()
    {
        var manifest = LoadManifest();
        var withoutAi = RecommendedIds(manifest, SelectionOf(
            ("form", "web"), ("backend", "dotnet"), ("frontend", "react"), ("ai-capability", "no")));
        var withAi = RecommendedIds(manifest, SelectionOf(
            ("form", "web"), ("backend", "dotnet"), ("frontend", "react"), ("ai-capability", "yes")));

        Assert.DoesNotContain(withoutAi, id => id == "maf");
        Assert.Contains(withAi, id => id == "maf");
    }

    [Fact]
    public void 前端组合按形态预选前端技能()
    {
        var manifest = LoadManifest();
        var recommended = RecommendedIds(manifest, SelectionOf(
            ("form", "web"), ("backend", "none"), ("frontend", "vue")));

        Assert.Contains("frontend-design", recommended);
        Assert.Contains("webapp-testing", recommended); // frontend 任一命中：react / vue
        Assert.DoesNotContain(recommended, id => id == "aspnet-core");
        Assert.DoesNotContain(recommended, id => id == "flutter-implementation");
    }

    [Fact]
    public void 条件取值是任一命中_维度缺失视为证据不足()
    {
        var definition = new SkillDefinition
        {
            Id = "demo",
            When =
            [
                new Dictionary<string, List<string>> { ["backend"] = ["dotnet", "go"] },
            ],
        };

        Assert.True(SkillCatalog.Matches(definition, new Dictionary<string, string> { ["backend"] = "go" }));
        Assert.False(SkillCatalog.Matches(definition, new Dictionary<string, string> { ["backend"] = "python" }));
        Assert.False(SkillCatalog.Matches(definition, new Dictionary<string, string> { ["frontend"] = "go" }));
    }

    [Fact]
    public void 多组条件任一组命中即可_无条件恒推荐()
    {
        var multiGroup = new SkillDefinition
        {
            Id = "demo",
            When =
            [
                new Dictionary<string, List<string>> { ["backend"] = ["dotnet"] },
                new Dictionary<string, List<string>> { ["frontend"] = ["avalonia"] },
            ],
        };
        var always = new SkillDefinition { Id = "demo", When = null };

        Assert.True(SkillCatalog.Matches(multiGroup, new Dictionary<string, string> { ["frontend"] = "avalonia" }));
        Assert.True(SkillCatalog.Matches(multiGroup, new Dictionary<string, string> { ["backend"] = "dotnet" }));
        Assert.False(SkillCatalog.Matches(multiGroup, new Dictionary<string, string> { ["backend"] = "python" }));
        Assert.True(SkillCatalog.Matches(always, new Dictionary<string, string>()));
    }

    [Fact]
    public void 分组按声明顺序构建且每个技能恰好归属一组()
    {
        var manifest = LoadManifest();
        var skills = DiscoverSkills();

        var groups = SkillCatalog.BuildGroups(manifest, skills).ToList();

        Assert.Equal(manifest.Skills.Select(def => def.Group).Distinct().Count(), groups.Count);
        var grouped = groups.SelectMany(group => group.SkillIndices).OrderBy(index => index).ToList();
        Assert.Equal(Enumerable.Range(0, skills.Count), grouped);
        Assert.DoesNotContain(groups, group => group.Title == SkillCatalog.FallbackGroup);
    }

    [Fact]
    public void 未登记技能兜底到其他组()
    {
        var manifest = LoadManifest();
        var skills = DiscoverSkills();
        skills.Add(new SkillInfo("orphan-skill", "orphan", "未登记技能", 1));

        var groups = SkillCatalog.BuildGroups(manifest, skills).ToList();

        var fallback = groups.Single(group => group.Title == SkillCatalog.FallbackGroup);
        var orphan = Assert.Single(fallback.SkillIndices);
        Assert.Equal("orphan-skill", skills[orphan].Id);
    }

    [Fact]
    public void 登记的技能ID必须与可发现技能一致()
    {
        var manifest = LoadManifest();
        var discovered = DiscoverSkills().Select(skill => skill.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.NotEmpty(manifest.Skills);
        Assert.All(manifest.Skills, def => Assert.Contains(def.Id, discovered));
    }
}
