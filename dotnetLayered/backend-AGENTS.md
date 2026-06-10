---
rule_id: backend-agents
version: 1.5.0
last_updated: 2026-06-10
dependencies: [agents-root]
---

# 后端规则

## 适用范围

- 作用域：服务端实现、接口、服务、数据访问、缓存、异常处理与后端测试
- 触发场景：涉及接口、Service、Repository、缓存、权限、异常处理或测试时阅读

### 阅读摘要
- 建议阅读：改接口、改 Service、改数据访问、改缓存、改权限、补后端验证
- 可先跳过：纯页面样式、纯文档整理、仅构建或部署配置调整
- 优先查看：服务端规则、数据访问 / 异常处理规范、测试规则

### 常见任务入口
- 改接口响应或状态码：先看接口 / 路由规则与异常处理规范
- 改业务流程或状态流转：先看 Service / 应用层规则与权限约束
- 改查询、分页、缓存、迁移：先看数据访问规则与验证要求
- 补服务端回归：先看 `提交前最小回归` 与测试规则

---

## 技术栈

### 后端
- .NET 10（2025 年已发布正式版）+ ASP.NET Core Web API
- 分层架构（9 个项目），按职责严格划分

### 核心库
- `Azrng.Core`：基础类库（实体基类、扩展方法、工具类、结果包装、异常体系）
- `Azrng.Core.Json`：JSON 序列化（基于 `System.Text.Json`）
- `Azrng.AspNetCore.Core`：API 基础设施（统一响应、模型校验、异常中间件）
- `Azrng.Swashbuckle`：Swagger 扩展（简化配置、自动 XML 注释、JWT 认证支持）
- `Azrng.SqlMigration`：SQL 脚本迁移执行
- `Common.Cache.Redis`：Redis 缓存封装（基于 StackExchange.Redis）
- `Common.Cache.MemoryCache`：内存缓存封装（基于 `IMemoryCacheProvider`）
- `Common.HttpClients`：HTTP 客户端封装（基于 `IHttpClientFactory`，支持依赖注入和弹性策略）

### 数据库访问库
- `Common.EFCore`：EF Core 基础封装（必须安装，所有 Provider 都依赖它）
- 根据项目使用的数据库，按需选择以下其中一个 Provider 包：
  - `Common.EFCore.PostgreSql`：PostgreSQL（默认推荐）
  - `Common.EFCore.MySQL`：MySQL / MariaDB
  - `Common.EFCore.SQLServer`：SQL Server
  - `Common.EFCore.SQLite`：SQLite
  - `Common.EFCore.InMemory`：内存数据库（仅用于测试）

### 依赖注入规范
- 使用 Azrng 标记接口方式：
  - `ITransientDependency`：临时注入
  - `IScopedDependency`：作用域注入
  - `ISingletonDependency`：单例注入
- 在 `Program.cs` 中调用 `services.RegisterBusinessServices(assemblies)` 扫描并注册服务
- 服务类实现对应标记接口，自动被扫描注册到 DI 容器

### 数据库
- 默认使用 PostgreSQL
- 可根据项目需求切换为其他数据库（MySQL、SQL Server、SQLite 等）
- ORM：Entity Framework Core，通过 `Common.EFCore` 系列包访问
- 测试环境可使用 `Common.EFCore.InMemory` 内存数据库

---

