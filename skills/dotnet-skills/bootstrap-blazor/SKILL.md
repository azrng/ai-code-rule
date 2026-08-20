---
name: bootstrap-blazor
version: 10.9.4
last_updated: 2026-08-20
description: Build, review, or refactor Blazor UI using the BootstrapBlazor component library (v10.9.2). Use when the project references the `BootstrapBlazor` NuGet package and the task involves building pages, CRUD tables (server-side paging/search/edit), forms (ValidateForm/EditorForm), feedback (Dialog/Toast/Swal/Modal/Drawer), or admin layouts (Layout/Menu/Tab). Covers real component tag names, service injection by concrete class, the `BootstrapBlazorRoot` host requirement, and the `OnQueryAsync -> QueryData<T>` server query pattern. Also use to audit code for hallucinated APIs (wrong param names, params borrowed from MudBlazor/Radzen/AntDesign Blazor).
---

# BootstrapBlazor (v10.9.2)

用真实的 BootstrapBlazor 组件库 API 构建 Blazor 页面——优先使用其组件而非手写 HTML/JS，绝不臆造可能属于其它 Blazor UI 库的 API。

本 skill 的全部 API 事实提取自 BootstrapBlazor **10.9.2** 源码树（`D:/SourceCode/blazor/bootstrap/BootstrapBlazor`），并与官方 `BootstrapBlazor.Server` 示例及真实 RBAC 项目（`rbac-blazor`，使用 10.6.0）交叉核对。参考文件中每个参数都标注 `源码文件:行号`。

## 适用场景

- 项目引用了 `<PackageReference Include="BootstrapBlazor" ... />`（查 `.csproj` / `Directory.Packages.props`）。
- 正在添加应使用 BootstrapBlazor（而非原生 `<table>` / Bootstrap CSS / JS）的页面、表格、表单、弹窗、Toast 或后台布局。
- 正在审查/审计 Blazor markup 中臆造或版本不符的 BootstrapBlazor API。

## 核心原则

1. **优先用 BootstrapBlazor 组件。** 已有 `<Table>`、`<Dialog>`、`<Modal>`、`<Toast>` 能力的，直接用——不要用原生 HTML、Bootstrap CSS 类或 JavaScript 重造。
2. **绝不臆造 API。** BootstrapBlazor 不是 MudBlazor / Radzen / Ant Design Blazor / Blazorise。参数名、服务方法名、标签名都不同。拿不准 → 先读 [references/anti-hallucination.md](references/anti-hallucination.md) 或源码。
3. **严格对齐 10.9.2 API。** 常见陷阱：`ShowAsync` 不存在（是 `Show`）；表格分页是 `IsPagination`（非 `Pageable`）；多选是 `IsMultipleSelect`（非 `ShowRowCheckbox`）；文本输入是 `<BootstrapInput>` / `<BootstrapInputNumber>`（带前缀）。
4. **遵循项目既有约定。** 仓库已用 Modal 子组件 + `@ref` 模式做编辑弹窗（如 rbac-blazor）时，照着用，不要换成 `DialogService`。

## 图标资源

不要假设 BootstrapBlazor bundle 自带 Font Awesome、Bootstrap Icons 或其它独立图标字体。使用原生 `<i>` 图标类前，先检查宿主页的 CSS 引用和发布后的字体资源。项目选定 Bootstrap Icons 时，必须在 `App.razor` 加载版本固定的本地 CSS/字体，并统一使用 `bi bi-*`；不得与 `fa-*` 混用。BootstrapBlazor 组件参数仅在已核实该参数支持时使用 `IconName`，否则传入项目中已加载图标库的 CSS 类名。

## UI 审查模式

做 UI/交互审查时，先读 [references/anti-hallucination.md](references/anti-hallucination.md)，再读 [references/ui-audit.md](references/ui-audit.md)。只报告有 markup、代码、配置或渲染页面证据支撑的发现；区分 API 缺陷与产品/设计建议。

用此模式捕捉编译能过、但让后台系统不可靠的问题：未校验的持久化表单、可重复写入、缺失 loading/空/错误三态、无确认的破坏性操作、键盘不可达的自定义导航、破损的响应式布局。若项目刻意使用满足同样行为与可访问性标准的自定义实现，不强制要求换成 BootstrapBlazor 组件。

## 前置条件（缺一项则所有页面失效）

任何 BootstrapBlazor 页面能工作之前，宿主必须先配置好。见 [references/setup.md](references/setup.md)。

- `builder.Services.AddBootstrapBlazor();` —— 一行注册全部弹窗/工具服务（命名空间 `Microsoft.Extensions.DependencyInjection`）。
- **交互 Layout 内放 `<BootstrapBlazorRoot>`** —— 放在 `MainLayout.razor` 内包裹 `@Body`，**不是**放 `App.razor` 包 `<Routes>`。弹窗组件（Modal/Toast/Dialog/Swal/Drawer）的 DOM 通过 `<BootstrapBlazorRootContent>` portal 到它，因此它必须与页面处于同一交互 circuit。放错位置 → 弹窗永不显示，且 `Show()` **不抛任何异常**（JS 静默失败；最难排查的 bug）。两个相关陷阱（不能给它加 `@rendermode`；`ChildContent` 不可序列化）见 [references/setup.md](references/setup.md) 与 [references/anti-hallucination.md](references/anti-hallucination.md)。
- `App.razor` 内的静态资源：`_content/BootstrapBlazor/css/bootstrap.blazor.bundle.min.css` + `_content/BootstrapBlazor/js/bootstrap.blazor.bundle.min.js`。
- `_Imports.razor` 有 `@using BootstrapBlazor.Components`。
- `UseBootstrapBlazor()` 中间件来自**独立的** `BootstrapBlazor.Middleware` 包——核心库不依赖它。

