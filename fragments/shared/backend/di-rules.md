### 依赖注入规范
- 使用 Azrng 标记接口声明服务生命周期：
  - `ITransientDependency`：每次解析创建新实例
  - `IScopedDependency`：每个请求或工作作用域内复用实例
  - `ISingletonDependency`：应用程序生命周期内复用实例
- 在组合根调用 `services.RegisterBusinessServices(assemblies)` 扫描并注册服务（扩展由 `Azrng.AspNetCore.Core` 提供），避免逐一手动注册；组合根已按洋葱架构拆分 `Startup` 时在 `Startup.RegisterServices` 内调用、未拆分时在 `Program.cs` 内集中调用——拆分口径以 onion-architecture 技能的 `Program.cs` / `Startup` 分工为准（`Program.cs` 只留宿主生命周期），已拆分项目禁止把注册内容搬回 `Program.cs`
- Controller 和其他消费者通过构造函数注入依赖
- 禁止使用静态服务定位器 pattern
