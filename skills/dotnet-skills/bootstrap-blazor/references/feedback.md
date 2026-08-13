# 反馈与弹窗参考

反馈分两类：**服务**（代码调用 `Show`，由 `<BootstrapBlazorRoot>` 渲染）和**声明式组件**（如 `<Modal>`/`<Drawer>`）。

> 服务全部**无接口**，注入具体类；方法名是 `Show`（**无 `ShowAsync`**）。弹窗服务继承 `BootstrapServiceBase<TOption>`（`Services/BootstrapServiceBase.cs`），未配 `<BootstrapBlazorRoot>` 调用即抛异常。

## 服务清单

| 服务 | 方法 | 选项类 | 来源 |
|---|---|---|---|
| `DialogService` | `Show(DialogOption, Dialog?)` | `DialogOption` | `Dialog/DialogService.cs:20` |
| `ToastService` | `Show(ToastOption, ToastContainer?)` | `ToastOption` | `Toast/ToastService.cs:21` |
| `MessageService` | `Show(MessageOption, Message?)` | `MessageOption` | `Message/MessageService.cs:23` |
| `SwalService` | `Show(SwalOption, SweetAlert?)` | `SwalOption` | `SweetAlert/SwalService.cs:23` |
| `DrawerService` | `Show(DrawerOption)` | `DrawerOption` | `Drawer/DrawerService.cs:19` |
| `NotificationService` | `Dispatch(NotificationItem)` | `NotificationItem` | `Services/NotificationService.cs:55` |
| `FullScreenService` | `Toggle(FullScreenOption?)` | `FullScreenOption` | `Services/FullScreenService.cs:23` |

## ToastService（最常用）

快捷扩展（`Extensions/ToastServiceExtensions.cs`）：

```csharp
await ToastService.Success("消息通知", "操作成功");           // :22
await ToastService.Error("消息通知", result.Message);         // :50
await ToastService.Warning("消息通知", "请选择记录");         // :78  (无 Question)
await ToastService.Information("消息通知", "提示");           // :106
// 重载：(title, content, autoHide) / (title, content, autoHide, showClose)
```

`ToastOption`（`Components/Toast/ToastOption.cs`，继承 `PopupOptionBase`）：

| 属性 | 类型 | 说明 | 来源 |
|---|---|---|---|
| `Category` | `ToastCategory` | Success/Information/Error/Warning | `:24` |
| `Title` | `string?` | 标题 | `:54` |
| `Content` | `string?` | 内容（继承 PopupOptionBase:18） | `PopupOptionBase.cs:18` |
| `IsAutoHide` | `bool` | 自动隐藏（默认 true） | `PopupOptionBase.cs:24` |
| `Delay` | `int` | 延迟 ms（默认 4000） | `PopupOptionBase.cs:30` |
| `PreventDuplicates` | `bool` | 防重复 | — |
| `ChildContent` / `ShowClose` / `ShowHeader` | — | — | `:60,66,72` |
| `OnCloseAsync` | `Func<Task>?` | 关闭回调 | `:96` |

自定义：`await ToastService.Show(new ToastOption { Category = ToastCategory.Error, Title = "消息通知", Content = msg, Delay = 3000 });`

## SwalService（SweetAlert 确认框）

```csharp
// 确认框（阻塞，返回 bool）— Extensions/SwalExtensions.cs:21
var op = new SwalOption {
    Category = SwalCategory.Question, Title = "确认删除？", Content = "不可恢复",
    ConfirmButtonText = "删除", CancelButtonText = "取消"
};
if (await SwalService.ShowModal(op)) { /* 已确认 */ }

// 普通提示
await SwalService.Show(new SwalOption { Category = SwalCategory.Success, Title = "成功" });
```

`SwalOption`（`SweetAlert/SwalOption.cs`，构造强制 `IsAutoHide=false`）：`IsConfirm`(`:31`)、`Category`(`:37`, Success/Error/Warning/Information/Question)、`Title`(`:43`)、`Content`、`BodyTemplate`(`:49`)、`Footer`/`FooterTemplate`(`:56,62`)、`ConfirmButtonText`/`CancelButtonText`/`CloseButtonText`(`:104,110,98`)、`OnConfirmAsync`/`OnCloseAsync`(`:128,122`)。

