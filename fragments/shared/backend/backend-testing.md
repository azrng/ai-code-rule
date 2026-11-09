### 后端测试
- 单元测试使用 xUnit + Moq
- Controller 测试：验证状态码、响应结构、参数校验
- Service 测试：验证业务逻辑分支，使用 mock 隔离依赖
- Repository 测试：使用 In-Memory 数据库或 Testcontainers
- 涉及 PostgreSQL 时间语义、JSON 映射或 SQL 方言相关的查询逻辑，优先用 Testcontainers 连真实数据库验证；In-Memory 无法暴露此类差异
- 集成测试：使用 WebApplicationFactory 测试完整请求管道
- 新增测试 / 源工程必须与工程本体同提交登记进解决方案（`*.slnx` / `*.sln`）：`dotnet test` 按解决方案枚举工程，对未登记工程**静默跳过**（不报错，绿灯行数悄悄变小）。验证时把 `dotnet test` 输出的工程结果行与解决方案工程清单逐一核对，缺行即登记遗漏。
- 项目涉及启动期数据库迁移与后台服务（`IHostedService` / `IHostedLifecycleService`）时序时，保留首次空库真实启动 smoke：EF InMemory 与构建验证暴露不了迁移晚于普通 `StartAsync` 导致的「先查未建表」问题；依赖启动顺序的后台任务延后到 `StartedAsync`，以日志确认迁移先行
