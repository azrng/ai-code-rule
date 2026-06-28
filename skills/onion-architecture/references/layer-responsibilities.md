# 分层职责详解与代码示例

## 目录
1. 各层职责对照
2. 跨层数据流（一个请求的完整旅程）
3. 各层完整代码示例
4. .csproj 依赖关系

---

## 1. 各层职责对照

### Core（内核，零业务依赖）
**放**：DTO、Request/Response、枚举、常量、通用结果类型。纯 POCO，不引用任何项目，不引用 EFCore/HttpContext 等基础设施。
**不放**：实体、业务逻辑、数据访问、任何 `using` 基础设施包。
**包引用**：仅 `Azrng.Core`（取 `ResultModel` 等）等基础类库。

```csharp
namespace MovieManage.Core.Models;

public class MovieDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Status { get; set; }
    public DateTime CreateTime { get; set; }
}

public class MovieListRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Name { get; set; }
}
```

### IDomain（领域契约层）
**放**：实体（继承 `IdentityOperatorStatusEntity`）、EFCore 配置类（`IEntityTypeConfiguration<T>`）。
**不放**：DbContext、业务逻辑、DTO。
**包引用**：`Common.EFCore`（取基类 + 配置基类 + `IdHelper`）。

实体示例：
```csharp
using Azrng.EFCore.Entities;
using Coldairarrow.Util;

namespace MovieManage.IDomain.Entities;

public class Movie : IdentityOperatorStatusEntity   // Id + 审计 + 软删除
{
    public Movie()
    {
        Id = IdHelper.GetLongId();
        SetCreator("system");
    }

    public string Name { get; set; } = string.Empty;
    public long CategoryId { get; set; }
    public decimal Price { get; set; }
    public int Status { get; set; } = 1;
}
```

配置类示例：
```csharp
using Azrng.EFCore.EntityTypeConfigurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MovieManage.IDomain.EntityTypeConfigurations;

public class MovieConfiguration : EntityTypeConfigurationIdentityOperatorStatus<Movie>
{
    public override void Configure(EntityTypeBuilder<Movie> builder)
    {
        base.Configure(builder);   // 必调：主键/审计列/表名/时间类型
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired().HasComment("电影名称");
        builder.Property(x => x.Price).HasComment("票价");
    }
}
```

### IApplication（应用契约层）
**放**：`I*AppService` 接口，方法签名用 Core 的 DTO。是控制器唯一依赖的契约。
**不放**：实现、仓储、实体直接暴露（接口参数/返回一律 DTO）。
**依赖**：Core、IDomain。

```csharp
using MovieManage.Core.Models;

namespace MovieManage.IApplication.MovieManager;

public interface IMovieAppService
{
    Task<MovieListResponse> GetListAsync(MovieListRequest request);
    Task<MovieDto> CreateAsync(CreateMovieRequest request);
    Task DeleteAsync(long id);
}
```

### Application（应用实现层）
**放**：`*AppService` 实现，注入 `IBaseRepository<T>` / `IUnitOfWork`，承担**全部数据访问与业务逻辑**（校验、编排、事务、实体↔DTO 映射）。
**不放**：HTTP 概念、`HttpContext`、`Ok()`、路由。
**依赖**：IApplication（+ 间接 Domain/EntityFramework）。

```csharp
using Azrng.Core.Results;
using Azrng.EFCore;
using Microsoft.EntityFrameworkCore;
using MovieManage.Core.Models;
using MovieManage.IApplication.MovieManager;
using MovieManage.IDomain.Entities;

namespace MovieManage.Application.MovieManager;

public class MovieAppService : IMovieAppService
{
    private readonly IBaseRepository<Movie> _movieRepository;

    public MovieAppService(IBaseRepository<Movie> movieRepository)
        => _movieRepository = movieRepository;

    public async Task<MovieListResponse> GetListAsync(MovieListRequest request)
    {
        var query = _movieRepository.EntitiesNoTacking.Where(m => !m.Deleted);
        if (!string.IsNullOrWhiteSpace(request.Name))
            query = query.Where(m => m.Name.Contains(request.Name!));

        var total = await query.CountAsync();
        var list = await query.OrderByDescending(m => m.CreateTime)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return new MovieListResponse
        {
            // 实体 → DTO 映射（不暴露审计字段给前端）
            List = list.Select(m => new MovieDto
            {
                Id = m.Id, Name = m.Name, Price = m.Price,
                Status = m.Status, CreateTime = m.CreateTime
            }).ToList(),
            Total = total
        };
    }

    public async Task<MovieDto> CreateAsync(CreateMovieRequest request)
    {
        // 业务校验下沉到此处，不在控制器
        var movie = new Movie { Name = request.Name, Price = request.Price };
        await _movieRepository.AddAsync(movie, submit: true);
        return new MovieDto { Id = movie.Id, Name = movie.Name, Price = movie.Price, Status = movie.Status };
    }

    public async Task DeleteAsync(long id) => await _movieRepository.DeleteAsync(m => m.Id == id);
}
```

需要多步原子写时用 `IUnitOfWork.CommitTransactionAsync`（自动开/提交/回滚事务，传入业务委托）：
```csharp
// 注入 IUnitOfWork；委托内各 AddAsync(..., submit: false) 不单独提交
await _unitOfWork.CommitTransactionAsync(async () =>
{
    await _orderRep.AddAsync(order);          // submit 默认 false
    await _itemRep.AddRangeAsync(items);
});
// 第二个参数可传 IsolationLevel（默认 ReadCommitted）；委托抛异常自动回滚
```

