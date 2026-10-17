---
rule_id: ui-agents
version: 1.34.0
last_updated: 2026-09-02
dependencies: [agents-root]
---

# 界面规则

## 适用范围

- 作用域：页面、布局、导航、样式、交互、移动端适配、界面测试
- 触发场景：涉及 ContentPage、Shell 导航、布局、控件、交互、样式、移动端适配、UI smoke test 时阅读

### 常见任务入口
- 新增页面或布局：先看组件规则与推荐目录结构
- 改表单、弹窗、导航、交互：先看状态管理规则与导航规则
- 改样式、主题、视觉展示：先看样式规则与设计系统
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
<!-- include: shared/ui/design-system-liveness.md -->
- 使用 `design-system.yaml` 中定义的设计 token
- 样式必须使用 MAUI ResourceDictionary（`.xaml`），禁止把颜色、间距、圆角直接硬编码在控件属性中
- 颜色、间距、圆角必须来自样式资源，使用 `StaticResource` 或 `DynamicResource` 引用

<!-- include: shared/ui/spec-lock.md -->

<!-- include: shared/application/tech-choice.md -->

---

<!-- include: shared/ui/advisory.md -->
- 设计体系基线：MAUI 样式资源与各平台设计指南
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

## 阶段 1 — 视图实现（界面实现角色主导）

**触发条件**：用户发出「开始视图开发」指令

**入场要求**：阶段 0 设计文档已由用户确认；原型已经用户确认，或本次改动命中原型跳过条件

移动端涉及复杂手势、动画、转场或平台原生行为时，优先用 MAUI 搭建最小可运行原型验证交互；交互简单时允许降级为 HTML 原型。

<!-- include: shared/ui/prototype-workflow.md -->

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
- 视觉验收按「视觉验收」小节执行，用户终审通过前不得进入阶段 2。
- 满足以上三点后，才允许进入阶段 2。

---

<!-- include: shared/ui/visual-acceptance.md -->

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
- 安全区域处理：iOS 默认遵循安全区域，需调整时使用 `ios:Page.UseSafeArea` 平台特定设置（或新版 `SafeAreaEdges` 按边控制）；Android 使用系统边衬区（WindowInsets）适配
- 列表滚动使用 `CollectionView` 优先，避免 `ListView` 已知性能问题
- 图片资源按平台提供合适分辨率，优先使用 SVG 矢量图
- 下拉刷新使用 Syncfusion PullToRefresh 控件
- 手势操作（滑动删除、捏合缩放）使用 MAUI 内置手势识别器
- 键盘遮挡输入框时使用 `SoftInputExtensions` 自动调整

### 组件规则
- 优先复用 `src/AppName/Controls/` 下已有控件，禁止重复创建
<!-- include: shared/ui/component-rules-core.md -->
- 优先使用 Syncfusion.Maui.Toolkit 控件，不满足需求时再使用 MAUI 内置控件或自定义控件
- 图标使用 MAUI 内置图形能力（`FontImageSource`）或项目既有素材

<!-- include: shared/ui/mvvm-rules.md -->

### 导航规则
- 使用 MAUI Shell 导航（`Shell.Current.GoToAsync`）进行页面切换
- 导航路由统一在 `AppShell.xaml.cs` 或 `MauiProgram.cs` 中注册
- 页面参数通过导航查询参数（`ShellNavigationQueryParameters`）或 `IQueryAttributable` 传递，禁止使用静态全局状态
- 需要历史记录时，应支持前进 / 后退
- 模态页面通过目标页面标记 `Shell.PresentationMode="Modal"` 配合 `GoToAsync` 实现，或直接使用 `Shell.Current.Navigation.PushModalAsync`；路由前缀 `//` 仅表示绝对路由，与模态无关
- 深度链接依赖的路由结构在 `AppShell.xaml` 中声明，URI Scheme 等平台级配置在 `AndroidManifest.xml` / `Info.plist` 中维护

### 弹窗规则
- 简单提示使用 `CommunityToolkit.Maui.Alerts`（Snackbar / Toast）
- 复杂弹窗使用 `CommunityToolkit.Maui.Views.Popup` 或 Syncfusion Popup，按项目已有方案选择
- 弹窗内容必须使用 ViewModel，禁止在 Code-behind 编写业务逻辑
- 弹窗结果通过异步返回，禁止依赖隐式全局状态
- 危险操作需提供明确的二次确认

<!-- include: shared/ui/data-binding.md -->
- 使用 `x:DataType` 启用编译时绑定检查，提升性能和类型安全

<!-- include: shared/ui/state-management.md -->
- 页面生命周期事件（`OnNavigatedTo`、`OnNavigatedFrom`）中只做初始化和清理，不承载业务逻辑

### 主题规则
- 默认使用浅色主题，深色主题仅在项目明确要求时扩展
- 若支持深色模式，所有颜色必须通过 `design-system.yaml` 中的语义化 token 引用，禁止在 XAML 或代码中硬编码颜色值
- 主题切换通过 `Application.Current.UserAppTheme` 控制，不自建主题切换机制
- 页面中任何视觉状态（含弹窗、Loading、Error 页面）必须同时验证浅色与深色下的可读性

---

## 测试规则

<!-- include: shared/ui/pre-commit-regression.md -->

<!-- include: shared/ui/general-requirements.md -->

<!-- include: shared/application/view-layer-testing.md -->
- 推荐使用 xUnit + 项目既有 UI 测试方案

<!-- include: shared/desktop/external-dependencies.md -->

<!-- include: shared/no-test-fallback.md -->

---
