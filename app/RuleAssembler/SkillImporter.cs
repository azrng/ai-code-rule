namespace RuleAssembler;

// Skills 导入目标（项目级目录约定，按各工具实际发现的目录）：
// Codex 读取 .agents/skills，ZCode 读取 .zcode/skills，Claude Code 读取 .claude/skills，pi 读取 .pi/skills
internal sealed record SkillTarget(string Id, string Label, string RelativeRoot, string? LegacyRoot = null);

internal static class SkillTargets
{
    public static readonly IReadOnlyList<SkillTarget> All = new[]
    {
        // Codex 只发现 .agents/skills（项目级向上探测与用户级 ~/.agents/skills 同标准）；
        // LegacyRoot 是历史版本误写的 .codex/skills（Codex 不读取），导入同名技能时顺手清理迁移
        new SkillTarget("codex", "Codex", ".agents/skills", ".codex/skills"),
        new SkillTarget("zcode", "ZCode", ".zcode/skills"),
        new SkillTarget("claude", "Claude Code", ".claude/skills"),
        new SkillTarget("pi", "pi", ".pi/skills")
    };

    public static SkillTarget? Find(string id)
    {
        return All.FirstOrDefault(target => target.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }
}

// 技能释放：按 <目标目录>/<RelativeRoot>/<技能ID>/ 整体写入，字节级原样拷贝；导入是追加语义，未选中的技能不动。
// 同名技能（以 SKILL.md 为准）整目录先删后写，消除上游删改文件造成的残留；无 SKILL.md 的目录不算同名技能。
// 技能不单独加入 .gitignore，但 .claude/ 等工具目录已随生成器默认忽略，需共享技能的项目自行取反（如 !.claude/skills/）
internal sealed class SkillImporter
{
    // 冲突检测键集合：「<targetId>/<skillId>」表示该目标下已存在同名技能目录（以 SKILL.md 为准）。
    // 冲突的处置（覆盖 / 跳过）由调用方决定，本类只负责发现与按 skipKeys 落盘
    public HashSet<string> DetectConflicts(
        string rootDirectory, IReadOnlyList<SkillTarget> targets, IReadOnlyList<SkillInfo> skills)
    {
        var conflicts = new HashSet<string>(StringComparer.Ordinal);
        foreach (var target in targets)
        {
            foreach (var skill in skills)
            {
                var marker = PathGuard.GetPathWithin(rootDirectory, $"{target.RelativeRoot}/{skill.Id}/SKILL.md");
                if (File.Exists(marker))
                    conflicts.Add($"{target.Id}/{skill.Id}");
            }
        }

        return conflicts;
    }

    // written 单位是文件数；同一技能会向每个选中目标各写一份，skipKeys 命中的「目标/技能」组合原样保留并打印跳过说明
    public int Import(
        string rootDirectory,
        IReadOnlyList<SkillTarget> targets,
        IReadOnlyList<SkillInfo> skills,
        SkillsStore store,
        IReadOnlySet<string>? skipKeys = null)
    {
        var written = 0;
        foreach (var target in targets)
        {
            foreach (var skill in skills)
            {
                if (skipKeys is not null && skipKeys.Contains($"{target.Id}/{skill.Id}"))
                {
                    Console.WriteLine($"  [{target.Id}] {skill.Id}（已存在同名技能，跳过）");
                    continue;
                }

                // 同名技能（以 SKILL.md 为准）先删后写，配方外的技能与其他内容不受影响
                var skillRoot = PathGuard.GetPathWithin(rootDirectory, $"{target.RelativeRoot}/{skill.Id}");
                if (File.Exists(Path.Combine(skillRoot, "SKILL.md")))
                    Directory.Delete(skillRoot, recursive: true);

                foreach (var file in store.ReadFiles(skill))
                {
                    var output = $"{target.RelativeRoot}/{skill.Id}/{file.RelativePath}";
                    var path = PathGuard.GetPathWithin(rootDirectory, output);
                    var directory = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(directory))
                        Directory.CreateDirectory(directory);
                    File.WriteAllBytes(path, file.Content);
                    written++;
                }

                // 旧目录迁移：历史版本写入 LegacyRoot 的同名技能是工具不读取的死文件，本次写入后顺手删除
                var migratedFrom = target.LegacyRoot is not null
                    && File.Exists(Path.Combine(
                        PathGuard.GetPathWithin(rootDirectory, $"{target.LegacyRoot}/{skill.Id}"), "SKILL.md"))
                    ? target.LegacyRoot
                    : null;
                if (migratedFrom is not null)
                    Directory.Delete(
                        PathGuard.GetPathWithin(rootDirectory, $"{migratedFrom}/{skill.Id}"), recursive: true);

                Console.WriteLine(migratedFrom is null
                    ? $"  [{target.Id}] {skill.Id}（{skill.FileCount} 个文件）"
                    : $"  [{target.Id}] {skill.Id}（{skill.FileCount} 个文件，已从 {migratedFrom} 迁移）");
            }
        }

        return written;
    }
}
