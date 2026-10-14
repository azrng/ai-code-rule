namespace RuleAssembler.Tests;

public sealed class ResourceStoreTests
{
    [Theory]
    [InlineData("a\r\nb\r\n", "a\nb\n")]
    [InlineData("a\r\n\r\n\r\nb", "a\n\n\nb")]
    [InlineData("a\rb\n", "a\nb\n")]
    [InlineData("a\nb\n", "a\nb\n")]
    [InlineData("", "")]
    public void NormalizeLineEndings_FlattensAllVariants_ToLf(string input, string expected)
    {
        // CRLF 检出（autocrlf worktree）、孤立 CR、已是 LF 的内容都归一为 LF，
        // 防止根模板按 \n 写死的空行收敛等逻辑因检出环境不同产生内容差异
        Assert.Equal(expected, ResourceStore.NormalizeLineEndings(input));
    }

    [Fact]
    public void Read_ReturnsContentWithoutCarriageReturn()
    {
        var content = new ResourceStore().Read("shared/ui/design-system-liveness.md");

        Assert.NotNull(content);
        Assert.DoesNotContain('\r', content);
    }
}
