### 统一结果包装
- 服务层方法返回值统一使用项目既有的结果包装类型（如 `ResultModel<T>`）
- 成功响应：`ResultModel<T>.Success(data)`
- 错误响应：`ResultModel<T>.Failure(message, errorCode)`
- 对于仅表示操作结果的方法，也应保持统一的结果语义，不返回随意结构
