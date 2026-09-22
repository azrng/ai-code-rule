---
rule_id: backend-agents
version: 1.48.0
last_updated: 2026-09-22
dependencies: [agents-root]
---

# 后端规则

<!-- include: shared/backend/scope.md -->

<!-- include: shared/backend/common-tasks.md -->

---

## 技术栈

### 后端
- .NET + ASP.NET Core Web API；版本以仓库现有 `TargetFramework` 为准，不主动升级，新项目先联网检索当前最新正式版
- 分层架构（9 个项目），按职责严格划分

<!-- include: shared/backend/core-libs.md -->

<!-- include: shared/backend/database-libs.md -->

### 数据库
- 默认使用 PostgreSQL
- 可根据项目需求切换为其他数据库（MySQL、SQL Server、SQLite 等）
- ORM：Entity Framework Core，通过 `Common.EFCore` 系列包访问
- 测试环境可使用 `Common.EFCore.InMemory` 内存数据库

<!-- include: shared/backend/pg-time-convention.md -->

---

<!-- include: shared/backend/advisory.md -->
- 发现 Application 与 Domain 职责混杂或依赖方向被破坏（外层引用内层以外的方向）时，应主动提醒
---

## 分层架构

### 项目结构

```text
src/
├── YourProject.Core/
├── YourProject.IDomain/
├── YourProject.Adapter/
├── YourProject.EntityFramework/
├── YourProject.Domain/
├── YourProject.IApplication/
├── YourProject.Application/
├── YourProject.Storage/
└── YourProject.Service/
```

### 各层职责

| 层 | 项目 | 职责 | 依赖 | 禁止 |
| --- | --- | --- | --- | --- |
| 基础层 | Core | 公共 DTO、工具类、安全、缓存配置 | 无 | 禁止依赖任何业务层 |
| 领域接口 | IDomain | 实体定义、领域服务接口、仓储接口 | Core | 禁止依赖 Adapter / EntityFramework / Domain |
| 适配器 | Adapter | 外部 HTTP 服务调用、第三方 SDK 封装 | Core, IDomain | 禁止依赖 Domain / Application |
| 数据访问 | EntityFramework | DbContext、EF Core 实体配置、自定义仓储实现 | Core, IDomain | 禁止依赖 Domain / Application |
| 领域实现 | Domain | 核心业务逻辑，编排 Adapter 和数据访问 | IDomain, Adapter, EntityFramework | 禁止依赖 IApplication / Application |
| 应用接口 | IApplication | 应用服务接口定义、CQRS 请求 / 响应 | Core, IDomain | 禁止依赖 Domain / Application |
| 应用实现 | Application | 应用服务实现、业务流程编排 | IApplication, Domain | 禁止依赖 Service |
| 存储 | Storage | 文件上传下载、对象存储操作 | Core | 禁止依赖 Application / Service |
| 宿主层 | Service | Controller、中间件、DI 注册、配置 | Application, Storage | 禁止包含业务逻辑 |

- Storage 的能力接口定义在 Core（Storage 仅依赖 Core），实现放在 Storage 项目；Application / Domain 通过 Core 中的接口使用存储能力，Service 只负责装配。禁止 Controller 绕过业务层直接调用存储实现。

### 层内目录组织

各层内部按业务域与类型组织；公开契约类（DTO、实体、接口等）默认一个类一个文件，文件名与类名一致。

| 类型 | 所在层 / 目录 | 命名约定 |
| --- | --- | --- |
| 应用服务接口 | `IApplication/{业务域}/` | `I{业务域}AppService.cs` |
| 应用层 DTO（请求 / 响应 / 查询） | `IApplication/{业务域}/Dto/` | `CreateOrderRequest.cs`、`OrderDetailResponse.cs` |
| 领域服务接口 | `IDomain/{业务域}/` | `I{业务域}DomainService.cs` |
| 实体 / 值对象 | `IDomain/{业务域}/` | `{实体名}.cs` |
| 自定义仓储接口（仅复杂查询需要时，位置与判定规则见 `EntityFramework（数据访问层）规则`） | `IDomain/{业务域}/` | `I{实体名}Repository.cs` |
| EF Core 实体配置 | `EntityFramework/` | `{实体名}Etc.cs`（继承 Azrng 配置基类） |
| DbContext | `EntityFramework/` | `{项目名}DbContext.cs` |
| 数据库迁移脚本 | `Service/wwwroot/MigrationSql/` | `版本号.sql` / `版本号.txt`，命名细则见 `数据库迁移规则` |
| 外部服务调用 / 第三方封装 | `Adapter/Services/`；HTTP 客户端包装按需放 `Adapter/HttpClients/` | `{名称}Adapter.cs`、`I{名称}HttpClient.cs` / `{名称}HttpClient.cs` |
| 跨业务域通用 DTO / 枚举 / 常量 / 工具 | `Core/Models/`、`Core/Enums/` 等 | `{名称}.cs` |
| 领域业务对象 Bo（领域服务 / 适配器接口签名用） | `IDomain/{业务域}/Bo/`；Adapter 的 Bo 在 `IDomain/Adapter/Bo/{外部系统}/` | `SendSessionMessageBo.cs` |
| 文件 / 对象存储 | `Storage/` | `{名称}Storage.cs` |
| Controller / 中间件 / DI 注册 | `Service/` | `{资源}Controller.cs` 等 |

