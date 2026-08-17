# BootstrapBlazor Agent Skill 制作任务

你是一名资深 .NET / Blazor / BootstrapBlazor 框架专家。

我已经向你提供了：

1. BootstrapBlazor 完整源码
2. BootstrapBlazor 官方 Example / Demo 项目
3. 当前 BootstrapBlazor 的版本源码

你的任务不是开发业务功能，而是**分析 BootstrapBlazor 框架本身，并为 AI Coding Agent 制作一套高质量、可长期维护的 BootstrapBlazor Agent Skill**。

目标使用环境包括：

* OpenAI Codex
* Claude Code
* Cursor 等支持 Agent Skills / SKILL.md 的编码 Agent

## 一、核心原则

不要根据你已有的 BootstrapBlazor 知识直接编写 Skill。

必须优先以我提供的：

* BootstrapBlazor 源码
* Example
* Demo
* XML 注释
* Razor 示例
* 组件参数定义
* 官方项目实际使用方式

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

## 二、第一阶段：分析框架

首先扫描 BootstrapBlazor 源码和 Example 项目，整理 BootstrapBlazor 的整体能力。

重点分析：

### 1. 组件体系

识别主要组件，例如：

* Button
* Input
* Select
* Checkbox
* Radio
* Switch
* DateTimePicker
* Table
* Tree
* TreeView
* Dialog
* Modal
* Message
* Toast
* Notification
* Card
* Collapse
* Tabs
* Steps
* Pagination
* Upload
* AutoComplete
* Search
* Layout
* Row
* Col
* Menu
* Dropdown
* Tooltip
* Popover
* Progress
* Spinner
* Chart

以上只是示例。

必须根据实际源码识别真实组件，不允许根据此列表臆造。

---

### 2. 每个组件分析

针对重要组件分析：

* 组件名称
* namespace
* 泛型参数
* Parameters
* EventCallback
* 数据绑定方式
* 常用属性
* 常见组合方式
* Dependency Injection 依赖
* 使用场景
* 官方 Example 中的标准用法
* 常见错误
* 推荐写法
* 不推荐写法

---

### 3. 服务体系

分析 BootstrapBlazor 提供的 Service，例如：

* DialogService
* MessageService
* ToastService
* SwalService
* NotificationService

以实际源码为准。

说明：

* 如何注册
* 如何注入
* 生命周期
* 如何调用
* 推荐使用场景

---

### 4. Table 深度分析

Table 是重点组件，必须单独深入分析。

包括：

* 基础 Table
* 泛型模型
* Items
* 数据绑定
* Columns
* 自动列
* 手动列
* Template
* 排序
* 搜索
* Filtering
* Pagination
* PageSize
* Server Side Data
* Query
* OnQueryAsync
* Loading
* Selection
* MultiSelect
* Edit
* Delete
* Add
* Inline Edit
* Toolbar
* RowTemplate
* Formatter
* FormatterAsync

以上仍需以当前源码实际 API 为准。

最终应能够指导 Coding Agent 正确生成复杂 CRUD Table。

---

## 三、分析 Example 项目

不要只分析组件源码。

必须重点分析官方 Example / Demo，因为：

> Example 中的实际写法优先于 AI 自己推断出来的写法。

寻找：

* 最小示例
* 官方推荐示例
* CRUD 示例
* Table 示例
* Form 示例
* Dialog 示例
* Toast / Message 示例
* Layout 示例
* Upload 示例
* Tree 示例
* Search 示例

对于重复示例，不需要全部复制。

提取最典型、最值得 AI 学习的模式。

---

## 四、生成 Skill

建议生成以下结构：

```text
skills/
└── bootstrap-blazor/
    ├── SKILL.md
    │
    ├── references/
    │   ├── overview.md
    │   ├── setup.md
    │   ├── components.md
    │   ├── forms.md
    │   ├── table.md
    │   ├── dialog.md
    │   ├── message-toast.md
    │   ├── layout.md
    │   ├── upload.md
    │   ├── tree.md
    │   ├── dependency-injection.md
    │   └── best-practices.md
    │
    └── examples/
        ├── basic-page.razor
        ├── form.razor
        ├── table.razor
        ├── crud-table.razor
        ├── dialog.razor
        └── layout.razor
```

如果根据框架规模判断有更合理的目录结构，可以调整。

但是：

**不要把所有内容全部塞进 SKILL.md。**

SKILL.md 应负责：

* Skill 的适用范围
* AI 工作原则
* BootstrapBlazor 关键约束
* 如何选择组件
* references 导航
* 代码生成流程
* 禁止事项

详细 API 和大型示例放到 references / examples。

---

## 五、SKILL.md 必须告诉 AI 的事情

当用户开发 Blazor 页面，并且项目使用 BootstrapBlazor 时：

优先使用 BootstrapBlazor 已有组件。

例如存在：

```razor
<Table>
```

时，不应该优先生成：

```html
<table class="table">
```

