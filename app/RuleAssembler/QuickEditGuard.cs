using System.Runtime.InteropServices;

namespace RuleAssembler;

// Windows conhost 的 QuickEdit 模式：鼠标点击窗口即进入文本选择，选择期间控制台
// 输入与输出统一挂起——方向键菜单表现为整体卡死（按键无响应、点击无反应），
// 与进程级故障难以区分，用户只能 Ctrl+C 杀进程重来。交互入口统一关闭 QuickEdit
// （保留扩展标志与模式其余位）；非 Windows、句柄不可用或模式不可调（重定向等）
// 静默跳过，不影响主流程
internal static class QuickEditGuard
{
    private const int StdInputHandle = -10;
    private const int ExtendedFlags = 0x0080;
    private const int QuickEditMode = 0x0040;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out int lpMode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleMode(IntPtr hConsoleHandle, int dwMode);

    public static void Disable()
    {
        if (!OperatingSystem.IsWindows())
            return;

        try
        {
            var input = GetStdHandle(StdInputHandle);
            if (input == IntPtr.Zero || input == new IntPtr(-1))
                return;
            if (!GetConsoleMode(input, out var mode))
                return;
            SetConsoleMode(input, (mode & ~QuickEditMode) | ExtendedFlags);
        }
        catch
        {
            // 控制台模式不可调（管道 / 非控制台宿主）时保持系统默认
        }
    }
}
