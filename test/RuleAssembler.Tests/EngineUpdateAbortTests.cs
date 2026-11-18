using System.Text;

namespace RuleAssembler.Tests;

// 引擎替换中断收尾回归：move2（.new → 原路径）失败时不得把唯一可用副本 .old 当残留删掉——
// 原路径空缺时先还原 .old，再清 .new；还原失败保留 .old 返回 false 提示用户手动恢复
public sealed class EngineUpdateAbortTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "ruleassembler-engine-abort-tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private string ExePath()
    {
        Directory.CreateDirectory(_tempDir);
        return Path.Combine(_tempDir, "use-ai-rule.exe");
    }

    [Fact]
    public void Abort_OriginalMissing_RestoresOldAndDeletesNew()
    {
        var exe = ExePath();
        File.WriteAllText(exe + ".old", "old bytes");
        File.WriteAllText(exe + ".new", "new bytes");

        var fullyAborted = PackResolver.TryAbortEngineSwap(exe);

        Assert.True(fullyAborted);
        Assert.True(File.Exists(exe));
        Assert.Equal("old bytes", File.ReadAllText(exe));
        Assert.False(File.Exists(exe + ".old"));
        Assert.False(File.Exists(exe + ".new"));
    }

    [Fact]
    public void Abort_OriginalIntact_KeepsItAndDeletesNew()
    {
        // 失败发生在 move1 之前（下载 / 写 .new 阶段）：原 exe 未动，只需清 .new 半成品
        var exe = ExePath();
        File.WriteAllText(exe, "current bytes");
        File.WriteAllText(exe + ".new", "new bytes");

        var fullyAborted = PackResolver.TryAbortEngineSwap(exe);

        Assert.True(fullyAborted);
        Assert.Equal("current bytes", File.ReadAllText(exe));
        Assert.False(File.Exists(exe + ".new"));
    }

    [Fact]
    public void Abort_RestoreBlocked_KeepsOldAsLastCopyAndReturnsFalse()
    {
        // 原路径空缺且 .old 被占用（还原失败）：.old 是唯一副本绝不能删，只清 .new
        var exe = ExePath();
        File.WriteAllText(exe + ".old", "old bytes");
        File.WriteAllText(exe + ".new", "new bytes");

        using (var lockOld = File.Open(exe + ".old", FileMode.Open, FileAccess.Read, FileShare.None))
        {
            var fullyAborted = PackResolver.TryAbortEngineSwap(exe);

            Assert.False(fullyAborted);
            Assert.False(File.Exists(exe));
            Assert.True(File.Exists(exe + ".old"));
            Assert.False(File.Exists(exe + ".new"));
        }
        // 内容断言在锁释放后做（FileShare.None 下 ReadAllBytes 会撞占用）
        Assert.Equal("old bytes", File.ReadAllText(exe + ".old"));
    }
}