---

## 阶段 1 — 后端实现

**触发条件**：用户发出「开始开发」或「开始后端开发」指令

**入场要求**：阶段 0 设计文档或接口契约已明确；若为例外情况任务，可直接进入实现

**工作内容**：
1. 先根据需求定位涉及的层级，再在正确层内实现对应职责。
2. 保持依赖方向正确，禁止通过偷渡引用规避分层边界。
3. 所有增删改必须真实生效，并能通过 Swagger 或测试独立验证。
4. 涉及数据库结构变更时，同步补齐迁移脚本和初始化说明。

**门控规则**：
- 关键接口路径已可通过 Swagger 独立验证。
- 核心业务改动已补测试或在交付说明中解释未补原因。
- 分层依赖方向未被破坏。
- 后续实现发现契约缺口时，按 `collaboration-AGENTS.md`「纠错、回退与重构」回到 DTO / 接口定义变更流程，不在实现层绕过契约。

---

## 后端规则

### Service（宿主层）规则
- Controller 只负责 HTTP 处理，不包含业务逻辑。
- 使用 `[ApiController]` 特性，自动处理 400 响应。
- 使用 `[Route("api/[controller]")]` 统一路由前缀。
- 通过构造函数注入 IApplication 层的服务接口。
- Controller action 直接返回 `Task<T>` 并一行转发 AppService；不写 `Ok()`，不手动构造 `ResultModel`，统一包装由全局过滤器与异常中间件完成（见 `统一响应格式`）。
- DI 注册、中间件配置、Swagger 配置等都在此层完成。
- `Service` 项目按需引用 `DotNetEnv`，用于本地开发时加载仓库根目录 `.env`；`Program.cs` 在 `WebApplication.CreateBuilder(args)` 之前调用 `Env.NoClobber().TraversePath().Load()`，再进入 `Startup` 装配。`.env` 必须加入根 `.gitignore`，敏感值不得提交，`.env.example` 只保留键名或非敏感示例值。
- 禁止在 Controller、AppService、DomainService、Repository、Entity、DTO 等 C# 类中使用主构造函数（Primary Constructor），统一使用显式构造函数。

### IApplication（应用接口层）规则
- 按业务域拆分接口文件夹（如 `UserManager/`、`OrderManager/`）。
- 接口方法定义清晰的入参和返回类型。
- 使用 DTO 作为数据契约，不暴露领域实体；对 IDomain 的依赖仅用于复用枚举、值对象等共享定义。
- 接口方法必须是异步的（返回 `Task<T>`）。
- 命令型操作（新建、编辑、删除、启用、禁用等）默认返回 `Task<bool>` 表示是否成功；除非调用方在同一请求中确实需要新建 / 更新后的服务端派生字段、主键或状态，否则不返回完整详情 DTO——需要详情时由独立的查询接口提供。

### Application（应用实现层）规则
- 实现类实现对应标记接口以自动注册 DI。
- 编排 Domain 层服务完成业务流程。
- 负责事务管理和跨领域协调。
- DTO 与实体之间的转换在此层完成。
- 校验职责边界：请求 DTO 已由 `[Required]`、范围特性或 `[ApiController]` 模型校验覆盖的必填与格式约束，无需使用 `Require` / `RequireId` 等私有辅助方法做同义重复校验。

### IDomain（领域接口层）规则
- 定义领域服务接口（按业务域拆分）。
- 定义实体模型和值对象。
- 定义仓储接口。
- 禁止依赖具体实现层（Adapter、EntityFramework、Domain）。

### Domain（领域实现层）规则
- 实现核心业务逻辑和业务规则。
- 编排 Adapter（外部服务）和 EntityFramework（数据访问）。
- 实体类继承自 Azrng 基类（包含 Id、创建时间等公共属性）。
- 业务异常抛出继承自 Azrng 异常体系的自定义异常。

### EntityFramework（数据访问层）规则
- 数据访问默认直接注入 `Common.EFCore` 提供的 `IBaseRepository<T>`，不为每个实体创建自定义仓储；仅当查询逻辑复杂、需要封装复用时才定义 `I{实体名}Repository`（接口放 `IDomain/{业务域}/`，实现放 `EntityFramework/Repositories/`）。
- 使用 LINQ 查询，避免原生 SQL。
- 多步原子写操作优先使用 `IUnitOfWork` 事务，不直接操作 `IDbContextTransaction`。
- 软删除统一使用实体基类的 `Deleted` 字段；包不自动做全局过滤，默认在查询中显式过滤 `!x.Deleted`，或在实体配置类中统一配置查询过滤器。
- EF Core 实体配置类（`{实体名}Etc.cs`）在此层定义，DbContext 通过 `ApplyConfigurationsFromAssembly` 扫描应用。
- 新增实体必须三处同步：实体类、DbContext 的 `DbSet<{实体名}>`、`{实体名}Etc.cs` 配置类；表名以配置类的 `ToTable` 为准，迁移 SQL 的表名必须与之一致——只登记 DbSet 不建配置类时 EF 模型缺失，运行期才暴露。
- 数据库迁移脚本位置与规则见 `数据库迁移规则`。

