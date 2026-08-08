---
rule_id: backend-agents
version: 1.10.0
last_updated: 2026-08-08
dependencies: [agents-root, ai-agents]
---

# 后端规则

> 本规则采用 **单项目整洁架构**（单 .csproj，按依赖方向分层）；数据库 **SQLite 默认、PostgreSQL 可选**；Redis 为 **可选**；含 AI 集成边界（详见 `ai-AGENTS.md`）。

## 适用范围

- 作用域：单项目整洁架构、服务端实现、接口、服务、数据访问、异常处理与后端测试
- 触发场景：涉及接口、Service、Repository、异常处理、AI 集成边界或测试时阅读

### 常见任务入口
- 改接口响应或状态码：先看接口 / 路由规则与异常处理规范
- 改业务流程或状态流转：先看 Application 规则与权限约束
- 改查询、分页、迁移、切换数据库：先看数据访问规则与数据库切换规范
- 涉及模型调用或 Agent：先读 `ai-AGENTS.md`，再回到本文件确认分层边界
- 补服务端回归：先看 `提交前最小回归` 与测试规则

---

## 技术栈

### 后端
- .NET 10 + ASP.NET Core（Minimal API 与 Controller 按场景选用，详见"接口 / 路由规则"）
- **单项目整洁架构**：一个 `.csproj`，项目内用文件夹遵循整洁架构依赖方向
- ORM：Entity Framework Core
- 测试框架：xUnit + Moq

### 数据库与缓存
- 数据库：**SQLite 默认**（本地零依赖、结构易改）；PostgreSQL 可选（生产或并发场景升级）
- 数据库切换通过 EF Core Provider 与连接串配置完成，业务层不动
- 缓存：**默认不引入**；确有热点或会话缓存需求时再用 Redis（可选）

### 核心库
- `Azrng.Core`：基础类库（实体基类、扩展方法、工具类、结果包装、异常体系）
- `Azrng.Core.Json`：JSON 序列化（基于 System.Text.Json）
- `Azrng.AspNetCore.Core`：API 基础设施（统一响应、模型校验、异常中间件）
- `Azrng.SqlMigration`：SQL 脚本迁移执行
- `Common.Cache.Redis`（可选）：Redis 缓存封装（基于 StackExchange.Redis），仅在引入缓存时使用

### AI 集成
- `Microsoft.Extensions.AI` + Microsoft Agent Framework，规则全部以 `ai-AGENTS.md` 为准

### 依赖注入规范
- 使用 Azrng 标记接口方式：
  - `ITransientDependency`：临时注入
  - `IScopedDependency`：作用域注入
  - `ISingletonDependency`：单例注入
- 在 Program.cs 中调用 `services.RegisterBusinessServices(assemblies)` 扫描并注册服务
- 服务类实现对应标记接口，自动被扫描注册到 DI 容器

如果仓库已经有真实实现，以现有代码为准，不要强行重构或替换技术栈。
技术债务与重构判断遵循根 `AGENTS.md` 的全局规则。

---

## 主动建议规则
- 发现业务逻辑放错层、端点与 Service 职责混杂、权限校验缺失、事务边界不清或异常处理不一致时，应主动提醒
- 发现接口契约、DTO、存储结构、缓存策略或状态流转可能影响历史数据、兼容性或权限安全时，必须先说明风险，不得直接扩大修改
- 发现模型调用绕过 `IChatClient`、Agent 越界直接操作 Repository、或 Prompt 散落业务代码时，应提醒并指向 `ai-AGENTS.md`
- 复用、查证优先级等共性规则见根 `AGENTS.md`「模型行为约束与主动建议」

## 推荐目录结构（单项目整洁架构）

- 后端聚焦在 `backend/` 下，单个启动项目即可。

