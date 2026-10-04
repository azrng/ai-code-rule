---
name: onion-architecture
description: "Guide for the .NET onion/clean architecture used across Azrng solution templates (Core/IDomain/IApplication/Domain/Application/EntityFramework/Service layers, plus Storage/Adapter). Covers layer responsibilities, dependency direction, and the cross-cutting AppService conventions — global result-packing and paged queries. Use when adding a new feature, deciding which project a class belongs in, creating an AppService/Controller/Entity/Configuration, wiring DI/middleware through Service Startup, or placing business logic vs data-access vs DTOs. Keywords: onion architecture, clean architecture, 分层, IApplication vs Application, AppService, IResultModel, 分页查询."
---

# 洋葱架构（Onion / Clean Architecture）分层指南

指导在 Azrng 系列解决方案里正确分层：哪一层放什么、层间依赖方向、项目结对（接口/实现）、典型 CRUD 功能落点，以及贯穿所有 AppService 的两条横切约定——**全局返回包装**与**分页查询**。

> 本文件是**决策入口与规则总览**；各层完整代码示例、跨层数据流、`.csproj` 依赖详见 `references/layer-responsibilities.md`。

## 分层全景

依赖**由外向内**，内层不得引用外层。`Core` 是最内核（零业务依赖），`Service` 是最外层（组装+入口）。

```
                ┌─────────────────────────────────────────┐
   外层(依赖内层) │  Service（Web 入口：Controllers/Program） │
                ├─────────────────────────────────────────┤
                │  Application（应用服务实现：业务编排）      │  ← IApplication 的实现
                ├─────────────────────────────────────────┤
                │  Domain（领域服务实现：可复用公共操作）               │  ← IDomain 的实现（按需）
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
| **IDomain** | 领域契约层 | Core | 实体（Entity） |
| **IApplication** | 应用契约层 | Core, IDomain | AppService 接口（`I*AppService`） |
| **Domain** | 领域实现层 | IDomain, Adapter, EntityFramework | 领域服务实现（`{X}DomainService`）：封装可复用公共领域操作，可注入 IBaseRepository 写库；纯 CRUD 可空 |
| **Application** | 应用实现层 | IApplication, Domain | AppService 实现（`*AppService`）：用例编排、实体↔DTO 映射、调用 DomainService/仓储/适配器 |
| **EntityFramework** | 基础设施-数据 | Core, IDomain | `DbContext`、EFCore 配置类（`{Entity}Etc.cs`）、迁移 |
| **Service** | 入口/组装层 | Application, EntityFramework, Storage | `Controllers`、`Program.cs`（宿主生命周期）、`Startup.cs`（DI 装配 + 中间件管道）、配置 |
| **Adapter** | 外部适配器 | IDomain | 外部系统调用封装：`I{X}Adapter`（接口在 `IDomain/Adapter/`）+ 适配实现（`Adapter/Services/`）+ 可选 HTTP 客户端包装（`Adapter/HttpClients/`），屏蔽 HTTP/SDK 细节（可选） |
| **Storage** | 存储适配 | Core、IDomain | 文件/对象存储实现（可选）；存储接口放 `IDomain` 或真正通用时放 `Core` |

> **接口/实现结对**：`I*` 项目只有抽象，对应无前缀项目是实现。`IApplication`/`Application`、`IDomain`/`Domain` 总是成对出现。控制器永远只依赖 `I*AppService`，不依赖实现。

## 解决方案与目录结构

解决方案文件放在仓库根目录，使用相对路径引用 `src/` 下的业务项目；测试项目统一放在根目录 `test/` 下。不要把 `.slnx` 放进某个业务项目目录，也不要把测试项目混入 `src/`。

```text
YourProject/
├── YourProject.slnx
├── src/
│   ├── YourProject.Core/
│   ├── YourProject.IDomain/
│   ├── YourProject.IApplication/
│   ├── YourProject.Domain/
│   ├── YourProject.Application/
│   ├── YourProject.EntityFramework/
│   ├── YourProject.Service/
│   └── ...
└── test/
    └── YourProject.Tests/
```

`.slnx` 至少按 `/src/` 与 `/test/` 划分解决方案目录；没有测试项目时可以先保留空的 `/test/` 节点：

```xml
<Solution>
  <Folder Name="/src/">
    <Project Path="src/YourProject.Core/YourProject.Core.csproj" />
    <Project Path="src/YourProject.Service/YourProject.Service.csproj" />
  </Folder>
  <Folder Name="/test/" />
