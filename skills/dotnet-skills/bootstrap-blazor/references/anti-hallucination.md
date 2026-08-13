# 防幻觉 API 速查表（写代码/审查前先读）

BootstrapBlazor 的 API 与 MudBlazor / Radzen / Ant Design Blazor / Blazorise 差异极大，且与多数 AI 训练记忆不符。本表列出**最常被写错**的 API，每条给出错误写法、10.9.2 正确写法、来源 `文件:行号`。

> 基线版本 10.9.2。源码根：`D:/SourceCode/blazor/bootstrap/BootstrapBlazor/src/BootstrapBlazor/`。下文路径相对此根。

## 黄金法则（5 条）

1. **服务无接口**，注入具体类：`@inject DialogService`（不是 `IDialogService`）。`BootstrapBlazorServiceCollectionExtensions.cs:109-125`。
2. **弹窗方法是 `Show`，不是 `ShowAsync`**。`DialogService.cs:20`、`ToastService.cs:21`、`SwalService.cs:23`、`MessageService.cs:23`、`DrawerService.cs:19`。
3. **必须放 `<BootstrapBlazorRoot>`**，否则任何弹窗服务调用抛 `InvalidOperationException`。`Services/BootstrapServiceBase.cs:31-39`。
4. **`Items` 与 `OnQueryAsync` 在 `<Table>` 上互斥**，同设抛异常。`Table.razor.cs:1038-1041`。
5. **表单文本输入带 `Bootstrap` 前缀**：`<BootstrapInput>`、`<BootstrapInputNumber>`、`<BootstrapInputGroup>`；其余表单组件（Select/Switch/...）不带。

## 服务层

| 错误 | 正确 | 来源 |
|---|---|---|
| `await DialogService.ShowAsync(op)` | `await DialogService.Show(op)` | `Components/Dialog/DialogService.cs:20` |
| `var ref = await DialogService.Show<T>(...)` 拿引用 | 无 `IDialogReference`；关闭用 `DialogOption.CloseDialogAsync()` | `Components/Dialog/DialogOption.cs:250` |
| `ToastService.Question(...)` | 不存在；仅 `Success/Error/Information/Warning`（扩展方法） | `Extensions/ToastServiceExtensions.cs:22,50,78,106` |
| `await SwalService.ShowAsync(op)` | `await SwalService.Show(op)`；确认框用扩展 `ShowModal()`→`Task<bool>` | `Components/SweetAlert/SwalService.cs:23`、`Extensions/SwalExtensions.cs:21` |
| `NotificationService.Show(NotificationOption)` | `NotificationService.Dispatch(NotificationItem)`（浏览器原生通知，非页面弹窗） | `Services/NotificationService.cs:55` |
| `FullScreenService.Open()/Close()` | `FullScreenService.Toggle(FullScreenOption?)` | `Services/FullScreenService.cs:23` |
| `@inject IToastService` | `@inject ToastService`（无接口） | — |

`DialogService` 扩展方法（`Extensions/DialogServiceExtensions.cs`）：`Show<T>(title, parameters, dialog)` `:36`、`ShowSearchDialog<T>()` `:64`、`ShowEditDialog<T>()` `:117`、`ShowModal<T>()`→`Task<DialogResult>` `:139`、`ShowModal(title,content)`→`Task<DialogResult>` `:202`、`ShowSaveDialog<T>()` `:269`、`ShowValidateFormDialog<T>()` `:347`。

## Table（`Components/Table/`）

| 错误 | 正确 | 来源 |
|---|---|---|
| `<Table DataSource="@items">` | `<Table TItem="Foo" Items="@items">`（数据参数叫 `Items`） | `Table.razor.cs:643` |
| `Pageable="true"` | `IsPagination="true"` | `Table.razor.Pagination.cs:15` |
| `ShowRowCheckbox="true"` | `IsMultipleSelect="true"`（无 `ShowRowCheckbox` 参数） | `Table.razor.Checkbox.cs:73` |
| `IsMultiple="true"` | `IsMultipleSelect="true"` | `Table.razor.Checkbox.cs:72` |
| `<TableColumn>` 用 markup 设 `SortName="x" SortOrder=Asc` | `SortName`/`SortOrder` 是 `protected`，**非 Parameter**；初始排序用列 `DefaultSort="true" DefaultSortOrder="SortOrder.Asc"` | `Table.razor.Sort.cs:16,22`、`Table.razor.cs:1131` |
| `ReadOnly="true"` | `Readonly="true"`（拼写） | `TableColumn.cs:177` |
| `FormatterAsync="..."` | `Formatter: Func<object?, Task<string?>>`（已异步，无 `FormatterAsync`） | `TableColumn.cs:309` |
| `<TableToolbar>` 当 Table 的参数 | `<TableToolbar TItem>` 是**独立组件**（放 Table 外）；Table 上的工具栏是 `ShowToolbar` + `ToolbarTemplate` | `TableToolbar.razor.cs:17` |
| `OnPageClick="..."` | 不存在该 Parameter；翻页由组件内部处理 | `Table.razor.Pagination.cs:177` |
| 服务端查询类型 `QueryPageableOptions` | `QueryPageOptions` | `Options/QueryPageOptions.cs:15` |
| 同时设 `Items` 和 `OnQueryAsync` | 二选一，互斥（抛异常） | `Table.razor.cs:1038-1041` |
| `AutoRefresh="true"` | `IsAutoRefresh="true"` | `Table.razor.cs:753` |