```text
backend/
├── src/
│   └── YourProject/                     # 单个项目（一个 .csproj）
│       ├── Domain/                      # 领域层：零外部依赖
│       │   ├── Entities/                # 领域实体
│       │   ├── ValueObjects/            # 值对象
│       │   ├── Enums/                   # 枚举
│       │   ├── Events/                  # 领域事件
│       │   └── Exceptions/              # 领域异常
│       ├── Application/                 # 应用层：依赖 Domain
│       │   ├── Services/                # 应用服务接口与实现
│       │   ├── Interfaces/              # 应用服务接口
│       │   ├── DTOs/                    # 数据传输对象（按业务域拆分）
│       │   └── Mappings/                # 对象映射配置
│       ├── Infrastructure/              # 基础设施层：依赖 Application / Domain
│       │   ├── Data/
│       │   │   ├── Context/             # DbContext
│       │   │   ├── Configurations/      # EF Core 实体配置
│       │   │   ├── Migrations/          # EF Core 迁移文件（SQLite 默认场景）
│       │   │   └── Repositories/        # 仓储实现
│       │   └── Services/                # 外部服务实现（邮件、文件存储等；模型客户端在 AI/Clients/）
│       ├── AI/                          # AI 能力（见 ai-AGENTS.md）
│       │   ├── Clients/                 # IChatClient 注册与封装
│       │   ├── Agents/                  # Agent 定义与编排
│       │   ├── Tools/                   # Function Calling 工具
│       │   ├── Skills/                  # Skills 集合
│       │   ├── Prompts/                 # Prompt 模板
│       │   └── Options/                 # AIOptions 等配置
│       ├── Endpoints/                   # 接口入口（Minimal API 端点 或 Controllers）
│       │   └── ...                      # 按业务域组织端点映射或 Controller
│       ├── Program.cs                   # 应用入口（DI、中间件、IChatClient 注册）
│       ├── appsettings.json             # 配置文件
│       └── YourProject.csproj
├── tests/
│   ├── YourProject.UnitTests/
│   └── YourProject.IntegrationTests/
├── Dockerfile
└── backend.sln
```

> 物理单层、逻辑整洁：文件夹依赖方向严格遵守"Domain ← Application ← Infrastructure / AI，Endpoints 为入口"。将来需要拆工程时，把文件夹升级为独立项目，代码几乎不动。

---

## 后端规则（单项目整洁架构）

### 分层依赖规则
```
Endpoints → Application → Domain ← Infrastructure / AI
                ↑____________________|
```
- `Domain`：无外部依赖，包含核心业务模型
- `Application`：依赖 Domain，定义服务接口和 DTO；Agent 编排放此层
- `Endpoints`：依赖 Application，处理 HTTP 请求（Minimal API 或 Controller）
- `Infrastructure`：依赖 Application 和 Domain，实现数据访问、外部服务（邮件、文件存储等）
- `AI`：平级 feature 文件夹（方案 A），集中放所有 AI 代码；其中 `Clients/` 逻辑偏基础设施、`Agents/` 逻辑偏应用层、`Tools/` 是对 Domain / Repository 的薄封装

### 接口 / 路由规则
- 单一职责的轻量端点（如 Agent 调用入口、健康检查、简单 CRUD）优先用 **Minimal API**
- 需要模型校验过滤器、复杂约定或团队更熟悉时，用 **Controller**（`[ApiController]` + `[Route("api/[controller]")]`）
- 两种风格可在同一项目并存，但同一业务域内保持一致，避免混用造成阅读负担
- Controller 只负责 HTTP 处理，不包含业务逻辑；注入 Application 层服务接口，不直接依赖 Infrastructure

