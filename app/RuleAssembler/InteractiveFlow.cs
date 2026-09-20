namespace RuleAssembler;

// 交互入口流：入口菜单（问答 / 检测推荐 / 经典大类）、组件问答主循环、检测补答、
// 能力开关勾选与展示标签。全部入口只产出 Selection（EntryOutcome），渲染交给组合内核
internal static class InteractiveFlow
{
    // 入口结果：全部入口（问答 / 检测推荐 / 经典大类）都产出 Selection 走组合内核
    internal sealed record EntryOutcome(
        Selection Selection,
        string Mode,
        Dictionary<string, string> Answers,
        Dictionary<string, string> AnswerSources);

    // 向导/入口流结果：BackToEntry 表示用户在第一问按「0 返回上一步」交还入口菜单，
    // 由 OfferEntry 的入口循环重新展示检测与入口选项；Outcome=null 且 BackToEntry=false
    // 才是真正的「取消」，由入口菜单的 0 项触发并结束生成器
    internal sealed record EntryFlowResult(EntryOutcome? Outcome, bool BackToEntry)
    {
        public static EntryFlowResult Done(EntryOutcome outcome) => new(outcome, false);

        public static EntryFlowResult Cancel() => new(null, false);

        public static EntryFlowResult Back() => new(null, true);
    }

    // 经典大类选择的组合口径转换：选中样例的维度选择直接进入组合内核，
    // 问答来源整体标记为 classic，配方与渲染走组合路径
    internal static EntryOutcome ClassicOutcome(CompatibilityCase caseItem)
    {
        var answers = new Dictionary<string, string>(caseItem.Selection, StringComparer.Ordinal);
        var sources = answers.Keys
            .ToDictionary(key => key, _ => "classic", StringComparer.Ordinal);
        return SelectionOutcome(
            new Selection { Dimensions = answers }, "classic", answers, sources);
    }

    private static EntryOutcome SelectionOutcome(
        Selection selection, string mode, Dictionary<string, string> answers, Dictionary<string, string> sources)
    {
        return new EntryOutcome(selection, mode, answers, sources);
    }

    internal static EntryOutcome? OfferEntry(Manifest manifest, ConsoleWizard wizard, IConsoleUi ui)
    {
        // 入口循环：向导第一问按「0 返回上一步」交还到这里，重跑检测、重新展示入口菜单；
        // 各入口菜单自身的 0 项保持「取消」语义，是结束生成器的唯一出口
        while (true)
        {
            var detection = TryDetect(manifest, Environment.CurrentDirectory);
            if (detection is null)
            {
                var choice = ui.Choose(
                    "请选择确定规则组合的方式：",
                    "取消",
                    new List<UiChoice>
                    {
                        new("问答选择（推荐）", "回答项目形态、前后端技术栈等几个问题"),
                        new("经典选择（兼容）", "技术大类 → 组合")
                    });
                if (choice is null)
                    return null;
                if (choice == 0)
                {
                    var wizardResult = RunSelectionWizard(manifest, ui, "wizard");
                    if (wizardResult.BackToEntry)
                    {
                        ui.Line("已返回入口选择。");
                        continue;
                    }
                    return wizardResult.Outcome;
                }
                return wizard.SelectCase(manifest) is { } casePick ? ClassicOutcome(casePick) : null;
            }

            ui.Head("已检测当前目录，判断依据：");
            ui.Bullets(detection.Reasons.Take(4));
            ui.Bullets(detection.Notes.Take(2));
            ui.Line("");

            // 部分检测结果：证据不足以锁定唯一组合，不伪造推荐，进入补答流程
            if (!detection.Converged)
            {
                var supplement = OfferSupplement(manifest, wizard, ui, detection);
                if (supplement.BackToEntry)
                {
                    ui.Line("已返回入口选择。");
                    continue;
                }
                return supplement.Outcome;
            }

            var sampleLabel = detection.SampleLabel;
            ui.Line($"推荐组合：{sampleLabel}");

            var confirm = ui.Choose(
                "请确认：",
                "取消",
                new List<UiChoice>
                {
                    new("使用推荐组合", "随后可选择能力开关并确认生成"),
                    new("问答选择（重新回答）", ""),
                    new("经典选择（兼容）", "技术大类 → 组合")
                });
            if (confirm is null)
                return null;
            if (confirm == 0)
            {
                // 收敛的检测证据直接映射为 Selection：取推荐样例的全部维度取值
                var answers = detection.BuildSampleAnswers();
                var selection = new Selection { Dimensions = new Dictionary<string, string>(answers, StringComparer.Ordinal) };
                return SelectionOutcome(
                    selection, "detected", answers,
                    answers.Keys.ToDictionary(key => key, _ => "detected", StringComparer.Ordinal));
            }
            if (confirm == 1)
            {
                var wizardResult = RunSelectionWizard(manifest, ui, "wizard");
                if (wizardResult.BackToEntry)
                {
                    ui.Line("已返回入口选择。");
                    continue;
                }
                return wizardResult.Outcome;
            }
            return wizard.SelectCase(manifest) is { } classicPick ? ClassicOutcome(classicPick) : null;
        }
    }

