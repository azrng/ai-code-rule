---
name: avalonia
description: "Avalonia UI 12.x 桌面客户端开发指南（.NET + Avalonia 12.1.2 + Semi.Avalonia 主题 + Ursa.Avalonia 控件 + CommunityToolkit.Mvvm）。覆盖样式系统与设计 token 管线、编译绑定、Flyout/Popup 弹层行为、动画 Animator 对照、虚拟化、自绘窗口、启动崩溃排查。Keywords: Avalonia, AvaloniaUI, XAML, axaml, .NET 桌面, Semi.Avalonia, Ursa, ControlTheme, Flyout, Popup, 编译绑定, x:DataType, VirtualizingStackPanel, MVVM, ObservableProperty, RelayCommand, 动画, Animator, 样式, 伪类, pointerover。"
---

# Avalonia UI 12.x 开发指南

指导在 .NET 桌面项目中正确使用 Avalonia UI（锁定 12.x，消费工程基准 12.1.2 + Semi.Avalonia 12.1.0.1 + Ursa.Avalonia 2.2.0 + CommunityToolkit.Mvvm）：样式与资源管线、编译绑定、弹层体系、动画、列表虚拟化、窗口自绘，以及三类「编译通过但启动/运行崩溃」的高频坑。

**适用场景**：
- 新建 / 修改 Avalonia 视图（.axaml）、样式（Styles / ControlTheme）、资源字典
- 数据绑定报 `AVLN2000`、样式附加期抛 `InvalidCastException`、弹层不弹出或秒关
- 写动画（`Style.Animations`）、操作剪贴板 / 文件选择等平台 API
- 列表性能（虚拟化）、窗口自绘（`WindowDecorations="None"`）

**范围外**：业务分层（ViewModel/Service/Repository 怎么摆）归 onion-architecture skill；EF Core 归 azrng-efcore skill；Semi/Ursa 全量控件 API 以各自源码为准，本 skill 只写装配与高频用法。

## 工作原则

1. **版本纪律**：Avalonia 12 与 11.x 存在破坏性差异（IClipboard 重构、PresentationSource、Obsolete 清理）。涉及动画、剪贴板、平台 API 时，**先查本地源码或 NuGet ref 程序集确认 12.x 的真实 API，不凭 11.x 或 WPF 经验书写**。本地源码路径：`D:\SourceCode\avalonias\Avalonia`（核对 tag `12.1.2`）。
2. **样式走管线**：颜色 / 间距 / 圆角 / 阴影一律来自资源 token（`Assets/Styles/*.axaml`），通过 `Classes` + 选择器 Setter 生效，**禁止控件内联硬编码**。新 token 先加资源字典（注意类型：`x:Double` 标量 ≠ `Thickness`），再引用。
3. **视觉状态用伪类**：`:pointerover` / `:pressed` / `:selected` / `:checked`（不是 WPF 的 Trigger，Avalonia 没有 Trigger）。数据驱动的条件视觉用 `Classes.Xxx="{Binding}"`。
4. **编译绑定必须带 `x:DataType`**：Window / UserControl / DataTemplate 都要写；跨 ViewModel 用 `$parent[Window].((vm:Xxx)DataContext)` 强转，不靠反射绑定兜底。

## 本地源码按需查询（不确定就查，禁止凭经验写）

