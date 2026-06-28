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
namespace App.Core.Models;

public class ProductDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Status { get; set; }
    public DateTime CreateTime { get; set; }
}

public class ProductListRequest
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

namespace App.IDomain.Entities;

public class Product : IdentityOperatorStatusEntity   // Id + 审计 + 软删除
{
    public Product()
    {
        Id = IdHelper.GetLongId();
        SetCreator("system");
    }

    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Status { get; set; } = 1;
}
```

配置类示例：
```csharp
using Azrng.EFCore.EntityTypeConfigurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.IDomain.EntityTypeConfigurations;

public class ProductConfiguration : EntityTypeConfigurationIdentityOperatorStatus<Product>
{
    public override void Configure(EntityTypeBuilder<Product> builder)
    {
        base.Configure(builder);   // 必调：主键/审计列/表名/时间类型
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired().HasComment("商品名称");
        builder.Property(x => x.Price).HasComment("价格");
    }
}
```

### IApplication（应用契约层）
**放**：`I*AppService` 接口，方法签名用 Core 的 DTO。是控制器唯一依赖的契约。
**不放**：实现、仓储、实体直接暴露（接口参数/返回一律 DTO）。
**依赖**：Core、IDomain。

```csharp
using Azrng.Core.Results;
using App.Core.Models;

namespace App.IApplication.Product;

public interface IProductAppService
{
    Task<IResultModel<ProductListResponse>> GetListAsync(ProductListRequest request);
    Task<IResultModel<ProductDto>> CreateAsync(CreateProductRequest request);
    Task<IResultModel> DeleteAsync(long id);
}
```

### Application（应用实现层）
**放**：`*AppService` 实现，注入 `IBaseRepository<T>` / `IUnitOfWork`，承担**全部数据访问与业务逻辑**（校验、编排、事务、实体↔DTO 映射）。实现类标 `IScopedDependency` 触发自动注册。
**不放**：HTTP 概念、`HttpContext`、`Ok()`、路由。
**依赖**：IApplication（+ 间接 Domain/EntityFramework）。
**按需引包**（只有 AppService 真用到时才加，引在 Application.csproj）：
- `Azrng.Core` — `ResultModel`/`IResultModel`、`IScopedDependency`、`ToUnspecifiedDateTime()`、`NotFoundException`（几乎总要）
- `Azrng.AspNetCore.Authentication.JwtBearer` — 登录/鉴权 AppService 注入 `IBearerAuthService` 生成 token
- `Azrng.DistributeLock.Core` — 并发控制 AppService 注入 `ILockProvider`
- `Microsoft.EntityFrameworkCore`（经 `Common.EFCore` 传递可达）— `CountAsync`/`ToListAsync`/`Skip`/`Take` 等异步扩展

```csharp
using Azrng.Core.DependencyInjection;   // IScopedDependency
using Azrng.Core.Results;                // ResultModel<T>、IResultModel<T>
using Azrng.EFCore;
using Microsoft.EntityFrameworkCore;
using App.Core.Models;
using App.IApplication.Product;
using App.IDomain.Entities;

namespace App.Application.Product;

// IScopedDependency 触发约定式自动注册（RegisterBusinessServices 扫描），无需手写 AddScoped
public class ProductAppService : IScopedDependency, IProductAppService
{
    private readonly IBaseRepository<Product> _productRepository;

    public ProductAppService(IBaseRepository<Product> productRepository)
        => _productRepository = productRepository;

    public async Task<IResultModel<ProductListResponse>> GetListAsync(ProductListRequest request)
    {
        // 查询用 EntitiesNoTacking（只读、不加 ChangeTracker，更快）
        var query = _productRepository.EntitiesNoTacking.Where(p => !p.Deleted);
        if (!string.IsNullOrWhiteSpace(request.Name))
            query = query.Where(p => p.Name.Contains(request.Name!));

        var total = await query.CountAsync();
        var list = await query.OrderByDescending(p => p.CreateTime)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return ResultModel<ProductListResponse>.Success(new ProductListResponse
        {
            // 实体 → DTO 映射（不暴露审计字段给前端）
            List = list.Select(p => new ProductDto
            {
                Id = p.Id, Name = p.Name, Price = p.Price,
                Status = p.Status, CreateTime = p.CreateTime
            }).ToList(),
            Total = total
        });
    }