存在 BootstrapBlazor Dialog 能力时，不要自己重新实现 Bootstrap Modal。

存在消息组件时，不要自己通过 JS 实现 Toast。

---

## 六、禁止幻觉 API

这是整个 Skill 最重要的规则之一。

当 AI 不确定某个 BootstrapBlazor API 时：

必须：

1. 搜索 references
2. 搜索 Example
3. 搜索源码
4. 确认组件定义

之后才能生成代码。

禁止根据其他组件库经验猜测，例如：

* MudBlazor
* Radzen
* Ant Design Blazor
* DevExpress Blazor
* Telerik
* Blazorise

的 API 不得套用到 BootstrapBlazor。

---

## 七、代码生成原则

生成 BootstrapBlazor 页面时，优先顺序：

```text
BootstrapBlazor 官方推荐模式
        ↓
BootstrapBlazor Example
        ↓
BootstrapBlazor Components
        ↓
Bootstrap Utility / CSS
        ↓
自定义 Razor
        ↓
自定义 HTML/CSS
        ↓
JavaScript
```

能够使用 BootstrapBlazor 完成的功能，不应该重新造组件。

---

## 八、CRUD 标准模式

研究 Example 后，总结 BootstrapBlazor 推荐 CRUD 模式。

AI 应能够根据：

> 创建一个用户管理页面。

自动推导：

```text
UserManagement.razor

Search Area
    ↓
Toolbar
    ↓
Table<UserDto>
    ↓
Pagination
    ↓
Add
Edit
Delete
    ↓
Dialog / Form
    ↓
Validation
    ↓
Message / Toast
```

同时保证使用的全部 BootstrapBlazor API 与当前源码版本一致。

---

## 九、版本意识

在 Skill 中明确记录：

* BootstrapBlazor 当前源码版本
* 分析日期
* API 来源

例如：

```text
BootstrapBlazor Version:
Source Commit:
Skill Generated:
```

如果源码升级，应能够重新扫描变化并更新 Skill。

---

## 十、生成前先输出分析报告

不要立即开始生成 SKILL.md。

第一阶段先输出：

```text
BootstrapBlazor Framework Analysis
```

包括：

1. 发现多少组件
2. 核心组件分类
3. 主要 Service
4. Table 体系
5. Form 体系
6. Dialog / Message 体系
7. Layout 体系
8. Example 项目结构
9. 推荐提取到 Skill 的知识
10. Skill 目录设计

确认框架理解完成以后，再开始创建 Skill 文件。

---

## 十一、最终验收

Skill 创建完成后，用它完成以下测试：

### Test 1

创建简单登录页面。

### Test 2

创建设备管理 CRUD，要求使用自定义的编辑页面，不要使用默认的：

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
搜索
分页
新增
编辑
删除
确认删除
Toast
```

### Test 3

创建包含：

```text
左侧 Menu
顶部 Header
主 Content
```

的后台管理 Layout。

### Test 4

创建复杂 Table：

```text
Server Side Query
Pagination
Sort
Search
Multi Select
Toolbar
```

检查生成代码是否：

* 使用真实 BootstrapBlazor API
* 可以编译
* 没有虚构参数
* 没有混入其他 Blazor UI 框架 API

如果存在问题，修正 Skill，再重新测试。

最终交付：

```text
skills/bootstrap-blazor/
```

完整目录。

我建议你**不要一次让 AI“随便总结整个仓库”**。更好的工作流是：

```text
BootstrapBlazor 源码
        +
BootstrapBlazor Example
        ↓
① Repository Exploration
        ↓
② 组件/API清单
        ↓
③ 高频组件深度分析
        ↓
④ Example最佳实践提取
        ↓
⑤ SKILL.md
        ↓
⑥ references
        ↓
⑦ examples
        ↓
⑧ 编译测试
        ↓
⑨ 修复 Skill
```

特别是 **“编译测试”一定要写进去**。因为 BootstrapBlazor 这种组件库 Skill 最大的问题不是 AI 不知道怎么设计页面，而是很容易写出类似：

```razor
<Table DataSource="@items"
       Pageable="true"
       OnPageChanged="...">
```

这种**看起来非常合理，但可能根本不是 BootstrapBlazor 当前版本真实 API** 的代码。让 AI 生成测试项目并实际 `dotnet build`，能把 Skill 质量提高一个档次。

另外，我会建议你把 Skill 做成 **“源码型 Skill”而不是“百科全书型 Skill”**。`SKILL.md` 本身控制在比较小的规模，例如：

```text
SKILL.md
├── 什么时候使用 Skill
├── BootstrapBlazor 核心原则
├── API 查询规则
├── 组件选择规则
├── 页面生成规范
└── references 索引
```

真正详细的数据：

```text
Table API
Form API
Dialog API
组件参数
示例代码
```

放 `references/`。这样 Codex 不需要每一次都加载几万字 BootstrapBlazor 文档。