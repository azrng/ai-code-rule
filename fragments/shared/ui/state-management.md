### 状态管理规则
- 本地状态使用 `[ObservableProperty]` 管理
- 全局共享状态使用 `IMessenger` 传递跨 ViewModel 消息
- 禁止使用静态全局类存储业务状态
- 主题、语言等低频全局配置才放入应用级资源或全局上下文
