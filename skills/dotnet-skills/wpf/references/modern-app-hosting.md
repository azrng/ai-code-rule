# 现代 WPF 应用装配（生产基线）

参照生产工程（.NET 10 WPF + CommunityToolkit.Mvvm 8.4.2 + WPF-UI 4.2.0 + Hardcodet.NotifyIcon.Wpf 2.0.1 + Microsoft.Extensions.Hosting 10.x）整理的「新项目从零装配」口径。教学级 code-behind 写法（事件直连控件）适用于原型与课件；正式项目按本册装配。

## 版本矩阵（消费工程 csproj 核实）

> 加包 / 升级只改本表；新项目按目标框架就近取版本。

| 包 | 参考版本 | 用途 |
|---|---|---|
| CommunityToolkit.Mvvm | 8.4.2 | 源生成器 MVVM（`[ObservableProperty]` / `[RelayCommand]` / Messenger） |
| Microsoft.Extensions.Hosting | 10.x | Generic Host：DI + 生命周期 |
| WPF-UI | 4.2.0 | Fluent 主题 + `FluentWindow` / `TitleBar` / Snackbar |
| Hardcodet.NotifyIcon.Wpf | 2.0.1 | 托盘图标（`TaskbarIcon`） |
| System.IO.Ports | 6.x+ | 串口（上位机） |
| S7netplus | 0.13+ | 西门子 PLC（上位机） |

## 目录与分层

```text
src/App/
├─ Assets/Styles/        # 资源 token 字典（Theme / Colors / Fonts / Spacing / Global）
├─ Models/               # 实体 + DTO + 序列化上下文
├─ Repositories/         # 持久化（Interfaces + Implementations）
├─ Services/             # 业务服务（Interfaces + Implementations）
├─ ViewModels/
│  ├─ Base/ViewModelBase.cs   # abstract partial : ObservableObject
│  └─ Windows/                # 一窗一 VM
└─ Views/
   ├─ Controls/            # 复用控件（UserControl）
   └─ Windows/             # 窗口（xaml + xaml.cs 只有 InitializeComponent 级代码）
```

- ViewModel 构造注入 Service，Service 注入 Repository；一个界面一个 ViewModel，巨型界面拆子 ViewModel
- ViewModel 需要「让窗口关闭」时定义事件（如 `RequestClose`），窗口 xaml.cs 订阅后 `Close()`——ViewModel 不引用 Window 类型

## Generic Host 装配（App.xaml.cs）

```csharp
public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _host = Host.CreateDefaultBuilder(e.Args)
            .ConfigureServices(services =>
            {
                services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
                // Repositories / Services / ViewModel / Window 逐个注册
                services.AddSingleton<MainWindowViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();
        await _host.StartAsync();

        Current.MainWindow = _host.Services.GetRequiredService<MainWindow>();
        // 初始化服务、显示窗口
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync();   // IDisposable 的服务在此统一释放
            _host.Dispose();
            _host = null;
        }
        base.OnExit(e);
    }
}
```

要点：
- 窗口与 ViewModel 一起进 DI；ViewModel 单例 / Transient 按生命周期定（对话框类 Transient，主窗单例）
- 托盘常驻应用：`App.xaml` 设 `ShutdownMode="OnExplicitShutdown"`，退出走显式 `Shutdown()`；`OnExit` 里释放托盘（Dispose）再停 Host
- 单实例：命名 `Mutex`（`Local\\<应用名>.SingleInstance`），`OnStartup` 里 `!isFirstInstance` 直接 `Shutdown()`，`OnExit` 释放

## 全局异常挂点（新项目必配）

```csharp
DispatcherUnhandledException += (s, e) => { Log(e.Exception); e.Handled = true; };
AppDomain.CurrentDomain.UnhandledException += (s, e) => { if (e.ExceptionObject is Exception ex) Log(ex); };
TaskScheduler.UnobservedTaskException += (s, e) => { Log(e.Exception); e.SetObserved(); };
```

- UI 线程未处理异常 → `DispatcherUnhandledException`；后台线程 → `AppDomain`；未观察 Task 异常 → `TaskScheduler`
- 日志落文件（含时间 + 场景标记 + 异常全链），不能只吞不记；「记了继续跑」只适用于可恢复异常，启动失败要退出

## 资源 token 分层（App.xaml）

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ui:ThemesDictionary Theme="Light"/>   <!-- WPF-UI 基础主题 -->
            <ui:ControlsDictionary/>               <!-- WPF-UI 控件样式 -->
            <ResourceDictionary Source="Assets/Styles/Theme.xaml"/>
            <ResourceDictionary Source="Assets/Styles/Colors.xaml"/>
            <ResourceDictionary Source="Assets/Styles/Fonts.xaml"/>
            <ResourceDictionary Source="Assets/Styles/Spacing.xaml"/>
            <ResourceDictionary Source="Assets/Styles/Global.xaml"/>
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

- 分层顺序固定：主题库在前，自有 token 在后允许覆盖；`Global.xaml` 的复合样式（文字样式、卡片样式）引用底层 token
- token 命名三级：`Color.*`（原始色）→ `Brush.*`（SolidColorBrush/渐变）→ 样式与 `Thickness.*` / `Radius.*` / `FontSize.*`；控件里只引用 Brush/Thickness/FontSize 级，不写裸色值
- 颜色 / 画刷可随主题切换跟随的用 `DynamicResource`，固定语义（状态色）用 `StaticResource`

## MVVM 源生成器全量用法

```csharp
public partial class HydrationReminderViewModel : ViewModelBase
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };

    [ObservableProperty]
    private string _headline = "主人，该喝水了";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CountdownText))]   // 派生属性联动通知
    private int _secondsRemaining = 5;

    public string CountdownText => $"{SecondsRemaining} 秒后自动关闭";

    [RelayCommand]
    private void Dismiss() => _timer.Stop();
}
```

- `[ObservableProperty]` 标字段（`_camelCase`），生成 `Headline` 公有属性；关联派生值用 `[NotifyPropertyChangedFor]`
- `[RelayCommand]` 标私有方法，生成 `DismissCommand`；`CanExecute` 用 `[RelayCommand(CanExecute = nameof(CanXxx))]`
- UI 计时 / 倒计时用 `DispatcherTimer`（天生 UI 线程，Tick 里直接改属性）；后台采集才用 Task + 封送
- 跨 ViewModel 消息：`WeakReferenceMessenger`（DI 里注册为 `IMessenger` 单例），收发消息类型定义强类型 record

## FluentWindow 与托盘

```xml
<ui:FluentWindow ...
    ExtendsContentIntoTitleBar="True"
    WindowBackdropType="Mica">
    <Grid>
        <ui:TitleBar Title="应用名" ShowMinimize="False" ShowMaximize="False"/>
        ...
    </Grid>
</ui:FluentWindow>
```

- `FluentWindow` + `ExtendsContentIntoTitleBar` + `ui:TitleBar`：自绘标题栏标准组合；窗体背景绑定 `Brush.Background.App`
- 托盘：`TaskbarIcon`（Hardcodet）注入 TrayService 统一管理；主窗「关闭」改藏匿（`Cancel` Closing → `Hide()`），真退出在托盘菜单；退出路径必须 Dispose 托盘图标，否则残留僵尸图标
- 开机自启：注册表 `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` 写入，包成 `IAutoStartService`
