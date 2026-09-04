using Spectre.Console;

namespace RuleAssembler.Tests;

// 汇总确认只有 1 个选项 + 取消共 2 行，曾因 PageSize 取到 2 触发 Spectre.Console 参数校验异常（要求 >= 3）
public sealed class SpectreConsoleUiTests
{
    [Theory]
    [InlineData(0, 3)]
    [InlineData(1, 3)]
    [InlineData(2, 3)]
    [InlineData(3, 3)]
    [InlineData(5, 5)]
    [InlineData(12, 12)]
    [InlineData(30, 12)]
    public void PageBudget_ClampsIntoSpectreAllowedRange(int rowCount, int expected)
    {
        Assert.Equal(expected, SpectreConsoleUi.PageBudget(rowCount));
    }

    [Fact]
    public void PageBudget_ResultAlwaysAcceptedBySpectrePageSize()
    {
        // Spectre.Console 的 PageSize setter 对 < 3 抛 ArgumentException，预算值必须始终能直接传入
        var exception = Record.Exception(() =>
        {
            foreach (var rowCount in new[] { 0, 1, 2, 3, 7, 12, 13, 100 })
                new SelectionPrompt<string>().PageSize(SpectreConsoleUi.PageBudget(rowCount));
        });
        Assert.Null(exception);
    }
}
