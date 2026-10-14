# 表单组件

## 3.1 ValidateForm（表单验证）

```razor
<!-- 自动提交验证 -->
<ValidateForm Model="@Model" OnValidSubmit="@OnValidSubmit">
    <BootstrapInput @bind-Value="@Model.Name" DisplayText="名称"
                    PlaceHolder="请输入名称" ShowLabel="true"/>
</ValidateForm>

<!-- 手动提交验证 -->
<ValidateForm Model="@Model">
    <BootstrapInput @bind-Value="@Model.Name" DisplayText="名称"/>
    <Button OnClick="@OnManualSubmit">提交</Button>
</ValidateForm>
```

**注意**：组件库中不存在 `BootstrapInputFormItem` / `RadioListFormItem` / `DateTimePickerFormItem` 类表单包装组件；表单项 = `ValidateForm` + 各输入组件直接组合，标签由输入组件的 `DisplayText` 提供。

**必须绑定模型属性，不可绑 `@code` 裸字段**：ValidateForm 内的输入组件绑裸字段时，BB 校验组件反射 `MemberInfo` 为 null 抛 `ArgumentNullException('element')`；单字段小表单也建一个模型类承载。

**自动化测试提交表单**：`ButtonType.Submit` + `IsAsync` 的提交按钮不吃页内 JS 合成 `click()`，等价路径是对表单调 `form.requestSubmit()`；JS 填值需原生 value setter + `input` + `change` 双事件（详见 `troubleshooting.md`）。

**手动验证模式**（需要自定义验证逻辑时）：
```csharp
private async Task OnManualSubmit()
{
    var validationContext = new ValidationContext(Model);
    var validationResults = new List<ValidationResult>();
    bool isValid = Validator.TryValidateObject(Model, validationContext, validationResults, true);
    if (!isValid)
    {
        foreach (var result in validationResults)
            await ToastService.Error("验证错误", result.ErrorMessage ?? "验证失败");
        return;
    }
    // 继续业务逻辑...
}
```

## 3.2 输入组件一览

**宽度控制**：`BootstrapInput` / `BootstrapInputNumber` / `Textarea` 等输入组件**没有 `Width` 参数**（BB 10.10.x 仅 `BootstrapInputGroupLabel` 有）；写了会经 `CaptureUnmatchedValues` 输出为无效 HTML attribute **静默失效**（表现为各筛选项各占一行）。宽度用栅格容器（`row g-3` + `col-*`）约束，或原生 input + class 工具类。

**文本输入**：
```razor
<BootstrapInput @bind-Value="@Model.UserName" DisplayText="用户名"
                PlaceHolder="请输入用户名" maxlength="50" ShowLabel="true"/>

<!-- 只读 -->
<BootstrapInput @bind-Value="@Model.UserName" DisplayText="用户账号"
                ReadOnly="true" ShowLabel="true"/>

<!-- 密码 -->
<BootstrapInput @bind-Value="@Model.Password" PlaceHolder="请输入密码"
                type="Password" DisplayText="密码" ShowLabel="true"/>
```

**数字输入**：
```razor
<BootstrapInputNumber @bind-Value="@Model.SortNumber" DisplayText="排序"
                      ShowLabel="true" Min="0"/>
```

**多行文本**：
```razor
<Textarea @bind-Value="@Model.Remark" DisplayText="备注"
          PlaceHolder="请输入备注" maxlength="500" ShowLabel="true" rows="3"/>
```

**开关**：
```razor
<Switch @bind-Value="@Model.Disabled" DisplayText="是否禁用"
        ShowLabel="true" OnLabel="是" OffLabel="否"/>
```

## 3.3 下拉选择组件

**静态选项**：
```razor
<Select @bind-Value="@Model.Status" DisplayText="状态" ShowLabel="true">
    <Options>
        <SelectOption Text="全部" Value="" />
        <SelectOption Text="启用" Value="false" />
        <SelectOption Text="禁用" Value="true" />
    </Options>
</Select>
```

**动态数据源 + 可搜索 + 可清除**：
```razor
<Select ShowSearch="true" TValue="string" Items="@DepartmentOptions"
        @bind-Value="@Model.DepartmentId" DisplayText="所属部门"
        PlaceHolder="请选择部门" ShowLabel="true" IsClearable="true"/>
```

```csharp
private List<SelectedItem> DepartmentOptions { get; set; } = new();
// 在 OnInitializedAsync 或 OnAfterRenderAsync 中加载：
DepartmentOptions = departments.Select(x => new SelectedItem(x.Id, x.Name)).ToList();
```

**枚举类型选择**：
```razor
<Select TValue="MenuTypeEnum" Items="@MenuTypeOptions"
        @bind-Value="@Model.MenuType" DisplayText="菜单类型"
        PlaceHolder="请选择" ShowLabel="true"/>
```

**泛型参数**：`Select` 的类型参数是 `TValue`，与 `@bind-Value` 绑定属性类型一致，通常由绑定自动推断、无需显式声明；不要照抄 `Table` 的 `TItem`（写成 `TItem="string"` 会编译报错）。`bool` / `bool?` / 枚举 / `int?` / `Guid` 绑定均内置支持，`SelectOption` 的 `Value` 用字符串表示即可。

**可空标注**：`.razor` 标记内 `TValue="string?"` 等可空标注会写进非可空生成代码，触发 CS8669；字符串筛选下拉用「全部」占位项（`SelectOption Text="全部" Value=""`）+ 非可空绑定（空串表示全部）替代，不给组件泛型参数标可空。

## 3.4 复选框列表

```razor
<CheckboxList TValue="string" @bind-Value="@SelectedIdsString"
              Items="@Options" ShowLabel="true" DisplayText="选项"
              IsVertical="true" ShowBorder="true"/>
```

```csharp
private List<SelectedItem> Options { get; set; } = new();
private string SelectedIdsString { get; set; } = string.Empty;
// 解析：SelectedIdsString.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
```

