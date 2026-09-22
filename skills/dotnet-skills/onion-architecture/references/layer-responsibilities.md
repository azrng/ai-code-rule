# 分层职责详解与代码示例

## 目录
1. 解决方案结构
2. 各层职责对照
3. 跨层数据流（一个请求的完整旅程）
4. 各层完整代码示例
5. .csproj 依赖关系

---

## 0. 解决方案结构

`.slnx` 放在仓库根目录，业务项目放在 `src/`，测试项目放在 `test/`。解决方案使用相对路径引用项目，并按 `/src/`、`/test/` 两个目录节点组织；没有测试项目时保留空的 `/test/` 节点即可。

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
│   └── YourProject.Service/
└── test/
    └── YourProject.Tests/
```

```xml
<Solution>
  <Folder Name="/src/">
    <Project Path="src/YourProject.Core/YourProject.Core.csproj" />
    <Project Path="src/YourProject.Service/YourProject.Service.csproj" />
  </Folder>
  <Folder Name="/test/" />
</Solution>
```

新增测试项目时，项目路径必须位于 `test/` 下，并加入 `.slnx` 的 `/test/` 节点；不要把测试项目放回 `src/`。

---

## 1. 各层职责对照

### Core（内核，零业务依赖）
**放**：**跨业务域复用**的 DTO、枚举、常量、通用结果类型（如公共枚举、跨域包装、分页基类衍生参数）。纯 POCO，不引用任何项目，不引用 EFCore/HttpContext 等基础设施。**单业务域的请求/响应 DTO 不放这里**——放 `IApplication/{业务域}/Dto/`（见下文）；「接口、实现、控制器都用」是应用层 DTO 的常态，不构成放 Core 的判据（判据详见 SKILL.md 核心规则 4）。
**不放**：实体、业务逻辑、数据访问、单业务域 DTO、任何 `using` 基础设施包。
**包引用**：仅 `Azrng.Core`（取 `GetPageRequest`/`ResultModel` 等）等基础类库。

```csharp
namespace App.Core.Models;

// Core/Models/OrderStatus.cs —— 被多个业务域引用的公共枚举才放 Core
public enum OrderStatus
{
    Pending = 0,
    Paid = 1,
    Shipped = 2
}
```

公开契约类一类一个文件（文件名与类名一致）。单业务域的 DTO/Request/Response 落位示例见下方 IApplication 节。

### IDomain（领域契约层）
**放**：实体（继承 `IdentityOperatorStatusEntity`）。**EFCore 配置类不在这层——放 EntityFramework 层**（见下）。
**不放**：DbContext、业务逻辑、DTO、EFCore 配置类。
**包引用**：`Common.EFCore`（取实体基类 + `IdHelper`）。

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

#### 外部存储契约

文件或对象存储属于可替换的外部适配能力。面向业务的接口放在 `IDomain`（例如 `IDomain/Storage/IMaterialStorage.cs`），接口只表达保存、读取、删除等领域语义，不出现 `IFormFile`、`HttpContext` 或具体磁盘路径；本地磁盘、对象存储等实现放在 `Storage` 项目。

```csharp
// 文件：IDomain/Storage/IMaterialStorage.cs
namespace App.IDomain.Storage;

public interface IMaterialStorage
{
    string GenerateStorageName(string extension);
    Task<string> SaveAsync(string storageName, Stream content);
    Task<Stream?> OpenReadAsync(string storageName);
    Task DeleteAsync(string storageName);
}
```

`Storage.csproj` 依赖 `Core + IDomain`，Service 组合根注册 `IMaterialStorage` 的具体实现；Application 只依赖 `IDomain` 中的接口。

### IApplication（应用契约层）
**放**：`I*AppService` 接口 + 本业务域的 DTO/Request/Response（`{业务域}/Dto/`，一类一文件）。是控制器唯一依赖的契约，接口签名只出现本业务域 DTO 或跨业务域通用 DTO。
**不放**：实现、仓储、实体直接暴露（接口参数/返回一律 DTO）。
**依赖**：Core、IDomain。
**返回类型**：**全局包装模式**——方法返回 `Task<T>` / `Task<GetQueryPageResult<T>>`，**不出现 `IResultModel`**。

```csharp
// 文件：IApplication/Product/Dto/ProductDto.cs —— 单业务域 DTO 放 IApplication，不放 Core/Models
namespace App.IApplication.Product.Dto;