列声明：`<TableColumns><TableColumn @bind-Field="context.Prop" Text=".." />`（`Table.razor.cs:623`）。`@bind-Field` 走 `Field`+`FieldExpression`（`TableColumn.cs:47,55`）。

服务端查询签名：`Func<QueryPageOptions, Task<QueryData<TItem>>>`（`Table.razor.Pagination.cs:59`）。返回 `QueryData<TItem>{ Items, TotalCount, IsFiltered, IsSorted, IsSearch, IsAdvanceSearch }`（`Misc/QueryData.cs`）。详见 [table.md](table.md)。

## 表单组件标签名（最易错）

| 错误标签 | 正确标签 | 来源 |
|---|---|---|
| `<Input @bind-Value>` | **`<BootstrapInput @bind-Value>`** | `Components/Input/BootstrapInput.razor.cs` |
| `<InputNumber @bind-Value>` | **`<BootstrapInputNumber @bind-Value>`** | `Components/InputNumber/BootstrapInputNumber.razor.cs` |
| `<InputGroup>` / `<InputGroupLabel>` | `<BootstrapInputGroup>` / `<BootstrapInputGroupLabel>` | `Components/Input/BootstrapInputGroup.razor.cs` |
| `<TextBox>` / `<TextField>` | `<BootstrapInput>` | — |
| `<NumericEdit>` / `<NumberEdit>` | `<BootstrapInputNumber>` | — |

**不带前缀**的正确标签：`<Select>`、`<MultiSelect>`、`<SelectGeneric>`、`<Cascader>`、`<Checkbox>`、`<CheckboxList>`、`<RadioList>`、`<Switch>`、`<DateTimePicker>`、`<DateTimeRange>`、`<TimePicker>`、`<AutoComplete>`、`<AutoFill>`、`<Rate>`、`<Slider>`、`<Segmented>`、`<Textarea>`、`<ValidateForm>`、`<EditorForm>`、`<Upload>`、`<Transfer>`、`<QueryBuilder>`、`<Mask>`。

## 表单组件参数

| 错误 | 正确 | 来源 |
|---|---|---|
| `<Select Category=.. IsMultiple="true">` | 无 `Category`、无 `IsMultiple`；多选用 `<MultiSelect>` | `Components/Select/Select.razor.cs` |
| `<Select>` 只能用 `Items` | 也可声明式 `<Options><SelectOption Text=".." Value=".."/>` | `Select.razor.cs:70`、`SelectOption.cs:19` |
| `Select` 的 `Items` 是 `string[]` | `Items: IEnumerable<SelectedItem>`（`SelectedItem(value, text)`） | `Misc/SelectedItem.cs:34,40` |
| `<Cascader ShowFullPath Items=SelectedItem>` | `ShowFullLevels`；`Items: IEnumerable<CascaderItem>` | `Components/Cascader/Cascader.razor.cs:55,76` |
| `<DateTimePicker DisplayFormat="yyyy-MM-dd">` | `DateTimeFormat` / `DateFormat` / `TimeFormat` | `DateTimePicker.razor.cs:113,121,129` |
| `<Textarea Rows="5">` | Textarea **无 `Rows` 参数**（`Rows` 仅在 `IEditorItem` 上供 EditorForm 自动生成） | `Components/Textarea/Textarea.razor.cs` |
| `<Switch OnLabel OffLabel>` | `OnText` / `OffText`（或 `OnInnerText`/`OffInnerText`） | `Components/Toggle/ToggleBase.cs:35,43` |
| `<Input UseInputEvent="true">` | `UseInputEvent` 仅在 `Textarea`/`InputNumber` 上（Input 继承 `BootstrapInputBase`，无此参数） | `BootstrapInputEventBase.cs:23` |

绑定统一 `@bind-Value`（基于 `Value`+`ValueChanged`+`ValueExpression`，`DisplayBase.cs:68,75,82`）。详见 [forms.md](forms.md)。

## ValidateForm / EditorForm / EditorItem

