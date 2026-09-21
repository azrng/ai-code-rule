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

    [Fact]
    public void GetTechStackMismatchWarning_LocalReactIncomingBlazor_ReturnsWarning()
    {
        // 实测踩坑场景：Blazor 组合重新生成时保留了 React 组合的旧本地文件
        var root = CreateTempDirectory();
        try
        {
            WriteDesignSystemWithStack(root, "已定制", "# 技术栈：React + TypeScript + Ant Design 5 + ECharts + Zustand");
            var blazorBaseline = "# 设计与组件规则的唯一事实来源。\n"
                + "# 技术栈：.NET（版本以仓库 TargetFramework 为准）+ Blazor Server + BootstrapBlazor (Bootstrap 5)\n"
                + "meta:\n  name: \"x\"\n";

            var warning = DesignSystemGuard.GetTechStackMismatchWarning(root, blazorBaseline);

            Assert.NotNull(warning);
            Assert.Contains("React + TypeScript + Ant Design 5", warning);
            Assert.Contains("Blazor Server + BootstrapBlazor", warning);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void GetTechStackMismatchWarning_SameStack_ReturnsNull()
    {
        var root = CreateTempDirectory();
        try
        {
            const string stack = "# 技术栈：React + TypeScript + Ant Design 5 + ECharts + Zustand";
            WriteDesignSystemWithStack(root, "已定制", stack);

            Assert.Null(DesignSystemGuard.GetTechStackMismatchWarning(root, $"# 头注释\n{stack}\nmeta:\n"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void GetTechStackMismatchWarning_AnchorMissing_NoWarning()
    {
        // 任一侧读不出锚点（如手写文件无头部注释）时不告警，避免误报
        var root = CreateTempDirectory();
        try
        {
            WriteDesignSystem(root, "已定制");

            Assert.Null(DesignSystemGuard.GetTechStackMismatchWarning(
                root, "# 头注释\n# 技术栈：.NET + Blazor Server + BootstrapBlazor (Bootstrap 5)\nmeta:\n"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static void WriteDesignSystemWithStack(string root, string status, string stackLine)
    {
        File.WriteAllText(
            Path.Combine(root, "design-system.yaml"),
            $"# 设计系统唯一事实来源\n{stackLine}\nmeta:\n  name: \"x\"\n  status: \"{status}\"\ntokens:\n  colors: {{}}\n");
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