## 主动建议规则
- 发现 Controller 承载业务逻辑、Application 与 Domain 职责混杂、依赖方向被破坏、权限校验缺失或异常处理不一致时，应主动提醒
- 发现接口契约、DTO、数据库结构、缓存策略或状态流转可能影响历史数据、兼容性或权限安全时，必须先说明风险，不得直接扩大修改
- 发现可以复用既有 Application、Domain、Repository、DTO、异常体系、缓存封装或第三方集成封装时，应优先建议复用
- 不确定业务规则、权限规则、数据含义或外部接口行为时，应按根 `AGENTS.md` 的查证优先级处理，禁止按通用经验补写规则

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
| 数据访问 | EntityFramework | DbContext、数据库迁移、EF Core 配置 | Core, IDomain | 禁止依赖 Domain / Application |
| 领域实现 | Domain | 核心业务逻辑，编排 Adapter 和数据访问 | IDomain, Adapter, EntityFramework | 禁止依赖 IApplication / Application |
| 应用接口 | IApplication | 应用服务接口定义、CQRS 请求 / 响应 | Core, IDomain | 禁止依赖 Domain / Application |
| 应用实现 | Application | 应用服务实现、业务流程编排 | IApplication, Domain | 禁止依赖 Service |
| 存储 | Storage | 文件上传下载、对象存储操作 | Core | 禁止依赖 Application / Service |
| 宿主层 | Service | Controller、中间件、DI 注册、配置 | Application, Storage | 禁止包含业务逻辑 |

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
- 后续实现发现契约缺口时，按根 `AGENTS.md` 的纠错与回退机制回到 DTO / 接口定义变更流程，不在实现层绕过契约。

---

## 后端规则

### Service（宿主层）规则
- Controller 只负责 HTTP 处理，不包含业务逻辑。
- 使用 `[ApiController]` 特性，自动处理 400 响应。
- 使用 `[Route("api/[controller]")]` 统一路由前缀。
- 通过构造函数注入 IApplication 层的服务接口。
- 统一使用 `ActionResult<T>` 或 `IActionResult` 返回类型。
- 所有响应统一使用 `ResultModel<T>` 包装。
- DI 注册、中间件配置、Swagger 配置等都在此层完成。

### IApplication（应用接口层）规则
- 按业务域拆分接口文件夹（如 `UserManager/`、`OrderManager/`）。
- 接口方法定义清晰的入参和返回类型。
- 使用 DTO 作为数据契约，不暴露领域实体。
- 接口方法必须是异步的（返回 `Task<T>`）。

### Application（应用实现层）规则
- 实现类实现对应标记接口以自动注册 DI。
- 编排 Domain 层服务完成业务流程。
- 负责事务管理和跨领域协调。
- Handler 模式处理请求（可结合 MediatR）。
- DTO 与实体之间的转换在此层完成。

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
- DbContext 通过构造函数注入。
- 使用 LINQ 查询，避免原生 SQL。
- 涉及事务时使用 `IDbContextTransaction`。
- 软删除通过全局查询过滤器实现。
- EF Core 实体配置在此层定义。
- 数据库迁移脚本放在 `Migrations/` 目录。

### Adapter（适配器层）规则
- 封装外部 HTTP 服务调用（通过 `Common.HttpClients`）。
- 封装第三方 SDK 集成。
- 提供统一的错误处理和重试机制。
- 外部服务响应与内部模型转换在此层完成。

### Core（基础层）规则
- 提供全局共享的 DTO、枚举、常量。
- 提供通用工具类和扩展方法。
- 提供安全相关的配置和工具。
- 禁止依赖任何业务层项目。

### 数据模型规则
- 实体类继承自 Azrng 基类（包含 Id、创建时间等公共属性）。
- DTO 按用途区分：查询参数、创建请求、更新请求、响应结果。
- 接口层（IApplication / IDomain）只定义 DTO 和接口，不包含实现。
- 禁止在 DTO 中暴露数据库内部字段。

### 统一响应格式
- 所有 API 响应统一使用 `ResultModel<T>` 包装。
- 成功响应：`ResultModel<T>.Success(data)`。
- 错误响应：`ResultModel<T>.Failure(message, errorCode)`。
- 状态码：200 表示成功，其他表示各类错误。
- 异常处理由 `Azrng.AspNetCore.Core` 中间件统一捕获。

### 依赖注入规范
- 服务类必须实现以下接口之一：
  - `ITransientDependency`：每次请求创建新实例
  - `IScopedDependency`：每个请求作用域内单例
  - `ISingletonDependency`：应用程序生命周期单例
- Controller 通过构造函数注入服务。
- 禁止使用静态服务定位器 pattern。
- 禁止手动在 `Program.cs` 中逐一注册服务，应使用 `RegisterBusinessServices` 批量扫描注册。

