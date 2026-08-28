---
rule_id: application-agents
version: 1.15.0
last_updated: 2026-08-27
dependencies: [agents-root]
---

# 应用规则

<!-- include: shared/application/scope.md -->

<!-- include: shared/application/common-tasks.md -->

---

<!-- include: shared/infra/tech-choice.md -->

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

### 代码组织规范
- 一个文件只放一个主类型：ViewModel、Service 接口与实现、Entity、DTO 等，默认一个类一个文件，文件名与类名一致。
- DTO 目录组织：DTO 放 `Models/DTOs/`，一类一文件，不合并到 `Dtos.cs`。
- 触发拆分的信号：职责混杂、同文件出现多个主类型、参数或字段持续堆叠、方法跨多个不相关业务时，应主动拆分。
- 允许例外：`private` / `internal` 且只服务当前文件的小辅助类型、与主类型强绑定的局部 mapping extension、测试文件中只服务当前测试类的小型 fixture。
- 反模式：`Dtos.cs` 长期堆放多个 DTO；把 ViewModel 专用模型内联写在 ViewModel 文件里；业务逻辑塞进 View 的 code-behind。

<!-- include: shared/application/kiss.md -->

### 统一结果包装
- 服务层方法返回值统一使用 `ResultModel<T>` 包装
- 成功响应：`ResultModel<T>.Success(data)`
- 错误响应：`ResultModel<T>.Failure(message, errorCode)`
- 异常处理使用 Azrng.Core 异常体系

### 异常处理规范
- 业务异常继承 `BaseException` 或其子类：
  - `LogicBusinessException`：业务逻辑异常
  - `ParameterException`：参数校验异常
  - `NotFoundException`：资源不存在
  - `ForbiddenException`：禁止访问
  - `InternalServerException`：服务器内部错误
- 异常在 ViewModel 层捕获并转换为用户友好的提示信息

### 代码注释规范
- 必须补 XML 注释的位置：
  - ViewModel 公共命令与关键属性：用 `<summary>` 说明用户操作意图与可观察状态。
  - Service 接口方法：说明该能力做什么，不写实现细节。
  - 不来自接口的实现类自有方法（接口未约束的核心方法）：补 `<summary>` 说明该能力做什么。
- 接口已注释时，实现方法不重复注释，也不加 `/// <inheritdoc />`；只有实现类自有的、接口未定义的方法才需要补注释。
- 默认不写 `<remarks>`：实现类、实现方法上的 `<remarks>` 多为实现细节，按"不写实现细节"原则省略。
  - 例外：ViewModel 公共命令需要向使用者说明绑定约定、关联属性、触发条件等对外信息时，可保留精简后的 `<remarks>`，但不写内部实现说明。
- 优先补行内注释的位置：复杂状态流转、命令分支、外部调用编排，说明“为什么这样做”——包括 fail fast 的原因、为什么跳过某类输入、为什么保留某一步骤。
- 不要补的注释：普通属性的 get / set、简单 `if` / `return`、"返回结果"这类复述代码的低价值注释。
- 注释统一使用中文，不在注释里泄露密钥、token、连接串或真实生产地址。

<!-- include: shared/application/anti-patterns.md -->

<!-- include: shared/application/maintainability-check.md -->

---

## 测试规则

<!-- include: shared/application/pre-commit-regression.md -->
<!-- include: shared/application/general-requirements.md -->

<!-- include: shared/application/view-layer-testing.md -->

### 业务逻辑层测试
- ViewModel 命令执行、属性变更、消息发送发生变化时，应补充对应测试
- Service 层业务逻辑、数据转换、异常处理发生变化时，应补充对应测试
- 使用 xUnit 作为测试框架

<!-- include: shared/desktop/external-dependencies.md -->

<!-- include: shared/no-test-fallback.md -->

---
