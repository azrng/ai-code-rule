---
name: onion-architecture
description: Guide for the .NET onion/clean architecture used across Azrng solution templates (Core/IDomain/IApplication/Domain/Application/EntityFramework/Service layers, plus Storage/Adapter). Use when adding a new feature, deciding which project a class belongs in, wiring dependency-injection in Program.cs, creating a new AppService/Controller/Entity/Configuration, or reviewing where business logic vs data-access vs DTOs should live. Triggers on questions like "这个类该放哪一层", "新建一个 XXX 功能", "控制器太胖怎么拆", or confusion about IApplication vs Application / IDomain vs Domain project pairs.
---

# 洋葱架构（Onion / Clean Architecture）分层指南

指导在 Azrng 系列解决方案里正确分层：哪一层放什么、层间依赖方向、项目结对（接口/实现）、典型 CRUD 功能落点。

## 分层全景

依赖**由外向内**，内层不得引用外层。`Core` 是最内核（零业务依赖），`Service` 是最外层（组装+入口）。

```
                ┌─────────────────────────────────────────┐
   外层(依赖内层) │  Service（Web 入口：Controllers/Program） │
                ├─────────────────────────────────────────┤
                │  Application（应用服务实现：业务编排）      │  ← IApplication 的实现
                ├─────────────────────────────────────────┤
                │  Domain（领域服务实现，可选）               │  ← IDomain 的实现（多数项目可空）
                ├─────────────────────────────────────────┤
                │  EntityFramework / Storage（DbContext/存储）│
                ├─────────────────────────────────────────┤
   内核(被依赖)  │  IApplication  │  IDomain  │  Core        │
                │ (服务接口+DTO入口)│(实体+配置)│(纯DTO/常量) │
                └─────────────────────────────────────────┘
```

### 标准项目清单（按依赖从内到外）

| 项目 | 角色 | 可依赖 | 典型内容 |
|---|---|---|---|
| **Core** | 最内核，纯契约/契约数据 | 仅 NuGet 基础包 | DTO、Request/Response、枚举、常量 |
| **IDomain** | 领域契约层 | Core | 实体（Entity）、EFCore 配置类（EntityTypeConfigurations） |
| **IApplication** | 应用契约层 | Core, IDomain | AppService 接口（`I*AppService`） |
| **Domain** | 领域实现层（可选） | IDomain, Adapter, EntityFramework | 领域服务实现；多数 CRUD 项目此层可空 |
| **Application** | 应用实现层 | IApplication, Domain | AppService 实现（`*AppService`），业务编排、调用仓储 |
| **EntityFramework** | 基础设施-数据 | Core, IDomain | `DbContext`（继承 `BaseDbContext`）、迁移相关 |
| **Service** | 入口/组装层 | Application, Storage | `Controllers`、`Program.cs`（DI 装配）、中间件、配置 |
| **Adapter** | 外部适配器 | Core, IDomain | 第三方 API 封装、外部协议适配（可选） |
| **Storage** | 存储适配 | Core | 文件/对象存储封装（可选） |

> **接口/实现结对**：`I*` 项目只有抽象，对应无前缀项目是实现。`IApplication`/`Application`、`IDomain`/`Domain` 总是成对出现。控制器永远只依赖 `I*AppService`，不依赖实现。

## 核心规则（AI 必读）

1. **依赖单向向内**：外层引用内层，内层绝不引用外层。`Core` 不引用任何项目；`Service` 引用 `Application`（不直接引用 `IBaseRepository`）。
2. **控制器瘦**：控制器只做 HTTP 适配——接参数 → 调 `I*AppService` → 返回业务对象。**不写 EF 查询、不注入 `IBaseRepository`、不写业务校验**。
3. **查询/写库在 Application 层**：`AppService` 注入 `IBaseRepository<T>` / `IUnitOfWork`，承担所有数据访问与业务逻辑。
4. **DTO 在 Core**：请求/响应/传输对象统一放 `Core/Models`，供接口、实现、控制器、AppService 共享。不要在控制器内 inline 定义请求类。
5. **实体+配置在 IDomain**：实体继承 `Azrng.EFCore.Entities.IdentityOperatorStatusEntity`；配套配置类放 `IDomain/EntityTypeConfigurations/`（详见 azrng-efcore skill）。
6. **接口先行**：每个 AppService 先在 `IApplication` 定义 `IXxxAppService`，再在 `Application` 实现。控制器注入的是接口。
7. **返回值靠全局包装**：控制器方法直接 `return 业务对象`（项目已注册 `AddMvcResultPackFilter` 全局包装为 `ResultModel`），错误路径 `return ResultModel.Error(msg, code)`，不用 `Ok(...)`。

## 新增一个完整 CRUD 功能的标准流程

以"电影(Movie)管理"为例，自内向外依次落地 6 处：