public class ProductDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Status { get; set; }
    public DateTime CreateTime { get; set; }
}
```

```csharp
// 文件：IApplication/Product/Dto/ProductListRequest.cs
using Azrng.Core.Requests;   // GetPageRequest

namespace App.IApplication.Product.Dto;

// 分页请求：继承 GetPageRequest，免手写 PageIndex/PageSize 字段
public class ProductListRequest : GetPageRequest
{
    public string? Name { get; set; }
}
```

```csharp
// 文件：IApplication/Product/Dto/CreateProductRequest.cs
namespace App.IApplication.Product.Dto;

public class CreateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
```

```csharp
// 文件：IApplication/Product/IProductAppService.cs
using Azrng.Core.Results;            // GetQueryPageResult<T>
using App.IApplication.Product.Dto;

namespace App.IApplication.Product;

public interface IProductAppService
{
    Task<GetQueryPageResult<ProductDto>> GetListAsync(ProductListRequest request);  // 分页查询
    Task<ProductDto> GetAsync(long id);          // 成功返 DTO，资源不存在由实现抛 NotFoundException
    Task<long> CreateAsync(CreateProductRequest request);   // 成功返 id，校验失败由实现抛 ParameterException
    Task DeleteAsync(long id);                    // 无返回数据
}
```

### Application（应用实现层）
**放**：`*AppService` 实现，承担**用例编排**（接 DTO → 调领域服务/仓储/适配器 → 返回 DTO）、实体↔DTO 映射、事务编排。**可注入 `IBaseRepository<T>` / `IUnitOfWork` 做数据访问**（并非 AppService 独占——DomainService 也会注入仓储）。实现类标 `IScopedDependency` 触发自动注册。
**不放**：HTTP 概念、`HttpContext`、`Ok()`、路由。
**依赖**：IApplication（+ 间接 Domain/EntityFramework）。
**包引用**：核心包经依赖链**传递可达**，多数无需在 Application.csproj 显式声明：
- **传递可达**（经 `Application → IApplication → IDomain → Common.EFCore → Azrng.Core / Microsoft.EntityFrameworkCore`，无需显式引）：`IBaseRepository`/`IUnitOfWork`/`ToPageListAsync`（Common.EFCore）、`FirstOrDefaultAsync`/`CountAsync`/`ToListAsync`（Microsoft.EntityFrameworkCore）、`IScopedDependency`/`ParameterException`/`NotFoundException`/`WhereIfNotNullOrWhiteSpace`/`GetQueryPageResult`/`RefAsync`/`ToUnspecifiedDateTime()`（Azrng.Core）。
  > 实测 `MovieManage.Application` 只显式引 `Azrng.Core`（可选，为注释清晰）、`JwtBearer`、`DistributeLock.Core`；synyi 的 Application 甚至一个 Azrng 包都不显式引（全靠传递）。
- **按需显式引**（独立功能包，不在传递链，AppService 真用到才加）：
  - `Azrng.AspNetCore.Authentication.JwtBearer` — 登录/鉴权 AppService 注入 `IBearerAuthService` 生成 token
  - `Azrng.DistributeLock.Core` — 并发控制 AppService 注入 `ILockProvider`

**核心约定（全局包装模式）**：成功直接 `return` 业务对象；**业务校验/规则失败 `throw new ParameterException(msg)`**；资源不存在 `if (entity is null) throw new NotFoundException("...")`。**不要手写 `ResultModel.Success/Error`**（那是历史写法）。

```csharp
using Azrng.Core.CommonDto;            // RefAsync
using Azrng.Core.DependencyInjection;  // IScopedDependency
using Azrng.Core.Exceptions;           // ParameterException, NotFoundException
using Azrng.Core.Extension;            // WhereIfNotNullOrWhiteSpace
using Azrng.Core.Results;              // GetQueryPageResult
using Azrng.EFCore;                    // IBaseRepository
using Azrng.EFCore.Extensions;         // ToPageListAsync
using Microsoft.EntityFrameworkCore;
using App.IApplication.Product;
using App.IApplication.Product.Dto;
using App.IDomain.Entities;

