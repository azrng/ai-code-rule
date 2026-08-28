---
rule_id: backend-agents
version: 1.14.0
last_updated: 2026-08-27
dependencies: [agents-root]
---

# 后端规则

<!-- include: shared/backend/scope.md -->

<!-- include: shared/backend/common-tasks.md -->

---

<!-- include: shared/infra/tech-choice.md -->

---

<!-- include: shared/backend/advisory.md -->

---

## 技术栈

### 后端
- .NET 10 (2025 年已经发布正式版) + ASP.NET Core Web API
- Clean Architecture 分层架构：
  - `API`（表现层）→ `Application`（应用层）→ `Domain`（领域层）← `Infrastructure`（基础设施层）
- ORM：Entity Framework Core
- 测试框架：xUnit + Moq

### 核心库
- `Azrng.Core` (1.15.8)：基础类库（实体基类、扩展方法、工具类、结果包装、异常体系）
- `Azrng.Core.Json` (1.3.1)：JSON 序列化（基于 System.Text.Json）
- `Azrng.AspNetCore.Core` (1.3.0)：API 基础设施（统一响应、模型校验、异常中间件）
- `Azrng.SqlMigration` (0.4.0)：SQL 脚本迁移执行
- `Common.Cache.Redis` (2.0.0)：Redis 缓存封装（基于 StackExchange.Redis）

### 依赖注入规范
- 使用 Azrng 标记接口方式：
  - `ITransientDependency`：临时注入
  - `IScopedDependency`：作用域注入
  - `ISingletonDependency`：单例注入
- 在 Program.cs 中调用 `services.RegisterBusinessServices(assemblies)` 扫描并注册服务
- 服务类实现对应标记接口，自动被扫描注册到 DI 容器

---

### 阶段 2 — 后端实现（后端实现角色主导）

**触发条件**：用户发出「开始后端开发」指令

**入场要求**：前端页面已完成，接口需求已明确

**工作内容**：
1. 按照接口需求实现 ASP.NET Core Web API 接口
2. 遵循 `Controller → Application → Domain → Infrastructure` 分层架构
3. 涉及数据库结构变更时，使用 Azrng.SqlMigration 生成迁移脚本
4. 每个接口必须可通过 Swagger 或 Postman 独立验证

**字段命名约定**：
- C# 属性：PascalCase
- JSON 输出：camelCase（通过 System.Text.Json 配置自动转换）
- 数据库字段：snake_case（通过 EF Core 命名约定配置）

**门控规则**：
- 核心接口独立测试（Swagger / Postman）通过后，才允许进入阶段 3

---

## 后端规则（Clean Architecture）

### 分层依赖规则
```
API → Application → Domain ← Infrastructure
                ↑_______________|
```
- `Domain` 层：无外部依赖，包含核心业务模型
- `Application` 层：依赖 Domain，定义服务接口和 DTO
- `API` 层：依赖 Application，处理 HTTP 请求
- `Infrastructure` 层：依赖 Application 和 Domain，实现具体技术细节

### 层内目录组织

各层内部按职责与类型组织；公开契约类（DTO、实体、接口等）默认一个类一个文件，文件名与类名一致。

| 类型 | 所在层 / 目录 | 命名约定 |
| --- | --- | --- |
| Controller | `API/Controllers/` | `{资源}Controller.cs` |
| 应用服务接口 | `Application/Interfaces/` | `I{业务域}Service.cs` |
| 应用层 DTO | `Application/DTOs/{业务域}/` | `CreateOrderRequest.cs`、`OrderDetailResponse.cs` |
| 应用服务实现 | `Application/Services/` | `{业务域}Service.cs` |
| 实体 | `Domain/Entities/` | `{实体名}.cs` |
| 值对象（record） | `Domain/ValueObjects/` | `{名称}.cs` |
| 枚举 | `Domain/Enums/` | `{枚举名}.cs` |
| DbContext / EF 配置 | `Infrastructure/Data/` | `{项目名}DbContext.cs`、`{实体名}Configuration.cs` |
| 仓储实现 | `Infrastructure/Data/Repositories/` | `{实体名}Repository.cs` |
| SQL 迁移脚本 | `API/MigrationSql/` | `版本号.sql` / `版本号.txt`（如 `1.0.0.sql`、`1.1.0.txt`，不含描述文字） |

