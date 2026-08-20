# Program.cs 装配与横切能力详解

> 事实来源：`Azrng.AspNetCore.Core` 1.4.0、`Azrng.Core` 1.20.0 源码（nuget-packages 仓库，2026-08-20 校准）。装配概览见 `SKILL.md → 依赖注入与中间件装配`，本文件是 API 级细则。

## 目录
1. RegisterBusinessServices 注册规则
2. 完整装配模板与中间件顺序
3. 审计日志（UseAutoAuditLog）
4. RequestId（UseRequestIdMiddleware）
5. CORS 家族
6. 调试与其他中间件
7. LongToStringConverter
8. ICurrentUser
9. 周边能力包注册入口

---

## 1. RegisterBusinessServices 注册规则

标记接口（`Azrng.Core.DependencyInjection`）：`ITransientDependency` / `IScopedDependency` / `ISingletonDependency`（空接口，分别映射 Transient / Scoped / Singleton）。

两个重载：

```csharp
// 重载一：按程序集匹配模式扫描（推荐，通配所有分层项目）
builder.Services.RegisterBusinessServices("App.*.dll");
// 第二参数可选：逗号分隔要忽略的命名空间（如 "App.Adapter.HttpWebClient"）
builder.Services.RegisterBusinessServices("App.*.dll", ignoreNameSpace: "App.Adapter");

// 重载二：显式传程序集（精确控制扫描范围）
builder.Services.RegisterBusinessServices(typeof(ProductAppService).Assembly);
```

注册规则（源码 `RegisterUniteServices`）：

- 扫描程序集内**实现了任一标记接口的类**。
- 把该类实现的**所有剩余接口**（剔除三个标记接口、剔除 `Microsoft.` / `System.` 前缀及 `ignoreNameSpace` 命中的接口）逐一注册到该实现类；一个业务接口都不剩时按**自身类型**注册。
- 因此：**一个实现类不要挂多个不相关业务接口**（会被同时注册成多个服务）；也不用手写 `AddScoped`。
- 方法标注 `[RequiresUnreferencedCode]`（反射扫描程序集），启用 AOT / Trimming 的项目需评估。

---

## 2. 完整装配模板与中间件顺序

```csharp
var builder = WebApplication.CreateBuilder(args);

// 三件套：方式一（分开）或方式二（一站式 AddDefaultControllers），见 result-packing-and-exceptions.md
builder.Services.AddMvcResultPackFilter("/swagger");
builder.Services.AddMvcModelVerifyFilter();

// 数据访问（azrng-efcore skill）+ 批量服务注册
builder.Services.AddEntityFramework<AppDbContext>(o => { /* ... */ });
builder.Services.RegisterBusinessServices("App.*.dll");

// 审计日志自定义落库（可选）：实现 ILoggerService 后注册，DefaultLoggerService 只写日志
// builder.Services.AddScoped<ILoggerService, DbAuditLogService>();

var app = builder.Build();

app.UseGlobalException();          // ① 必须最外层（最早），兜住后续所有异常
app.UseRequestIdMiddleware();      // ② 请求 id 传播（可选）
app.UseAutoAuditLog(o =>           // ③ 审计日志（可选）：只记写操作
{
    o.IgnoreRoutePrefix = new List<string> { "/health", "/swagger" };
});
app.UseCors();                     // ④ CORS（先 AddCorsByOrigins/AddCorsPolicy 注册策略）
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

> `UseGlobalException` 放最外层是硬要求；其余按需，顺序遵循"异常兜底 → 请求标识 → 审计 → CORS → 认证授权 → 路由"。

---

## 3. 审计日志（UseAutoAuditLog）

`AuditLogMiddleware` 记录请求/响应全文。两个注册重载：

```csharp
// 重载一：只忽略部分路由（向后兼容写法）
app.UseAutoAuditLog(new List<string> { "/health" });