namespace App.Application.Product;

// IScopedDependency 触发约定式自动注册（RegisterBusinessServices 扫描），无需手写 AddScoped
public class ProductAppService : IScopedDependency, IProductAppService
{
    private readonly IBaseRepository<Product> _productRepository;

    public ProductAppService(IBaseRepository<Product> productRepository)
        => _productRepository = productRepository;

    // 分页查询：链式 WhereIfNotNullOrWhiteSpace + ToPageListAsync（一次 Count+分页），返回 GetQueryPageResult<Dto>
    public async Task<GetQueryPageResult<ProductDto>> GetListAsync(ProductListRequest request)
    {
        var total = new RefAsync<int>();
        var list = await _productRepository.EntitiesNoTacking.Where(p => !p.Deleted)
            .WhereIfNotNullOrWhiteSpace(request.Name, p => p.Name.Contains(request.Name!))
            .OrderByDescending(p => p.CreateTime)
            .Select(p => new ProductDto
            {
                Id = p.Id, Name = p.Name, Price = p.Price,
                Status = p.Status, CreateTime = p.CreateTime
            })
            .ToPageListAsync(request, total);

        return new GetQueryPageResult<ProductDto>(list, new GetQueryPageResult(request, total));
    }

    public async Task<ProductDto> GetAsync(long id)
    {
        var product = await _productRepository.EntitiesNoTacking.FirstOrDefaultAsync(p => p.Id == id);
        if (product is null)                          // 资源不存在 → 由异常中间件映射成 404；is null 检查原生收窄可空流分析
            throw new NotFoundException("商品不存在");
        return new ProductDto { Id = product.Id, Name = product.Name, Price = product.Price, Status = product.Status };
    }

    public async Task<long> CreateAsync(CreateProductRequest request)
    {
        // 业务校验下沉到此处，不在控制器；失败抛 ParameterException → 由异常中间件映射成 400
        if (request.Price < 0)
            throw new ParameterException("价格不能为负");

        var product = new Product { Name = request.Name, Price = request.Price };
        await _productRepository.AddAsync(product, submit: true);   // submit:true 立即提交
        return product.Id;   // 成功 → 全局包装过滤器自动包成 ResultModel<long>(success)
    }

    public async Task DeleteAsync(long id)
    {
        await _productRepository.DeleteAsync(p => p.Id == id);   // 无返回值 → 过滤器包成 ResultModel(true,"成功")
    }
}
```

#### 文件上传的 HTTP 适配

Application 不接受 ASP.NET 的 `IFormFile`。在 IApplication 业务域下定义只包含业务数据的 Command（`IApplication/Material/Dto/MaterialUploadCommand.cs`——仅 Material 业务域使用，非跨域通用，不放 Core/Models），由 Controller 把 HTTP 文件对象转换后调用 AppService：

```csharp
// 文件：IApplication/Material/Dto/MaterialUploadCommand.cs
namespace App.IApplication.Material.Dto;

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

#### 返回值与错误处理（全局包装模式）

AppService 方法**统一返回裸 `Task<T>`**（业务对象），由 `Service` 层装配的框架三件套自动包成统一 `ResultModel`。错误通过**抛业务异常**表达（不要返回错误码、不要手写 `ResultModel.Error`）：成功 `return dto;`、资源不存在 `if (entity is null) throw new NotFoundException("...")`（is null 检查原生收窄流分析，后续直接用实体，不要 `entity!` 断言或中间变量）、参数/业务校验失败 `throw new ParameterException(msg)`（也可是 `LogicBusinessException`）、无权限 `throw new ForbiddenException(msg)`。

> 业务异常 → 结果映射表、三件套装配、`IResultModel<T>` 迁移期说明见 `SKILL.md → 全局返回包装`。这套要工作，**异常中间件 `app.UseGlobalException()` 必须在 `Startup.SetupMiddlewares` 管道最外层装配**，否则抛出的异常会变成裸 500。

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

