# 完整页面模板

页面统一的标题区、操作区、内容层级和加载/空数据/错误状态见 `admin-shell-patterns.md`；本文专注 CRUD 页面的目录与组件组织步骤。

创建新 CRUD 页面时的标准步骤：

## Step 1：创建页面目录和文件

```
Components/Pages/EntityName/
├── Index.razor
├── Index.razor.cs
├── Add.razor
├── Add.razor.cs
├── Edit.razor
└── Edit.razor.cs
```

## Step 2：注册服务（Startup.cs）

```csharp
services.AddScoped<IEntityQuery, EntityQuery>();
services.AddScoped<IEntityService, EntityService>();
```

## Step 3：配置菜单路由

确保页面的 `@page` 路由与菜单的 `Component` 字段一致（不带前导 `/`）。

| 页面路由 | 菜单 Component |
|----------|---------------|
| `@page "/users"` | `users` |
| `@page "/role-management"` | `role-management` |

## Step 4：Index.razor 模板

```razor
@page "/entity-name"
@attribute [TabItemOption(Text = "实体管理")]
@attribute [Authorize]

<PageHeader Title="实体管理" SubTitle="管理系统中的实体数据"/>

<div class="mb-3">
    <Button Color="Color.Primary" OnClick="@OnShowAddModal">
        <i class="fas fa-plus me-2"></i>添加
    </Button>
    <Button Color="Color.Danger" OnClick="@OnBatchDelete" IsAsync="true">
        <i class="fas fa-trash me-2"></i>批量删除
    </Button>
</div>

<Table @ref="EntityTable" TItem="GetEntityListResult"
       IsPagination="true" PageItemsSource="new[] {10, 20, 50}"
       IsStriped="true" IsBordered="true"
       ShowSearch="true" SearchMode="SearchMode.Top"
       ShowExtendButtons="true"
       ShowExtendEditButton="false" ShowExtendDeleteButton="false"
       OnQueryAsync="@OnQueryAsync"
       CustomerSearchModel="@SearchModel">
    <!-- 列定义：TableColumn 必须包在 TableColumns 内，直接放在 Table 下不生效 -->
    <TableColumns>
        <TableColumn @bind-Field="@context.Name" Text="名称"/>
        <TableColumn @bind-Field="@context.CreateTime" Text="创建时间" FormatString="yyyy-MM-dd HH:mm:ss"/>
    </TableColumns>
    <RowButtonTemplate>
        <TableCellButton Size="Size.ExtraSmall" Color="Color.Success" Icon="fas fa-edit"
                         Text="编辑" OnClick="() => OnShowEditModal(context)"/>
        <TableCellPopConfirmButton Size="Size.ExtraSmall" Color="Color.Danger"
                                   Icon="fa-solid fa-trash" Text="删除"
                                   Content="确定删除？" OnConfirm="@(() => OnDelete(context))"/>
    </RowButtonTemplate>
</Table>

<Add @ref="AddComponent" OnSaveCallback="@OnAddSave"/>
<Edit @ref="EditComponent" OnSaveCallback="@OnEditSave"/>
```