### Adapter（适配器层）规则
- 封装外部 HTTP 服务调用默认通过 `Common.HttpClients`（`IHttpHelper`）完成，库选型与能力边界见核心库清单。
- 封装第三方 SDK 集成。
- 提供统一的错误处理和重试机制。
- 外部服务响应与内部模型转换在此层完成。

### Core（基础层）规则
- 提供全局共享的 DTO、枚举、常量。
- 提供通用工具类和扩展方法。
- 提供安全相关的配置和工具。
- 禁止依赖任何业务层项目。

### 代码组织规范
- 一个文件只放一个主类型：实体、DTO、Request、Response、领域服务接口、仓储接口等公开契约类，默认一个类一个文件，文件名与类名一致。
- DTO 目录组织：应用层 DTO 放 `IApplication/{业务域}/Dto/`，Request 与 Response 各自独立文件；跨业务域通用 DTO 放 Core 层对应目录，不合并到 `Dtos.cs`。**通用 = 被多个业务域复用**（公共枚举 / 常量 / 跨域包装）；单业务域的 DTO 即使接口、实现、控制器三方都在用也放 `IApplication/{业务域}/Dto/`——跨层使用是应用层 DTO 的常态，不构成放 Core 的判据。
- 触发拆分的信号：职责混杂、同文件出现多个主类型、参数或字段持续堆叠、方法跨多个不相关业务时，应主动拆分。
- 允许例外：`private` / `internal` 且只服务当前文件的小辅助类型、与主类型强绑定的局部 mapping extension、测试文件中只服务当前测试类的小型 fixture。
- 反模式：`Dtos.cs` 长期堆放十几个 Request / Response；把 Controller 专用 request 内联写在 Controller 文件里；跨层把多个不相关类型塞进同一文件。

<!-- include: shared/application/kiss.md -->

### 数据模型规则
- 实体类继承自 Azrng 基类（如 `IdentityOperatorStatusEntity`，自带雪花 Id、审计与软删除字段）。
- DTO 按用途区分（查询参数、创建请求、更新请求、响应结果），文件组织见 `代码组织规范`。
- 接口层（IApplication / IDomain）只定义 DTO 和接口，不包含实现。
- 禁止在 DTO 中暴露数据库内部字段。

<!-- include: shared/backend/response-format.md -->

<!-- include: shared/backend/di-rules.md -->

<!-- include: shared/backend/exception-handling.md -->

<!-- include: shared/backend/redis-cache.md -->

<!-- include: shared/backend/memory-cache.md -->

<!-- include: shared/backend/swagger.md -->

<!-- include: shared/backend/db-migration.md -->

<!-- include: shared/backend/field-naming.md -->

### 代码注释规范
- 必须补 XML 注释的位置：
  - Controller action：用 `<summary>` 说明接口用途、关键参数、返回结果（Swagger 会消费这些说明）。
  - IApplication / IDomain 接口方法：说明应用能力或领域能力，不写实现细节。
  - Domain 核心业务规则方法：说明规则意图。
  - 不来自接口的实现类自有方法（接口未约束的核心方法）：补 `<summary>` 说明该能力做什么。
- 接口已注释时，实现方法不重复注释，也不加 `/// <inheritdoc />`；只有实现类自有的、接口未定义的方法才需要补注释。
- 默认不写 `<remarks>`：实现类、实现方法上的 `<remarks>` 多为实现细节，按“不写实现细节”原则省略。
  - 例外：Controller action 需要向调用方暴露事件格式、鉴权要求、分页约定等对外契约信息时，可保留精简后的 `<remarks>`，但不写内部实现说明。
- 优先补行内注释的位置：涉及权限、状态流转、事务、缓存、幂等或降级的复杂业务分支，说明“为什么这样做”——包括 fail fast 的原因、为什么跳过某类脏数据、为什么保留某一层结构（如月份层、品种层）。
- 不要补的注释：普通属性的 get / set、简单 `if` / `return`、“返回结果”这类复述代码的低价值注释。
- 注释统一使用中文，不使用纯英文注释；不在注释里泄露密钥、token、连接串或真实生产地址。

<!-- include: shared/application/anti-patterns.md -->

---

## 测试规则

<!-- include: shared/backend/pre-commit-regression.md -->

<!-- include: shared/common/general-requirements.md -->

### 后端测试
- 测试框架：xUnit + Moq
- Controller 测试：验证状态码、响应结构、参数校验
- Application 测试：验证业务流程编排逻辑
- Domain 测试：验证核心业务规则，使用 mock 隔离外部依赖
- 数据访问测试：使用 In-Memory 数据库或 Testcontainers
- 集成测试：使用 `WebApplicationFactory` 测试完整请求管道

<!-- include: shared/external-dependencies.md -->

<!-- include: shared/no-test-fallback.md -->

---
