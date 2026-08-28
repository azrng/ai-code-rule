### 核心库
- `Azrng.Core`：基础类库（实体基类、扩展方法、工具类、结果包装、异常体系）
- `Azrng.Core.Json`：JSON 序列化（基于 `System.Text.Json`）
- `Azrng.AspNetCore.Core`：API 基础设施（统一响应、模型校验、异常中间件）
- `Azrng.SqlMigration`：SQL 脚本迁移执行
- `Common.Cache.Redis`：Redis 缓存封装（基于 `StackExchange.Redis`）
- Swagger 与内存缓存按需使用对应专项规则，不在核心库清单中重复定义
- 引入 `Common.HttpClients` 前先确认仓库已有依赖和真实使用场景，不仅为单次调用新增封装
