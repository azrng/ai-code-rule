# 分页与 JSON 序列化细则

> 由 SKILL.md「分页查询最佳实践」「JSON 序列化」节下沉：完整代码写法、long→string 精度处理。四件套清单与选型口径见 SKILL.md。

## 分页标准写法（AppService）

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