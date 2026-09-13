using System.Text;

namespace RuleAssembler.Tests;

// SkillImporter 落盘行为回归：三目标（codex → .codex/skills、zcode、claude）目录约定、
// 字节级原样拷贝、同名冲突检测（以 SKILL.md 为准）、同名技能先删后写（消除残留）、
// 无 SKILL.md 目录并入不删除、skipKeys 跳过语义（保留本地版本）
public sealed class SkillImporterTests : IDisposable
{
    private readonly string _root;
    private readonly SkillsStore _store = new();
    private readonly SkillImporter _importer = new();

    public SkillImporterTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "rule-skill-import-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void Import_WritesSkillFolderIntoEveryTargetConvention()
    {
        var skill = _store.Find("brainstorming");
        Assert.NotNull(skill);

        var written = _importer.Import(_root, SkillTargets.All, new[] { skill! }, _store);

        var expected = skill.FileCount * SkillTargets.All.Count;
        Assert.Equal(expected, written);
        foreach (var target in SkillTargets.All)
        {
            var path = Path.Combine(_root, target.RelativeRoot.Replace('/', Path.DirectorySeparatorChar), skill.Id, "SKILL.md");
            Assert.True(File.Exists(path), $"缺少 {path}");
        }
    }

    [Fact]
    public void Import_PreservesBytesExactly()
    {
        var skill = _store.Find("brainstorming");
        Assert.NotNull(skill);

        _importer.Import(_root, new[] { SkillTargets.Find("codex")! }, new[] { skill! }, _store);

        foreach (var file in _store.ReadFiles(skill!))
        {
            var path = Path.Combine(
                _root, ".codex", "skills", skill.Id,
                file.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(path), $"缺少 {path}");
            Assert.Equal(file.Content, File.ReadAllBytes(path));
        }
    }

    [Fact]
    public void Import_OverwriteDeletesSameNameSkillBeforeWriting_WhenNoSkipKeys()
    {
        // 重放 / 升级路径语义：同名技能（以 SKILL.md 为准）先删后写，
        // 内置内容整体重写，上游已删改的残留文件随之清理
        var skill = _store.Find("brainstorming");
        Assert.NotNull(skill);
        var skillRoot = Path.Combine(_root, ".zcode", "skills", skill.Id);
        Directory.CreateDirectory(skillRoot);
        File.WriteAllText(Path.Combine(skillRoot, "SKILL.md"), "旧的本地内容", Encoding.UTF8);
        File.WriteAllText(Path.Combine(skillRoot, "上游已删除的残留.md"), "stale", Encoding.UTF8);

        _importer.Import(_root, new[] { SkillTargets.Find("zcode")! }, new[] { skill! }, _store);

        var text = File.ReadAllText(Path.Combine(skillRoot, "SKILL.md"));
        Assert.NotEqual("旧的本地内容", text);
        Assert.Contains("name:", text);
        Assert.False(File.Exists(Path.Combine(skillRoot, "上游已删除的残留.md")));
    }

    [Fact]
    public void Import_DirectoryWithoutSkillManifestIsMergedNotDeleted()
    {
        // 无 SKILL.md 的目录不算同名技能：不删除目录，用户文件保留，内置文件并入写入
        var skill = _store.Find("brainstorming");
        Assert.NotNull(skill);
        var skillRoot = Path.Combine(_root, ".codex", "skills", skill.Id);
        Directory.CreateDirectory(skillRoot);
        File.WriteAllText(Path.Combine(skillRoot, "用户笔记.txt"), "非技能内容", Encoding.UTF8);

        _importer.Import(_root, new[] { SkillTargets.Find("codex")! }, new[] { skill! }, _store);

        Assert.True(File.Exists(Path.Combine(skillRoot, "SKILL.md")));
        Assert.Equal("非技能内容", File.ReadAllText(Path.Combine(skillRoot, "用户笔记.txt")));
    }

    [Fact]
    public void DetectConflicts_ReportsExistingPairsBySkillMarker()
    {
        var skill = _store.Find("brainstorming");
        Assert.NotNull(skill);

        // 未导入前无冲突；只导入 codex 一份后，冲突只报告 codex 这一对
        Assert.Empty(_importer.DetectConflicts(_root, SkillTargets.All, new[] { skill! }));

        _importer.Import(_root, new[] { SkillTargets.Find("codex")! }, new[] { skill! }, _store);
        var conflicts = _importer.DetectConflicts(_root, SkillTargets.All, new[] { skill! });

        var key = $"codex/{skill.Id}";
        Assert.Contains(key, conflicts);
        Assert.DoesNotContain($"zcode/{skill.Id}", conflicts);
    }

    [Fact]
    public void DetectConflicts_IgnoresDirectoryWithoutSkillManifest()
    {
        // 只有无 SKILL.md 的空目录不算同名技能，允许直接写入
        var skill = _store.Find("brainstorming");
        Assert.NotNull(skill);
        Directory.CreateDirectory(Path.Combine(_root, ".codex", "skills", skill.Id));

        Assert.Empty(_importer.DetectConflicts(_root, new[] { SkillTargets.Find("codex")! }, new[] { skill! }));
    }

    [Fact]
    public void Import_SkipKeys_KeepsLocalVersionUntouched()
    {
        var skill = _store.Find("brainstorming");
        Assert.NotNull(skill);
        var localSkillRoot = Path.Combine(_root, ".zcode", "skills", skill.Id);
        Directory.CreateDirectory(localSkillRoot);
        File.WriteAllText(Path.Combine(localSkillRoot, "SKILL.md"), "本地定制版本", Encoding.UTF8);
        var skipKeys = new HashSet<string>(StringComparer.Ordinal) { $"zcode/{skill.Id}" };

        var written = _importer.Import(
            _root, new[] { SkillTargets.Find("zcode")!, SkillTargets.Find("claude")! }, new[] { skill! }, _store, skipKeys);

        // zcode 一份被跳过，只写 claude 一份
        Assert.Equal(skill.FileCount, written);
        Assert.Equal("本地定制版本", File.ReadAllText(Path.Combine(localSkillRoot, "SKILL.md")));
        Assert.True(File.Exists(Path.Combine(_root, ".claude", "skills", skill.Id, "SKILL.md")));
    }

    [Fact]
    public void Import_UnknownTargetIdIsNotResolvable()
    {
        Assert.Null(SkillTargets.Find("cursor"));
        Assert.Equal(".codex/skills", SkillTargets.Find("Codex")!.RelativeRoot);
    }
}
