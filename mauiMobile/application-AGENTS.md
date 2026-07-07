---
rule_id: application-agents
version: 1.4.0
last_updated: 2026-07-08
dependencies: [agents-root]
---

# 应用规则

## 适用范围

- 作用域：ViewModel、Service、命令、业务编排、结果包装与应用层测试
- 触发场景：涉及业务流程、命令逻辑、状态流转、应用服务或应用层测试时阅读

### 阅读摘要
- 建议阅读：改 ViewModel、改命令、改 Service、改业务规则、补应用层验证
- 可先跳过：纯界面样式、纯文档整理、仅发布配置调整
- 优先查看：应用层规则、业务流程 / 状态流转规则、测试规则

### 常见任务入口
- 改命令流程或状态切换：先看 ViewModel / 命令规则与结果处理约束
- 改 Service 或业务规则：先看应用层职责边界与异常处理规范
- 改 DTO、映射、结果包装：先看输入输出结构与边界约定
- 补应用层回归：先看 `提交前最小回归` 与测试规则

---

## 技术栈

### 分层架构
- `View → ViewModel → Service → Repository → Model`
- View 负责展示，ViewModel 负责状态与命令编排，Service 负责业务逻辑，Repository 负责数据访问边界，Model 负责领域数据表达

### 核心库
- `CommunityToolkit.Mvvm`：`[ObservableProperty]`、`[RelayCommand]`、`IMessenger`
- `Syncfusion.Maui.Toolkit`：UI 控件库（图表、下拉刷新、卡片等）
- `CommunityToolkit.Maui`：移动端扩展能力（弹窗、转换器、动画等）
- `Azrng.Core`：实体基类、扩展方法、工具类、结果包装、异常体系（如项目已使用则沿用）

### 技术选择原则
如果仓库已经有真实实现，以现有代码为准，不要强行重构或替换技术栈。
技术债务与重构判断遵循根 `AGENTS.md` 的全局规则。

---

## 主动建议规则
- 发现业务逻辑放错层、ViewModel 与 Service 职责混杂、DTO 契约不稳定、结果包装不一致或异常处理不统一时，应主动提醒
- 发现状态流转、命令执行、消息传递或业务规则可能影响既有流程、用户可见结果或数据一致性时，必须先说明风险，不得直接扩大修改
- 发现可以复用既有 ViewModel 基类、Service、DTO、映射逻辑、结果包装、异常体系或 `IMessenger` 消息类型时，应优先建议复用
- 不确定业务规则、状态含义、DTO 字段语义或外部服务行为时，应先查证或列为待确认项，禁止按通用经验补写规则

---

## 推荐目录结构

- 应用层目录建议聚焦在 `src/AppName/ViewModels/`、`src/AppName/Services/`、`src/AppName/Models/` 下组织，优先复用现有结构，不强制迁移。

```text
src/AppName/
├── ViewModels/
│   ├── Base/
│   ├── Dialogs/
│   └── [Module]/
├── Services/
│   ├── Interfaces/            # 服务接口定义
│   └── Implementations/       # 服务实现
└── Models/
    ├── Entities/              # 数据实体
    └── DTOs/                  # 数据传输对象
```

### 目录职责映射

按职责将类放到对应目录，文件命名与类名保持一致；View（`.xaml`）的组织见 `ui-AGENTS.md`。

| 目录 | 放什么类 | 命名约定 |
| --- | --- | --- |
| `ViewModels/Base/` | ViewModel 基类 | `*ViewModelBase.cs` |
| `ViewModels/[Module]/`、`ViewModels/Dialogs/` | 模块 / 对话框 ViewModel（状态 + 命令编排） | `{名称}ViewModel.cs` |
| `Services/Interfaces/` | 服务接口 | `I{业务域}Service.cs` |
| `Services/Implementations/` | 服务实现 | `{业务域}Service.cs` |
| `Models/Entities/` | 数据实体，一类一文件 | `{实体名}.cs` |
| `Models/DTOs/` | DTO（视图与业务层契约），一类一文件 | `{名称}Dto.cs` |

---

## 阶段 2 — 业务逻辑实现（业务实现角色主导）

**触发条件**：用户发出「开始业务逻辑开发」指令

**入场要求**：前端视图已完成，`src/AppName/Models/DTOs/` 契约文件已明确

**工作内容**：
1. 严格按照 DTO 中的类型定义实现 ViewModel 命令编排与 Service 层逻辑。
2. 遵循 `ViewModel → Service → Repository → Model` 完整分层。
3. 涉及数据访问或数据库结构变更时，结合 `infrastructure-AGENTS.md` 同步补齐仓储实现与迁移脚本。
4. 每个关键服务方法和关键 ViewModel 命令都应能通过测试独立验证。

**字段命名约定**：
- C# 类型 / 属性：PascalCase
- DTO / 序列化字段：遵循当前协议约定并保持一致
- 数据库存储字段和映射规则见 `infrastructure-AGENTS.md`

**门控规则**：
- 核心业务逻辑测试通过后，才允许进入阶段 3。

---

## 应用层规则

### 分层边界规则
- View 只负责展示，不承载业务逻辑
- ViewModel 负责状态管理、命令触发、调用 Service 与处理用户可见结果
- Service 负责业务逻辑、流程编排和规则校验，不直接操作 View
- Repository 是数据访问边界，具体实现细则见 `infrastructure-AGENTS.md`
- Model / DTO 负责承载业务数据，不在其中夹带 UI 行为

