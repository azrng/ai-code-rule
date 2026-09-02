using System.Text.RegularExpressions;

namespace RuleAssembler;

internal sealed class RuleRenderer
{
    private static readonly string[] OptionalSlots =
    {
        "{{intro-quote}}",
        "{{extra-sections}}",
        "{{model-behavior-extra}}",
        "{{verify-quote}}"
    };

    private readonly FragmentExpander _expander;

    public RuleRenderer(ResourceStore resources)
    {
        _expander = new FragmentExpander(resources);
    }

    public List<PlannedFile> Plan(Manifest manifest, Profile profile)
    {
        // 兼容路径：默认能力集 = Profile 当前组合（与 1.39.0 基线一致）
        return Plan(manifest, profile, FeatureSelection.Defaults(manifest, profile));
    }

    // 按 Q5 能力开关渲染：被关闭能力的输出与根导航条目整体移除
    public List<PlannedFile> Plan(Manifest manifest, Profile profile, IReadOnlySet<string> features)
    {
        var modules = ResolveModules(manifest, profile);
        var rootModule = modules.FirstOrDefault(module => !string.IsNullOrWhiteSpace(module.RootSource));
        if (rootModule is null)
            throw new InvalidOperationException($"Profile {profile.Id} 没有可用的公共根模块。");

        var rootTemplate = ReadRequired(rootModule.RootSource);
        var root = FillRootTemplate(rootTemplate, manifest, profile, features);
        var leftover = FindSlots(root);
        if (leftover.Count > 0)
            throw new InvalidOperationException($"Profile {profile.Id} 的根模板存在未填充变量：{string.Join("、", leftover)}");

        var result = new List<PlannedFile>();
        var outputs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddFile(result, outputs, "AGENTS.md", NormalizeNewlines(root, manifest.LineEnding));

        foreach (var module in modules)
        {
            foreach (var file in module.Files)
            {
                if (IsFeatureEnabled(features, file.Feature))
                    AddRuleFile(result, outputs, file, manifest);
            }
        }

        foreach (var file in profile.Subrules)
        {
            if (IsFeatureEnabled(features, file.Feature))
                AddRuleFile(result, outputs, file, manifest);
        }

        var pointers = modules
            .SelectMany(module => module.Pointers)
            .Concat(profile.Pointers)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        foreach (var pointer in pointers)
            AddFile(result, outputs, pointer, NormalizeNewlines(ReadRequired($"pointers/{pointer}"), manifest.LineEnding));

        if (profile.DesignSystem && features.Contains(RootConsistencyValidator.DesignSystemFeature))
        {
            var source = ManifestLoader.GetDesignSystemSource(profile);
            AddFile(result, outputs, "design-system.yaml", NormalizeNewlines(ReadRequired(source), manifest.LineEnding));
        }

        return result;
    }

    // 能力归属为空表示核心输出，始终生成；非空时按启用能力过滤
    private static bool IsFeatureEnabled(IReadOnlySet<string> features, string feature)
    {
        return string.IsNullOrWhiteSpace(feature) || features.Contains(feature);
    }

    private void AddRuleFile(List<PlannedFile> result, HashSet<string> outputs, Subrule file, Manifest manifest)
    {
        var content = CombineSources(file, manifest);
        AddFile(result, outputs, file.Output, NormalizeNewlines(content, manifest.LineEnding));
    }

    // 单源直接展开；多源先去各段尾部空白再以空行拼接，保证文件仍以单个换行结尾
    internal string CombineSources(Subrule file, Manifest manifest)
    {
        var sources = file.GetSources();
        var content = sources.Count == 1
            ? ReadRequired(sources[0])
            : string.Join("\n\n", sources.Select(source => ReadRequired(source).TrimEnd())) + "\n";
        return AlignVersion(content, manifest);
    }

    private static void AddFile(List<PlannedFile> result, HashSet<string> outputs, string output, string content)
    {
        if (!PathGuard.TryNormalizeRelative(output, out var canonical))
            throw new InvalidOperationException($"生成结果存在无效输出路径：{output}");
        if (!outputs.Add(canonical))
            throw new InvalidOperationException($"生成结果存在重复输出文件：{output}");
        result.Add(new PlannedFile { Output = output, Content = content });
    }

