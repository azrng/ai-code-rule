---
rule_id: backend-agents
version: 1.31.0
last_updated: 2026-09-02
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
- .NET + ASP.NET Core（Minimal API 与 Controller 按场景选用，详见「接口 / 路由规则」）；版本以仓库现有 `TargetFramework` 为准，不主动升级，新项目先联网检索当前最新正式版
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

<!-- include: shared/backend/di-rules.md -->

---

## 主动建议规则
- 发现业务逻辑放错层、端点与 Service 职责混杂、权限校验缺失、事务边界不清或异常处理不一致时，应主动提醒
- 发现接口契约、DTO、存储结构、缓存策略或状态流转可能影响历史数据、兼容性或权限安全时，必须先说明风险，不得直接扩大修改
- 发现模型调用绕过 `IChatClient`、Agent 越界直接操作 Repository、或 Prompt 散落业务代码时，应提醒并指向 `ai-AGENTS.md`
- 发现可以复用既有服务、数据访问封装、DTO、校验器或错误处理封装时，应优先建议复用
- 不确定业务规则、权限规则、数据含义或外部接口行为时，应按根 `AGENTS.md` 的查证优先级处理，禁止按通用经验补写规则

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

> 物理单层、逻辑整洁：文件夹依赖方向严格遵守「Domain ← Application ← Infrastructure / AI，Endpoints 为入口」。将来需要拆工程时，把文件夹升级为独立项目，代码几乎不动。

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

<!-- include: shared/backend/domain-layer.md -->

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

<!-- include: shared/application/kiss.md -->

<!-- include: shared/backend/data-access-rules.md -->

### 数据库切换规范（SQLite ↔ PostgreSQL）
- 默认 Provider：SQLite（`UseSqlite`），连接串指向本地文件（如 `Data Source=app.db`）
- 升级 Provider：PostgreSQL（`UseNpgsql`），通过配置切换；切换只改 Provider 与连接串，不改业务层
- 不同 Provider 的差异（如大小写敏感、JSON、数组类型、并发）需在迁移与查询中注意，必要时在 `doc/design/设计文档.md` 记录
- 迁移脚本必须保证在两种 Provider 下都可执行或分别提供，避免只跑通一边

<!-- include: shared/backend/response-format.md -->
- 流式接口（如 Agent 输出）可不套 `ResultModel<T>`，按 `ai-AGENTS.md` 与 `doc-AGENTS.md` 约定的事件结构返回

### 依赖注入规范
- 服务类必须实现以下接口之一：
  - `ITransientDependency`：每次请求创建新实例
  - `IScopedDependency`：每个请求作用域内单例
  - `ISingletonDependency`：应用程序生命周期单例
- 端点 / Controller 通过构造函数注入服务
- `IChatClient` 按 `ai-AGENTS.md` 注册，业务层注入抽象
- 禁止使用静态服务定位器 pattern

<!-- include: shared/backend/exception-handling.md -->
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
- 默认不写 `<remarks>`：实现类、实现方法上的 `<remarks>` 多为实现细节，按“不写实现细节”原则省略。
  - 例外：端点需要向调用方暴露事件格式、鉴权要求、流式约定、分页约定等对外契约信息时，可保留精简后的 `<remarks>`，但不写内部实现说明。
- 优先补行内注释的位置：涉及权限、状态流转、事务、缓存、幂等、模型调用风险或降级的复杂业务分支，说明“为什么这样做”——包括 fail fast 的原因、为什么跳过某类脏数据、为什么保留某一层结构。
- 不要补的注释：普通属性的 get / set、简单 `if` / `return`、“返回结果”这类复述代码的低价值注释。
- 注释统一使用中文，不在注释里泄露密钥、token、连接串、模型密钥或真实生产地址。

<!-- include: shared/application/anti-patterns.md -->

<!-- include: shared/application/maintainability-check.md -->

---

## 测试规则

### 提交前最小回归
- 默认执行：项目现有的静态检查、类型检查或编译校验
- 业务逻辑改动：至少补一项业务规则、权限判断或状态流转验证
- 接口 / 路由 / 端点改动：至少补一项输入输出、状态码或异常分支验证
- 数据访问、缓存、迁移改动：至少补一项查询结果、缓存行为、事务或迁移同步验证
- AI / Agent 改动：按 `ai-AGENTS.md` 用 mock `IChatClient` 验证链路
- 若仓库暂时缺少自动化测试基础，可使用接口调用、页面联调、脚本或命令行验证替代，但必须能证明核心逻辑真实生效

<!-- include: shared/common/general-requirements.md -->

### 后端测试
- 单元测试使用 xUnit + Moq
- 端点 / Controller 测试：验证状态码、响应结构、参数校验
- Service 测试：验证业务逻辑分支，使用 mock 隔离依赖
- Repository 测试：使用 In-Memory 数据库或 Testcontainers（注意 SQLite In-Memory 与 PostgreSQL 行为差异）
- 集成测试：使用 WebApplicationFactory 测试完整请求管道
- AI 测试：mock `IChatClient`，不真实调用模型服务

<!-- include: shared/external-dependencies.md -->

<!-- include: shared/no-test-fallback.md -->

---
