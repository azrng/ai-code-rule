---
rule_id: ui-agents
version: 1.0.0
last_updated: 2026-05-02
dependencies: [agents-root]
---

# AGENTS.md

## 适用范围

- 作用域：页面、布局、导航、样式、交互、移动端适配、界面测试
- 触发场景：涉及 ContentPage、Shell 导航、布局、控件、交互、样式、移动端适配、UI smoke test 时阅读

### 阅读摘要
- 建议阅读：新增页面、改导航、改表单交互、改控件样式、补界面验证
- 可先跳过：纯服务层逻辑、纯数据访问、仅文档整理、仅部署配置调整
- 优先查看：界面规则、移动端适配规则、测试规则

### 常见任务入口
- 新增页面或布局：先看界面结构与组件规则
- 改表单、弹窗、导航、交互：先看状态管理与事件处理规则
- 改样式、主题、视觉展示：先看样式规则与设计系统约束
- 改移动端适配、手势、平台特定 UI：先看移动端适配规则
- 补界面回归：先看 `提交前最小回归` 与测试规则

---

## 技术栈

### UI
- .NET MAUI + XAML
- 目标平台：Android、iOS（优先），Windows、macOS（按需）
- UI 控件库：Syncfusion.Maui.Toolkit（开源免费，优先使用其控件）
- MVVM 模式：CommunityToolkit.Mvvm
- 消息传递：`IMessenger`
- 设计风格：Material Design（Android）/ Human Interface Guidelines（iOS），通过 MAUI 跨平台统一

### 设计系统
- 使用 `design-system.yaml` 中定义的设计 token
- 样式必须使用 MAUI ResourceDictionary（`.xaml`），禁止把颜色、间距、圆角直接硬编码在控件属性中
- 颜色、间距、圆角必须来自样式资源，使用 `StaticResource` 或 `DynamicResource` 引用

### 技术选择原则
如果仓库已经有真实实现，以现有代码为准，不要强行重构或替换技术栈。

**技术债务评估框架**：
- **沿用现有实现**：代码可正常运行、无明显性能问题、维护成本可接受、团队熟悉度高
- **必须重构**：存在安全漏洞、严重影响性能、阻碍新功能开发、维护成本过高
- **可选重构**：代码风格不一致、存在更好的替代方案、但不紧急
- **禁止重构**：仅为个人偏好、追赶技术潮流、非关键路径的过度优化

**重构决策流程**：
1. 先评估现有代码的技术债务等级（高 / 中 / 低）。
2. 判断重构的紧急程度和业务价值。
3. 评估重构风险和工作量。
4. 高风险或大规模重构需先与团队确认。

---

## 推荐目录结构

- UI 目录建议聚焦在 `src/AppName/Pages/`、`src/AppName/Controls/`、`src/AppName/ViewModels/`、`src/AppName/Resources/` 下组织，优先复用现有结构，不强制迁移。

```text
src/AppName/
├── Pages/
│   ├── Base/                  # 页面基类
│   ├── Dialogs/               # 弹窗页面
│   └── [Module]/              # 按业务域拆分的页面
├── Controls/                  # 自定义控件
├── Converters/                # 值转换器
├── Resources/
│   ├── Styles/
│   │   ├── Colors.xaml        # 颜色资源
│   │   ├── Fonts.xaml         # 字体资源
│   │   ├── Sizes.xaml         # 间距与尺寸资源
│   │   └── Global.xaml        # 全局样式
│   └── Images/                # 图片资源（SVG 优先）
├── ViewModels/
│   ├── Base/
│   ├── Dialogs/
│   └── [Module]/
├── App.xaml                   # 应用级资源与 Shell 定义
└── AppShell.xaml              # Shell 导航结构
```

---

## 阶段 1 — 视图实现（Claude Code 主导）

**触发条件**：用户发出「开始视图开发」指令

**入场要求**：阶段 0 设计文档已由用户确认

**工作内容**：
1. 按设计文档实现页面和控件，遵循 `design-system.yaml` 和 MAUI 样式规范。
2. 数据层使用 mock（静态 mock 数据），不依赖真实服务。
3. 同步输出接口契约文件 `src/AppName/Models/DTOs/`，定义所有数据传输对象。

**产物**：
- 可运行的 MAUI 页面
- `src/AppName/Models/DTOs/` 契约类

**门控规则**：
- 用户确认页面符合设计文档预期。
- DTO 类中的类型已定稳，不再变动。
- 满足以上两点后，才允许进入阶段 2。

---

## UI 规则

### 样式规则
- 所有样式使用 MAUI ResourceDictionary（`.xaml` 文件），禁止在控件中直接设置 `BackgroundColor`、`Margin` 等样式属性
- 所有颜色来自 `design-system.yaml` 中定义的语义化 token，通过样式资源引用
- 所有间距使用统一的资源或样式类，禁止硬编码数值
- 字体统一在 `Resources/Styles/Fonts.xaml` 中定义，页面引用资源名称

