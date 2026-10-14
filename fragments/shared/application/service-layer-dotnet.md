### Service 层规则
- 服务类必须实现接口，接口定义在 `Services/Interfaces/` 目录
- 服务方法必须优先采用异步形式（返回 `Task<T>` 或 `ValueTask<T>`）
- 服务层处理所有业务逻辑，不直接承担视图渲染、控件操作与数据访问实现细节
- 服务层异常必须统一封装，抛出 Azrng 异常体系的业务异常类型
- 服务类实现 `ITransientDependency` / `IScopedDependency` / `ISingletonDependency` 接口表达生命周期，按对应生命周期显式 `AddTransient` / `AddScoped` / `AddSingleton` 注册（`RegisterBusinessServices` 属 `Azrng.AspNetCore.Core`，桌面项目不使用）
- 禁止在 ViewModel、Service、Repository、Model、DTO 等 C# 类中使用主构造函数（Primary Constructor），统一使用显式构造函数
