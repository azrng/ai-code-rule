---
rule_id: application-agents
version: 1.46.0
last_updated: 2026-09-03
dependencies: [agents-root]
---

# 应用规则

<!-- include: shared/application/scope.md -->

### 常见任务入口
- 改命令流程或状态切换：先看 `MVVM 模式规则`（见 `ui-AGENTS.md`）与统一结果包装
- 改 Service 或业务规则：先看 Service 层规则、应用层协作边界与异常处理规范
- 改 DTO、映射、结果包装：先看目录职责映射与统一结果包装
- 补应用层回归：先看 `提交前最小回归` 与测试规则

---

<!-- include: shared/application/tech-choice.md -->

---

<!-- include: shared/application/advisory.md -->

---

### 阶段 2 — 业务逻辑实现（业务实现角色主导）

**触发条件**：用户发出「开始后端开发」指令

**入场要求**：`src/AppName/Models/DTOs/` 接口契约文件已定稳

**工作内容**：
1. 严格按照 DTO 中的类型定义实现 ViewModel 命令编排与 Service 层逻辑
2. 遵循 `ViewModel → Service → Model` 的应用层职责边界，持久化实现细则移交 `infrastructure-AGENTS.md`
3. ViewModel 只负责编排命令、状态与交互反馈，不直接编写数据访问细节
4. 每个服务方法和关键 ViewModel 命令都必须可通过单元测试独立验证

**字段命名约定**：
- C# 属性：PascalCase

**门控规则**：
- 核心业务逻辑单元测试通过后，才允许进入阶段 3

---

## 业务逻辑层规则

### 目录职责映射

按职责将类放到对应目录，文件命名与类名保持一致；View（`.xaml`）的组织见 `ui-AGENTS.md`。

| 目录 | 放什么类 | 命名约定 |
| --- | --- | --- |
| `ViewModels/` | ViewModel（状态 + 命令编排） | `{名称}ViewModel.cs` |
| `Services/Interfaces/` | 服务接口 | `I{业务域}Service.cs` |
| `Services/` | 服务实现 | `{业务域}Service.cs` |
| `Models/DTOs/` | DTO（视图与业务层契约），一类一文件 | `{名称}Dto.cs` |
| `Models/Entities/` | 数据实体，一类一文件 | `{实体名}.cs` |

### Service 层规则
- 服务类必须实现接口，接口定义在 `Services/Interfaces/` 目录
- 服务方法必须是异步的（返回 `Task<T>` 或 `ValueTask<T>`）
- 服务层处理所有业务逻辑，不直接承担视图渲染、控件操作与数据访问实现细节
- 服务层异常必须统一封装，抛出 Azrng 异常体系的业务异常类型
- 服务类实现 `ITransientDependency`/`IScopedDependency`/`ISingletonDependency` 接口，通过 `RegisterBusinessServices` 批量注册
- 禁止在 ViewModel、Service、Repository、Model、DTO 等 C# 类中使用主构造函数（Primary Constructor），统一使用显式构造函数

### 应用层协作边界
- ViewModel 只依赖 Service 接口，不直接依赖 Repository、数据库连接或 SQL
- 若业务需要持久化能力，可在应用层定义仓储接口或调用边界，但具体实现与数据访问规则统一放在 `infrastructure-AGENTS.md`
- 涉及缓存、数据库、文件系统或第三方 SDK 的具体接入时，必须同时阅读 `infrastructure-AGENTS.md`

<!-- include: shared/application/code-organization.md -->

<!-- include: shared/application/kiss.md -->

<!-- include: shared/application/result-wrapping.md -->
- 异常处理使用 Azrng.Core 异常体系

### 异常处理规范
- 业务异常继承 `BaseException` 或其子类：
  - `LogicBusinessException`：业务逻辑异常
  - `ParameterException`：参数校验异常
  - `NotFoundException`：资源不存在
  - `ForbiddenException`：禁止访问
  - `InternalServerException`：服务器内部错误
- 异常在 ViewModel 层捕获并转换为用户友好的提示信息

<!-- include: shared/application/comment-rules.md -->

<!-- include: shared/application/anti-patterns.md -->

<!-- include: shared/application/maintainability-check.md -->

---

## 测试规则

<!-- include: shared/application/pre-commit-regression.md -->
<!-- include: shared/common/general-requirements.md -->

<!-- include: shared/application/view-layer-testing.md -->

### 业务逻辑层测试
- ViewModel 命令执行、属性变更、消息发送发生变化时，应补充对应测试
- Service 层业务逻辑、数据转换、异常处理发生变化时，应补充对应测试
- 使用 xUnit 作为测试框架

<!-- include: shared/desktop/external-dependencies.md -->

<!-- include: shared/no-test-fallback.md -->

---
