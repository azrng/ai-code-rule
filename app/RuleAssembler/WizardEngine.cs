namespace RuleAssembler;

// 受限决策树的纯逻辑部分：候选 Profile 按 trait 逐步过滤。
// 选项永远从当前候选中推导，因此交互路径上不会走进 0 匹配的死胡同；
// 未接入的组合表现为「不出现的选项」而非「报错」。
internal sealed record WizardQuestion(
    string Trait,
    string Question,
    string Hint,
    List<WizardOptionView> Options);

internal sealed record WizardOptionView(
    string Trait,
    string Value,
    string Label,
    string Desc);

internal sealed class WizardEngine
{
    private readonly Manifest _manifest;

    public WizardEngine(Manifest manifest)
    {
        _manifest = manifest;
    }

    public List<Profile> AllProfiles => _manifest.Profiles;

    // 回答是否命中 Profile 的 trait 值；* 为通配，匹配任意回答
    public static bool Matches(Profile profile, string trait, string value)
    {
        var traitValue = profile.Traits?.Get(trait);
        if (traitValue is null)
            return false;
        return traitValue == value || traitValue == ProfileTraits.Wildcard;
    }

    public List<Profile> Filter(List<Profile> candidates, string trait, string value)
    {
        return candidates.Where(profile => Matches(profile, trait, value)).ToList();
    }

    // 按已记录的回答全集过滤（用于检测结果的解析）
    public List<Profile> Resolve(IReadOnlyDictionary<string, string> answers)
    {
        var candidates = _manifest.Profiles;
        foreach (var step in _manifest.Wizard.Steps)
        {
            if (!answers.TryGetValue(step.Trait, out var value))
                continue;
            candidates = Filter(candidates, step.Trait, value);
        }

        return candidates;
    }

    // 下一个需要提问的步骤：先决条件满足、且当前候选在该 trait 上存在 ≥2 个可选项。
    // 选项按候选数量降序排列，让更常见的组合排前面。
    public WizardQuestion? NextQuestion(List<Profile> candidates)
    {
        foreach (var step in _manifest.Wizard.Steps)
        {
            if (!IsApplicable(candidates, step))
                continue;

            var groups = GroupByTrait(candidates, step.Trait);
            if (groups.Count < 2)
                continue;

            var options = groups
                .OrderByDescending(group => group.Count())
                .ThenBy(group => OptionLabel(step.Trait, group.Key), StringComparer.Ordinal)
                .Select(group => new WizardOptionView(
                    step.Trait,
                    group.Key,
                    OptionLabel(step.Trait, group.Key),
                    OptionDesc(step.Trait, group.Key)))
                .ToList();

            return new WizardQuestion(step.Trait, step.Question, step.Hint, options);
        }

        return null;
    }

    // 决策树全路径仿真：每条路径必须收敛到恰好 1 个 Profile，且所有 Profile 均可达。
    // 用于 --check 与单元测试，防止 manifest 调整后出现歧义或不可达的配方。
    public List<string> SimulateAll()
    {
        var errors = new List<string>();
        var reachable = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Walk(List<Profile> candidates)
        {
            var question = NextQuestion(candidates);
            if (question is null)
            {
                if (candidates.Count != 1)
                    errors.Add($"决策树存在未收敛路径，剩余候选：{string.Join("、", candidates.Select(profile => profile.Id))}");
                else
                    reachable.Add(candidates[0].Id);
                return;
            }

            foreach (var option in question.Options)
                Walk(Filter(candidates, option.Trait, option.Value));
        }

        Walk(_manifest.Profiles);

        var unreachable = _manifest.Profiles
            .Where(profile => !reachable.Contains(profile.Id))
            .Select(profile => profile.Id)
            .ToList();
        if (unreachable.Count > 0)
            errors.Add($"以下 Profile 无法通过决策树到达：{string.Join("、", unreachable)}");

        return errors;
    }

    internal static bool IsApplicable(List<Profile> candidates, WizardStep step)
    {
        if (string.IsNullOrWhiteSpace(step.AppliesWhenTrait))
            return true;
        return candidates.Any(profile =>
            profile.Traits?.Get(step.AppliesWhenTrait!) is { } value
            && step.AppliesWhenValues.Contains(value));
    }

    private static List<IGrouping<string, string>> GroupByTrait(List<Profile> candidates, string trait)
    {
        return candidates
            .Select(profile => profile.Traits?.Get(trait))
            .Where(value => value is not null && value != ProfileTraits.Wildcard)
            .Cast<string>()
            .GroupBy(value => value, StringComparer.Ordinal)
            .ToList();
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
}