> rbac 实战**不用 Swal 做删除确认**，而是 Table 行内 `<TableCellPopConfirmButton>`（更轻）。见 [crud-recipes.md](crud-recipes.md)。

## DialogService

```csharp
// 泛型扩展 — Extensions/DialogServiceExtensions.cs:36
await DialogService.Show<UserDetail>("用户详情",
    new Dictionary<string, object?> { ["UserId"] = 123 });

// 完整 Option
var op = new DialogOption {
    Title = "用户详情", ShowFooter = false, IsBackdrop = true,
    Component = BootstrapDynamicComponent.CreateComponent<UserDetail>(
        new Dictionary<string, object?> { ["UserId"] = 123 })
};
await DialogService.Show(op);
```

`DialogOption`（`Dialog/DialogOption.cs`）常用：`Title`(`:24`)、`Size`(`:36`, 默认 ExtraExtraLarge)、`IsCentered`(`:54`)、`IsKeyboard`(`:90`, ESC)、`IsBackdrop`(`:96`)、`IsDraggable`(`:214`)、`ShowMaximizeButton`(`:48`)、`ShowResize`(`:66`)、`ShowFooter`(`:102`)、`ShowCloseButton`(`:72`)、`BodyContext`(`:132`)、`Component`(`:162`)、`BodyTemplate`/`HeaderTemplate`/`FooterTemplate`(`:138,150,144`)、`OnSaveAsync`(`:180`, 返回 true 触发关闭)、`OnCloseAsync`/`OnClosingAsync`/`OnShownAsync`(`:197,202,220`)。关闭：`await op.CloseDialogAsync()`（`:250`，无 `IDialogReference`）。

扩展方法：`ShowSearchDialog<T>()`(`:64`)、`ShowEditDialog<T>()`(`:117`)、`ShowModal<T>()`→`Task<DialogResult>`(`:139`)、`ShowModal(title,content)`→`Task<DialogResult>`(`:202`)、`ShowSaveDialog<T>()`(`:269`)、`ShowValidateFormDialog<T>()`(`:347`)。`DialogResult` 枚举：`Unset/Yes/No/Close`。

> rbac 实战**不用 DialogService 做编辑弹窗**，而是 `<Modal>` 子组件 + `@ref`（类型安全、易调试）。见下。

## MessageService / DrawerService / NotificationService / FullScreenService

```csharp
// Message（无快捷扩展）
await MessageService.Show(new MessageOption { Color = Color.Danger, Content = "操作失败", ShowDismiss = true, IsAutoHide = false });

// Drawer
await DrawerService.Show(new DrawerOption { Placement = Placement.Right, Width = "500px", IsBackdrop = true,
    Component = BootstrapDynamicComponent.CreateComponent<FilterPanel>() });

// Notification（浏览器原生通知，需先 CheckPermission）
if (await NotificationService.CheckPermission())
    await NotificationService.Dispatch(new NotificationItem { Title = "新消息", Message = "..." });

// FullScreen
await FullScreenService.Toggle();                                     // 整页
await FullScreenService.Toggle(new FullScreenOption { Id = "chart" }); // 指定元素
```

`MessageOption`：`Color`(`:18`, 默认 Primary)、`ShowDismiss`(`:24`)、`Icon`(`:30`)、`ChildContent`(`:60`)、`ShowMode`(`:66`)。
`DrawerOption`：`Width`/`Height`(`:24,30`)、`Placement`(`:60`, 默认 Left)、`IsBackdrop`(`:42`)、`ShowBackdrop`(`:48`)、`AllowResize`(`:78`)、`ChildContent`/`Component`(`:66,72`)、`BodyContext`(`:96`)、`OnCloseAsync`(`:90`)、`CloseAsync()`(`:114`)。
`NotificationItem`：`Title`/`Message`/`Icon`/`Silent`/`Sound`/`OnClick`(`:26,32,38,44,50,58`)。
`FullScreenOption`：`Element`/`Id`/`Selector`(`:18,24,30`)。

## 声明式组件

### `<Modal>` + `<ModalDialog>`（rbac 编辑弹窗首选）