    public async Task<IResultModel<ProductDto>> CreateAsync(CreateProductRequest request)
    {
        // 业务校验下沉到此处，不在控制器；失败返 ResultModel<T>.Error
        if (request.Price < 0)
            return ResultModel<ProductDto>.Error("价格不能为负", "400");

        var product = new Product { Name = request.Name, Price = request.Price };
        // 写操作用 AddAsync(entity, submit: true) 立即提交
        await _productRepository.AddAsync(product, submit: true);
        return ResultModel<ProductDto>.Success(
            new ProductDto { Id = product.Id, Name = product.Name, Price = product.Price, Status = product.Status });
    }

    public async Task<IResultModel> DeleteAsync(long id)
    {
        await _productRepository.DeleteAsync(p => p.Id == id);
        return ResultModel.Success();
    }
}
```

#### 读取追踪 vs 不追踪（`Entities` / `EntitiesNoTacking`）
- **`EntitiesNoTacking`（默认首选）**：纯查询、列表、详情读取。不进 ChangeTracker，性能好。90% 的查询用这个。
- **`Entities`（带追踪）**：读出来后**还要修改并回写**时才用——先查、改属性、`UpdateAsync` 提交，EF 靠追踪算 diff。读了不改却用 `Entities` 会白白吃追踪开销。

```csharp
// 查询列表 → NoTracking
var list = await _repo.EntitiesNoTacking.Where(x => !x.Deleted).ToListAsync();

// 读出来要改 → 带追踪（或用 GetByIdAsync 后 UpdateAsync）
var entity = await _repo.Entities.FirstOrDefaultAsync(x => x.Id == id);
entity.Status = 0;
await _repo.UpdateAsync(entity, submit: true);
```

#### 返回类型与结果模型（`IResultModel<T>`）
AppService 方法返回统一用结果模型，**首选泛型 `Task<IResultModel<T>>`**：成功 `ResultModel<T>.Success(dto)`、失败 `ResultModel<T>.Error(msg, code)`。这样错误信息进统一结果体，类型安全，前端结构一致，也无需全局过滤器二次包裹（`IResultModel` 本身即对外契约）。

```csharp
// 首选：带数据的成功/失败 → Task<IResultModel<T>>
public Task<IResultModel<LoginResultDto>> LoginAsync(LoginRequest request)
{
    var user = /* ...查库... */;
    if (user == null)
        return ResultModel<LoginResultDto>.Error("用户名或密码错误", "400");
    // ... 校验密码、生成 token ...
    return ResultModel<LoginResultDto>.Success(new LoginResultDto { /* ... */ });
}

// 无返回数据的操作（删除/重置）→ Task<IResultModel>，成功 ResultModel.Success()
public Task<IResultModel> DeleteAsync(long id) { /* ... */ return ResultModel.Success(); }
```

**过渡期可容忍、但新代码别再用**的两种旧写法（项目历史代码里有，迁移时逐步替换）：
- `Task<object>`：成功返 DTO、失败返 `ResultModel.Error`，丢失泛型与类型信息。
- 纯成功路径直接返 `Task<T>`：无错误通道，靠全局 `ResultPackFilter` 包装（过滤器存在时才成立）。

> **别用抛异常表达"业务校验失败"**（异常是给意外错误用的）。但"资源确实不存在"（如按 id 查记录拿不到）属合理的异常路径，用框架异常：`Azrng.Core.Exceptions.NotFoundException.ThrowIfNull(entity, "记录不存在")`——它有明确语义，全局异常处理可映射成 404。

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

> 需要原生 SQL / `FOR UPDATE` 行锁时（如并发扣库存/抢购），AppService 注入 DbContext 直接用 `_dbContext.Database.BeginTransactionAsync()` + `FromSqlInterpolated`。这是少数 DbContext 可跨层注入 AppService 的合理场景。

### EntityFramework（基础设施-数据）
**放**：`DbContext`（继承 `Azrng.EFCore.PostgresSql.BaseDbContext`，自动扫描 IDomain 的配置类）。
**不放**：业务逻辑、实体定义（实体在 IDomain）、配置类（配置在 IDomain）。
**依赖**：Core、IDomain；包引用 `Common.EFCore` + provider 包。

```csharp
using Azrng.EFCore.PostgresSql;
using Microsoft.EntityFrameworkCore;
using App.IDomain.Entities;