</Solution>
```

新增测试项目时，物理路径使用 `test/YourProject.Tests/YourProject.Tests.csproj`，并在 `/test/` 节点下以相对路径引用。

## 核心规则（AI 必读）

1. **依赖单向向内**：外层引用内层，内层绝不引用外层。`Core` 不引用任何项目；`Service` 引用 `Application`（不直接引用 `IBaseRepository`）。
2. **控制器瘦**：控制器只做 HTTP 适配——接参数 → 调 `I*AppService` → 一行转发返回值。**不写 EF 查询、不注入 `IBaseRepository`、不写业务校验、不写 `Ok()`/`return ResultModel`**。
3. **应用编排 vs 领域操作分层**：`AppService`（应用层）承担**用例编排**——接 DTO → 调领域服务/仓储/适配器 → 返回 DTO + 实体↔DTO 映射；**可复用的公共领域操作下沉到 DomainService**（领域层），如「发会话消息」「读系统配置」这类被多个 AppService 复用的操作。**AppService 和 DomainService 都可注入 `IBaseRepository<T>` / `IUnitOfWork` 做数据访问**（并非 AppService 独占）。判断：单一用例、不复用 → 直接写在 AppService；多 AppService 复用 / 稳定领域概念 → 抽 DomainService。详见「领域服务 DomainService」。
4. **DTO 放 IApplication/{业务域}/Dto/**：应用层 DTO/Request/Response 放 `IApplication/{业务域}/Dto/`（如 `IApplication/Role/Dto/CreateRoleRequest.cs`），一类一文件。不要在控制器内 inline 定义请求类。**「通用」= 跨业务域/跨模块复用**（公共枚举、常量、跨域包装）才放 `Core/Models`——单业务域的 DTO 即使接口、实现、控制器三方都在用也放 IApplication（控制器注入 `I*AppService` 时本来就引用 IApplication，不产生新依赖）。注意：不是「跨层共享（三方都用）」就入 Core——跨层使用是应用层 DTO 的常态，按那个判据所有 DTO 都会命中 Core，`IApplication/{业务域}/Dto/` 规则将永远没有适用对象。领域服务/适配器接口签名用的业务对象 Bo 放 `IDomain/{业务域}/Bo/`；外部适配器接口签名用的 Bo 放 `IDomain/Adapter/Bo/{外部系统}/`。
5. **实体在 IDomain、配置在 EntityFramework**：实体（继承 `IdentityOperatorStatusEntity`）放 `IDomain/Entities/`；EFCore 配置类（`{Entity}Etc.cs`，继承 `EntityTypeConfigurationIdentityOperatorStatus<TEntity, TKey>`）放 **EntityFramework 层**，`Configure` 里设表名/列名/查询过滤；DbContext 用 `ApplyConfigurationsFromAssembly(当前程序集)` 扫描它们（详见 azrng-efcore skill）。
6. **接口先行**：每个 AppService 先在 `IApplication` 定义 `IXxxAppService`，再在 `Application` 实现。控制器注入的是接口。
7. **返回值走全局包装（首选）**：AppService **成功直接 `return Task<T>`（业务对象）**，由 `CustomResultPackFilter` 自动包成 `ResultModel<T>(success)`；**业务校验/规则失败 `throw new ParameterException(msg)`**（或 `LogicBusinessException`），由全局异常中间件 `app.UseGlobalException()` 统一映射成失败结果。控制器一行转发 `return _appService.XxxAsync(...)`。**三件套缺一不可**（见「全局返回包装」）。历史写法 `Task<IResultModel<T>>` 手动 `Success/Error` 为迁移期可容忍，新代码不再使用。

## 全局返回包装（三件套 + Startup 装配）

这是本架构的**统一对外结果契约**：所有 action 返回值最终都是 `{ isSuccess, isFailure, code, message, data, errors }` 形态的 `ResultModel`，但**业务代码不手动包装**——靠框架三件套自动完成。没有数据或没有错误时，`data` / `errors` 仍可能以空值或空数组输出，按实际序列化配置为准。

### 三件套（来自 `Azrng.AspNetCore.Core`，缺一不可）

| 件 | 注册 | 负责路径 | 行为 |
|---|---|---|---|
| 返回值包装过滤器 `CustomResultPackFilter` | `services.AddMvcResultPackFilter("/swagger")` | **成功路径** | action 返回值**非 `IResultModel`** → 包成 `ResultModel<T>(data, true, "success", "200")`；`EmptyResult`→`ResultModel(true,"成功")`；已是 `IResultModel` 则**不重复包装**；`[NoWrapper]` 特性或忽略前缀跳过 |
| 模型校验过滤器 `ModelVerifyFilter` | `services.AddMvcModelVerifyFilter()` | **模型校验失败** | 把 `ModelState` 错误转成 `ResultModel(false,"参数格式不正确","400", errors)` |
| 全局异常中间件 `CustomExceptionMiddleware` | `app.UseGlobalException()` | **业务异常 / 未处理异常** | catch 异常，按类型映射成 `ResultModel(false, msg, code)`（见下表） |

> **成功路径靠过滤器，失败路径靠异常中间件**——这是全局包装的核心。两者必须都装配，否则失败抛异常会变成裸 500。

### 业务异常 → 结果映射（`CustomExceptionMiddleware`）

| 业务场景 | 抛什么（`Azrng.Core.Exceptions`） | HTTP 状态码 | ErrorCode |
|---|---|---|---|
| 参数错误 / 业务校验失败 | `throw new ParameterException(msg)` | 400 | `"400"` |
| 业务规则不满足（语义同参数异常） | `throw new LogicBusinessException(msg)` | 400 | `"500"`（见下） |
| 资源不存在（按 id 查不到） | `if (entity is null) throw new NotFoundException("记录不存在")` | 404 | `"404"` |
| 未认证（未登录或凭证无效） | `throw new UnauthorizedException(msg)` | 401 | `"401"` |
| 已认证但无权限 | `throw new ForbiddenException(msg)` | 403 | `"403"` |
| 服务器内部错误 | `throw new InternalServerException(msg)` | 500 | `"500"` |
| 其他未处理异常 | （任何 `Exception`） | 500 | `"500"` |

> 默认按真实 HTTP 状态码返回（400/401/403/404/500）。若要统一返回 200、错误信息只放 body，配 `CommonMvcConfig.UseHttpStateCode = false`。

> **`LogicBusinessException` 的结果码版本差异**（Azrng.Core 1.20.0 实测）：中间件把它映射为 HTTP 400，但 `ResultModel.code` 取自异常的 `ErrorCode`；`LogicBusinessException(string message)` 构造走 `BaseException(string)`，`ErrorCode` 为 `"500"`，只有无参构造才是 `"400"`。因此需要 code 与 HTTP 400 一致时优先用 `ParameterException(msg)`；用 `LogicBusinessException` 时前端按 `isSuccess=false` 判错，不要依赖 code。

> 当前 `CustomExceptionMiddleware` 对 `ForbiddenException` 写入 HTTP 403 和结果码 `403`；部分 `Azrng.Core` 版本的 `ForbiddenException.ErrorCode` 构造值仍是 `401`，这是包内历史兼容问题，不应据此把“已认证但无权限”文档化为 401。

> `AddMvcResultPackFilter` 的忽略前缀按 URI 路径匹配，传值应以 `/` 开头，例如 `/swagger`。传入 `swagger` 可能不影响启动，但在首个结果过滤请求时才抛参数异常。

### Startup 装配（标准模板，三件套都在这里）

Service 项目使用与 `Program.cs` 平级的 `Startup.cs` 承担依赖注入和中间件管道装配。`Program.cs` 只负责创建 WebApplicationBuilder、调用 Startup、构建应用并运行宿主，不堆积业务服务注册或中间件细节。

```csharp
// Program.cs
using DotNetEnv;