### Application 层规则
- 服务类实现接口，接口定义在 `Interfaces/` 目录
- 服务方法必须是异步的（返回 `Task<T>`）
- 使用 DTO 进行数据传递，不直接暴露 Domain 实体
- 使用 AutoMapper 进行对象映射（如需要）
- 禁止在 Controller、Service、Repository、Entity、DTO 等 C# 类中使用主构造函数（Primary Constructor），统一使用显式构造函数
- 业务异常抛出继承自 `BaseException` 的自定义异常
- Agent 编排与工具调度放此层（物理上在 `AI/Agents/`，见 `ai-AGENTS.md` 方案 A）；模型调用细节委托给 `AI/Clients/` 的客户端封装，不在此层手拼模型请求

### Domain 层规则
- 实体类继承自基类（包含 Id、创建时间等公共属性）
- 值对象使用 record 类型，实现值相等比较
- 禁止依赖外部库，保持纯净

### Infrastructure 层规则
- DbContext 实现数据上下文，配置连接和映射
- 数据库结构变更：**SQLite（默认）优先用 EF Core Migrations；PostgreSQL 优先用 `Azrng.SqlMigration`**（详见「数据库迁移规则」）
- 仓储实现依赖注入，遵循仓储模式
- 外部服务（邮件、文件存储等）实现在此层
- 模型客户端（`IChatClient` 注册、模型选择）放在 `AI/Clients/`（见 `ai-AGENTS.md` 方案 A），逻辑偏基础设施，向 Application 暴露抽象

### 代码组织规范
- 一个文件只放一个主类型：Endpoint 映射、Controller、Service 接口与实现、Entity、值对象、DTO 等，默认一个类一个文件，文件名与类名一致。
- DTO 目录组织：应用层 DTO 放 `Application/DTOs/{业务域}/`，Request 与 Response 各自独立文件；领域事件放 `Domain/Events/`，不合并到 `Dtos.cs`。
- 触发拆分的信号：职责混杂、同文件出现多个主类型、参数或字段持续堆叠、方法跨多个不相关业务时，应主动拆分。
- 允许例外：`private` / `internal` 且只服务当前文件的小辅助类型、与主类型强绑定的局部 mapping extension、测试文件中只服务当前测试类的小型 fixture。
- 反模式：`Dtos.cs` 长期堆放十几个 Request / Response；把端点专用 request 内联写在端点文件里；业务逻辑放到 Infrastructure。

### 核心逻辑可读性（KISS）
- 以下场景优先显式直写，不要本能压成一条链：聚合、分组排序后取 Top N、多层字典构造、有失败边界的控制流、需要稳定输出顺序的逻辑。
- 优先形式：`foreach`、`if / else`、中间变量、明确命名的临时集合、显式排序 / 裁剪 / 显式异常。
- 谨慎使用：多层嵌套 LINQ、一次性 `GroupBy + Select + ToDictionary + ToList` 链、只为"看起来干净"而拆的 helper。
- helper / 拆分只在降低认知负担时才成立：让职责边界更清楚、真正复用、让调用方少理解细节；若拆完读者要在多个位置来回跳，或用 helper 隐藏本该直说的业务判断，就退回直白写法。
- 共性的"先读再写""范围最小扰动"见根 `AGENTS.md`「理解检查清单」与「全局工作规则」

### 数据访问规则
- 使用 Entity Framework Core 进行数据访问
- DbContext 通过构造函数注入
- 使用 LINQ 查询，避免原生 SQL
- 涉及事务时使用 `IDbContextTransaction`
- 软删除通过全局查询过滤器实现

### 数据库切换规范（SQLite ↔ PostgreSQL）
- 默认 Provider：SQLite（`UseSqlite`），连接串指向本地文件（如 `Data Source=app.db`）
- 升级 Provider：PostgreSQL（`UseNpgsql`），通过配置切换；切换只改 Provider 与连接串，不改业务层
- 不同 Provider 的差异（如大小写敏感、JSON、数组类型、并发）需在迁移与查询中注意，必要时在 `doc/design/设计文档.md` 记录
- 迁移脚本必须保证在两种 Provider 下都可执行或分别提供，避免只跑通一边

