namespace RuleAssembler;

// 组件驱动的 Selection 问答引擎（P2 阶段 3）：候选不是 Profile 列表，而是 manifest.components。
// 存续组件 = when 与当前已回答维度全部命中（未回答维度不参与判断）；
// 只提问「存续组件之间仍有分歧」的维度，唯一取值自动推导（记录来源 derived），不问不猜。
// 问题顺序与选项文案沿用 manifest.wizard.steps / options，前端先于后端的口径不变。
internal sealed record SelectionQuestion(
    string Trait,
    string Question,
    string Hint,
    List<WizardOptionView> Options);

internal sealed class SelectionEngine
{
    private readonly Manifest _manifest;

    public SelectionEngine(Manifest manifest)
    {
        _manifest = manifest;
    }

    // 存续组件：when 中已回答维度必须相等，未回答维度忽略
    public List<ComponentDefinition> Live(IReadOnlyDictionary<string, string> dimensions)
    {
        return _manifest.Components
            .Where(component => component.When.All(pair =>
                !dimensions.TryGetValue(pair.Key, out var value) || value == pair.Value))
            .ToList();
    }

    // 尚需确认的维度：存续组件 when 引用了、但选择未回答的维度，按 wizard.steps 顺序排列
    public List<string> MissingDimensions(IReadOnlyDictionary<string, string> dimensions)
    {
        var live = Live(dimensions);
        var known = _manifest.Wizard.Steps.Select(step => step.Trait).ToHashSet(StringComparer.Ordinal);
        var missing = new List<string>();
        foreach (var step in _manifest.Wizard.Steps)
        {
            if (!selectionNeeds(step.Trait))
                continue;
            missing.Add(step.Trait);
        }

        // steps 之外的维度（当前不存在）排在末尾，保证组合器永远拿得到完整维度
        foreach (var component in live)
        {
            foreach (var pair in component.When)
            {
                if (!known.Contains(pair.Key) && !missing.Contains(pair.Key)
                    && !dimensions.ContainsKey(pair.Key))
                    missing.Add(pair.Key);
            }
        }

        return missing;

        bool selectionNeeds(string trait)
        {
            return !dimensions.ContainsKey(trait) && live.Any(component => component.When.ContainsKey(trait));
        }
    }

    // 下一问：先自动推导存续组件取值唯一的维度（写入 selection 并记录到 derivedLog），
    // 再返回第一个仍有分歧维度的问题；返回 null 表示选择已完备，可交 CompositionPlanner 解析。
    // 选项经可行性剪枝：取值后至少仍存在一个兼容样例，保证交互路径不会走进死组合
    // （与旧引擎「选项从 Profile 推导所以永不变死」的保证对齐）
    public SelectionQuestion? NextQuestion(Selection selection, List<(string Trait, string Value)>? derivedLog = null)
    {
        while (true)
        {
            var missing = MissingDimensions(selection.Dimensions);
            if (missing.Count == 0)
                return null;

            foreach (var trait in missing)
            {
                var values = Live(selection.Dimensions)
                    .Where(component => component.When.TryGetValue(trait, out _))
                    .Select(component => component.When[trait])
                    .Where(value => IsViable(selection, trait, value))
                    .Distinct(StringComparer.Ordinal)
                    .ToList();
                if (values.Count == 0)
                {
                    // 可行状态下该维度必有兼容取值；走到这里说明种子选择已与全部样例冲突
                    throw new InvalidOperationException(
                        $"维度 {trait} 没有可行取值，当前选择与全部兼容样例冲突");
                }
                if (values.Count == 1)
                {
                    selection.Dimensions[trait] = values[0];
                    derivedLog?.Add((trait, values[0]));
                    continue;
                }

                return BuildQuestion(selection, trait, values);
            }
        }
    }

    // 可行性判断：先按钳制约束拒绝（壳/形态/前端的硬关系不依赖样例清单），
    // 再把候选取值并入选择，要求仍至少有一个兼容样例在共有维度上不冲突。
    // 兼容样例是当前全部可生成组合的清单，样例剪枝等价于旧引擎按 Profile 候选过滤选项
    private bool IsViable(Selection selection, string trait, string value)
    {
        var candidate = new Dictionary<string, string>(selection.Dimensions, StringComparer.Ordinal)
        {
            [trait] = value
        };
        var candidateSelection = new Selection { Dimensions = candidate };
        if (!CompositionPlanner.SatisfiesConstraints(_manifest, candidateSelection))
            return false;
        return MatchCompatibilityCases(_manifest, candidateSelection).Count > 0;
    }

