### MVVM 模式规则
- 所有 ViewModel 最终都必须继承 `ObservableObject`；若项目已有 ViewModel 基类（如 `ViewModelBase`），应由基类继承 `ObservableObject` 后统一复用
- 属性通知使用 `[ObservableProperty]` 特性自动生成，禁止手写重复样板
- 命令定义使用 `[RelayCommand]` 特性生成，禁止手动拼装重复命令逻辑
- ViewModel 依赖通过构造函数注入
- 禁止在 Code-behind 中编写业务逻辑