// 本地开发敏感配置走仓库根目录 .env（gitignore 排除）。注入为进程环境变量后由默认配置链读取，
// 不覆盖已存在的环境变量（NoClobber）；生产/容器无 .env 时不加载任何值
Env.NoClobber().TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);
var startup = new Startup(builder.Configuration, builder.Environment);

startup.RegisterServices(builder.Services);

var app = builder.Build();
startup.SetupMiddlewares(app);
app.Run();
```

`DotNetEnv` 是 Service 项目的可选开发依赖；`.env` 的放置、`.gitignore` 约定与「加载必须发生在 `CreateBuilder` 之前」的时序细则见 `references/layer-responsibilities.md → Program.cs 与 Startup.cs 分工`。

```csharp
// Service/Startup.cs
public sealed class Startup
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public Startup(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    public void RegisterServices(IServiceCollection services)
    {
        services.AddControllers();

        // 成功路径包装（Swagger 等前缀不包装）
        services.AddMvcResultPackFilter("/swagger");
        // 模型校验失败统一结果
        services.AddMvcModelVerifyFilter();

        services.AddEntityFramework<AppDbContext>(options =>
        {
            options.ConnectionString = _configuration.GetConnectionString("DefaultConnection")!;
        });
        services.RegisterBusinessServices("YourProject.*.dll");

        // CORS：默认放开任意来源（仅开发联调，开箱即用）；生产按需改 AddCorsByOrigins 读配置白名单
        services.AddAnyCors();

        var migrationSqlPath = Path.Combine(
            _environment.ContentRootPath,
            "wwwroot",
            "MigrationSql");
        services.AddSqlMigrationService("default", options =>
        {
            options.VersionPrefix = string.Empty;
            options.SqlRootPath = migrationSqlPath;
            options.ConnectionBuilder = _ =>
                new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection")!);
        }).AddAutoMigration();
    }

    public void SetupMiddlewares(WebApplication app)
    {
        // 必须放在管道最外层，才能兜住后续中间件和控制器抛出的异常
        app.UseGlobalException();

        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.UseCorsPolicy();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
    }
}
```

> **最常见的坑**：只注册了 `AddMvcResultPackFilter` 却忘了 `app.UseGlobalException()`——此时 AppService 抛 `ParameterException` 无人接住，业务失败会返回 HTTP 500，前端拿不到统一失败结构。三件套必须齐备。

### CORS 跨域装配

CORS 优先用 `Azrng.AspNetCore.Core` 内置扩展，不要手写原生 `AddCors`/`AddPolicy`（特殊策略需求除外）：

- **注册（模板默认）**：`services.AddAnyCors()`——`AllowAnyOrigin`，开箱即用，**仅限开发环境**，上线前必须改掉。
- **生产按需改**：`services.AddCorsByOrigins(string[] allowedOrigins)`——来源白名单走配置（`appsettings.json` 的 `Cors:AllowedOrigins` 节，生产用环境变量 `Cors__AllowedOrigins__N` 覆盖）；空列表 / 含空白项启动即抛 `ArgumentException`，防漏配；`allowCredentials: true` 仅 SignalR / 携带凭证场景。
- **管道**：`app.UseCorsPolicy()`（默认策略名 `DefaultCors`，两种注册方式通用），放在异常处理 / Swagger / 静态文件之后、`UseAuthentication` / `UseAuthorization` / `MapControllers` 之前。
- **禁忌**：未注册 CORS 服务就写 `app.UseCors()`，首个跨域请求直接 500；`AddAnyCors` 不得用于生产；`allowCredentials: true` 不得与 `AllowAnyOrigin` 组合。纯后端 / 无跨域需求的项目整体删掉注册与中间件两行，不是必装项。

### Swagger 与静态文件装配

当前 `Azrng.Swashbuckle` 的 `UseDefaultSwagger` 行为如下：`onlyDevelopmentEnabled: true` 时仅当 `ASPNETCORE_ENVIRONMENT` 为 `Development` 才注册 Swagger；传 `false`（默认值）时所有环境都注册。默认不传自定义 UI 配置时，UI 的 `RoutePrefix` 为空，即访问根路径 `/`；JSON 端点是 `/swagger/v1/swagger.json`。

需要把 UI 放到 `/swagger` 时显式设置路由前缀：

```csharp
app.UseDefaultSwagger(
    onlyDevelopmentEnabled: false,
    action: options =>
    {
        options.RoutePrefix = "swagger";
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "App API v1");
    });
