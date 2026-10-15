# MudDataGrid 完全指南

事实来源：`src/MudBlazor/Components/DataGrid/`（MudDataGrid.razor.cs、Column.razor.cs、TemplateColumn.cs、PropertyColumn.cs、SelectColumn.razor.cs、MudDataGridPager.razor.cs）+ `src/MudBlazor.Docs/Pages/Components/DataGrid/Examples/*.razor` + `src/MudBlazor.UnitTests/Components/DataGrid*.cs`。

## 最小示例

```razor
<MudDataGrid T="Person" Items="@_people" Filterable="true" Hover="true" Dense="true" Striped="true">
    <Columns>
        <PropertyColumn Property="x => x.Name" Title="姓名" />
        <PropertyColumn Property="x => x.Age" Title="年龄" Sortable="false" />
        <TemplateColumn Title="操作">
            <CellTemplate>
                <MudIconButton Icon="@Icons.Material.Filled.Edit" Size="Size.Small"
                               OnClick="@(() => Edit(context.Item))" />
            </CellTemplate>
        </TemplateColumn>
    </Columns>
</MudDataGrid>

@code {
    private List<Person> _people = [];
}
```

## 核心参数（v9 实名清单）

### 数据源（三者互斥）

| 参数 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `Items` | `IEnumerable<T>?` | null | 客户端数据 |
| `ServerData` | `Func<GridState<T>, CancellationToken, Task<GridData<T>>>?` | null | **双参签名**（含 CancellationToken）；过滤排序需自己执行 |
| `VirtualizeServerData` | `Func<GridStateVirtualize<T>, CancellationToken, Task<GridData<T>>>?` | null | 滚动加载，无分页器 |
| `QuickFilter` | `Func<T,bool>?` | null | 客户端快速过滤；**与 ServerData/VirtualizeServerData 同设抛异常** |

同时设置多个数据源抛 `InvalidOperationException: ...can only accept one item source`。

### 外观

| 参数 | 默认值 | 说明 |
| --- | --- | --- |
| `Hover` / `Dense` / `Striped` / `Bordered` | false | |
| `Elevation` | 1 | 0–25 |
| `Square` / `Outlined` | false | |
| `FixedHeader` / `FixedFooter` | false | |
| `Height` | null | `Virtualize="true"` 时**必须**设置 |
| `Hideable` | false | 列可隐藏；**不存在 `HideHeaders`** |
| `ShowColumnOptions` | true | 列头选项菜单 |
| `ColumnResizeMode` | None | ResizeMode 枚举 |
| `RowClassFunc` / `RowStyleFunc` | null | `Func<T,int,string>`；**不存在列 `DataStyle`，单元格样式是列上 `CellStyle`/`CellStyleFunc`** |

### 排序 / 过滤 / 分页

| 参数 | 默认值 | 说明 |
| --- | --- | --- |
| `SortMode` | SortMode.Multiple | None/Single/Multiple |
| `AllowUnsorted` | false | |
| `Filterable` | false | 列过滤 UI 开关 |
| `FilterMode` | Simple | Simple/ColumnFilterMenu/ColumnFilterRow |
| `FilterCaseSensitivity` | Default | |
| `RowsPerPage` | 10 | 需 `PagerContent` 放分页器才可见；`@bind-RowsPerPage` |
| `Virtualize` | false | 配 `ItemSize`(50f)、`OverscanCount`(3) |

### 选择 / 分组 / 编辑

| 参数 | 默认值 | 说明 |
| --- | --- | --- |
| `MultiSelection` | false | |
| `SelectedItems` | null | `HashSet<T>`；**按引用变更检测，就地 Clear() 不触发刷新，须赋新 HashSet** |
| `SelectedItem` | null | 单选绑定 |
| `SelectOnRowClick` | true | |
| `Groupable` | false | 配列 `Grouping="true"`；`GroupTemplate` 自定义组头 |
| `ReadOnly` | **true** | 编辑必须显式 `ReadOnly="false"`（注意 MudTable 默认相反） |
| `EditMode` | null（即 Cell） | Cell/Form(弹窗)/Inline |
| `EditTrigger` | Manual | OnRowClick/Manual |
| `CanCancelEdit` | true | |
| `EditDialogOptions` | null | Form 模式弹窗选项 |

### 事件（真实签名）

| 事件 | 签名 |
| --- | --- |
| `RowClick` | `EventCallback<DataGridRowClickEventArgs<T>>`（**不是 OnRowClick**，那是 MudTable 的） |
| `SelectedItemsChanged` | `EventCallback<HashSet<T>?>` |
| `StartedEditingItem` / `CanceledEditingItem` | `EventCallback<T>` |
| `CommittedItemChanges` | **`Func<T, Task<DataGridEditFormAction>>?`**（不是 EventCallback！返回 Close/KeepOpen） |
| `CommittedItemChanged` | `EventCallback<T>`（提交落盘后触发） |
| `SortChanged` | `EventCallback<Dictionary<string, SortDefinition<T>>>` |
| `FilterChanged` | `EventCallback<IReadOnlyCollection<IFilterDefinition<T>>>` |