### 异常处理规范
- 业务异常继承 Azrng 异常体系：
  - `LogicBusinessException`：业务逻辑异常
  - `ParameterException`：参数校验异常
  - `NotFoundException`：资源不存在
  - `ForbiddenException`：禁止访问
  - `InternalServerException`：服务器内部错误
- 异常由中间件统一捕获并转换为 `ResultModel` 响应。
- 禁止在 Controller 外部直接返回错误状态码。

### Redis 缓存使用规范
- 使用 `Common.Cache.Redis` 封装的 `IRedisProvider` 接口。
- 通过 `services.AddRedisCacheStore()` 注册 Redis 服务。
- 缓存操作必须设置合理的过期时间。
- 热点数据使用本地缓存 + Redis 二级缓存。
- 发布订阅场景使用 `PublishAsync` / `SubscribeAsync`。
- 禁止在循环中频繁调用 Redis。

### 内存缓存使用规范
- 使用 `Common.Cache.MemoryCache` 封装的 `IMemoryCacheProvider` 接口。
- 通过 `services.AddMemoryCacheStore()` 注册内存缓存服务。
- 适用于单实例部署、不需要分布式共享的缓存场景。
- 可通过 `MemoryConfig` 配置默认过期时间（默认 5 秒）等参数。
- 与 Redis 搭配使用时作为一级缓存，Redis 作为二级缓存。

### Swagger 规范
- 使用 `Azrng.Swashbuckle` 包，禁止直接引用 `Swashbuckle.AspNetCore`。
- 服务注册：`services.AddDefaultSwaggerGen(title: "项目名称", showJwtToken: true)`。
- 中间件启用：`app.UseDefaultSwagger(onlyDevelopmentEnabled: true)`。
- 项目必须启用 XML 文档生成：`<GenerateDocumentationFile>true</GenerateDocumentationFile>`。
- Controller 和公开方法必须添加 XML 注释，Swagger 会自动展示；XML 注释内容统一使用中文。
- 接口需通过 Swagger UI 独立验证，确保请求 / 响应结构正确。

### 数据库迁移规则
- 使用 `Azrng.SqlMigration` 进行数据库脚本迁移。
- 迁移脚本放在 `EntityFramework/Migrations/` 目录，按版本号命名。
- 迁移脚本必须幂等，可重复执行。
- 应用启动时自动执行迁移（可选配置）。
- 回滚脚本必须与正向脚本配套提供。
- 每次迁移后更新数据库版本记录表。

### 字段命名约定
- C# 属性：PascalCase
- JSON 输出：camelCase（通过 `System.Text.Json` 配置自动转换）
- 数据库字段：snake_case（通过 EF Core 命名约定配置）

---

## 测试规则

### 提交前最小回归
- 默认执行：项目现有的静态检查、类型检查或编译校验
- 业务逻辑改动：至少补一项业务规则、权限判断或状态流转验证
- 接口 / 路由 / 控制器改动：至少补一项输入输出、状态码或异常分支验证
- 数据访问、缓存、迁移改动：至少补一项查询结果、缓存行为、事务或迁移同步验证
- 若仓库暂时缺少自动化测试基础，可使用接口调用、页面联调、脚本或命令行验证替代，但必须能证明核心逻辑真实生效

### 总体要求
- 影响行为的改动应优先补充或更新测试。
- 若本次改动未补测试，必须在最终说明中写明原因和风险。
- 测试应覆盖真实业务行为。

### 后端测试
- 测试框架：xUnit + Moq
- Controller 测试：验证状态码、响应结构、参数校验
- Application 测试：验证业务流程编排逻辑
- Domain 测试：验证核心业务规则，使用 mock 隔离外部依赖
- 数据访问测试：使用 In-Memory 数据库或 Testcontainers
- 集成测试：使用 `WebApplicationFactory` 测试完整请求管道

### 外部依赖与数据
- 测试中不要真实调用第三方服务，统一使用 mock。
- 测试数据应尽量最小化、可读、可重复执行。
- 不要让测试依赖本地人工状态。

### 无法执行测试时
- 必须说明未执行的测试类型。
- 必须说明未执行原因。
- 必须说明潜在影响范围和风险。

---
