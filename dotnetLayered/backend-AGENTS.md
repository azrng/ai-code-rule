---
rule_id: backend-agents
version: 1.11.0
last_updated: 2026-08-13
dependencies: [agents-root]
---

# 后端规则

## 适用范围

- 作用域：服务端实现、接口、服务、数据访问、缓存、异常处理与后端测试
- 触发场景：涉及接口、Service、Repository、缓存、权限、异常处理或测试时阅读

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
  - `Common.EFCore.PostgresSql`：PostgreSQL（默认推荐；注意实际 NuGet 包 id 拼写为 `PostgresSql`）
  - `Common.EFCore.MySQL`：MySQL / MariaDB
  - `Common.EFCore.SQLServer`：SQL Server
  - `Common.EFCore.SQLite`：SQLite
  - `Common.EFCore.InMemory`：内存数据库（仅用于测试）

### 数据库
- 默认使用 PostgreSQL
- 可根据项目需求切换为其他数据库（MySQL、SQL Server、SQLite 等）
- ORM：Entity Framework Core，通过 `Common.EFCore` 系列包访问
- 测试环境可使用 `Common.EFCore.InMemory` 内存数据库

---

## 主动建议规则
- 发现 Controller 承载业务逻辑、Application 与 Domain 职责混杂、依赖方向被破坏、权限校验缺失或异常处理不一致时，应主动提醒
- 发现接口契约、DTO、数据库结构、缓存策略或状态流转可能影响历史数据、兼容性或权限安全时，必须先说明风险，不得直接扩大修改
- 复用、查证优先级等共性规则见根 `AGENTS.md`「模型行为约束与主动建议」

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
| 数据库迁移脚本 | `Service/MigrationSql/` | `版本号.sql` / `版本号.txt`，命名细则见 `数据库迁移规则` |
| 外部服务调用 / 第三方封装 | `Adapter/{业务域}/` | `{名称}Adapter.cs` |
| 通用 DTO / 枚举 / 常量 / 工具 | `Core/Models/`、`Core/Enums/` 等 | `{名称}.cs` |
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
- 后续实现发现契约缺口时，按根 `AGENTS.md` 的纠错与回退机制回到 DTO / 接口定义变更流程，不在实现层绕过契约。

---

## 后端规则

### Service（宿主层）规则
- Controller 只负责 HTTP 处理，不包含业务逻辑。
- 使用 `[ApiController]` 特性，自动处理 400 响应。
- 使用 `[Route("api/[controller]")]` 统一路由前缀。
- 通过构造函数注入 IApplication 层的服务接口。
- Controller action 直接返回 `Task<T>` 并一行转发 AppService；不写 `Ok()`，不手动构造 `ResultModel`，统一包装由全局过滤器与异常中间件完成（见 `统一响应格式`）。
- DI 注册、中间件配置、Swagger 配置等都在此层完成。
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
- 数据库迁移脚本位置与规则见 `数据库迁移规则`。

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

### 代码组织规范
- 一个文件只放一个主类型：实体、DTO、Request、Response、领域服务接口、仓储接口等公开契约类，默认一个类一个文件，文件名与类名一致。
- DTO 目录组织：应用层 DTO 放 `IApplication/{业务域}/Dto/`，Request 与 Response 各自独立文件；通用 DTO 放 Core 层对应目录，不合并到 `Dtos.cs`。
- 触发拆分的信号：职责混杂、同文件出现多个主类型、参数或字段持续堆叠、方法跨多个不相关业务时，应主动拆分。
- 允许例外：`private` / `internal` 且只服务当前文件的小辅助类型、与主类型强绑定的局部 mapping extension、测试文件中只服务当前测试类的小型 fixture。
- 反模式：`Dtos.cs` 长期堆放十几个 Request / Response；把 Controller 专用 request 内联写在 Controller 文件里；跨层把多个不相关类型塞进同一文件。

