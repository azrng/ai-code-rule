using Spectre.Console;
using Spectre.Console.Testing;

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

// 按键驱动回归：注入 TestConsole 驱动 SpectreConsoleUi 的方向键 / 回车交互，
// 锁定「空勾选可直接回车确认」与「方括号文案转义」两条键盘路径行为
public sealed class SpectreConsoleUiInteractiveTests
{
    // Spectre 0.52 的按键输入契约还是原生 ConsoleKeyInfo（KeyInfo 结构是后续版本引入的）
    private static ConsoleKeyInfo Key(ConsoleKey key, char character)
        => new(character, key, shift: false, alt: false, control: false);

    // Selection/MultiSelection 要求终端交互能力，TestConsole 缺省不是 interactive，需显式开启
    private static TestConsole InteractiveConsole() => new TestConsole().Interactive();

    [Fact]
    public void ChooseMany_EmptyEnter_ConfirmsEmptySelection()
    {
        // 回归：MultiSelectionPrompt 缺省 Required=true 时空勾选回车被拒，且 ChooseMany 没有 0 项出口，
        // 交互用户会被锁死。此处第一下回车必须直接确认空集合。
        // 防挂死设计：若回归（回车被拒），后续空格会勾上首项、再回车返回非空 → 断言失败而非死循环等待输入
        var console = InteractiveConsole();
        console.Input.PushKey(Key(ConsoleKey.Enter, '\r'));
        console.Input.PushKey(Key(ConsoleKey.Spacebar, ' '));
        console.Input.PushKey(Key(ConsoleKey.Enter, '\r'));

        var result = new SpectreConsoleUi(console).ChooseMany(
            "选择能力：",
            new[] { new UiChoice("能力甲", ""), new UiChoice("能力乙", "") },
            Array.Empty<int>());

        Assert.Empty(result);
    }

    [Fact]
    public void ChooseMany_ToggleThenEnter_MapsBackToOriginalIndices()
    {
        var console = InteractiveConsole();
        console.Input.PushKey(Key(ConsoleKey.Spacebar, ' '));
        console.Input.PushKey(Key(ConsoleKey.DownArrow, '\0'));
        console.Input.PushKey(Key(ConsoleKey.Spacebar, ' '));
        console.Input.PushKey(Key(ConsoleKey.Enter, '\r'));

        var result = new SpectreConsoleUi(console).ChooseMany(
            "选择能力：",
            new[] { new UiChoice("能力甲", ""), new UiChoice("能力乙", "") },
            Array.Empty<int>());

        Assert.Equal(new[] { 0, 1 }, result.OrderBy(index => index).ToList());
    }

    [Fact]
    public void ChooseManyGrouped_EmptyEnter_ConfirmsEmptySelection()
    {
        var console = InteractiveConsole();
        console.Input.PushKey(Key(ConsoleKey.Enter, '\r'));
        console.Input.PushKey(Key(ConsoleKey.Spacebar, ' '));
        console.Input.PushKey(Key(ConsoleKey.Enter, '\r'));

        var result = new SpectreConsoleUi(console).ChooseManyGrouped(
            "选择技能：",
            new[] { new UiChoice("skill-a", ""), new UiChoice("skill-b", "") },
            new[] { new SkillGroupChoice("dotnet 组", new[] { 0 }), new SkillGroupChoice("web 组", new[] { 1 }) },
            Array.Empty<int>());

        Assert.Empty(result);
    }

    [Fact]
    public void Choose_LabelWithSquareBrackets_RendersAndMapsBack()
    {
        // 回归：选项文案未转义时含 [ ] 的 Label/Desc 会被当作 markup 解析，渲染即抛 MarkupException
        var console = InteractiveConsole();
        console.Input.PushKey(Key(ConsoleKey.DownArrow, '\0'));
        console.Input.PushKey(Key(ConsoleKey.Enter, '\r'));

        var result = new SpectreConsoleUi(console).Choose(
            "请选择 [重要]：",
            "取消",
            new[] { new UiChoice("普通[选项]", "描述[带括号]"), new UiChoice("第二[项]", "") });

        Assert.Equal(1, result);
        Assert.Contains("普通[选项]", console.Output, StringComparison.Ordinal);
        Assert.Contains("描述[带括号]", console.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void ChooseMany_LabelWithSquareBrackets_DoesNotThrow()
    {
        var console = InteractiveConsole();
        console.Input.PushKey(Key(ConsoleKey.Enter, '\r'));

        var result = new SpectreConsoleUi(console).ChooseMany(
            "选择 [能力]：",
            new[] { new UiChoice("能力[甲]", "desc[x]"), new UiChoice("能力乙", "") },
            Array.Empty<int>());

        Assert.Empty(result);
        Assert.Contains("能力[甲]", console.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Confirm_YesKey_ReturnsTrue()
    {
        // 0.52 的 ConfirmationPrompt 基于 TextPrompt<char>（按行读取）：y 之后需回车提交
        var console = InteractiveConsole();
        console.Input.PushKey(Key(ConsoleKey.Y, 'y'));
        console.Input.PushKey(Key(ConsoleKey.Enter, '\r'));

        Assert.True(new SpectreConsoleUi(console).Confirm("是否继续？", defaultValue: false));
    }

    [Fact]
    public void Confirm_NoKey_ReturnsFalse()
    {
        var console = InteractiveConsole();
        console.Input.PushKey(Key(ConsoleKey.N, 'n'));
        console.Input.PushKey(Key(ConsoleKey.Enter, '\r'));

        Assert.False(new SpectreConsoleUi(console).Confirm("是否继续？", defaultValue: true));
    }
}

// 纯文本是非确认：按行读取 y/n，空输入取默认值（stdin 重定向场景的行为契约）
public sealed class PlainConsoleUiConfirmTests
{
    [Theory]
    [InlineData("y\n", true, false)]
    [InlineData("YES\n", true, false)]
    [InlineData("n\n", false, true)]
    [InlineData("\n", true, true)]
    [InlineData("\n", false, false)]
    [InlineData("maybe\ny\n", true, false)]
    public void Confirm_ParsesLineInput(string input, bool expected, bool defaultValue)
    {
        var original = Console.In;
        Console.SetIn(new StringReader(input));
        try
        {
            Assert.Equal(expected, new PlainConsoleUi().Confirm("确认？", defaultValue));
        }
        finally
        {
            Console.SetIn(original);
        }
    }
}