`Modal` 是外壳容器（`Modal/Modal.razor.cs:14`），`ModalDialog` 是内容（`ModalDialog.razor.cs:14`，通过 `[CascadingParameter] Modal` 关联）。

**Modal**：`IsBackdrop`(`:41`)、`IsKeyboard`(`:48`, 默认 true)、`IsFade`(`:55`)、`ChildContent`(`:62`)、`OnShownAsync`/`OnCloseAsync`/`OnClosingAsync`(`:76,82,88`)。方法 `Show()`(`:227`)、`Close()`(`:237`)、`Toggle()`(`:217`)、`SetHeaderText(string)`(`:251`)。

**ModalDialog**：`Title`(`:45`)、`Size`(`:66`, 默认 ExtraExtraLarge)、`FullScreenSize`(`:77`)、`IsCentered`(`:84`)、`IsDraggable`(`:105`)、`ShowMaximizeButton`(`:116`)、`ShowCloseButton`(`:123`)、`ShowHeader`/`ShowFooter`(`:201,208`)、`BodyTemplate`/`HeaderTemplate`/`FooterTemplate`/`HeaderToolbarTemplate`(`:229,250,236,222`)、`OnSaveAsync`(`:257`)、`IsAutoCloseAfterSave`(`:264`)、`SaveButtonText`/`CloseButtonText`(`:287,271`)、`BodyContext`(`:215`)。

```razor
<Modal @ref="_modal" IsBackdrop="true">
    <ModalDialog Title="编辑" Size="Size.Large" IsDraggable="true" OnSaveAsync="OnSave">
        <BodyTemplate>
            <ValidateForm Model="_model"> ... </ValidateForm>
        </BodyTemplate>
    </ModalDialog>
</Modal>
<Button OnClick="@(() => _modal.Show())">打开</Button>
```

子组件封装模式（rbac）：父页面 `@ref` 调 `Show()`，子组件 `EventCallback` 回调，提交后 `Close()`。完整见 [crud-recipes.md](crud-recipes.md) 配方 2。

> **自定义表单 Modal 的三个坑**（详见 [anti-hallucination.md](anti-hallucination.md) §运行时陷阱）：① `ShowCloseButton` 默认 true，会与自定义"取消"按钮**重复**——自定义 footer 时设 `ShowCloseButton="false"`（`FooterTemplate` 是追加不是替代）；② 保存按钮放 `FooterTemplate`（`<ValidateForm>` 之外）时 `Type="ButtonType.Submit"` **不触发** `OnValidSubmit`，改用 `OnClick` + `<ValidateForm @ref="_form">` 的 `await _form.ValidateAsync()` 手动校验；③ `Modal.Show()` 是纯 JS interop，依赖 `<BootstrapBlazorRoot>` 放在交互 Layout 内，否则静默不显示。正确写法见 [examples/FormModal.razor](../examples/FormModal.razor)。

### `<Drawer>`（`Drawer/Drawer.razor.cs`）
`IsOpen`(`:57`, @bind)、`Placement`(`:92`, 默认 Left)、`Width`(`:43`, 默认 360px)、`Height`(`:50`)、`IsBackdrop`/`ShowBackdrop`(`:78,85`)、`IsKeyboard`(`:141`)、`AllowResize`(`:113`)、`ChildContent`/`BodyContext`(`:106,134`)、`OnCloseAsync`/`OnClickBackdrop`(`:127,71`)、`IsLazyLoadChildContent`(`:156`)。方法 `Close()`(`:227`)。

### `<Tooltip>`（`Tooltip/Tooltip.razor.cs`）
`Title`(`:57`)、`Placement`(`:82`, 默认 Top)、`ChildContent`(`:115`)、`IsHtml`(`:70`)、`Sanitize`(`:76`, 默认 true)、`Trigger`(`:108`, click/focus/hover/manual 可组合)、`Delay`/`Selector`/`Offset`/`CustomClass`/`FallbackPlacements`(`:45,51,96,102,89`)。方法 `Show/Hide/Toggle`(`:165,172,179`)。

### `<Popover>`（`Popover/Popover.razor.cs`，继承 Tooltip）
新增 `Content`(`:18`)、`Template`(`:32`)、`ShowShadow`(`:25`)。区别：Popover 标题+正文两段（Title+Content），Tooltip 单段（Title）。
