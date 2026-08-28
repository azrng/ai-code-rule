### 异常处理规范
- 业务异常继承 Azrng 异常体系：
  - `LogicBusinessException`：业务逻辑异常
  - `ParameterException`：参数校验异常
  - `NotFoundException`：资源不存在
  - `ForbiddenException`：禁止访问
  - `InternalServerException`：服务器内部错误
- 异常由中间件统一捕获并转换为 `ResultModel` 响应。
- 业务失败一律通过抛出上述异常表达；禁止在业务层手工构造失败 `ResultModel` 或直接设置错误状态码。
