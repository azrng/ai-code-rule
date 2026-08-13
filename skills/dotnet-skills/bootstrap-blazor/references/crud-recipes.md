# CRUD 标准配方（AI 照此生成）

来源：rbac-blazor（10.6.0 真实 RBAC 后台）实战 + 官方 Samples。每个配方给出用途、关键骨架（真实 API）、取舍说明。

> 核心取舍信号：rbac-blazor 用 `<Modal>` 子组件做编辑弹窗（非 `DialogService`）、用 `<TableCellPopConfirmButton>` 做删除确认（非 `Swal`）、只用 `ToastService` 反馈、服务端 `OnQueryAsync` 分页、`Index/Add/Edit.razor` + `.razor.cs` code-behind 三件套。

## 配方 1：CRUD 列表页（Index）

用途：后台业务列表（搜索 + 工具栏 + 服务端分页 Table + 操作列 + 删除确认 + Toast）。

```razor
@* Index.razor *@
@page "/users"
@attribute [Authorize]
<PageTitle>用户管理</PageTitle>
<div class="container-fluid">
    <Card class="mb-4 shadow-sm border-0">
        <BodyTemplate>
            <Button Color="Color.Primary" OnClick="OnShowAdd"><i class="fas fa-plus me-2"></i>添加</Button>
            <Button Color="Color.Danger" OnClick="OnBatchDelete"><i class="fas fa-trash-alt me-2"></i>批量删除</Button>
            <Button Color="Color.Success" OnClick="OnRefresh"><i class="fas fa-sync-alt me-2"></i>刷新</Button>
        </BodyTemplate>
    </Card>

    <Table TItem="UserListDto" @ref="_table"
           IsPagination="true" PageItemsSource="new[] {10,20,50,100}"
           IsStriped="true" IsBordered="true"
           ShowSearch="true" SearchMode="SearchMode.Top" IsMultipleSelect="true"
           ShowExtendButtons="true" ShowExtendEditButton="false" ShowExtendDeleteButton="false"
           CustomerSearchModel="@_search" ShowEmpty="true"
           OnQueryAsync="OnQueryAsync" OnResetSearchAsync="OnResetSearch" OnDeleteAsync="OnDeleteAsync">
        <TableColumns>
            <TableColumn @bind-Field="@context.RealName" Text="姓名" Width="150" />
            <TableColumn @bind-Field="@context.Disabled" Text="状态" Width="80">
                <Template Context="v">
                    <Badge Color="@(v.Value ? Color.Danger : Color.Success)">
                        @(v.Value ? "禁用" : "启用")
                    </Badge>
                </Template>
            </TableColumn>
            <TableColumn @bind-Field="@context.CreateTime" Text="创建时间" FormatString="yyyy-MM-dd HH:mm:ss" />
        </TableColumns>
        <RowButtonTemplate>
            <TableCellButton Size="Size.ExtraSmall" Color="Color.Success" Icon="fas fa-edit" Text="编辑"
                             OnClick="() => OnShowEdit(context)" />
            <TableCellPopConfirmButton Size="Size.ExtraSmall" Color="Color.Danger" Icon="fa-solid fa-trash"
                                       Text="删除" Content="确定删除该用户吗？"
                                       OnConfirm="@(() => OnDeleteAsync(new[] { context }))" />
        </RowButtonTemplate>
        <CustomerSearchTemplate>
            @if (context is UserSearchVm m)
            {
                <GroupBox Title="搜索条件">
                    <BootstrapInput @bind-Value="@m.RealName" DisplayText="姓名" ShowLabel="true" />
                    <Select @bind-Value="@m.Disabled" DisplayText="状态" ShowLabel="true">
                        <Options>
                            <SelectOption Text="全部" Value="" />
                            <SelectOption Text="启用" Value="false" />
                            <SelectOption Text="禁用" Value="true" />
                        </Options>
                    </Select>
                </GroupBox>
            }
        </CustomerSearchTemplate>
    </Table>

    <Add @ref="_add" OnSaveCallback="OnAddSaved" />
    <Edit @ref="_edit" OnSaveCallback="OnEditSaved" />
</div>
```

```csharp
// Index.razor.cs
[Authorize]
public partial class Index : ComponentBase
{
    private UserSearchVm _search = new();
    private Table<UserListDto>? _table;
    private Add? _add; private Edit? _edit;

    [Inject] IUserQuery Query { get; set; } = null!;
    [Inject] IUserService Service { get; set; } = null!;
    [Inject] ToastService ToastService { get; set; } = null!;

    private async Task<QueryData<UserListDto>> OnQueryAsync(QueryPageOptions opt)
    {
        _search.PageIndex = opt.PageIndex; _search.PageSize = opt.PageItems;
        var r = await Query.GetPageListAsync(_search);
        if (r.IsSuccess && r.Data != null)
            return new QueryData<UserListDto>
            {
                Items = r.Data.Rows,
                TotalCount = Convert.ToInt32(r.Data.Total),
                IsFiltered = opt.Filters.Any(),
                IsSorted = opt.SortOrder != SortOrder.Unset,
                IsAdvanceSearch = opt.AdvanceSearches.Any(),
                IsSearch = opt.Searches.Any()
            };
        return new QueryData<UserListDto>();
    }

    private Task OnResetSearch(UserSearchVm m) { _search = new(); return Task.CompletedTask; }

    private async Task<bool> OnDeleteAsync(IEnumerable<UserListDto> items)
    {
        var r = await Service.DeleteAsync(items.Select(x => x.Id));
        if (r.IsSuccess) { await ToastService.Success("消息通知", "删除成功"); return true; }
        await ToastService.Error("消息通知", r.Message); return false;
    }

    private async Task OnRefresh() { if (_table != null) await _table.QueryAsync(); }
    private async Task OnShowAdd() { if (_add != null) await _add.Show(); }
    private async Task OnShowEdit(UserListDto u) { if (_edit != null) await _edit.Show(u.Id); }
    private async Task OnAddSaved() { if (_add != null) await _add.Hide(); await OnRefresh(); }
    private async Task OnEditSaved() { if (_edit != null) await _edit.Hide(); await OnRefresh(); }
}
```

