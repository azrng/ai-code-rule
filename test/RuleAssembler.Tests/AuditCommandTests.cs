using System.Text.Json;

namespace RuleAssembler.Tests;

// --audit 审计命令回归：报告结构、五类检测、体积指标与退出语义（只报告不拦截）
public sealed class AuditCommandTests
{
    private static Manifest LoadManifest()
        => new ManifestLoader(new ResourceStore()).Load();

    [Fact]
    public void Run_OnRealRuleSource_ProducesMarkdownAndJsonReports()
    {
        var cwd = Environment.CurrentDirectory;
        var dir = Path.Combine(Path.GetTempPath(), "audit-tests-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            Environment.CurrentDirectory = dir;
            var exit = AuditCommand.Run(LoadManifest(), new ResourceStore(), new SkillsStore());

            Assert.Equal(0, exit);
            Assert.True(File.Exists(Path.Combine(dir, "rule-audit-report.md")), "markdown 报告应写入当前目录");
            var json = JsonSerializer.Deserialize<JsonElement>(
                File.ReadAllText(Path.Combine(dir, "rule-audit-report.json")));

            // JSON 契约：camelCase 字段 + findings 数组 + 逐组合体积指标
            Assert.Equal(LoadManifest().Vars.Version, json.GetProperty("version").GetString());
            Assert.True(json.GetProperty("caseCount").GetInt32() > 0);
            Assert.True(json.GetProperty("findings").GetArrayLength() >= 0);
            Assert.Equal(json.GetProperty("caseCount").GetInt32(), json.GetProperty("sizeMetrics").GetArrayLength());
        }
        finally
        {
            Environment.CurrentDirectory = cwd;
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Run_OnRealRuleSource_ReportsNoUnreachableFragments()
    {
        // 指针文件（pointers/CLAUDE.md 等）经 manifest pointment 声明可达；
        // 当前规则源不应存在全组合不可达片段——出现即说明审计判定回归
        var manifest = LoadManifest();
        var resources = new ResourceStore();
        var planner = new CompositionPlanner(manifest);
        var renderer = new RuleRenderer(resources);
        var rendered = manifest.CompatibilityCases
            .Select(c => (c, renderer.Plan(manifest, planner.Plan(new Selection
            {
                Dimensions = new Dictionary<string, string>(c.Selection, StringComparer.Ordinal)
            }), FeatureSelection.Defaults(manifest, planner.Plan(new Selection
            {
                Dimensions = new Dictionary<string, string>(c.Selection, StringComparer.Ordinal)
            })))))
            .ToList();

        var findings = AuditCommand.FindUnreferencedFragmentsForTest(manifest, resources, rendered);
        Assert.DoesNotContain(findings, f => f.Check == "不可达片段");
    }

    [Fact]
    public void DuplicateCandidates_ExcludePointerFilesAndFrontMatter()
    {
        // 指针文件（CLAUDE/GEMINI/GROK）与 front matter 的 version 行不参与重复判定
        var manifest = LoadManifest();
        var resources = new ResourceStore();
        var planner = new CompositionPlanner(manifest);
        var renderer = new RuleRenderer(resources);
        var rendered = new List<(CompatibilityCase, List<PlannedFile>)>();
        foreach (var c in manifest.CompatibilityCases)
        {
            var selection = new Selection { Dimensions = new Dictionary<string, string>(c.Selection, StringComparer.Ordinal) };
            var plan = planner.Plan(selection);
            rendered.Add((c, renderer.Plan(manifest, plan, FeatureSelection.Defaults(manifest, plan))));
        }

        var findings = AuditCommand.FindDuplicateCandidatesForTest(rendered);

        // 版本元数据行不报
        Assert.DoesNotContain(findings, f => f.Evidence.Contains("version:", StringComparison.Ordinal));
        // 指针文件不作为重复证据来源
        Assert.DoesNotContain(findings, f => f.Evidence.Contains("CLAUDE.md", StringComparison.Ordinal));
    }

    [Fact]
    public void SizeMetrics_CoverEveryCaseWithRootAndFixedSection()
    {
        var manifest = LoadManifest();
        var resources = new ResourceStore();
        var planner = new CompositionPlanner(manifest);
        var renderer = new RuleRenderer(resources);
        var rendered = new List<(CompatibilityCase, List<PlannedFile>)>();
        foreach (var c in manifest.CompatibilityCases)
        {
            var selection = new Selection { Dimensions = new Dictionary<string, string>(c.Selection, StringComparer.Ordinal) };
            var plan = planner.Plan(selection);
            rendered.Add((c, renderer.Plan(manifest, plan, FeatureSelection.Defaults(manifest, plan))));
        }

        var metrics = AuditCommand.MeasureSizesForTest(rendered);

        Assert.Equal(manifest.CompatibilityCases.Count, metrics.Count);
        Assert.All(metrics, m =>
        {
            Assert.True(m.RootBytes > 0, $"根文件字节应大于 0：{m.CaseId}");
            Assert.True(m.FixedSectionBytes > 0, $"固定规则段字节应大于 0：{m.CaseId}");
            Assert.True(m.SubruleCount > 0, $"子规则数应大于 0：{m.CaseId}");
            // 闭包至少含根文件自身
            Assert.True(m.ContextClosureBytes >= m.RootBytes, $"闭包应不小于根文件：{m.CaseId}");
        });
    }

    [Fact]
    public void SkillReferenceIssues_RealSkillsHaveNoMissingLink()
    {
        // 回归锚点：内置技能当前零缺失引用（skill-creator 围栏内的 FORMS.md 等示例链接不算）；
        // 未来新增技能引入真实缺失会在此暴露
        var issues = AuditCommand.FindSkillReferenceIssuesForTest(new SkillsStore());
        Assert.Empty(issues);
    }

    [Fact]
    public void SkillReferenceIssues_SkipFencedExampleLinks()
    {
        // 围栏内是「怎么写技能」的示例文本（引用假想文件），不算真实引用
        var content = """
            # 示例技能

            正文引用真实文件：见 [接口字段](references/openai_yaml.md)。

            ```markdown
            - **Form filling**: See [FORMS.md](FORMS.md) for complete guide
            - **Examples**: See [EXAMPLES.md](EXAMPLES.md) for common patterns
            ```
            """;
        var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "references/openai_yaml.md" };

        var issues = AuditCommand.FindSkillReferenceIssuesIn("demo", content, files);

        Assert.Empty(issues);
    }

    [Fact]
    public void SkillReferenceIssues_FlagMissingLinkOutsideFence()
    {
        var content = "见 [手册](docs/manual.md)。";
        var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "references/openai_yaml.md" };

        var issues = AuditCommand.FindSkillReferenceIssuesIn("demo", content, files);

        var issue = Assert.Single(issues);
        Assert.Contains("docs/manual.md", issue.Evidence, StringComparison.Ordinal);
    }
}