> 需要原生 SQL / `FOR UPDATE` 行锁时（如并发扣库存/抢购），AppService 注入 DbContext 直接用 `_dbContext.Database.BeginTransactionAsync()` + `FromSqlInterpolated`。这是少数 DbContext 可跨层注入 AppService 的合理场景。分页/条件查询扩展（`ToPageListAsync`/`WhereIfNotNullOrWhiteSpace`/`PagedBy` 等）完整签名见 azrng-efcore skill。

### Domain（领域服务层）
**放**：`*DomainService` 实现，封装**可复用的公共领域操作**——被多个 AppService 复用、或承载稳定的多实体领域规则/数据访问的操作（如「发会话消息」「读系统配置」「指标计算」）。**DomainService 可注入 `IBaseRepository<T>` / `IUnitOfWork` 自己读写数据**（Domain 项目依赖 EntityFramework），也可注入 Adapter/缓存等。实现类标 `IScopedDependency` 触发自动注册。
**不放**：HTTP 概念、DTO 映射（那是 AppService 的活）——DomainService 只返回/接收领域对象（Entity/Bo）。
**依赖**：IDomain、EntityFramework、Adapter。
**结对**：接口 `I{X}DomainService` 在 `IDomain/{Module}/`，实现 `{X}DomainService : IScopedDependency, I{X}DomainService` 在 `Domain/{Module}/`（与 AppService 的 IApplication/Application 结对方式一致）。**AppService 注入 `I{X}DomainService` 调用它**。

```csharp
// 文件：IDomain/SessionManager/Bo/SendSessionMessageBo.cs —— DomainService 接口签名用的业务对象 Bo，统一放 IDomain/{模块}/Bo/（Adapter 的 Bo 例外，在 IDomain/Adapter/{模块}/）
namespace App.IDomain.SessionManager.Bo;

public class SendSessionMessageBo
{
    public string SessionName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

// IDomain/SessionManager/ISessionDomainService.cs —— 接口，返回领域对象（Entity/Bo），不出现 DTO
public interface ISessionDomainService
{
    Task<string> SendSessionMessageAsync(SendSessionMessageBo dto);
    Task<ChatMemoryEntity> GetChatMemoryAsync(string messageId);
}

// Domain/SessionManager/SessionDomainService.cs —— 实现：注入多个仓储，做跨实体的领域编排（自己写库）
public class SessionDomainService : IScopedDependency, ISessionDomainService
{
    private readonly IBaseRepository<ChatSessionEntity> _sessionRepository;
    private readonly IBaseRepository<ChatMessageEntity> _chatMessageRepository;
    public SessionDomainService(IBaseRepository<ChatSessionEntity> sessionRepository,
                                IBaseRepository<ChatMessageEntity> chatMessageRepository)
    { /* ... */ }

    public async Task<string> SendSessionMessageAsync(SendSessionMessageBo dto)
    {
        var sessionId = await StartSessionAsync(...);        // 领域规则：会话不存在才创建
        await _chatMessageRepository.AddAsync(new ChatMessageEntity(...), true);  // 自己写库
        return sessionId;
    }
}

// Application/ConversationService.cs —— AppService 编排：注入并调用 DomainService，做 DTO 映射
public class ConversationService : IConversationAppService
{
    private readonly ISessionDomainService _sessionDomainService;   // 复用的公共操作
    public async Task<SendResultDto> SendAsync(SendRequest req)
    {
        var sessionId = await _sessionDomainService.SendSessionMessageAsync(MapToBo(req));  // 调领域服务
        return new SendResultDto { SessionId = sessionId };                                  // DTO 映射在 AppService
    }
}
```

> **什么时候抽 DomainService**：操作只被一个 AppService 用、不会复用 → 直接写在那个 AppService；**多个 AppService 都要用同一段操作**（参考项目里 `ISystemConfigDomainService` 被 5+ 个 AppService 共用）→ 抽成 DomainService 避免重复。纯 CRUD 项目 Domain 层可空。

