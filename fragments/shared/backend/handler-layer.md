### Handler 层规则
- Handler 方法定义在 App 结构体上（Wails 绑定要求）
- 只做参数校验和转发，不写业务逻辑
- 方法命名使用 PascalCase
- 参数和返回值必须是可序列化类型
- 错误通过返回 error 传递
