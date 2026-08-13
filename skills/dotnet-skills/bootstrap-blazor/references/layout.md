# 布局与展示组件参考

## Layout（`Components/Layout/Layout.razor.cs`）— 后台主壳

| 参数 | 类型 | @bind | 说明 | 来源 |
|---|---|---|---|---|
| `IsCollapsed` | `bool` | ✅ | 侧栏收起 | `:115` |
| `SideWidth` | `string?` | — | 侧栏宽（**非 SideWith**；支持百分比，设 0 关闭） | `:223` |
| `IsFullSide` | `bool` | — | 侧栏占满左侧 | `:238` |
| `IsPage` | `bool` | — | 整页布局 | `:249` |
| `Menus` | `IEnumerable<MenuItem>?` | — | 侧栏菜单 | `:256` |
| `UseTabSet` | `bool` | — | 右侧用 Tab（路由页在 Tab 内渲染） | `:263` |
| `IsFixedTabHeader` | `bool` | — | 固定 Tab 头 | `:270` |
| `IsFixedHeader` / `IsFixedFooter` | `bool` | — | 固定头/脚 | `:298,291` |
| `ShowCollapseBar` | `bool` | — | 收起按钮 | `:305` |
| `ShowFooter` / `ShowGotoTop` | `bool` | — | 显示脚/Goto 顶部 | `:312,319` |
| `ShowSplitBar` | `bool` | — | 分割栏（`ShowSplitebar` 已弃用） | `:171` |
| `IsAccordion` | `bool` | — | 菜单手风琴 | `:129` |
| `TabDefaultUrl` | `string` | — | 默认 Tab 地址 | `:340` |
| `AdditionalAssemblies` | `IEnumerable<Assembly>?` | — | 路由程序集（用 `new[]{ GetType().Assembly }`） | `:527` |
| `ExcludeUrls` | `IEnumerable<string>?` | — | 排除路由 | `:520` |
| `OnClickMenu` | `Func<MenuItem, Task>?` | — | 点击菜单回调 | `:326` |
| `OnCollapsed` | `Func<bool, Task>?` | — | 收起回调 | `:333` |
| `Header`/`Footer`/`Side`/`Main` | `RenderFragment?` | — | 各区模板（`Main` 必填） | `:143,150,164,231` |

> 无 `Logo`/`LogoText`/`Data`/`IsAutoMatch` 参数。Logo 在 `Header` 模板内自行渲染。Layout 另透传 30+ 个 Tab 参数（`TabStyle`/`ShowToolbar`/`ShowTabItemClose` 等，`:27-108,347-438`）。`UseTabSet=true` 时 `Main` 作为 `Tab.Body`。

Layout 内自动用 `<Menu IsVertical="true">` 渲染 `Menus`。

## MenuItem（`Components/Menu/MenuItem.cs`，继承 `NodeItem`）

```csharp
new MenuItem("首页", "/", "fa-solid fa-house")         // (text, url?, icon?)
new MenuItem("用户管理") {
    Icon = "fa-solid fa-users",
    Items = new List<MenuItem> { new("列表", "/users") }   // 子项是 Items，非 Children
}
```

属性：`Text`/`Icon`/`Url`/`Match`(默认 `NavLinkMatch.Prefix`)（`MenuItem.cs:32,38,44`）；继承 `NodeItem`：`Id`/`ParentId`/`CssClass`/`IsDisabled`/`IsActive`/`IsCollapsed`/`Template`（`Misc/NodeItem.cs`）。**无 `Children`、无 `Badge`/`BadgeColor`**。

## Menu（`Components/Menu/Menu.razor.cs`）

| 参数 | 类型 | 说明 | 来源 |
|---|---|---|---|
| `Items` | `IEnumerable<MenuItem>?` | 菜单项（`[NotNull]`） | `:44` |
| `IsVertical` | `bool` | 垂直 | `:80` |
| `IsAccordion` | `bool` | 手风琴 | `:55` |
| `IsCollapsed` | `bool` | 收起（**非 IsCollapse**） | `:73` |
| `IsExpandAll` | `bool` | 全展开 | `:66` |
| `DisableNavigation` | `bool` | 禁用导航（仅触发 OnClick） | `:109` |
| `OnClick` | `Func<MenuItem, Task>?` | 点击（参数 MenuItem） | `:116` |

## Tab（`Components/Tab/Tab.razor.cs`）+ TabItem

