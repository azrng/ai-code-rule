### 统一结果包装
- 桌面 / 移动应用没有 Web 那样的全局结果包装过滤器：Service 方法默认返回裸业务对象（`Task<T>`），参数校验等可预期错误抛 Azrng 业务异常，由 ViewModel 捕获并转为用户提示
- 仅当调用方需要「有结果但失败」的流程型返回时使用 `IResultModel<T>`，构造用 `ResultModelFactory.Success(data)` / `ResultModelFactory.Failure(message)`（`Azrng.Core` 提供；`Failure` 默认 code 为 `"ERROR"`）
- `IResultModel<T>` 仅暴露 `Data`；成败标志与错误信息（`IsSuccess` / `Message` / `Errors`）在非泛型 `IResultModel` 上，实现类 `ResultModel<T>` 同时实现两个接口，消费侧按 `is IResultModel` 判别
- 对于仅表示操作结果的方法，也应保持统一的结果语义，不返回随意结构