```

生产环境不需要开放 Swagger 时使用 `onlyDevelopmentEnabled: true`，不要依赖约定中的 `SwaggerAPI` 路径。若项目提供 `wwwroot/index.html` 作为手工联调页，在 `MapControllers()` 前加入：

```csharp
app.UseDefaultFiles();
app.UseStaticFiles();
```

### 默认健康检查控制器

Service 项目默认提供 `HealthController`，供容器编排、负载均衡和运维探针使用。`alive` 只表示进程仍能响应，`ready` 表示服务已完成启动；如果 `ready` 还需要核对数据库或其它依赖，应在该端点中接入对应检查，不要让 `alive` 依赖外部服务。两个探针必须使用 `[NoWrapper]` 返回裸 JSON，避免被全局结果包装器改写。

```csharp
using Azrng.AspNetCore.Core.Filter;
using Microsoft.AspNetCore.Mvc;

namespace YourProject.Service.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class HealthController : ControllerBase
{
    [HttpGet("alive")]
    [NoWrapper]
    public IActionResult Alive() => Ok(new { status = "ok" });

    [HttpGet("ready")]
    [NoWrapper]
    public IActionResult Ready() => Ok(new { status = "ok" });
}
```

### 文件下载与原始响应

`NoWrapperAttribute` 位于 `Azrng.AspNetCore.Core.Filter`。包装过滤器本身会自动跳过 `FileResult`，因此 `return File(...)` 通常不需要 `[NoWrapper]`；如果 action 返回的是其它原始 `IActionResult`，可以显式加 `[NoWrapper]` 作为保护。

```csharp
using Azrng.AspNetCore.Core.Filter;
using Microsoft.AspNetCore.Mvc;

[HttpGet("download/{id:long}")]
[NoWrapper]
public async Task<IActionResult> Download(long id)
{
    var (originalName, content) = await _materialAppService.DownloadAsync(id);
    return File(content, "application/octet-stream", originalName);
}
```

### AppService / 控制器写法（全局包装模式）

```csharp
// 接口（IApplication）：返回裸 Task<T>，不出现 IResultModel
public interface IOrderAppService
{
    Task<OrderDto> GetAsync(long id);                 // 成功返 DTO
    Task<long> CreateAsync(CreateOrderRequest req);   // 成功返 id
    Task DeleteAsync(long id);                        // 无返回数据
}

// 实现（Application）：成功直接 return；失败 throw 业务异常（不要手写 ResultModel.Error）
public async Task<OrderDto> GetAsync(long id)
{
    var order = await _repository.EntitiesNoTacking.FirstOrDefaultAsync(x => x.Id == id);
    if (order is null)                                  // 资源不存在 → 404；is null 检查原生收窄可空流分析
        throw new NotFoundException("订单不存在");
    return new OrderDto { /* 直接使用 order 完成实体→DTO 映射，无需断言 */ };
}

public async Task<long> CreateAsync(CreateOrderRequest req)
{
    if (req.Amount <= 0)
        throw new ParameterException("金额必须大于 0");   // 业务校验失败 → 400
    var order = new Order { /* ... */ };
    await _repository.AddAsync(order, submit: true);
    return order.Id;                                     // 成功 → 过滤器包成 ResultModel<long>(success)
}
```

```csharp
// 控制器（Service）：一行转发，不写 Ok() / 不写 ResultModel
[HttpGet("{id}")]
public Task<OrderDto> Get(long id) => _orderAppService.GetAsync(id);