| 参数 | 类型 | 说明 | 来源 |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | 放 `<TabItem>`（`Items` 只读，非 Parameter） | `:194,63` |
| `IsBorderCard` / `IsCard` | `bool` | 卡片样式 | `:85,78` |
| `Placement` | `Placement` | 默认 Top | `:120` |
| `ShowClose` | `bool` | 关闭按钮 | `:127` |
| `ShowExtendButtons` | `bool` | 扩展按钮 | `:159` |
| `AllowDrag` | `bool` | 拖拽（**非 IsDraggable**） | `:345` |
| `ClickTabToNavigation` | `bool` | 点击导航 | `:187` |
| `OnClickTabItemAsync` | `Func<TabItem, Task>?` | 点击（**非 OnClick**） | `:250` |
| `OnCloseTabItemAsync` | `Func<TabItem, Task<bool>>?` | 关闭 | `:152` |
| `DefaultUrl` / `AdditionalAssemblies` / `ExcludeUrls` | — | 路由 | `:243,229,236` |
| `Body` | `RenderFragment?` | 主体 | `:222` |
| `TabStyle` | `TabStyle` | 样式 | `:359` |

`TabItem`（`Tab/TabItem.cs`）：`Text`/`Url`(`:19,34`，Url 必填)/`IsActive`/`IsDisabled`/`Closable`(`:41,48,55`, 默认 true)/`Icon`(`:76`)/`HeaderTemplate`(`:26`)/`ChildContent`(`:90`)/`ShowFullScreen`(`:83`)。

## Breadcrumb（`Components/Breadcrumb/Breadcrumb.razor.cs`）

唯一参数 `Value: IEnumerable<BreadcrumbItem>?`（`:20`，`[NotNull]`）。**无 Items/ChildContent/OnClick**。

```razor
<Breadcrumb Value="@Items" />
@code {
    private List<BreadcrumbItem> Items = [new("首页","/"), new("用户","/users"), new("详情")];
}
```

`BreadcrumbItem(text, url?, cssClass?)`（`BreadcrumbItem.cs`）：`Text`/`Url`/`CssClass` 只读。

## Pagination（`Components/Pagination/Pagination.razor.cs`）

| 参数 | 类型 | 说明 | 来源 |
|---|---|---|---|
| `PageIndex` | `int` | 当前页（**不可 @bind**，无 Changed） | `:90` |
| `PageCount` | `int` | 总页数（`[EditorRequired]`） | `:98` |
| `OnPageLinkClick` | `Func<int, Task>?` | 翻页（**非 OnPageClick**） | `:112` |
| `ShowPageInfo` | `bool` | 分页信息（默认 true，**非 ShowTotal**） | `:152` |
| `PageInfoTemplate` / `GotoTemplate` | `RenderFragment?` | 模板（**非 PageLinkTemplate**） | `:159,131` |
| `ShowGotoNavigator` | `bool` | Goto 跳转 | `:138` |
| `MaxPageLinkCount` | `int` | 页码数（默认 5） | `:105` |
| `Alignment` | `Alignment` | 默认 Right | `:55` |

## Step（`Components/Step/Step.razor.cs`）+ StepItem + StepOption

| 参数 | 类型 | 说明 | 来源 |
|---|---|---|---|
| `Items` | `List<StepOption>?` | 步骤（**非 Steps**，`[NotNull]`） | `:20` |
| `IsVertical` | `bool` | 垂直 | `:27` |
| `StepIndex` | `int` | 当前步骤（默认 0） | `:34` |
| `ChildContent` | `RenderFragment?` | 放 `<StepItem>` | `:41` |
| `FinishedTemplate` | `RenderFragment?` | 完成模板 | `:48` |
| `OnFinishedCallback` | `Func<Task>?` | 完成回调（**非 OnClick**） | `:55` |

方法：`Next()`/`Prev()`/`SetStepIndex(int)`/`Reset()`。**无 `IsCompleted`**（靠 `StepIndex` 索引判定）。
`StepItem`：`Text`/`Title`/`Icon`/`FinishedIcon`/`Description`(`:19,26,33,40,47`)、`HeaderTemplate`/`TitleTemplate`/`ChildContent`(`:54,61,68`)。

## Card / Collapse / Row / Divider

**Card**（`Card/Card.razor.cs`）：`HeaderText`(`:35`)、`HeaderTemplate`/`BodyTemplate`/`FooterTemplate`(`:42,49,56`)、`Color`(`:63`)、`IsCenter`(`:70`)、`IsCollapsible`(`:77`)、`Collapsed`(`:84`, @bind)、`IsShadow`(`:98`)。

