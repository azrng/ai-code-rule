### 代码注释规范
- 必须补 XML 注释的位置：
  - Controller action：用 `<summary>` 说明接口用途、关键参数、返回结果。
  - Application 层服务接口方法：说明应用能力，不写实现细节。
  - Domain 核心业务规则方法：说明规则意图。
  - 不来自接口的实现类自有方法（接口未约束的核心方法）：补 `<summary>` 说明该能力做什么。
  - 例外：Controller action 需要向调用方暴露事件格式、鉴权要求、分页约定等对外契约信息时，可保留精简后的 `<remarks>`，但不写内部实现说明。
- 涉及时间字段的 DTO / 接口注释必须写明转换责任方：入参是本地墙钟还是 UTC、由哪一层完成转换（如“本地墙钟（服务层统一转换）”），而不是只标存储单位。

<!-- include: shared/application/comment-discipline.md -->