### ViewModel 规则
- ViewModel 中可以组织用户操作流程，但禁止直接写 SQL 或直接依赖存储细节
- 命令执行后的成功、失败、空状态必须显式反馈到界面状态
- 异常在 ViewModel 层转换为用户友好的提示信息，不把底层异常原样暴露给用户
- 跨 ViewModel 协作优先使用 `IMessenger` 或显式服务，不依赖静态全局状态
- 禁止在 ViewModel、Service、Repository、Model、DTO 等 C# 类中使用主构造函数（Primary Constructor），统一使用显式构造函数

### Service 层规则
- 服务类必须实现接口，接口定义在 `Services/Interfaces/` 目录
- 服务方法必须优先采用异步形式（返回 `Task<T>` 或 `ValueTask<T>`）
- 服务层处理所有业务逻辑，不直接访问 Repository 以外的数据依赖
- 服务层异常必须统一封装，抛出项目异常体系中的业务异常类型
- 若项目使用标记接口模式批量注册（如 `ITransientDependency`），服务类应实现对应接口

### DTO 与模型规则
- DTO 是视图层与业务逻辑层之间的稳定契约，变更时必须同步更新相关映射和调用方
- 若仓库已有真实实体或 DTO 结构，优先沿用现状，不为模板强行改名或重组
- 数据转换规则应集中放在 Service 或明确的映射层，不散落在 View 或 Repository 调用点

### 代码组织规范
- 一个文件只放一个主类型：ViewModel、Service 接口与实现、Entity、DTO 等，默认一个类一个文件，文件名与类名一致。
- DTO 目录组织：DTO 放 `Models/DTOs/`，一类一文件，不合并到 `Dtos.cs`。
- 触发拆分的信号：职责混杂、同文件出现多个主类型、参数或字段持续堆叠、方法跨多个不相关业务时，应主动拆分。
- 允许例外：`private` / `internal` 且只服务当前文件的小辅助类型、与主类型强绑定的局部 mapping extension、测试文件中只服务当前测试类的小型 fixture。
- 反模式：`Dtos.cs` 长期堆放多个 DTO；把 ViewModel 专用模型内联写在 ViewModel 文件里；业务逻辑塞进 View。

### 统一结果包装
- 服务层方法返回值统一使用项目既有的结果包装类型（如 `ResultModel<T>`）
- 成功响应：`ResultModel<T>.Success(data)`
- 错误响应：`ResultModel<T>.Failure(message, errorCode)`
- 对于仅表示操作结果的方法，也应保持统一的结果语义，不返回随意结构

### 异常处理规范
- 业务异常继承项目既有的异常基类，常见分类：
  - 业务逻辑异常
  - 参数校验异常
  - 资源不存在异常
  - 禁止访问异常
  - 服务器内部错误异常
- 禁止抛出非项目体系的随意自定义异常
- 异常处理应尽量保留可定位信息，同时对用户输出友好、可理解的提示

### 移动端应用生命周期
- 应用生命周期事件（`OnStart`、`OnResume`、`OnSleep` / `OnStop`）中只做必要的初始化和状态保存
- 后台返回前台时，应根据业务需要刷新关键数据
- 网络状态变化时，应通过 `Connectivity.Current` 监听并给出适当提示
- 权限请求（相机、位置、存储等）必须在真正需要时才请求，禁止启动时一次性全部请求

### 代码注释规范
- 必须补 XML 注释的位置：
  - ViewModel 公共命令与关键属性：说明用户操作意图与可观察状态。
  - Service 接口方法与核心实现方法：说明该能力做什么，不写实现细节。
- 优先补行内注释的位置：复杂状态流转、命令分支、外部调用编排，说明为什么这样处理。
- 不要补的注释：普通属性的 get / set、简单 `if` / `return`、"返回结果"这类复述代码的低价值注释。
- 注释统一使用中文，不在注释里泄露密钥、token、连接串或真实生产地址。

---

## 测试规则

### 提交前最小回归
- 默认执行：项目现有的编译、静态检查或单元测试
- ViewModel、命令、Service 改动：至少验证一项业务流程、状态流转或错误分支
- DTO、映射、结果包装改动：至少验证输入输出结构和边界条件
- 与数据访问、集成或配置相关的改动：至少补一项联调或等价验证，证明真实链路生效

### 总体要求
- 影响行为的改动应优先补充或更新测试
- 若本次改动未补测试，必须在最终说明中写明原因和风险
- 测试应覆盖真实业务行为，而不是只覆盖静态分支

### 应用层测试
- ViewModel 命令执行、属性变更、消息发送发生变化时，应补充对应测试
- Service 层业务逻辑、数据转换、异常处理发生变化时，应补充对应测试
- 推荐使用 xUnit 作为测试框架

### 外部依赖与数据
- 测试中不要真实调用外部服务，统一使用 mock、stub 或测试替身
- 测试数据应尽量最小化、可读、可重复执行
- 不要让测试依赖本地人工状态或不可控外部环境

### 无法执行测试时
- 必须说明未执行的测试类型
- 必须说明未执行原因
- 必须说明潜在影响范围和风险

---

文件结束。
