# Layout 布局组件

后台整体的信息架构、页面层级、视觉密度与响应式约定见 `admin-shell-patterns.md`；本文专注 BootstrapBlazor Layout / Menu / Tab 的组件结构与行为。

```razor
<Layout ShowGotoTop="true" ShowCollapseBar="true" ShowFooter="true"
        IsPage="true" IsFullSide="true" IsFixedHeader="true" IsFixedFooter="false"
        ShowSplitBar="true" SidebarMinWidth="200" SidebarMaxWidth="350"
        TabStyle="TabStyle.Chrome" ShowToolbar="true" ShowTabContextMenu="true"
        ShowTabInHeader="false" ShowTabExtendButtons="false"
        TabDefaultUrl="/" UseTabSet="true" IsFixedTabHeader="false"
        AdditionalAssemblies="new[] { GetType().Assembly }"
        Menus="@Menus">
    <Header><!-- 头部内容 --></Header>
    <Side><!-- 侧边栏菜单 --></Side>
    <Main>
        <ErrorLogger>
            @Body
        </ErrorLogger>
    </Main>
    <Footer><!-- 页脚 --></Footer>
</Layout>
```

**CRITICAL - `Menus` 依赖 `<Side>` 片段（源码行为，官方文档未写）**：Layout 源码 `@if (Side == null)` 分支只渲染 Header + Main——不写 `<Side />` 时侧栏整段跳过，`Menus` 参数随之失效且**无任何警告**。「只传 `Menus` 却没有菜单」不是 bug，是缺 `<Side />`：即使侧栏内容全部由 `Menus` 数据驱动，也必须显式声明 `<Side />`（可为空片段）。

**菜单数据构建**：
```csharp
private List<MenuItem> Menus { get; set; } = new();

// 方式一：从数据源转换
Menus = menuData.Select(m => new MenuItem()
{
    Text = m.Name,
    Icon = m.Icon,
    Url = m.Component,  // 对应 @page 路由（不带前导 /）
    Match = NavLinkMatch.All,
    Items = m.Children?.Select(c => new MenuItem() { ... }).ToList()
}).ToList();

// 方式二：静态定义
Menus = new List<MenuItem>()
{
    new() { Text = "首页", Icon = "fa-solid fa-fw fa-home", Url = "/", Match = NavLinkMatch.All },
    new() { Text = "系统管理", Icon = "fa-solid fa-fw fa-cogs",
        Items = new List<MenuItem>()
        {
            new() { Text = "用户管理", Icon = "fa-solid fa-users", Url = "/users" },
            new() { Text = "角色管理", Icon = "fa-solid fa-user-shield", Url = "/role-management" }
        }
    }
};
```

**CRITICAL - 侧边栏子菜单展开行为（源码行为，官方文档未写）**：

Menu 组件初始化时（`InitMenus`），**仅当叶子菜单项的 `Url` 与当前页面路径完全相等**（忽略大小写、去前导 `/`、去查询串）时才自动展开其父分组——**不使用 `Match` 属性、不做前缀匹配**。因此进入子路由页面（如 `/users/edit/1`）时，所属分组会保持默认收起。

修复方法：构建菜单数据后，按当前路径前缀命中子项，手动将分组 `IsCollapsed = false`（public 属性，`MenuItem : NodeItem`，默认 `true`；注意 `SetCollapse()` 是 protected internal，应用代码不可调用）：

```csharp
protected override async Task OnInitializedAsync()
{
    Menus = await BuildMenusAsync();

    // 子路由页面 URL 不与任何菜单项完全相等，Menu 不会自动展开所属分组，需手动展开
    var currentPath = Nav.ToBaseRelativePath(Nav.Uri).Split('?', '#')[0].TrimStart('/');
    foreach (var group in Menus)
    {
        if (group.Items == null) continue;
        var hit = group.Items.Any(child =>
        {
            var url = (child.Url ?? string.Empty).TrimStart('/');
            return url.Length > 0 && (currentPath.Equals(url, StringComparison.OrdinalIgnoreCase)
                || currentPath.StartsWith(url + "/", StringComparison.OrdinalIgnoreCase));
        });
        if (hit)
        {
            group.IsCollapsed = false;
        }
    }
}
```

**要点**：
- 启用导航时 `InitMenus` 每次初始化都会把所有菜单项 `IsActive` 重置为 false（再按地址栏精确匹配回填），手动设置子项 `IsActive` 无效
- `NavLinkMatch` 只有两个值：`All`（精确匹配）、`Prefix`（前缀匹配，**默认值**）；**没有 `Exact`**，写 `NavLinkMatch.Exact` 会编译报错 CS0117
- 顶级菜单（如 `/admin`）务必显式 `Match = NavLinkMatch.All`，否则默认 Prefix 会吞掉所有 `/admin/*` 子页面的高亮
- 叶子菜单保持默认 Prefix，子路由页面（编辑/详情）即可维持高亮

**Tab 标签页属性**：
```razor
@page "/your-page"
@attribute [TabItemOption(Text = "页面标题", Closable = false)]
@attribute [Authorize]
```
