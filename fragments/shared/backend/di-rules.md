### 依赖注入规范
- 使用 Azrng 标记接口声明服务生命周期：
  - `ITransientDependency`：每次解析创建新实例
  - `IScopedDependency`：每个请求或工作作用域内复用实例
  - `ISingletonDependency`：应用程序生命周期内复用实例
- 在 `Program.cs` 中调用 `services.RegisterBusinessServices(assemblies)` 扫描并注册服务，避免逐一手动注册
- Controller 和其他消费者通过构造函数注入依赖
- 禁止使用静态服务定位器 pattern
- 如果仓库已有真实实现，以现有代码为准，不要强行重构或替换技术栈
- 技术债务与重构判断遵循根 `AGENTS.md` 的全局规则
