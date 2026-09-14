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

