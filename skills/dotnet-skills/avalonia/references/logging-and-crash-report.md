# 崩溃挂点与日志实践

## 反例：只有 LogToTrace 等于崩溃零记录（真实案例）

消费工程 ApixPress 曾连续出现「双击启动即闪退」，事后全仓库排查：

- **无任何全局异常挂点**：`AppDomain.CurrentDomain.UnhandledException`、`Dispatcher.UIThread.UnhandledException`、`TaskScheduler.UnobservedTaskException` 三处都没有
- **无日志框架**（Serilog / NLog 均未引入），也没有任何写日志文件的代码
- `Program.cs` 只有 `.LogToTrace()`——它只输出到调试器 Trace 通道，**挂着调试器才能看到；直接运行的闪退不落盘**

结果：闪退原因完全丢失。教训：**Avalonia 桌面应用交付的第一件事是把三类异常挂点接上，让崩溃落盘**。

## 没有日志时的事后取证（Windows）

1. 事件查看器（Win+R → `eventvwr`）→ Windows 日志 → 应用程序：找来源 `.NET Runtime` 或 `Application Error`、程序名为应用名的错误条目，通常含异常类型、错误代码与堆栈
2. `%LOCALAPPDATA%\CrashDumps`：WER 本地转储默认不一定开启，有则可用 WinDbg / dotnet-dump 分析

## 最简崩溃日志（不引第三方框架，覆盖三类崩溃）

```csharp
// CrashLog.cs —— 线程安全、按日滚动、写日志自身失败时静默（不能二次抛）
internal static class CrashLog
{
    private static readonly object Gate = new();

    public static void Write(Exception? exception, string source)
    {
        if (exception is null) return;
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ApixPress", "logs");
            Directory.CreateDirectory(dir);
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{source}] "
                     + $"{exception.GetType().FullName}: {exception.Message}"
                     + Environment.NewLine + exception.StackTrace
                     + Environment.NewLine;
            lock (Gate)
            {
                File.AppendAllText(Path.Combine(dir, $"crash-{DateTime.Now:yyyyMMdd}.log"),
                    line, new UTF8Encoding(false));
            }
        }
        catch { /* 日志失败不能再抛 */ }
    }
}
```

三个挂点，各自负责一类崩溃：

```csharp
// Program.Main 最早处（覆盖后台线程未处理异常 + 未观察任务异常）
[STAThread]
public static void Main(string[] args)
{
    AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        CrashLog.Write(e.ExceptionObject as Exception, "AppDomain");
    TaskScheduler.UnobservedTaskException += (_, e) =>
    {
        CrashLog.Write(e.Exception, "UnobservedTask");
        e.SetObserved();   // 标记已观察，避免按未处理异常升级
    };

    BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
}
```

```csharp
// App.axaml.cs —— UI 线程异常（Avalonia 12.1.2 核实：Dispatcher.Exceptions.cs）
public override void OnFrameworkInitializationCompleted()
{
    // 注意：Application 基类有实例属性 Dispatcher，在 App 类内必须全限定类型名，
    // 写 Dispatcher.UIThread 会报 CS0176「无法使用实例引用来访问成员」
    Avalonia.Threading.Dispatcher.UIThread.UnhandledException += (_, e) =>
    {
        CrashLog.Write(e.Exception, "UIThread");
        // e.Handled = true 可吞异常避免闪退；谨慎使用——状态可能已损坏，
        // 建议配合弹窗提示用户保存工作并重启，而不是静默继续
    };
    ...
}
```

要点：

- `AppDomain.UnhandledException` 回调在后台线程，**只写文件，不碰 UI 控件**；它是终结性通知，进程随后退出
- `Dispatcher.UIThread.UnhandledException` 只拦 UI 线程（绑定回调、输入事件、渲染回调里抛的异常），`Handled` 属性决定是否吞掉
- `TaskScheduler.UnobservedTaskException` 在 GC 回收带未观察异常的 Task 时触发（延迟触发，不能只靠它）
- 落盘路径 `%LOCALAPPDATA%\<AppName>\logs\`；按日滚动 `crash-yyyyMMdd.log`；UTF-8 无 BOM
- 三处挂齐才覆盖完整：只挂 UI 线程漏掉后台线程，只挂 AppDomain 拿不到 UI 线程可恢复异常

## 日志实践分层（按需递进）

1. **崩溃日志（必做）**：上文三挂点，几十行代码，崩溃必落盘
2. **开发期诊断**：`.LogToTrace()` 保留（挂调试器时看 Avalonia 内部诊断）；开发期定位样式 / 绑定问题优先编译错误与 Trace
3. **运行期业务日志**：关键失败路径（网络请求失败、导入 / 保存失败、用户可感知的降级）走统一的轻量 helper 写文件（与 CrashLog 同目录 `app-yyyyMMdd.log`），**不散落 `File.AppendAllText`**；记录操作上下文（接口 id、项目 id）而非敏感数据（token / 密码）
4. **何时引入 Serilog / NLog**：需要结构化字段、滚动清理策略、多 sink（文件 + 控制台 + 远端）时再引入；体量小的工具类应用用自写几十行足够（KISS，避免为日志引一堆依赖）

## 崩溃时对用户

- 优先让进程带着已写好的日志退出，二次启动时检测到未正常退出的标记（可选）提示「上次异常退出」
- 不要在 `UnhandledException` 里尝试保存业务数据后继续运行——状态一致性无法保证，保存动作本身可能再次抛异常
