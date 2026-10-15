# MudBlazor Agent Skill 制作任务

你是一名资深 .NET / Blazor / MudBlazor 框架专家。

我已经向你提供了：

1. MudBlazor 主仓库完整源码（`D:\SourceCode\blazor\mudblazor\MudBlazor`，dev 分支，v9.11.0-65-g326bf8c8d，2026-10-14）
2. MudBlazor.Templates 官方项目模板仓库（`D:\SourceCode\blazor\mudblazor\Templates`，独立仓库）
3. 官方文档站源码 MudBlazor.Docs（在主仓库 `src/MudBlazor.Docs/`，含按组件分类的 .razor 示例）

你的任务不是开发业务功能，而是**分析 MudBlazor 框架本身，并为 AI Coding Agent 制作一套高质量、可长期维护的 MudBlazor Agent Skill**。

目标使用环境包括：

* OpenAI Codex
* Claude Code
* OMP 等支持 Agent Skills / SKILL.md 的编码 Agent

---

## 一、核心原则

不要根据你已有的 MudBlazor 知识直接编写 Skill。

必须优先以我提供的：

* MudBlazor 组件库源码（`src/MudBlazor/Components/`）
* MudBlazor.Docs 官方示例（`src/MudBlazor.Docs/Pages/Components/`）
* MudBlazor.Templates 官方模板（`src/mudblazor`、`src/mudblazor-wasm`）
* 官方 AGENTS.md（仓库根目录）
* 组件参数定义与 XML 注释

作为事实来源。

如果你的已有知识与源码冲突，以当前源码为准。

禁止猜测不存在的：

* 组件
* 参数
* API
* Event
* Service
* Extension Method

---

## 二、两类参考资料的区分（关键）

编写 Skill 时，必须把两类参考资料的职责区分开，不能混用：

### 1. Templates 仓库：学习「怎么开项目」

位置：`D:\SourceCode\blazor\mudblazor\Templates`

学习内容：

* 项目创建：`dotnet new mudblazor` / `dotnet new mudblazorwasm`，完整选项见 `src/mudblazor/.template.config/template.json` 与仓库 `README.md`（`-int` 渲染模式、`-ai` 全局交互、`-au` 认证、`-e` 空项目等）
* 依赖注入：模板 `Program.cs` 中 MudBlazor 服务的注册方式
* 主题挂载：`MudThemeProvider` 等 Provider 在 `App.razor` / 布局中的挂载位置
* 布局骨架：`MainLayout`、导航、`MudAppBar` / `MudDrawer` 的组织方式
* 渲染模式：Blazor Web App 的 Server / Auto / WebAssembly / None 交互模式如何选择与配置
* 服务配置：Dialog、Snackbar、Popover 等 Provider 的挂载约定

注意：Templates 是「应用初始化」的事实来源，不是组件用法的事实来源。

### 2. MudBlazor.Docs：学习「怎么用组件」

位置：`src/MudBlazor.Docs/Pages/Components/<组件>/`

每个组件目录包含：

* `<组件>Page.razor`：文档页，含组件说明、API 表入口、示例组织方式
* `Examples/`：**官方按组件分类的 .razor 示例**，这是组件正确参数、绑定方式、事件处理的第一参考

学习内容：

* 每个组件的真实参数与默认值（对照 `src/MudBlazor/Components/` 源码验证）
* `@bind-Value`、`@bind-SelectedValues` 等绑定方式
* `EventCallback` 事件处理
* 组件组合方式（AppBar + Drawer + MainContent、Dialog 内嵌 Form 等）

注意：Docs 示例是「组件用法」的事实来源，不是项目脚手架，不要把它当项目结构照搬。

---

## 三、三层参考策略

Skill 内必须写明以下参考优先级，AI 生成代码时按此顺序取材：

```text
① 官方源码  src/MudBlazor/Components/<组件>/Mud*.razor(.cs)
        → 确认组件 API、参数签名、真实行为（最高优先级）
② 官方文档示例  src/MudBlazor.Docs/Pages/Components/<组件>/Examples/*.razor
        → 学习正确的 Razor 组件使用方式
③ 官方 Templates  src/mudblazor、src/mudblazor-wasm
        → 学习完整应用的初始化、布局和配置
```

冲突裁决：源码 > 文档示例 > AI 已有知识。

---

## 四、官方 AGENTS.md（必读）

主仓库根目录的 `AGENTS.md`（约 33KB）是官方专门面向 AI Coding Agent 的指导，**特别推荐完整阅读**。它已包含：

