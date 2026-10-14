# Table 组件（数据表格）

## 1.1 分页表格 + 搜索（标准 CRUD 页面）

**.razor 文件**：
```razor
<Table @ref="UserTable" TItem="GetUserListResult"
       IsPagination="true" PageItemsSource="new[] {10, 20, 50, 100}"
       IsStriped="true" IsBordered="true"
       ShowToolbar="false" ShowSearch="true" IsMultipleSelect="true"
       ShowExtendButtons="true"
       ShowExtendEditButton="false" ShowExtendDeleteButton="false"
       CustomerSearchModel="@CustomerSearchModel" SearchMode="SearchMode.Top"
       ShowEmpty="true"
       OnQueryAsync="@OnSearchModelQueryAsync"
       OnResetSearchAsync="@OnResetSearchAsync"
       OnDeleteAsync="@OnDeleteAsync">

    <!-- 自定义搜索模板 -->
    <CustomerSearchTemplate>
        @if (context is GetUserPageListVm model)
        {
            <GroupBox Title="搜索条件">
                <div class="row g-3 form-inline">
                    <div class="col-12 col-sm-6">
                        <BootstrapInput @bind-Value="@model.RealName" DisplayText="真实姓名"
                                        PlaceHolder="请输入真实姓名" ShowLabel="true"/>
                    </div>
                    <div class="col-12 col-sm-6">
                        <Select @bind-Value="@model.Disabled" DisplayText="状态" ShowLabel="true">
                            <Options>
                                <SelectOption Text="全部" Value="" />
                                <SelectOption Text="启用" Value="false" />
                                <SelectOption Text="禁用" Value="true" />
                            </Options>
                        </Select>
                    </div>
                </div>
            </GroupBox>
        }
    </CustomerSearchTemplate>

    <!-- 表格列：TableColumn 必须包在 TableColumns 内，直接放在 Table 下不生效 -->
    <TableColumns>
        <TableColumn @bind-Field="@context.UserName" Text="用户名" Width="120"/>
        <TableColumn @bind-Field="@context.RealName" Text="真实姓名" Width="120"/>
        <TableColumn @bind-Field="@context.CreateTime" Text="创建时间" Width="180"
                     FormatString="yyyy-MM-dd HH:mm:ss"/>

        <!-- 状态列（Badge 模板）：模板上下文显式改名，避免与外层默认 context 同名冲突 -->
        <TableColumn @bind-Field="@context.Disabled" Text="状态" Width="80">
            <Template Context="value">
                <Badge Color="@(value.Value ? Color.Danger : Color.Success)">
                    @(value.Value ? "禁用" : "启用")
                </Badge>
            </Template>
        </TableColumn>
    </TableColumns>

    <!-- 行操作按钮 -->
    <RowButtonTemplate>
        <TableCellButton Size="Size.ExtraSmall" Color="Color.Success" Icon="fas fa-edit"
                         Text="编辑" OnClick="() => OnShowEditModal(context)" />
        <TableCellPopConfirmButton Size="Size.ExtraSmall" Color="Color.Danger"
                                   Icon="fa-solid fa-trash" Text="删除"
                                   Content="确定要删除吗？"
                                   OnConfirm="@(() => OnDeleteAsync(new[] { context }))"/>
    </RowButtonTemplate>
</Table>
```

