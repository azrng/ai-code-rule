---
name: wpf
description: "WPF Windows 桌面与上位机（HMI）开发指南（.NET 6+ WPF + System.IO.Ports + S7netplus + 三菱 MX Component）。在编写或修改 xaml 视图、Grid/Dock 布局、数据绑定、MVVM 视图模型、DependencyProperty 自定义控件 / UserControl、Dispatcher 跨线程更新 UI、串口通信、西门子 S7 PLC 读写、三菱 PLC 读写或排查绑定不刷新 / UI 卡死 / 串口乱码 / PLC 连接失败时使用。Keywords: WPF, xaml, .NET 桌面, 上位机, HMI, 布局, Grid, DockPanel, 绑定, INotifyPropertyChanged, ICommand, MVVM, DependencyProperty, UserControl, Dispatcher, 线程, 串口, SerialPort, 串口助手, S7netplus, 西门子, S7-1200, S7-1500, PLC, 三菱, MX Component, ActUtlType, MC 协议, 轮询。"
---

# WPF 桌面与上位机开发指南

指导在 .NET（`net6.0-windows` 起）做 WPF 桌面应用与上位机（HMI）程序：XAML 布局面板选型、数据绑定与 MVVM、DependencyProperty 自定义控件、UI 线程模型，以及串口 / 西门子 S7netplus / 三菱 MX Component 三类设备接入。

**适用场景**：
- 新建 / 修改 .xaml 视图、布局（Grid / StackPanel / DockPanel / WrapPanel / Canvas）、资源与样式
- 绑定不刷新、`INotifyPropertyChanged` 实现、ICommand 命令挂接、MVVM 分层
- 自定义仪表控件（温度计、液位、曲线）：DependencyProperty 注册、属性变化回调、UserControl 组合
- 后台线程更新 UI（采集线程 → 界面）、`Dispatcher.Invoke` 与异步封装
- 串口参数配置、`DataReceived` 接收处理、十六进制 / 文本协议解析
- 西门子 PLC（S7-1200 / S7-1500）经 S7netplus 读写 I/Q/M/DB；三菱 PLC 经 MX Component（`ActUtlType`）读写软元件

**范围外**：业务分层（ViewModel/Service/Repository 怎么摆）归 onion-architecture skill；EF Core 归 azrng-efcore skill；Avalonia 跨平台桌面归 avalonia skill（WPF 的 API 不适用于 Avalonia，反之亦然）；MX Component 的安装、PLC 侧参数设置（MELSOFT GX Works）不在本 skill，只覆盖程序侧调用。

## 工作原则

1. **版本纪律**：WPF API 自 .NET Framework 4.x 迁移到 .NET Core 3.0+ 后基本稳定，但 .NET 版本影响行为（如 .NET 6 起 `SerialPort` 底层流变化、高 DPI 处理差异）。涉及平台行为、性能问题时先确认目标框架版本；第三方包版本以消费工程 csproj 为准。
2. **布局交给容器**：禁止用固定像素坐标摆放界面（Canvas 只留给绘图类场景）。窗口能跟随分辨率与内容变化是底线；尺寸约束用 `MinWidth` / `MaxWidth`，不用 `Width` 钉死。
3. **数据驱动界面**：能用绑定就不用代码后置操作控件。代码后置只保留：`InitializeComponent`、纯视图交互（焦点、动画触发）；业务状态一律进 ViewModel。
4. **UI 线程红线**：所有触碰可视化树的操作（读控件属性、改绑定源、`MessageBox.Show`）必须在 UI 线程；后台线程 / `DataReceived` 线程一律封送（见 references/threading-and-dispatcher.md）。

## 设备接入速查（上位机高频坑，详见 references）

| 症状 | 根因 | 正确写法 |
|---|---|---|
| 串口收到乱码 | 两端编码不一致（`Encoding.Default` 随系统代码页变化） | 显式约定编码：`new UTF8Encoding(false)` 或 ASCII / GBK（`Encoding.GetEncoding("GB18030")`），两端一致；十六进制协议用 byte[] 收发，禁止 string 直传 |
| 串口接收事件里更新界面卡死或抛 `InvalidOperationException` | `DataReceived` 在线程池线程触发，直接操作控件 | 事件里只读缓冲区，UI 更新封送：`Dispatcher.BeginInvoke(...)`（见 references/threading-and-dispatcher.md） |
| `SerialPort.Open()` 抛 `UnauthorizedAccessException` | 端口被占用（上次异常退出未释放 / 被其他程序占用） | 单一持有者 + `using`/显式 `Close()`；重连前先判断 `IsOpen`；关闭窗体时释放串口 |
| S7netplus 连接超时 / `ERROR_TIMEOUT` | IP、机架号 rack、插槽 slot 不对；S7-1500 未开启「允许来自远程对象的 PUT/GET 通信访问」 | `new Plc(CpuType.S71500, ip, 0, 1)`；1500 需在 TIA Portal CPU 属性 → Protection & Security 勾选 PERMISSIVE 或对应访问权限 |
| S7netplus 读到的值类型不对 / 抛转换异常 | `Read("DB1.DBW2")` 返回 object（Word → ushort），直接当 int/string 用 | 按地址类型转换（DBW→ushort、DBD→uint/float、DBX→bool）；批量读用 `ReadMultipleVars` + `DataItem`（自带头部类型，见 references/plc-s7.md） |
| 三菱 `ActUtlType.Open()` 返回非 0 错误码 | 逻辑站号与 MX Component 运行时设置不一致；64 位程序用了 32 位组件或未装 MX Component | 站号在 MX Component「通信设置向导」里配（`ActLogicalStationNumber` 对应）；程序位数与所装组件位数一致（`ActUtlType64` 对应 x64） |
| 三菱批量读 `ReadDeviceBlock` 崩溃 / 数据错位 | `out` 参数传的是首元素引用，数组长度与软元件点数不一致 | `int[] data = new int[点数]`，`ReadDeviceBlock(addr, 点数, out data[0])`；点数与数组长度严格一致（见 references/plc-mitsubishi.md） |