* Change Type Matrix：什么改动落在什么位置、如何验证
* Component Authoring Rules：参数必须自动属性、`ParameterState<T>` 状态管理（v9 模式）、根元素渲染规则、`CssBuilder` / `StyleBuilder`、设计 token 不硬编码颜色、公共 API 的 XML 文档要求
* Docs Pages and Examples：以 `ButtonPage.razor`、`MenuPage.razor` 为组件文档页的结构参考
* Testing Rules：bUnit 交互规则（`FindAll()` 快照失效、`InvokeAsync`、优先 `ClickAsync` 等异步交互）
* 验证回路：`dotnet test --filter` + `-p:SkipBunCompile=true` 等仓库内部约定

Skill 的「代码规范」小节应提炼此文件的结论（注明提取自哪个版本），尤其是 `ParameterState<T>` 模式与「不要硬编码颜色、用 CSS 变量 / 设计 token」这类直接影响生成代码质量的规则。

---

## 五、第一阶段：分析框架

首先扫描 MudBlazor 源码和 Docs，整理 MudBlazor 的整体能力。

重点分析：

### 1. 组件体系

以 `src/MudBlazor/Components/` 实际目录清单为准识别组件（当前已有 80+ 组件目录），重点覆盖：

* 输入类：TextField、NumericField、Select、Autocomplete、Checkbox、Radio、Switch、DatePicker、TimePicker、DateRangePicker、ColorPicker、FileUpload
* 数据类：**DataGrid**、Table、SimpleTable、List、TreeView、Timeline、Carousel、Chip
* 反馈类：Dialog、MessageBox、Snackbar、Alert、Progress、Skeleton
* 布局类：Layout、AppBar、Drawer、MainContent、Grid、Stack、Container、Paper、Divider、Tabs、ExpansionPanel、Stepper
* 导航类：Menu、NavMenu、Breadcrumbs、Pagination、Tabs、Link
* 图表类：Chart 系列
* 其他：Popover、Tooltip、Badge、Avatar、Icon、Overlay、DropZone、Hidden、BreakpointProvider

以上只是导航用示例。必须根据实际源码识别真实组件，不允许根据此列表臆造。

### 2. 每个组件分析

针对重要组件分析：

