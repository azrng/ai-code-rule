# Table 组件深度参考

`<Table TItem>` 是 BootstrapBlazor 的核心数据组件。泛型签名 `Table<TItem> where TItem : class`（`Table.razor.cs:17`），markup 写 `TItem="Foo"`。

> 目录：`Components/Table/`。行号基于 10.9.2。`Items`（客户端）与 `OnQueryAsync`（服务端）**互斥**，同设抛 `InvalidOperationException`（`Table.razor.cs:1038`）。

## 1. 数据源

| 参数 | 类型 | 说明 | 来源 |
|---|---|---|---|
| `Items` | `IEnumerable<TItem>?` | 数据集合（纯展示，无高级功能） | `Table.razor.cs:643` |
| `OnQueryAsync` | `Func<QueryPageOptions, Task<QueryData<TItem>>>?` | 服务端查询回调（与 Items 互斥） | `Table.razor.Pagination.cs:59` |
| `IsPagination` | `bool` | 是否分页（默认 false） | `Table.razor.Pagination.cs:15` |
| `AutoGenerateColumns` | `bool` | 自动生成列（默认 false） | `Table.razor.Edit.cs:233` |
| `DataService` | `IDataService<TItem>?` | 数据服务（就近优先级：Items > OnQueryAsync > DataService > 全局） | `Table.razor.Edit.cs:250` |
| `IsAutoQueryFirstRender` | `bool` | 首次渲染自动查询（默认 true） | `Table.razor.cs:739` |
| `DynamicContext` | `IDynamicObjectContext?` | 动态数据上下文（DataTable 场景） | `Table.razor.cs:799` |

## 2. 服务端查询（最常用）

`OnQueryAsync(QueryPageOptions) → Task<QueryData<TItem>>`。

**`QueryPageOptions`**（`Options/QueryPageOptions.cs`）——Table 传入的查询条件：

| 字段 | 类型 | 说明 |
|---|---|---|
| `SearchText` | `string?` | 模糊关键字 |
| `SearchModel` | `object?` | 搜索模型 |
| `PageIndex` | `int` | 当前页（首页 1） |
| `PageItems` | `int` | 每页条数 |
| `IsPage` | `bool` | 是否分页模式 |
| `SortName` | `string?` | 排序字段 |
| `SortOrder` | `SortOrder` | 排序方向（Unset/Asc/Desc） |
| `SortList` | `List<string>` | 多列排序（如 `"Name", "Age desc"`） |
| `AdvancedSortList` | `List<string>` | 高级多列排序 |
| `Searches` | `List<IFilterAction>` | 模糊搜索条件 |
| `CustomerSearches` | `List<IFilterAction>` | 自定义搜索条件 |
| `AdvanceSearches` | `List<IFilterAction>` | 高级搜索条件 |
| `Filters` | `List<IFilterAction>` | 列过滤条件 |
| `IsFirstQuery` | `bool` | 是否首次查询 |

**`QueryData<TItem>`**（`Misc/QueryData.cs`）——回调返回值：

| 字段 | 类型 | 说明 |
|---|---|---|
| `Items` | `IEnumerable<TItem>?` | 当前页数据 |
| `TotalCount` | `int` | 总条数 |
| `IsFiltered` | `bool` | 已在服务端过滤（true 则组件不再过滤） |
| `IsSorted` | `bool` | 已在服务端排序 |
| `IsSearch` | `bool` | 已处理模糊搜索 |
| `IsAdvanceSearch` | `bool` | 已处理高级搜索 |

服务端已完成全部处理时，四个 `Is*` 标志都设 `true`，组件不再重复处理。

## 3. 列（`TableColumn`，在 `<TableColumns>` 内）

列容器 `TableColumns: RenderFragment<TItem>`（`Table.razor.cs:623`）。声明：`<TableColumns><TableColumn @bind-Field="context.Prop" ... /></TableColumns>`。

