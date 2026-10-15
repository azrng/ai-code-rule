# 高频组件参数速查

事实来源：`src/MudBlazor/Components/**` + `src/MudBlazor.Docs/Pages/Components/**/Examples/*.razor`。默认值出自 `[Parameter]` + `<remarks>`，均为 v9.11.x 实测。

## 公共枚举

| 枚举 | 值 |
| --- | --- |
| `Variant` | `Text` / `Filled` / `Outlined`（**没有 Fill 参数**） |
| `Size` | `Small` / `Medium` / `Large` |
| `Color` | `Default, Primary, Secondary, Tertiary, Info, Success, Warning, Error, Dark, Transparent, Inherit, Surface` |
| `Severity` | `Normal, Info, Success, Warning, Error` |
| `InputType` | `Text, Password, Email, Number, Search, Telephone(tel), Url, Date, DateTimeLocal, ...` |
| `Adornment` | `None` / `Start` / `End` |
| `Margin` | `None` / `Dense` / `Normal` |

## 按钮

```razor
<MudButton Variant="Variant.Filled" Color="Color.Primary" StartIcon="@Icons.Material.Filled.Save">保存</MudButton>
<MudButton Variant="Variant.Outlined" EndIcon="@Icons.Material.Filled.Send">发送</MudButton>
<MudIconButton Icon="@Icons.Material.Filled.Delete" Color="Color.Error" Variant="Variant.Outlined" Size="Size.Small" />
<MudFab Color="Color.Primary" StartIcon="@Icons.Material.Filled.Add" Label="新建" />
```

| 参数 | 默认值 | 说明 |
| --- | --- | --- |
| `Variant` | **Text**（Fab 为 Filled） | MudIconButton 仅在 Variant≠Text 时有按钮底色 |
| `Color` | Default | |
| `Size` | Medium（Fab 为 Large） | |
| `StartIcon` / `EndIcon` | null | SVG path（`Icons.Material.Filled.Xxx`） |
| `FullWidth` | false | MudButton 独有 |
| `Href` / `Target` | null | 设置后渲染 `<a>`；Disabled 时回退 `<button>`（Href 失效） |
| `Ripple` / `DropShadow` | true / true | |
| `ButtonType` | Button | Submit 在 MudForm 内禁用（见 forms-and-validation.md） |
| `OnClick` | — | `EventCallback<MouseEventArgs>` |

## MudTextField\<T\>

```razor
<MudTextField @bind-Value="_name" Label="姓名" Required="true" For="() => _model.Name" />
<MudTextField @bind-Value="_pwd" Label="密码" InputType="InputType.Password"
              Adornment="Adornment.End" AdornmentIcon="@_pwdIcon" OnAdornmentClick="TogglePwd" />
<MudTextField @bind-Value="_search" Label="搜索" Immediate="true" Adornment="Adornment.End"
              AdornmentIcon="@Icons.Material.Filled.Search" Clearable="true" />
<MudTextField @bind-Value="_desc" Label="描述" Lines="3" />   @* Lines>1 = textarea *@
```

| 参数 | 默认值 | 说明 |
| --- | --- | --- |
| `@bind-Value` | — | 泛型 T 自动推断 |
| `Label` | null | |
| `For` | null | 校验目标表达式（表单校验见 forms-and-validation.md） |
| `InputType` | Text | |
| `Variant` | Text | Text/Filled/Outlined |
| `Margin` | None | Dense 紧凑 |
| `Immediate` | **false** | false=Enter/失焦才更新；搜索框必须显式开 |
| `DebounceInterval` | 0 (double) | >0 隐式启用即时更新；`OnDebounceIntervalElapsed` 回调 |
| `Lines` | 1 | >1 渲染 textarea；`Sizing="InputSizing.Auto"` + `MaxLines` 自适应高度 |
| `Clearable` | false | |
| `HelperText` / `Counter` / `MaxLength` | null / null / 524288 | |
| `Adornment` / `AdornmentIcon` / `AdornmentText` | None/null/null | **AdornmentText 优先于 Icon** |
| `Mask` | null | PatternMask/RegexMask；设置后 Lines/Sizing 失效 |