要点：
- 服务端查询 `OnQueryAsync` 返回 `QueryData<T>{ Items, TotalCount, Is*, ... }`；刷新 `await _table.QueryAsync()`。
- 删除确认走行内 `TableCellPopConfirmButton`（非 Swal），批量删除 `OnDeleteAsync` 收 `IEnumerable`。
- 编辑/新增放 `<Add>`/`<Edit>` 子组件，父用 `@ref` 调 `Show()`、子用 `EventCallback` 回调。

## 配方 2：表单弹窗（Modal 子组件）

用途：新增/编辑表单窗，类型安全、易调试（优于 DialogService）。

```razor
@* Add.razor *@
<Modal @ref="_modal" Size="Size.Large">
    <ModalDialog Title="添加用户" IsCentered="true">
        <BodyTemplate>
            <ValidateForm Model="@Model">
                <div class="row g-3">
                    <div class="col-md-6">
                        <BootstrapInput @bind-Value="@Model.UserName" DisplayText="用户名"
                                        PlaceHolder="请输入" ShowLabel="true" Required="true" />
                    </div>
                    <div class="col-md-6">
                        <Select ShowSearch="true" TValue="string" Items="@DeptOptions"
                                @bind-Value="@Model.DepartmentId" DisplayText="部门" ShowLabel="true" IsClearable="true" />
                    </div>
                    <div class="col-md-6">
                        <BootstrapInputNumber @bind-Value="@Model.Sort" DisplayText="排序" ShowLabel="true" Min="0" />
                    </div>
                    <div class="col-md-6">
                        <Switch @bind-Value="@Model.Disabled" DisplayText="禁用" OnText="是" OffText="否" />
                    </div>
                </div>
            </ValidateForm>
        </BodyTemplate>
        <FooterTemplate>
            <Button Color="Color.Primary" OnClick="OnSave"><i class="fas fa-save me-2"></i>提交</Button>
        </FooterTemplate>
    </ModalDialog>
</Modal>
```

```csharp
// Add.razor.cs
public partial class Add : ComponentBase
{
    private Modal? _modal;
    private AddUserVm Model { get; set; } = new();
    private List<SelectedItem> DeptOptions { get; set; } = new();

    [Inject] IUserService UserService { get; set; } = null!;
    [Inject] ToastService ToastService { get; set; } = null!;
    [Parameter] public EventCallback<AddUserVm> OnSaveCallback { get; set; }

    public async Task Show()
    {
        await InvokeAsync(async () =>
        {
            Model = new AddUserVm();
            StateHasChanged();
            if (_modal != null) await _modal.Show();
        });
    }
    public async Task Hide() { if (_modal != null) await _modal.Close(); }

    private async Task OnSave()
    {
        if (string.IsNullOrWhiteSpace(Model.UserName)) { await ToastService.Error("消息通知", "请输入用户名"); return; }
        var r = await UserService.AddAsync(Model);
        if (r.IsFailure) { await ToastService.Error("消息通知", r.Message); return; }
        await ToastService.Success("消息通知", r.Message);
        await Hide();
        if (OnSaveCallback.HasDelegate) await OnSaveCallback.InvokeAsync(Model);
    }
}
```

编辑版（`Edit.razor.cs`）：`Show(string id)` → `await Query.GetDetailAsync(id)` 回填 `Model` → `_modal.Show()`；只读字段加 `Readonly="true"`。

## 配方 3：后台 Layout

用途：管理系统主壳。详见 [layout.md](layout.md) 末尾示例。关键：`<Layout IsFullSide IsPage UseTabSet Menus AdditionalAssemblies>` + `Header/Side/Main/Footer` 模板；`<ErrorLogger>` 包 `@Body` 做全局异常；`<Logout>` 做登出。

## 配方 4：反馈用法

| 场景 | 写法 |
|---|---|
| 成功 | `await ToastService.Success("消息通知", "操作成功");` |
| 失败 | `await ToastService.Error("消息通知", result.Message);` |
| 警告 | `await ToastService.Warning("消息通知", "请选择记录");` |
| 自定义 Toast | `await ToastService.Show(new ToastOption { Category = ToastCategory.Error, Title = "消息通知", Content = msg });` |
| 删除确认（首选） | 行内 `<TableCellPopConfirmButton Content="确定删除吗？" OnConfirm="..." />` |
| 确认框（通用） | `if (await SwalService.ShowModal(new SwalOption { Category = SwalCategory.Question, Title = "确认？" })) {...}` |
| 弹窗组件 | `<Modal><ModalDialog>` 子组件 + `@ref`（首选）或 `DialogService.Show<T>()` |

> 标题约定「消息通知」（rbac 项目约定）。批量删除前用 `ToastService.Warning` 提示「请选择记录」。