### 统一响应格式
- 所有 API 响应统一为 `ResultModel<T>` 结构，由框架三件套自动完成，业务代码不手动包装：
  - `services.AddMvcResultPackFilter("/swagger")`：成功路径自动把 action 返回值包成 `ResultModel<T>`
  - `services.AddMvcModelVerifyFilter()`：模型校验失败统一转为失败 `ResultModel`
  - `app.UseGlobalException()`：业务异常与未处理异常统一映射为失败 `ResultModel`，必须放在中间件管道最外层
- Service 成功直接返回业务对象（`Task<T>`），失败抛出 Azrng 业务异常；新代码禁止手写 `ResultModel<T>.Success / Error` 手动包装（该写法仅存量代码迁移期可容忍）。
- HTTP 状态码默认按异常类型返回真实状态码（400 / 401 / 404 / 500）；`ResultModel` 的 `code` 字段承载业务码。
- 流式接口（如 Agent 输出）可不套 `ResultModel<T>`，按 `ai-AGENTS.md` 与 `doc-AGENTS.md` 约定的事件结构返回

### 依赖注入规范
- 服务类必须实现以下接口之一：
  - `ITransientDependency`：每次请求创建新实例
  - `IScopedDependency`：每个请求作用域内单例
  - `ISingletonDependency`：应用程序生命周期单例
- 端点 / Controller 通过构造函数注入服务
- `IChatClient` 按 `ai-AGENTS.md` 注册，业务层注入抽象
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
- 模型调用异常（超时、限流、空响应）需转换为可读的业务异常，不向上层泄漏 SDK 细节

### Redis 缓存使用规范（可选）
- 仅在明确需要缓存时引入 `Common.Cache.Redis`，封装的 `IRedisProvider` 接口
- 通过 `services.AddRedisCacheStore()` 注册 Redis 服务
- 缓存操作必须设置合理的过期时间
- 热点数据使用本地缓存 + Redis 二级缓存
- 发布订阅场景使用 `PublishAsync`/`SubscribeAsync`
- 禁止在循环中频繁调用 Redis

### 数据库迁移规则
- **迁移工具优先级**：**PostgreSQL 优先用 `Azrng.SqlMigration`（SQL 脚本迁移）**；**SQLite（默认）及其他场景优先用 EF Core Migrations**
- 两种工具可在同一项目并存，但同一数据库的同一变更只走一种，避免重复执行或互相覆盖
- `Azrng.SqlMigration` 脚本放在宿主项目下 `MigrationSql/` 目录，文件名按 `版本号.sql` 或 `版本号.txt` 命名（如 `1.0.0.sql`、`1.1.0.txt`），不含描述文字——去掉版本前缀与扩展名后的剩余部分会被整体解析为版本号；按版本号顺序执行；通过 `AddSqlMigrationService(...).AddAutoMigration()` 在应用启动时自动执行未应用的脚本，已执行版本由包自动记录在版本日志表，不重复执行，无需手工维护版本表
- `Azrng.SqlMigration` 已应用的脚本不可修改，结构调整必须新增更高版本号的脚本；涉及数据的高风险结构变更，建议同步准备回滚脚本并在文档中说明恢复方式
- EF Core Migrations 通过 `dotnet ef migrations add` 生成，注意 SQLite 与 PostgreSQL 的 Provider 差异（大小写敏感、JSON、并发等）
- 应用启动时自动执行迁移（可选配置）；默认 SQLite 需注意文件锁与并发限制，生产场景再评估是否切 PostgreSQL

### 代码注释规范
- 必须补 XML 注释的位置：
  - 接口 / 端点 / Controller action：用 `<summary>` 说明用途、关键参数、返回结果。
  - Application 层服务接口方法：说明应用能力，不写实现细节。
  - Domain 核心业务规则方法：说明规则意图。
  - 不来自接口的实现类自有方法（接口未约束的核心方法）：补 `<summary>` 说明该能力做什么。
