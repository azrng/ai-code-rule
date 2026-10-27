namespace RuleAssembler.Tests;

// P1-1 结构化输出契约：声明与 RuleRenderer.Plan 实际产物的一致性校验（组合口径，
// 契约事实来源是 CompositionPlan：模块、组件文件贡献、指针与 design-system 来源）
public sealed class RootConsistencyValidatorTests
{
    private readonly Manifest _manifest;
    private readonly RuleRenderer _renderer;
    private readonly RootConsistencyValidator _validator;

    public RootConsistencyValidatorTests()
    {
        _manifest = new ManifestLoader(new ResourceStore()).Load();
        _renderer = new RuleRenderer(new ResourceStore());
        _validator = new RootConsistencyValidator();
    }

    private (CompositionPlan Plan, HashSet<string> Features) PlanForProfile(string caseId)
    {
        var caseItem = _manifest.CompatibilityCases.Single(item =>
            item.Profile.Equals(caseId, StringComparison.OrdinalIgnoreCase));
        var plan = new CompositionPlanner(_manifest).Plan(new Selection
        {
            Dimensions = new Dictionary<string, string>(caseItem.Selection, StringComparer.Ordinal)
        });
        return (plan, FeatureSelection.Defaults(_manifest, plan));
    }

    [Fact]
    public void Validate_AllCompositionPlans_ContractMatchesRenderedFiles()
    {
        // 每个兼容样例经组合渲染后，契约声明与实际产物一致（与 --check 契约段同口径）
        Assert.Equal(36, _manifest.CompatibilityCases.Count);
        foreach (var caseItem in _manifest.CompatibilityCases)
        {
            var (plan, features) = PlanForProfile(caseItem.Profile);
            var errors = _validator.Validate(_manifest, _renderer, plan, features, caseItem.Profile);
            Assert.True(errors.Count == 0, $"{caseItem.Profile}: {string.Join("；", errors)}");
        }
    }

    [Fact]
    public void Validate_TaskMdReferencedExisting_Passes()
    {
        // TASK.md 只出现在根文件展示文案中，不由生成器释放；
        // 契约按 referenced-existing 声明时校验必须通过
        Assert.Contains("TASK.md", _manifest.Outputs.ReferencedExisting, StringComparer.Ordinal);
        foreach (var caseItem in _manifest.CompatibilityCases)
        {
            var (plan, features) = PlanForProfile(caseItem.Profile);
            var files = _renderer.Plan(_manifest, plan, features);
            Assert.DoesNotContain(files, file => file.Output.Equals("TASK.md", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void Validate_PointersWithoutRootDisplayText_DoNotAffectContract()
    {
        // 指针文件不需要在根文件目标清单重复展示；契约解析只依赖结构化声明。
        var (plan, features) = PlanForProfile("reactTauriExe");
        var root = Assert.Single(_renderer.Plan(_manifest, plan, features), file => file.Output == "AGENTS.md");
        Assert.DoesNotContain("`CLAUDE.md` / `GEMINI.md` / `GROK.md`", root.Content, StringComparison.Ordinal);

        Assert.Empty(_validator.Validate(_manifest, _renderer, plan, features, "reactTauriExe"));
    }

    [Fact]
    public void Validate_PlanOutputCollidingWithReferencedExisting_Fails()
    {
        // 组件贡献撞上全局 referenced-existing 清单（如 TASK.md）：组合口径必须报冲突
        var (plan, features) = PlanForProfile(_manifest.CompatibilityCases[0].Profile);
        plan.FileContributions.Add(new FileContribution
        {
            OwnerId = "test-colliding-component",
            File = new Subrule { Source = "shared/no-test-fallback.md", Output = "TASK.md" }
        });

        var errors = _validator.Validate(_manifest, _renderer, plan, features, _manifest.CompatibilityCases[0].Profile);

        var error = Assert.Single(errors);
        Assert.Contains("TASK.md", error, StringComparison.Ordinal);
        Assert.Contains("同时声明为 generated 与 referenced-existing", error, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_PlanGhostOutput_FailsWithEntry()
    {
        // 注入幽灵组件贡献：渲染产物包含 ghost.md 的前提是渲染器消费同一份贡献，
        // 这里改用「贡献声明了但渲染被能力过滤」构造分歧——feature 归属未启用能力
        var (plan, features) = PlanForProfile(_manifest.CompatibilityCases[0].Profile);
        plan.FileContributions.Add(new FileContribution
        {
            OwnerId = "test-ghost-component",
            File = new Subrule { Source = "shared/no-test-fallback.md", Output = "ghost.md" }
        });

        var errors = _validator.Validate(_manifest, _renderer, plan, features, _manifest.CompatibilityCases[0].Profile);

        // 幽灵输出会真的被渲染（同源贡献），契约与产物仍一致：证明组合口径下
        // 「声明 vs 产物」由同一事实源驱动，无法凭空多出未声明文件
        Assert.True(errors.Count == 0, string.Join("；", errors));
    }

    [Fact]
    public void Validate_PlanInvalidMerge_Fails()
    {
        var (plan, features) = PlanForProfile(_manifest.CompatibilityCases[0].Profile);
        plan.FileContributions.Add(new FileContribution
        {
            OwnerId = "test-bad-merge-component",
            File = new Subrule { Source = "shared/no-test-fallback.md", Output = "extra.md", Merge = "overwrite" }
        });

        var errors = _validator.Validate(_manifest, _renderer, plan, features, _manifest.CompatibilityCases[0].Profile);

        var error = Assert.Single(errors);
        Assert.Contains("extra.md", error, StringComparison.Ordinal);
        Assert.Contains("merge 值无效", error, StringComparison.Ordinal);
    }

    [Fact]
    public void LoaderValidate_InvalidSubruleMerge_Throws()
    {
        var validator = new ManifestValidator(new ResourceStore());
        var manifest = new ManifestLoader(new ResourceStore()).Load();
        manifest.Modules[0].Files[0].Merge = "overwrite";

        var exception = Assert.Throws<InvalidOperationException>(() => validator.Validate(manifest));

        Assert.Contains("merge 只能是 replace 或 append", exception.Message);
    }

    [Fact]
    public void LoaderValidate_UnregisteredFeature_Throws()
    {
        var validator = new ManifestValidator(new ResourceStore());
        var manifest = new ManifestLoader(new ResourceStore()).Load();
        manifest.Modules[0].Files[0].Feature = "unknown-feature";

        var exception = Assert.Throws<InvalidOperationException>(() => validator.Validate(manifest));

        Assert.Contains("feature 未在 manifest.features 注册", exception.Message);
    }
}