## MudSelect\<T\>

```razor
@* 单选 *@
<MudSelect @bind-Value="_city" Label="城市" Clearable="true">
    <MudSelectItem Value="@("北京")">北京</MudSelectItem>
    <MudSelectItem Value="@("上海")">上海</MudSelectItem>
</MudSelect>

@* 多选：绑 SelectedValues *@
<MudSelect T="string" Label="标签" MultiSelection="true" @bind-SelectedValues="_tags">
    @foreach (var tag in _allTags)
    {
        <MudSelectItem Value="@tag">@tag</MudSelectItem>
    }
</MudSelect>
@code { private IReadOnlyCollection<string> _tags = []; }
```

- 单选绑 `@bind-Value`、多选绑 `@bind-SelectedValues`（`IReadOnlyCollection<T>`）；**不存在 `SelectedItem` 参数**
- `ToStringFunc`：复杂类型选项文本；`Comparer`：值相等比较（枚举/复杂类型建议重写 Equals）
- `Strict`(false)：true=值必须在选项内；`SelectAll`(false)：多选全选行；`MaxHeight`(300)
- `MudSelectItem` 的 T 必须与 `MudSelect<T>` 一致

## MudAutocomplete\<T\>

```razor
<MudAutocomplete T="Person" @bind-Value="_person" SearchFunc="SearchPersons"
                 MinCharacters="1" DebounceInterval="300" ToStringFunc="p => p?.Name"
                 ResetValueOnEmptyText="true" Clearable="true" />

@code {
    private Person? _person;
    private async Task<IEnumerable<Person?>> SearchPersons(string text, CancellationToken token)
    {
        return await Task.Run(() => _all.Where(p => p.Name.Contains(text, StringComparison.OrdinalIgnoreCase)), token);
    }
}
```

| 参数 | 默认值 | 说明 |
| --- | --- | --- |
| `SearchFunc` | — | `Func<string?, CancellationToken, Task<IEnumerable<T>>?>`；**双参签名** |
| `MinCharacters` | 0 | |
| `DebounceInterval` | **100 (int)** | 注意与 TextField 的 double 类型不同 |
| `MaxItems` | 10 | null=全部 |
| `CoerceText` | **true** | 选中后覆写文本；false 允许保留半截输入 |
| `CoerceValue` | false | true=无匹配输入也写 Value（配合校验报错） |
| `Strict` | true | |
| `ResetValueOnEmptyText` | false | |
| `ItemTemplate` | null | 自定义下拉项 |

空文本返回空集合则下拉不展开；返回 `Task<IEnumerable<T>>` 不是列表。

## 布尔与选择

```razor
<MudCheckBox @bind-Value="_agree" Label="同意协议" Color="Color.Primary" T="bool" />
<MudSwitch @bind-Value="_enabled" Label="启用" Color="Color.Primary" T="bool" />
<MudRadioGroup @bind-Value="_pay">
    <MudRadio Value="@("alipay")">支付宝</MudRadio>
    <MudRadio Value="@("wechat")">微信</MudRadio>
</MudRadioGroup>
<MudRating @bind-SelectedValue="_score" MaxValue="5" />
```

- `MudRadio` 必须在 `MudRadioGroup<T>` 内且泛型一致；Radio 不参与 Form 校验
- `MudCheckBox` 三态：`T="bool?"` + `TriState="true"`
- `MudRating` 绑定名是 `SelectedValue`（不是 Value）

## 日期选择

```razor
<MudDatePicker @bind-Date="_date" Label="开始日期" DateFormat="yyyy-MM-dd" />
<MudDatePicker @bind-Date="_date" Editable="true" MinDate="_min" MaxDate="_max" />
<MudDateRangePicker @bind-DateRange="_range" PlaceholderStart="开始" PlaceholderEnd="结束" />
@code {
    private DateTime? _date = DateTime.Today;
    private DateRange _range = new(DateTime.Today, DateTime.Today.AddDays(7));
}
```