### 移动端适配规则
- 页面布局必须适配不同屏幕尺寸和方向（竖屏 / 横屏）
- 使用 `OnPlatform` 和 `OnIdiom` 标记处理平台差异，差异逻辑收敛到 XAML 资源或布局层，不散落在代码各处
- 触摸目标尺寸最小 44x44（iOS）/ 48x48（Android），关键操作区域需考虑拇指热区
- 安全区域处理：iOS 使用 `SafeArea` 设置，Android 使用系统边衬区
- 列表滚动使用 `CollectionView` 优先，避免 `ListView` 已知性能问题
- 图片资源按平台提供合适分辨率，优先使用 SVG 矢量图
- 下拉刷新使用 Syncfusion PullToRefresh 控件
- 手势操作（滑动删除、捏合缩放）使用 MAUI 内置手势识别器
- 键盘遮挡输入框时使用 `SoftInputExtensions` 自动调整

### 组件规则
- 优先复用 `src/AppName/Controls/` 下已有控件，禁止重复创建
- 只有确实有复用价值时才新增共享控件，避免为单次需求过度抽象
- 页面状态必须完整：`loading`、`empty`、`error`、`no-permission`
- 优先使用 Syncfusion.Maui.Toolkit 控件，不满足需求时再使用 MAUI 内置控件或自定义控件
- 图标使用 MAUI 内置图形能力（Font Image Source）或项目既有素材

### MVVM 模式规则
- 所有 ViewModel 最终都必须继承 `ObservableObject`；若项目已有 `BaseViewModel`，应由基类继承 `ObservableObject` 后统一复用
- 属性通知使用 `[ObservableProperty]` 特性自动生成，禁止手写重复样板
- 命令定义使用 `[RelayCommand]` 特性生成，禁止手动拼装重复命令逻辑
- ViewModel 依赖通过构造函数注入
- 禁止在 Code-behind（`.xaml.cs`）中编写业务逻辑

### 导航规则
- 使用 MAUI Shell 导航（`Shell.Current.GoToAsync`）进行页面切换
- 导航路由统一在 `AppShell.xaml.cs` 或 `MauiProgram.cs` 中注册
- 页面参数通过导航查询参数（`ShellNavigationQueryParameters`）或 `IQueryAttributable` 传递，禁止使用静态全局状态
- 需要历史记录时，应支持前进 / 后退
- 模态页面使用 `://` 前缀或 `Shell.Current.Navigation.PushModalAsync`
- 深度链接配置统一在 `AppShell.xaml` 中声明

### 弹窗规则
- 简单提示使用 `CommunityToolkit.Maui.Alerts`（Snackbar / Toast）
- 复杂弹窗使用 `CommunityToolkit.Maui.Views.Popup` 或 Syncfusion Popup，按项目已有方案选择
- 弹窗内容必须使用 ViewModel，禁止在 Code-behind 编写业务逻辑
- 弹窗结果通过异步返回，禁止依赖隐式全局状态
- 危险操作需提供明确的二次确认

### 数据绑定规则
- 列表数据使用 `ObservableCollection<T>`
- 复杂集合变更优先使用批量更新策略，而不是简单清空后重添
- 异步数据加载必须支持取消（`CancellationToken`）
- 绑定路径必须可维护，禁止依赖脆弱的控件查找方式
- 使用 `x:DataType` 启用编译时绑定检查，提升性能和类型安全

### 状态管理规则
- 本地状态使用 `[ObservableProperty]` 管理
- 全局共享状态使用 `IMessenger` 传递跨 ViewModel 消息
- 禁止使用静态全局类存储业务状态
- 主题、语言等低频全局配置才放入应用级资源或全局上下文
- 页面生命周期事件（`OnNavigatedTo`、`OnNavigatedFrom`）中只做初始化和清理，不承载业务逻辑

### 主题规则
- 默认使用浅色主题，深色主题仅在项目明确要求时扩展
- 若支持深色模式，所有颜色必须通过 `design-system.yaml` 中的语义化 token 引用，禁止在 XAML 或代码中硬编码颜色值
- 主题切换通过 `Application.Current.UserAppTheme` 控制，不自建主题切换机制
- 页面中任何视觉状态（含弹窗、Loading、Error 页面）必须同时验证浅色与深色下的可读性

---

## 测试规则

### 提交前最小回归
- 默认执行：项目现有的编译、静态检查或等价校验
- 页面、导航、表单交互改动：至少补一次受影响界面或组件的 smoke test / 等价验证
- **微小调整**（文案修改、颜色调整、间距优化等不影响逻辑的改动，修改内容少于10行）：可不补自动化测试，但仍应确认受影响界面的关键状态、布局与主要交互未回退
- 仅样式或视觉改动：至少确认受影响界面的关键状态、布局与主要交互未回退
- 若影响共享控件、布局或状态流转，优先验证影响范围最大的界面，而不是只看局部控件

### 总体要求
- 影响行为的改动应优先补充或更新测试
- 若本次改动未补测试，必须在最终说明中写明原因和风险
- 测试应覆盖真实业务行为，不要只验证静态渲染

### 视图层测试
- 页面交互、表单校验、列表行为、状态展示、异常状态变化时，应补充对应测试
- 至少关注以下关键状态：`loading`、`empty`、`error`、`no-permission`
- 若涉及数据请求、筛选、提交等关键路径，应验证主要交互结果
- 推荐使用 xUnit + 项目既有 UI 测试方案

### 外部依赖与数据
- 测试中不要真实调用外部服务，统一使用 mock、stub 或测试替身
- 测试数据应尽量最小化、可读、可重复执行
- 不要让测试依赖本地人工状态或不可控外部环境

### 无法执行测试时
- 必须说明未执行的测试类型
- 必须说明未执行原因
- 必须说明潜在影响范围和风险

---

文件结束。
