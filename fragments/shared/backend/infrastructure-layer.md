### Infrastructure 层规则
- DbContext 实现数据上下文，配置连接和映射
- 数据库结构变更通过 `Azrng.SqlMigration` SQL 脚本迁移管理（见 `数据库迁移规则`），不使用 EF Core Code-First Migrations
- 仓储实现依赖注入，遵循仓储模式
- 外部服务（邮件、文件存储等）实现在此层
