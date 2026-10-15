---
name: mudblazor
description: "MudBlazor 组件库开发指南。在 .NET Blazor 项目中使用 MudBlazor（Material Design）的最佳实践与代码模板，锚定 v9（9.11.x）源码验证。Keywords: MudBlazor, Blazor, .NET, DataGrid, MudDataGrid, Dialog, Snackbar, MudForm, Layout, Theme, DarkMode, Drawer, Table, CRUD."
---

# MudBlazor 组件库开发指南

MudBlazor 开发指南，提供在 .NET Blazor 项目中使用 MudBlazor 组件库的最佳实践、代码模板和常见模式。所有示例基于 **MudBlazor 9.11.x**（源码 commit 326bf8c8d，2026-10-14 dev）编译验证。

> 版本锚定：生成代码前先读目标项目 `.csproj` 的 `<PackageReference Include="MudBlazor" Version="..." />` 确认版本。v9 相对 v8 有大量 API 变更（同步 `Show()` 移除、`Validate()` 过时改 `ValidateAsync()`、`MudFormItem` 移除、Chip 重组进 ChipSet、Select 移除 `SelectedItem` 等）；遇到与本文不符的编译错误，以目标版本的官方文档为准，不要把 dev 分支新 API 套到旧版本项目。

**适用场景**：
- 使用 MudBlazor 构建 Blazor Web App（Server / Auto / WebAssembly 交互模式）
- 后台管理系统：Layout + Drawer 导航 + DataGrid CRUD + Dialog 表单
- MudBlazor 组件参数、绑定方式、服务用法问题排查

**不适用场景**：
- 非 MudBlazor 的 Blazor UI 框架（BootstrapBlazor / Ant Design Blazor / Radzen 等 API 互不通用）
- 纯前端 React/Vue 项目

## 按需查阅

本文件只做路由，按当前任务读对应参考文档即可，不必全部加载。

| 任务场景 | 参考文档 |
| --- | --- |
| 新建项目（dotnet new mudblazor）、包安装、DI 注册、Provider 挂载、静态资源 | `references/project-setup.md` |
| Layout 骨架、MudTheme 自定义、暗色模式切换、断点响应、Stack/Grid 栅格 | `references/layout-and-theme.md` |
| 高频组件参数速查：Button/TextField/Select/Autocomplete/日期选择/Chip/Alert/Menu/List 等 | `references/components.md` |
| MudDataGrid：列体系、服务端分页、行内编辑、过滤排序 API、与 MudTable 取舍 | `references/data-grid.md` |
| MudForm 校验（For/Validation）、Dialog 服务、Snackbar 通知、MessageBox | `references/forms-and-validation.md` |
| 完整页面参考：仪表盘、CRUD 页、对话框表单 | `examples/dashboard.razor`、`examples/crud-page.razor`、`examples/dialog-form.razor` |

典型组合：CRUD 页面按 `data-grid.md` → `forms-and-validation.md` → `crud-page.razor` 顺序查阅；新建项目先读 `project-setup.md`；整体视觉/导航用 `layout-and-theme.md`。

## 高频陷阱速览

- **组件命名必须带 `Mud` 前缀**：`<MudButton>` 不是 `<Button>`；混入其他 UI 库标签（`<Table>`、`<Alert>`）是 BootstrapBlazor/Blazorise 经验污染
- 按钮/输入外观没有 `Fill` 参数：一律 `Variant` 枚举（`Text` / `Filled` / `Outlined`）
- `MudTextField` 默认 Enter/失焦才更新绑定值：搜索框需 `Immediate="true"` 或 `DebounceInterval="300"`
- `MudSelect` 单选绑 `@bind-Value`、多选绑 `@bind-SelectedValues` + `MultiSelection="true"`；**不存在 `SelectedItem` 参数**
- `MudDataGrid` 的 `Items` / `ServerData` / `VirtualizeServerData` 三者互斥，同时设置抛 `InvalidOperationException`；`ServerData` 签名是双参 `(GridState<T> state, CancellationToken token)`，过滤排序需自己执行
- `TemplateColumn` 内容必须包在 `<CellTemplate>` 内（`context.Item` 取数据），直接放子内容触发 MUD0002 分析器警告且不渲染
- Dialog 全部是异步 API：`ShowAsync<T>()` / `ShowMessageBoxAsync()`；**同步 `Show()` 在 v9 已移除**；`MudDialog.Close()` 不会触发表单校验，先 `await _form.ValidateAsync()` 再判断 `IsValid`
- `MudForm.Validate()` 已过时：用 `ValidateAsync()`；MudForm 内禁用 `ButtonType.Submit` 按钮
- Dialog/Snackbar 服务要求布局挂载 `<MudDialogProvider />` `<MudSnackbarProvider />`（+ `MudPopoverProvider`），缺失时 `ShowAsync` 阻塞 5 秒后静默失败；Provider 必须与调用方同一交互渲染模式，静态 SSR 不可用
- Snackbar 去重：全局 `Configuration.PreventDuplicates=true`，同消息显示中被拒显；动作按钮文字是 `SnackbarOptions.Action`（不存在 `CloseButtonText`）
- `MudLayout` 内 Provider 顺序：`MudThemeProvider` / `MudPopoverProvider` / `MudDialogProvider` / `MudSnackbarProvider` 放在 `<MudLayout>` **之上**
- 暗色切换没有 `SetTheme` 方法：`<MudThemeProvider @bind-IsDarkMode="_isDarkMode" />`；`DarkMode` 参数不存在
- `MudTabs` 激活方法是 `ActivatePanelAsync()`（不是 `ActivatePanel`）
- 修改默认抽屉宽度用 `MudTheme.LayoutProperties.DrawerWidthLeft/Right`（默认 240px），不是 `Width` 参数（裸容器时才用）
- `MudHidden Breakpoint="Breakpoint.Lg"` 语义是「LG 及以上不渲染」（可用 `Invert` 反转），不是 CSS 隐藏
- v9 `MudChart` 是泛型组件：`<MudChart T="double" ChartType="..." ChartSeries="..." ChartLabels="..." />`，数据是 `List<ChartSeries<double>>`（`Data = new double[]{...}`）；旧版非泛型 `ChartSeries[]` + `XAxisLabels`/`InputData` 写法已不存在
- `DialogParameters<T>` 的集合初始化器 `{ { x => x.P, v } }` 与逐个 `Add(x => x.P, v)` 都可用（泛型 `Add` 来自 `DialogParametersGeneric.cs`）；真正会失败的是**模型类型不同源**：页面与对话框各自嵌套定义同名 record 时类型标识不一致，lambda 无法转换，回退到 `Add(string, object?)` 报 CS1660（"无法将 lambda 转换为 string"）。模型放独立 `Models/` 文件共用
- 对话框传参模型放独立 Models/ 文件共用；页面注入 `HttpClient` 需在 Program.cs 自行 `AddScoped<HttpClient>()`（Web App 模板不自动提供）

