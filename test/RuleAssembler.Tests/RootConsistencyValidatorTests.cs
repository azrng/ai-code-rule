namespace RuleAssembler.Tests;

// P1-1 结构化输出契约：声明与 RuleRenderer.Plan 实际产物的一致性校验
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

    [Fact]
    public void Validate_AllProfiles_ContractMatchesPlan()
    {
        Assert.Equal(29, _manifest.Profiles.Count);
        foreach (var profile in _manifest.Profiles)
        {
            var errors = _validator.Validate(_manifest, _renderer, profile);
            Assert.True(errors.Count == 0, $"{profile.Id}: {string.Join("；", errors)}");
        }
    }

    [Fact]
    public void Validate_TaskMdReferencedExisting_Passes()
    {
        // TASK.md 只出现在根文件展示文案中，不由生成器释放；
        // 契约按 referenced-existing 声明时校验必须通过
        Assert.Contains("TASK.md", _manifest.Outputs.ReferencedExisting, StringComparer.Ordinal);
        foreach (var profile in _manifest.Profiles)
        {
            var files = _renderer.Plan(_manifest, profile);
            Assert.DoesNotContain(files, file => file.Output.Equals("TASK.md", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void Validate_PointersWithoutRootDisplayText_DoNotAffectContract()
    {
        // 指针文件不需要在根文件目标清单重复展示；契约解析只依赖结构化声明。
        var profile = Assert.Single(_manifest.Profiles, item => item.Id == "reactTauriExe");
        var root = Assert.Single(_renderer.Plan(_manifest, profile), file => file.Output == "AGENTS.md");
        Assert.DoesNotContain("`CLAUDE.md` / `GEMINI.md` / `GROK.md`", root.Content, StringComparison.Ordinal);

        Assert.Empty(_validator.Validate(_manifest, _renderer, profile));
    }

    [Fact]
    public void Validate_GhostGeneratedDeclaration_FailsWithEntry()
    {
        var profile = _manifest.Profiles[0];
        profile.Outputs.Add(new OutputDeclaration
        {
            Path = "ghost.md",
            Kind = OutputDeclaration.KindGenerated
        });

        var errors = _validator.Validate(_manifest, _renderer, profile);

        var error = Assert.Single(errors);
        Assert.Contains(profile.Id, error, StringComparison.Ordinal);
        Assert.Contains("ghost.md", error, StringComparison.Ordinal);
        Assert.Contains("generated", error, StringComparison.Ordinal);
        Assert.Contains("未生成", error, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_TaskMdDeclaredGenerated_Fails()
    {
        var profile = _manifest.Profiles[0];
        profile.Outputs.Add(new OutputDeclaration
        {
            Path = "TASK.md",
            Kind = OutputDeclaration.KindGenerated
        });

        var errors = _validator.Validate(_manifest, _renderer, profile);

        // TASK.md 已在全局 referenced-existing 中：误标为 generated 直接报双重声明冲突，校验失败
        var error = Assert.Single(errors);
        Assert.Contains("TASK.md", error, StringComparison.Ordinal);
        Assert.Contains("同时声明", error, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_UnknownFeature_Fails()
    {
        var profile = _manifest.Profiles[0];
        profile.Outputs.Add(new OutputDeclaration
        {
            Path = "extra.md",
            Kind = OutputDeclaration.KindGenerated,
            Feature = "no-such-feature"
        });

        var errors = _validator.Validate(_manifest, _renderer, profile);

        Assert.Single(errors);
        Assert.Contains("feature 未在 manifest.features 注册：no-such-feature", errors[0], StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_InvalidMerge_Fails()
    {
        var profile = _manifest.Profiles[0];
        profile.Subrules[0].Merge = "merge-unknown";

        var errors = _validator.Validate(_manifest, _renderer, profile);

        // 声明非法时只报结构错误，不再比对产物
        Assert.Single(errors);
        Assert.Contains("merge 值无效", errors[0], StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_InvalidKind_Fails()
    {
        var profile = _manifest.Profiles[0];
        profile.Outputs.Add(new OutputDeclaration { Path = "extra.md", Kind = "unknown-kind" });

        var errors = _validator.Validate(_manifest, _renderer, profile);

        Assert.Single(errors);
        Assert.Contains("kind 值无效", errors[0], StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_PathEscapingRoot_Fails()
    {
        var profile = _manifest.Profiles[0];
        profile.Outputs.Add(new OutputDeclaration { Path = "../outside.md", Kind = OutputDeclaration.KindGenerated });

        var errors = _validator.Validate(_manifest, _renderer, profile);

        Assert.Single(errors);
        Assert.Contains("路径无效或越界", errors[0], StringComparison.Ordinal);
    }

    [Fact]
    public void Load_FeatureRegistryAndAttribution_AreConsistent()
    {
        Assert.Equal(
            new[] { "doc", "infra", "ai-capability", "design-system" },
            _manifest.Features.Select(feature => feature.Id).ToArray());

        // 两个 AI 模块（后端口径 / 前端口径）各自输出 ai-AGENTS.md，能力归属必须一致
        var aiFiles = _manifest.Modules
            .SelectMany(module => module.Files)
            .Where(file => file.Output == "ai-AGENTS.md")
            .ToList();
        Assert.Equal(2, aiFiles.Count);
        Assert.All(aiFiles, file => Assert.Equal("ai-capability", file.Feature));

        Assert.All(
            _manifest.Profiles.SelectMany(profile => profile.Subrules)
                .Where(rule => rule.Output == "doc-AGENTS.md"),
            rule => Assert.Equal("doc", rule.Feature));
        Assert.All(
            _manifest.Profiles.SelectMany(profile => profile.Subrules)
                .Where(rule => rule.Output is "infra-AGENTS.md" or "infrastructure-AGENTS.md"),
            rule => Assert.Equal("infra", rule.Feature));
    }

    [Fact]
    public void LoaderValidate_UnregisteredFeature_Throws()
    {
        var loader = new ManifestLoader(new ResourceStore());
        var manifest = loader.Load();
        var subrule = Assert.Single(
            manifest.Profiles[0].Subrules,
            rule => rule.Output == "backend-AGENTS.md");
        subrule.Feature = "unknown-feature";

        var exception = Assert.Throws<InvalidOperationException>(() => loader.Validate(manifest));

        Assert.Contains("feature 未在 manifest.features 注册", exception.Message);
    }

    [Fact]
    public void LoaderValidate_InvalidSubruleMerge_Throws()
    {
        var loader = new ManifestLoader(new ResourceStore());
        var manifest = loader.Load();
        manifest.Profiles[0].Subrules[0].Merge = "overwrite";

        var exception = Assert.Throws<InvalidOperationException>(() => loader.Validate(manifest));

        Assert.Contains("merge 只能是 replace 或 append", exception.Message);
    }
}