- 单选绑 `Date`、区间绑 `DateRange`（`DateRange(start, end)`，两者可 null）
- `DateFormat="yyyy-MM-dd"`；字面 `/` 要转义写 `'/'`
- `Editable`(false)：v9 默认只读选择，手输必须显式开
- `AutoClose`、`DisplayMonths`(1；Range 默认 2)、`FirstDayOfWeek`、`ShowWeekNumbers`

## MudNumericField\<T\>

```razor
<MudNumericField @bind-Value="_age" Label="年龄" Min="0" Max="150" />
<MudNumericField @bind-Value="_price" Label="价格" Step="0.5M" Variant="Variant.Outlined" />
```

- `Min`/`Max` 默认是**类型极值**（不是 0/100）；`Step` 默认 1
- 支持 sbyte…double/decimal 及可空版本；`HideSpinButtons`(false)、`Culture`/`Format`

## MudFileUpload\<T\>

```razor
@* 单文件 *@
<MudFileUpload T="IBrowserFile" Accept=".pdf,.xlsx" @bind-Files="_file" />
@* 多文件 + 自定义按钮 *@
<MudFileUpload T="IReadOnlyList<IBrowserFile>" Accept=".png,.jpg" AppendMultipleFiles
               MaximumFileCount="20" @bind-Files="_images">
    <CustomContent>
        <MudButton Variant="Variant.Filled" StartIcon="@Icons.Material.Filled.CloudUpload"
                   OnClick="@context.OpenFilePickerAsync">选择图片</MudButton>
    </CustomContent>
    <SelectedTemplate />
</MudFileUpload>
```

- 类型限制参数是 **`Accept`**（不存在 `FileTypes`）
- 多文件 T 必须是 `IReadOnlyList<IBrowserFile>`；`AppendMultipleFiles` 只对它生效
- `OnFilesChanged`：`EventCallback<InputFileChangeEventArgs>`；`MaxFileSize`(long?) 超限拒绝；`DragAndDrop="true"` 拖放区
- 上传动作仍用 `IBrowserFile.OpenReadStreamAsync()`（标准 InputFile 语义）

## 展示与反馈

```razor
<MudAlert Severity="Severity.Success" Dense="true">操作成功</MudAlert>
<MudChip T="string" Color="Color.Primary" Size="Size.Small">标签</MudChip>
<MudChipSet T="string" SelectionMode="SelectionMode.MultiSelection" @bind-SelectedValues="_sel" CheckMark="true">
    <MudChip Text="a" Value="a" />
    <MudChip Text="b" Value="b" />
</MudChipSet>
<MudBadge Content="3" Overlap="true" Color="Color.Error"><MudIcon Icon="@Icons.Material.Filled.Mail" /></MudBadge>
<MudTooltip Text="删除"><MudIconButton Icon="@Icons.Material.Filled.Delete" /></MudTooltip>
<MudProgressLinear Value="75" Color="Color.Primary" />
<MudProgressCircular Indeterminate="true" Size="Size.Small" />
<MudSkeleton Height="40px" />
```

- `MudAlert`：`Severity`(Normal)、`Variant`(**Text**)、`ShowCloseIcon`(false)、`NoIcon`
- v9 Chip 重组：选择/互斥在 `MudChipSet<T>.SelectionMode` 枚举（`SingleSelection`/`MultiSelection`/`ToggleSelection`），不是 `MultiSelection` 布尔；Chip 的 `Value` 参与选中集合
- `MudBadge`：`Content`(object)、`Dot`、`Overlap`、`Max`(99)、`Bordered`
- `MudTooltip`：`Placement`(Bottom)、`Visible` 默认 **false**（`@bind-Visible` 编程控制）
- `MudProgressLinear`：`Value`(0-100)、`Indeterminate`、`Striped`、`BufferValue`

## 导航

