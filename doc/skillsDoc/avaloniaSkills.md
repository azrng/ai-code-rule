# Avalonia Agent Skill 制作任务

你是一名资深 .NET / Avalonia UI / 桌面客户端专家。

我已经向你提供了：

1. Avalonia 完整源码：`D:\SourceCode\avalonias\Avalonia`
2. Semi.Avalonia 主题库源码：`D:\SourceCode\avalonias\Semi.Avalonia`
3. Ursa.Avalonia 控件库源码：`D:\SourceCode\avalonias\Ursa.Avalonia`
4. 官方示例库：`D:\SourceCode\avalonias\Avalonia.Samples`（CompleteApps / CustomControls / DataTemplates / DesktopIntegration / Drawing / MVVM / Routing / Testing / ViewInteraction，每例带 README.adoc；Headless 测试样板在 `Testing/TestableApp.Headless.XUnit`，对话框范式在 `ViewInteraction/`）
5. 真实消费工程参照：`D:\Work\github\ApixPress`（Avalonia 12.1.2 + Semi.Avalonia 12.1.0.1 + Ursa.Avalonia + CommunityToolkit.Mvvm，含 `ui-AGENTS.md` 界面规则与 `design-system.yaml` 设计 token 基线）
6. 官方模板与文档（联网检索，仅用于核实版本与 API 存在性）

你的任务不是开发业务功能，而是**分析 Avalonia UI 框架本身，并为 AI Coding Agent 制作一套高质量、可长期维护、与框架版本同步的 Avalonia Agent Skill**。

目标使用环境包括：

* OpenAI Codex
* Claude Code
* Cursor 等支持 Agent Skills / SKILL.md 的编码 Agent

## 范围界定

本 skill 覆盖：

* Avalonia UI 框架核心：XAML 编译与编译绑定、样式系统（Selectors / Classes / ControlTheme）、资源字典与主题变体、布局面板、常用控件目录、窗口与弹层体系（Window / Popup / Flyout）、动画与 Animator、输入与命令、CommunityToolkit.Mvvm 集成、Headless 测试、桌面端常见启动崩溃排查
* 主题与控件库组合：以 Semi.Avalonia（主题）+ Ursa.Avalonia（业务控件）为默认装配基线给出接入指路（与消费工程一致）

不覆盖、按下列方式处理：

* Semi.Avalonia / Ursa.Avalonia 的全量控件 API：**不逐一展开**。skill 只写「装配接入 + 高频控件（Dialog / Overlay / Badge / Loading 等）速查」，控件细节待各自成专项 skill 或以各自源码为准
* Avalonia for iOS / Android / Browser（移动与 Web）：范围外，本 skill 面向 Windows 桌面
* DataGrid / TreeDataGrid 独立包：一句话指路，不展开
* 业务分层规则（ViewModel / Service / Repository 怎么摆）：归洋葱架构 skill，本 skill 只管 UI 层
* design-system.yaml 的具体 token 值：以消费工程的 token 管线为例讲解「token → 资源字典 → 样式 → 控件」引用链，不固化具体颜色值

范围外内容在本 skill 中最多一句话指路，不展开 API。

---

## 一、核心原则

不要根据你已有的 WPF / UWP / WinUI / MAUI 知识直接编写 Skill。

Avalonia 与 WPF 形似而神不同，**最大风险就是把 WPF 经验当 Avalonia 事实**。必须优先以我提供的：

* Avalonia 源码（`src/Avalonia.Controls`、`src/Avalonia.Base`、`src/Avalonia.Styling`、`src/Avalonia.Markup`）
* Semi.Avalonia / Ursa.Avalonia 的主题 axaml 与控件实现
* 消费工程 `D:\Work\github\ApixPress` 的实际写法与踩坑记录
* 各包 csproj 锁定的 NuGet 版本

作为事实来源。

如果你的已有知识与源码冲突，以当前源码为准。

禁止猜测不存在的：

