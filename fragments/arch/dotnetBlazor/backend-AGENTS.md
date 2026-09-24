---
rule_id: backend-agents
version: 1.60.0
last_updated: 2026-09-07
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

<!-- include: shared/application/tech-choice.md -->

---

<!-- include: shared/backend/advisory.md -->
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

<!-- include: shared/backend/code-organization-dotnet.md -->
- 通用 DTO 放 Domain 层对应目录；业务逻辑不塞回 Razor 组件

<!-- include: shared/application/kiss.md -->

<!-- include: shared/backend/data-access-rules.md -->

<!-- include: shared/backend/response-format.md -->

<!-- include: shared/backend/di-rules.md -->
- Blazor Server 中 Scoped 服务基于 SignalR 连接生命周期；Blazor 组件通过 `@inject` 注入服务

<!-- include: shared/backend/exception-handling.md -->
- Blazor 页面使用 `ErrorBoundary` 组件捕获渲染异常

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

<!-- include: shared/backend/time-contract-rules.md -->

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