## API 查询规则

不确定某个 API 是否存在时，按顺序确认后再生成代码：

1. 查本文 `references/` 对应文档
2. 查官方文档示例（https://mudblazor.com/components/）
3. 查源码 `src/MudBlazor/Components/<组件>/` 的 `[Parameter]` 定义与 `<remarks>` 默认值
4. 编译验证：`dotnet build`（Windows 禁止 `&&` 链式命令）

已知不存在的高频幻觉 API（v9 源码 0 命中，写了必编译失败或静默失效）：

| 幻觉 API | 真实替代 |
| --- | --- |
| `MudDataGrid` 的 `RowsPerPage`（在 `<MudDataGrid>` 上） | 网格参数 `RowsPerPage` 存在但需 `PagerContent` 放 `MudDataGridPager` 才生效；分页选项在 Pager 上 |
| `RowDoubleClick` / `HideHeaders` / `LoadGroupsCount` / 列 `DataStyle` | `RowClick` / `Hideable` / — / `RowStyleFunc` 或列 `CellStyle` |
| `MudTable` 的 `QuickFilter` / `PagerParams` | `Filter`（委托）；分页经 `PagerContent` + `MudTablePager` |
| `MudThemeProvider DarkMode=` / `SetTheme()` | `@bind-IsDarkMode` / 换 `Theme` 参数实例 |
| `MudSelect` 的 `SelectedItem` | `@bind-Value`（单选）/ `@bind-SelectedValues`（多选） |
| `MudFileUpload` 的 `FileTypes` | `Accept=".pdf,.png"` |
| `ChipSet` 的 `MultiSelection="true"` 布尔 | `SelectionMode="SelectionMode.MultiSelection"` 枚举 |
| `MudFormItem` / `Spacers`（MudForm 上） | 不存在；间距用 `Spacing`，嵌套直接用 `MudForm` |
| Snackbar 的 `CloseButtonText` / `RenderMode` / `MaximumDuplicatedSnackbars` | `SnackbarOptions.Action` / `SnackbarVariant` / `PreventDuplicates`+`key` |
| `MudMessageBox` 同步 `Show()` | `ShowAsync()` 返回 `bool?` |

## 代码生成原则

生成 MudBlazor 页面时的优先顺序：

```text
MudBlazor 官方推荐模式（官方模板 + Docs 示例）
        ↓
MudBlazor 组件与参数（版本匹配目标项目）
        ↓
MudBlazor 服务（IDialogService / ISnackbar）
        ↓
MudBlazor 布局与 utility class（mud-* 类）
        ↓
自定义 Razor 组件
        ↓
自定义 HTML/CSS
        ↓
JavaScript interop
```

- 颜色一律走主题（`Color.Primary` 等）或 CSS 变量 `--mud-palette-*`，禁止硬编码色值
- 参数绑定用官方推荐方式；不要通过 `@ref` 改子组件参数
- 组件职责单一，页面过大时拆分为子组件 + `DialogParameters` 传参

## 参考资源

- [MudBlazor 官方文档](https://mudblazor.com/)
- [MudBlazor GitHub](https://github.com/MudBlazor/MudBlazor)（AGENTS.md 含官方 AI 编码规范）
- [MudBlazor.Templates](https://github.com/MudBlazor/Templates)（`dotnet new mudblazor` 项目模板）
