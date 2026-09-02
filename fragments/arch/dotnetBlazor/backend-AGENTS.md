---
rule_id: backend-agents
version: 1.35.0
last_updated: 2026-09-02
dependencies: [agents-root]
---

# 后端规则

<!-- include: shared/backend/scope.md -->

<!-- include: shared/backend/common-tasks.md -->

---

## 技术栈

### 后端服务层
- ASP.NET Core Web API（如需独立 API 接口）
- Clean Architecture 分层架构：
  - `Web`（Blazor Server 表现层，如需独立 API 再加 `API` 层）→ `Application`（应用层）→ `Domain`（领域层）← `Infrastructure`（基础设施层）
- ORM：Entity Framework Core（可选）
- 测试框架：xUnit + Moq

<!-- include: shared/backend/core-libs.md -->
- Blazor 项目中数据库、`Azrng.SqlMigration` 迁移与 Redis 缓存为可选能力，按项目实际启用

### 依赖注入规范
- 使用 Azrng 标记接口方式：
  - `ITransientDependency`：临时注入
  - `IScopedDependency`：作用域注入
  - `ISingletonDependency`：单例注入
- 在 `Program.cs` 中调用 `services.RegisterBusinessServices(assemblies)` 扫描并注册服务
- Blazor Server 中 Scoped 服务基于 SignalR 连接生命周期

<!-- include: shared/application/tech-choice.md -->

---

## 主动建议规则
- 发现业务逻辑放错层、服务与数据访问职责混杂、权限校验缺失、事务边界不清或异常处理不一致时，应主动提醒
- 发现接口契约、DTO、存储结构或状态流转可能影响历史数据、兼容性或权限安全时，必须先说明风险，不得直接扩大修改
- 发现可以复用既有服务、数据访问封装、DTO、校验器或错误处理封装时，应优先建议复用
- 不确定业务规则、权限规则、数据含义或外部接口行为时，应按根 `AGENTS.md` 的查证优先级处理，禁止按通用经验补写规则

---

## 推荐目录结构

```text
src/
├── YourProject.Web/
├── YourProject.Application/
│   ├── Services/
│   ├── DTOs/
│   └── Interfaces/
├── YourProject.Domain/
│   ├── Entities/
│   ├── ValueObjects/
│   └── Enums/
└── YourProject.Infrastructure/
    ├── Data/
    └── Services/
```

### 层内目录组织

各层内部按职责与类型组织；公开契约类（DTO、实体、接口等）默认一个类一个文件，文件名与类名一致。

| 类型 | 所在层 / 目录 | 命名约定 |
| --- | --- | --- |
| Controller（如提供 API） | `Web/Controllers/` | `{资源}Controller.cs` |
| Razor 组件 / 页面 | `Web/Pages/`、`Web/Components/`（UI 细节见 `ui-AGENTS.md`） | `{名称}.razor` |
| 应用服务接口 | `Application/Interfaces/` | `I{业务域}Service.cs` |
| 应用层 DTO | `Application/DTOs/{业务域}/` | `CreateOrderRequest.cs`、`OrderDetailResponse.cs` |
| 应用服务实现 | `Application/Services/` | `{业务域}Service.cs` |
| 实体 | `Domain/Entities/` | `{实体名}.cs` |
| 值对象（record） | `Domain/ValueObjects/` | `{名称}.cs` |
| 枚举 | `Domain/Enums/` | `{枚举名}.cs` |
| DbContext / EF 配置 | `Infrastructure/Data/` | `{项目名}DbContext.cs`、`{实体名}Configuration.cs` |
| 外部服务实现 | `Infrastructure/Services/` | `{名称}Service.cs` |
| SQL 迁移脚本（如使用数据库） | `Web/MigrationSql/` | `版本号.sql` / `版本号.txt`（如 `1.0.0.sql`、`1.1.0.txt`，不含描述文字） |

---

## 阶段 2 — 服务逻辑与数据访问

**触发条件**：用户发出「开始开发」或「开始服务层开发」指令

**入场要求**：阶段 0 设计文档已由用户确认；若界面已先行搭建，相关页面状态和交互入口已明确

**工作内容**：
1. 实现服务层逻辑、数据访问、权限控制和独立 API（如需要）。
2. 保持服务层、领域层、基础设施层职责清晰，不把复杂业务逻辑塞回 Razor 组件。
3. 所有增删改必须真实生效，并能通过页面、Swagger 或测试验证。
4. 涉及数据库结构变更时，使用 `Azrng.SqlMigration` 生成迁移脚本（如使用数据库）。

**门控规则**：
- 核心业务路径 smoke test 通过。
- 关键服务逻辑已补测试或在交付说明中解释未补原因。

---

## 后端规则

