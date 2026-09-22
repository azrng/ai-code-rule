### 核心库
- `Azrng.Core`：基础类库（实体基类、扩展方法、工具类、结果包装、异常体系）
- `Azrng.Core.Json`：JSON 序列化（基于 `System.Text.Json`）
- `Azrng.AspNetCore.Core`：API 基础设施（统一响应、模型校验、异常中间件）
- `Azrng.SqlMigration`：SQL 脚本迁移执行
- `Common.Cache.Redis`：Redis 缓存封装（基于 `StackExchange.Redis`）
- Swagger 与内存缓存按需使用对应专项规则，不在核心库清单中重复定义
- 外部 HTTP 调用默认走 `Common.HttpClients`（`IHttpHelper`，当前核对 4.1.0，按项目锁定的 4.x 版本为准），不为单次调用重复封装 HttpClient；只有需要包未暴露的底层控制时才使用原生 `HttpClient` / `IHttpClientFactory`；无外部调用时不引入该包。
- 引入 `Common.HttpClients` 时宿主须在组合根显式调用其 `AddHttpClientService` 注册运行时依赖：包不随工程引用自动注册，Adapter 等调用方编译通过不代表 `IHttpHelper` 运行时已可注入。
