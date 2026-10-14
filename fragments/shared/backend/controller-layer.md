### Controller 层规则（如提供 API 接口）
- Controller 只负责 HTTP 处理，不包含业务逻辑
- 使用 `[ApiController]` 特性，自动处理 400 响应
- 使用 `[Route("api/[controller]")]` 统一路由前缀
- 注入 Application 层的服务接口，不直接依赖 Infrastructure
- Controller action 直接返回 `Task<T>` 并一行转发 Service；不写 `Ok()`，不手动构造 `ResultModel`，统一包装由全局过滤器与异常中间件完成（见 `统一响应格式`）
