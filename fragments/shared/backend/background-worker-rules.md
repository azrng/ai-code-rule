#### 后台任务（Worker）基线

- 仅在引入进程内后台任务（`BackgroundService` / `IHostedService`、定时扫描、生命周期循环等）时适用以下口径。
- **编排归属**：Worker 类本体放宿主 / Service 层，只负责调度（触发、节奏、重试）；业务编排 MUST 经应用层服务接口（洋葱架构：`IApplication` 接口）完成，NEVER 在 Worker 内直接查询 EF / 数据库或写业务规则。
- **scope 粒度**：Worker 是单例、活在整个进程生命周期；每个处理单元（每轮 / 每条记录）MUST 用 `IServiceScopeFactory.CreateScope()` 创建独立 DI scope 并在完成后立即释放，NEVER 持有长生命周期 scope 或跨处理单元复用同一个 scope——单次失败不得拖垮整个循环的回滚上下文。
- **多实例幂等**：同一服务可能多实例部署时，记录级领取 / 状态推进与副作用写入 MUST 由数据库侧裁决（CAS 条件更新、唯一键抢占、`FOR UPDATE SKIP LOCKED`，成败以影响行数 / 唯一键冲突显式检出），NEVER 依赖进程内锁、内存状态或 `lock` 裁决跨进程互斥——跨进程锁不存在。
- **作业级互斥**：整跑不允许跨实例并行的任务 SHOULD 用分布式锁（Redis / 数据库锁包）；锁不是裁决层，有外部副作用的作业锁内仍 MUST 留数据库侧幂等断言（CAS / 批次唯一键），NEVER 拿"拿到了锁"替代幂等。
- **启动时序**：依赖已迁移表的后台任务 MUST 推迟到启动迁移完成后才首轮扫描（初始等待，或 `IHostedService.StartedAsync`），NEVER 假定迁移与 Worker 顺序自然正确；时序是否成立按 backend-testing 的「空库真实启动冒烟」验证。