**不存在**：`RowDoubleClick`、`LoadGroupsCount`。

## 列体系

三列基类都是 `Column<T>`，放在 `<Columns>` 内。

### PropertyColumn\<T, P\>

```razor
<PropertyColumn Property="x => x.Name" Title="姓名" Sortable="true" Filterable="true" Editable="true" />
<PropertyColumn Property="x => x.Salary" Title="薪资" Format="C" />
<PropertyColumn Property="x => x.Created" Title="创建时间" Format="yyyy-MM-dd" />
```

- `Property`：编译期强类型表达式（动态表达式 `x => x.A * x.B` 也允许，生成 GUID 列名，`SetSortAsync` 联动失效）
- `Title`、`Sortable`(随网格)、`Filterable`、`Editable`、`Hideable`、`Groupable`、`Resizable`
- `Format`：标准/自定义格式字符串
- 单元格样式：`CellStyle` / `CellStyleFunc(Func<T,string>)` / `CellClass` / `CellClassFunc`
- `Grouping`(bool) + `AggregateDefinition`：`new AggregateDefinition<T> { Type = AggregateType.Avg, NumberFunc = x => x.Age }`

### TemplateColumn\<T\>

```razor
<TemplateColumn Title="状态" Sortable="false">
    <CellTemplate>
        <MudChip T="string" Size="Size.Small">@context.Item.Status</MudChip>
    </CellTemplate>
</TemplateColumn>
```

- **内容必须包 `<CellTemplate>`**，`context.Item` 取行数据；直接放子内容触发 MUD0002 分析器警告（Illegal Attribute 'ChildContent'）
- 默认 `Sortable=false`、`Filterable=false`（与 PropertyColumn 相反），需要时显式开
- `EditTemplate`：编辑态模板（见行内编辑）

### SelectColumn

```razor
<Columns>
    <SelectColumn T="Person" />
    ...
</Columns>
```

配 `MultiSelection="true"` + `@bind-SelectedItems`；表头复选框全选。

### MudDataGridPager

```razor
<PagerContent>
    <MudDataGridPager T="Person" PageSizeOptions="[10, 25, 50]" RowsPerPageString="每页 {0} 条" />
</PagerContent>
```

- **泛型组件，`T=` 必填**（漏写编译错误）
- `PageSizeOptions`(默认 [10,20,50,100])、`RowsPerPageString`、`InfoFormat`、`DisableRowsPerPagePicker`

## 服务端分页完整模式（官方示例裁剪）

```razor
<MudDataGrid @ref="_grid" T="Element" ServerData="ServerReload" Filterable="true" Hover="true">
    <Columns>
        <PropertyColumn Property="x => x.Name" Title="名称" />
    </Columns>
    <PagerContent><MudDataGridPager T="Element" PageSizeOptions="[10, 25]" /></PagerContent>
</MudDataGrid>

@code {
    private MudDataGrid<Element> _grid = null!;

    private async Task<GridData<Element>> ServerReload(GridState<Element> state, CancellationToken token)
    {
        var data = await _http.GetFromJsonAsync<List<Element>>("api/elements", token);

        // 过滤自己执行
        foreach (var filter in state.FilterDefinitions)
            data = data.Where(filter.GenerateFilterFunction()).ToList();

        // 排序自己执行
        var sort = state.SortDefinitions.FirstOrDefault();
        if (sort.Value is not null)
            data = sort.Value.Descending
                ? data.OrderByDescending(x => x.Name).ToList()
                : data.OrderBy(x => x.Name).ToList();

        return new GridData<Element>
        {
            Items = data.Skip(state.Page * state.PageSize).Take(state.PageSize),
            TotalItems = data.Count
        };
    }

    private Task ReloadAsync() => _grid.ReloadServerData();
}
```

要点：
- `GridState<T>`：`Page`(0 起)、`PageSize`、`SortDefinitions: ICollection<SortDefinition<T>>`、`FilterDefinitions: ICollection<IFilterDefinition<T>>`
- `SortDefinition<T>` 是 record：`(string SortBy, bool Descending, int Index, Func<T,object?> SortFunc, ...)`
- 过滤谓词生成：`filter.GenerateFilterFunction()`；排序辅助扩展 `items.OrderBySortDefinitions(state)`（`DataGridExtensions.cs`）
- 搜索/增删改后调 `_grid.ReloadServerData()` 刷新

## 行内编辑

### Inline 模式（手动触发，官方示例裁剪）