namespace App.EntityFramework;

public class AppDbContext : BaseDbContext   // 自动 ApplyConfigurationsFromAssembly
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<Product> Products { get; set; }
}
```

### Service（入口/组装层）
**放**：`Controllers`、`Program.cs`（DI 装配 + 中间件管道）、`appsettings`、迁移 SQL 脚本、安全/过滤器。
**不放**：EF 查询、业务校验、`IBaseRepository` 注入。
**依赖**：Application（+ Storage/EntityFramework 经传递可达）。

控制器（瘦）——直接转发 AppService 返回的 `IResultModel<T>`，不包 `Ok()`：
```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Azrng.Core.Results;
using App.Core.Models;
using App.IApplication.Product;

namespace App.Service.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ProductController(IProductAppService productAppService) : ControllerBase
{
    [HttpGet("list")]
    public Task<IResultModel<ProductListResponse>> GetList([FromQuery] ProductListRequest request)
        => productAppService.GetListAsync(request);   // 一行转发

    [HttpPost]
    public Task<IResultModel<ProductDto>> Create([FromBody] CreateProductRequest request)
        => productAppService.CreateAsync(request);

    [HttpDelete("{id}")]
    public Task<IResultModel> Delete(long id)
        => productAppService.DeleteAsync(id);
}
```

> 控制器直接转发 AppService 返回的 `IResultModel<T>`，无需 `Ok()` 包装——`IResultModel` 本身就是对外的统一结果契约（含 isSuccess/code/data）。错误路径在 AppService 里 `return ResultModel<T>.Error(msg, code)`。

---

## 2. 跨层数据流（一个 GET 请求的旅程）

```
HTTP GET /api/product/list?name=xx
  │
  ▼
[Service] ProductController.GetList(ProductListRequest)   ← 仅接参数
  │  调用接口
  ▼
[Application] ProductAppService.GetListAsync              ← 业务+查询在这里
  │  注入 IBaseRepository<Product>
  ▼
[EntityFramework] IBaseRepository → DbContext → PostgreSQL   ← 基础设施
  │  返回 Product 实体（带审计字段）
  ▼
[Application] 实体 → ProductDto 映射（剥掉审计字段）   ← 边界转换
  │  返回 IResultModel<ProductListResponse>（ResultModel<T>.Success(...)）
  ▼
[Service] 控制器 return IResultModel<ProductListResponse>（一行转发）
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

对应 `<ProjectReference>` 示例（Application.csproj，`App` 替换成实际解决方案名）：
```xml
<ItemGroup>
  <ProjectReference Include="..\App.IApplication\App.IApplication.csproj" />
  <ProjectReference Include="..\App.Domain\App.Domain.csproj" />
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
| 5 | `Application/Order/OrderAppService.cs` | Application | 实现 `IScopedDependency` + 仓储 + 业务（自动注册，免手写 AddScoped） |
| 6 | `Service/Controllers/OrderController.cs` | Service | 瘦控制器，转发 `IResultModel<T>` |
| 7 | `Service/Program.cs` | Service | 已有 `RegisterBusinessServices("App.*.dll")`，实现 `IScopedDependency` 即自动注册，无需改 |

DbContext 无需改动（`BaseDbContext` 自动扫描新配置）。若用 SQL 脚本迁移，加 `Service/MigrationSql/x.x.x.sql`。