## 服务——注入具体类，方法是 `Show`

没有 `IXxxService` 接口，注入具体类：

```razor
@inject DialogService DialogService
@inject ToastService ToastService
@inject SwalService SwalService
@inject MessageService MessageService
@inject DrawerService DrawerService
```

| 服务 | 核心方法 | 快捷扩展 |
|---|---|---|
| `DialogService` | `Show(DialogOption)` | `Show<T>()`、`ShowEditDialog<T>()`、`ShowModal()`→`Task<DialogResult>` |
| `ToastService` | `Show(ToastOption)` | `Success/Error/Information/Warning(title,content)`（**无 `Question`**） |
| `SwalService` | `Show(SwalOption)` | `ShowModal()`→`Task<bool>`（确认框） |
| `MessageService` | `Show(MessageOption)` | （无） |
| `DrawerService` | `Show(DrawerOption)` | （无） |
| `NotificationService` | `Dispatch(NotificationItem)` | 浏览器原生通知，非页面内弹窗 |
| `FullScreenService` | `Toggle(...)` | （无 Open/Close） |

完整服务清单 + 选项类：[references/di-services.md](references/di-services.md)。

## 代码生成优先级（从高到低）

```text
BootstrapBlazor 推荐模式（来自 Samples / rbac-blazor）
        ↓
BootstrapBlazor 组件 + 其参数
        ↓
Bootstrap 工具类 / CSS
        ↓
自定义 Razor 组件
        ↓
自定义 HTML/CSS
        ↓
JavaScript   ← 最后手段；几乎不需要
```

## 组件速选

| 需求 | 用法 |
|---|---|
| 数据表格 | `<Table TItem>` —— `Items`（客户端）或 `OnQueryAsync`（服务端） |
| 带校验表单 | `<ValidateForm Model OnValidSubmit>` |
| 自动生成表单 | `<ValidateForm>` 内嵌 `<EditorForm TModel>` |
| 文本输入 | `<BootstrapInput @bind-Value>` / `<BootstrapInputNumber>` / `<Textarea>` |
| 下拉 | `<Select @bind-Value Items>`；多选 → `<MultiSelect>`；强类型项 → `<SelectGeneric>` |
| 复选组 | `<CheckboxList @bind-Value Items>` |
| 开关 | `<Switch @bind-Value OnText OffText>` |
| 日期 | `<DateTimePicker @bind-Value>` |
| 成功/错误 Toast | `ToastService.Success(title, content)` |
| 删除确认 | 行内 `<TableCellPopConfirmButton>`（首选）或 `SwalService.ShowModal()` |
| 编辑弹窗 | `<Modal><ModalDialog>` 子组件 + `@ref`（首选）或 `DialogService.Show<T>()` |
| 抽屉面板 | `<Drawer @bind-IsOpen>` 或 `DrawerService.Show()` |
| 后台主壳 | `<Layout>` 的 `Header/Side/Main/Footer` + `Menus` + `UseTabSet` |

标签名坑：`<BootstrapInput>`、`<BootstrapInputNumber>`、`<BootstrapInputGroup>` 带 `Bootstrap` 前缀；`<Select>`、`<Switch>`、`<Checkbox>`、`<CheckboxList>`、`<RadioList>`、`<DateTimePicker>`、`<Cascader>`、`<AutoComplete>`、`<AutoFill>`、`<ValidateForm>`、`<EditorForm>`、`<Textarea>` **不带**。

## CRUD 页面配方

被要求"建一个用户管理页"时，按此结构生成（源自 rbac-blazor）。完整代码：[references/crud-recipes.md](references/crud-recipes.md) 与 [examples/](examples/)。

```text
页面（Index.razor + .razor.cs code-behind）
  ├─ 工具栏：<Card><BodyTemplate> 按钮组 </BodyTemplate></Card>
  ├─ <Table TItem OnQueryAsync IsPagination IsMultipleSelect ShowSearch>
  │     ├─ <TableColumns>...<TableColumn @bind-Field>...</TableColumns>
  │     ├─ <CustomerSearchTemplate> 搜索字段 </CustomerSearchTemplate>
  │     └─ <RowButtonTemplate> 编辑 + <TableCellPopConfirmButton> 删除 </RowButtonTemplate>
  ├─ <Add @ref OnSaveCallback/>   ← Modal 子组件
  └─ <Edit @ref OnSaveCallback/>  ← Modal 子组件
```