```razor
@* 菜单 *@
<MudMenu Label="操作" Icon="@Icons.Material.Filled.MoreVert">
    <MudMenuItem Label="编辑" OnClick="Edit" />
    <MudMenuItem Label="删除" OnClick="Delete" />
</MudMenu>

@* 侧边导航 *@
<MudNavMenu>
    <MudNavLink Href="/" Match="NavLinkMatch.All" Icon="@Icons.Material.Filled.Home">首页</MudNavLink>
    <MudNavGroup Title="设置" Expanded="true" Icon="@Icons.Material.Filled.Settings">
        <MudNavLink Href="/users">用户</MudNavLink>
    </MudNavGroup>
</MudNavMenu>

@* 面包屑 *@
<MudBreadcrumbs Items="_items" />
@code { private List<BreadcrumbItem> _items = [new("首页", "/"), new("用户", "/users", disabled: true)]; }

@* 分页器（独立组件，DataGrid 的分页见 data-grid.md） *@
<MudPagination Count="10" @bind-Selected="_page" Color="Color.Primary" Rectangular="true" />
```

- `MudNavLink.Match` 默认 `NavLinkMatch.Prefix`
- `BreadcrumbItem` 是 record：`new(text, href, disabled, icon)`
- `MudMenu`：`ActivatorContent` 自定义激活器；`PositionAtCursor="true"` 右键菜单

## 列表与轮播

```razor
<MudPaper Width="300px">
    <MudList T="string" @bind-SelectedValue="_picked">
        <MudListSubheader>收件箱</MudListSubheader>
        <MudListItem Value="inbox" Text="收件" Icon="@Icons.Material.Filled.Inbox" />
        <MudListItem Value="trash" Text="垃圾箱" SecondaryText="已删除邮件" />
    </MudList>
</MudPaper>
```

- `MudList<T>` **没有 Items/ItemTemplate 数据绑定参数**（静态子项声明）；T 是选中值类型
- 多选：`SelectionMode="SelectionMode.MultiSelection"` + `@bind-SelectedValues`
- 嵌套：`MudListItem` 内 `<NestedList>`；分组标题用 `MudListSubheader`（**不存在 `MudSubheader`**）
- `MudCarousel<T>`：声明式 `MudCarouselItem` 或 `ItemsSource` + `ItemTemplate`；`@bind-SelectedIndex`、`AutoCycle`(true)、`AutoCycleTime`(5s)

## 图标 MudIcon

```razor
<MudIcon Icon="@Icons.Material.Filled.ThumbUp" Color="Color.Primary" />
<MudIcon Icon="@Icons.Material.Outlined.Settings" />
<MudIcon Icon="fas fa-database" />                     @* Font Awesome *@
<MudIcon Icon="@($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 24 24\"><path d=\"{_path}\"/></svg>")" />
```

- 图标集：`Icons.Material.{Filled,Outlined,Rounded,Sharp,TwoTone}.Xxx`、`Icons.Custom.*`；以 `<` 开头的字符串按内联 SVG 渲染
- `Size`(Medium)、`Color`(**Inherit**)、`ViewBox`("0 0 24 24")

## 数据表格（MudTable / MudSimpleTable）

> 新项目优先 `MudDataGrid`（见 data-grid.md）；这里仅速查。

- `MudSimpleTable`：纯样式壳，手写 `<thead>/<tbody>`；`Hover`、`Striped`、`Dense`、`Bordered`、`FixedHeader`（需配 `Style="height:300px"`）
- `MudTable<T>`：`Items` 或 `ServerData(Func<TableState, CancellationToken, Task<TableData<T>>>)`；列是 `<HeaderContent>` + `<MudTh>` / `<RowTemplate>` + `<MudTd DataLabel=>` 手写（**没有 PropertyColumn/TemplateColumn**）；过滤用 `Filter="new Func<T,bool>(FilterFunc)"`（DataGrid 里叫 QuickFilter）；行点击事件叫 `OnRowClick`（DataGrid 叫 `RowClick`）；多选 `MultiSelection` + `SelectedItems`(HashSet\<T\>)；`Breakpoint`(Xs) 小屏切卡片布局；分页 `<PagerContent><MudTablePager /></PagerContent>`
- `MudTablePager` **没有 `Size` 参数**（写 `Size=` 触发 MUD0002）；真实参数是 `PageSizeOptions`([10,25,50,100])、`RowsPerPageString`、`InfoFormat`、`HideRowsPerPage` / `HidePageNumber` / `HidePagination`、`HorizontalAlignment`、`AllItemsText`、`FirstIcon`/`BeforeIcon`/`NextIcon`/`LastIcon`