| 参数 | 类型 | 说明 | 来源 |
|---|---|---|---|
| `Field` / `FieldExpression` | `TType` / `Expression<Func<TType>>` | 绑定（`@bind-Field` 自动生成表达式） | `TableColumn.cs:47,55` |
| `Text` | `string?` | 列标题 | `:249` |
| `Width` | `int?` | 列宽 px | `:261` |
| `FormatString` | `string?` | 格式串（如 `yyyy-MM-dd`） | `:303` |
| `Formatter` | `Func<object?, Task<string?>>` | 异步格式化（无 `FormatterAsync`） | `:309` |
| `Align` | `Alignment?` | 对齐 | `:297` |
| `Sortable` | `bool?` | 允许排序 | `:67` |
| `DefaultSort` / `DefaultSortOrder` | `bool` / `SortOrder` | 默认排序列/方向（设初始排序的唯一方式） | `:73,139` |
| `Filterable` | `bool?` | 允许过滤 | `:145` |
| `Searchable` | `bool?` | 参与搜索 | `:151` |
| `Readonly` | `bool?` | 编辑只读（拼写 Readonly） | `:177` |
| `IsReadonlyWhenAdd` / `IsReadonlyWhenEdit` | `bool?` | 新建/编辑时只读 | `:183,189` |
| `Visible` / `IsVisibleWhenAdd` / `IsVisibleWhenEdit` | `bool?` | 可见性 | `:195,201,207` |
| `Ignore` | `bool?` | 忽略此列 | `:163` |
| `Fixed` | `bool` | 固定列 | `:267` |
| `ComponentType` | `Type?` | 编辑时渲染的组件类型 | `:28` |
| `Items` / `Lookup` | `IEnumerable<SelectedItem>?` | 下拉数据源 | `:410,422` |
| `Template` | `RenderFragment<TableColumnContext<TItem,TType?>>?` | 单元格模板 | `:315` |
| `EditTemplate` | `RenderFragment<TItem>?` | 编辑模板 | `:346` |
| `SearchTemplate` | `RenderFragment<TItem>?` | 搜索模板 | `:366` |
| `FilterTemplate` | `RenderFragment?` | 过滤模板 | `:386` |
| `HeaderTemplate` | `RenderFragment<ITableColumn>?` | 表头模板 | `:392` |

单元格模板取值：`<Template Context="ctx">@ctx.Value</Template>`（`TableColumnContext<TItem, TType?>`，含 `Value`/`Row`）。

## 4. 排序（`Table.razor.Sort.cs`）

| 参数 | 类型 | 说明 | 来源 |
|---|---|---|---|
| `SortString` | `string?` | 多列排序串（逗号分隔，如 `"Name, Age desc"`） | `:60` |
| `OnSort` | `Func<string, SortOrder, string>?` | 点击表头排序回调（返回排序串） | `:67` |
| `SortIcon` / `SortIconAsc` / `SortIconDesc` | `string?` | 排序图标 | `:30,38,46` |
| `ShowAdvancedSort` | `bool` | 高级排序按钮 | `:470` |

> `SortName`/`SortOrder` 是 `protected`（`:16,22`），**不是 Parameter**，不能 markup 绑定。设初始排序只能用列 `DefaultSort="true" DefaultSortOrder="SortOrder.Asc"`。

## 5. 搜索 / 过滤（`Table.razor.Search.cs`）

| 参数 | 类型 | 说明 | 来源 |
|---|---|---|---|
| `ShowSearch` | `bool` | 显示搜索框 | `:59` |
| `SearchModel` | `TItem?` | 搜索模型 | `:38` |
| `SearchTemplate` | `RenderFragment<TItem>?` | 搜索模板 | `:31` |
| `CustomerSearchModel` | `ITableSearchModel?` | 自定义搜索模型 | `:45` |
| `CustomerSearchTemplate` | `RenderFragment<ITableSearchModel>?` | 自定义搜索模板（rbac 实战用 `@if(context is XxxVm m)`） | `:52` |
| `SearchMode` | `SearchMode` | 搜索栏渲染方式（默认 Popup；rbac 用 `Top`） | `:115` |
| `SearchText` | `string?` | 模糊关键字 | `:108` |
| `ShowSearchText` / `ShowAdvancedSearch` / `ShowResetButton` / `ShowSearchButton` | `bool` | 搜索控件开关（默认 true） | `:73,101,87,94` |
| `CollapsedTopSearch` | `bool` | 收缩顶部搜索 | `:66` |
| `OnResetSearchAsync` | `Func<TItem, Task>?` | 重置搜索 | `:167` |
| `GetAdvancedSearchFilterCallback` | `Func<PropertyInfo,TItem,List<SearchFilterAction>?>?` | 高级搜索条件 | `Table.razor.cs:863` |

