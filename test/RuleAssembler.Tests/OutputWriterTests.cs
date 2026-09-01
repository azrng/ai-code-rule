namespace RuleAssembler.Tests;

public sealed class OutputWriterTests
{
    [Fact]
    public void WriteFiles_CommitsToProvidedRoot()
    {
        var root = CreateTempDirectory();
        try
        {
            var files = new[]
            {
                new PlannedFile { Output = "profile/AGENTS.md", Content = "内容" }
            };

            var written = new OutputWriter().WriteFiles(root, files);

            Assert.Equal(1, written);
            Assert.Equal("内容", File.ReadAllText(Path.Combine(root, "profile", "AGENTS.md")));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void WriteFiles_RollsBackEarlierFilesWhenCommitFails()
    {
        var root = CreateTempDirectory();
        try
        {
            File.WriteAllText(Path.Combine(root, "blocked"), "目录位置被文件占用");
            var files = new[]
            {
                new PlannedFile { Output = "first.md", Content = "first" },
                new PlannedFile { Output = "blocked/second.md", Content = "second" }
            };

            Assert.Throws<IOException>(() => new OutputWriter().WriteFiles(root, files));

            Assert.False(File.Exists(Path.Combine(root, "first.md")));
            Assert.Equal("目录位置被文件占用", File.ReadAllText(Path.Combine(root, "blocked")));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ruleassembler-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}