### Adapter（外部适配器层）
**放**：外部系统调用的封装——`I{X}Adapter`（领域适配器）实现 + `IXxxHttpClient`（HTTP 客户端）实现。屏蔽第三方 API / 微服务 / SDK 的 HTTP/协议细节，对内暴露领域语义方法。
**不放**：业务规则、用例编排（那是 AppService/DomainService 的活）；把 HttpClient/URL 直接暴露给业务层。
**依赖**：IDomain（+ 第三方 SDK 包）。`Adapter.csproj` 只引用 `IDomain`。
**结对 + 两层**（复杂外部调用推荐）：
- 领域适配器：接口 `I{X}Adapter` 在 `IDomain/Adapter/`，实现 `{X}Adapter : IScopedDependency, I{X}Adapter` 在 `Adapter/Service/`，方法用 **Bo**（`IDomain/Adapter/{模块}/`）。
- HTTP 客户端：接口 `IXxxHttpClient` 在 `Adapter/HttpWebClient/`，实现 `XxxHttpClient : IScopedDependency, IXxxHttpClient`，用 **Ho**（`Adapter/Ho/{模块}/`），注入 `IHttpHelper` 做真实 HTTP 调用（统一走包内调用；需响应头/状态码语义时在实现内二次封装，见 SKILL.md「外部适配器 Adapter → 关键点」）。

```csharp
// IDomain/Adapter/ISmsAdapter.cs —— 接口在 IDomain，用 Bo，不暴露 HttpClient
public interface ISmsAdapter { Task<bool> SendAsync(SendSmsBo bo); }

// Adapter/Service/SmsAdapter.cs —— 实现：标 IScopedDependency，做 Bo→Ho 转换，调下层 HttpClient
public class SmsAdapter : IScopedDependency, ISmsAdapter
{
    private readonly ISmsHttpClient _client;
    public SmsAdapter(ISmsHttpClient client) => _client = client;
    public Task<bool> SendAsync(SendSmsBo bo)
        => _client.SendAsync(new SendSmsRequestHo { Phone = bo.Phone, Content = bo.Content });
}

// Adapter/HttpWebClient/SmsHttpClient.cs —— 真实 HTTP 调用，输入/输出用 Ho
public class SmsHttpClient : IScopedDependency, ISmsHttpClient
{
    private readonly IHttpHelper _httpHelper;
    public SmsHttpClient(IHttpHelper httpHelper) => _httpHelper = httpHelper;
    public Task<bool> SendAsync(SendSmsRequestHo req)
        => _httpHelper.PostAsync<bool>("https://sms.example.com/send", req);
}

// Application/XxxAppService.cs —— 业务层只依赖 IDomain 里的接口
public class XxxAppService
{
    private readonly ISmsAdapter _smsAdapter;   // 依赖接口；外部协议升级只改 Adapter
}
```

> **什么时候用 Adapter**：调用**本系统数据库** → AppService/DomainService 直接注入 `IBaseRepository`；调用**外部系统** → 走 Adapter（业务层依赖 `I{X}Adapter` 接口）。简单外部调用可只用领域适配器一层（直接调通用 `IHttpHelper`，前提是能接受其能力边界：不暴露响应头、非 2xx 抛原生异常或静默返回 `Stream.Null`）；需要响应头/状态码语义或客户端策略时，在 `IXxxHttpClient` 实现内二次封装（内部可用原生 `HttpClient`），不绕开包另起炉灶。

### EntityFramework（基础设施-数据）
**放**：`DbContext`、EFCore 配置类（`{Entity}Etc.cs`）、迁移。
**不放**：业务逻辑、实体定义（实体在 IDomain）。
**依赖**：Core、IDomain；包引用 `Common.EFCore` + provider 包。

