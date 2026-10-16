# 生命周期与 UI 线程调度

事实来源：官方文档（docs.avaloniaui.net「Application lifetimes」「Threading model」，对应 12.x）核实后提炼；版本敏感处按 SKILL.md「上游源码按需查证」回 `12.1.2` tag 核实。

## 桌面生命周期

`Program.cs` 的两个方法各司其职（`BuildAvaloniaApp` 供预览器调用，不要在里面弹窗或碰生命周期）：

```csharp
public static AppBuilder BuildAvaloniaApp()
    => AppBuilder.Configure<App>().UsePlatformDetect();

[STAThread]
public static int Main(string[] args)
    => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
```

窗口在 `OnFrameworkInitializationCompleted` 里创建，按 lifetime 分支（12 新增 Android 的 `IActivityApplicationLifetime`，用 `MainViewFactory` 工厂应对 Activity 重建，不能复用 11 的单视图启动代码）：

```csharp
public override void OnFrameworkInitializationCompleted()
{
    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
        desktop.MainWindow = new MainWindow { DataContext = new MainViewModel() };
        desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
    }
    base.OnFrameworkInitializationCompleted();
}
```

- 预览器（设计器进程）中 `ApplicationLifetime` 为 null——分支写法天然兼容。
- `ShutdownMode` 三选一，按应用真实语义显式选：
  - `OnLastWindowClose`（默认）：最后一个窗口关闭才退出（多窗口工具类应用常用）；
  - `OnMainWindowClose`：主窗口关即退（单主窗口 + 临时弹窗场景必须显式选它，否则弹窗活着应用不退）；
  - `OnExplicitShutdown`：完全手动调 `Shutdown()`（托盘常驻类应用）。
- `IClassicDesktopStyleApplicationLifetime` 继承 `IControlledApplicationLifetime`：可订阅 `Startup` / `Exit`、显式 `Shutdown()`、持有 `Windows` 集合。
- 需要完全接管主循环时走 `BuildAvaloniaApp().Start(AppMain, args)`（自行 `app.Run(token)`）。

## 模态对话框：owner + await 结果

```csharp
var result = await dialog.ShowDialog<Result>(ownerWindow);   // 必须给 owner
```

- `ShowDialog` 不给 owner 在部分平台抛异常/行为未定义；结果经 `Close(result)` 返回。
- 窗口创建收敛在 composition 层 / 窗口服务，ViewModel 不 new Window——需要弹框时注入对话框服务（Ursa 弹层装配见 references/ursa-overlays.md）。

## UI 线程模型与封送

单线程 UI 模型：控件的读写在非 UI 线程直接抛 `InvalidOperationException: Call from invalid thread`。

| API | 语义 | 何时用 |
|---|---|---|
| `Dispatcher.UIThread.Post(action)` | 排队即返回（fire-and-forget），可带 `DispatcherPriority` | 不关心完成时点的通知 |
| `Dispatcher.UIThread.InvokeAsync(func)` | 返回 Task，回调完成后才完成 | 调用方要等结果 / 观察完成 |
| `Dispatcher.UIThread.CheckAccess()` | 当前是否 UI 线程 | 决定要不要封送 |
| `Dispatcher.UIThread.VerifyAccess()` | 非 UI 线程即抛 | 断言必须在 UI 线程的代码路径 |
| `myControl.Dispatcher` | 对象创建线程的 dispatcher | 多 dispatcher 库代码（12），应用代码基本等价 `UIThread` |

```csharp
// 后台拿数、UI 封送：I/O await 后如果已切到工作线程，更新绑定状态前封送回 UI 线程
var results = await service.LoadAsync(ct).ConfigureAwait(false);
await Dispatcher.UIThread.InvokeAsync(() =>
{
    Items.Clear();
    foreach (var r in results) Items.Add(r);
});
```

- 在 UI 线程上下文里 `await` 后默认回到 UI 线程（Avalonia 装了 SynchronizationContext）；`ConfigureAwait(false)` 之后才需要手动封送。
- **绑定集合只在 UI 线程改**；后台线程直接 `Items.Add` 是「偶尔才崩」类事故。
- 禁止在 UI 线程 `.Result` / `.Wait()`（死锁 + 卡界面）；长操作 `Task.Run` + await。
- 后台操作生命周期跟宿主走：窗口/标签页关闭时取消（CancellationToken），别让孤儿任务回调已释放的 ViewModel。
- 进度上报用 `Progress<T>`（回调自动在 UI 线程）；周期任务用 `DispatcherTimer`（Tick 在 UI 线程）；长循环让出渲染用 `await Dispatcher.Yield()`。

## DispatcherPriority 速查（高 → 低）

`Send` → `Normal` → `Default`（前台最低）→ `Render` → `Loaded` → `Input` → `Background` → `ContextIdle` → `ApplicationIdle` → `SystemIdle`。

常用就三档：默认（普通更新）、`Render`（渲染前抢跑）、`Background`（等界面忙完，如「等激活稳定再补显示 Flyout」——见 references/windows-flyouts-popups.md 陷阱一）。

## 与测试的交界

Headless 测试的 dispatcher 感知写法（`AvaloniaFact` / `AvaloniaTestApplication`）见 references/testing-and-performance.md；本文件只管生产代码的调度正确性。
