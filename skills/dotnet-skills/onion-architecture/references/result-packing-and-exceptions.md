# 全局返回包装与异常体系详解

> 事实来源：`Azrng.AspNetCore.Core` 1.4.0、`Azrng.Core` 1.20.0 源码（nuget-packages 仓库，2026-08-20 校准）。分层用法与三件套概览见 `SKILL.md → 全局返回包装`，本文件是 API 级细则。

## 目录
1. 注册方式与 CommonMvcConfig
2. CustomResultPackFilter 包装分支
3. ModelVerifyFilter 与校验特性
4. 异常家族与状态码映射
5. ResultModel 契约形状
6. 迁移期 IResultModel 写法

---

## 1. 注册方式与 CommonMvcConfig

两种等价路径，二选一：

```csharp
// 方式一：分开注册（存量项目常见，可分别控制）
builder.Services.AddMvcResultPackFilter("/swagger");   // 成功路径包装，参数为不包装的路由前缀
builder.Services.AddMvcModelVerifyFilter();            // 模型校验失败统一结构

// 方式二：一站式注册（新项目推荐）
builder.Services.AddDefaultControllers(o =>
{
    o.EnabledCustomerResultPack = true;   // 默认 true：挂 CustomResultPackFilter
    o.EnabledModelVerify = true;          // 默认 true：挂 ModelVerifyFilter + SuppressModelStateInvalidFilter
    o.UseHttpStateCode = true;            // 默认 true：失败返回真实 HTTP 状态码
});
// 返回 IMvcBuilder，可继续 .AddJsonOptions(...) 等链式配置
```

`CommonMvcConfig`（`Azrng.AspNetCore.Core` 命名空间）全表：

| 配置项 | 默认值 | 作用 |
|---|---|---|
| `EnabledCustomerResultPack` | `true` | 是否挂返回值包装过滤器（仅 `AddDefaultControllers` 路径生效） |
| `EnabledModelVerify` | `true` | 是否挂模型校验过滤器 + `SuppressModelStateInvalidFilter = true`（关闭 MVC `[ApiController]` 内置 400 自动响应，避免双份错误结构） |
| `UseHttpStateCode` | `true` | 失败（校验失败 / 异常）是否返回真实 HTTP 状态码；`false` 时统一返回 200、错误信息只放 body。异常中间件与校验过滤器都会读它（经 `IOptions<CommonMvcConfig>`） |

> 注意：`AddMvcResultPackFilterFilter` 是 `[Obsolete]` 的旧别名，禁止生成。`UseCustomExceptionMiddleware` 同为 `[Obsolete]`，用 `UseGlobalException`。

---

## 2. CustomResultPackFilter 包装分支

`IResultFilter`，在结果执行前逐分支判定（顺序即判定顺序）：

| 分支 | 行为 |
|---|---|
| 路由以 `ignoreRoutePrefix` 任一前缀开头（`StartsWithSegments`） | 不包装（如 `/swagger`） |
| action/控制器带 `[NoWrapper]`（`NoWrapperAttribute : ActionFilterAttribute`，`Azrng.AspNetCore.Core.Filter`） | 不包装 |
| `EmptyResult` | 包成 `ResultModel(true, "成功")` |
| `FileResult` | 不包装（文件下载） |
| `ObjectResult.Value` 已是 `IResultModel` | 不重复包装 |
| `ObjectResult.Value` 是 `ProblemDetails` | 不包装 |
| 其他 `ObjectResult` | 包成 `ResultModel<object?>(value, true, "success", "200")` |
| `ContentResult` / `StatusCodeResult` / `RedirectToActionResult` / `LocalRedirectResult` | 不包装，保持原行为 |

对返回 `Task<T>` 的 action：框架先把 `T` 落成 `ObjectResult`，走"其他 ObjectResult"分支包成 `ResultModel<T>(data, true, "success", "200")`。

---

## 3. ModelVerifyFilter 与校验特性

`ModelState` 无效时（`OnActionExecuting`）：

- 错误逐字段收敛成 `ErrorInfo { Field, Message }`，同字段多条错误用 `|` 拼接。
- 返回 `ResultModel(false, "参数格式不正确", "400", errorResults)`。
- `UseHttpStateCode = true`（默认）时 HTTP 400（`BadRequestObjectResult`）；`false` 时 HTTP 200。

自带校验特性（**命名空间是 `System.ComponentModel.DataAnnotations`**，与原生特性同源，无需额外 using）：

| 特性 | 行为 |
|---|---|
| `CollectionNotEmptyAttribute` | 集合非 null 且非空才通过（`IEnumerable`） |
| `MinValueAttribute(int minValue = 1)` | 数值 >= minValue 才通过；支持 `int`/`long`/`decimal`/`double`/`float`，其他类型一律通过 |

```csharp
public class CreateOrderRequest
{
    [Required, MinValue(1)]
    public int Count { get; set; }

    [CollectionNotEmpty]
    public List<OrderItemRequest> Items { get; set; } = new();
}
```

校验失败的响应示例：