    private List<ModuleDefinition> ResolveModules(Manifest manifest, Profile profile)
    {
        var modules = new List<ModuleDefinition>();
        foreach (var id in profile.ModuleIds.Count > 0 ? profile.ModuleIds : new List<string> { "core" })
        {
            var module = manifest.Modules.FirstOrDefault(item =>
                item.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (module is null)
                throw new InvalidOperationException($"Profile {profile.Id} 引用了不存在的模块：{id}");
            modules.Add(module);
        }

        return modules;
    }

    private string ReadRequired(string path)
    {
        // 统一走 include 展开，保证根模板、子规则、指针与设计系统都能引用共享小节
        return _expander.Expand(path);
    }

    private static string FillRootTemplate(string template, Manifest manifest, Profile profile, IReadOnlySet<string> features)
    {
        var root = profile.Root;
        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["{{version}}"] = manifest.Vars.Version,
            ["{{last_updated}}"] = manifest.Vars.LastUpdated,
            ["{{target-list}}"] = RenderLines(root.TargetList, features),
            ["{{subrule-index}}"] = RenderLines(root.SubruleIndex, features),
            ["{{dev-stage}}"] = RenderLines(root.DevStage, features),
            ["{{understanding-checklist}}"] = RenderLines(root.UnderstandingChecklist, features),
            ["{{search-nouns}}"] = root.SearchNouns,
            ["{{root-scope-nouns}}"] = root.RootScopeNouns,
            ["{{context-scope-nouns}}"] = root.ContextScopeNouns,
            ["{{high-risk-nouns}}"] = root.HighRiskNouns,
            ["{{high-risk-prefix}}"] = root.HighRiskPrefix,
            ["{{task-kind-nouns}}"] = root.TaskKindNouns,
            ["{{evidence-nouns}}"] = root.EvidenceNouns,
            ["{{reuse-noun}}"] = root.ReuseNoun,
            ["{{persistence-suffix}}"] = root.PersistenceSuffix,
            ["{{destructive-nouns}}"] = root.DestructiveNouns,
            ["{{contract-error-line}}"] = root.ContractErrorLine,
            ["{{reading-order}}"] = root.ReadingOrder.Count > 0
                ? RenderLines(root.ReadingOrder, features)
                : "先读本文件确认全局规则与任务阶段 → 按任务类型命中对应子规则 → 再读相关模块与现有实现，确认复用点后动手。",
            ["{{intro-quote}}"] = root.IntroQuote,
            ["{{checklist-infra}}"] = root.ChecklistInfra,
            ["{{model-behavior-extra}}"] = root.ModelBehaviorExtra,
            ["{{verify-nouns}}"] = root.VerifyNouns,
            ["{{verify-quote}}"] = root.VerifyQuote,
            ["{{rollback-scope}}"] = root.RollbackScope,
            ["{{boundary-nouns}}"] = root.BoundaryNouns,
            ["{{persistence-line}}"] = root.PersistenceLine.Length > 0
                ? root.PersistenceLine
                : $"- 涉及新增、修改、删除等业务逻辑时，必须能真实生效并持久化{root.PersistenceSuffix}",
            ["{{extra-sections}}"] = string.Join("\n\n---\n\n", ResolveLines(root.ExtraSections, features))
        };

        var result = template;
        foreach (var slot in OptionalSlots)
        {
            if (values[slot].Length == 0)
            {
                result = new Regex(
                    $"(?m)^[ \\t]*{Regex.Escape(slot)}[ \\t]*(?:\\r?\\n|$)")
                    .Replace(result, "", 1);
            }
        }

        foreach (var pair in values)
            result = result.Replace(pair.Key, pair.Value, StringComparison.Ordinal);

        result = ApplyFeatureMarkers(result, manifest, features);

        // 空变量槽整行删除后可能残留连续空行，统一收敛为一个空行
        result = Regex.Replace(result, @"\n{3,}", "\n\n");
        return result;
    }

