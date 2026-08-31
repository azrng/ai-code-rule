### 分层边界规则
- View 只负责展示，不承载业务逻辑
- ViewModel 负责状态管理、命令触发、调用 Service 与处理用户可见结果
- Service 负责业务逻辑、流程编排和规则校验，不直接操作 View
- Repository 是数据访问边界，具体实现细则见 `infrastructure-AGENTS.md`
- Model / DTO 负责承载业务数据，不在其中夹带 UI 行为
