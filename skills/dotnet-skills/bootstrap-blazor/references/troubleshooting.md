# 常见问题

## TreeView 不显示选中状态
确保数据模型实现了 `Equals` 和 `GetHashCode`，并使用 `Key` 属性强制重建。

## 模态框数据不更新
在 `Show()` 方法中使用 `InvokeAsync` + `StateHasChanged()` 确保在 UI 线程执行并强制渲染。

## 表格自定义行按钮不显示
必须设置 `ShowExtendButtons="true"` 且 `ShowExtendEditButton="false"` `ShowExtendDeleteButton="false"`，然后使用 `<RowButtonTemplate>`。

## 菜单点击 404
确保页面 `@page` 路由与菜单 `Component` 字段完全匹配（不带前导 `/`）。

## 进入子路由页面（如编辑页 /users/edit/1）后侧边栏分组自动收起
Menu 仅在叶子菜单 Url 与当前路径**完全相等**时才自动展开父分组（源码 `InitMenus` 精确比较，不看 Match/前缀），子路由不会触发。修复：构建菜单后按前缀命中子项并设置分组 `IsCollapsed = false`，见 `layout-menu.md` 的「侧边栏子菜单展开行为」。注意手动设置子项 `IsActive` 无效（每次初始化被重置），`SetCollapse()` 是 protected internal 调不到。

## 顶级菜单高亮异常（所有子页面都亮同一个菜单）
顶级菜单 Url（如 `/admin`）默认 `Match = NavLinkMatch.Prefix`，前缀吞掉所有子页面的高亮。显式设置 `Match = NavLinkMatch.All` 改为精确匹配。`NavLinkMatch` 只有 `All` 和 `Prefix`，**没有 `Exact`**。

## 表格搜索不生效
`CustomerSearchModel` 必须是组件属性（非局部变量），`OnQueryAsync` 中从 `QueryPageOptions` 获取分页参数。

