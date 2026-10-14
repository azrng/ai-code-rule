using System.Text;

namespace RuleAssembler.Tests;

// 内置技能仓库（Skills.* 嵌入资源）行为回归：
// 技能按「含 SKILL.md 的最外层目录」发现、分组目录只做归档、嵌套 SKILL.md 归外层、
// description 支持单行与 YAML 块写法、文件按字节读取保证脚本原样释放
public sealed class SkillsStoreTests
{
    private readonly SkillsStore _store = new();

    [Fact]
    public void Discover_FindsAllEmbeddedSkills_WithUniqueIds()
    {
        var skills = _store.Discover();

        // 下限只防「嵌入资源整体丢失」这类回归：技能增删是日常维护动作，不锁精确数量
        Assert.True(skills.Count >= 30, $"内置技能应不少于 30 个，实际 {skills.Count}");
        var ids = skills.Select(skill => skill.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void Discover_GroupDirectoryIsArchiveOnly_SkillIdUsesDeepestFolderName()
    {
        var skills = _store.Discover();

        var aspnet = skills.FirstOrDefault(skill => skill.Id == "aspnet-core");
        Assert.NotNull(aspnet);
        Assert.Equal("dotnet-skills/aspnet-core", aspnet.ResourceRoot);
        Assert.True(aspnet.FileCount >= 1);
    }

    [Fact]
    public void Discover_NestedSkillManifestsBelongToTheirOwnFolder()
    {
        // test-skills 目录本身没有 SKILL.md，其下的子技能各自成技能且互不吸收
        var skills = _store.Discover();

        var runTests = skills.FirstOrDefault(skill => skill.Id == "run-tests");
        Assert.NotNull(runTests);
        Assert.Equal("dotnet-skills/test-skills/run-tests", runTests.ResourceRoot);
    }

    [Fact]
    public void Discover_EverySkillHasNonEmptyDescription()
    {
        // 选择列表与 --check 都依赖 description；frontmatter 缺失会导致 --check 失败
        var missing = _store.Discover()
            .Where(skill => string.IsNullOrWhiteSpace(skill.Description))
            .Select(skill => skill.Id)
            .ToList();
        Assert.Empty(missing);
    }

    [Fact]
    public void ReadFiles_ReturnsAllSkillFilesUnderRoot()
    {
        var skill = _store.Find("aspnet-core");
        Assert.NotNull(skill);

        var files = _store.ReadFiles(skill!);

        Assert.Equal(skill.FileCount, files.Count);
        Assert.Contains(files, file => file.RelativePath == "SKILL.md");
        Assert.All(files, file => Assert.DoesNotContain("..", file.RelativePath));
    }

    [Fact]
    public void ReadFiles_SkillMarkdownDecodesToUtf8Text()
    {
        var skill = _store.Find("frontend-design");
        Assert.NotNull(skill);

        var manifest = _store.ReadFiles(skill!).First(file => file.RelativePath == "SKILL.md");
        var text = Encoding.UTF8.GetString(manifest.Content);

        Assert.Contains("name:", text);
        Assert.DoesNotContain('\0', text);
    }

    [Theory]
    [InlineData("---\nname: demo\ndescription: 单行描述\n---\n\n正文", "单行描述")]
    [InlineData("---\nname: demo\ndescription:\n  多行续写描述。\n  触发场景：测试。\n---\n\n正文", "多行续写描述。")]
    [InlineData("---\nname: demo\ndescription: \"带引号的描述\"\n---\n", "带引号的描述")]
    [InlineData("---\nname: demo\ndescription: >-\n  折叠块描述首行。\n  折叠块描述次行。\n---\n", "折叠块描述首行。")]
    [InlineData("---\nname: demo\ndescription: |\n  字面量块描述。\n---\n", "字面量块描述。")]
    public void ParseDescription_SupportsInlineAndBlockStyles(string markdown, string expected)
    {
        Assert.Equal(expected, SkillsStore.ParseDescription(markdown));
    }

    [Fact]
    public void ParseDescription_MissingOrMalformedFrontmatter_ReturnsEmpty()
    {
        Assert.Equal("", SkillsStore.ParseDescription("# 无 frontmatter\n正文"));
        Assert.Equal("", SkillsStore.ParseDescription("---\nname: demo\n---\n正文"));
        Assert.Equal("", SkillsStore.ParseDescription("---\nname: demo\ndescription:\n---\n正文"));
    }
}
