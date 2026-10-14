### Application 层规则
- 服务类实现接口，接口定义在 `Interfaces/` 目录
- 服务方法必须是异步的（返回 `Task<T>`）
- 使用 DTO 进行数据传递，不直接暴露 Domain 实体
- 使用 AutoMapper 进行对象映射（如需要）
- 禁止在 Controller、Service、Repository、Entity、DTO 等 C# 类中使用主构造函数（Primary Constructor），统一使用显式构造函数
- 业务异常抛出继承自 `BaseException` 的自定义异常