### 分层架构规则
```text
Web (Blazor Server) → Application → Domain ← Infrastructure
```
- 小型项目可合并到 `Web` 项目中，按目录分层。
- 大型项目建议拆分为独立类库。
- `Domain` 层：无外部依赖，包含核心业务模型。
- `Application` 层：依赖 Domain，定义服务接口和 DTO。
- `Infrastructure` 层：依赖 Application 和 Domain，实现具体技术细节。

<!-- include: shared/backend/controller-layer.md -->

### Service 层规则
- 服务类实现接口，接口定义在 `Interfaces/` 目录。
- 服务方法必须是异步的（返回 `Task<T>`）。
- 使用 DTO 进行数据传递，不直接暴露 Domain 实体。
- 业务异常抛出继承自 `BaseException` 的自定义异常。
- 禁止在 Controller、Service、Domain 实体、DTO 等 C# 类中使用主构造函数（Primary Constructor），统一使用显式构造函数。

<!-- include: shared/backend/domain-layer.md -->

### 代码组织规范
- 一个文件只放一个主类型：实体、值对象、DTO、Service 接口与实现等公开契约类，默认一个类一个文件，文件名与类名一致。
- DTO 目录组织：应用层 DTO 放 `Application/DTOs/{业务域}/`，Request 与 Response 各自独立文件；通用 DTO 放 Domain 层对应目录，不合并到 `Dtos.cs`。
- 触发拆分的信号：职责混杂、同文件出现多个主类型、参数或字段持续堆叠、方法跨多个不相关业务时，应主动拆分。
- 允许例外：`private` / `internal` 且只服务当前文件的小辅助类型、与主类型强绑定的局部 mapping extension、测试文件中只服务当前测试类的小型 fixture。
- 反模式：`Dtos.cs` 长期堆放十几个 Request / Response；把 Controller 专用 request 内联写在 Controller 文件里；把业务逻辑塞回 Razor 组件。

<!-- include: shared/application/kiss.md -->

### 数据访问规则（可选）
- 使用 Entity Framework Core 进行数据访问。
- DbContext 通过构造函数注入。
- 使用 LINQ 查询，避免原生 SQL。
- 涉及事务时使用 `IDbContextTransaction`。
- 软删除通过全局查询过滤器实现。

<!-- include: shared/backend/response-format.md -->

### 依赖注入规范
- 服务类必须实现以下接口之一：
  - `ITransientDependency`：每次请求创建新实例
  - `IScopedDependency`：每个 SignalR 连接作用域内单例
  - `ISingletonDependency`：应用程序生命周期单例
- Blazor 组件通过 `@inject` 注入服务。
- 禁止使用静态服务定位器 pattern。

### 异常处理规范
- 业务异常继承 `BaseException` 或其子类：
  - `LogicBusinessException`：业务逻辑异常
  - `ParameterException`：参数校验异常
  - `NotFoundException`：资源不存在
  - `ForbiddenException`：禁止访问
  - `InternalServerException`：服务器内部错误
- Blazor 页面使用 `ErrorBoundary` 组件捕获渲染异常。
- API 接口异常由中间件统一捕获并转换为 `ResultModel` 响应。

### Redis 缓存使用规范（可选）
- 使用 `Common.Cache.Redis` 封装的 `IRedisProvider` 接口。
- 通过 `services.AddRedisCacheStore()` 注册 Redis 服务。
- 缓存操作必须设置合理的过期时间。
- 禁止在循环中频繁调用 Redis。

### 数据库迁移规则（可选）
- 使用 `Azrng.SqlMigration` 进行数据库脚本迁移，不使用 EF Core Code-First Migrations。
- 迁移脚本放在宿主项目下 `Web/MigrationSql/` 目录，文件名按 `版本号.sql` 或 `版本号.txt` 命名（如 `1.0.0.sql`、`1.1.0.txt`），不含描述文字——去掉版本前缀与扩展名后的剩余部分会被整体解析为版本号；按版本号顺序执行。
- 通过 `AddSqlMigrationService(...).AddAutoMigration()` 在应用启动时自动执行未应用的脚本；已执行版本由包自动记录在版本日志表，不重复执行，无需手工维护版本表。
- 已应用的脚本不可修改；结构调整必须新增更高版本号的脚本。
- 涉及数据的高风险结构变更，建议同步准备回滚脚本并在文档中说明恢复方式。

<!-- include: shared/backend/comment-rules.md -->

<!-- include: shared/application/anti-patterns.md -->

<!-- include: shared/application/maintainability-check.md -->

---

## 测试规则

<!-- include: shared/backend/pre-commit-regression.md -->

<!-- include: shared/backend/general-requirements.md -->

<!-- include: shared/backend/backend-testing.md -->

<!-- include: shared/external-dependencies.md -->

<!-- include: shared/no-test-fallback.md -->

---