[HttpPost]
public Task<long> Create([FromBody] CreateOrderRequest req) => _orderAppService.CreateAsync(req);
```

> 关于 `IResultModel<T>`（历史/迁移期）：旧代码里 AppService 返回 `Task<IResultModel<T>>`、手动 `ResultModel<T>.Success/Error`，仍能工作（过滤器不重复包装），迁移期可与全局包装共存。但新代码统一用全局包装：更省事、控制器/AppService 一行转发。逐步替换即可。

## 分页查询最佳实践

列表查询**不要手写 `CountAsync()` + `Skip().Take().ToListAsync()`**，也不要自定义 `{ List, Total }` 响应类。用框架的一套：请求继承 `GetPageRequest` + 链式条件 + `ToPageListAsync` + 返回 `GetQueryPageResult<T>`。

### 四件套

| 角色 | 类型/API | 命名空间（using） |
|---|---|---|
| 分页请求基类（带 `PageIndex`/`PageSize`） | `GetPageRequest` | `Azrng.Core.Requests` |
| 条件筛选（值非空才 Where，替代手写 `if`） | `IQueryable<T>.WhereIfNotNullOrWhiteSpace(value, predicate)` | `Azrng.Core.Extension` |
| 一次完成 Count + 分页 | `IQueryable<T>.ToPageListAsync(GetPageRequest, RefAsync<int>)` | `Azrng.EFCore.Extensions` |
| 总数引用容器 | `RefAsync<int>` | `Azrng.Core.CommonDto` |
| 分页结果（`Rows` + `PageInfo`） | `GetQueryPageResult<T>` | `Azrng.Core.Results` |

### 标准写法（AppService）

```csharp
using Azrng.Core.CommonDto;
using Azrng.Core.DependencyInjection;
using Azrng.Core.Extension;
using Azrng.Core.Requests;
using Azrng.Core.Results;
using Azrng.EFCore;
using Azrng.EFCore.Extensions;
using Microsoft.EntityFrameworkCore;

// 1) 请求类继承 GetPageRequest，只声明业务筛选字段（PageIndex/PageSize 由基类提供）
public class EmployeeListRequest : GetPageRequest
{
    public string? Username { get; set; }
    public string? Role { get; set; }
}

// 2) AppService：链式 WhereIfNotNullOrWhiteSpace + ToPageListAsync，返回 GetQueryPageResult<Dto>
public async Task<GetQueryPageResult<EmployeeDto>> GetListAsync(EmployeeListRequest request)
{
    var total = new RefAsync<int>();                 // 接收总数
    var list = await _repository.EntitiesNoTacking.Where(e => !e.Deleted)
        .WhereIfNotNullOrWhiteSpace(request.Username, t => t.Username.Contains(request.Username!))
        .WhereIfNotNullOrWhiteSpace(request.Role, t => t.Role == request.Role)
        .OrderByDescending(e => e.CreateTime)
        .Select(t => new EmployeeDto { Id = t.Id, Username = t.Username, /* ... */ })
        .ToPageListAsync(request, total);            // 内部一次 Count + Skip/Take

    return new GetQueryPageResult<EmployeeDto>(list, new GetQueryPageResult(request, total));
    // 或等价的四参构造：new GetQueryPageResult<EmployeeDto>(list, request.PageIndex, request.PageSize, total);
}
```

> `ToPageListAsync` 实际返回 `Task<List<T>>`，不是分页结果对象；必须再用 `new GetQueryPageResult<T>(list, new GetQueryPageResult(request, total))` 或四参数构造函数组装分页结果。完整 using 为 `Azrng.Core.CommonDto`、`Azrng.Core.Extension`、`Azrng.Core.Requests`、`Azrng.Core.Results`、`Azrng.EFCore.Extensions`。

> `RefAsync<T>` 有到 `T` 的隐式转换，所以 `new GetQueryPageResult(request, total)` 里 `RefAsync<int>` 可直接当 `long totalCount` 用。
> 返回的 `GetQueryPageResult<Dto>` 经全局包装过滤器自动包成 `ResultModel<GetQueryPageResult<EmployeeDto>>(success)`，前端拿到 `{ isSuccess, data: { rows:[...], pageInfo:{ pageIndex, pageSize, total, totalPage } } }`。
> 分页/条件扩展 API 的完整签名（`PagedBy`、`WhereIfNotNull<T,F>`、`WhereAny` 等）见 azrng-efcore skill「分页/条件查询扩展」。

## 领域服务 DomainService（可复用公共操作）

当一段领域操作**会被多个 AppService 复用**，或封装**稳定的多实体领域规则/数据访问**时，不要在每个 AppService 里重复写——抽成 DomainService。典型：会话消息收发、系统配置读写、指标计算、同义词管理等“公共能力”。

**结对模式**（与 AppService 一致：接口在 `I*`、实现在无前缀项目）：

| | 位置 | 命名 |
|---|---|---|
| 接口 | `IDomain/{Module}/` | `I{X}DomainService` |
| 实现 | `Domain/{Module}/` | `{X}DomainService : IScopedDependency, I{X}DomainService` |

**关键点**：
- DomainService **可注入 `IBaseRepository<T>` / `IUnitOfWork` 自己读写数据**（`Domain` 项目依赖 `EntityFramework`），也可注入 Adapter/缓存等——**数据访问并非 AppService 独占**。
- AppService 注入 `I{X}DomainService` 调用它，自己只做用例编排 + 实体↔DTO 映射。
- DomainService 返回领域对象（Entity/Bo），**不碰 HTTP 与 DTO 映射**（那是 AppService 的活）；同样标 `IScopedDependency` 自动注册。

> 完整代码示例（接口/实现/AppService 调用三方结对）见 `references/layer-responsibilities.md → Domain 层`。

## 文件上传与外部存储

应用层和领域契约层不接受 `IFormFile`、`HttpContext` 等 ASP.NET 类型。文件上传时，在 `IApplication/{业务域}/Dto/` 定义只包含业务数据的 Command（例如 `FileName`、`Size`、`Stream`；仅本业务域使用，不放 `Core/Models`），由 Controller 把 `IFormFile` 适配后再调用 AppService。

存储接口放在 `IDomain`（例如 `IDomain/Storage/IMaterialStorage.cs`），本地磁盘或对象存储实现放在 `Storage` 项目。`Storage` 可以依赖 `Core + IDomain`，Service 组合根负责注册实现；Application 只依赖 `IDomain` 中的接口。

```csharp
// 文件：IApplication/Material/Dto/MaterialUploadCommand.cs —— 仅 Material 业务域使用，非跨域通用，不放 Core/Models
public class MaterialUploadCommand
{
    public string FileName { get; set; } = string.Empty;
    public long Size { get; set; }
    public Stream Content { get; set; } = Stream.Null;
}