配置类示例（`{Entity}Etc.cs`，继承配置基类，设表名/列名/查询过滤）：
```csharp
using Azrng.EFCore.EntityTypeConfigurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using App.IDomain.Entities;

namespace App.EntityFramework;

public class ProductEtc : EntityTypeConfigurationIdentityOperatorStatus<Product, long>
{
    public override void Configure(EntityTypeBuilder<Product> builder)
    {
        base.Configure(builder);                          // 必调：主键/审计列/时间类型
        builder.ToTable("product");                       // 表名
        builder.HasQueryFilter(x => !x.Deleted);          // 软删除全局过滤
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired().HasComment("商品名称");
        builder.Property(x => x.Price).HasComment("价格");
    }
}
```

DbContext（继承 `DbContext`，`OnModelCreating` 里扫描当前程序集的 Etc 配置）：
```csharp
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using App.IDomain.Entities;

namespace App.EntityFramework;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<Product> Products { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());  // 扫描本程序集的 *Etc 配置
        base.OnModelCreating(modelBuilder);
    }
}
```

### Service（入口/组装层）
**放**：`Controllers`、`Program.cs`（宿主生命周期）、`Startup.cs`（DI 装配 + 中间件管道）、`appsettings`、`wwwroot/index.html`、`wwwroot/MigrationSql/`、安全/过滤器。
**不放**：EF 查询、业务校验、`IBaseRepository` 注入。
**依赖**：Application、EntityFramework、Storage。Service 是组合根，负责注册存储实现、数据库、迁移、Swagger 和中间件。

控制器（瘦）——**一行转发** AppService 返回值，不写 `Ok()`、不写 `ResultModel`（全局包装过滤器自动包装）：
```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using App.IApplication.Product;
using App.IApplication.Product.Dto;

namespace App.Service.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ProductController(IProductAppService productAppService) : ControllerBase
{
    [HttpGet("list")]
    public Task<GetQueryPageResult<ProductDto>> GetList([FromQuery] ProductListRequest request)
        => productAppService.GetListAsync(request);   // 一行转发，过滤器自动包成 ResultModel

    [HttpGet("{id}")]
    public Task<ProductDto> Get(long id)
        => productAppService.GetAsync(id);

    [HttpPost]
    public Task<long> Create([FromBody] CreateProductRequest request)
        => productAppService.CreateAsync(request);

    [HttpDelete("{id}")]
    public Task Delete(long id)
        => productAppService.DeleteAsync(id);
}
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

### Program.cs 与 Startup.cs 分工

`Program.cs` 只保留宿主生命周期代码；所有 DI 注册放在 `Startup.RegisterServices(IServiceCollection)`，所有中间件和端点管道放在 `Startup.SetupMiddlewares(WebApplication)`。这样新增数据库、认证、Swagger 或中间件时不会持续膨胀宿主入口。

```csharp
// Service/Program.cs
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

`DotNetEnv` 是 Service 项目的可选开发依赖：

```xml
<PackageReference Include="DotNetEnv" />
```

将 `.env` 放在仓库根目录并加入根 `.gitignore`；可提交不含敏感值的 `.env.example`。加载必须发生在 `WebApplication.CreateBuilder(args)` 之前，不能放到 `Startup.RegisterServices` 之后，否则默认配置链可能已经完成构建。

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
        services.AddMvcResultPackFilter("/swagger");
        services.AddMvcModelVerifyFilter();
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

三件套装配细节见 `SKILL.md → 全局返回包装 → Startup 装配` 与 `SKILL.md → 依赖注入与中间件装配`。

> 控制器直接转发 AppService 返回的 `Task<T>`，无需 `Ok()` 包装——`CustomResultPackFilter` 自动把 `Task<T>` 的结果包成 `ResultModel<T>`（含 isSuccess/isFailure/code/message/data/errors）。错误路径在 AppService 里 `throw new ParameterException(msg)`，由 `app.UseGlobalException()` 兜底。

Service 中如需提供 `wwwroot/index.html` 联调页，在 `MapControllers()` 前调用 `app.UseDefaultFiles()` 和 `app.UseStaticFiles()`。SQL 迁移脚本放在 `Service/wwwroot/MigrationSql/`，代码通过 `ContentRootPath` 组合出绝对路径。文件下载 action 返回 `FileResult` 时包装过滤器会自动跳过；如果返回其它原始 `IActionResult`，可使用 `Azrng.AspNetCore.Core.Filter.NoWrapperAttribute`。