### Controller 层规则
- Controller 只负责 HTTP 处理，不包含业务逻辑
- 使用 `[ApiController]` 特性，自动处理 400 响应
- 使用 `[Route("api/[controller]")]` 统一路由前缀
- 注入 Application 层的服务接口，不直接依赖 Infrastructure
- Controller action 直接返回 `Task<T>` 并一行转发 Service；不写 `Ok()`，不手动构造 `ResultModel`，统一包装由全局过滤器与异常中间件完成（见 `统一响应格式`）

### Application 层规则
- 服务类实现接口，接口定义在 `Interfaces/` 目录
- 服务方法必须是异步的（返回 `Task<T>`）
- 使用 DTO 进行数据传递，不直接暴露 Domain 实体
- 使用 AutoMapper 进行对象映射（如需要）
- 业务异常抛出继承自 `BaseException` 的自定义异常
- 禁止在 Controller、Service、Repository、Entity、DTO 等 C# 类中使用主构造函数（Primary Constructor），统一使用显式构造函数

### Domain 层规则
- 实体类继承自基类（包含 Id、创建时间等公共属性）
- 值对象使用 record 类型，实现值相等比较
- 禁止依赖外部库，保持纯净

### Infrastructure 层规则
- DbContext 实现数据上下文，配置连接和映射
- 数据库结构变更通过 `Azrng.SqlMigration` SQL 脚本迁移管理（见 `数据库迁移规则`），不使用 EF Core Code-First Migrations
- 仓储实现依赖注入，遵循仓储模式
- 外部服务（邮件、文件存储等）实现在此层

### 代码组织规范
- 一个文件只放一个主类型：Controller、Service 接口与实现、Entity、值对象、DTO 等，默认一个类一个文件，文件名与类名一致。
- DTO 目录组织：应用层 DTO 放 `Application/DTOs/{业务域}/`，Request 与 Response 各自独立文件；领域事件放 `Domain/Events/`，不合并到 `Dtos.cs`。
- 触发拆分的信号：职责混杂、同文件出现多个主类型、参数或字段持续堆叠、方法跨多个不相关业务时，应主动拆分。
- 允许例外：`private` / `internal` 且只服务当前文件的小辅助类型、与主类型强绑定的局部 mapping extension、测试文件中只服务当前测试类的小型 fixture。
- 反模式：`Dtos.cs` 长期堆放十几个 Request / Response；把 Controller 专用 request 内联写在 Controller 文件里；业务逻辑放到 Infrastructure。

<!-- include: shared/application/kiss.md -->

### 数据访问规则
- 使用 Entity Framework Core 进行数据访问
- DbContext 通过构造函数注入
- 使用 LINQ 查询，避免原生 SQL
- 涉及事务时使用 `IDbContextTransaction`
- 软删除通过全局查询过滤器实现

### 统一响应格式
- 所有 API 响应统一为 `ResultModel<T>` 结构，由框架三件套自动完成，业务代码不手动包装：
  - `services.AddMvcResultPackFilter("/swagger")`：成功路径自动把 action 返回值包成 `ResultModel<T>`
  - `services.AddMvcModelVerifyFilter()`：模型校验失败统一转为失败 `ResultModel`
  - `app.UseGlobalException()`：业务异常与未处理异常统一映射为失败 `ResultModel`，必须放在中间件管道最外层
- Service 成功直接返回业务对象（`Task<T>`），失败抛出 Azrng 业务异常；新代码禁止手写 `ResultModel<T>.Success / Error` 手动包装（该写法仅存量代码迁移期可容忍）。
- HTTP 状态码默认按异常类型返回真实状态码（400 / 401 / 404 / 500）；`ResultModel` 的 `code` 字段承载业务码。