// 重载二：完整配置（推荐）
app.UseAutoAuditLog(o => { /* AuditLogOptions */ });
```

`AuditLogOptions`（`Azrng.AspNetCore.Core.AuditLog`）全表：

| 配置项 | 默认值 | 说明 |
|---|---|---|
| `IncludeHttpMethods` | `{ "POST", "PUT", "DELETE" }` | 只审计这些方法（默认不记 GET） |
| `IgnoreRoutePrefix` | 空 | 忽略的路由前缀列表 |
| `MaxResponseBodySize` | `1048576`（1MB） | 响应体截断上限 |
| `LogOnlyApiRoutes` | `true` | 只记 `/api` 开头路由 |
| `FormatJson` | `false` | 是否格式化 JSON 输出 |

落日志扩展点：默认 `DefaultLoggerService` 只写 ILogger。要落库时自定义 `ILoggerService`（`Write(AuditLogInfo)` / `WriteAsync`）并注册；`AuditLogInfo` 已带 `[Table("audit_logs")]` 与 `RequestBody` / `ResponseBody` / `StatusCode` / `UserId` / `ElapsedMilliseconds` 等列，可直接建 EF 实体映射。

> **输出行为（实测）**：每条写操作输出一行结构化 JSON（含 TraceId、路由、方法、请求/响应体、状态码、耗时），日志级别跟随响应状态码——2xx 记 Information、4xx/5xx 记 Error。敏感字段（token、密码）落审计前应在自定义 `ILoggerService` 里脱敏。

---

## 4. RequestId（UseRequestIdMiddleware）

- 请求头/响应头统一 `X-RequestId`：请求已带该头则透传其值，否则生成新 id。
- 同步写入 `context.TraceIdentifier`——日志、异常中间件记录的 TraceId 与之对齐，便于全链路排查。

```csharp
app.UseRequestIdMiddleware();
```

---

## 5. CORS 家族

| 注册（ServiceCollection） | 签名要点 | 场景 |
|---|---|---|
| `AddAnyCors()` | 允许任意来源 | **仅开发环境**，生产禁用 |
| `AddCorsByOrigins(string[] allowedOrigins, string policyName = "DefaultCors", bool allowCredentials = false)` | 按域名白名单 | 生产常规选择；SignalR/需要凭证时 `allowCredentials: true` |
| `AddCorsPolicy(string policyName, Action<CorsPolicyBuilder> configurePolicy)` | 完全自定义策略 | 需要暴露头、预检缓存等精细控制 |

管道侧 `app.UseCorsPolicy("DefaultCors")`（或原生 `app.UseCors()`）启用。示例：

```csharp
builder.Services.AddCorsByOrigins(
    new[] { "https://admin.example.com" },
    allowCredentials: true);
// ...
app.UseCors();
```

---

## 6. 调试与其他中间件

| 入口 | 用途 | 备注 |
|---|---|---|
| `AddShowAllServices(path)` + `app.UseShowAllServices()` | 端点列出容器内全部注册服务 | 仅开发环境，**不得上生产** |
| `app.UseRequestBodyRepetitionRead()` | 请求体可重复读 | 契合审计日志等多次读 body 的场景 |

---

## 7. LongToStringConverter

`Azrng.AspNetCore.Core.JsonConverters.LongToStringConverter : JsonConverter<long>`：序列化 long→string（防 JS 精度丢失，雪花 ID 必须），反序列化兼容字符串与数字。配合 MVC JSON 配置使用：

```csharp
builder.Services.AddDefaultControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new LongToStringConverter());
    });
```

> 若已用 `Azrng.Core.Json` 的 `ConfigureDefaultJson()`，其默认选项已内置 long→string 转换，无需重复挂。

---

## 8. ICurrentUser

`Azrng.Core.ICurrentUser<T>`（`ICurrentUser : ICurrentUser<string>`，继承 `IScopedDependency`）：`Subject` / `UserId` / `UserName` / `NickName` / `Auds` / `Token`。**包内无默认实现**（源码中默认实现被注释），按解决方案自己实现并注册：

```csharp
// Service 层：从 HttpContext claims 取值
public class CurrentUser : IScopedDependency, ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;
    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    public string Subject => _accessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
    public string UserId => _accessor.HttpContext?.User.FindFirst("UserId")?.Value ?? string.Empty;
    public string UserName => _accessor.HttpContext?.User.Identity?.Name ?? string.Empty;
    // NickName / Auds / Token 按实际 claim 结构实现
}
```

AppService / DomainService / 审计落库需要"当前操作人"时注入 `ICurrentUser`，不直接摸 `HttpContext`（保持分层——`HttpContext` 只允许出现在 Service 层）。

---

## 9. 周边能力包注册入口

选型速查见 `SKILL.md → 周边能力包速查`。补充备选入口（源码核实）：

| 包 | 入口 |
|---|---|
| `Azrng.AspNetCore.Authorization.Default` | `AddMyAuthorization<TPermissionService>` / `AddPathBasedAuthorization<TPermissionService>`（实现 `IPermissionVerifyService` 做权限校验） |
| `Common.Cache.Redis` | `AddRedisCacheService`（缓存能力）/ `AddRedisCacheStore` |
| `Common.Cache.MemoryCache` | `AddMemoryCacheExtension` / `AddMemoryCacheStore` |

这些包的配置项、选项模型不在本 skill 范围——用前回各自包 README（`C:\Work\gitee\nuget-packages\src\Shared\<包名>\README.md`）核实。

---

## 附：动态排序 SortContent 的绑定坑（实测）

`SortContent`（`Azrng.Core.Requests`）只有带参构造 `SortContent(string sortName, SortEnum sort)`、**无无参构造**：

- **POST body（`[FromBody]` JSON）可以**：System.Text.Json 支持带参构造反序列化（参数名与属性名匹配）。`{"sortContents":[{"sortName":"Price","sort":0}]}`（sort：0=Asc，1=Desc）。
- **GET query 绑定不行**：`?sortContents[0].sortName=Price` 会抛 `InvalidOperationException`（无法创建实例）→ 全局异常中间件兜底 500。列表接口要支持动态排序时用 POST body，或筛选/分页仍走 GET、排序参数单独定义平铺字段再手工构造 `SortContent`。