### 核心逻辑可读性（KISS）
- 以下场景优先显式直写，不要本能压成一条链：聚合、分组排序后取 Top N、多层字典构造、有失败边界的控制流、需要稳定输出顺序的逻辑。
- 优先形式：`foreach`、`if / else`、中间变量、明确命名的临时集合、显式排序 / 裁剪 / 显式异常。
- 谨慎使用：多层嵌套 LINQ、一次性 `GroupBy + Select + ToDictionary + ToList` 链、只为"看起来干净"而拆的 helper。
- helper / 拆分只在降低认知负担时才成立：让职责边界更清楚、真正复用、让调用方少理解细节；若拆完读者要在多个位置来回跳，或用 helper 隐藏本该直说的业务判断，就退回直白写法。
- 单调用点判定：私有方法、局部映射 / 包装方法若仅有一个调用点，且不承载复杂业务规则、边界隔离、异常处理或类型收窄，也无复用预期，必须直接内联回调用处；不得为形式整洁抽取只被调用一次的方法。
- 提交前自检：维护者能否一遍看懂核心逻辑？是否要跳转多个 helper 才知道发生了什么？这是不是当前需求的最简单可行实现？犹豫写"抽象版"还是"直白版"时，默认选直白版。
- 共性的"先读再写""范围最小扰动"见根 `AGENTS.md`「理解检查清单」与「全局工作规则」

### 数据模型规则
- 实体类继承自 Azrng 基类（如 `IdentityOperatorStatusEntity`，自带雪花 Id、审计与软删除字段）。
- DTO 按用途区分（查询参数、创建请求、更新请求、响应结果），文件组织见 `代码组织规范`。
- 接口层（IApplication / IDomain）只定义 DTO 和接口，不包含实现。
- 禁止在 DTO 中暴露数据库内部字段。

### 统一响应格式
- 所有 API 响应统一为 `ResultModel<T>` 结构，由框架三件套自动完成，业务代码不手动包装：
  - `services.AddMvcResultPackFilter("/swagger")`：成功路径自动把 action 返回值包成 `ResultModel<T>`
  - `services.AddMvcModelVerifyFilter()`：模型校验失败统一转为失败 `ResultModel`
  - `app.UseGlobalException()`：业务异常与未处理异常统一映射为失败 `ResultModel`，必须放在中间件管道最外层
- AppService 成功直接返回业务对象（`Task<T>`），失败抛出 Azrng 业务异常；新代码禁止手写 `ResultModel<T>.Success / Error` 手动包装（该写法仅存量代码迁移期可容忍）。
- HTTP 状态码默认按异常类型返回真实状态码（400 / 401 / 404 / 500）；`ResultModel` 的 `code` 字段承载业务码。

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
- 业务失败一律通过抛出上述异常表达；禁止在业务层手工构造失败 `ResultModel` 或直接设置错误状态码。

### Redis 缓存使用规范
- 使用 `Common.Cache.Redis` 封装的 `IRedisProvider` 接口。
- 通过 `services.AddRedisCacheStore()` 注册 Redis 服务。
- 缓存操作必须设置合理的过期时间。
- 热点数据使用本地缓存 + Redis 二级缓存。
- 发布订阅场景使用 `PublishAsync` / `SubscribeAsync`。
- 禁止在循环中频繁调用 Redis。

### 内存缓存使用规范
- 使用 `Common.Cache.MemoryCache` 封装的 `IMemoryCacheProvider` 接口。
- 通过 `services.AddMemoryCacheStore()` 注册内存缓存服务，配置项使用 `MemoryCacheOptions`。
- `MemoryCacheOptions.DefaultExpiry` 默认仅 5 秒，业务缓存必须显式传入过期时间，不依赖默认值。
- 适用于单实例部署、不需要分布式共享的缓存场景。
- 与 Redis 搭配使用时作为一级缓存，Redis 作为二级缓存。

### Swagger 规范
- 使用 `Azrng.Swashbuckle` 包，禁止直接引用 `Swashbuckle.AspNetCore`。
- 服务注册：`services.AddDefaultSwaggerGen(title: "项目名称", showJwtToken: true)`。
- 中间件启用：`app.UseDefaultSwagger(onlyDevelopmentEnabled: true)`。
- 项目必须启用 XML 文档生成：`<GenerateDocumentationFile>true</GenerateDocumentationFile>`。
- Controller 和公开方法必须添加 XML 注释，Swagger 会自动展示；XML 注释内容统一使用中文。
- AppService / DomainService 的方法 XML 注释按 `代码注释规范` 处理（不依赖 Swagger 自动展示，但需保证可读与可维护）。
- 接口需通过 Swagger UI 独立验证，确保请求 / 响应结构正确。