- 服务端查询：`OnQueryAsync(QueryPageOptions) → Task<QueryData<TItem>>`，返回 `{ Items, TotalCount, IsFiltered, IsSorted, IsSearch, IsAdvanceSearch }`。见 [references/table.md](references/table.md)。
- 反馈：`ToastService.Success("消息通知", "操作成功")` / `.Error(...)`。
- 刷新表格：`await TableRef.QueryAsync();`。
- code-behind：遵循仓库既有约定。新建或持续成长的组件，`.razor` markup + `.razor.cs` partial 类是有用的默认；已存在的紧凑 `@code` 组件保持原样，除非拆分能改善可读性或可测试性。

## 参考文档

只加载与任务匹配的参考文件——默认不要全部加载。

- [references/setup.md](references/setup.md) —— 安装、`AddBootstrapBlazor`、`BootstrapBlazorRoot`、静态资源、`Program.cs` / `Startup.cs` 模板。
- [references/anti-hallucination.md](references/anti-hallucination.md) —— **★ 错误 vs 正确 API 对照表**。写或审查任何 markup 前先读它。
- [references/ui-audit.md](references/ui-audit.md) —— 仅审查用的清单：表单/状态/反馈、导航可访问性、响应式、BootstrapBlazor 运行时接线。
- [references/table.md](references/table.md) —— `<Table TItem>` 深度 API：数据源、列、排序、搜索、分页、选择、编辑/增删改、工具栏、模板、服务端查询。
- [references/forms.md](references/forms.md) —— `ValidateForm` / `EditorForm` 与每个表单控件的真实参数。
- [references/feedback.md](references/feedback.md) —— Dialog / Toast / Swal / Message / Modal / Drawer / Tooltip / Popover。
- [references/layout.md](references/layout.md) —— `Layout` / `Menu` / `Tab` / `Breadcrumb` / `Pagination` / `Step` / `Card` / `Row`。
- [references/components.md](references/components.md) —— 其它高频组件（Upload / TreeView / Validate / Badge / Tag / Avatar / Spinner / Skeleton）。
- [references/di-services.md](references/di-services.md) —— 完整服务注册表 + 选项类。
- [references/crud-recipes.md](references/crud-recipes.md) —— 四个标准配方（列表 / 表单 / 布局 / 反馈）及骨架。
- examples/：`BasicPage.razor`、`Login.razor`、`CrudTable.razor`、`FormModal.razor`、`AdminLayout.razor`。

## 禁止事项

- 不得臆造组件、参数、`EventCallback`、服务或扩展方法。未在源码/参考文件确认的，先搜源码树（`D:/SourceCode/blazor/bootstrap/BootstrapBlazor/src/BootstrapBlazor/Components/`）。
- 不得从 MudBlazor / Radzen / Ant Design Blazor / DevExpress / Telerik / Blazorise 移植 API。
- 不得在 `<Table>` 上同时设 `Items` 与 `OnQueryAsync`——二者互斥，同设抛异常。
- 不得从 markup 绑定 `SortName`/`SortOrder`（非 `[Parameter]`）；初始排序用列的 `DefaultSort`/`DefaultSortOrder`。
- 不得漏放 `<BootstrapBlazorRoot>`——否则每个弹窗服务调用都会抛异常。
- 不得写 `ShowAsync`/`ShowModalAsync`——方法名是 `Show`（存在 `ShowModal` 等扩展，不存在 `ShowAsync`）。

## 版本与来源

```text
BootstrapBlazor 版本 : 10.9.2   （源码 HEAD，多目标 net6–net10）
Skill 生成时间       : 2026-08-13
API 来源             : D:/SourceCode/blazor/bootstrap/BootstrapBlazor (src/BootstrapBlazor/Components/*)
交叉核对             : BootstrapBlazor.Server 官方 Samples + rbac-blazor (10.6.0)
构建验证             : examples/ 针对 BootstrapBlazor 10.9.2 + .NET 10.0.400 Blazor Server
                       编译通过（0 警告、0 错误）。覆盖高频 API 面——
                       ValidateForm/BootstrapInput(+/Number)/Textarea/Select(+Options/SelectOption)/
                       CheckboxList/Switch/DateTimePicker, Table(OnQueryAsync, IsPagination, IsMultipleSelect,
                       TableColumn, RowButtonTemplate, TableCellButton/TableCellPopConfirmButton), Modal+
                       ModalDialog, Card/Row/Divider/Badge/Spinner/Progress, Layout/Menu/Logout/ErrorLogger,
                       ToastService.Success/Error, QueryData<T>/QueryPageOptions/SortOrder.
                       参考文件中超出示例范围的参数逐行取自 10.9.2 源码树（均带 文件:行号），
                       但并非全部经编译验证——依赖前先核实。
```

复现构建验证：`dotnet new blazor --interactivity Server`、`dotnet add package BootstrapBlazor --version 10.9.2`，加 `AddBootstrapBlazor()` + `<BootstrapBlazorRoot>` + 两个 bundle 静态资源，放入 `examples/` 文件后 `dotnet build`。

若源码升级，重扫 `Components/` 树并更新参考文件；参数名（拼写、能否 `@bind`、类型）是稳定信号，行号会漂移。
