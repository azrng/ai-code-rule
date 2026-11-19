namespace RuleAssembler;

// 经典选择交互：技术大类 → 兼容样例的浏览式兜底入口。
// 选中后的渲染与配方走组合内核（按样例的维度选择渲染），
// 本类只负责两级选择 UI；样例携带 label / desc / family 展示元数据
internal sealed class ConsoleWizard
{
    private readonly IConsoleUi _ui;

    public ConsoleWizard(IConsoleUi ui)
    {
        _ui = ui;
    }

    // 经典入口：大类 → 样例；取消返回 null
    public CompatibilityCase? SelectCase(Manifest manifest)
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
            var cases = manifest.CompatibilityCases
                .Where(item => item.Family.Equals(family.Id, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (cases.Count == 0)
            {
                _ui.Line("该大类下暂无已接入组合，请重新选择。");
                continue;
            }

            while (true)
            {
                var caseChoice = _ui.Choose(
                    $"请选择 {family.Label} 组合：",
                    "返回上一级",
                    cases.Select(item => new UiChoice(item.Label, item.Desc)).ToList());
                if (caseChoice is null)
                    break;

                return cases[caseChoice.Value];
            }
        }
    }
}
