---
rule_id: backend-agents
version: 1.45.0
last_updated: 2026-09-03
dependencies: [agents-root]
---

# 后端规则

## 适用范围

- 作用域：服务端实现、接口、服务、数据访问、缓存、异常处理与后端测试
- 触发场景：涉及接口、AppService、数据访问、缓存、权限、异常处理或测试时阅读

### 常见任务入口
- 改接口响应或状态码：先看 Controller 规则、统一响应格式与异常处理规范
- 改业务流程或状态流转：先看 AppService / DomainService 规则与权限约束
- 改查询、分页、缓存、迁移：先看数据访问规则与验证要求
- 补服务端回归：先看 `提交前最小回归` 与测试规则

---

## 技术栈

### 后端
- .NET + ASP.NET Core Web API；版本以仓库现有 `TargetFramework` 为准，不主动升级，新项目先联网检索当前最新正式版
- 单项目整洁架构：一个 Web API 项目内以文件夹划分层（Domain / Application / Infrastructure / 宿主入口），层职责与依赖方向和多项目模板 `dotnetLayered` 保持一致

<!-- include: shared/backend/core-libs.md -->

<!-- include: shared/backend/database-libs.md -->

### 数据库
- 默认使用 PostgreSQL
- 可根据项目需求切换为其他数据库（MySQL、SQL Server、SQLite 等）
- ORM：Entity Framework Core，通过 `Common.EFCore` 系列包访问
- 测试环境可使用 `Common.EFCore.InMemory` 内存数据库

<!-- include: shared/backend/pg-time-convention.md -->

<!-- include: shared/backend/time-contract-rules.md -->

<!-- include: shared/application/tech-choice.md -->

---

<!-- include: shared/backend/advisory.md -->
---

## 单项目整洁架构

### 推荐目录结构

若仓库尚未形成稳定结构，可优先参考以下组织方式；若仓库已有实现，以现状为准，不强制迁移。

```text
project-root/
├── src/
│   └── YourProject/
│       ├── Domain/                    # 领域层（最内层业务）
│       │   ├── Entities/              # EF Core 实体
│       │   ├── Enums/                 # 跨模块枚举
│       │   ├── Adapters/              # 外部服务适配器接口（按需）
│       │   └── {业务域}/              # 领域服务接口与实现（按需）
│       ├── Application/               # 应用层
│       │   └── {业务域}/              # 应用服务接口与实现
│       │       └── Dto/               # 请求 / 响应 / 查询参数
│       ├── Infrastructure/            # 基础设施层
│       │   ├── Data/                  # DbContext、EF Core 实体配置
│       │   ├── Adapters/              # 外部服务适配器实现（按需）
│       │   └── Storage/               # 文件 / 对象存储封装（按需）
│       ├── Controllers/               # 宿主入口：API 控制器
│       ├── Middleware/                # 中间件（按需）
│       ├── Extensions/                # 扩展方法、DI 注册扩展
│       ├── MigrationSql/              # SQL 迁移脚本（Azrng.SqlMigration）
│       ├── Program.cs
│       ├── appsettings.json
│       └── YourProject.csproj
├── tests/
│   └── YourProject.Tests/
└── ...
```

### 文件夹职责与命名

按业务职责将类放到对应文件夹，文件命名与类名保持一致；与本文件 `代码组织规范`、`数据模型规则` 中的约定保持统一。

| 文件夹 | 放什么类 | 命名约定 |
| --- | --- | --- |
| `Domain/Entities/` | EF Core 实体，继承 Azrng 基类，一个实体一个文件 | `{实体名}.cs` |
| `Domain/Enums/` | 跨模块复用的枚举；仅服务单个 DTO 的可随主类型 | `{枚举名}.cs` |
| `Domain/Adapters/` | 外部服务适配器接口 | `I{名称}Adapter.cs` |
| `Domain/{业务域}/` | 领域服务接口与实现（被多个 AppService 复用的公共领域操作） | `I{X}DomainService.cs`、`{X}DomainService.cs` |
| `Application/{业务域}/` | 应用服务接口与实现（用例编排、DTO 与实体映射） | `I{业务域}AppService.cs`、`{业务域}AppService.cs` |
| `Application/{业务域}/Dto/` | 请求 / 响应 / 查询参数，Request 与 Response 各自独立文件 | `CreateOrderRequest.cs`、`OrderDetailResponse.cs`、`OrderQuery.cs` |
| `Infrastructure/Data/` | DbContext、EF Core 实体配置 | `{项目名}DbContext.cs`、`{实体名}Etc.cs`（继承 Azrng 配置基类） |
| `Infrastructure/Adapters/` | 适配器实现（HTTP 调用、第三方 SDK 封装） | `{名称}Adapter.cs` |
| `Infrastructure/Storage/` | 文件 / 对象存储封装 | `{名称}Storage.cs` |
| `Controllers/` | API 控制器，仅做 HTTP 编排，不含业务逻辑 | `{资源}Controller.cs` |
| `Middleware/` | 中间件 | `{名称}Middleware.cs` |
| `Extensions/` | 扩展方法、DI 注册扩展 | `{类型}Extensions.cs` |
| `MigrationSql/` | SQL 迁移脚本 | `版本号.sql` / `版本号.txt`（如 `1.0.0.sql`、`1.1.0.txt`，不含描述文字） |