> 需要原生 SQL / `FOR UPDATE` 行锁时（如并发购票），AppService 注入 DbContext 直接用 `_dbContext.Database.BeginTransactionAsync()` + `FromSqlInterpolated`。这是少数 DbContext 可跨层注入 AppService 的合理场景。

### EntityFramework（基础设施-数据）
**放**：`DbContext`（继承 `Azrng.EFCore.PostgresSql.BaseDbContext`，自动扫描 IDomain 的配置类）。
**不放**：业务逻辑、实体定义（实体在 IDomain）、配置类（配置在 IDomain）。
**依赖**：Core、IDomain；包引用 `Common.EFCore` + provider 包。

```csharp
using Azrng.EFCore.PostgresSql;
using Microsoft.EntityFrameworkCore;
using MovieManage.IDomain.Entities;

namespace MovieManage.EntityFramework;

public class MovieManageDbContext : BaseDbContext   // 自动 ApplyConfigurationsFromAssembly
{
    public MovieManageDbContext(DbContextOptions<MovieManageDbContext> options) : base(options) { }
    public DbSet<Movie> Movies { get; set; }
    public DbSet<Category> Categories { get; set; }
}
```

### Service（入口/组装层）
**放**：`Controllers`、`Program.cs`（DI 装配 + 中间件管道）、`appsettings`、迁移 SQL 脚本、安全/过滤器。
**不放**：EF 查询、业务校验、`IBaseRepository` 注入。
**依赖**：Application（+ Storage/EntityFramework 经传递可达）。

控制器（瘦）：
```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieManage.Core.Models;
using MovieManage.IApplication.MovieManager;

namespace MovieManage.Service.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class MovieController(IMovieAppService movieAppService) : ControllerBase
{
    [HttpGet("list")]
    public Task<MovieListResponse> GetList([FromQuery] MovieListRequest request)
        => movieAppService.GetListAsync(request);   // 一行转发，不包 Ok()

    [HttpPost]
    public Task<MovieDto> Create([FromBody] CreateMovieRequest request)
        => movieAppService.CreateAsync(request);

    [HttpDelete("{id}")]
    public Task Delete(long id)
        => movieAppService.DeleteAsync(id);
}
```

> 返回值由全局 `AddMvcResultPackFilter` 自动包成 `ResultModel<T>`，控制器直接 `return 业务对象`。错误路径在 AppService 里 `return ResultModel.Error(msg, code)`。

---

## 2. 跨层数据流（一个 GET 请求的旅程）

```
HTTP GET /api/movie/list?name=xx
  │
  ▼
[Service] MovieController.GetList(MovieListRequest)   ← 仅接参数
  │  调用接口
  ▼
[Application] MovieAppService.GetListAsync            ← 业务+查询在这里
  │  注入 IBaseRepository<Movie>
  ▼
[EntityFramework] IBaseRepository → DbContext → PostgreSQL   ← 基础设施
  │  返回 Movie 实体（带审计字段）
  ▼
[Application] 实体 → MovieDto 映射（剥掉审计字段）   ← 边界转换
  │  返回 MovieListResponse（Core 的 DTO）
  ▼
[Service] 控制器 return MovieListResponse
  │  ResultPackFilter 自动包成 ResultModel<MovieListResponse>
  ▼
HTTP 200 { isSuccess, code, data: { list, total } }
```

**关键边界**：实体只在 `AppService ↔ DbContext` 之间流动；**出了 AppService 一律是 DTO**。控制器和前端永远看不到实体。

---

## 3. .csproj 依赖关系

```
Core              → (无项目引用)
IDomain           → Core
IApplication      → Core, IDomain
Domain            → IDomain, EntityFramework, Adapter    ← 可选，多数项目空
Application       → IApplication, Domain
EntityFramework   → Core, IDomain
Service           → Application, Storage
Adapter           → Core, IDomain                         ← 可选
Storage           → Core                                   ← 可选
```

对应 `<ProjectReference>` 示例（Application.csproj）：
```xml
<ItemGroup>
  <ProjectReference Include="..\MovieManage.IApplication\MovieManage.IApplication.csproj" />
  <ProjectReference Include="..\MovieManage.Domain\MovieManage.Domain.csproj" />
</ItemGroup>
<ItemGroup>
  <PackageReference Include="Azrng.Core" Version="1.19.0" />
  <!-- 仅当 AppService 需要时才引：JWT 认证、分布式锁等 -->
</ItemGroup>
```

**依赖方向自检**：若发现内层项目（如 Core）要 `using` 外层（如 Application），说明分层错了，立刻停下来纠正。

---

## 4. 完整功能落点速查（新增"订单"功能）

| 步骤 | 文件 | 层 | 内容 |
|---|---|---|---|
| 1 | `Core/Models/OrderDto.cs` | Core | `OrderDto`/`OrderListRequest`/`CreateOrderRequest` |
| 2 | `IDomain/Entities/Order.cs` | IDomain | 实体 + `SetCreator` |
| 3 | `IDomain/EntityTypeConfigurations/OrderConfiguration.cs` | IDomain | 配置类 |
| 4 | `IApplication/Order/IOrderAppService.cs` | IApplication | 接口 |
| 5 | `Application/Order/OrderAppService.cs` | Application | 实现 + 仓储 + 业务 |
| 6 | `Service/Controllers/OrderController.cs` | Service | 瘦控制器 |
| 7 | `Service/Program.cs` | Service | `AddScoped<IOrderAppService, OrderAppService>()` |

DbContext 无需改动（`BaseDbContext` 自动扫描新配置）。若用 SQL 脚本迁移，加 `Service/MigrationSql/x.x.x.sql`。
