---
rule_id: ui-agents
version: 1.35.0
last_updated: 2026-09-02
dependencies: [agents-root]
---

# 界面规则

<!-- include: shared/ui/scope-web.md -->

<!-- include: shared/ui/common-tasks-web.md -->

---

## 技术栈

### Blazor Server（界面层）
- .NET + Blazor Server（SignalR 实时通信）；版本以仓库现有 `TargetFramework` 为准，不主动升级，新项目先联网检索当前最新正式版
- 组件库在 BootstrapBlazor（基于 Bootstrap 5）与 MudBlazor（Material Design）之间以项目实际引用为准，看包引用与 `_Imports.razor` / `App.razor` 的 using 和组件前缀确认；新项目未定库时先向用户确认再写代码，不得混用两套组件
- 图标库随组件库：BootstrapBlazor 配 Bootstrap Icons，MudBlazor 配 MudBlazor 内置 Icon（Material Icons）
- Blazor 内置路由管理
- Blazor 状态管理（`CascadingValue` / `CascadingParameter` / Scoped 服务）

### 设计系统
<!-- include: shared/ui/design-system-liveness.md -->
- 使用所选组件库的主题系统，所有视觉规范在 `design-system.yaml` 中定义（基线以 BootstrapBlazor 语义书写；项目用 MudBlazor 时组件映射与 CSS 变量按 `mudblazor` skill 或官方文档换算，色彩 / 间距 token 语义仍适用）
- 优先使用所选组件库的组件和样式，禁止 inline style，禁止硬编码颜色 / 间距 / 圆角
- 组件库的具体参数与用法不在本规则展开：环境提供 `bootstrap-blazor` 或 `mudblazor` skill 时优先遵循（按项目实际选库对应者），否则以项目锁定版本的官方文档为准；禁止从非所选组件库借参数或样式类

<!-- include: shared/ui/spec-lock.md -->

<!-- include: shared/application/tech-choice.md -->

---

<!-- include: shared/frontend/advisory.md -->
---

## 推荐目录结构

若仓库尚未形成稳定结构，可优先参考以下组织方式；若仓库已有实现，以现状为准，不强制迁移。

```text
src/
└── YourProject.Web/
    ├── Components/
    │   ├── Layout/
    │   │   ├── MainLayout.razor
    │   │   ├── NavMenu.razor
    │   │   └── LoginLayout.razor
    │   ├── Common/
    │   └── Pages/
    ├── Models/
    │   ├── DTOs/
    │   └── Enums/
    ├── Services/
    │   └── Interfaces/
    ├── wwwroot/
    │   ├── css/
    │   │   ├── app.css
    │   │   └── theme.css
    │   └── js/
    ├── Program.cs
    └── appsettings.json
```

---

## 阶段 1 — 界面与交互实现

**触发条件**：用户发出「开始开发」或「开始界面开发」指令

**入场要求**：阶段 0 设计文档已由用户确认；原型已经用户确认，或本次改动命中原型跳过条件

HTML 原型即视觉基线，实现需逐页对照原型。

<!-- include: shared/ui/prototype-workflow.md -->

**工作内容**：
1. 按设计文档实现 Razor 页面、布局、导航和组件，遵循 `design-system.yaml` 与所选组件库规范。
2. 补齐页面的 `loading`、`empty`、`error`、`no-permission` 等关键状态。
3. 先搭建完整可交互的页面骨架，再逐步接入服务层能力。
4. 需要共享状态时，优先通过 Scoped 服务或 `CascadingValue` 管理。

**门控规则**：
- 主要页面和关键交互已可演示。
- 页面状态完整，且与设计文档保持一致。
- 视觉验收按「视觉验收」小节执行，用户终审通过前不得进入阶段 2。
- 满足以上三点后，才允许进入阶段 2。

<!-- include: shared/ui/visual-acceptance.md -->

---

## Blazor 界面规则

### Razor 组件规范
- 页面组件使用 `@page` 指令定义路由。
- 组件代码使用 `@code { }` 块，复杂逻辑提取到 `@inject` 的服务中。
- 组件参数使用 `[Parameter]` 特性标注。
- 事件回调使用 `[Parameter] public EventCallback<T> OnXxx { get; set; }`。
- 组件命名使用 PascalCase，文件名与类名一致。

### 组件通信规范
- 父传子：使用 `[Parameter]` 属性，只读不可修改。
- 子传父：使用 `EventCallback<T>`。
- 跨级传值：使用 `CascadingValue` / `CascadingParameter`。
- 跨页面 / 用户会话状态：使用 Scoped 服务注入（Blazor Server 中随 Circuit 存活）；跨用户全局配置：使用 Singleton 服务。
- 避免直接修改参数，需要双向绑定时使用 `@bind-Value`。

### 路由管理规范
- 路由通过 `@page` 指令在页面组件顶部声明。
- 路由参数使用 `@page "/users/{id:int}"`。
- 导航使用 `NavigationManager.NavigateTo()`。
- 路由守卫通过 `AuthorizeView` 和 `[Authorize]` 特性实现。
- 路由命名使用有意义的路径（如 `/users/list`、`/users/detail/{id}`）。

### 状态管理规范
- 组件内状态：`@code` 块中的局部变量和 `@bind` 双向绑定。
- 页面间状态：Scoped 服务（如 `UserStateService`，每用户独立）。
- 全局共享状态（跨用户）：Singleton 服务（如全局配置、缓存）。
- 修改共享状态必须通过服务方法，禁止直接修改。
- 异步操作使用 `async Task` 方法，UI 更新通过 `StateHasChanged()` 或 `InvokeAsync(StateHasChanged)`。

### 样式规则
- 优先使用所选组件库的内置组件和样式 class。
- 自定义样式使用 CSS 变量和 `wwwroot/css/app.css`。
- 禁止 inline style，禁止硬编码颜色值（如 `#1A90FF`）。
- 主题覆盖统一在 `wwwroot/css/theme.css`。
<!-- include: shared/ui/blazor-scoped-css.md -->

### 组件规则
<!-- include: shared/ui/blazor-component-parameters.md -->
- 优先复用 `Components/` 下已有组件，禁止重复创建。
- 只有确实有复用价值时才新增共享组件，避免为单次需求过度抽象。
- 页面状态必须完整：`loading`、`empty`、`error`、`no-permission`；组件名随所选组件库取对应实现（如 MudBlazor 用 `MudProgressLinear` / `MudAlert` 表达）
- 除非仓库已在使用，否则不要引入新的组件库或样式体系；已定库的项目不得再引入第二套组件库

### 服务调用规则
- 页面组件通过 `@inject` 注入服务。
- 服务层封装业务逻辑，组件层只负责 UI 交互。
- 异步操作使用 `await`，禁止同步阻塞。
- 服务方法命名规范：`GetXxxAsync`、`CreateXxxAsync`、`UpdateXxxAsync`、`DeleteXxxAsync`。

---

## 测试规则

<!-- include: shared/ui/pre-commit-regression-web.md -->

<!-- include: shared/ui/browser-walkthrough.md -->

<!-- include: shared/common/general-requirements.md -->

### Blazor 组件测试
- 使用 bUnit 进行组件测试（如需要）。
- 测试组件渲染输出、用户交互、参数传递。
- 至少关注关键页面状态和核心交互结果。

### 界面验证
- 对关键页面执行 smoke test，确认页面可访问、主要交互可触发、核心状态可展示。
- 对登录、菜单导航、关键表单和列表页优先补验证。

<!-- include: shared/external-dependencies.md -->

<!-- include: shared/no-test-fallback.md -->

---
