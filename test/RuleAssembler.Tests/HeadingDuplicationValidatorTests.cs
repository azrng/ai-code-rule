namespace RuleAssembler.Tests;

public sealed class HeadingDuplicationValidatorTests
{
    [Fact]
    public void FindDuplicateHeadings_同文件同级同名标题被检出()
    {
        const string content = """
            # 标题

            ## Docker 规则

            ### Docker 交付规则
            - 第一份

            ### Docker 交付规则
            - 第二份（叠加缺陷）
            """;

        var duplicates = HeadingDuplicationValidator.FindDuplicateHeadings(content);

        var heading = Assert.Single(duplicates);
        Assert.Equal("### Docker 交付规则", heading);
    }

    [Fact]
    public void FindDuplicateHeadings_围栏代码块内的井号行不算标题()
    {
        const string content = """
            ## 后端规则

            ```text
            ### 后端规则
            Endpoints → Application → Domain
            ```

            ### 后端规则
            - 真正的重复来源是 include 叠加
            """;

        var duplicates = HeadingDuplicationValidator.FindDuplicateHeadings(content);

        Assert.Empty(duplicates);
    }

    [Fact]
    public void FindDuplicateHeadings_不同层级同名标题不算重复()
    {
        const string content = """
            ## 依赖注入规范

            ### 依赖注入规范
            - 父节概览与子节细则分层
            """;

        var duplicates = HeadingDuplicationValidator.FindDuplicateHeadings(content);

        Assert.Empty(duplicates);
    }

    [Fact]
    public void FindDuplicateHeadings_四级及以下与一级标题不参与()
    {
        const string content = """
            # 主标题

            ## 规则区

            #### 依赖注入规范

            #### 依赖注入规范

            ## 另一节
            """;

        var duplicates = HeadingDuplicationValidator.FindDuplicateHeadings(content);

        Assert.Empty(duplicates);
    }

    [Fact]
    public void FindDuplicateHeadings_无重复时返回空()
    {
        const string content = """
            # 标题

            ## 适用范围

            ## 技术栈

            ### 部署

            ### 默认端口
            """;

        var duplicates = HeadingDuplicationValidator.FindDuplicateHeadings(content);

        Assert.Empty(duplicates);
    }
}