### 依赖方向约定（单项目关键防线）

```text
Controllers → Application → Domain ← Infrastructure
```

单项目内没有项目引用做编译期隔离，依赖方向完全靠约定维持。AI 每次新增或修改类时必须自查引用方向，阶段 1 门控包含此项：

- `Domain/`：只引用本文件夹内类型与基础 NuGet 包（`Azrng.Core`、`Common.EFCore` 的实体基类、`IBaseRepository<T>`、`IUnitOfWork`）；禁止引用 `Application/`、`Infrastructure/`、`Controllers/` 的类型
- `Application/`：可引用 `Domain/`；禁止引用 `Controllers/`、`Middleware/`，禁止出现 `HttpContext` 等 HTTP 相关类型
- `Infrastructure/`：可引用 `Domain/`（实体、适配器接口）；禁止引用 `Application/`、`Controllers/`
- `Controllers/`：只注入 `Application/` 的 `I*AppService`；禁止注入 DbContext、`IBaseRepository<T>` 或 Infrastructure 实现类
- 实体只在 AppService / DomainService 与数据访问之间流动，禁止作为 API 返回值

### 按需使用与演进
- 小型 CRUD 不强制建满所有文件夹：`Domain/{业务域}/`（领域服务）、`Domain/Adapters/`、`Infrastructure/Adapters/`、`Infrastructure/Storage/` 都按需出现，不为架构仪式感创建空文件夹
- 单一用例、不复用的逻辑直接写在 AppService；被多个 AppService 复用或领域规则复杂时，才下沉为 DomainService
- 出现以下信号时，考虑按 `dotnetLayered` 模板拆分为多项目：多个宿主（Web API + Worker）、领域逻辑需要被其他服务复用、团队并行开发频繁冲突、需要编译期强制依赖方向
- 文件夹与 `dotnetLayered` 各层一一对应（`Domain/` → IDomain / Domain、`Application/` → IApplication / Application、`Infrastructure/Data/` → EntityFramework、`Infrastructure/Adapters/` → Adapter、`Controllers/` → Service），拆分时可平滑迁移

---

## 阶段 1 — 后端实现

**触发条件**：用户发出「开始开发」或「开始后端开发」指令

**入场要求**：阶段 0 设计文档或接口契约已明确；若为例外情况任务，可直接进入实现

**工作内容**：
1. 先根据需求定位涉及的文件夹层级，再在正确层内实现对应职责。
2. 保持依赖方向约定，禁止跨层直接引用规避边界。
3. 所有增删改必须真实生效，并能通过 Swagger 或测试独立验证。
4. 涉及数据库结构变更时，同步补齐迁移脚本和初始化说明。

**门控规则**：
- 核心接口路径已可通过 Swagger 独立验证。
- 影响行为的后端改动已补测试或在交付说明中解释未补原因。
- 文件夹依赖方向未被破坏。
- 后续实现发现契约缺口时，按 `collaboration-AGENTS.md`「纠错、回退与重构」回到 DTO / 接口定义变更流程，不在实现层绕过契约。

---

## 后端规则

### Controller 规则
- Controller 只负责 HTTP 处理，不包含业务逻辑。
- 使用 `[ApiController]` 特性，自动处理 400 响应。
- 使用 `[Route("api/[controller]")]` 统一路由前缀。
- 通过构造函数注入 `I*AppService`；禁止直接操作 DbContext 或 `IBaseRepository<T>`。
- Controller action 直接返回 `Task<T>` 并一行转发 AppService；不写 `Ok()`，不手动构造 `ResultModel`，统一包装由全局过滤器与异常中间件完成（见 `统一响应格式`）。

### AppService（应用服务）规则
- 接口 `I{业务域}AppService` 与实现放 `Application/{业务域}/`，方法必须是异步的（返回 `Task<T>`）。
- 承担用例编排与 DTO / 实体映射；使用 DTO 传递数据，不暴露实体。
- 可注入 `IBaseRepository<T>` / `IUnitOfWork` 做数据访问，注入 `I*DomainService` / `I*Adapter` 复用领域与外部能力。
- 成功直接返回业务对象，失败抛出 Azrng 业务异常（见 `统一响应格式` 与 `异常处理规范`）。
- 实现类实现标记接口（`ITransientDependency` / `IScopedDependency` / `ISingletonDependency`）以自动注册。
- 禁止在 Controller、AppService、DomainService、Entity、DTO 等 C# 类中使用主构造函数（Primary Constructor），统一使用显式构造函数。

