namespace RuleAssembler.Tests;

public sealed class DesignSystemGuardTests
{
    [Fact]
    public void ShouldPreserve_FileNotExists_AllowsWrite()
    {
        var root = CreateTempDirectory();
        try
        {
            Assert.False(DesignSystemGuard.ShouldPreserve(root));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ShouldPreserve_BaselineStatus_AllowsRefresh()
    {
        var root = CreateTempDirectory();
        try
        {
            WriteDesignSystem(root, "基线待定制");

            Assert.False(DesignSystemGuard.ShouldPreserve(root));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ShouldPreserve_CustomizedStatus_PreservesLocal()
    {
        var root = CreateTempDirectory();
        try
        {
            WriteDesignSystem(root, "已定制");

            Assert.True(DesignSystemGuard.ShouldPreserve(root));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ShouldPreserve_LegacyStatusWithoutBaselineContract_PreservesLocal()
    {
        // 旧版本生成的 yaml（status=启用中）无法证明未被定制，按已定制保守处理
        var root = CreateTempDirectory();
        try
        {
            WriteDesignSystem(root, "启用中");

            Assert.True(DesignSystemGuard.ShouldPreserve(root));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ShouldPreserve_StatusLineMissing_PreservesLocal()
    {
        var root = CreateTempDirectory();
        try
        {
            File.WriteAllText(
                Path.Combine(root, "design-system.yaml"),
                "# 手写的设计系统文件，没有 meta.status 标记\nmeta:\n  name: \"x\"\n");

            Assert.True(DesignSystemGuard.ShouldPreserve(root));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static void WriteDesignSystem(string root, string status)
    {
        File.WriteAllText(
            Path.Combine(root, "design-system.yaml"),
            $"# 设计系统唯一事实来源\nmeta:\n  name: \"x\"\n  status: \"{status}\"\ntokens:\n  colors: {{}}\n");
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "ds-guard-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