* 样式选择器语法与伪类名（`:pointerover` 不是 `:hover`，`Trigger` 在 Avalonia 样式中不存在）
* 附加属性 / StyledProperty / AttachedProperty 的注册名
* Animator 类型与属性类型的对应关系
* Flyout / Popup / Dialog 的 ShowMode 与配置项
* 命名空间与 NuGet 包 id

---

## 二、第一阶段：分析框架

首先扫描 Avalonia 源码与消费工程，整理整体能力。重点分析：

### 1. 版本矩阵（先做）

| 项 | 来源 | 说明 |
|---|---|---|
| Avalonia | 消费工程 csproj（2026-09：12.1.2） | 版本事实基准，源码仓库按对应 tag 检出再分析 |
| Semi.Avalonia | 消费工程 csproj | 主题库 |
| Ursa.Avalonia | 消费工程 csproj | 控件库 |
| CommunityToolkit.Mvvm | 消费工程 csproj | MVVM 基建 |

版本号必须逐一回 csproj 核实后再写入 Skill。Avalonia 11 与 12 存在破坏性差异（PresentationSource 重构、Obsolete 清理、TopLevel API 变化），skill 必须**锁定单一主版本**，禁止混写两版 API；发现条件编译差异时随 API 一并记录「签名 + 起始版本」。

### 2. 样式系统（重点）

* Selectors 语法全表：类型选择器、`.class` 类选择器、`:pseudo-class` 伪类（`:pointerover` / `:pressed` / `:focus` / `:selected` / `:checked` / `:flyout-open` 等，逐一回源码核实）、组合器（后代空格、直接子代 `>`、`/template/`、`^` 原点）、函数选择器（`$parent`、`nth-child` 等）
* `Styles` vs `ControlTheme`：12 推荐的 ControlTheme 替代隐式样式的场景与写法
* `Classes="A B"` 多类、`Classes.Foo="{Binding}"` 条件类、`iri:ClassHelper.Classes` 这类第三方扩展的取舍
* Setter 值类型严格性：`Padding` 是 Thickness，赋标量资源在**样式附加期**抛 `InvalidCastException`（见第八节易错清单）
* 资源解析时机：`StaticResource`（加载期一次）与 `DynamicResource`（运行时跟随，主题切换必需）的选型规则
* ThemeVariant（Light / Dark）与主题库接入（Semi.Avalonia 的 `App.axaml` 装配方式）

### 3. 布局系统

* Panel 家族：Grid（`ColumnDefinitions="Auto,*,Auto"` 紧凑语法、`Grid.SetColumn`）、StackPanel（`Spacing` 是 Avalonia 独有，WPF 无）、WrapPanel / DockPanel / UniformGrid / Canvas / RelativePanel
* 度量与对齐：`HorizontalAlignment` / `VerticalAlignment` / `MinWidth` / `MaxWidth`、`ClipToBounds` 的真实裁剪边界
* GridSplitter 的行为与约束（消费工程 HttpInterfaceWorkbenchView 有实际用法）
* ScrollViewer 与滚动条占位、虚拟化面板（`ItemsControl` 默认不虚拟化，StackPanel 作 ItemsPanel 时虚拟化失效——核实 VirtualizingStackPanel 的正确配置）

### 4. 控件目录（以 `src/Avalonia.Controls` 源码为准，禁止按 WPF 清单臆造）

Button / ToggleButton / CheckBox / RadioButton / ComboBox / ListBox / ItemsControl / TabControl / TabStrip / TextBox（ watermarks、内嵌按钮）/ MaskableTextBox / AutoCompleteBox / SelectableTextBlock / Slider / ProgressBar / ToggleSwitch / SplitView / Expander / Menu / ContextMenu / TreeView / HeaderedContentControl / Border / Viewbox / Splitter 家族 / WindowChrome 相关。每个控件记录：

* 关键样式类与伪类（控制视觉状态的标准途径）
* 高频属性与易错默认值
* 消费工程中的实际用法示例（引用文件路径）
* 常见错误（如 ComboBox 需要 `SelectedItem` 而非 WPF 习惯的 `SelectedValue` 组合、TreeView 展开绑定 `IsExpanded` 需要双向等——逐项回源码核实后写入）

