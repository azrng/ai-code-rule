### ViewModel 规则
- ViewModel 中可以组织用户操作流程，但禁止直接写 SQL 或直接依赖存储细节
- 命令执行后的成功、失败、空状态必须显式反馈到界面状态
- 异常在 ViewModel 层转换为用户友好的提示信息，不把底层异常原样暴露给用户
- 跨 ViewModel 协作优先使用 `IMessenger` 或显式服务，不依赖静态全局状态
- 禁止在 ViewModel、Service、Repository、Model、DTO 等 C# 类中使用主构造函数（Primary Constructor），统一使用显式构造函数