---

## 2. 跨层数据流（一个 GET 请求的旅程）

```
HTTP GET /api/product/list?name=xx
  │
  ▼
[Service] ProductController.GetList(ProductListRequest)   ← 仅接参数、一行转发
  │  调用接口
  ▼
[Application] ProductAppService.GetListAsync              ← 业务+查询在这里
  │  注入 IBaseRepository<Product>；WhereIfNotNullOrWhiteSpace + ToPageListAsync
  ▼
[EntityFramework] IBaseRepository → DbContext → PostgreSQL   ← 基础设施
  │  返回 Product 实体（带审计字段）
  ▼
[Application] 实体 → ProductDto 映射（剥掉审计字段）   ← 边界转换
  │  返回 GetQueryPageResult<ProductDto>（裸业务对象，非 IResultModel）
  ▼
[Service] 控制器 return GetQueryPageResult<ProductDto>（一行转发）
  │  CustomResultPackFilter 自动包装
  ▼
HTTP 200 { isSuccess:true, code:"200", data:{ rows:[...], pageInfo:{ pageIndex, pageSize, total, totalPage } } }
```

失败路径（如 `CreateAsync` 校验不通过）：AppService `throw new ParameterException("价格不能为负")` → 冒泡穿过控制器 → 被 `app.UseGlobalException()` 捕获 → 映射成 `HTTP 400 { isSuccess:false, code:"400", message:"价格不能为负" }`。

**关键边界**：实体只在 `AppService ↔ DbContext` 之间流动；**出了 AppService 一律是 DTO**。控制器和前端永远看不到实体。

---

## 3. .csproj 依赖关系

```
Core              → (无项目引用)
IDomain           → Core
IApplication      → Core, IDomain
Domain            → IDomain, EntityFramework, Adapter    ← 纯 CRUD 可空，有复用操作则建
Application       → IApplication, Domain
EntityFramework   → Core, IDomain
Service           → Application, EntityFramework, Storage
Adapter           → Core, IDomain                         ← 可选
Storage           → Core, IDomain                          ← 可选
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
| 1 | `IApplication/{业务域}/Dto/` | IApplication | 应用层契约类一类一个文件：`OrderDto.cs`、`OrderListRequest.cs`（继承 `GetPageRequest`）、`CreateOrderRequest.cs`；跨业务域通用 DTO/枚举/常量才放 `Core/Models`（判据见 SKILL.md 核心规则 4） |
| 2 | `IDomain/Entities/Order.cs` | IDomain | 实体 + `SetCreator` |
| 3 | `EntityFramework/OrderEtc.cs` | EntityFramework | EFCore 配置类（表名/列名/查询过滤） |
| 4 | `IApplication/Order/IOrderAppService.cs` | IApplication | 接口，返回 `Task<T>` / `Task<GetQueryPageResult<T>>`（全局包装，无 IResultModel） |
| 5 | `Application/Order/OrderAppService.cs` | Application | 实现 `IScopedDependency` + 仓储 + 业务：成功 `return`、失败 `throw ParameterException`、资源不存在 `if (entity is null) throw NotFoundException`、列表用 `ToPageListAsync`+`GetQueryPageResult`（自动注册，免手写 AddScoped） |
| 6 | `Service/Controllers/OrderController.cs` | Service | 瘦控制器，一行转发 `Task<T>`（不写 `Ok()`） |
| 7 | `Service/Program.cs` + `Service/Startup.cs` | Service | `Program.cs` 只保留宿主生命周期；`Startup.RegisterServices` 放 `RegisterBusinessServices`、结果过滤器等 DI 注册，`Startup.SetupMiddlewares` 放 `app.UseGlobalException()` 等管道配置 |

新增实体在 DbContext 加 `DbSet` + 写 `{Entity}Etc.cs` 配置类，`ApplyConfigurationsFromAssembly` 自动扫描。若用 SQL 脚本迁移，装 `Azrng.SqlMigration` 包 + 加 `Service/wwwroot/MigrationSql/x.x.x.sql`（详见 azrng-efcore「SQL 脚本迁移」）。