**.razor.cs 代码隐藏文件**：
```csharp
public partial class Index
{
    [Inject] private IUserQuery UserQuery { get; set; } = null!;
    [Inject] private ToastService ToastService { get; set; } = null!;

    private Table<GetUserListResult>? UserTable { get; set; }
    private GetUserPageListVm CustomerSearchModel { get; set; } = new();

    private async Task<QueryData<GetUserListResult>> OnSearchModelQueryAsync(QueryPageOptions options)
    {
        CustomerSearchModel.PageSize = options.PageItems;
        CustomerSearchModel.PageIndex = options.PageIndex;

        var result = await UserQuery.GetPageListAsync(CustomerSearchModel);
        if (result.IsSuccess && result.Data != null)
        {
            return new QueryData<GetUserListResult>()
            {
                Items = result.Data.Rows,
                TotalCount = Convert.ToInt32(result.Data.PageInfo.Total),
                IsFiltered = options.Filters.Any(),
                IsSorted = options.SortOrder != SortOrder.Unset,
                IsAdvanceSearch = options.AdvanceSearches.Any(),
                IsSearch = options.Searches.Any()
            };
        }
        return new QueryData<GetUserListResult>();
    }

    private Task<GetUserPageListVm> OnResetSearchAsync()
    {
        CustomerSearchModel = new GetUserPageListVm();
        return Task.FromResult(CustomerSearchModel);
    }

    private async Task<bool> OnDeleteAsync(IEnumerable<GetUserListResult> items)
    {
        var ids = items.Select(x => x.Id).ToArray();
        var result = await UserService.DeleteAsync(ids);
        if (result.IsSuccess)
        {
            await ToastService.Success("消息通知", "删除成功");
            return true;
        }
        await ToastService.Error("消息通知", result.Message);
        return false;
    }
}
```

**要点**：
- `ShowExtendButtons="true"` 启用扩展按钮区域
- `ShowExtendEditButton="false"` `ShowExtendDeleteButton="false"` 隐藏内置编辑/删除按钮，改用自定义行按钮
- `SearchMode="SearchMode.Top"` 搜索框在表格上方
- `OnQueryAsync` 返回 `QueryData<T>` 包含分页信息
- 批量删除通过 `IsMultipleSelect="true"` + `OnDeleteAsync` 处理
- 列定义必须包在 `<TableColumns>` 内，`TableColumn` 直接放在 `Table` 下不生效
- 嵌套模板的上下文显式改名（如 `<Template Context="value">`）：`TableColumns`、`RowButtonTemplate`、`AuthorizeView` 等默认上下文都叫 `context`，嵌套时同名互相遮蔽导致编译错误或取错对象

## 1.2 树形表格（菜单/部门管理）

```razor
<Table @ref="TableRef" TItem="GetMenuListResult"
       OnQueryAsync="@OnQueryAsync"
       IsTree="true"
       TreeNodeConverter="@TreeNodeConverter"
       ShowToolbar="false" ShowSearch="false"
       IsStriped="true" IsBordered="true"
       ShowExtendButtons="true">
```

```csharp
private static Task<IEnumerable<TableTreeNode<GetMenuListResult>>> TreeNodeConverter(
    IEnumerable<GetMenuListResult> items)
{
    var itemList = items?.ToList() ?? [];
    var lookup = itemList.ToLookup(x => x.ParentId);

    IEnumerable<TableTreeNode<GetMenuListResult>> BuildTree(string parentId)
    {
        return lookup[parentId].Select(item => new TableTreeNode<GetMenuListResult>(item)
        {
            HasChildren = lookup[item.Id].Any(),
            IsExpand = false,
            Items = BuildTree(item.Id)
        });
    }
    return Task.FromResult(BuildTree(string.Empty));
}
```

## 1.3 内联编辑表格（简单字典数据）

```razor
<Table TItem="GetDictDataResult"
       AddModalTitle="增加字典数据" EditModalTitle="编辑字典数据"
       OnAddAsync="@OnAddAsync" OnSaveAsync="@OnSaveAsync"
       OnDeleteAsync="@OnDeleteAsync"
       ShowExtendButtons="true"
       IsStriped="true" IsBordered="true">
```

```csharp
private static Task<GetDictDataResult> OnAddAsync() =>
    Task.FromResult(new GetDictDataResult { IsEnabled = true });

private async Task<bool> OnSaveAsync(GetDictDataResult item, ItemChangedType changedType)
{
    var result = changedType == ItemChangedType.Add
        ? await Service.AddAsync(item)
        : await Service.EditAsync(item);
    return result.IsSuccess;
}
```

**要点**：适合简单 CRUD，无需自定义 Add/Edit 组件的场景。列定义中通过 `IsVisibleWhenAdd="false"` `IsVisibleWhenEdit="false"` 控制字段可见性。

## 1.4 刷新表格数据

```csharp
// 查询/刷新表格
if (UserTable != null)
    await UserTable.QueryAsync();

// 获取选中行
var selectedRows = UserTable?.SelectedRows ?? new List<GetUserListResult>();
```