列过滤：列上 `Filterable`/`FilterTemplate`；Table 上 `ShowFilterHeader`/`ShowMultiFilterHeader`（`Table.razor.cs:693,700`）。

## 6. 分页（`Table.razor.Pagination.cs`）

| 参数 | 类型 | 说明 | 来源 |
|---|---|---|---|
| `IsPagination` | `bool` | 是否分页（默认 false） | `:15` |
| `PageItems` | `int?` | 每页条数（默认 null 取 PageItemsSource 首项） | `:84` |
| `PageItemsSource` | `IEnumerable<int>?` | 每页候选（默认 20/50/100/200/500/1000；rbac 用 `{10,20,50,100}`） | `:52` |
| `ShowPageInfo` | `bool` | 显示分页信息（默认 true） | `:112` |
| `ShowGotoNavigator` | `bool` | Goto 跳转（默认 true） | `:91` |
| `MaxPageLinkCount` | `int` | 页码数量（默认 5） | `:22` |
| `ShowTopPagination` | `bool` | 顶端分页 | `:29` |
| `ShowLineNo` | `bool` | 行号列 | `:36` |
| `PageInfoTemplate` / `GotoTemplate` | `RenderFragment?` | 分页信息/Goto 模板 | `:126,105` |

> 无 `OnPageClick` Parameter；翻页由组件内部处理，服务端模式下自动重新调 `OnQueryAsync`。

## 7. 选择（`Table.razor.Checkbox.cs` + `Edit.cs`）

| 参数 | 类型 | 说明 | 来源 |
|---|---|---|---|
| `IsMultipleSelect` | `bool` | 多选模式（开启显示复选列；**非 ShowRowCheckbox**） | `Checkbox.cs:73` |
| `ShowRowCheckboxCallback` | `Func<TItem, bool>?` | 按行控制是否显示复选框 | `Checkbox.cs:96` |
| `ShowCheckboxText` | `bool` | 显示复选框文字 | `Checkbox.cs:80` |
| `SelectedRows` | `List<TItem>` | 选中行集合 | `Edit.cs:24` |
| `SelectedRowsChanged` | `EventCallback<List<TItem>>` | 选中行变化 | `Edit.cs:31` |
| `ClickToSelect` | `bool` | 点击行即选中 | `Edit.cs:219` |
| `IsKeepSelectedRows` | `bool` | 翻页保持选择 | `Checkbox.cs:29` |

## 8. 编辑 / 增删改（`Table.razor.Toolbar.cs` + `Edit.cs`）

| 参数 | 类型 | 说明 | 来源 |
|---|---|---|---|
| `EditMode` | `EditMode` | 编辑模式 Popup/EditForm/InCell/Drawer（默认 Popup） | `Edit.cs:61` |
| `ShowExtendButtons` | `bool` | 行内扩展按钮列 | `Toolbar.cs:172` |
| `ShowExtendEditButton` / `ShowExtendDeleteButton` | `bool` | 行内编辑/删除按钮（默认 true） | `Toolbar.cs:221,276` |
| `ShowExtendEditButtonCallback` / `ShowExtendDeleteButtonCallback` | `Func<TItem,bool>?` | 按行控制 | `Toolbar.cs:228,283` |
| `IsExtendButtonsInRowHeader` | `bool` | 扩展按钮在行首 | `Toolbar.cs:200` |
| `ExtendButtonColumnWidth` | `int` | 扩展列宽（默认 130） | `Toolbar.cs:207` |
| `FixedExtendButtonsColumn` | `bool` | 固定扩展按钮列 | `Toolbar.cs:303` |
| `DoubleClickToEdit` | `bool` | 双击编辑 | `Edit.cs:226` |
| `InsertRowMode` | `InsertRowMode` | 新建行位置 First/Last（默认 Last） | `Edit.cs:38` |
| `OnAddAsync` | `Func<Task<TItem>>?` | 新建回调（补默认值） | `Toolbar.cs:419` |
| `OnEditAsync` | `Func<TItem, Task>?` | 编辑回调 | `Toolbar.cs:426` |
| `OnSaveAsync` | `Func<TItem, ItemChangedType, Task<bool>>?` | 保存回调（`ItemChangedType` Add/Update） | `Toolbar.cs:433` |
| `OnDeleteAsync` | `Func<IEnumerable<TItem>, Task<bool>>?` | 删除回调 | `Toolbar.cs:440` |
| `OnExportAsync` | `Func<ITableExportDataContext<TItem>, Task<bool>>?` | 导出回调 | `Toolbar.cs:447` |
| `OnAfterSaveAsync` / `OnAfterDeleteAsync` | `Func<...Task>?` | 保存/删除后回调 | `Edit.cs:117,124` |
| `EditTemplate` | `RenderFragment<TItem>?` | 编辑模板 | `Edit.cs:162` |
| `EditDialogCloseAsync` | `Func<TItem, bool, Task>?` | 编辑弹窗关闭前回调 | `Toolbar.cs:889` |
| `IsTracking` | `bool` | Tracking 模式 | `Table.razor.cs:510` |

