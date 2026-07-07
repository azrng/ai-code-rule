---
rule_id: backend-agents
version: 1.5.1
last_updated: 2026-07-07
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
- 单项目结构（不分层），按文件夹组织代码

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

如果仓库已经有真实实现，以现有代码为准，不要强行重构或替换技术栈。

**技术债务评估框架**：
- **沿用现有实现**：代码可正常运行、无明显性能问题、维护成本可接受、团队熟悉度高
- **必须重构**：存在安全漏洞、严重影响性能、阻碍新功能开发、维护成本过高
- **可选重构**：代码风格不一致、存在更好的替代方案、但不紧急
- **禁止重构**：仅为个人偏好、追赶技术潮流、非关键路径的过度优化

**重构决策流程**：
1. 先评估现有代码的技术债务等级（高 / 中 / 低）。
2. 判断重构的紧急程度和业务价值。
3. 评估重构风险和工作量。
4. 高风险或大规模重构需先与团队确认。

## 主动建议规则
- 发现业务逻辑放错层、入口层与服务层职责混杂、权限校验缺失、事务边界不清或异常处理不一致时，应主动提醒
- 发现接口契约、DTO、存储结构、缓存策略或状态流转可能影响历史数据、兼容性或权限安全时，必须先说明风险，不得直接扩大修改
- 发现可以复用既有服务、数据访问封装、DTO、校验器、错误处理封装或第三方集成封装时，应优先建议复用
- 不确定业务规则、权限规则、数据含义或外部接口行为时，应按根 `AGENTS.md` 的查证优先级处理，禁止按通用经验补写规则

---

## 推荐目录结构

若仓库尚未形成稳定结构，可优先参考以下组织方式；若仓库已有实现，以现状为准，不强制迁移。

```text
project-root/
├── src/
│   └── YourProject/
│       ├── Controllers/
│       ├── Services/
│       │   └── Interfaces/
│       ├── Models/
│       │   ├── Entities/
│       │   ├── DTOs/
│       │   └── Enums/
│       ├── Data/
│       │   ├── Context/
│       │   └── Configurations/
│       ├── Middleware/
│       ├── Filters/
│       ├── Extensions/
│       ├── Migrations/
│       ├── Program.cs
│       ├── appsettings.json
│       └── YourProject.csproj
├── tests/
│   └── YourProject.Tests/
└── ...
```

### 目录职责映射

按业务职责将类放到对应目录，文件命名与类名保持一致；与本文件 `代码组织规范`、`Service 规则`、`数据模型规则` 中的目录约定保持统一。

| 目录 | 放什么类 | 命名约定 |
| --- | --- | --- |
| `Controllers/` | API 控制器，仅做 HTTP 编排，不含业务逻辑 | `{资源}Controller.cs` |
| `Services/` | 业务服务实现（应用逻辑、状态流转、权限判断） | `{业务域}Service.cs` |
| `Services/Interfaces/` | 服务接口 | `I{业务域}Service.cs` |
| `Models/Entities/` | EF Core 实体，一个实体一个文件 | `{实体名}.cs` |
| `Models/DTOs/{业务域}/` | 请求 / 响应 / 查询参数 DTO，Request 与 Response 各自独立文件 | `CreateOrderRequest.cs`、`OrderDetailResponse.cs`、`OrderQuery.cs` |
| `Models/Enums/` | 枚举；跨模块复用的单独成文件，仅服务单个 DTO 的可随主类型 | `{枚举名}.cs` |
| `Data/Context/` | DbContext | `{项目名}DbContext.cs` |
| `Data/Configurations/` | EF Core 实体配置（字段映射、索引、关系） | `{实体名}Configuration.cs` |
| `Middleware/` | 中间件 | `{名称}Middleware.cs` |
| `Filters/` | 动作过滤器、自定义特性 | `{名称}Attribute.cs` / `{名称}Filter.cs` |
| `Extensions/` | 扩展方法、DI 注册扩展 | `{类型}Extensions.cs` |
| `Migrations/` | 数据库迁移脚本，必须幂等、可重复执行 | 按版本号命名 |

---

## 阶段 1 — 后端实现

**触发条件**：用户发出「开始开发」或「开始后端开发」指令

**入场要求**：阶段 0 设计文档或接口契约已明确；若为例外情况任务，可直接进入实现

**工作内容**：
1. 严格按照接口契约实现 Controller、Service、DTO 和数据访问逻辑。
2. 保持单项目目录边界清晰，避免把配置、业务、持久化逻辑无序混杂。
3. 所有增删改必须真实生效，并能通过 Swagger 或测试独立验证。
4. 涉及数据库结构变更时，同步补齐迁移脚本和初始化说明。

**门控规则**：
- 核心接口路径已可通过 Swagger 独立验证。
- 影响行为的后端改动已补测试或在交付说明中解释未补原因。

---

## 后端规则