    // 渲染根变量条目：按启用能力过滤后，重排连续的「1. 」编号行，保证能力行移除后序号连续
    internal static string RenderLines(IReadOnlyList<RootLine> lines, IReadOnlySet<string> features)
    {
        var selected = ResolveLines(lines, features);

        var numberPattern = new Regex(@"^\d+\.\s");
        var counter = 0;
        for (var i = 0; i < selected.Count; i++)
        {
            var match = numberPattern.Match(selected[i]);
            if (match.Success)
            {
                selected[i] = $"{++counter}. {selected[i][match.Length..]}";
            }
            else
            {
                counter = 0;
            }
        }

        return string.Join("\n", selected);
    }

    // 仅按能力解析条目，不做编号重排；extra-sections 等独立 Markdown 块使用，
    // 条目内部自带的编号、表格等结构必须原样保留
    internal static List<string> ResolveLines(IReadOnlyList<RootLine> lines, IReadOnlySet<string> features)
    {
        var selected = new List<string>();
        foreach (var line in lines)
        {
            var text = ResolveLine(line, features);
            if (text is not null)
                selected.Add(text);
        }

        return selected;
    }

    private static string? ResolveLine(RootLine line, IReadOnlySet<string> features)
    {
        if (line.When is { Count: > 0 })
        {
            foreach (var variant in line.When)
            {
                if (variant.Features.All(feature => features.Contains(feature)))
                    return variant.Text;
            }

            return null;
        }

        return line.Features.All(feature => features.Contains(feature)) ? line.Text : null;
    }

    // 根模板行级能力条件：行尾 <!-- if-feature: id --> 在能力关闭时移除整行，启用时仅剥掉标记；
    // 全部启用时输出与 1.39.0 基线一致。行尾只吃本行水平空白与单个换行，不得跨越后续空行
    private static string ApplyFeatureMarkers(string content, Manifest manifest, IReadOnlySet<string> features)
    {
        var pattern = new Regex(
            @"^(?<text>.*?)[ \t]*<!--\s*if-feature:\s*(?<feature>[\w-]+)\s*-->[ \t]*(?:\r?\n)?",
            RegexOptions.Multiline);
        return pattern.Replace(content, match =>
        {
            var feature = match.Groups["feature"].Value;
            if (!manifest.Features.Any(item => item.Id.Equals(feature, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"根模板 if-feature 引用了未注册的能力：{feature}");
            }

            return features.Contains(feature) ? match.Groups["text"].Value + "\n" : "";
        });
    }

    private static List<string> FindSlots(string content)
    {
        return Regex.Matches(content, @"\{\{[^}\r\n]+\}\}")
            .Select(match => match.Value)
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static string NormalizeNewlines(string content, string lineEnding)
    {
        var normalized = content.Replace("\r\n", "\n").Replace('\r', '\n');
        return lineEnding.Equals("CRLF", StringComparison.OrdinalIgnoreCase)
            ? normalized.Replace("\n", "\r\n")
            : normalized;
    }

    internal static string AlignVersion(string content, Manifest manifest)
    {
        content = ReplaceFirstInFrontmatter(content, "version", manifest.Vars.Version);
        return ReplaceFirstInFrontmatter(content, "last_updated", manifest.Vars.LastUpdated);
    }

    private static string ReplaceFirstInFrontmatter(string content, string key, string value)
    {
        if (!content.StartsWith("---", StringComparison.Ordinal))
            return content;

        var end = content.IndexOf("\n---", 3, StringComparison.Ordinal);
        if (end < 0)
            return content;

        var header = content[..end];
        // 行尾允许可选 \r：CRLF 片段的 $ 只匹配 \n 之前，不带 \r? 会整行匹配失败导致版本盖写被跳过
        var pattern = new Regex($"^{Regex.Escape(key)}:[^\\r\\n]*\\r?$", RegexOptions.Multiline);
        var replaced = pattern.Replace(header, $"{key}: {value}", 1);
        return replaced + content[end..];
    }
}