// 文件：Service/Controllers/MaterialController.cs
[HttpPost("upload")]
public Task<MaterialDto> Upload(IFormFile file)
    => _materialAppService.UploadAsync(new MaterialUploadCommand
    {
        FileName = file.FileName,
        Size = file.Length,
        Content = file.OpenReadStream()
    });
```

> **判断门槛**：操作只被**一个 AppService 用、不会复用**时，直接写在那个 AppService 里即可，不必提前抽 DomainService（避免过度设计）。复用真正出现、或领域规则变复杂时再下沉——`ISystemConfigDomainService` 就是后来被 5+ 个 AppService 共用才抽出来的。

## 外部适配器 Adapter（封装第三方/外部系统调用）

调用**外部系统**（第三方 API、其他微服务、SDK、外部协议）时，用 Adapter 层封装——把 HTTP/协议细节屏蔽在 Adapter 内，对内暴露领域语义方法，让 AppService/DomainService 依赖**接口**而非具体 HttpClient。

### 两层结构（复杂外部调用推荐）

| 层 | 接口位置 | 实现位置 | 数据对象 | 职责 |
|---|---|---|---|---|
| 领域适配器 `I{X}Adapter` | `IDomain/Adapter/` | `Adapter/Services/` | **Bo**（`IDomain/Adapter/Bo/{外部系统}/`） | 对外领域语义方法、Bo↔Ho 转换 |
| HTTP 客户端包装 `I{X}HttpClient` / `{X}HttpClient`（可选） | `Adapter/HttpClients/` | `Adapter/HttpClients/` | **Ho**（`Adapter/Ho/{外部系统}/`） | 注入 `IHttpHelper`，负责真实 HTTP 调用；简单场景可省略 |

### 关键点

- **接口放 IDomain/Adapter/（不放 Core）**：这样 AppService/DomainService 只依赖 IDomain 就能用 Adapter，而 HttpClient/SDK 等**实现细节留在 Adapter 项目**，依赖方向正确（外层 Adapter 实现内层 IDomain 接口；IDomain 不依赖 Adapter）。`Adapter.csproj` 的项目引用只依赖 `IDomain`，外部 HTTP 所需 NuGet 包按需引用在 Adapter。
- **Bo vs Ho**：`Bo`（业务对象，接口签名用）在 `IDomain/Adapter/Bo/{外部系统}/`；`Ho`（HTTP 传输对象）在 `Adapter/Ho/{外部系统}/`。Adapter 实现里做 Bo↔Ho 转换——外部协议/字段变了只改 Adapter，不波及业务层。
- **HTTP 默认走 `IHttpHelper`**（`Common.HttpClients`）：不为单次调用包装 HttpClient，命名客户端、超时、重试等客户端策略在 Service 组合根配置（`AddHttpClientService` / `IHttpHelperFactory`）；只有需要包未暴露的底层控制时才在 Adapter 内使用原生 `HttpClient` / `IHttpClientFactory`，HTTP 细节不向业务层暴露。

> 完整代码示例与 `IHttpHelper` 用法细则（`IHttpResult` 处理、`SendAsync`、版本核对）见 `references/layer-responsibilities.md → Adapter 层`。

> **判断门槛**：调用**本系统数据库** → AppService/DomainService 直接注入 `IBaseRepository`；调用**外部系统** → 走 Adapter（接口 `I{X}Adapter` 在 IDomain，实现+HttpClient 在 Adapter），业务层只依赖接口。简单外部调用可只用 `Adapter/Services/{X}Adapter.cs` 一层，复杂或需要独立测试边界时再拆 `Adapter/HttpClients/`。

## JSON 序列化（IJsonSerializer）

需要 JSON 序列化时，统一注入 `IJsonSerializer`（`Azrng.Core.IJsonSerializer`），**不要在各处直接用 `JsonSerializer`/`JsonConvert`**——底层切换 STJ↔Newtonsoft 只改一处。API：`ToJson<T>(obj)`、`ToObject<T>(json)`、`Clone<T>(obj)`（深拷贝）、`ToList<T>(json)`。

- 实现 + 注册：`Azrng.Core.Json`（System.Text.Json 实现 `SysTextJsonSerializer`，推荐、AOT 友好）或 `Azrng.Core.NewtonsoftJson`（`NewtonsoftJsonSerializer`，兼容旧代码）。Service 项目装实现包后 `builder.Services.ConfigureDefaultJson();`（`using Azrng.Core.Json;`）注册。
- 默认配置（`DefaultJsonSerializerOptions`）：驼峰命名、不转义中文（`UnsafeRelaxedJsonEscaping`）、忽略循环引用。
- **HTTP 响应的 long→string 需单独配置**：`ConfigureDefaultJson` 只注册 `IJsonSerializer` 及其 options，**不碰 MVC 的 `AddJsonOptions`**，控制器的 JSON 响应序列化由 MVC 自己的 `JsonOptions` 决定。雪花 long 主键超出 JS `2^53` 精度，前端拿数字 id 会精度丢失——需要在 HTTP 响应中 long→string 时，必须在 MVC 层自行注册：

```csharp
// Service/Startup.cs 的 RegisterServices：为 MVC 响应启用 long→string，防前端精度丢失
// 包内置转换器在 Azrng.Core.Json.JsonConverters 命名空间（using Azrng.Core.Json.JsonConverters;）
services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new LongToStringConverter());
});
```

也可在 Service/Filters/LongToStringJsonConverter.cs 自定义转换器（入口层横切关注，不入侵业务层）：`JsonConverter<long>`，Write 时 `value.ToString(CultureInfo.InvariantCulture)`，Read 时 `long.Parse`。若还要覆盖 `long?` / `IEnumerable<long>` 等形态，用 `JsonConverterFactory` 按请求类型分发。

> 业务层依赖 `IJsonSerializer` 接口；切换底层只改 Service 项目装的实现包 + 注册，不波及业务。

## 新增一个完整 CRUD 功能的标准流程

以"商品(Product)管理"为例，自内向外依次落地（详版含每步完整代码见 `references/layer-responsibilities.md → 完整功能落点速查`）：

1. **IApplication/Product/Dto/** — 应用层契约类一类一个文件、文件名与类名一致：`ProductDto.cs`、`ProductListRequest.cs`（继承 `GetPageRequest`）、`CreateProductRequest.cs`、`UpdateProductRequest.cs`。这些类只服务 Product 业务域，即使接口/实现/控制器三方共用也不放 Core（`Core/Models` 仅收跨业务域通用 DTO/枚举/常量，判据见核心规则 4）。
2. **IDomain/Entities/Product.cs** — 实体继承 `IdentityOperatorStatusEntity`，构造函数 `SetCreator("system")`。
3. **EntityFramework/ProductEtc.cs** — 配置类继承 `EntityTypeConfigurationIdentityOperatorStatus<Product, long>`，`base.Configure(builder)` 后设表名/列名/查询过滤。
4. **IApplication/Product/IProductAppService.cs** — 接口，返回 `Task<T>` / `Task<GetQueryPageResult<T>>`（全局包装，不出现 IResultModel）。
5. **Application/Product/ProductAppService.cs** — 实现类加 `IScopedDependency`（自动注册），注入 `IBaseRepository<Product>`，写查询/校验/CRUD；成功 `return dto`，业务失败 `throw new ParameterException(msg)`，资源不存在 `if (entity is null) throw new NotFoundException(msg)`；列表用 `WhereIfNotNullOrWhiteSpace` + `ToPageListAsync` + `GetQueryPageResult<T>`。`is null` 检查会被编译器原生流分析收窄，后续代码直接使用实体；不要写 `entity = entity!` 自赋值（CS1717）和 `var required = entity!` 中间变量（侵入方法体、可读性差），也不要依赖 `NotFoundException.ThrowIfNull`（包未标注 `[NotNull]`，不产生流分析收窄；若坚持使用，只能在成员访问处补 `entity!.Xxx` 断言）。
6. **Service/Controllers/ProductController.cs** — 注入 `IProductAppService`，每个 action 一行转发。无需手写 `AddScoped`（扫描注册自动完成）。

> 配置类由 DbContext 的 `ApplyConfigurationsFromAssembly(当前程序集)` 自动扫描应用；新增实体只需在 DbContext 加 `DbSet` + 写一个 `{Entity}Etc.cs` 配置类。如用 SQL 脚本迁移，在 `Service/wwwroot/MigrationSql/` 增版本脚本（详见 azrng-efcore skill）。

## 命名约定

| 类型 | 命名 | 位置 |
|---|---|---|
| 应用服务接口 | `I{Entity}AppService` | IApplication |
| 应用服务实现 | `{Entity}AppService` | Application |
| 控制器 | `{Entity}Controller` | Service/Controllers |
| 实体 | 单数 `{Entity}` | IDomain/Entities |
| 配置类 | `{Entity}Etc` | EntityFramework |
| DbContext | `{Solution}DbContext` | EntityFramework |

> **实体名与模块命名空间同名时必须消歧**：AppService 实现放 `HotelManagement.Application.User` 这类模块命名空间后，同文件内裸引用实体 `User` 会被解析为命名空间（CS0118）。消歧任选其一：① `using UserEntity = HotelManagement.IDomain.Entities.User;` 别名后用 `UserEntity`；② 模块名复数化（`Application.Users`），实体保持单数。推荐②，一劳永逸。

## 何时该拆/不该拆（决策速查）

只列**决策增量**——基础规则见上方「核心规则」，分页/全局包装写法见对应章节。

- **该不该新建项目？** 标准模板共 9 个项目：7 个核心分层（Core/IDomain/IApplication/Domain/Application/EntityFramework/Service）+ 2 个可选基础设施（Adapter 外部 API 封装、Storage 文件/对象存储）。只有引入全新基础设施（如 Redis、消息队列、第三方 SDK）才新增独立项目（如 `Cache`/`Notification`），并保持单向依赖。多数纯 CRUD 项目初期 Domain/Adapter/Storage 可空，出现对应需求（复用操作 / 外部调用 / 文件存储）再建。
- **读取用 `Entities` 还是 `EntitiesNoTacking`？** 只读查询默认 `EntitiesNoTacking`（不带追踪、更快）；读出来还要改并回写才用 `Entities`。
- **事务该用 `IUnitOfWork` 还是直接注入 DbContext？** 多步原子写优先用 `IUnitOfWork.CommitTransactionAsync(async () => {...})`（自动开/提交/回滚）。只有需要原生 SQL / `FOR UPDATE` 行锁（如并发扣库存/抢购）才让 AppService 注入 DbContext 用 `BeginTransactionAsync()` + `FromSqlInterpolated`——这是 DbContext 跨层注入的少数合理场景。
- **控制器能不能调多个 AppService？** 可以，但要警惕——若一个请求要协调多个服务完成一个事务，说明该把编排逻辑下沉到一个 AppService（用 `IUnitOfWork` 包事务），控制器只调这一个。

## 依赖注入与中间件装配（Startup）

AppService 用**约定式自动扫描注册**：实现类实现 `Azrng.Core.DependencyInjection.IScopedDependency`（或 `ITransientDependency`/`ISingletonDependency`），再调一次 `RegisterBusinessServices` 扫描程序集，按生命周期接口自动注册。将所有服务注册放入 `Startup.RegisterServices(IServiceCollection)`，将所有中间件放入 `Startup.SetupMiddlewares(WebApplication)`；完整模板见上方「全局返回包装 → Startup 装配」。

```csharp
builder.Services.AddEntityFramework<AppDbContext>(o =>
{
    o.ConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
    o.Schema = "public";
});

