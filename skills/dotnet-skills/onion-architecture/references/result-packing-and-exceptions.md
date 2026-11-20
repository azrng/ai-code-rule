# 全局返回包装与业务异常细则

> 由 SKILL.md「全局返回包装」节下沉：三件套注册、异常映射全表、版本差异、CORS / Swagger / 健康探针 / 文件下载装配细节与控制器写法。成功路径靠过滤器、失败路径靠异常中间件的硬约束见 SKILL.md。

这是本架构的**统一对外结果契约**：所有 action 返回值最终都是 `{ isSuccess, isFailure, code, message, data, errors }` 形态的 `ResultModel`，但**业务代码不手动包装**——靠框架三件套自动完成。没有数据或没有错误时，`data` / `errors` 仍可能以空值或空数组输出，按实际序列化配置为准。

## 三件套（来自 `Azrng.AspNetCore.Core`，缺一不可）

| 件 | 注册 | 负责路径 | 行为 |
|---|---|---|---|
| 返回值包装过滤器 `CustomResultPackFilter` | `services.AddMvcResultPackFilter("/swagger")` | **成功路径** | action 返回值**非 `IResultModel`** → 包成 `ResultModel<T>(data, true, "success", "200")`；`EmptyResult`→`ResultModel(true,"成功")`；已是 `IResultModel` 则**不重复包装**；`[NoWrapper]` 特性或忽略前缀跳过 |
| 模型校验过滤器 `ModelVerifyFilter` | `services.AddMvcModelVerifyFilter()` | **模型校验失败** | 把 `ModelState` 错误转成 `ResultModel(false,"参数格式不正确","400", errors)` |
| 全局异常中间件 `CustomExceptionMiddleware` | `app.UseGlobalException()` | **业务异常 / 未处理异常** | catch 异常，按类型映射成 `ResultModel(false, msg, code)`（见下表） |

> **成功路径靠过滤器，失败路径靠异常中间件**——这是全局包装的核心。两者必须都装配，否则失败抛异常会变成裸 500。

## 业务异常 → 结果映射（`CustomExceptionMiddleware`）

| 业务场景 | 抛什么（`Azrng.Core.Exceptions`） | HTTP 状态码 | ErrorCode |
|---|---|---|---|
| 参数错误 / 业务校验失败 | `throw new ParameterException(msg)` | 400 | `"400"` |
| 业务规则不满足（语义同参数异常） | `throw new LogicBusinessException(msg)` | 400 | `"400"` |
| 资源不存在（按 id 查不到） | `if (entity is null) throw new NotFoundException("记录不存在")` | 404 | `"404"` |
| 未认证（未登录或凭证无效） | `throw new UnauthorizedException(msg)` | 401 | `"401"` |
| 已认证但无权限 | `throw new ForbiddenException(msg)` | 403 | `"403"` |
| 服务器内部错误 | `throw new InternalServerException(msg)` | 500 | `"500"` |
| 其他未处理异常 | （任何 `Exception`） | 500 | `"500"` |

> 默认按真实 HTTP 状态码返回（400/401/403/404/500）。若要统一返回 200、错误信息只放 body，配 `CommonMvcConfig.UseHttpStateCode = false`。

> **异常 ErrorCode 口径（Azrng.Core 2.0.0 起，NuGet 已发布）**：`LogicBusinessException`/`ParameterException` 全构造 400、`ForbiddenException` 全构造 403、`NotFoundException` 全构造 404，上表按此成立。**注意实际生效版本**：当前基线（`Common.EFCore` 1.8.0 与 `Azrng.Core` 2.0.0 同批发布）只引 EFCore 家族时传递解析到的也是 2.0.0；**存量项目锁 `Common.EFCore` 1.7.x 的传递下限是 `Azrng.Core 1.20.1`（传递版本只是下限）**，只引 EFCore 家族不显式引 Core 时生效的是 1.20.1——需要 2.0.0 口径必须显式 `PackageReference Azrng.Core ≥ 2.0.0`（新项目骨架清单第 1 条已要求 Core.csproj 显式引用，按 2.0.0+ 锁版本）。**旧版（< 2.0.0）差异**：`LogicBusinessException(string message)` 走 `BaseException(string)`，`ResultModel.code` 为 `"500"`（HTTP 仍是 400）；`ForbiddenException` 无参构造的 `ErrorCode` 是 `"401"`——存量项目锁旧版时按旧版实测值处理，不要按上表预期旧版行为；无论哪个版本，前端判错统一按 `isSuccess=false`，不要依赖 code。

> **成功包装保留 action 状态码（Azrng.AspNetCore.Core ≥ 1.6.0）**：`CustomResultPackFilter` 只替换响应体为统一外壳，不再把 `CreatedAtAction`(201)/`Accepted`(202)/`StatusCode(...)` 强制改成 200——但外壳内 `code` 仍是 `"200"`，前端判成功按 `isSuccess`，不要按 `code` 推断 HTTP 状态。

> `AddMvcResultPackFilter` 的忽略前缀按 URI 路径匹配，传值应以 `/` 开头，例如 `/swagger`。传入 `swagger` 可能不影响启动，但在首个结果过滤请求时才抛参数异常。

> **包 API 以锁定版本为准**：Azrng 生态包的注入签名 / 参数单位 / 自动注册范围**以锁定版本解包 XML 或反射探测为准，本文档口径可能滞后于包版本**。已核实的易错事实：`AddMvcResultPackFilter` / `AddMvcModelVerifyFilter` 挂 `IServiceCollection`（不是 `AddControllers().Add…` 链式扩展）；`HttpClientOptions.Timeout` 单位是秒（int，不是 TimeSpan）；`IBearerAuthService` 无自动注册需手动 `AddScoped`；`AddEntityFramework<T>` 已内含 UnitOfWork / Repository / 发号注册，不重复手写。

