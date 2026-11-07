### 代码注释规范
- 必须补 XML 注释的位置：
  - ViewModel 公共命令与关键属性：用 `<summary>` 说明用户操作意图与可观察状态。
  - Service 接口方法：说明该能力做什么，不写实现细节。
  - 不来自接口的实现类自有方法（接口未约束的核心方法）：补 `<summary>` 说明该能力做什么。
  - 例外：ViewModel 公共命令需要向使用者说明绑定约定、关联属性、触发条件等对外信息时，可保留精简后的 `<remarks>`，但不写内部实现说明。

<!-- include: shared/application/comment-discipline.md -->
