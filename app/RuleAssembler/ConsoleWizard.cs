namespace RuleAssembler;

// 一次选择的结果：Profile + 入口模式（classic/wizard/detected）+ 决策树回答（经典入口为空）
internal sealed record ProfileSelection(
    Profile Profile,
    string Mode,
    Dictionary<string, string> Answers);

// 选择交互逻辑：渲染与输入由 IConsoleUi 承担（Spectre 或纯文本），本类只维护候选过滤与回退栈
internal sealed class ConsoleWizard
{
    private readonly IConsoleUi _ui;

    public ConsoleWizard(IConsoleUi ui)
    {
        _ui = ui;
    }

    // 经典入口：大类 → Profile，保留为问答向导的兜底路径
    public ProfileSelection? SelectProfile(Manifest manifest)
    {
        while (true)
        {
            var familyChoice = _ui.Choose(
                "请选择技术大类：",
                "取消",
                manifest.Families
                    .Select(family => new UiChoice(family.Label, family.Desc))
                    .ToList());
            if (familyChoice is null)
                return null;

            var family = manifest.Families[familyChoice.Value];
            var profiles = manifest.Profiles
                .Where(profile => profile.Family.Equals(family.Id, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (profiles.Count == 0)
            {
                _ui.Line("该大类下暂无已接入 Profile，请重新选择。");
                continue;
            }

            while (true)
            {
                var profileChoice = _ui.Choose(
                    $"请选择 {family.Label} Profile：",
                    "返回上一级",
                    profiles.Select(profile => new UiChoice(profile.Label, profile.Desc)).ToList());
                if (profileChoice is null)
                    break;

                return new ProfileSelection(
                    profiles[profileChoice.Value],
                    "classic",
                    new Dictionary<string, string>(StringComparer.Ordinal));
            }
        }
    }

    // 受限决策树问答：每一步的选项都从当前候选 Profile 推导，不会选出无法生成的组合；
    // 选择 0 返回上一步，第一问选择 0 则交还主菜单
    public ProfileSelection? SelectByWizard(Manifest manifest)
    {
        var engine = new WizardEngine(manifest);
        var candidates = manifest.Profiles.ToList();
        var answers = new Dictionary<string, string>(StringComparer.Ordinal);
        var history = new List<(List<Profile> Candidates, Dictionary<string, string> Answers)>();

        while (true)
        {
            var question = engine.NextQuestion(candidates);
            if (question is null)
                break;

            var choice = _ui.Choose(
                question.Question,
                "返回上一步",
                question.Options
                    .Select(option => new UiChoice(option.Label, option.Desc))
                    .ToList());
            if (choice is null)
            {
                if (history.Count == 0)
                    return null;
                (candidates, answers) = history[^1];
                history.RemoveAt(history.Count - 1);
                continue;
            }

            history.Add((candidates, new Dictionary<string, string>(answers, StringComparer.Ordinal)));
            var selected = question.Options[choice.Value];
            answers[selected.Trait] = selected.Value;
            candidates = engine.Filter(candidates, selected.Trait, selected.Value);
        }

        var profile = candidates.Single();
        return new ProfileSelection(profile, "wizard", answers);
    }
}