### DomainService（领域服务）规则
- 封装被多个 AppService 复用的公共领域操作或稳定领域规则；接口与实现放 `Domain/{业务域}/`。
- 返回实体或领域对象，不做 DTO 映射，不接触 HTTP。
- 可注入 `IBaseRepository<T>` / `IUnitOfWork` 与适配器接口。

<!-- include: shared/backend/code-organization-dotnet.md -->
- 请求 / 响应 / 查询参数按业务域归入 `Application/{业务域}/Dto/`，不合并到 `{模块}Dto.cs`

<!-- include: shared/application/kiss.md -->

### 数据模型规则
- 实体类继承自 Azrng 基类（如 `IdentityOperatorStatusEntity`，自带雪花 Id、审计与软删除字段），放 `Domain/Entities/`。
- DTO 按用途区分（查询参数、创建请求、更新请求、响应结果），文件组织见 `代码组织规范`。
- 跨模块枚举放 `Domain/Enums/` 目录。
- 禁止在 DTO 中暴露数据库内部字段。

### 数据访问规则
- 数据访问默认直接注入 `Common.EFCore` 提供的 `IBaseRepository<T>`，不自建仓储层；只读查询优先使用 `EntitiesNoTacking`。
- 使用 LINQ 查询，避免原生 SQL。
- 多步原子写操作优先使用 `IUnitOfWork` 事务，不直接操作 `IDbContextTransaction`。
- 分页列表查询使用 `GetPageRequest` + `ToPageListAsync` + `GetQueryPageResult<T>`，不手写 `CountAsync()` + `Skip().Take()`。
- 软删除统一使用实体基类的 `Deleted` 字段；包不自动做全局过滤，默认在查询中显式过滤 `!x.Deleted`，或在实体配置类中统一配置查询过滤器。
- DbContext 与 EF Core 实体配置类（`{实体名}Etc.cs`）放 `Infrastructure/Data/`，DbContext 通过 `ApplyConfigurationsFromAssembly` 扫描应用配置。
- 新增实体必须三处同步：实体类、DbContext 的 `DbSet<{实体名}>`、`Infrastructure/Data/{实体名}Etc.cs` 配置类；表名以配置类的 `ToTable` 为准，迁移 SQL 的表名必须与之一致——只登记 DbSet 不建配置类时 EF 模型缺失，运行期才暴露。

<!-- include: shared/backend/response-format.md -->

<!-- include: shared/backend/di-rules.md -->

<!-- include: shared/backend/exception-handling.md -->

<!-- include: shared/backend/redis-cache.md -->

<!-- include: shared/backend/memory-cache.md -->

<!-- include: shared/backend/db-migration.md -->

<!-- include: shared/backend/swagger.md -->

<!-- include: shared/backend/field-naming.md -->

### 代码注释规范
- 必须补 XML 注释的位置：
  - Controller action：用 `<summary>` 说明接口用途、关键参数、返回结果（Swagger 会消费这些说明）。
  - AppService / DomainService / Adapter 接口方法：说明该能力做什么，不写实现细节。
  - 不来自接口的实现类自有方法（接口未约束的核心方法）：同上，补 `<summary>` 说明该能力做什么。
- 接口已注释时，实现方法不重复注释，也不加 `/// <inheritdoc />`。
- 默认不写 `<remarks>`：实现类、实现方法上的 `<remarks>` 多为实现细节，按“不写实现细节”原则省略。
  - 例外：Controller action 需要向调用方暴露事件格式、鉴权要求、分页约定等对外契约信息时，可保留精简后的 `<remarks>`，但不写内部实现说明。
- 涉及时间字段的 DTO / 接口注释必须写明转换责任方：入参是本地墙钟还是 UTC、由哪一层完成转换（如“本地墙钟（服务层统一转换）”），而不是只标存储单位。
- 优先补行内注释的位置：涉及权限、状态流转、事务、缓存、幂等或降级的复杂业务分支，说明“为什么这样做”——包括 fail fast 的原因、为什么跳过某类脏数据、为什么保留某一层结构。
- 不要补的注释：普通属性的 get / set、简单 `if` / `return`、“返回结果”这类复述代码的低价值注释。
- 注释统一使用中文，不使用纯英文注释；不在注释里泄露密钥、token、连接串或真实生产地址。

<!-- include: shared/application/anti-patterns.md -->

<!-- include: shared/application/maintainability-check.md -->

---

## 测试规则

<!-- include: shared/backend/pre-commit-regression.md -->

<!-- include: shared/backend/general-requirements.md -->

### 后端测试
- 测试框架：xUnit + Moq
- Controller 测试：验证状态码、响应结构、参数校验
- AppService / DomainService 测试：验证业务流程编排与核心业务规则，使用 mock 隔离外部依赖
- 数据访问测试：使用 In-Memory 数据库或 Testcontainers
- 集成测试：使用 `WebApplicationFactory` 测试完整请求管道

<!-- include: shared/backend/dotnet-build-isolation.md -->

<!-- include: shared/external-dependencies.md -->

<!-- include: shared/no-test-fallback.md -->

---