* 组件名称与泛型参数（如 `MudDataGrid<T>`、`MudTextField<T>`）
* namespace
* Parameters 与默认值（以 `[Parameter]` + XML `<remarks>` 注释为准）
* EventCallback
* 数据绑定方式（`@bind-Value` / `Value` + `ValueChanged` / 双向绑定禁忌）
* 常见组合方式
* 依赖的 Service / Provider
* Docs 中的标准用法（Examples/*.razor）
* 常见错误与推荐写法

### 3. 服务体系

分析 MudBlazor 提供的 Service（以源码为准，当前可见 `Snackbar/`、`Dialog/` 等）：

* IDialogService / DialogService
* ISnackbar / SnackbarService
* IBrowserViewportService（断点/视口订阅，经 `AddMudBlazorResizeListener` 注册；不存在 `IBreakpointService`）、IPopoverService 等以实际源码为准

说明：

* 如何注册（对照 Templates 模板的 `Program.cs`）
* 对应 Provider 组件挂载在哪里（`MudDialogProvider`、`MudSnackbarProvider`、`MudPopoverProvider` 等）
* 如何注入与调用
* 推荐使用场景

### 4. DataGrid 深度分析

`MudDataGrid` 是重点组件，必须单独深入分析；同时必须说清它与经典 `MudTable` 是**并存的两个体系**，不要混写：

* 列定义：PropertyColumn / TemplateColumn / SelectColumn
* 数据源：`Items` 与 `ServerData` 的区别与适用场景
* 排序、筛选（FilterMode）、分页（MudDataGridPager）、分组
* 行选择（多选、全选）、行点击
* 行内编辑与对话框编辑
* 聚合（AggregateDefinition）
* Hover、Dense 等外观参数；列冻结用**列上的** `StickyLeft` / `StickyRight`（`Column.razor.cs:357-366`；不存在 `StickyColumns` 参数，`HasStickyColumns` 是内部私有判定）

以上以当前源码 `src/MudBlazor/Components/DataGrid/`（`MudDataGrid.razor.cs` 约 129KB）实际 API 为准，最终应能指导 Coding Agent 正确生成复杂 CRUD DataGrid。

### 5. Form 与校验体系

* MudForm（**v9 无 `MudFormItem`**，嵌套表单直接用 `MudForm`，`IsValid` 自动聚合子表单）
* MudTextField 等表单组件的 `For` 表达式校验（反射属性 `ValidationAttribute` 自动执行，无需 DataAnnotationsValidator）
* `Validation` 参数多签名分发（`Func<T,bool>` / `Func<T,string?>` / `Func<T,IEnumerable<string?>>` / `ValidationAttribute` 实例及各 Task 版本；**不存在 `ValidationResolver`**）
* 手动校验触发（**`ValidateAsync()` / `ResetAsync()` / `ResetValidationAsync()`**；同步 `Validate()` 已 `[Obsolete]`）

### 6. 主题体系

* MudThemeProvider、MudTheme、调色板（Palette）、暗色模式切换
* 排版（Typography）、圆角 / 浮影（LayoutProperties）
* 设计 token：优先 CSS 变量，禁止硬编码颜色（官方 AGENTS.md 明确要求）

### 7. Layout 体系

* MudLayout / MudAppBar / MudDrawer / MudDrawerContainer / MudMainContent
* 断点响应（MudBreakpointProvider、MudHidden、Breakpoint）

---

## 六、分析 Docs 示例

不要只分析组件源码。

必须重点分析 `src/MudBlazor.Docs/Pages/Components/` 下的官方示例，因为：

> Docs 中的实际写法优先于 AI 自己推断出来的写法。

注意：Docs 示例文件头部带 `@namespace MudBlazor.Docs.Examples`，部分依赖 `MudBlazor.Examples.Data`（如 `Employee`、`ClientInfo` 模型）与 `httpClient` 注入。提取进 skill 前必须剥离这些引用，换为示例自包含的最小模型与数据。

寻找：

* 最小示例
* 官方推荐示例
* DataGrid / Table CRUD 示例
* Form 校验示例
* Dialog 示例（含 DialogService 用法）
* Snackbar 示例
* Layout 完整骨架示例
* FileUpload、TreeView、Autocomplete 等高频组件示例

对于重复示例，不需要全部复制。提取最典型、最值得 AI 学习的模式。

---

## 七、生成 Skill

建议生成以下结构：

```text
mudblazor-skill/
├── SKILL.md
├── references/
│   ├── project-setup.md
│   ├── layout-and-theme.md
│   ├── components.md
│   ├── data-grid.md
│   └── forms-and-validation.md
└── examples/
    ├── dashboard.razor
    ├── crud-page.razor
    └── dialog-form.razor
```

按本仓库技能规范落盘时，目录放在 `skills/dotnet-skills/mudblazor/`（技能 ID 取最外层含 SKILL.md 的目录名，即 `mudblazor`；`references/` 与 `examples/` 结构保持上面不变）。

如果根据框架规模判断有更合理的目录结构，可以调整。但是：

**不要把所有内容全部塞进 SKILL.md。**

SKILL.md 应负责：

* Skill 的适用范围
* AI 工作原则（三层参考策略、版本检查）
* MudBlazor 关键约束（ParameterState、设计 token、Provider 挂载）
* 如何选择组件
* references 导航
* 代码生成流程
* 禁止事项

详细 API 和大型示例放到 references / examples。

各 reference 的职责划分：

| 文件 | 内容 | 事实来源 |
| --- | --- | --- |
| project-setup.md | dotnet new 模板选项、包安装、DI 注册、Provider 挂载、渲染模式 | Templates 仓库 + 主仓库 Services |
| layout-and-theme.md | Layout 骨架、MudTheme、调色板、暗色模式、断点 | 源码 Layout/ThemeProvider + Docs 示例 |
| components.md | 高频组件参数速查、绑定方式、事件、组合模式 | 源码 Components/ + Docs Examples |
| data-grid.md | DataGrid 全参数、列体系、ServerData、选择、编辑、与 MudTable 的取舍 | 源码 DataGrid/ + Docs DataGrid 示例 |
| forms-and-validation.md | MudForm、For 校验、DataAnnotations、Dialog 内表单模式 | 源码 Form/ + Docs Form/Dialog 示例 |

---

## 八、SKILL.md 必须告诉 AI 的事情

当用户开发 Blazor 页面，并且项目使用 MudBlazor 时：

优先使用 MudBlazor 已有组件。

例如存在：

```razor
<MudButton>
```

时，不应该优先生成：

```html
<button class="btn btn-primary">
```

存在 MudBlazor Dialog 能力时，不要自己重新实现 Bootstrap Modal。

存在 Snackbar 时，不要自己通过 JS 实现 Toast。

存在 MudDataGrid 时，不要手写 `<table>` + 分页逻辑。

---

## 九、禁止幻觉 API

这是整个 Skill 最重要的规则之一。

当 AI 不确定某个 MudBlazor API 时：

必须：

1. 搜索 references
2. 搜索 Docs 示例
3. 搜索源码（`src/MudBlazor/Components/`）
4. 确认组件定义与参数

之后才能生成代码。

禁止根据其他组件库经验猜测，例如：

* BootstrapBlazor
* Radzen
* Ant Design Blazor
* DevExpress Blazor
* Telerik
* Blazorise
* Materialize / MUI（Web 版）

的 API 不得套用到 MudBlazor。

典型幻觉形态（必须以当前版本源码核实每个属性是否真实存在）：

```razor
<MudDataGrid Items="@items" Pageable="true" RowsPerPage="10"
             OnRowClick="..." Filterable="true">
</MudDataGrid>
```

这段示例本身就藏着三个真实幻觉：`Pageable` 不存在（分页靠 `PagerContent` 放 `MudDataGridPager`）、`OnRowClick` 是 MudTable 的事件名（DataGrid 叫 `RowClick`，参数类型 `DataGridRowClickEventArgs<T>`）、`RowsPerPage` 需配合分页器才可见。Skill 必须给出「先查源码再写参数」的硬性流程，并收录 Table↔DataGrid 互异命名对照（`QuickFilter`↔`Filter`、`OnRowClick`↔`RowClick`、`TableData<T>`↔`GridData<T>`、默认 `ReadOnly` false↔true）。

---

## 十、代码生成原则

生成 MudBlazor 页面时，优先顺序：

```text
MudBlazor 官方 Docs 推荐模式
        ↓
官方 Docs Examples（src/MudBlazor.Docs/Pages/Components）
        ↓
MudBlazor 组件与参数
        ↓
MudBlazor 服务（Dialog / Snackbar / Breakpoint）
        ↓
MudBlazor 布局与 utility class
        ↓
自定义 Razor 组件
        ↓
自定义 HTML/CSS
        ↓
JavaScript（JS interop）
```

能够使用 MudBlazor 完成的功能，不应该重新造组件。

同时遵守官方 AGENTS.md 提炼出的规范：

* 颜色用主题 / CSS 变量，不硬编码
* 参数绑定用官方推荐方式，不通过 `@ref` 改子组件参数
* 组件职责单一，页面拆分遵循仓库通用拆分口径

---

## 十一、CRUD 标准模式

研究 Docs 的 DataGrid / Dialog / Form 示例后，总结 MudBlazor 推荐 CRUD 模式。

AI 应能够根据：

> 创建一个用户管理页面。

自动推导：

```text
UserManagement.razor

MudDataGrid<T>（ServerData 或 Items）
    ↓
列定义（PropertyColumn / TemplateColumn）
    ↓
MudDataGridPager
    ↓
Toolbar（搜索、新增按钮）
    ↓
DialogService.ShowAsync + 自定义 Dialog
    ↓
Dialog 内 MudForm + For 校验
    ↓
确认删除
    ↓
Snackbar 反馈
```

同时保证使用的全部 MudBlazor API 与当前源码版本一致。

---

## 十二、版本意识

在 Skill 中明确记录并强制 AI 检查版本：

**生成代码前，必须先确认目标项目使用的 MudBlazor 版本**（读 `.csproj` 的 PackageReference 或 `Directory.Packages.props`）。不要直接把 dev 分支最新示例套用到不同版本的项目中。

已知事实：

* 本地源码：dev 分支，`v9.11.0-65-g326bf8c8d`（commit `326bf8c8d`，2026-10-14）
* 组件库目标框架：net8.0 / net9.0 / net10.0
* `ParameterState<T>` 参数状态框架自 **v6.18.0** 引入（git tag 证实，2024-03 #8258），并非 v9 新增；但 v9 大量组件参数改用它（`@bind-IsDarkMode`、`@bind-Open`、`@bind-ActivePanelIndex` 等），从 v7/v8 迁移时注意绑定语义差异

Skill 应记录：

```text
MudBlazor Version: 9.11.0-65 (dev)
Source Commit: 326bf8c8dcd9f1765d4301e468fcad36d398a16c
Templates Commit: 0efa6ba300f7376996186d08bc15b6f0725239a5
Skill Generated: 2026-10-15
```

如果源码升级，应能够重新扫描变化（尤其 `CHANGELOG.md`、迁移文档、API 变更）并更新 Skill。

生成代码时按项目实际版本裁剪：references 中的 API 条目应标注「自哪个版本可用」，对 dev 才有的 API 标注 `[仅 dev / vX.Y+]`。

---

## 十三、生成前先输出分析报告

不要立即开始生成 SKILL.md。

第一阶段先输出：

```text
MudBlazor Framework Analysis
```

包括：

1. 发现多少组件（按 `src/MudBlazor/Components/` 实际目录数）
2. 核心组件分类
3. 主要 Service 与 Provider
4. DataGrid 体系（与 MudTable 的关系）
5. Form / 校验体系
6. Dialog / Snackbar 体系
7. Layout / Theme 体系
8. Templates 模板的项目结构与选项
9. Docs 示例组织方式
10. 推荐提取到 Skill 的知识
11. Skill 目录设计

确认框架理解完成以后，再开始创建 Skill 文件。

---

## 十四、最终验收

Skill 创建完成后，用它完成以下测试：

### Test 1

创建简单登录页面（MudCard + MudTextField + MudButton + Snackbar 反馈）。

### Test 2

创建设备管理 CRUD，使用自定义编辑 Dialog（不用行内编辑）：

字段：

```text
Name
IP
Port
Status
Description
```

要求：

```text
MudDataGrid 展示
搜索
分页
新增
编辑
删除
确认删除
Snackbar 反馈
Form 校验（必填、IP 格式、端口范围）
```

### Test 3

创建包含：

```text
MudAppBar（顶部 Header）
MudDrawer（左侧 Menu）
MudMainContent（主 Content）
MudThemeProvider（含暗色模式切换）
```

的后台管理 Layout。

### Test 4

创建复杂 DataGrid：

```text
ServerData（服务端分页）
排序
搜索
多选（SelectColumn）
Toolbar
```

检查生成代码是否：

* 使用真实 MudBlazor API（参数名、事件签名与当前版本源码一致）
* 可以编译（实际执行 `dotnet build`，Windows 环境下禁止 `&&` 链式命令）
* **`dotnet build` 0 警告**——过时 API（如 `Validate()`）、MUD0002（TemplateColumn 未包 `<CellTemplate>`）等都是靠警告而非错误暴露的，带警告通过 = Skill 教了将过时的写法
* 与项目 csproj 声明的 MudBlazor 版本一致，没有把 dev 专属 API 用到旧版本
* 没有虚构参数
* 没有混入其他 Blazor UI 框架 API
* 颜色通过主题 / CSS 变量表达，无硬编码色值

如果存在问题，修正 Skill，再重新测试。

最终交付：

```text
skills/dotnet-skills/mudblazor/
```

完整目录。

---

我建议你的工作流是：

```text
MudBlazor 源码 + MudBlazor.Docs + Templates + 官方 AGENTS.md
        ↓
① Repository Exploration（第十三节清单）
        ↓
② 组件 / Service / API 清单
        ↓
③ 高频组件深度分析（DataGrid / Form / Dialog / Layout / Theme）
        ↓
④ Docs 示例最佳实践提取
        ↓
⑤ Plan Report（第十三节）
        ↓
⑥ SKILL.md 撰写
        ↓
⑦ references 撰写
        ↓
⑧ examples 撰写
        ↓
⑨ 编译测试（dotnet build）+ 十四节五个 Test
        ↓
⑩ 修复 Skill
```

特别是 **「编译测试一定要执行」**（Windows 环境下禁止 `&&` 链式命令）。MudBlazor 这类组件库 Skill 最大的问题不是 AI 不懂怎么设计页面，而是很容易写出第九节那种「看起来非常合理，但当前版本不存在的参数」。让 AI 生成测试项目并实际 `dotnet build`，能把 Skill 质量提高一个档次。

另外，保持 Skill 的 **「源码型 Skill」而不是「百科全书型 Skill」** 定位：SKILL.md 控制在小规模（适用范围 / 工作原则 / 版本检查 / 关键约束 / 速查 / references 索引），详细的数据：

```text
DataGrid 全参数
Form 校验规则
Dialog 服务 API
主题调色板
示例代码
```

放 `references/`。这样 Codex、Claude Code、OMP 不需要每一次都加载几万字 MudBlazor 文档。
