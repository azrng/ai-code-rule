namespace RuleAssembler;

// --detect 与交互菜单「检测当前目录组合（仅查看）」共用的只读检测：
// 识别技术栈并展示推荐或待确认组合，不写入文件；两条入口共用同一输出逻辑避免口径漂移
internal static class DetectCommand
{
    internal static int Run(Manifest manifest, IConsoleUi ui, string directory)
    {
        var result = new ProjectDetector(manifest).Detect(directory);
        if (result is null)
        {
            ui.Line($"未能在 {directory} 识别出已接入的技术栈。");
            return 1;
        }

        ui.Head("已检测当前目录，判断依据：");
        ui.Bullets(result.Reasons);
        ui.Bullets(result.Notes);
        ui.Line("");

        if (result.Converged)
        {
            ui.Line($"推荐组合：{result.SampleLabel}（{result.SampleProfileId}）");
            ui.Line("回答组合：" + string.Join(
                "，",
                result.BuildSampleAnswers().Select(pair => $"{pair.Key}={pair.Value}")));
            return 0;
        }

        ui.Line("检测未收敛，以下维度已确认：" + string.Join(
            "，",
            result.Confirmed.Select(pair => $"{pair.Key}={pair.Value}")));
        foreach (var trait in result.Unconfirmed)
            ui.Line($"待确认维度：{trait}");
        ui.Line("候选组合：");
        foreach (var candidate in result.Candidates)
            ui.Line($"  - {candidate.Label}（{candidate.SampleProfileId}）");
        return 0;
    }
}
