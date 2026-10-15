# Form 校验、Dialog 与 Snackbar

事实来源：`src/MudBlazor/Components/{Form,Dialog,Snackbar,MessageBox,TextField}/`、`src/MudBlazor/Base/MudFormComponent.cs`、`src/MudBlazor/Services/Dialog/`、`src/MudBlazor/Interfaces/ISnackbar.cs` + 对应 Docs Examples 与 UnitTests。

## MudForm 校验体系

### 基本模式（官方示例裁剪）

```razor
<MudForm @ref="_form" @bind-IsValid="@_success" @bind-Errors="@_errors" Spacing="2">
    <MudTextField T="string" @bind-Value="_name" Label="用户名"
                  For="() => _name" Required="true" RequiredError="用户名必填" />
    <MudTextField T="string" @bind-Value="_email" Label="邮箱" For="() => _email"
                  Validation="@(new EmailAddressAttribute { ErrorMessage = "邮箱格式不正确" })" />
    <MudButton Variant="Variant.Filled" OnClick="SaveAsync">提交</MudButton>
</MudForm>

@code {
    private MudForm? _form;
    private bool _success;
    private string[] _errors = [];
    private string _name = "", _email = "";

    private async Task SaveAsync()
    {
        if (_form is null) return;
        await _form.ValidateAsync();          // Validate() 已 [Obsolete]
        if (_form.IsValid)
        {
            // 提交逻辑
        }
    }
}
```

### MudForm 参数速查

| 参数 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `IsValid` | bool | true | `@bind-IsValid`；自动聚合嵌套子表单 |
| `IsTouched` | bool | false | 只读参数（setter 为空），只能 `@bind-IsTouched` |
| `Errors` | string[] | [] | `@bind-Errors` 汇总全部校验错误 |
| `Model` | object? | null | 配合子控件 `For` 使用（FluentValidation 模式） |
| `Validation` | object? | null | 表单级默认校验器，**只下发到设置了 `For` 的子控件** |
| `ValidationDelay` | int | 300 (ms) | 校验防抖 |
| `Spacing` | int | 0 | 子输入间距（4px 单位）；**不存在 `Spacers` 参数** |
| `ReadOnly` / `Disabled` | bool | false | 级联到全部子输入 |
| `SuppressImplicitSubmission` | bool | true | 阻止 Enter 隐式提交/关闭父 Dialog |

方法：`ValidateAsync()` / `ResetAsync()`（清值+校验态）/ `ResetValidationAsync()` / `ResetTouched()`。

### 校验三通道（互不依赖）

1. **DataAnnotations 自动通道**：组件自己从 `For` 表达式反射属性上的 `ValidationAttribute`（`[Required]`、`[EmailAddress]`、`[Range]` 等）执行校验，**不需要也不应该再放 `DataAnnotationsValidator`**（那是 EditForm 体系的）
2. **`Validation` 参数通道**（object?，支持签名分发）：
   - `Func<T,bool>`（false → "Invalid"）/ `Func<T,Task<bool>>`
   - `Func<T,string?>`（null=有效，非 null=错误文本）/ Task 版
   - `Func<T,IEnumerable<string?>>`（空=有效）/ Task 版
   - `ValidationAttribute` 实例（如上例 EmailAddressAttribute）
   - `Func<object,string,IEnumerable<string?>>`（model+属性路径，FluentValidation 集成用；表单级 `MudForm Model= Validation=` 传入）
3. **EditForm 通道**：放 `<EditForm Model= OnValidSubmit=>` + `<DataAnnotationsValidator/>`，Mud 输入组件级联 `EditContext` 参与原生校验

`For` 必须是**属性访问表达式**（`For="() => _model.Email"`）；`[Compare]` 等需要模型上下文的特性依赖表达式 Model，裸 new 对象当 Model 时此类特性不生效。

### 陷阱

- **MudForm 内禁用 `ButtonType.Submit` 按钮**（官方 FormPage.razor 明确警告）；MudForm 没有 `OnValidSubmit`，提交逻辑自己写（按钮 `OnClick` → `ValidateAsync()` → 判 `IsValid`）
- 想 Enter 触发：`MudForm.OnEnterPressed` 事件
- `MudFormItem` 组件在 v9 **不存在**；嵌套表单直接放 `<MudForm>`
- 表单级 `Validation` 只作用于带 `For` 的子控件；无 `For` 的子控件必须各自设 `Validation`
- 错误列表属性叫 `ValidationErrors`（控件级，`List<string>`）；表单级叫 `Errors`
- `MudTextField` 默认 Enter/失焦才更新绑定值；`Immediate="true"` 或 `DebounceInterval>0`（隐式启用即时）才逐键更新，配合 `ValidationDelay` 防抖