## 决策入口

- **选布局容器**：整体骨架 → `Grid`（行列 + `*`/`Auto`）；工具栏 / 状态栏 / 上下边缘停靠 → `DockPanel`；同向顺序排列 → `StackPanel`；流式换行 → `WrapPanel`；绝对定位绘图 → `Canvas`；等分网格 → `UniformGrid`。嵌套组合正常，禁止单个 Grid 摆几十行列（拆子容器，见 references/layout.md）。
- **选数据流方式**：一次性取值 / 原型 demo → 代码后置直读控件；正式界面 → 绑定 + ViewModel；控件间联动（Slider 驱动文本）→ 绑定 + 转换器或 ViewModel 属性计算。
- **写命令**：按钮点击业务 → `ICommand`（项目已有 CommunityToolkit.Mvvm 用 `[RelayCommand]`；无包时自建 `RelayCommand`，见 references/bindings-and-mvvm.md）。纯视图行为（选中聚焦）→ 事件。
- **做自定义控件**：先判断复用范围——单窗口内部组合 → 直接写 XAML；跨页面 / 跨项目复用带属性绑定 → UserControl + DependencyProperty（仪表、温度计类，见 references/dependency-properties-and-usercontrols.md）；需要换模板 → TemplatedControl（WPF 是 Lookless Control + ControlTemplate）。
- **接后台数据**：一次性请求 → `async/await`（UI 事件 handler 直接 async）；周期采集 → 专用线程 / `System.Threading.Timer` + Dispatcher 封送，禁止 UI 线程 `while(true)`（见 references/threading-and-dispatcher.md）。
- **接设备**：串口设备（传感器 / 单片机）→ references/serial-ports.md；西门子 S7 → references/plc-s7.md；三菱 → references/plc-mitsubishi.md。选型原则：有 .NET 库优先库（S7netplus），COM 组件兜底（MX Component），裸串口协议最后手撸。

## references 导航

| 文件 | 何时读 |
|---|---|
| `references/layout.md` | 布局面板选型、行列定义、嵌套骨架、测量/排列两阶段 |
| `references/bindings-and-mvvm.md` | 绑定语法、INotifyPropertyChanged、ICommand/RelayCommand、MVVM 分层 |
| `references/dependency-properties-and-usercontrols.md` | DependencyProperty 注册与回调、UserControl 组合仪表控件、SizeChanged 重绘 |
| `references/threading-and-dispatcher.md` | UI 线程模型、Dispatcher.Invoke/BeginInvoke、后台采集线程、async UI |
| `references/serial-ports.md` | 串口参数、DataReceived、编码与十六进制、串口助手类工具实现 |
| `references/plc-s7.md` | S7netplus 连接（S7-1200/1500）、地址语法、单点与批量读写、轮询 |
| `references/plc-mitsubishi.md` | MX Component（ActUtlType/ActUtlType64）、软元件地址、单点与批量读写 |
| `references/modern-app-hosting.md` | 新项目生产基线装配：Generic Host DI、CommunityToolkit.Mvvm 全量用法、WPF-UI FluentWindow、托盘常驻、全局异常挂点、资源 token 分层 |

## 代码生成优先级

```text
项目既有 ViewModel / 控件 / 服务（复用优先）
        ↓
WPF 内置控件 + 布局面板
        ↓
绑定 + ViewModel（CommunityToolkit.Mvvm 或自建 RelayCommand）
        ↓
UserControl + DependencyProperty（确有复用价值才抽象）
        ↓
设备库（S7netplus / System.IO.Ports / MX Component）
```

## 禁止事项

- 禁止用 `Canvas.Left/Top` 摆常规表单界面（绘图刻度、流程图节点除外）
- 禁止后台线程直接碰控件（读 `TextBox.Text` 也算）——必须 `Dispatcher` 封送
- 禁止 `Encoding.Default` 收发串口数据（随系统代码页漂移）；禁止按字符数解读十六进制协议
- 禁止 UI 线程同步阻塞等设备响应（`plc.Open()` / `serialPort.Open()` 直接放 UI 事件里不带异步时，连接超时会冻界面）——放后台任务 + 封送回 UI
- 禁止在 `OnPropertyChanged` 回调里再改同一个依赖属性（递归重入）
- 禁止把业务逻辑写进 xaml.cs（事件里拼 SQL、算业务结果）；xaml.cs 只留视图交互
- Windows / PowerShell 场景禁止 `&&` 链式命令

## 版本与事实来源

- 基准组合：.NET 6（`net6.0-windows`）+ `System.IO.Ports` 6.x + `S7netplus` 0.13+；三菱走 MX Component COM（`ActUtlType64`，程序位数需匹配）。升级 / 加包先看消费工程 csproj 实际版本。
- 事实来源：官方文档（learn.microsoft.com 的 WPF / System.IO.Ports 章节）、S7netplus GitHub（github.com/fbarthhmaier/S7netplus，读源码确认地址解析行为）、三菱 MX Component 手册（MELSEC 的 `ActUtlType` 接口）。写不确定的 API 前先核实，不凭印象。