| 错误 | 正确 | 来源 |
|---|---|---|
| `<ValidateForm OnSubmit=..>` | 无 `OnSubmit`；只有 `OnValidSubmit` / `OnInvalidSubmit`（`Func<EditContext, Task>`） | `ValidateForm.razor.cs:26,33` |
| `<ValidateForm ValidateAll CustomerValidator=..>` | `ValidateAllProperties`；无 `CustomerValidator` 参数（自定义校验走组件 `ValidateRules` 或模型 `IValidatableObject`） | `ValidateForm.razor.cs:55`、`ValidateBase.cs:417` |
| `form.Validate()` | `await form.ValidateAsync()`（`Validate()` 已废弃） | `ValidateForm.razor.cs:677,683` |
| `<EditorItem FieldName=.. Visible=..>` | 用 `Field`/`FieldExpression`；可见性用 `Ignore`（无 `FieldName`/`Visible`） | `EditorItem.cs:22,36,56` |
| `<EditorForm>` 不传 Model | 嵌套在 `<ValidateForm Model>` 内时会自动捕获级联 EditContext | `EditorForm.razor.cs:207-215` |

## 反馈与弹窗

| 错误 | 正确 | 来源 |
|---|---|---|
| `<Modal>` 内直接放 `Title=` `<Body>` | `<Modal>` 是外壳容器；内容放 `<ModalDialog Title=..><BodyTemplate>` | `Modal.razor.cs:14`、`ModalDialog.razor.cs:14` |
| `await Modal.ShowAsync()` | `Modal.Show()` / `Modal.Close()` / `Modal.Toggle()`（实例方法，非 service） | `Modal.razor.cs:217,227,237` |
| `<Drawer IsResizeable=..>` | `AllowResize` | `Components/Drawer/Drawer.razor.cs:113` |
| `<Tooltip>` 只能 hover | `Trigger` 可组合 `click/focus/hover/manual` | `Tooltip.razor.cs:108` |
| `Card Header=".." Body=".." Footer=".."` | `HeaderTemplate` / `BodyTemplate` / `FooterTemplate`（标题文本 `HeaderText`） | `Components/Card/Card.razor.cs:35,42,49,56` |

`<TableCellPopConfirmButton>`（Table 行内删除确认，rbac 实战首选，非 Swal）、`<TableCellButton>`（行内操作按钮）。

## 布局组件

| 错误 | 正确 | 来源 |
|---|---|---|
| `<Layout SideWith=.. Logo=.. LogoText=.. Data=.. IsAutoMatch=..>` | `SideWidth`；无 `Logo`/`LogoText`/`Data`/`IsAutoMatch`（Logo 在 `Header` 模板自渲染） | `Layout.razor.cs:223` |
| `<Layout Header=.. >` 传字符串 | `Header`/`Side`/`Main`/`Footer` 是 `RenderFragment` 模板 | `Layout.razor.cs:143,150,164,231` |
| `MenuItem.Children` | `MenuItem.Items` | `Components/Menu/MenuItem.cs:26` |
| `MenuItem` 构造复杂 | `new MenuItem(text, url?, icon?)`；子项设 `.Items` | `MenuItem.cs:74` |
| `<Tab OnClick=.. IsDraggable=..>` | `OnClickTabItemAsync`；`AllowDrag` | `Tab.razor.cs:250,345` |
| `<Tab Items=..>` 绑定 | `Items` 只读；添加用 `ChildContent` 内 `<TabItem>` 或 `AddTab()` | `Tab.razor.cs:63,894` |
| `<Pagination @bind-PageIndex OnPageClick=.. ShowTotal=.. PageLinkTemplate=..>` | `PageIndex` 不可 `@bind`（无 Changed）；`OnPageLinkClick`；`ShowPageInfo`；`PageInfoTemplate` | `Pagination.razor.cs:90,112,152,159` |
| `<Breadcrumb Items=.. ChildContent=.. OnClick=..>` | 唯一参数 `Value`（`IEnumerable<BreadcrumbItem>`） | `Breadcrumb.razor.cs:20` |
| `<Step Steps=.. OnClick=..>`；`StepItem.IsCompleted` | `Items`；`OnFinishedCallback`；无 `IsCompleted`（靠 `StepIndex` 索引） | `Step.razor.cs:20,55` |
| `<Row><Col Span=..></Col></Row>` | **`Col` 组件不存在**；`<Row ItemsPerRow="ItemsPerRow.Three">` 由 JS 自动套 `col-*` | `Components/Row/Row.razor.cs:19` |

## 展示组件