## Dialog

### 服务式（最常用）

```razor
@inject IDialogService DialogService

<MudButton Variant="Variant.Filled" OnClick="OpenDialogAsync">编辑</MudButton>

@code {
    private async Task OpenDialogAsync()
    {
        var parameters = new DialogParameters<EditDialog>();
        parameters.Add(x => x.Person, _person);   // 泛型 Add（集合初始化器 { {x=>x.P, v} } 写法不被 DialogParameters 支持）
        var options = new DialogOptions
        {
            CloseButton = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
            CloseOnEscapeKey = true,
        };
        var dialog = await DialogService.ShowAsync<EditDialog>("编辑人员", parameters, options);
        var result = await dialog.Result;
        if (result is not null && !result.Canceled)
        {
            var saved = (Person)result.Data!;
            // 处理保存结果
        }
    }
}
```

### 对话框内容组件

```razor
@* EditDialog.razor *@
@inject ISnackbar Snackbar

<MudDialog>
    <TitleContent><MudText Typo="Typo.h6">编辑人员</MudText></TitleContent>
    <DialogContent>
        <MudForm @ref="_form" Model="_person">
            <MudTextField @bind-Value="_person.Name" For="() => _person.Name"
                          Label="姓名" Required="true" />
        </MudForm>
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="Cancel">取消</MudButton>
        <MudButton Color="Color.Primary" Variant="Variant.Filled" OnClick="SubmitAsync">保存</MudButton>
    </DialogActions>
</MudDialog>

@code {
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;
    [Parameter] public Person Person { get; set; } = new();

    private MudForm? _form;

    private async Task SubmitAsync()
    {
        if (_form is null) return;
        await _form.ValidateAsync();
        if (_form.IsValid)
        {
            Snackbar.Add("保存成功", Severity.Success);
            MudDialog.Close(DialogResult.Ok(Person));
        }
    }

    private void Cancel() => MudDialog.Cancel();
}
```

### 关键 API 事实

- **v9 全部异步**：`ShowAsync<T>()`（8 个重载：title/options/parameters 组合）、`ShowAsync(Type, ...)`；**同步 `Show()` 已移除**
- `IDialogReference`：`Task<DialogResult?> Result`、`Close()`、`Close(DialogResult?)`、`Task<T?> GetReturnValueAsync<T>()`；没有 Enqueue、没有公共 CancellationToken（内部 5 秒渲染超时）
- `DialogResult`：`Data`、`Canceled`、静态 `DialogResult.Ok<T>(T)` / `DialogResult.Cancel()`；ESC 关闭返回 Cancel
- 对话框内部句柄 `[CascadingParameter] IMudDialogInstance`：`Close()` / `Close(DialogResult)` / `Close<T>(T)` / `Cancel()` / `SetOptionsAsync` / `SetTitleAsync`
- `MudDialog` 没有 `MudDialogActions` 子组件——`TitleContent` / `DialogContent` / `DialogActions` 是 MudDialog 的 **RenderFragment 参数**
- 内联对话框：`<MudDialog Visible="@_visible" @bind-Visible>` + `ShowAsync()` / `CloseAsync()`（无需 Provider，但同页要可交互）

### DialogOptions（record，全可空 bool 表示未设置走 Provider 全局值）

| 属性 | 说明 |
| --- | --- |
| `MaxWidth` | MaxWidth.Xs/Sm/Md/Lg/Xl/Xxl（默认 Md 上限） |
| `FullWidth` | 占满 MaxWidth |
| `FullScreen` | 全屏 |
| `CloseButton` | 右上角 X |
| `CloseOnEscapeKey` | ESC 关闭 |
| `BackdropClick` | 点遮罩关闭（编辑表单建议 false 防误关） |
| `NoHeader` | 无标题栏 |
| `Position` | DialogPosition.Center/TopLeft/... |
| `DefaultFocus` | DefaultFocus.Element/FirstChild... |

Provider 全局默认在 `<MudDialogProvider NoHeader= CloseButton= ...>` 上设置。

### MessageBox

```csharp
// 服务式：null=取消，true=Yes，false=No
var confirmed = await DialogService.ShowMessageBoxAsync(
    "确认删除",
    "删除后不可恢复，确定继续吗？",
    yesText: "删除", cancelText: "取消");
if (confirmed is true) { /* 执行删除 */ }

// 内联式
<MudMessageBox @ref="_box" Title="警告">
    <MessageContent>此操作 <b>不可撤销</b>！</MessageContent>
    <YesButton><MudButton Variant="Variant.Filled" Color="Color.Error">删除</MudButton></YesButton>
</MudMessageBox>
```