1. **Core/Models/MovieDto.cs** — 定义 `MovieDto` / `MovieListRequest` / `MovieListResponse` / `CreateMovieRequest` / `UpdateMovieRequest`。
2. **IDomain/Entities/Movie.cs** — 实体继承 `IdentityOperatorStatusEntity`，业务属性 + 构造函数 `SetCreator("system")`。
3. **IDomain/EntityTypeConfigurations/MovieConfiguration.cs** — 继承 `EntityTypeConfigurationIdentityOperatorStatus<Movie>`，`base.Configure(builder)` 后配业务列。
4. **IApplication/MovieManager/IMovieAppService.cs** — 接口，方法签名用 Core 的 DTO。
5. **Application/MovieManager/MovieAppService.cs** — 实现，注入 `IBaseRepository<Movie>`，写查询/校验/CRUD；成功返回 DTO，失败 `ResultModel.Error(...)`。
6. **Service/Controllers/MovieController.cs** — 注入 `IMovieAppService`，每个 action 一行转发；并在 `Program.cs` `AddScoped<IMovieAppService, MovieAppService>()`。

> 数据库表随实体自动映射（`BaseDbContext` 扫描配置类），无需手改 DbContext；如用 SQL 脚本迁移，在 `Service/MigrationSql/` 增版本脚本。

## 命名约定

| 类型 | 命名 | 位置 |
|---|---|---|
| 应用服务接口 | `I{Entity}AppService` | IApplication |
| 应用服务实现 | `{Entity}AppService` | Application |
| 控制器 | `{Entity}Controller` | Service/Controllers |
| 实体 | 单数 `{Entity}` | IDomain/Entities |
| 配置类 | `{Entity}Configuration` | IDomain/EntityTypeConfigurations |
| DbContext | `{Solution}DbContext` | EntityFramework |

## 何时该拆/不该拆（决策速查）

- **业务逻辑该去 Application 还是 Domain？** 纯 CRUD + 简单校验 → Application 层 `AppService` 直接搞定。涉及多实体复杂领域规则、可复用的领域计算 → 抽到 Domain 层 `DomainService`，Application 调用它。多数业务系统 Domain 层可空。
- **DTO 还是实体？** 跨层传输/对外暴露用 DTO（Core）。DB 持久化用实体（IDomain）。**永远不要把实体直接返回给前端**（暴露审计字段、耦合存储结构），在 AppService 里映射成 DTO。
- **该不该新建项目？** 标准模板共 9 个项目：7 个核心分层（Core/IDomain/IApplication/Domain/Application/EntityFramework/Service）+ 2 个可选基础设施（Adapter 外部 API 封装、Storage 文件/对象存储）。只有引入全新基础设施（如 Redis、消息队列、第三方 SDK）才新增独立项目（如 `Cache`/`Notification`），并保持单向依赖。多数 CRUD 项目 Domain/Adapter/Storage 留空（占位 `Class1.cs`）即可。
- **控制器能不能调多个 AppService？** 可以，但要警惕——若一个请求要协调多个服务完成一个事务，说明该把编排逻辑下沉到一个 AppService（用 `IUnitOfWork` 包事务），控制器只调这一个。

## 依赖注入装配（Program.cs）

实体框架注入一次（详见 azrng-efcore skill），AppService 逐个 `AddScoped`：

```csharp
builder.Services.AddEntityFramework<MovieManageDbContext>(o =>
{
    o.ConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
    o.Schema = "public";
});

// 每个 AppService 一行注册（接口→实现，Scoped）
builder.Services.AddScoped<IMovieAppService, MovieAppService>();
builder.Services.AddScoped<ICategoryAppService, CategoryAppService>();
// ...
```

> 不自动扫描注入，保持显式注册（可追溯）。控制器由 `AddControllers()` 自动发现，无需注册。

## 常见反模式（必须避免）

| 反模式 | 正确做法 |
|---|---|
| 控制器注入 `IBaseRepository<T>` 写查询 | 查询移到 AppService，控制器只调 AppService |
| 控制器里 `return Ok(result)` | 直接 `return result`（全局 `ResultPackFilter` 已包装） |
| 实体直接作为 API 返回值 | AppService 映射成 Core 的 DTO 再返回 |
| 在 Core 引用 EFCore/HttpContext | Core 零基础设施依赖，只放纯数据契约 |
| AppService 里写 `SELECT *` 原生 SQL 拼 UI | 用 `IBaseRepository.Entities/EntitiesNoTacking` + LINQ |
| 新功能不建接口直接写实现类 | 先 `I*AppService` 接口，控制器依赖接口 |
| 业务校验写在控制器 | 校验下沉到 AppService，返回 `ResultModel.Error` |

## 深入参考

- **各层职责详细对照 + 跨层数据流 + 完整代码示例**：见 references/layer-responsibilities.md
- **数据访问（仓储/工作单元/实体基类/配置类/DbContext）细节**：用 azrng-efcore skill