    // Q5 能力开关（组合路径）：可用性由 CompositionPlan 的文件贡献与 design-system 来源推导，
    // 关闭能力不会生成对应文件，根导航同步收敛
    internal static HashSet<string> ChooseFeaturesForPlan(Manifest manifest, IConsoleUi ui, CompositionPlan plan)
    {
        var defaults = FeatureSelection.Defaults(manifest, plan);
        var aiFeature = manifest.Features
            .FirstOrDefault(feature => feature.Id.Equals("ai-capability", StringComparison.OrdinalIgnoreCase));
        if (aiFeature is not null && defaults.Contains(aiFeature.Id))
            ui.Line($"AI 能力：已启用（{aiFeature.Desc}）；是否启用由问答回答决定，此处不调整");

        var toggleable = FeatureSelection.Available(manifest, plan)
            .Where(feature => !feature.Id.Equals("ai-capability", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var final = toggleable.Count == 0
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : ui.ChooseMany(
                    "选择本次生成的能力：",
                    toggleable.Select(feature => new UiChoice(feature.Label, feature.Desc)).ToList(),
                    toggleable
                        .Select((feature, index) => (feature, index))
                        .Where(pair => defaults.Contains(pair.feature.Id))
                        .Select(pair => pair.index)
                        .ToList())
                .Select(index => toggleable[index].Id)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // 用勾选结果覆盖可开关能力的默认值，AI 能力保留问答回答的状态，再按可用性归一化
        foreach (var id in defaults)
        {
            if (id.Equals("ai-capability", StringComparison.OrdinalIgnoreCase))
                final.Add(id);
        }

        return FeatureSelection.Normalize(manifest, plan, final);
    }

    // 组件问答主循环：检测补答与全新问答共用；已确认（detected）维度保留且不再提问，
    // 唯一取值自动推导（来源 derived）；0 在非第一问回退一步，第一问交还入口菜单
    // （不再直接取消生成器），入口菜单的「取消」才是唯一退出出口
    internal static EntryFlowResult RunSelectionWizard(
        Manifest manifest, IConsoleUi ui, string mode, Dictionary<string, string>? seeds = null)
    {
        var engine = new SelectionEngine(manifest);
        var seedDims = seeds ?? new Dictionary<string, string>();
        var selection = new Selection
        {
            Dimensions = new Dictionary<string, string>(seedDims, StringComparer.Ordinal)
        };
        var answers = new Dictionary<string, string>(seedDims, StringComparer.Ordinal);
        var sources = seedDims.Keys
            .ToDictionary(key => key, _ => "detected", StringComparer.Ordinal);
        var history = new List<(Dictionary<string, string> Dims, Dictionary<string, string> Answers, Dictionary<string, string> Sources)>();

        while (true)
        {
            var derived = new List<(string Trait, string Value)>();
            var question = engine.NextQuestion(selection, derived);
            foreach (var pair in derived)
            {
                answers[pair.Trait] = pair.Value;
                sources[pair.Trait] = "derived";
                ui.Line($"已自动确定 {DescribeTraitValue(manifest, pair.Trait, pair.Value)}（当前组合下唯一取值）");
            }

            if (question is null)
            {
                return EntryFlowResult.Done(SelectionOutcome(selection, mode, answers, sources));
            }

            var dimsBefore = new Dictionary<string, string>(selection.Dimensions, StringComparer.Ordinal);
            var answersBefore = new Dictionary<string, string>(answers, StringComparer.Ordinal);
            var sourcesBefore = new Dictionary<string, string>(sources, StringComparer.Ordinal);
            var choice = ui.Choose(
                question.Question,
                "返回上一步",
                question.Options.Select(option => new UiChoice(option.Label, option.Desc)).ToList());
            if (choice is null)
            {
                // 第一问没有更上一步：交还入口菜单重新选择，而不是直接取消整个生成器
                if (history.Count == 0)
                    return EntryFlowResult.Back();
                (var dims, answers, sources) = history[^1];
                history.RemoveAt(history.Count - 1);
                selection.Dimensions = dims;
                continue;
            }

            history.Add((dimsBefore, answersBefore, sourcesBefore));
            var selected = question.Options[choice.Value];
            selection.Dimensions[selected.Trait] = selected.Value;
            answers[selected.Trait] = selected.Value;
            sources[selected.Trait] = "manual";
        }
    }

    // 检测部分结果的补答入口（组合路径）：展示已确认维度与候选组合，
    // 已确认维度作为种子带入组件问答，只回答仍有分歧的维度；
    // 补答向导第一问的「返回上一步」透传为 BackToEntry，交还入口循环
    private static EntryFlowResult OfferSupplement(Manifest manifest, ConsoleWizard wizard, IConsoleUi ui, DetectionResult detection)
    {
        var questionByTrait = manifest.Wizard.Steps
            .GroupBy(step => step.Trait, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().Question, StringComparer.Ordinal);

        ui.Line("已确认维度：" + string.Join(
            "，",
            detection.Confirmed.Select(pair => DescribeTraitValue(manifest, pair.Key, pair.Value))));
        if (detection.Unconfirmed.Count > 0)
        {
            ui.Bullets(detection.Unconfirmed.Select(trait =>
                $"{trait} 证据不足，待确认（{questionByTrait.GetValueOrDefault(trait, trait)}）"));
        }

        ui.Line("候选组合：");
        ui.Bullets(detection.Candidates.Select(candidate =>
            candidate.SampleProfileId is null
                ? candidate.Label
                : $"{candidate.Label}（{candidate.SampleProfileId}）"));
        ui.Line("");

        var choice = ui.Choose(
            "请选择：",
            "取消",
            new List<UiChoice>
            {
                new("补答确认（保留已确认结果，只回答缺失项）", ""),
                new("问答选择（重新回答）", ""),
                new("经典选择（兼容）", "技术大类 → 组合")
            });
        if (choice is null)
            return EntryFlowResult.Cancel();

        if (choice == 0)
            return RunSelectionWizard(manifest, ui, "detected", new Dictionary<string, string>(detection.Confirmed, StringComparer.Ordinal));
        if (choice == 1)
            return RunSelectionWizard(manifest, ui, "wizard");
        var classic = wizard.SelectCase(manifest);
        return classic is null ? EntryFlowResult.Cancel() : EntryFlowResult.Done(ClassicOutcome(classic));
    }

    // 组合路径的展示标签：唯一映射兼容样例时用样例名称，否则列组件
    internal static string DescribeCompositionLabel(Manifest manifest, CompositionPlan plan)
    {
        var matches = SelectionEngine.MatchCompatibilityCases(manifest, plan.Selection);
        var caseItem = matches.Count == 1
            ? manifest.CompatibilityCases.FirstOrDefault(item =>
                item.Profile.Equals(matches[0], StringComparison.OrdinalIgnoreCase))
            : null;
        return caseItem is not null ? caseItem.Label : $"组件组合（{string.Join("、", plan.ComponentIds)}）";
    }

    private static string DescribeTraitValue(Manifest manifest, string trait, string value)
    {
        var label = manifest.Wizard.Options.TryGetValue(trait, out var options)
            && options.TryGetValue(value, out var option)
            ? option.Label
            : value;
        return $"{label}（{trait}={value}）";
    }

    private static DetectionResult? TryDetect(Manifest manifest, string directory)
    {
        try
        {
            return new ProjectDetector(manifest).Detect(directory);
        }
        catch (Exception)
        {
            // 检测失败不阻塞交互，直接落到问答选择
            return null;
        }
    }
}
