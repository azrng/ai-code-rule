### 后端测试
- 单元测试使用 xUnit + Moq
- Controller 测试：验证状态码、响应结构、参数校验
- Service 测试：验证业务逻辑分支，使用 mock 隔离依赖
- Repository 测试：使用 In-Memory 数据库或 Testcontainers
- 涉及 PostgreSQL 时间语义、JSON 映射或 SQL 方言相关的查询逻辑，优先用 Testcontainers 连真实数据库验证；In-Memory 无法暴露此类差异
- 集成测试：使用 WebApplicationFactory 测试完整请求管道
- 新增测试 / 源工程必须与工程本体同提交登记进解决方案（`*.slnx` / `*.sln`）：`dotnet test` 按解决方案枚举工程，对未登记工程**静默跳过**（不报错，绿灯行数悄悄变小）。验证时把 `dotnet test` 输出的工程结果行与解决方案工程清单逐一核对，缺行即登记遗漏。