### 5. 弹层体系（重点，含行为陷阱）

* `Popup` / `PopupRoot` / `OverlayPopupHost` 双宿主模式（窗口化弹层 vs Overlay 内弹层）与切换条件
* `FlyoutBase` 家族：`Flyout` / `MenuFlyout` / `FlyoutShowMode`（`Standard` / `Transient` / `TransientWithDismissOnPointerMoveAway`）
* 光熄灭（Light Dismiss）真实行为：`Popup` 订阅所在 Window 的 `Deactivated` 与平台 `LostFocus`，触发即 `Close()`——**窗口失活时的首次点击会让弹层显示后立即关闭**（消费工程 caret 弹层缺陷的真实根因，源码 `Popup.WindowDeactivated` / `WindowLostFocus` 可证）。skill 必须写清该行为与「补显」处理模式
* `Button.Flyout` 的 toggle 语义（`OnClick` 内 `_isFlyoutOpen` 分支）与 `Placement` 枚举
* ContextMenu、Dialog（消费工程用 Ursa 的 `OverlayDialogHost`）的边界与推荐场景

### 6. 窗口体系

* `Window` / `WindowBase` / `TopLevel` 的职责分层
* 自绘标题栏：`WindowDecorations="None"` + `ExtendClientAreaToDecorationsHint` + `BeginMoveDrag`（消费工程 MainWindowTitleBarView 模式）
* `WindowState` 切换与最大化按钮字形联动
* 文件对话框走 `StorageProvider`（不是 WPF 的 `Microsoft.Win32.OpenFileDialog`）

### 7. 绑定与 MVVM（重点）

* 编译绑定：`AvaloniaUseCompiledBindingsByDefault=true` 下的强制 `x:DataType`、DataTemplate 也要 `x:DataType`、类型不匹配报 `AVLN2000`
* 跨 ViewModel 取值：`$parent[Window].((vm:MainWindowViewModel)DataContext)` 转换语法与转义（花括号开头需 `{}` 转义的场景）
* `ReflectionBinding` 与编译绑定混用的场景（消费工程样式内 `ReflectionBinding IsExpanded` 一例）
* CommunityToolkit.Mvvm：`ObservableObject` 基类、`[ObservableProperty]`、`[RelayCommand]`（含 `AsyncRelayCommand` 与 `CanExecute`）、`IMessenger` 弱消息
* `UpdateSourceTrigger=PropertyChanged` 等 Avalonia 支持面核实

### 8. 动画与 Animator（重点，含崩溃级易错）

* `Style.Animations` / `Animation` / `KeyFrame` / `Cue` 语法
* **属性类型 → Animator 对照表**（必须逐项回源码 `src/Avalonia.Base/Animation/Animators/` 核实）：double → DoubleAnimator、Color → ColorAnimator、BoxShadows → BoxShadowsAnimator、Transform / TransformOperations → TransformOperationsAnimator / TransformAnimator……
* **未匹配 Animator 直接运行时抛 `InvalidOperationException: No animator registered`**。`rotate(0deg)` 这类 TransformOperations 关键帧需要显式 Animator 注册；稳妥替代是 `RenderTransform` 挂 `<RotateTransform />` 后动画 `RotateTransform.Angle`（double 自动推断，对齐 Ursa `Loading.axaml` 官方写法）
* Transitions 与动画的选型边界

### 9. 资源与设计 token 管线

以消费工程为事实样例讲解管线：

```text
design-system.yaml（token 定义）
        ↓
Assets/Styles/Colors.axaml · Spacing.axaml · Shadows.axaml（资源字典，区分类型：SolidColorBrush / x:Double / Thickness / CornerRadius / BoxShadows）
        ↓
Assets/Styles/领域Styles.axaml（Selectors + Setter 引用资源）
        ↓
Views（Classes 挂类，不写内联颜色 / 间距 / 圆角）
```