// 扫描匹配程序集，把实现 IScope/Transient/SingletonDependency 的类自动注册
builder.Services.RegisterBusinessServices("App.*.dll");
```

AppService 实现类标接口即可被扫到（无需手写 `AddScoped`）：
```csharp
using Azrng.Core.DependencyInjection;

public class ProductAppService : IScopedDependency, IProductAppService  // ← IScopedDependency 触发自动注册
{ /* ... */ }
```

> 早期代码里逐个 `AddScoped<IXxx, Xxx>()` 的写法仍在（迁移过渡期），新代码用 `IScopedDependency` + 扫描注册，避免漏注册。控制器由 `AddControllers()` 自动发现，无需注册。
> **Service.csproj 需引用的包**：`Azrng.AspNetCore.Core`（三件套 + `RegisterBusinessServices`）、对应的 `Common.EFCore.*` provider 包（如 `Common.EFCore.PostgresSql`）、按需 `Azrng.SqlMigration`（SQL 迁移）/ `Azrng.Swashbuckle`（Swagger）/ `Azrng.AspNetCore.Authentication.JwtBearer`（JWT）/ `Azrng.Core.Json`（`ConfigureDefaultJson` + `IJsonSerializer`）/ `DotNetEnv`（仅本地 `.env` 开发配置）。如果代码直接写 `new NpgsqlConnection(...)`，Service 还应显式引用 `Npgsql`；不要依赖 provider 的传递引用。

## 常见反模式（最易踩）

| 反模式 | 正确做法 |
|---|---|
| 控制器注入 `IBaseRepository<T>` 写查询 / 写业务校验 | 查询和校验移到 AppService，控制器只一行转发 |
| 列表查询手写 `CountAsync()` + `Skip().Take()` + 自定义 `{List,Total}` 类 | 用分页四件套（`ToPageListAsync` + `GetQueryPageResult<T>`） |
| 实体直接作为 API 返回值 | AppService 映射成 Core 的 DTO 再返回；实体只在 AppService↔DbContext 间流动 |
| 新功能不建接口直接写实现类 | 先 `I*AppService` 接口，控制器依赖接口 |
| 管道写 `app.UseCors()` 但未注册 CORS 服务 | 先 `AddCorsByOrigins`（白名单）/ `AddAnyCors`（仅开发）注册，再 `app.UseCorsPolicy()` |

> 遇到存量项目未遵循上述约定（如用 Newtonsoft、自研 ResultFilter、`QueryableWhereIf`），按现有代码风格走、不强行改写可工作代码；新增功能才按本指南。

## 深入参考

- **各层职责详细对照 + 跨层数据流 + 完整代码示例（全局包装 / 分页写法）**：见 references/layer-responsibilities.md
- **数据访问（仓储/工作单元/实体基类/配置类/DbContext + 分页条件扩展 API）细节**：用 azrng-efcore skill