### 依赖注入规范
- 服务类必须实现以下接口之一：
  - `ITransientDependency`：每次请求创建新实例
  - `IScopedDependency`：每个请求作用域内单例
  - `ISingletonDependency`：应用程序生命周期单例
- Controller 通过构造函数注入服务
- 禁止使用静态服务定位器 pattern

### 异常处理规范
- 业务异常继承 `BaseException` 或其子类：
  - `LogicBusinessException`：业务逻辑异常
  - `ParameterException`：参数校验异常
  - `NotFoundException`：资源不存在
  - `ForbiddenException`：禁止访问
  - `InternalServerException`：服务器内部错误
- 异常由中间件统一捕获并转换为 `ResultModel` 响应
- 业务失败一律通过抛出上述异常表达；禁止在业务层手工构造失败 `ResultModel` 或直接设置错误状态码

### Redis 缓存使用规范
- 使用 `Common.Cache.Redis` 封装的 `IRedisProvider` 接口
- 通过 `services.AddRedisCacheStore()` 注册 Redis 服务
- 缓存操作必须设置合理的过期时间
- 热点数据使用本地缓存 + Redis 二级缓存
- 发布订阅场景使用 `PublishAsync`/`SubscribeAsync`
- 禁止在循环中频繁调用 Redis

### 数据库迁移规则
- 使用 `Azrng.SqlMigration` 进行数据库脚本迁移，不使用 EF Core Code-First Migrations
- 迁移脚本放在宿主项目下 `API/MigrationSql/` 目录，文件名按 `版本号.sql` 或 `版本号.txt` 命名（如 `1.0.0.sql`、`1.1.0.txt`），不含描述文字——去掉版本前缀与扩展名后的剩余部分会被整体解析为版本号；按版本号顺序执行
- 通过 `AddSqlMigrationService(...).AddAutoMigration()` 在应用启动时自动执行未应用的脚本；已执行版本由包自动记录在版本日志表，不重复执行，无需手工维护版本表
- 已应用的脚本不可修改；结构调整必须新增更高版本号的脚本
- 涉及数据的高风险结构变更，建议同步准备回滚脚本并在文档中说明恢复方式

### 代码注释规范
- 必须补 XML 注释的位置：
  - Controller action：用 `<summary>` 说明接口用途、关键参数、返回结果。
  - Application 层服务接口方法：说明应用能力，不写实现细节。
  - Domain 核心业务规则方法：说明规则意图。
  - 不来自接口的实现类自有方法（接口未约束的核心方法）：补 `<summary>` 说明该能力做什么。
- 接口已注释时，实现方法不重复注释，也不加 `/// <inheritdoc />`；只有实现类自有的、接口未定义的方法才需要补注释。
- 默认不写 `<remarks>`：实现类、实现方法上的 `<remarks>` 多为实现细节，按"不写实现细节"原则省略。
  - 例外：Controller action 需要向调用方暴露事件格式、鉴权要求、分页约定等对外契约信息时，可保留精简后的 `<remarks>`，但不写内部实现说明。
- 优先补行内注释的位置：涉及权限、状态流转、事务、缓存、幂等或降级的复杂业务分支，说明“为什么这样做”——包括 fail fast 的原因、为什么跳过某类脏数据、为什么保留某一层结构。
- 不要补的注释：普通属性的 get / set、简单 `if` / `return`、"返回结果"这类复述代码的低价值注释。
- 注释统一使用中文，不在注释里泄露密钥、token、连接串或真实生产地址。

<!-- include: shared/application/anti-patterns.md -->

<!-- include: shared/application/maintainability-check.md -->

---

## 测试规则

<!-- include: shared/backend/pre-commit-regression.md -->

<!-- include: shared/application/general-requirements.md -->

### 后端测试
- 单元测试使用 xUnit + Moq
- Controller 测试：验证状态码、响应结构、参数校验
- Service 测试：验证业务逻辑分支，使用 mock 隔离依赖
- Repository 测试：使用 In-Memory 数据库或 Testcontainers
- 集成测试：使用 WebApplicationFactory 测试完整请求管道

<!-- include: shared/external-dependencies.md -->

<!-- include: shared/no-test-fallback.md -->

---