`D:\SourceCode\avalonias\` 下三个仓库是本 skill 的事实来源，**写任何不确定的 API / 样式 / 控件行为前，先按下面方式查证**（Windows 下用 `git -C <仓库> ...`，PowerShell 禁止 `&&`）：

| 查什么 | 在哪查 | 怎么查 |
|---|---|---|
| 某成员 / 签名在 12.1.2 是否存在 | Avalonia 仓库 | `git -C D:\SourceCode\avalonias\Avalonia show 12.1.2:src/Avalonia.Base/Input/Platform/IClipboard.cs`（版本锚定，别看 main——main 可能超前） |
| 控件有哪些属性 / 伪类 | `Avalonia/src/Avalonia.Controls/*.cs` | grep `StyledProperty` / `PseudoClasses.Set(":...")` |
| 某类型有没有 Animator | `Avalonia/src/Avalonia.Base/Animation/Animators/` | 列目录比对属性类型 |
| 官方主题怎么写某控件样式 | `Semi.Avalonia/src/Semi.Avalonia/Controls/*.axaml` | 直接读对应控件 axaml（ControlTheme 范式） |
| 官方可运行示例（Headless 测试 / 对话框模式 / 自定义控件 / MVVM 验证 / IDataTemplate / TrayIcon） | `Avalonia.Samples/src/Avalonia.Samples/{Testing,ViewInteraction,CustomControls,MVVM,DataTemplates,DesktopIntegration}` | 每个样例目录带 README.adoc 讲解；写交互测试、对话框、自绘控件前先找对应官方样例 |
| Ursa 控件（Dialog / Loading 等） | `Ursa.Avalonia/src/Ursa.Avalonia` + `Ursa.Themes.Semi` | 读控件实现与主题 axaml |
| 免检参照 | `D:\Work\github\ApixPress\src\ApixPress.App` | 消费工程已在 12.1.2 实测通过的写法，优先复用 |

辅助：NuGet ref 程序集 XML（`~/.nuget/packages/avalonia/12.1.2/ref/net8.0/Avalonia.Base.xml`）可 grep 快速确认 API 存在性。查不到、核实不了的一律不写，向用户说明。

## 三类「编译通过但启动/运行崩溃」速查（高频，详见 references/startup-crash-checklist.md）

| 症状（错误原文） | 根因 | 正确写法 |
|---|---|---|
| `InvalidCastException: Setter value '8' is not a valid value for property 'Padding'`（启动即崩） | 标量 token（`x:Double` 的 `Space.*`）赋给了 Thickness 属性 | Padding/Margin 用 `Pad.*`（Thickness token） |
| `FormatException`（BoxShadow 解析，启动即崩） | 阴影值带 `px` 单位（`-24px 0 48px 0`） | 纯数值 `-24 0 48 0 #80000000`（`BoxShadow.Parse` 用 `double.Parse`） |
| `InvalidOperationException: No animator registered for the property RenderTransform`（动画首次运行崩） | `rotate(0deg)` 关键帧未注册 Animator | `RenderTransform` 挂 `<RotateTransform />` 后动画 `RotateTransform.Angle`（double 自动推断） |

**编译成功 ≠ 能启动**：样式附加期与动画解析异常发生在运行时。样式改动后必须实际启动冒烟。

## 决策入口

- **选弹层**：按钮附带小菜单 → `Button.Flyout`；右键菜单 → `ContextMenu`；菜单项列表 → `MenuFlyout`；模态确认对话框 → 项目既有 Dialog 方案（如 Ursa `OverlayDialogHost`）。注意：Transient 弹层在窗口失活时的首击会「打开即被光熄灭关闭」（references/windows-flyouts-popups.md）。
- **选列表控件**：只读展示 → `ItemsControl`；可选中 → `ListBox`；大数据量 → 必须配 `VirtualizingStackPanel`（默认 ItemsPanel **不虚拟化**）。
- **选绑定写法**：同 DataContext 内普通路径绑定；跨层级取值 `$parent[Window].((vm:X)DataContext)`；模板内先设 `x:DataType`。
- **写动画**：先查 references/animation.md 的「属性类型 → Animator 对照表」，Transform 类动画用 `RotateTransform.Angle` 安全写法。

## references 导航

下表是提炼后的速查；**超出覆盖范围或与源码疑有不符的细节，一律回 `D:\SourceCode\avalonias` 按上节方式查证，不猜**。

| 文件 | 何时读 |
|---|---|
| `references/version-and-setup.md` | 建项目、加包、配置 App.axaml 与主题 |
| `references/styling-and-tokens.md` | 写样式 / 选择器 / 伪类 / ControlTheme / 资源 token |
| `references/bindings-and-mvvm.md` | 绑定报错、写 ViewModel、消息通信 |
| `references/windows-flyouts-popups.md` | 窗口自绘、Flyout/Popup 行为与陷阱、剪贴板、文件对话框 |
| `references/animation.md` | 写 `Style.Animations`，Animator 选型 |
| `references/controls-catalog.md` | 选控件、查伪类与易错默认值 |
| `references/layout-and-virtualization.md` | 布局面板、对齐、列表虚拟化 |
| `references/startup-crash-checklist.md` | 编译过但启动崩 / 运行崩 |
| `references/logging-and-crash-report.md` | 接全局异常挂点、崩溃落盘、日志分层（**新桌面项目必读**） |
| `references/testing-and-performance.md` | 界面回归测试、性能基线 |

## 代码生成优先级

```text
项目既有样式类 / token / 既有视图组件（复用优先）
        ↓
Avalonia 官方控件 + ControlTheme
        ↓
主题库能力（Semi 资源 / Ursa 控件）
        ↓
自定义 UserControl（确实有复用价值才抽象）
        ↓
自定义绘制
```

## 禁止事项

- 禁止把 WPF / WinUI / MAUI 的 API 套用到 Avalonia：`Trigger`（不存在）、`Visibility` 枚举（Avalonia 是 bool `IsVisible`）、`RelativeSource AncestorType`（用 `$parent[Type]`）、`x:Bind`（不存在）、VSM（用伪类 + ControlTheme）、`Microsoft.Win32.OpenFileDialog`（用 `StorageProvider`）
- 禁止凭 11.x 经验写剪贴板代码：12.x 的 `IClipboard` 接口**没有** `SetTextAsync` 接口成员，走 `ClipboardExtensions`（命名空间 `Avalonia.Input.Platform`）或 `SetValueAsync(DataFormat.Text, text)`
- 禁止样式内联硬编码颜色 / 间距 / 圆角 / 阴影；禁止把标量 token 赋给 Thickness 属性；禁止 BoxShadow 带单位
- 禁止在 DataTemplate 漏写 `x:DataType`（编译绑定默认开启时直接编译失败）
- 禁止用普通 StackPanel 作大列表的 ItemsPanel 还期望虚拟化

## 版本记录

```text
Avalonia Version:        12.1.2（消费工程 csproj；源码 tag 12.1.2 逐一核实）
Semi.Avalonia Version:   12.1.0.1
Ursa.Avalonia Version:   2.2.0（Irihi.Ursa / Irihi.Ursa.Themes.Semi）
CommunityToolkit.Mvvm:   以消费工程 csproj 为准
Skill Generated:         2026-09-13
事实来源：D:\SourceCode\avalonias\{Avalonia,Semi.Avalonia,Ursa.Avalonia} + 消费工程 D:\Work\github\ApixPress
```