| 错误 | 正确 | 来源 |
|---|---|---|
| `<Avatar Src=..>` | `Url` | `Components/Avatar/Avatar.razor.cs:45` |
| `<Spinner Type=..>` | `SpinnerType`（Border/Grow） | `Components/Spinner/Spinner.razor.cs:44` |
| `<Alert IsShow=.. IsDismissable=.. Title=..>` | 无 `IsShow`/`Title`；`ShowDismiss` + `OnDismiss` | `Components/Alert/Alert.razor.cs` |
| `<Tag IsClose=.. OnClose=..>` | `ShowDismiss` + `OnDismiss`（继承 AlertBase） | `AlertBase.cs:34,62` |
| `<Badge>` 当 `<Tag>` | Badge 是徽标；`Color`(默认 Primary)、`IsPill`、`ChildContent` | `Components/Badge/Badge.razor.cs:25` |

## 不存在的组件 / 参数汇总

- 组件：`Col`（不存在）、`IXxxService` 接口（不存在）、`LocalizationService`（本地化用 `AddJsonLocalization` + `IStringLocalizer<T>`）。
- 服务方法：`ShowAsync` / `ShowModalAsync`（不存在，用 `Show` / `ShowModal`）。
- Table 参数：`Pageable`、`ShowRowCheckbox`、`OnPageClick`、markup 绑定的 `SortName/SortOrder`。
- 表单参数：`Select.Category`、`Select.IsMultiple`、`ValidateForm.OnSubmit`、`ValidateForm.ValidateAll`、`EditorItem.FieldName`、`EditorItem.Visible`、`DateTimePicker.DisplayFormat`、`Textarea.Rows`、`Cascader.ShowFullPath`。
- Layout 参数：`SideWith`、`Logo`、`LogoText`、`Data`、`IsAutoMatch`、`MenuItem.Children`。

> 拿不准时，去源码目录确认：`Components/<组件名>/<组件名>.razor(.cs)`，搜 `[Parameter]`。

## 运行时配置陷阱（编译能过、运行才暴露 —— 源码分析发现不了）

以下问题不会编译报错，但运行时组件不工作，且排查极难（多数无异常、静默失败）。**真正跑过 Test2（设备 CRUD）才验证出来**：

### 1. BootstrapBlazorRoot 放错位置 → Modal/Toast/Dialog/Swal 全部不显示（无异常）★最坑
- ❌ App.razor 内 `<BootstrapBlazorRoot><Routes @rendermode=.../></BootstrapBlazorRoot>`
- ❌ `<BootstrapBlazorRoot @rendermode="InteractiveServer">`（`ChildContent` 是 `RenderFragment`，跨 rendermode 不可序列化，启动报 `Cannot pass the parameter 'ChildContent' ... cannot be serialized`）
- ✅ 放 **MainLayout 内**包裹 `@Body`（继承 Routes 的交互模式，与页面同 circuit）
- **根因**：弹窗组件 DOM 通过 `<BootstrapBlazorRootContent>` portal 到 `BootstrapBlazorRoot`（`Modal.razor:5`）。BootstrapBlazorRoot 必须与页面在同一交互 circuit；放 App.razor 静态层则 portal 跨边界失效 → `Modal.Show()`（`InvokeVoidAsync("execute", Id, "show")`）的 JS 在 portal 容器里找不到元素，**C# 无异常、JS 静默失败**。现象：Table 能渲染，但任何弹窗 Show 无反应。
- 详见 [setup.md](setup.md)「Layout」段。官方 `BaseLayout.razor:3` 即放 Layout 内。

### 2. 缺 @rendermode → 整个应用退化为静态模式
- App.razor 的 `<HeadOutlet>` 和 `<Routes>` 都要加 `@rendermode="InteractiveServer"`。缺失则无 SignalR，交互组件（Table 数据、按钮 OnClick、Modal）全不工作，只剩静态 HTML。

### 3. Modal 按钮重复 + 保存失效
- `ModalDialog.ShowCloseButton` 默认 **true**，footer 会渲染默认"关闭"按钮；`FooterTemplate` 是**追加**而非替代（`ModalDialog.razor:62-86`）→ 与自定义"取消"重复。自定义 footer 时设 `ShowCloseButton="false"`。
- 保存按钮放 `FooterTemplate`（`<ValidateForm>` 之外）→ `Type="ButtonType.Submit"` **不触发** `OnValidSubmit`（不在 form 内）。改 `OnClick` + `<ValidateForm @ref="_form">` 的 `await _form.ValidateAsync()` 手动校验。正确写法见 [examples/FormModal.razor](../examples/FormModal.razor)。

### 4. `.razor` 文件名必须 PascalCase
- 连字符文件名（`form-modal.razor`）生成的类名非法 → 编译失败。组件用 `FormModal.razor`、`CrudTable.razor`。`@page` 路由随意，但被 `<标签>` 引用的组件文件名必须合法标识符。

### 5. `Console` 二义性（CS0104）
- `@using BootstrapBlazor.Components` 后，`Console` 在 `BootstrapBlazor.Components.Console`（组件）和 `System.Console` 间二义。代码写日志用全限定 `System.Console.WriteLine(...)`。