> rbac 实战**不使用 Table 内置编辑**（`ShowExtendEditButton="false" ShowExtendDeleteButton="false"`），而是 `RowButtonTemplate` 放自定义编辑按钮 + `TableCellPopConfirmButton` 删除，弹 `<Modal>` 子组件。见 [crud-recipes.md](crud-recipes.md)。

## 9. Toolbar（`Table.razor.Toolbar.cs`）

Table 自带顶部工具栏：

| 参数 | 类型 | 说明 | 来源 |
|---|---|---|---|
| `ShowToolbar` | `bool` | 显示工具栏 | `:373` |
| `ShowDefaultButtons` | `bool` | 默认按钮组（默认 true） | `:32` |
| `ShowAddButton` / `ShowEditButton` / `ShowDeleteButton` | `bool` | 各默认按钮 | `:39,46,53` |
| `ShowRefresh` | `bool` | 刷新按钮（默认 true） | `:331` |
| `ShowColumnList` | `bool` | 列选择下拉 | `:345` |
| `ShowExportButton` | `bool` | 导出按钮 | `:60` |
| `ToolbarTemplate` | `RenderFragment?` | 工具栏中间模板 | `:380` |

**独立组件 `TableToolbar<TItem>`**（`TableToolbar.razor.cs:17`，放 Table 外）：自定义工具栏按钮，参数 `OnGetSelectedRows: Func<IEnumerable<TItem>>`（必需）、`ChildContent`。子按钮：`TableToolbarButton<TItem>`、`TableToolbarPopConfirmButton<TItem>`。

## 10. 模板（`Table.razor.cs` + 各 partial）

| 参数 | 类型 | 说明 | 来源 |
|---|---|---|---|
| `TableColumns` | `RenderFragment<TItem>?` | **列声明容器** | `:623` |
| `RowTemplate` | `RenderFragment<TableRowContext<TItem>>?` | 整行模板 | `:609` |
| `DetailRowTemplate` | `RenderFragment<TItem>?` | 明细行模板 | `:602` |
| `TableFooter` / `FooterTemplate` | `RenderFragment<IEnumerable<TItem>>?` | 表脚 | `:630,637` |
| `MultiHeaderTemplate` | `RenderFragment?` | 多表头 | `:284` |
| `EmptyTemplate` / `EmptyText` / `EmptyImage` | — | 无数据 | `:665,672,679` |
| `LoadingTemplate` | `RenderFragment?` | 加载模板 | `:40` |
| `BeforeRowButtonTemplate` / `RowButtonTemplate` | `RenderFragment<TItem>?` | 行内按钮（rbac 用 RowButtonTemplate 放操作列） | `Edit.cs:169,176` |
| `EditFooterTemplate` | `RenderFragment<TItem>?` | 编辑弹窗 Footer | `Toolbar.cs:882` |

行内操作按钮（rbac 实战用）：`<TableCellButton>`、`<TableCellPopConfirmButton Content="确认?" OnConfirm=..>`。

## 11. 其它常用