    private SelectionQuestion BuildQuestion(Selection selection, string trait, List<string> values)
    {
        var step = _manifest.Wizard.Steps.FirstOrDefault(item => item.Trait == trait);
        var live = Live(selection.Dimensions);
        var options = values
            .Select(value => (Value: value, Count: live.Count(component =>
                component.When.TryGetValue(trait, out var v) && v == value)))
            .OrderByDescending(pair => pair.Count)
            .ThenBy(pair => OptionLabel(trait, pair.Value), StringComparer.Ordinal)
            .Select(pair => new WizardOptionView(trait, pair.Value, OptionLabel(trait, pair.Value), OptionDesc(trait, pair.Value)))
            .ToList();
        return new SelectionQuestion(trait, step?.Question ?? trait, step?.Hint ?? "", options);
    }

    private string OptionLabel(string trait, string value)
    {
        return _manifest.Wizard.Options.TryGetValue(trait, out var options)
            && options.TryGetValue(value, out var option)
            ? option.Label
            : value;
    }

    private string OptionDesc(string trait, string value)
    {
        return _manifest.Wizard.Options.TryGetValue(trait, out var options)
            && options.TryGetValue(value, out var option)
                ? option.Desc
                : "";
    }

    // 组件决策树全路径仿真：每条交互可达路径（用户只会在存续组件提供的取值中选择）都必须
    // 收敛到可解析的组合，且恰好映射一个兼容样例；全部样例必须可达。
    // 用于 --check 与单元测试，防止组件/约束调整后出现死组合、歧义或不可达配方。
    // 空存续集的分支在交互上不可达（用户拿不到不存在的选项），直接剪枝。
    public List<string> SimulateAll(CompositionPlanner planner)
    {
        var errors = new List<string>();
        var reachableCases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Walk(Selection selection)
        {
            var live = Live(selection.Dimensions);
            if (live.Count == 0)
                return;

            var derived = new List<(string Trait, string Value)>();
            var question = NextQuestion(selection, derived);
            if (question is null)
            {
                // 叶子：组合必须可解析，且恰好映射一个兼容样例
                try
                {
                    var plan = planner.Plan(selection);
                    var matches = MatchCompatibilityCases(_manifest, selection);
                    if (matches.Count == 1)
                        reachableCases.Add(matches[0]);
                    else
                        errors.Add($"组合 {Describe(selection)} 映射到 {matches.Count} 个兼容样例（{string.Join("、", matches)}）");
                    if (plan.Warnings.Count > 0)
                        errors.Add($"组合 {Describe(selection)} 存在未消费槽位告警：{string.Join("；", plan.Warnings)}");
                }
                catch (InvalidOperationException ex)
                {
                    errors.Add($"组合 {Describe(selection)} 无法解析：{ex.Message}");
                }

                return;
            }

            foreach (var option in question.Options)
            {
                var branch = new Selection
                {
                    Dimensions = new Dictionary<string, string>(selection.Dimensions, StringComparer.Ordinal)
                };
                branch.Dimensions[option.Trait] = option.Value;
                Walk(branch);
            }
        }

        Walk(new Selection());

        var unreachable = _manifest.CompatibilityCases
            .Where(item => !reachableCases.Contains(item.Profile))
            .Select(item => item.Profile)
            .ToList();
        if (unreachable.Count > 0)
            errors.Add($"以下兼容样例无法通过组件问答到达：{string.Join("、", unreachable)}");

        return errors;
    }

    // 兼容样例映射：样例维度与选择在共有的维度上全部相等即视为候选，唯一候选时返回。
    // 选择可能不含样例的全部维度（组合器不需要的维度不会提问），因此不做维度全集比较
    public static List<string> MatchCompatibilityCases(Manifest manifest, Selection selection)
    {
        return manifest.CompatibilityCases
            .Where(item => item.Selection.All(pair =>
                !selection.Dimensions.TryGetValue(pair.Key, out var value) || value == pair.Value))
            .Select(item => item.Profile)
            .ToList();
    }

    private static string Describe(Selection selection)
    {
        return "(" + string.Join(" 且 ", selection.Dimensions.Select(pair => $"{pair.Key}={pair.Value}")) + ")";
    }
}
