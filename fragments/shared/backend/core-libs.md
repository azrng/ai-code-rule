### 核心库
- `Azrng.Core`：基础类库（实体基类、扩展方法、工具类、结果包装、异常体系）
- `Azrng.Core.Json`：JSON 序列化（基于 `System.Text.Json`）
- `Azrng.AspNetCore.Core`：API 基础设施（统一响应、模型校验、异常中间件）
- `Azrng.SqlMigration`：SQL 脚本迁移执行
- `Common.Cache.Redis`：Redis 缓存封装（基于 `StackExchange.Redis`）
- Swagger 与内存缓存按需使用对应专项规则，不在核心库清单中重复定义
- 外部 HTTP 调用统一走 `Common.HttpClients`（`IHttpHelper`），不为单次调用另写 HttpClient 封装；项目无任何外部 HTTP 调用时不引入该包
- `IHttpHelper` 能力边界：接口提供 Get/Post/Put/Delete/Patch/Form/Soap/Send/GetStreamAsync 全套方法，但**不暴露 HttpResponseMessage 响应头与状态码**（如 `GetStreamAsync` 仅返回 `Stream`，透传 `Content-Type` 拿不到）；非 2xx 行为二选一——`FailThrowException=true` 时抛原生 `HttpRequestException`（不走统一异常包装），`false` 时记日志后**静默返回 `Stream.Null`**，调用方难区分「远端失败」与「空内容」。需要透传响应头、按状态码映射业务异常或统一错误包装的场景，在 Adapter（或 `IXxxHttpClient` 实现）内部二次封装后使用（内部可用原生 `HttpClient` 作实现细节），不直接裸用 `IHttpHelper`