硬规则：**标量 token（x:Double 的 `Space.*`）与 Thickness token（`Pad.*`）是两种类型**，混用即启动崩溃；BoxShadow 值只接受纯数值（`-24 0 48 0 #80000000`），带 `px` 后缀解析抛 FormatException。

### 10. 测试与性能

* Avalonia.Headless + XUnit 的最小接入（消费工程测试工程尚无 Headless，评估给出推荐配置）
* XAML 结构断言测试模式（消费工程 `WorkbenchLayoutTests` 用 XDocument 断言 Classes 与绑定，作为轻量回归的推荐实践）
* 性能基线：虚拟化、惰性宿主（ContentControl 按需实例化，消费工程 `RequestEditorWorkspaceView` 模式）、集合批量更新（`ReplaceWith` 单次 Reset）、防抖重算

---

## 三、分析消费工程与主题库

不要只分析框架源码。

必须重点分析真实消费工程，因为：

> 真实工程的实际写法优先于 AI 自己推断出来的写法。

重点对象：

* `D:\Work\github\ApixPress\src\ApixPress.App\`：MainWindow 自绘标题栏、Views/Controls 下的领域视图组织、Assets/Styles 的 token 管线、`ui-AGENTS.md` 的 MVVM 与样式硬约束
* `ApixPress.App.Tests`：XAML 断言测试模式
* Semi.Avalonia 源码的控件主题（`Controls/*.axaml`）：学习官方 ControlTheme 写法与伪类用法
* Ursa.Avalonia 源码：Overlay / Dialog / Loading 的官方实现模式（`Loading.axaml` 的 `RotateTransform.Angle` 动画是动画章节的标准样例）

对于重复示例，提取最典型、最值得 AI 学习的模式，不全部复制。

---

## 四、生成 Skill

建议生成以下结构（按分析结论可调整）：

```text
skills/dotnet-skills/avalonia/
├── SKILL.md
│
└── references/
    ├── version-and-setup.md           # 版本矩阵、包引用、App.axaml 与主题接入
    ├── styling-and-tokens.md          # 选择器语法、ControlTheme、资源解析时机、token 管线
    ├── controls-catalog.md            # 常用控件、样式类 / 伪类、易错默认值
    ├── layout-and-virtualization.md   # 布局面板、对齐、虚拟化配置
    ├── bindings-and-mvvm.md           # 编译绑定、x:DataType、$parent 转换、CommunityToolkit.Mvvm
    ├── windows-flyouts-popups.md      # 窗口自绘、Flyout 光熄灭行为、Popup 双宿主
    ├── animation.md                   # 属性类型 → Animator 对照表、安全动画写法
    ├── startup-crash-checklist.md     # 「编译通过但启动崩溃」三类坑与排查法
    └── testing-and-performance.md     # Headless、XAML 断言、性能基线
```

如果根据分析结论有更合理的目录结构，可以调整。

但是：

**不要把所有内容全部塞进 SKILL.md。**

SKILL.md 应负责：

* Skill 的适用范围与触发场景（front matter description 同步维护，带 Keywords）
* AI 工作原则（源码为准、禁止套用 WPF 经验）
* Avalonia 关键约束与「编译过但启动崩」速查
* 如何选择控件与弹层方案
* references 导航
* 代码生成流程
* 禁止事项

详细语法表、Animator 对照全表、控件目录、启动崩溃排查步骤放 references。

修改带 YAML front matter 的文件时，必须同步更新 `version` 与 `last_updated`（格式与 dotnet-skills 其他 skill 一致）。

---

## 五、SKILL.md 必须告诉 AI 的事情

当用户开发 Avalonia 桌面界面时：

* 样式一律走 `Styles` + `Classes`，颜色 / 间距 / 圆角 / 阴影来自资源 token，**不在控件上内联硬编码**；新 token 先加资源字典再引用
* 视觉状态用伪类选择器（`:pointerover` / `:pressed` / `:selected`），**不写 WPF Trigger**；条件视觉用 `Classes.Xxx="{Binding}"` 或伪类
* 每个 UserControl / Window / DataTemplate 写 `x:DataType`；跨 ViewModel 绑定用 `$parent[Window].((vm:Xxx)DataContext)` 转换，**不依赖反射绑定兜底**
* 视图模型继承 ObservableObject，属性用 `[ObservableProperty]`、命令用 `[RelayCommand]`，**不手写 INotifyPropertyChanged 样板**
* 弹层优先 `Button.Flyout` / `MenuFlyout` / `ContextMenu`；确认对话框走项目既有 Dialog 方案（Ursa OverlayDialogHost），**不自己造模态遮罩**
* 列表优先 `ItemsControl` + 正确的虚拟化 ItemsPanel；集合变更用批量替换，**不逐项 Add 清空重灌**
* 动画先查「属性类型 → Animator 对照表」，Transform 动画默认用 `RotateTransform.Angle` 安全写法
* 文件选择走 `StorageProvider`，**不用 Win32 OpenFileDialog**
* 有 Avalonia 12 官方能力（如 TopLevel.OpenedPopups 等新 API，须回源码核实后写入）可替代手写逻辑时，用官方能力

---

## 六、禁止幻觉 API

这是整个 Skill 最重要的规则之一。

当 AI 不确定某个 Avalonia API 时：

1. 搜索 references
2. 搜索 Avalonia / Semi / Ursa 源码
3. 搜索消费工程实际用法
4. 确认签名、默认值与版本

之后才能生成代码。

禁止把以下经验直接套用到 Avalonia：

* WPF：`Trigger` / `DataTrigger` / `Style.Setters` 之外的行为、`Visibility` 枚举（Avalonia 是 bool `IsVisible`）、`MouseOver` 触发器、`RelativeSource AncestorType` 语法（Avalonia 是 `$parent[Type]`）、`x:Static` 之外的 StaticExtension 差异
* WinUI / UWP：`x:Bind`（Avalonia 没有）、VisualState Manager 优先（Avalonia 用伪类 + ControlTheme）
* MAUI：Handler 体系
* 第三方 XAML 框架的附加属性

易错细节必须显式写进 Skill（全部在消费工程实际踩过、可回源码验证）：

* `Padding="{StaticResource Space.2}"` 若 `Space.2` 是 `x:Double` 标量 → **启动崩溃** `InvalidCastException: Setter value '8' is not a valid value for property 'Padding'`；Padding/Margin 必须用 Thickness token
* `BoxShadow` 值 `-24px 0 48px 0 #80000000` 带 `px` 后缀 → 解析抛 `FormatException`（`BoxShadow.Parse` 用 `double.Parse`）；正确写法 `-24 0 48 0 #80000000`
* RenderTransform 动画关键帧写 `rotate(0deg)` 未注册 Animator → 运行时抛 `InvalidOperationException: No animator registered for the property RenderTransform`；安全写法见动画章节
* 无 `x:DataType` 且开启编译绑定 → `AVLN2000` 编译错误；`$parent[Window].((vm:X)DataContext)` 写法漏括号/强转也报 AVLN2000
* `Button.Flyout`（Transient）在窗口失活后的首次点击：弹层打开即被光熄灭关闭，表现为「点击 caret 没有菜单」，需要失活补显或改 `ShowMode`
* `ItemsControl` 用普通 StackPanel 作 ItemsPanel 时虚拟化静默失效

---

## 七、代码生成原则

生成 Avalonia 界面时的优先顺序：

```text
项目既有样式类 / 资源 token / 既有视图组件（复用优先）
        ↓
Avalonia 官方控件 + ControlTheme 能力
        ↓
主题库能力（Semi 主题资源 / Ursa 控件）
        ↓
自定义 UserControl（确实有复用价值才抽象）
        ↓
自定义绘制 / 自定义控件
```

能够用既有能力完成的界面，不重新造轮子；新界面先检索相似实现再动手（消费工程 `ui-AGENTS.md` 同款约束）。

---

## 八、标准模式

研究消费工程后，总结「新增一个功能界面」的标准流程。AI 应能够根据：

> 新增一个「环境变量管理」的右侧抽屉面板。

自动推导：

```text
ViewModel：继承 ViewModelBase(ObservableObject)
    [ObservableProperty] 状态 + [RelayCommand] 动作 + IMessenger 跨 VM 消息
        ↓
View：Views/Controls/XxxView.axaml
    x:DataType 对应 VM；Classes 挂样式类；不写内联颜色
        ↓
样式：Assets/Styles/领域Styles.axaml 增补 Selector + Setter（token 引用）
        ↓
宿主接线：布尔属性控制 IsVisible / ContentControl 惰性宿主
        ↓
对话框类需求：走项目 Dialog 方案 + 异步结果，危险操作二次确认
        ↓
回归：dotnet build + 既有 XAML 断言测试 + 启动冒烟
```

同时保证使用的全部 API 与当前版本源码一致。

---

## 九、版本意识

在 Skill 中明确记录：

```text
Avalonia Version:        12.1.2（逐一回消费工程 csproj 核实）
Semi.Avalonia Version:   12.1.0.1（同上核实）
Ursa.Avalonia Version:   （回 csproj 核实）
Source Commit:           （avalonias 仓库各子仓库当前 commit / 对应 tag）
Skill Generated / Updated:
```

如果框架升级，应能够重新扫描变化并更新 Skill：

* csproj 版本号与 TFM
* 11 → 12 的 API 差异（Obsolete 别名、移除成员、新增 TopLevel / Popup 能力）
* 动画 Animator 家族与伪类集合的变化
* Semi / Ursa 主题选择器与样式类 key 的变化

---

## 十、生成前先输出分析报告

不要立即开始写 SKILL.md。

第一阶段先输出：

```text
Avalonia Skill Plan Report
```

分三类列出：

1. **高频必收**：消费工程高频使用、源码证实的能力（样式管线、编译绑定、Flyout、动画安全写法等）
2. **陷阱必写**：会导致编译失败或启动崩溃的坑（第八节易错清单 + 源码证据）
3. **暂不收录**：移动端、独立包、低频控件的取舍理由

每项标注证据（源码文件路径 / 消费工程文件路径）。

确认清单以后，再开始写 skill 文件。

---

## 十一、当前已知差距（2026-09-13 初步探查）

以下为本次探查发现的候选差距，制作时逐项回源码核实后处理：

1. **WPF 习惯迁移错误集**（最高优先级）：Trigger 不存在、Visibility→IsVisible(bool)、`$parent[Type]` 替代 AncestorType、伪类名 `:pointerover`——全部需要对照表并给正确写法
2. **三类启动崩溃**（消费工程 2026-09-13 一天内实际踩中三类）：Thickness token 误用标量、BoxShadow px 后缀、Transform 动画缺 Animator——每类给「错误信息原文 → 根因源码位置 → 修复写法」三段式
3. **Flyout 光熄灭窗口失活陷阱**：源码 `Popup.cs` 的 `WindowDeactivated` / `WindowLostFocus` / `TopLevelLostPlatformFocus` 路径；自动化测试与窗口切换场景的「补显」模式
4. **编译绑定体系**：`x:DataType` 全覆盖规则、`$parent[Window].((vm:X)DataContext)` 强转语法、`ReflectionBinding` 逃生口、AVLN2000 排查
5. **虚拟化正确配置**：默认 ItemsPanel 不虚拟化的事实与正确写法（回源码核实 12 的虚拟化面板名与用法）
6. **ControlTheme 与 Styles 的选择**：12 的推荐与消费工程现状（领域 Styles.axaml + Classes）的取舍说明
7. **Semi.Avalonia / Ursa 接入**：App.axaml 装配顺序、主题变体、与自定义 token 管线的共存规则
8. **Headless 测试缺失**：消费工程测试工程未接 Avalonia.Headless，给出最小可运行接入样例（含 XUnit 配置）
9. **自绘窗口**：WindowDecorations=None + ExtendClientArea 的完整模式与坑（拖拽、最大化字形、DPI）
10. **动画 Animator 对照表**：逐属性类型回 `Animators/` 目录核实，标注哪些类型没有对应 Animator（避免再踩 RenderTransform 坑）

---

## 十二、最终验收

Skill 制作完成后，用它完成以下测试（可基于消费工程或新建最小验证工程）：

### Test 1

最小应用：Window + x:DataType + ViewModel（[ObservableProperty] / [RelayCommand]）绑定一个按钮与文本，编译并启动成功。

### Test 2

样式与 token：新增一个样式类（含 `:pointerover` 伪类），颜色间距全部引用资源 token，验证不写内联硬编码；故意把 `x:Double` token 赋给 Padding 复现启动崩溃，再用 `startup-crash-checklist.md` 排查修复。

### Test 3

弹层：Button.Flyout（Transient + BottomEdgeAlignedRight）+ MenuFlyout；验证窗口失活后首击的行为并按 skill 的模式处理。

### Test 4

列表：ItemsControl + 虚拟化面板 + ObservableCollection 批量更新 + 空态（loading / empty / error / no-permission 四态对齐 ui-AGENTS.md）。

### Test 5

动画：spinner 圆弧旋转（先给出错误写法确认抛「No animator registered」，再用安全写法修复），编译并启动验证。

检查生成代码是否：

* 使用真实存在的 API，签名与默认值正确
* 可以编译（实际执行 `dotnet build`，Windows 环境下禁止 `&&` 链式命令）
* 可以启动（样式附加期异常在 build 之外，必须实际运行冒烟）
* 没有虚构伪类、附加属性、Animator、配置项
* 没有混入 WPF / WinUI / MAUI 的 API

如果存在问题，修正 Skill，再重新测试。

最终交付：

```text
skills/dotnet-skills/avalonia/
```

完整目录（用户本机 `~\.zcode\skills\avalonia` 的安装副本另行同步，不作为修改目标）。

---

我建议你的工作流是：

```text
Avalonia / Semi / Ursa 源码 + 消费工程 ApixPress + csproj 版本
        ↓
① Repository Exploration（第二阶段清单）
        ↓
② 能力与陷阱清单（样式 / 绑定 / 弹层 / 动画 / 启动崩溃）
        ↓
③ 高频能力深度分析（token 管线 / 编译绑定 / Flyout 行为 / Animator 对照）
        ↓
④ 消费工程最佳实践提取
        ↓
⑤ Plan Report（第十节）
        ↓
⑥ SKILL.md 撰写
        ↓
⑦ references 撰写
        ↓
⑧ dotnet build + 启动冒烟 + 五个 Test
        ↓
⑨ 修复 Skill
```

特别是**「启动冒烟一定要执行」**（Windows 环境下禁止 `&&` 链式命令）。这类 skill 最大的问题不是 AI 不懂 Avalonia，而是很容易写出：

```xml
<Setter Property="Padding" Value="{StaticResource Space.2}" />   <!-- 标量 token 赋 Thickness：启动崩溃 -->
<Setter Property="BoxShadow" Value="-24px 0 48px 0 #80000000" /> <!-- px 后缀：解析 FormatException -->
```

```xml
<Animation Duration="0:0:0.8">
  <KeyFrame Cue="100%">
    <Setter Property="RenderTransform" Value="rotate(360deg)" />  <!-- 缺 Animator：运行时抛异常 -->
  </KeyFrame>
</Animation>
```

这种**看起来非常合理，但运行时才崩**的代码。让 AI 在真实工程上 `dotnet build` 并实际启动运行核对，能把 Skill 质量提高一个档次。

另外，保持 skill 的「源码型 Skill」定位而不是「百科全书型 Skill」：SKILL.md 控制在小规模（适用范围 / 工作原则 / 关键约束 / 速查 / references 索引），详细语法表、Animator 对照全表、控件目录放 `references/`。这样 Agent 不需要每次加载几万字文档。
