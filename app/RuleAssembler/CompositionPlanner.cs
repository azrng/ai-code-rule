using System.Text.RegularExpressions;

namespace RuleAssembler;

// P2 组合内核：把 Selection 解析为组件集合，合并根变量并产出 CompositionPlan。
// 解析是确定性的：组件按 kind 固定顺序（form -> shell -> frontend -> backend -> capability）、
// 同 kind 按 manifest 声明顺序；同一 Selection 与同一版本 manifest 必须得到相同结果。
// 证据不足时不隐式补组件：组件引用了选择中缺失的维度即视为部分匹配并报错，与检测器口径一致。
internal sealed class CompositionPlanner
{
    private static readonly string[] KindOrder =
    {
        ComponentDefinition.KindForm,
        ComponentDefinition.KindShell,
        ComponentDefinition.KindFrontend,
        ComponentDefinition.KindBackend,
        ComponentDefinition.KindCapability
    };

    private static readonly Regex SlotPattern = new(@"\{\{slot:([\w-]+)\}\}", RegexOptions.Compiled);

    private readonly Manifest _manifest;

    public CompositionPlanner(Manifest manifest)
    {
        _manifest = manifest;
    }

    public CompositionPlan Plan(Selection selection)
    {
        ValidateDimensions(selection);
        ValidateConstraints(selection);

        var (selected, missing) = ResolveComponents(selection);
        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                "组合证据不足，以下维度未确认，不得隐式补全："
                + string.Join("、", missing.Select(pair => $"{pair.Key}（组件 {pair.Value}）")));
        }

        // 结构完备性守卫：组件集非空，且已回答的非标记维度取值都被至少一个选中组件的 when 锚定——
        // 未锚定的正向取值意味着没有任何组件表达该选择（如 Electron 壳配 general 前端），属于尚未接入的组合
        if (selected.Count == 0)
        {
            throw new InvalidOperationException(
                $"组合 {DescribeSelection(selection)} 没有命中任何组件，该维度组合尚未接入");
        }

        var unpinned = UnpinnedDimensions(_manifest, selection.Dimensions, selected);
        if (unpinned.Count > 0)
        {
            throw new InvalidOperationException(
                $"组合 {DescribeSelection(selection)} 的以下维度取值未被任何选中组件表达：{string.Join("、", unpinned)}");
        }

        var ordered = selected
            .OrderBy(component => Array.IndexOf(KindOrder, component.Kind))
            .ThenBy(component => _manifest.Components.FindIndex(item => item.Id == component.Id))
            .ToList();

        var root = BuildRoot(selection, ordered, out var warnings, out var slots);
        var fileContributions = CollectFiles(ordered);
        var designSystemSource = ResolveDesignSystemSource(ordered);
        var moduleIds = ResolveModuleIds(ordered);

        return new CompositionPlan
        {
            Selection = selection,
            ModuleIds = moduleIds,
            ComponentIds = ordered.Select(component => component.Id).ToList(),
            Root = root,
            Files = fileContributions.Select(contribution => contribution.File).ToList(),
            FileContributions = fileContributions,
            DesignSystemSource = designSystemSource,
            Slots = slots,
            Warnings = warnings
        };
    }

    // 维度键必须由 wizard.steps 声明（维度空间的唯一事实来源），取值必须有 wizard.options 文案，
    // 防止组合结果游离于问答与检测之外
    private void ValidateDimensions(Selection selection)
    {
        var errors = new List<string>();
        foreach (var pair in selection.Dimensions)
        {
            if (!_manifest.Wizard.Steps.Any(step => step.Trait == pair.Key))
            {
                errors.Add($"未知维度：{pair.Key}");
                continue;
            }

            if (string.IsNullOrWhiteSpace(pair.Value))
            {
                errors.Add($"维度 {pair.Key} 的取值不能为空");
                continue;
            }

            if (_manifest.Wizard.Options.TryGetValue(pair.Key, out var options)
                && !options.ContainsKey(pair.Value))
            {
                errors.Add($"维度 {pair.Key} 的取值 {pair.Value} 缺少 wizard.options 文案");
            }
        }

        if (errors.Count > 0)
            throw new InvalidOperationException("组合选择非法：\n- " + string.Join("\n- ", errors));
    }

    // 约束校验：when 全部命中时 require / forbid 只对已回答维度生效（与 SatisfiesConstraints 的
    // 问答期语义一致）；未回答维度的钳制缺口由组件解析兜底——组合要解析成功必须存在
    // when 全命中的组件集，组件 when 自身携带维度取值（如 tauri-backend-rust 要求 backend=rust），
    // 不存在「require 未满足但组合仍解析成功」的漏洞
    private void ValidateConstraints(Selection selection)
    {
        foreach (var constraint in _manifest.Constraints)
        {
            if (!Matches(constraint.When, selection))
                continue;

            foreach (var pair in constraint.Require)
            {
                if (selection.Dimensions.TryGetValue(pair.Key, out var value) && value != pair.Value)
                {
                    throw new InvalidOperationException(
                        $"组合违反约束：当 {DescribePairs(constraint.When)} 时要求 {pair.Key}={pair.Value}，"
                        + $"当前 {DescribeSelection(selection)}");
                }
            }

            foreach (var pair in constraint.Forbid)
            {
                if (selection.Dimensions.TryGetValue(pair.Key, out var value) && value == pair.Value)
                {
                    throw new InvalidOperationException(
                        $"组合违反约束：当 {DescribePairs(constraint.When)} 时禁止 {pair.Key}={pair.Value}，"
                        + $"当前 {DescribeSelection(selection)}");
                }
            }
        }
    }

    // 问答期可行性判定：forbid 立即生效；require 只对已回答维度生效（未回答维度由后续问答收敛），
    // 供 SelectionEngine 在结构锚定校验之前先按钳制关系拒绝死组合
    internal static bool SatisfiesConstraints(Manifest manifest, Selection selection)
    {
        foreach (var constraint in manifest.Constraints)
        {
            var hit = constraint.When.All(pair =>
                selection.Dimensions.TryGetValue(pair.Key, out var value) && value == pair.Value);
            if (!hit)
                continue;

            var violated = constraint.Require.Any(pair =>
                    selection.Dimensions.TryGetValue(pair.Key, out var value) && value != pair.Value)
                || constraint.Forbid.Any(pair =>
                    selection.Dimensions.TryGetValue(pair.Key, out var value) && value == pair.Value);
            if (violated)
                return false;
        }

        return true;
    }

    // 维度取值是否为「不适用」标记（wizard step 的 absent，如 frontend/backend 的 none、arch-style 的 single）：
    // 标记取值允许没有组件消费；正向取值必须被锚定才是可生成组合
    internal static bool IsAbsentValue(Manifest manifest, string trait, string value)
    {
        var step = manifest.Wizard.Steps.FirstOrDefault(item => item.Trait == trait);
        return step is not null && step.Absent.Length > 0 && step.Absent == value;
    }

    // 结构锚定校验：返回未被任何组件 when 锚定的非标记维度取值（form=desktop 之类）；
    // 空列表表示组合结构完备。组件集为空时由调用方单独报错
    internal static List<string> UnpinnedDimensions(
        Manifest manifest,
        Dictionary<string, string> dimensions,
        IEnumerable<ComponentDefinition> components)
    {
        return dimensions
            .Where(pair => !IsAbsentValue(manifest, pair.Key, pair.Value))
            .Where(pair => !components.Any(component =>
                component.When.TryGetValue(pair.Key, out var value) && value == pair.Value))
            .Select(pair => $"{pair.Key}={pair.Value}")
            .ToList();
    }

    // 组件解析：全命中 -> 选中；部分命中（有命中且引用了缺失维度）-> 记录待确认维度；
    // 全不命中或条件明确不符 -> 排除
    private (List<ComponentDefinition> Selected, Dictionary<string, string> Missing) ResolveComponents(
        Selection selection)
    {
        var selected = new List<ComponentDefinition>();
        var missing = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var component in _manifest.Components)
        {
            var matched = 0;
            var absent = new List<string>();
            var excluded = false;
            foreach (var pair in component.When)
            {
                if (!selection.Dimensions.TryGetValue(pair.Key, out var value))
                {
                    absent.Add(pair.Key);
                    continue;
                }

                if (value != pair.Value)
                {
                    excluded = true;
                    break;
                }

                matched++;
            }

            if (excluded)
                continue;
            if (matched == 0 && absent.Count == component.When.Count && component.When.Count > 0)
                continue;

            if (absent.Count > 0)
            {
                foreach (var dimension in absent)
                    missing.TryAdd(dimension, component.Id);
                continue;
            }

            selected.Add(component);
        }

        // 同一结构性维度槽位只允许一个组件提供，避免多组件同时贡献同名输出或相互覆盖
        foreach (var kind in new[]
                 {
                     ComponentDefinition.KindForm,
                     ComponentDefinition.KindShell,
                     ComponentDefinition.KindFrontend,
                     ComponentDefinition.KindBackend
                 })
        {
            var sameKind = selected.Where(component => component.Kind == kind).ToList();
            if (sameKind.Count > 1)
            {
                throw new InvalidOperationException(
                    $"组件冲突：{kind} 维度被多个组件同时命中：{string.Join("、", sameKind.Select(item => item.Id))}");
            }
        }

        return (selected, missing);
    }

    // 根变量合并：以骨架组件声明的 family rootDefaults 为起点，按组件顺序叠加；
    // 列表字段拼接，标量字段禁止无提示覆盖，最后统一替换 {{slot:x}} 槽位。
    // 槽位取值同时输出到 plan.Slots，供子规则骨架正文渲染时消费
    private RootVariables BuildRoot(
        Selection selection,
        List<ComponentDefinition> ordered,
        out List<string> warnings,
        out Dictionary<string, string> slots)
    {
        warnings = new List<string>();
        var errors = new List<string>();

        var familyIds = ordered
            .Select(component => component.Family)
            .Where(family => !string.IsNullOrWhiteSpace(family))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (familyIds.Count > 1)
            errors.Add($"多个组件声明了不同的 family：{string.Join("、", familyIds)}");

        var root = familyIds.Count > 0
            ? ResolveFamilyDefaults(familyIds[0], errors)
            : new RootVariables();

        // componentOwned 记录已被某个组件贡献的字段：family rootDefaults 是继承基线，
        // 组件值按「未写则继承、写了则覆盖」处理；组件之间相互覆盖才是冲突
        var componentOwned = new HashSet<string>(StringComparer.Ordinal);
        var slotValues = new Dictionary<string, (string Value, string Provider)>(StringComparer.Ordinal);
        foreach (var component in ordered)
        {
            foreach (var pair in component.Slots)
            {
                if (slotValues.TryGetValue(pair.Key, out var existing))
                {
                    errors.Add($"槽位冲突：{pair.Key} 同时由 {existing.Provider} 与 {component.Id} 提供");
                    continue;
                }

                slotValues[pair.Key] = (pair.Value, component.Id);
            }
        }

        foreach (var component in ordered)
        {
            if (component.Root is null)
                continue;
            // 组件未显式声明的标量字段会携带类默认值，先按 JSON 存在性剥离再合并，避免伪贡献覆盖 family 基线
            MergeRootFields(root, SanitizeComponentRoot(component), component.Id, componentOwned, errors);
        }

        SubstituteSlots(root, slotValues, warnings, errors);

        if (errors.Count > 0)
            throw new InvalidOperationException(
                $"组合 {DescribeSelection(selection)} 的根变量合并失败：\n- " + string.Join("\n- ", errors));

        slots = slotValues.ToDictionary(pair => pair.Key, pair => pair.Value.Value, StringComparer.Ordinal);
        return root;
    }

    private RootVariables ResolveFamilyDefaults(string familyId, List<string> errors)
    {
        var family = _manifest.Families.FirstOrDefault(item =>
            item.Id.Equals(familyId, StringComparison.OrdinalIgnoreCase));
        if (family is null)
        {
            errors.Add($"组件声明的 family 不存在：{familyId}");
            return new RootVariables();
        }

        return (family.RootDefaults ?? new RootVariables()).Clone();
    }

    private static void MergeRootFields(
        RootVariables target, RootVariables source, string componentId,
        ISet<string> componentOwned, ICollection<string> errors)
    {
        ReplaceLines(target.TargetList, source.TargetList, componentId, "target-list", componentOwned, errors);
        ReplaceLines(target.SubruleIndex, source.SubruleIndex, componentId, "subrule-index", componentOwned, errors);
        ReplaceLines(target.DevStage, source.DevStage, componentId, "dev-stage", componentOwned, errors);
        ReplaceLines(target.UnderstandingChecklist, source.UnderstandingChecklist, componentId, "understanding-checklist", componentOwned, errors);
        ReplaceLines(target.ReadingOrder, source.ReadingOrder, componentId, "reading-order", componentOwned, errors);
        ReplaceLines(target.ExtraSections, source.ExtraSections, componentId, "extra-sections", componentOwned, errors);

        MergeScalar(target.SearchNouns, source.SearchNouns, componentId, "search-nouns", componentOwned, errors,
            value => target.SearchNouns = value);
        MergeScalar(target.ContractErrorLine, source.ContractErrorLine, componentId, "contract-error-line", componentOwned, errors,
            value => target.ContractErrorLine = value);
        MergeScalar(target.RootScopeNouns, source.RootScopeNouns, componentId, "root-scope-nouns", componentOwned, errors,
            value => target.RootScopeNouns = value);
        MergeScalar(target.ContextScopeNouns, source.ContextScopeNouns, componentId, "context-scope-nouns", componentOwned, errors,
            value => target.ContextScopeNouns = value);
        MergeScalar(target.HighRiskNouns, source.HighRiskNouns, componentId, "high-risk-nouns", componentOwned, errors,
            value => target.HighRiskNouns = value);
        MergeScalar(target.HighRiskPrefix, source.HighRiskPrefix, componentId, "high-risk-prefix", componentOwned, errors,
            value => target.HighRiskPrefix = value);
        MergeScalar(target.TaskKindNouns, source.TaskKindNouns, componentId, "task-kind-nouns", componentOwned, errors,
            value => target.TaskKindNouns = value);
        MergeScalar(target.EvidenceNouns, source.EvidenceNouns, componentId, "evidence-nouns", componentOwned, errors,
            value => target.EvidenceNouns = value);
        MergeScalar(target.ReuseNoun, source.ReuseNoun, componentId, "reuse-noun", componentOwned, errors,
            value => target.ReuseNoun = value);
        MergeScalar(target.PersistenceSuffix, source.PersistenceSuffix, componentId, "persistence-suffix", componentOwned, errors,
            value => target.PersistenceSuffix = value);
        MergeScalar(target.DestructiveNouns, source.DestructiveNouns, componentId, "destructive-nouns", componentOwned, errors,
            value => target.DestructiveNouns = value);
        MergeScalar(target.ChecklistInfra, source.ChecklistInfra, componentId, "checklist-infra", componentOwned, errors,
            value => target.ChecklistInfra = value);
        MergeScalar(target.ModelBehaviorExtra, source.ModelBehaviorExtra, componentId, "model-behavior-extra", componentOwned, errors,
            value => target.ModelBehaviorExtra = value);
        MergeScalar(target.VerifyNouns, source.VerifyNouns, componentId, "verify-nouns", componentOwned, errors,
            value => target.VerifyNouns = value);
        MergeScalar(target.VerifyQuote, source.VerifyQuote, componentId, "verify-quote", componentOwned, errors,
            value => target.VerifyQuote = value);
        MergeScalar(target.RollbackScope, source.RollbackScope, componentId, "rollback-scope", componentOwned, errors,
            value => target.RollbackScope = value);
        MergeScalar(target.BoundaryNouns, source.BoundaryNouns, componentId, "boundary-nouns", componentOwned, errors,
            value => target.BoundaryNouns = value);
        MergeScalar(target.IntroQuote, source.IntroQuote, componentId, "intro-quote", componentOwned, errors,
            value => target.IntroQuote = value);
        MergeScalar(target.PersistenceLine, source.PersistenceLine, componentId, "persistence-line", componentOwned, errors,
            value => target.PersistenceLine = value);
    }

    // 剥离伪声明：RootVariables 反序列化后未声明的标量字段也是非空默认串，不参与组件贡献。
    // 判断依据是 JSON 真实存在性（ManifestLoader.AnnotateComponentRootPresence 标注）：
    // 类默认值比对会把「刻意声明且恰好等于类默认值」的覆盖（如回退到通用措辞覆盖 family 基线）误剥掉。
    // 无存在性标注时（手工构造的组件）退化为类默认值比对
    private static RootVariables SanitizeComponentRoot(ComponentDefinition component)
    {
        var source = component.Root!;
        var declared = component.DeclaredScalarFields;
        var classDefaults = new RootVariables();
        var result = source.Clone();

        // 未声明（或退化口径下与类默认值相同）的标量置空，等价于「该组件未贡献此字段」
        void Strip(Func<string> get, Action<string> set, string field, string classDefault)
        {
            var fake = declared is null
                ? get() == classDefault
                : !declared.Contains(field);
            if (fake)
                set("");
        }

        Strip(() => result.SearchNouns, value => result.SearchNouns = value, "search-nouns", classDefaults.SearchNouns);
        Strip(() => result.ContractErrorLine, value => result.ContractErrorLine = value, "contract-error-line", classDefaults.ContractErrorLine);
        Strip(() => result.RootScopeNouns, value => result.RootScopeNouns = value, "root-scope-nouns", classDefaults.RootScopeNouns);
        Strip(() => result.ContextScopeNouns, value => result.ContextScopeNouns = value, "context-scope-nouns", classDefaults.ContextScopeNouns);
        Strip(() => result.HighRiskNouns, value => result.HighRiskNouns = value, "high-risk-nouns", classDefaults.HighRiskNouns);
        Strip(() => result.HighRiskPrefix, value => result.HighRiskPrefix = value, "high-risk-prefix", classDefaults.HighRiskPrefix);
        Strip(() => result.TaskKindNouns, value => result.TaskKindNouns = value, "task-kind-nouns", classDefaults.TaskKindNouns);
        Strip(() => result.EvidenceNouns, value => result.EvidenceNouns = value, "evidence-nouns", classDefaults.EvidenceNouns);
        Strip(() => result.ReuseNoun, value => result.ReuseNoun = value, "reuse-noun", classDefaults.ReuseNoun);
        Strip(() => result.PersistenceSuffix, value => result.PersistenceSuffix = value, "persistence-suffix", classDefaults.PersistenceSuffix);
        Strip(() => result.DestructiveNouns, value => result.DestructiveNouns = value, "destructive-nouns", classDefaults.DestructiveNouns);
        Strip(() => result.ChecklistInfra, value => result.ChecklistInfra = value, "checklist-infra", classDefaults.ChecklistInfra);
        Strip(() => result.ModelBehaviorExtra, value => result.ModelBehaviorExtra = value, "model-behavior-extra", classDefaults.ModelBehaviorExtra);
        Strip(() => result.VerifyNouns, value => result.VerifyNouns = value, "verify-nouns", classDefaults.VerifyNouns);
        Strip(() => result.VerifyQuote, value => result.VerifyQuote = value, "verify-quote", classDefaults.VerifyQuote);
        Strip(() => result.RollbackScope, value => result.RollbackScope = value, "rollback-scope", classDefaults.RollbackScope);
        Strip(() => result.BoundaryNouns, value => result.BoundaryNouns = value, "boundary-nouns", classDefaults.BoundaryNouns);
        Strip(() => result.IntroQuote, value => result.IntroQuote = value, "intro-quote", classDefaults.IntroQuote);
        Strip(() => result.PersistenceLine, value => result.PersistenceLine = value, "persistence-line", classDefaults.PersistenceLine);
        return result;
    }

    // 列表字段：family 默认值只是继承基线，首个贡献组件整体覆盖；第二个组件再贡献即冲突
    private static void ReplaceLines(
        List<RootLine> target, List<RootLine> source, string componentId, string field,
        ISet<string> componentOwned, ICollection<string> errors)
    {
        if (source.Count == 0)
            return;
        if (!componentOwned.Add(field))
        {
            errors.Add($"组件 {componentId} 与已有组件同时贡献 {field}，列表字段当前只允许单一贡献者");
            return;
        }

        target.Clear();
        target.AddRange(source.Select(line => line.Clone()));
    }

    // 标量字段：首个贡献组件覆盖 family 基线；组件之间值不同才判冲突，等值静默通过
    private static void MergeScalar(
        string current, string incoming, string componentId, string field,
        ISet<string> componentOwned, ICollection<string> errors, Action<string> assign)
    {
        if (incoming.Length == 0)
            return;
        if (componentOwned.Contains(field))
        {
            if (incoming != current)
                errors.Add($"组件 {componentId} 覆盖了已有标量根变量 {field}（{current} -> {incoming}）");
            return;
        }

        componentOwned.Add(field);
        assign(incoming);
    }

    // 槽位替换：先确认全部槽位有唯一提供者，再替换所有字段文本；替换后残留槽位即报错，
    // 已声明但未被任何字段消费的槽位降级为告警
    private static void SubstituteSlots(
        RootVariables root,
        Dictionary<string, (string Value, string Provider)> slotValues,
        List<string> warnings,
        ICollection<string> errors)
    {
        var consumed = new HashSet<string>(StringComparer.Ordinal);

        string Resolve(string field, string text)
        {
            foreach (var pair in SlotPattern.Matches(text).Select(MatchRef).Distinct(StringComparer.Ordinal))
            {
                if (!slotValues.TryGetValue(pair, out var slot))
                {
                    errors.Add($"槽位 {pair} 没有选中组件提供取值（{field}）");
                    continue;
                }

                consumed.Add(pair);
                text = text.Replace($"{{{{slot:{pair}}}}}", slot.Value, StringComparison.Ordinal);
            }

            return text;
        }

        foreach (var line in AllLines(root))
        {
            line.Text = Resolve("根变量条目", line.Text);
            if (line.When is null)
                continue;
            foreach (var variant in line.When)
                variant.Text = Resolve("根变量条目", variant.Text);
        }

        foreach (var scalar in ScalarFields(root))
        {
            if (scalar.Value.Length > 0)
                scalar.Assign(Resolve(scalar.Field, scalar.Value));
        }

        // 未被根骨架消费的槽位不告警：组件槽位同时供子规则 / design-system 骨架在渲染期消费，
        // 真正的缺口由 ApplySlots 在渲染时以残留占位硬报错兜底
    }

    private static string MatchRef(Match match)
    {
        return match.Groups[1].Value;
    }

    private static IEnumerable<RootLine> AllLines(RootVariables root)
    {
        return root.TargetList
            .Concat(root.SubruleIndex)
            .Concat(root.DevStage)
            .Concat(root.UnderstandingChecklist)
            .Concat(root.ReadingOrder)
            .Concat(root.ExtraSections);
    }

    private static IEnumerable<(string Field, string Value, Action<string> Assign)> ScalarFields(RootVariables root)
    {
        yield return ("search-nouns", root.SearchNouns, value => root.SearchNouns = value);
        yield return ("contract-error-line", root.ContractErrorLine, value => root.ContractErrorLine = value);
        yield return ("root-scope-nouns", root.RootScopeNouns, value => root.RootScopeNouns = value);
        yield return ("context-scope-nouns", root.ContextScopeNouns, value => root.ContextScopeNouns = value);
        yield return ("high-risk-nouns", root.HighRiskNouns, value => root.HighRiskNouns = value);
        yield return ("high-risk-prefix", root.HighRiskPrefix, value => root.HighRiskPrefix = value);
        yield return ("task-kind-nouns", root.TaskKindNouns, value => root.TaskKindNouns = value);
        yield return ("evidence-nouns", root.EvidenceNouns, value => root.EvidenceNouns = value);
        yield return ("reuse-noun", root.ReuseNoun, value => root.ReuseNoun = value);
        yield return ("persistence-suffix", root.PersistenceSuffix, value => root.PersistenceSuffix = value);
        yield return ("destructive-nouns", root.DestructiveNouns, value => root.DestructiveNouns = value);
        yield return ("checklist-infra", root.ChecklistInfra, value => root.ChecklistInfra = value);
        yield return ("model-behavior-extra", root.ModelBehaviorExtra, value => root.ModelBehaviorExtra = value);
        yield return ("verify-nouns", root.VerifyNouns, value => root.VerifyNouns = value);
        yield return ("verify-quote", root.VerifyQuote, value => root.VerifyQuote = value);
        yield return ("rollback-scope", root.RollbackScope, value => root.RollbackScope = value);
        yield return ("boundary-nouns", root.BoundaryNouns, value => root.BoundaryNouns = value);
        yield return ("intro-quote", root.IntroQuote, value => root.IntroQuote = value);
        yield return ("persistence-line", root.PersistenceLine, value => root.PersistenceLine = value);
    }

    // 模块集合：公共 core 始终在首位，其余模块按组件顺序与组件内声明顺序去重加入
    private static List<string> ResolveModuleIds(List<ComponentDefinition> ordered)
    {
        var moduleIds = new List<string> { "core" };
        foreach (var component in ordered)
        {
            foreach (var moduleId in component.ModuleIds)
            {
                if (!moduleIds.Contains(moduleId, StringComparer.OrdinalIgnoreCase))
                    moduleIds.Add(moduleId);
            }
        }

        return moduleIds;
    }

    // 输出文件合并：保留逐项贡献，允许一个 replace 基底后跟多个 append 片段；
    // 同一路径的 replace 数量以及 append 是否有基底由渲染阶段按能力集最终校验
    private static List<FileContribution> CollectFiles(List<ComponentDefinition> ordered)
    {
        var files = new List<FileContribution>();
        var contributionsByOutput = new Dictionary<string, List<FileContribution>>(StringComparer.OrdinalIgnoreCase);
        foreach (var component in ordered)
        {
            foreach (var file in component.Files)
            {
                if (!PathGuard.TryNormalizeRelative(file.Output, out var canonical))
                {
                    throw new InvalidOperationException(
                        $"组件 {component.Id} 存在无效输出路径：{file.Output}");
                }

                var contribution = new FileContribution { OwnerId = component.Id, File = file };
                files.Add(contribution);
                if (!contributionsByOutput.TryGetValue(canonical, out var outputContributions))
                {
                    outputContributions = new List<FileContribution>();
                    contributionsByOutput[canonical] = outputContributions;
                }

                outputContributions.Add(contribution);
            }
        }

        foreach (var pair in contributionsByOutput)
        {
            var replaceCount = pair.Value.Count(item => item.File.Merge == OutputDeclaration.MergeReplace);
            if (replaceCount > 1)
            {
                throw new InvalidOperationException(
                    $"组件冲突：输出文件 {pair.Key} 存在多个 replace 提供者："
                    + string.Join("、", pair.Value
                        .Where(item => item.File.Merge == OutputDeclaration.MergeReplace)
                        .Select(item => item.OwnerId)));
            }

            // append-only 组件可以扩展公共模块提供的 replace 基底；最终是否有基底由渲染器
            // 将模块贡献与组件贡献合并后校验。
        }

        return files;
    }

    private static string ResolveDesignSystemSource(List<ComponentDefinition> ordered)
    {
        var sources = ordered
            .Select(component => component.DesignSystemSource)
            .Where(source => !string.IsNullOrWhiteSpace(source))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (sources.Count > 1)
        {
            throw new InvalidOperationException(
                $"组件冲突：多个组件声明了 design-system 来源：{string.Join("、", sources)}");
        }

        return sources.Count > 0 ? sources[0] : "";
    }

    private static bool Matches(Dictionary<string, string> conditions, Selection selection)
    {
        return conditions.All(pair =>
            selection.Dimensions.TryGetValue(pair.Key, out var value) && value == pair.Value);
    }

    private static string DescribePairs(Dictionary<string, string> pairs)
    {
        return string.Join(" 且 ", pairs.Select(pair => $"{pair.Key}={pair.Value}"));
    }

    private static string DescribeSelection(Selection selection)
    {
        return "(" + DescribePairs(selection.Dimensions) + ")";
    }
}