| 参数 | 类型 | 说明 | 来源 |
|---|---|---|---|
| `RenderMode` | `TableRenderMode` | Auto/Table/CardView | `Edit.cs:68` |
| `ScrollMode` | `ScrollMode` | None/Virtual（虚拟滚动强制 IsFixedHeader+不分页，不支持树形） | `Table.razor.cs:480` |
| `IsFixedHeader` / `Height` | `bool` / `int?` | 固定表头/高度 px | `:270,263` |
| `ShowSkeleton` / `ShowLoading` | `bool` | 骨架屏/加载动画 | `Toolbar.cs:17`, `Edit.cs:240` |
| `AllowDragColumn` / `AllowResizing` | `bool` | 拖拽列/调列宽 | `:1935,714` |
| `IsStriped` / `IsBordered` | `bool` | 斑马线/边框 | `:732,746` |
| `TableSize` | `TableSize` | Normal/Compact | `:658` |
| `OnClickRowCallback` / `OnDoubleClickRowCallback` | `Func<TItem,Task>?` | 单/双击行 | `:774,781` |

刷新表格：`await TableRef.QueryAsync();`（实例方法）。

## 最小示例

### 客户端绑定

```razor
<Table TItem="Foo" Items="@Foos" IsStriped="true" IsBordered="true">
    <TableColumns>
        <TableColumn @bind-Field="context.Id" Width="80" />
        <TableColumn @bind-Field="context.Name" Text="姓名" />
        <TableColumn @bind-Field="context.CreatedAt" FormatString="yyyy-MM-dd" />
    </TableColumns>
</Table>
```

### 服务端分页 + 排序 + 多选 + 增删改

```razor
<Table TItem="Foo" @ref="_table"
       OnQueryAsync="OnQueryAsync" IsPagination="true" PageItemsSource="new[] {10,20,50}"
       IsMultipleSelect="true" ShowExtendButtons="true"
       OnSaveAsync="OnSaveAsync" OnDeleteAsync="OnDeleteAsync" OnAddAsync="OnAddAsync"
       ShowSearch="true" ShowToolbar="true" ShowLineNo="true">
    <TableColumns>
        <TableColumn @bind-Field="context.Id" Width="80" Readonly="true" />
        <TableColumn @bind-Field="context.Name" Text="姓名" Sortable="true"
                     DefaultSort="true" DefaultSortOrder="SortOrder.Asc" Searchable="true" />
    </TableColumns>
</Table>

@code {
    private Table<Foo>? _table;
    private static readonly List<Foo> _all = Enumerable.Range(1, 500)
        .Select(i => new Foo { Id = i, Name = $"用户{i}" }).ToList();

    private Task<QueryData<Foo>> OnQueryAsync(QueryPageOptions opt)
    {
        var q = _all.AsEnumerable();
        if (!string.IsNullOrEmpty(opt.SearchText))
            q = q.Where(x => x.Name.Contains(opt.SearchText));
        if (opt.SortOrder != SortOrder.Unset && !string.IsNullOrEmpty(opt.SortName))
            q = opt.SortOrder == SortOrder.Asc
                ? q.OrderBy(x => typeof(Foo).GetProperty(opt.SortName!)!.GetValue(x))
                : q.OrderByDescending(x => typeof(Foo).GetProperty(opt.SortName!)!.GetValue(x));
        var total = q.Count();
        var page = q.Skip((opt.PageIndex - 1) * opt.PageItems).Take(opt.PageItems).ToList();
        return Task.FromResult(new QueryData<Foo> { Items = page, TotalCount = total,
            IsSearch = true, IsSorted = true, IsFiltered = true });
    }

    private Task<Foo> OnAddAsync() => Task.FromResult(new Foo());
    private Task<bool> OnSaveAsync(Foo item, ItemChangedType type)
    {
        _all.RemoveAll(x => x.Id == item.Id); _all.Insert(0, item); return Task.FromResult(true);
    }
    private Task<bool> OnDeleteAsync(IEnumerable<Foo> rows)
    {
        _all.RemoveAll(x => rows.Any(r => r.Id == x.Id)); return Task.FromResult(true);
    }

    public class Foo { public int Id { get; set; } public string? Name { get; set; } }
}
```