**Collapse**（`Collapse/Collapse.razor.cs`）：`IsAccordion`(`:58`)、`CollapseItems`(`:65`, RenderFragment)、`OnCollapseChanged`(`:72`)。
`CollapseItem`：`Text`(`:19`)、`IsCollapsed`(`:26`, 默认 true)、`Icon`(`:33`)、`TitleColor`(`:40`)、`ChildContent`/`HeaderTemplate`(`:54,68`)。

**Row**（`Row/Row.razor.cs`）：`ItemsPerRow`(`:19`, 枚举 One/Two/Three/Four/Six/Twelve)、`RowType`(`:26`, Normal/Inline)、`ColSpan`(`:33`)、`ChildContent`(`:40`)。**无 `Col` 组件**——JS 自动为子元素套 `col-*`。

**Divider**（`Divider/Divider.razor.cs`）：`IsVertical`(`:38`)、`Alignment`(`:45`, 默认 Center)、`Text`(`:52`)、`Icon`(`:59`)、`ChildContent`(`:66`)。

## Badge / Tag / Alert / Avatar / Spinner / Skeleton

**Badge**（`Badge/Badge.razor.cs`）：`Color`(`:25`, 默认 Primary)、`IsPill`(`:33`)、`ChildContent`(`:40`)。
**Tag**（`Tag/Tag.razor`，继承 AlertBase）：`Color`/`ShowDismiss`/`Icon`/`ShowBar`/`ChildContent`/`OnDismiss`（`AlertBase.cs:27,34,41,48,55,62`）。
**Alert**（`Alert/Alert.razor.cs`，继承 AlertBase）：新增 `ShowShadow`(`:35`)、`ShowBorder`(`:42`)。**无 IsShow/Title**（显示状态私有）。
**Avatar**（`Avatar/Avatar.razor.cs`）：`Url`(`:45`, **非 Src**)、`IsCircle`(`:38`)、`IsIcon`/`Icon`(`:52,59`)、`IsText`/`Text`(`:66,73`)、`Size`(`:80`, 默认 Medium)、`IsBorder`(`:87`)。
**Spinner**（`Spinner/Spinner.razor.cs`）：`Color`(`:30`)、`Size`(`:37`)、`SpinnerType`(`:44`, Border/Grow，**非 Type**)。
**Skeleton** 系列：`SkeletonBase.Round`/`Active`(默认 true)；`SkeletonTable.Rows`/`Columns`/`ShowToolbar`(默认 7/3/true)；`SkeletonAvatar.Circle`。

## Nav / Navbar

**Nav**（`Nav/Nav.razor.cs`）：`Items: IEnumerable<NavLink>?`(`:37`)、`Alignment`/`IsVertical`/`IsPills`/`IsFill`/`IsJustified`(`:44,51,58,65,72`)、`ChildContent`(`:79`)。无 OnClick。
**Navbar**（`Navbar/Navbar.razor.cs`）：`Size`(`:19`)、`BackgroundColorCSSClass`(`:26`)、`ChildContent`(`:33`)；`NavbarBrand`/`NavbarItem` 仅 `ChildContent`；`NavbarLink`（继承 ButtonBase）有 `Url`/`Target`/`ImageUrl`。

## 完整后台 Layout 示例

```razor
@inherits LayoutComponentBase
<Layout IsFullSide="true" IsPage="true" IsFixedHeader="true" IsFixedTabHeader="true"
        UseTabSet="true" SideWidth="260" ShowCollapseBar="true" ShowGotoTop="true"
        TabDefaultUrl="/" Menus="@Menus"
        AdditionalAssemblies="new[] { GetType().Assembly }"
        IsCollapsed="@_collapsed" IsCollapsedChanged="b => _collapsed = b">
    <Header>
        <div class="d-flex align-items-center">
            <img src="/logo.png" height="32" />     @* 无 Logo 参数，自渲染 *@
            <span class="ms-2">后台管理</span>
        </div>
    </Header>
    <Side></Side>                                   @* 留空，Layout 自动用 Menus 渲染 Menu *@
    <Main>@Body</Main>                              @* UseTabSet 时作 Tab.Body *@
    <Footer><span>Copyright © 2026</span></Footer>
</Layout>

@code {
    private bool _collapsed;
    private List<MenuItem> Menus = new()
    {
        new MenuItem("首页", "/") { Icon = "fa-solid fa-house" },
        new MenuItem("用户管理") { Icon = "fa-solid fa-users",
            Items = new List<MenuItem> {
                new("用户列表", "/users") { Icon = "fa-solid fa-list" },
                new("角色管理", "/roles") { Icon = "fa-solid fa-user-shield" }
            }
        }
    };
}
```