- 接口已注释时，实现方法不重复注释，也不加 `/// <inheritdoc />`；只有实现类自有的、接口未定义的方法才需要补注释。
- 默认不写 `<remarks>`：实现类、实现方法上的 `<remarks>` 多为实现细节，按"不写实现细节"原则省略。
  - 例外：端点需要向调用方暴露事件格式、鉴权要求、流式约定、分页约定等对外契约信息时，可保留精简后的 `<remarks>`，但不写内部实现说明。
- 优先补行内注释的位置：涉及权限、状态流转、事务、缓存、幂等、模型调用风险或降级的复杂业务分支，说明"为什么这样做"——包括 fail fast 的原因、为什么跳过某类脏数据、为什么保留某一层结构。
- 不要补的注释：普通属性的 get / set、简单 `if` / `return`、"返回结果"这类复述代码的低价值注释。
- 注释统一使用中文，不在注释里泄露密钥、token、连接串、模型密钥或真实生产地址。

### 反模式与修根因清单
- 这是根 `AGENTS.md`「修 Bug 必须定位根因」在 .NET 中的具体禁止项，禁止用下列方式掩盖问题：
  - 空 `catch` 或 `catch (Exception)` 吞异常后静默继续
  - 用 `!`（null-forgiving）压制可空告警而非真正处理 null
  - 在异步上下文用 `.Result` / `.Wait()` 同步阻塞
  - `#pragma warning disable` 关闭编译 / 分析器告警掩盖问题
  - 删除或注释测试让构建通过
  - 用一层抽象重写掩盖原始复杂度，而不是简化它
- 出现这些倾向时立刻回退一步：复杂 LINQ 比业务逻辑本身更难读；helper 数量增加但理解成本没下降；核心逻辑缺注释只能靠猜。

### 可维护性自检（提交前）
提交前对核心逻辑至少问一遍：
- 维护者能否一遍看懂这段核心逻辑？
- 是否必须跳转多个 helper 才知道发生了什么？
- 关键方法和关键逻辑有没有注释？
- 这是不是当前需求的最简单可行实现？
- 我是不是为了"写得像高手"而牺牲了可读性？
- 犹豫写"抽象版"还是"直白版"时，默认选直白版。

---

## 测试规则

### 提交前最小回归
- 默认执行：项目现有的静态检查、类型检查或编译校验
- 业务逻辑改动：至少补一项业务规则、权限判断或状态流转验证
- 接口 / 路由 / 端点改动：至少补一项输入输出、状态码或异常分支验证
- 数据访问、缓存、迁移改动：至少补一项查询结果、缓存行为、事务或迁移同步验证
- AI / Agent 改动：按 `ai-AGENTS.md` 用 mock `IChatClient` 验证链路
- 若仓库暂时缺少自动化测试基础，可使用接口调用、页面联调、脚本或命令行验证替代，但必须能证明核心逻辑真实生效

### 总体要求
- 影响行为的改动应优先补充或更新测试
- 若本次改动未补测试，必须在最终说明中写明原因和风险
- 测试应覆盖真实业务行为

### 后端测试
- 单元测试使用 xUnit + Moq
- 端点 / Controller 测试：验证状态码、响应结构、参数校验
- Service 测试：验证业务逻辑分支，使用 mock 隔离依赖
- Repository 测试：使用 In-Memory 数据库或 Testcontainers（注意 SQLite In-Memory 与 PostgreSQL 行为差异）
- 集成测试：使用 WebApplicationFactory 测试完整请求管道
- AI 测试：mock `IChatClient`，不真实调用模型服务

### 外部依赖与数据
- 测试中不要真实调用第三方服务，统一使用 mock
- 测试数据应尽量最小化、可读、可重复执行
- 不要让测试依赖本地人工状态

### 无法执行测试时
- 必须说明未执行的测试类型
- 必须说明未执行原因
- 必须说明潜在影响范围和风险

---