`MudMessageBox.ShowAsync(DialogOptions?)` 返回 `bool?`；`YesText` 默认 "OK"（null 隐藏按钮）。

### 陷阱

- **`MudDialog.Close()` 不触发校验**：必须先 `await _form.ValidateAsync()` 判 `IsValid` 再 Close（Dialog 内 MudForm 的 `SuppressImplicitSubmission=true` 已防 Enter 误关）
- 缺 `MudDialogProvider` 时 `ShowAsync` 记错误日志并阻塞 5 秒后失败——页面对话框不弹出先查 Provider
- `IDialogService` 是 **Scoped**：不要用 Singleton 持有或转发到后台线程
- `dialog.Result` 是 `Task`：必须 await；取消时 `result.Canceled=true` 且 `Data` 为 null，解引用前判断

## Snackbar

```razor
@inject ISnackbar Snackbar

<MudButton OnClick='() => Snackbar.Add("已保存")'>普通</MudButton>
<MudButton OnClick="NotifySuccessAsync">成功</MudButton>

@code {
    private void NotifySuccessAsync()
    {
        Snackbar.Add("保存成功", Severity.Success, cfg =>
        {
            cfg.VisibleStateDuration = 3000;
            cfg.Action = "撤销";                     // 动作按钮文字（不存在 CloseButtonText）
            cfg.OnClick = snackbar => Task.CompletedTask;
            cfg.ShowCloseIcon = false;
        });
    }
}
```

### API 事实

- `Add` 4 组重载：`Add(string message, Severity severity = Normal, Action<SnackbarOptions>? configure = null, string? key = null)`；`MarkupString` 版（HTML 原样渲染）；`RenderFragment` 版；`Add<T>(Dictionary<string,object>?)` 渲染自定义组件
- `Severity`：`Normal, Info, Success, Warning, Error`
- string 消息会 **HTML 转义**；要富文本用 `Add((MarkupString)"<b>x</b>", ...)`
- 空串消息返回 null 不显示
- `Clear()` / `Remove(Snackbar)` / `RemoveByKey(string)`

### SnackbarOptions 关键项

| 属性 | 默认值 | 说明 |
| --- | --- | --- |
| `Action` | null | 动作按钮文字；设置后默认 `RequireInteraction=true` |
| `ActionColor` / `ActionVariant` | Primary / Filled | 动作按钮样式 |
| `OnClick` | null | `Func<Snackbar,Task>` |
| `VisibleStateDuration` | 5000 (ms) | 显示时长 |
| `ShowCloseIcon` | true | |
| `CloseAfterNavigation` | false | 导航后关闭 |
| `DuplicatesBehavior` | GlobalDefault | Prevent/Allow |
| `SnackbarVariant` | Filled | 视觉变体 |

### 全局配置（Program.cs）

```csharp
builder.Services.AddMudServices(cfg =>
{
    cfg.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
    cfg.SnackbarConfiguration.PreventDuplicates = true;   // 默认 true
    cfg.SnackbarConfiguration.MaxDisplayedSnackbars = 5;
    cfg.SnackbarConfiguration.VisibleStateDuration = 4000;
});
```

- 去重：`PreventDuplicates=true` 时相同 `key`（默认取消息文本）显示中会被拒显；需要重复提示时传不同 `key` 或 `DuplicatesBehavior.Allow`
- 位置常量：`Defaults.Classes.Position.TopRight/BottomRight/TopCenter/...`
- **`MaximumDuplicatedSnackbars`、`CloseButtonText`、`RenderMode` 在 v9 均不存在**

### 陷阱

- 必须有 `<MudSnackbarProvider />`（缺则 Add 静默无效）；必须同一交互渲染模式，静态 SSR 不可用
- `ISnackbar` 是 **Scoped**
- 连续操作反馈（如循环里逐条 Add）注意去重：同文本只出一条

## 组合模式：CRUD 页内反馈

```csharp
private async Task DeleteAsync(Person person)
{
    var confirmed = await DialogService.ShowMessageBoxAsync(
        "确认删除", $"确定删除 {person.Name} 吗？", yesText: "删除", cancelText: "取消");
    if (confirmed is not true) return;

    await _repo.DeleteAsync(person);
    Snackbar.Add($"已删除 {person.Name}", Severity.Success);
    await _grid.ReloadServerData();
}
```

对话框表单完整示例见 `examples/dialog-form.razor`；CRUD 整页见 `examples/crud-page.razor`。
