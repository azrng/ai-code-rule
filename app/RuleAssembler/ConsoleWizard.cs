namespace RuleAssembler;

// 经典选择交互：技术大类 → Profile 的浏览式兜底入口。
// 选中后的渲染与配方走组合内核（Program 按其兼容样例反查维度选择），
// 本类只负责两级选择 UI
internal sealed class ConsoleWizard
{
    private readonly IConsoleUi _ui;

    public ConsoleWizard(IConsoleUi ui)
    {
        _ui = ui;
    }

    // 经典入口：大类 → Profile；取消返回 null
    public Profile? SelectProfile(Manifest manifest)
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

                return profiles[profileChoice.Value];
            }
        }
    }
}