```json
{
  "isSuccess": false,
  "code": "400",
  "message": "参数格式不正确",
  "errors": [
    { "field": "Count", "message": "字段 Count 必须在 1 以上" },
    { "field": "Items", "message": "集合不能为空" }
  ]
}
```

---

## 4. 异常家族与状态码映射

全部在 `Azrng.Core.Exceptions`。基类 `BaseException : Exception`：`ErrorCode`（默认 `"500"`，只读）、`HttpCode`（`HttpStatusCode`，默认 500）。

`CustomExceptionMiddleware`（`app.UseGlobalException()`）按 `switch` 类型匹配，**映射表以当前源码为准**：

| 异常 | 语义 | HTTP | `code` 取值 |
|---|---|---|---|
| `UnauthorizedException` | 未认证（未登录 / token 无效） | **401** | 异常 `ErrorCode` |
| `ForbiddenException` | 已认证但无权限 | **403** | 固定 `"403"` |
| `NotFoundException` | 资源不存在 | 404 | 异常 `ErrorCode`（默认 `"404"`） |
| `ParameterException` | 参数 / 业务校验失败 | 400 | 异常 `ErrorCode`（默认 `"400"`） |
| `LogicBusinessException` | 业务规则不满足（语义同参数异常） | 400 | 异常 `ErrorCode`（默认 `"400"`） |
| `InternalServerException` | 服务器内部错误 | 500 | 异常 `ErrorCode` |
| 其他 `BaseException` 派生 | 自定义业务异常 | 500 | 异常 `ErrorCode` |
| 其他任何 `Exception` | 未处理异常 | 500 | 固定 `"500"`，`message` 固定"系统异常,请联系管理员" |

要点：

- **401 用 `UnauthorizedException`，403 用 `ForbiddenException`**，写反前端鉴权处理会全乱。
- `RetryMarkException` 是 `RetryTask` 重试机制内部标记，**不在 HTTP 映射表内**（会落到 default 500），业务代码不要用它表达 HTTP 语义。
- `UseHttpStateCode = false` 时上表 HTTP 一律改 200，`code`/`message` 不变。
- `NotFoundException.ThrowIfNull(entity, "记录不存在")`：null 才抛，等价 `if (entity == null) throw new NotFoundException("记录不存在")`。
- 中间件会 `LogError` 记录完整异常（含 TraceId），响应为 camelCase JSON、不转义中文。

---

## 5. ResultModel 契约形状

`Azrng.Core.Results` 命名空间。序列化后字段：`{ isSuccess, code, message, data, errors }`。

```csharp
public class ResultModel : IResultModel
{
    public bool IsSuccess { get; set; }
    public bool IsFailure => !IsSuccess;
    public string Code { get; set; }        // 默认 "200"
    public string Message { get; set; }     // 默认 "success"
    public IEnumerable<ErrorInfo> Errors { get; }

    // 静态方法（迁移期手写路径用；新代码走全局包装，不手调）
    public static IResultModel Success();
    public static IResultModel Success(string message);
    public static IResultModel Error(string message, string errorCode = "400", IEnumerable<ErrorInfo>? errorInfos = null);
    public static IResultModel Failure(string message, string errorCode = "400", ...);  // 同 Error
}

public class ResultModel<T> : ResultModel, IResultModel<T>
{
    public T? Data { get; set; }
    public static IResultModel<T> Success(T data);                  // (data, "success", "200")
    public static IResultModel<T> Success(T data, string message);
    public static IResultModel<T> Error(string message, string errorCode = "400", ...);
}

public struct ErrorInfo { public string Field { get; set; } public string Message { get; set; } }
```

配套工具（消费 `IResultModel` 时用，如客户端 SDK / 内部服务间调用）：

| 工具 | 方法 | 用途 |
|---|---|---|
| `ResultModelFactory` | `Success<T>(data, message?)` / `Failure<T>(message, errorCode = "ERROR")` / `FromException<T>(ex, message?)` | 手工构造 `IResultModel<T>`（注意 `Failure` 默认 code 是 `"ERROR"` 而非 `"400"`） |
| `ResultModelExtensions` | `DataOrEmpty<T>`（`IReadOnlyList<T>`/`List<T>`）/ `DataOrDefault<T>(default)` | 取 data 的空安全兜底 |
| `ResultModelExceptionExtensions` | `ToFailureResult<T>(this Exception, message?)` | 异常转失败结果 |

---

## 6. 迁移期 IResultModel 写法

旧代码 AppService 返回 `Task<IResultModel<T>>`、手动 `ResultModel<T>.Success/Error`：

- 过滤器见 `IResultModel` 不重复包装，**仍能正常工作**，可与全局包装共存。
- `Failure`/`Error` 默认 code 注意：`ResultModel.Error` 是 `"400"`，`ResultModelFactory.Failure` 是 `"ERROR"`——迁移对齐时核对。
- 新代码统一全局包装：接口返回裸 `Task<T>`，失败 `throw` 业务异常。逐步替换，不做一次性大改。