### Controller 规则
- Controller 只负责 HTTP 处理，不包含业务逻辑。
- 使用 `[ApiController]` 特性，自动处理 400 响应。
- 使用 `[Route("api/[controller]")]` 统一路由前缀。
- 通过构造函数注入 Service，禁止在 Controller 中直接操作 DbContext。
- 统一使用 `ActionResult<T>` 或 `IActionResult` 返回类型。
- 所有响应统一使用 `ResultModel<T>` 包装。

### Service 规则
- 服务类实现接口，接口定义在 `Services/Interfaces/` 目录。
- 服务方法必须是异步的（返回 `Task<T>`）。
- 使用 DTO 进行数据传递，不直接暴露实体。
- 业务异常抛出继承自 Azrng 异常体系的自定义异常。
- 服务类实现标记接口（`ITransientDependency` / `IScopedDependency` / `ISingletonDependency`）以自动注册。
- 禁止在 Controller、Service、Repository、Entity、DTO 等 C# 类中使用主构造函数（Primary Constructor），统一使用显式构造函数。

### 代码组织规范
- 一个文件只放一个主类型：Entity、DTO、Request、Response、ViewModel、枚举等公开契约类，默认一个类一个文件，文件名与类名一致。
- DTO 目录组织：请求 / 响应 / 查询参数按业务域归入 `Models/DTOs/{业务域}/`，Request 与 Response 分别独立文件，不合并到 `Dtos.cs` 或 `{模块}Dto.cs`。
  - 命名示例：`{业务域}/CreateOrderRequest.cs`、`{业务域}/OrderDetailResponse.cs`、`{业务域}/OrderQuery.cs`。
- 触发拆分的信号：职责混杂、同文件出现多个主类型、参数或字段持续堆叠、方法跨多个不相关业务时，应主动拆分。
- 允许例外：`private` / `internal` 且只服务当前文件的小辅助类型、与主类型强绑定的局部 mapping extension、测试文件中只服务当前测试类的小型 fixture。
- 反模式：`Dtos.cs` 长期堆放十几个 Request / Response；把 Controller 专用 request 内联写在 Controller 文件里。

### 数据模型规则
- 实体类继承自 Azrng 基类（包含 Id、创建时间等公共属性）。
- DTO 按用途区分（查询参数、创建请求、更新请求、响应结果），文件组织见 `代码组织规范`。
- 枚举放在 `Models/Enums/` 目录。
- 禁止在 DTO 中暴露数据库内部字段。

### 数据访问规则
- 使用 Entity Framework Core 进行数据访问。
- DbContext 通过构造函数注入。
- 使用 LINQ 查询，避免原生 SQL。
- 涉及事务时使用 `IDbContextTransaction`。
- 软删除通过全局查询过滤器实现。
- EF Core 实体配置放在 `Data/Configurations/` 目录。

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

### 数据库迁移规则
- 使用 `Azrng.SqlMigration` 进行数据库脚本迁移。
- 迁移脚本放在 `Migrations/` 目录，按版本号命名。
- 迁移脚本必须幂等，可重复执行。
- 应用启动时自动执行迁移（可选配置）。
- 回滚脚本必须与正向脚本配套提供。
- 每次迁移后更新数据库版本记录表。

### Swagger 规范
- 使用 `Azrng.Swashbuckle` 包，禁止直接引用 `Swashbuckle.AspNetCore`。
- 服务注册：`services.AddDefaultSwaggerGen(title: "项目名称", showJwtToken: true)`。
- 中间件启用：`app.UseDefaultSwagger(onlyDevelopmentEnabled: true)`。
- 项目必须启用 XML 文档生成：`<GenerateDocumentationFile>true</GenerateDocumentationFile>`。
- Controller 和公开方法必须添加 XML 注释，Swagger 会自动展示；XML 注释内容统一使用中文。
- Service / Repository 的核心方法与接口方法按 `代码注释规范` 补 XML 注释（不依赖 Swagger 自动展示，但需保证可读与可维护）。
- 接口需通过 Swagger UI 独立验证，确保请求 / 响应结构正确。

### 字段命名约定
- C# 属性：PascalCase
- JSON 输出：camelCase（通过 `System.Text.Json` 配置自动转换）
- 数据库字段：snake_case（通过 EF Core 命名约定配置）

### 代码注释规范
- 必须补 XML 注释的位置：
  - Controller action：说明接口用途、关键参数、返回结果（Swagger 会消费这些说明）。
  - Service 接口方法与 Service 核心实现方法：说明该能力做什么，不写实现细节。
- 优先补行内注释的位置：涉及权限、状态流转、事务、缓存、幂等或降级的复杂业务分支，说明为什么这样处理。
- 不要补的注释：普通属性的 get / set、简单 `if` / `return`、"返回结果"这类复述代码的低价值注释。
- 注释统一使用中文，不在注释里泄露密钥、token、连接串或真实生产地址。

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
- Service 测试：验证业务逻辑分支，使用 mock 隔离依赖
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

文件结束。