### 数据库迁移规则
- 使用 `Azrng.SqlMigration` 进行数据库脚本迁移，不使用 EF Core Code-First Migrations。
- 迁移脚本放在 `Service/MigrationSql/` 目录，文件名按 `版本号.sql` 或 `版本号.txt` 命名（如 `1.0.0.sql`、`1.1.0.txt`），不含描述文字——去掉版本前缀与扩展名后的剩余部分会被整体解析为版本号；按版本号顺序执行。
- 通过 `AddSqlMigrationService(...).AddAutoMigration()` 在应用启动时自动执行未应用的脚本；已执行版本由包自动记录在版本日志表，不重复执行，无需手工维护版本表。
- 已应用的脚本不可修改；结构调整必须新增更高版本号的脚本。
- 涉及数据的高风险结构变更，建议同步准备回滚脚本并在文档中说明恢复方式。

### 字段命名约定
- C# 属性：PascalCase
- JSON 输出：camelCase（通过 `System.Text.Json` 配置自动转换）
- 数据库字段：snake_case（通过 EF Core 命名约定配置）

### 代码注释规范
- 必须补 XML 注释的位置：
  - Controller action：用 `<summary>` 说明接口用途、关键参数、返回结果（Swagger 会消费这些说明）。
  - IApplication / IDomain 接口方法：说明应用能力或领域能力，不写实现细节。
  - Domain 核心业务规则方法：说明规则意图。
  - 不来自接口的实现类自有方法（接口未约束的核心方法）：补 `<summary>` 说明该能力做什么。
- 接口已注释时，实现方法不重复注释，也不加 `/// <inheritdoc />`；只有实现类自有的、接口未定义的方法才需要补注释。
- 默认不写 `<remarks>`：实现类、实现方法上的 `<remarks>` 多为实现细节，按"不写实现细节"原则省略。
  - 例外：Controller action 需要向调用方暴露事件格式、鉴权要求、分页约定等对外契约信息时，可保留精简后的 `<remarks>`，但不写内部实现说明。
- 优先补行内注释的位置：涉及权限、状态流转、事务、缓存、幂等或降级的复杂业务分支，说明"为什么这样做"——包括 fail fast 的原因、为什么跳过某类脏数据、为什么保留某一层结构（如月份层、品种层）。
- 不要补的注释：普通属性的 get / set、简单 `if` / `return`、"返回结果"这类复述代码的低价值注释。
- 注释统一使用中文，不在注释里泄露密钥、token、连接串或真实生产地址。

### 反模式与修根因清单
- 这是根 `AGENTS.md`「修 Bug 必须定位根因」在 .NET 中的具体禁止项，禁止用下列方式掩盖问题：
  - 空 `catch` 或 `catch (Exception)` 吞异常后静默继续
  - 用 `!`（null-forgiving）压制可空告警而非真正处理 null
  - 在异步上下文用 `.Result` / `.Wait()` 同步阻塞
  - `#pragma warning disable` 关闭编译 / 分析器告警掩盖问题
  - 删除或注释测试让构建通过
  - 用一层抽象重写掩盖原始复杂度，而不是简化它
- 出现这些倾向时立刻回退一步：复杂 LINQ 比业务逻辑本身更难读；helper 数量增加但理解成本没下降；核心逻辑缺注释只能靠猜。

---

## 测试规则

### 提交前最小回归
- 默认执行：项目现有的静态检查、类型检查或编译校验
- 业务逻辑改动：至少补一项业务规则、权限判断或状态流转验证
- 接口 / 路由 / 控制器改动：至少补一项输入输出、状态码或异常分支验证
- 数据访问、缓存、迁移改动：至少补一项查询结果、缓存行为、事务或迁移同步验证
- 若仓库暂时缺少自动化测试基础，可使用接口调用、脚本或命令行验证替代，但必须能证明核心逻辑真实生效

### 总体要求
- 影响行为的改动应优先补充或更新测试。
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

---
