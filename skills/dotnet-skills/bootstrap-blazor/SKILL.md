---
name: bootstrap-blazor
description: "BootstrapBlazor 组件库开发指南。.NET Blazor 项目中使用 BootstrapBlazor 的最佳实践与代码模板。Keywords: BootstrapBlazor, Blazor, .NET, Table, Modal, TreeView, Form, Toast, Menu, Layout, 侧边栏, NavLinkMatch, CQRS, RBAC."
---

# BootstrapBlazor 组件库开发指南

BootstrapBlazor 企业级开发指南，提供在 .NET Blazor Server 项目中使用 BootstrapBlazor 组件库的最佳实践、代码模板和常见模式。

**适用场景**：
- 使用 BootstrapBlazor 构建 Blazor Server 应用
- 企业级 RBAC/后台管理系统开发
- CQRS 架构模式下的 CRUD 页面开发
- BootstrapBlazor 组件使用问题排查

**不适用场景**：
- Blazor WebAssembly 客户端独立应用（部分模式不适用）
- 非 BootstrapBlazor 的 Blazor UI 框架
- 纯前端 React/Vue 项目

## 按需查阅

本文件只做路由，按当前任务读对应参考文档即可，不必全部加载。

| 任务场景 | 参考文档 |
| --- | --- |
| 安装 NuGet 包、服务注册、静态资源、`_Imports`、根组件配置 | `references/setup.md` |
| Table 数据表格：分页搜索、树形表格、内联编辑、刷新 | `references/table.md` |
| Modal 模态框：Add/Edit 分离模式、删除确认 | `references/modal.md` |
| 表单：ValidateForm 验证、输入组件、Select、CheckboxList | `references/forms.md` |
| TreeView 权限/菜单树、Toast 通知 | `references/treeview-toast.md` |
| Layout 布局、侧边栏菜单构建、Tab 标签页（含菜单展开陷阱） | `references/layout-menu.md` |
| Card/Badge/Drawer/Alert 等小组件、CSS 隔离 | `references/misc-components.md` |
| 新建完整 CRUD 页面的标准步骤与模板 | `references/crud-template.md` |
| 组件行为异常排查 | `references/troubleshooting.md` |

典型组合：标准 CRUD 页面按 `table.md` → `modal.md` → `forms.md` → `crud-template.md` 顺序查阅；RBAC 管理端再加 `layout-menu.md` 与 `treeview-toast.md`。

## 高频陷阱速览

- TreeView 选中状态丢失：数据模型须实现 `Equals`/`GetHashCode`，并用 `Key` 强制重建（`treeview-toast.md`）
- 模态框数据不更新：`Show()` 内用 `InvokeAsync` + `StateHasChanged()`（`modal.md`）
- 表格自定义行按钮不显示：`ShowExtendButtons="true"` 且关闭内置编辑/删除按钮，再用 `RowButtonTemplate`（`table.md`）
- 菜单点击 404、子路由侧边栏收起、顶级菜单高亮异常：见 `layout-menu.md` 菜单章节
- `NavLinkMatch` 只有 `All` 与 `Prefix`，没有 `Exact`（`layout-menu.md`）

## 参考资源

- [BootstrapBlazor 官方文档](https://www.blazor.zone/)
- [BootstrapBlazor GitHub](https://github.com/dotnetcore/BootstrapBlazor)
- [BootstrapBlazor 在线演示](https://www.blazor.zone/table)
