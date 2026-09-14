# Modal 组件（模态框）

## 2.1 自定义模态框（Add/Edit 分离模式）

**页面结构**：
```
Components/Pages/[Entity]/
├── Index.razor           # 主页面，包含表格和工具栏
├── Index.razor.cs        # 主页面代码隐藏
├── Add.razor             # 添加模态框组件
├── Add.razor.cs          # 添加组件代码隐藏
├── Edit.razor            # 编辑模态框组件
└── Edit.razor.cs         # 编辑组件代码隐藏
```

**Add.razor（子组件）**：
```razor
<Modal @ref="AddModal" Size="Size.Large">
    <ModalDialog Title="添加用户" IsCentered="true">
        <BodyTemplate>
            <ValidateForm Model="@Model" OnValidSubmit="@OnValidSubmit">
                <div class="row g-3">
                    <div class="col-md-6">
                        <BootstrapInput @bind-Value="@Model.UserName" DisplayText="用户名"
                                        PlaceHolder="请输入用户名" ShowLabel="true"/>
                    </div>
                    <div class="col-md-6">
                        <BootstrapInput @bind-Value="@Model.RealName" DisplayText="真实姓名"
                                        PlaceHolder="请输入真实姓名" ShowLabel="true"/>
                    </div>
                </div>
            </ValidateForm>
        </BodyTemplate>
        <FooterTemplate>
            <Button Color="Color.Primary" OnClick="@OnSave">
                <i class="fas fa-save me-2"></i>提交
            </Button>
        </FooterTemplate>
    </ModalDialog>
</Modal>
```

**Add.razor.cs（代码隐藏）**：
```csharp
public partial class Add
{
    [Inject] private IUserService UserService { get; set; } = null!;
    [Inject] private ToastService ToastService { get; set; } = null!;

    [Parameter] public EventCallback<AddUserVm> OnSaveCallback { get; set; }

    private Modal? AddModal { get; set; }
    private AddUserVm Model { get; set; } = new();

    public async Task Show()
    {
        await InvokeAsync(async () =>
        {
            Model = new AddUserVm();
            StateHasChanged();
            if (AddModal != null)
                await AddModal.Show();
        });
    }

    public async Task Hide()
    {
        if (AddModal != null)
            await AddModal.Close();
    }

    private async Task OnSave()
    {
        var result = await UserService.AddAsync(Model);
        if (result.IsSuccess)
        {
            await ToastService.Success("消息通知", "添加成功");
            if (OnSaveCallback.HasDelegate)
                await OnSaveCallback.InvokeAsync(Model);
            await Hide();
        }
        else
        {
            await ToastService.Error("消息通知", result.Message);
        }
    }
}
```

**Index.razor（父组件）**：
```razor
<Add @ref="AddComponent" OnSaveCallback="@OnAddSave"/>

@code {
    private Add? AddComponent { get; set; }

    private async Task OnShowAddModal()
    {
        if (AddComponent != null)
            await AddComponent.Show();
    }

    private async Task OnAddSave(AddUserVm model)
    {
        if (AddComponent != null)
            await AddComponent.Hide();
        if (UserTable != null)
            await UserTable.QueryAsync();
    }
}
```

**要点**：
- `Modal` 尺寸：`Size.Small`（简单确认）、`Size.Medium`（修改密码）、`Size.Large`（标准 CRUD）、`Size.ExtraLarge`（复杂表单如权限分配）
- `InvokeAsync` 确保在 UI 线程执行，`StateHasChanged()` 强制重新渲染
- `EventCallback<T>` 实现父子组件通信
- 子组件暴露 `Show()`/`Hide()` 方法供父组件调用

## 2.2 确认对话框（删除确认）

**行内确认（推荐）**：
```razor
<TableCellPopConfirmButton Size="Size.ExtraSmall" Color="Color.Danger"
                           Icon="fa-solid fa-trash" Text="删除"
                           Content="确定要删除吗？此操作不可恢复"
                           OnConfirm="@(() => OnDeleteAsync(new[] { context }))"/>
```

**自定义确认模态框**（复杂场景）：
```razor
<Modal @ref="ConfirmModal" IsCentered="true">
    <ModalDialog Title="确认操作" IsCentered="true">
        <BodyTemplate>
            <div class="text-center">
                <i class="fas fa-exclamation-triangle text-warning" style="font-size: 48px;"></i>
                <p class="text-muted mt-3">确定要执行此操作吗？此操作不可恢复！</p>
            </div>
        </BodyTemplate>
        <FooterTemplate>
            <Button Color="Color.Danger" OnClick="@OnConfirm">确认</Button>
            <Button Color="Color.Secondary" OnClick="@(() => ConfirmModal?.Close())">取消</Button>
        </FooterTemplate>
    </ModalDialog>
</Modal>
```
