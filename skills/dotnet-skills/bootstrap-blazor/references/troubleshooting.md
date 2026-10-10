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

## Drawer 编程式关闭不彻底（遮罩/容器残留，"关不掉"）
Drawer 的关闭链路分两段：`IsOpen=false` 后 `OnAfterRenderAsync` 调 JS `execute(id,false)`，其中 `.drawer-body`/`.drawer-backdrop` 的 `.show` 是**同步**移除的，但根元素 `.drawer` 的 `.show` 要等 `requestAnimationFrame` 循环 320ms 后才移除。在 rAF 被节流的环境（后台标签页、浏览器省电模式、嵌入式 webview、自动化工具受控页面）里 rAF 永不触发，根元素永远留着 `.drawer.show`——内容已滑出但容器/遮罩视觉残留，用户表现为"抽屉没关"。Modal 不受影响（走 Bootstrap 的 transition+setTimeout）。验收与自动化判断 Drawer 是否关闭要查 `.drawer.show` 是否存在，不要只查 `.drawer-body.show`。

## ValidateForm 的 Submit 按钮不吃页内 JS 合成 click（自动化测试）
普通 `Button`（`OnClick`）用页内 JS `element.click()` 可正常触发 Blazor 事件；但 `ButtonType.Submit` + `IsAsync="true"` 的提交按钮（BB Button JS 拦截 + ValidateForm 管线）对合成 `click()` 无反应。等价路径：对弹窗/抽屉内 `form` 调 `form.requestSubmit()`，可完整走验证 + `OnValidSubmit` 管线。

## 表单值填充后 Blazor 模型未同步（自动化测试）
JS 填充输入框必须三件套：`Object.getOwnPropertyDescriptor(HTMLInputElement.prototype,'value').set` 原生赋值 + 派发 `input` + 派发 `change` 事件，缺一不可（真实键盘输入即 CUA type 最稳）。另注意弹窗内**第一个 `<input>` 可能是隐藏域**（防伪令牌），`querySelector("input")` 会选错目标，应按 `input.form-control` + placeholder 定位。

## 电路内异步回调改状态 UI 不更新
Blazor Server 电路内，`Table.OnClickRowCallback` 等 BB 回调里改组件状态不会自动触发渲染，必须显式 `StateHasChanged()`；条件块内的子组件（如明细 Table）挂载后会自动首查，回调里不要再手动 `QueryAsync`（首拍引用可能为 null，且会与自动首查并发撞同一 scoped DbContext）。

## 同一表格两列绑定同一字段，列被吞/列头错显
BootstrapBlazor Table 按绑定字段合并列定义：同表两列 `@bind-Field` 指向同一属性时后一列会丢失或错显。计算列/占位列必须换绑一个未被占用的可写字段，在 `<Template>` 里自行展示计算值；且 `@bind-Field` 要求可写属性，绑定计算属性会 CS0200。

## ValidateForm 内输入组件绑裸字段抛 ArgumentNullException
`BootstrapInput`/`BootstrapInputNumber` 在 ValidateForm 内绑定 `@code` 裸字段（非模型属性）时，BB 校验组件反射 `MemberInfo` 为 null，抛 `ArgumentNullException('element')`。必须绑定表单模型属性——单字段场景也建一个小模型类承载。