```razor
<MudDataGrid T="Element" Items="@_elements" ReadOnly="false"
             EditMode="DataGridEditMode.Inline" EditTrigger="DataGridEditTrigger.Manual"
             CommittedItemChanges="CommitItemAsync" Bordered Dense Hover>
    <Columns>
        <PropertyColumn Property="x => x.Name" Title="名称" Editable="true">
            <EditTemplate>
                <MudTextField @bind-Value="context.Item.Name" Margin="Margin.Dense" Variant="Variant.Text" />
            </EditTemplate>
        </PropertyColumn>
        <TemplateColumn>
            <CellTemplate>
                @if (context.IsEditing)
                {
                    <MudIconButton Icon="@Icons.Material.Filled.Check" Size="Size.Small"
                                   OnClick="@context.Actions.CommitEditingItemAsync" />
                    <MudIconButton Icon="@Icons.Material.Filled.Close" Size="Size.Small"
                                   OnClick="@context.Actions.CancelEditingItemAsync" />
                }
                else
                {
                    <MudIconButton Icon="@Icons.Material.Outlined.Edit" Size="Size.Small"
                                   OnClick="@context.Actions.StartEditingItemAsync" />
                }
            </CellTemplate>
        </TemplateColumn>
    </Columns>
</MudDataGrid>

@code {
    private Task<DataGridEditFormAction> CommitItemAsync(Element item)
    {
        if (string.IsNullOrWhiteSpace(item.Name))
            return Task.FromResult(DataGridEditFormAction.KeepOpen);   // 校验失败保持编辑态
        // ... 保存 item
        return Task.FromResult(DataGridEditFormAction.Close);
    }
}
```

### 三种 EditMode 语义

- `Cell`：单元格点击即编辑，值变更自动写回并触发 `CommittedItemChanges`；但 EditTemplate 里的自定义控件不自动提交，需自己在值回调里改 `context.Item`
- `Form`：编辑走弹窗；Save 自动触发 `CommittedItemChanges`，返回 `KeepOpen` 可留窗校验
- `Inline`：整行内联，`EditTrigger.Manual` + `context.Actions.StartEditingItemAsync` 手动进入

编辑操作的是**克隆副本**（默认 SystemTextJson 深克隆），Cancel 不落盘；`CommittedItemChanges` 处理器类型必须是 `Func<T, Task<DataGridEditFormAction>>`，写成 EventCallback 或返回 void/bool 都编译不过。

编程进入编辑：`await _grid.SetEditingItemAsync(item)`（Form 模式新增行用）。

## 过滤与排序 API

```csharp
// 清空
await _grid.ClearFiltersAsync();
foreach (var field in _grid.SortDefinitions.Keys.ToList())
    await _grid.RemoveSortAsync(field);

// 程序化过滤（测试代码实证）
await _grid.AddFilterAsync(new FilterDefinition<Element>
{
    Column = _grid.RenderedColumns.First(),
    Operator = FilterOperator.String.Equal,
    Value = "C"
});

// 程序化排序（field 必须等于列 PropertyName 才联动表头）
await _grid.SetSortAsync("Name", SortDirection.Ascending, x => x.Name);
```

- `FilterOperator` 常量组：`String`（contains/equals/starts with...）、`Number`（"=", "!=", ">", ">="...）、`Enum`、`Boolean`、`DateTime`、`Guid`
- `FilterDefinition<T>` 可自定义 `FilterFunction` 谓词短路
- `FilterMode`：Simple（菜单式）/ ColumnFilterMenu / ColumnFilterRow（行内过滤输入）

## MudDataGrid vs MudTable 取舍

| 维度 | MudDataGrid | MudTable |
| --- | --- | --- |
| 列模型 | PropertyColumn/TemplateColumn 强类型 | `<MudTh>/<MudTd>` 手写 |
| 内置过滤 UI | FilterMode 三态 | 无（`Filter` 委托自写） |
| 多列排序 | SortMode.Multiple 默认 | MudTableSortLabel + SortLabel 字符串协议 |
| 分组 / 聚合 / 列选择器 / 列拖拽 | 有 | 无 |
| 行内编辑 | EditMode 三态 | RowEditCommit |
| 虚拟化 | VirtualizeServerData | 无 |
| 默认 ReadOnly | **true** | false |

**新项目默认选 MudDataGrid**；MudTable 适合简单只读分页表或维护存量代码。

互异命名对照（混用最易错）：`QuickFilter`↔`Filter`、`RowClick`↔`OnRowClick`、`GridData<T>`↔`TableData<T>`、`SortDefinition` 字典↔`SortLabel` 字符串、默认 `ReadOnly` true↔false。

## 常见编译/运行错误

1. `Items` + `ServerData` 同设 → `InvalidOperationException`
2. `ServerData` 用单参 lambda → 编译不过（签名含 CancellationToken）
3. `CommittedItemChanges` 写成 EventCallback → 编译不过
4. `MudDataGridPager` 漏 `T=` → 编译不过
5. 列上 `SortIcon` → `[Obsolete(error:true)]` 编译错误
6. `SelectedItems.Clear()` 就地改 → 不刷新（赋新 HashSet）
7. `Virtualize="true"` 不设 `Height` → 布局异常（remarks 明确要求）
8. TemplateColumn 直接放子内容 → MUD0002 警告 + 不渲染
9. `SetSortAsync` field 与列 `PropertyName` 不一致 → 表头排序状态不联动
10. 服务端模式忘执行 FilterDefinitions → 过滤 UI 操作无效果