## Startup 装配（标准模板见 references）

Service 项目使用与 `Program.cs` 平级的 `Startup.cs` 承担依赖注入和中间件管道装配，`Program.cs` 只负责创建宿主、调用 Startup、构建运行，不堆积业务注册与中间件细节。三件套按上表「注册」列装配；完整标准模板（Program.cs / Startup.cs / DotNetEnv `.env` 时序）见 `layer-responsibilities.md → Program.cs 与 Startup.cs 分工`，`Startup` 使用显式构造函数（禁主构造函数，见 SKILL.md「新项目骨架清单」）。

> **最常见的坑**：只注册了 `AddMvcResultPackFilter` 却忘了 `app.UseGlobalException()`——此时 AppService 抛 `ParameterException` 无人接住，业务失败会返回 HTTP 500，前端拿不到统一失败结构。三件套必须齐备。

## CORS 跨域装配

CORS 优先用 `Azrng.AspNetCore.Core` 内置扩展，不要手写原生 `AddCors`/`AddPolicy`（特殊策略需求除外）：

- **注册（模板默认）**：`services.AddAnyCors()`——`AllowAnyOrigin`，开箱即用，**仅限开发环境**，上线前必须改掉。
- **生产按需改**：`services.AddCorsByOrigins(string[] allowedOrigins)`——来源白名单走配置（`appsettings.json` 的 `Cors:AllowedOrigins` 节，生产用环境变量 `Cors__AllowedOrigins__N` 覆盖）；空列表 / 含空白项启动即抛 `ArgumentException`，防漏配；`allowCredentials: true` 仅 SignalR / 携带凭证场景。
- **管道**：`app.UseCorsPolicy()`（默认策略名 `DefaultCors`，两种注册方式通用），放在异常处理 / Swagger / 静态文件之后、`UseAuthentication` / `UseAuthorization` / `MapControllers` 之前。
- **禁忌**：未注册 CORS 服务就写 `app.UseCors()`，首个跨域请求直接 500；`AddAnyCors` 不得用于生产；`allowCredentials: true` 不得与 `AllowAnyOrigin` 组合。纯后端 / 无跨域需求的项目整体删掉注册与中间件两行，不是必装项。
- **版本提示**：`Azrng.AspNetCore.Core` 1.5.0 起移除了 `AddCorsPolicy()`（与框架原生 `services.AddCors(o => o.AddPolicy(...))` 完全等价）——高级自定义策略直接用原生 `AddCors` 注册，策略名与 `app.UseCorsPolicy(策略名)` 对齐即可；存量模板里仍见 `AddCorsPolicy` 说明包版本 < 1.5.0，升级时按原生写法迁移。

## Swagger 与静态文件装配

Swagger 注册用 `services.AddDefaultSwaggerGen(title: "项目名称", showJwtToken: ...)`（Azrng.Swashbuckle，禁止直接引 Swashbuckle.AspNetCore；签名以锁定版本为准，当前基线 0.4.0，源码核实）。`showJwtToken` 默认 `false`：开启会向 OpenAPI 文档声明全局 Bearer 安全要求、改变接口契约——**仅在认证方案就绪后传 `true`**，并与 `app.UseAuthentication()` / `app.UseAuthorization()` 一起验证；无认证的服务保持默认（见 SKILL.md「新项目骨架清单」）。`UseDefaultSwagger` 行为细节（`onlyDevelopmentEnabled` 语义、默认路由前缀、`/swagger` 前缀写法、联调页静态文件）见 `layer-responsibilities.md → Swagger 与静态文件装配`。

## 默认健康检查控制器

Service 项目默认提供 `HealthController`（`alive` / `ready` 双探针）：`alive` 只表示进程仍能响应，`ready` 表示已完成启动——若 `ready` 需核对数据库等依赖，必须在端点内接入对应检查，不要让 `alive` 依赖外部服务。两个探针必须 `[NoWrapper]` 返回裸 JSON。完整模板见 `layer-responsibilities.md → 默认健康检查控制器`。

## 文件下载与原始响应

`NoWrapperAttribute` 位于 `Azrng.AspNetCore.Core.Filter`。包装过滤器本身会自动跳过 `FileResult`，因此 `return File(...)` 通常不需要 `[NoWrapper]`；action 返回其它原始 `IActionResult` 时可显式加 `[NoWrapper]` 保护。示例见 `layer-responsibilities.md → 文件下载与原始响应`。

## AppService / 控制器写法（全局包装模式）

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
// 控制器（Service）：一行转发，不写 Ok() / 不写 ResultModel（块体 + return，不用表达式体）
[HttpGet("{id}")]
public Task<OrderDto> Get(long id)
{
    return _orderAppService.GetAsync(id);
}

[HttpPost]
public Task<long> Create([FromBody] CreateOrderRequest req)
{
    return _orderAppService.CreateAsync(req);
}
```

> 关于 `IResultModel<T>`（历史/迁移期）：旧代码里 AppService 返回 `Task<IResultModel<T>>`、手动 `ResultModel<T>.Success/Error`，仍能工作（过滤器不重复包装），迁移期可与全局包装共存。但新代码统一用全局包装：更省事、控制器/AppService 一行转发。逐步替换即可。