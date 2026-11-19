### 数据库迁移规则
- 使用 `Azrng.SqlMigration` 进行数据库脚本迁移，不使用 EF Core Code-First Migrations
- 迁移脚本放在后端项目的 `MigrationSql/` 目录（多项目分层模板位于 `Service/wwwroot/MigrationSql/`，其余以所在子规则目录结构表为准），文件名按 `版本号.sql` 或 `版本号.txt` 命名（如 `1.0.0.sql`、`1.1.0.txt`），不含描述文字——去掉版本前缀与扩展名后的剩余部分会被整体解析为版本号；按版本号顺序执行
- 通过 `AddSqlMigrationService(...).AddAutoMigration()` 在应用启动时自动执行未应用的脚本；已执行版本由包自动记录在版本日志表，不重复执行，无需手工维护版本表
> **新项目骨架必装**：接入数据库的项目从骨架阶段就引用迁移包、配置脚本目录（`MigrationSql/`）与 `AddSqlMigrationService(...).AddAutoMigration()` 自动迁移入口——迁移框架属于数据层骨架能力，不要求当下已存在业务 SQL；不要因「暂无业务实体」判定可延后，避免启动阶段数据库依赖缺位。
- 已应用的脚本不可修改；结构调整必须新增更高版本号的脚本
- 多副本部署默认配迁移锁（用户明确声明单实例且不扩副本时可省）：锁源按项目现状选——已用 Redis 则 `AddRedisLockProvider(连接串)`（`Azrng.DistributeLock.Redis`），无 Redis 则 `AddDbLockProvider(同库连接串)`（`Azrng.DistributeLock.PostgreSql`，锁表由包在同库自动创建，零新增外部组件）；锁 key 按迁移名（如 `migration_{迁移名}`）。
- 迁移锁关键坑：`ILockProvider.LockAsync` 拿不到锁**返回 null 不抛错**，挂进 `SqlMigrationOption.LockProvider` 时必须 `?? throw`，否则第二副本静默无锁并发迁移；并发启动场景等待超时显式传长（如 5 分钟）让后启动副本排队等首个副本迁完，锁有效期覆盖长脚本。
- 涉及数据的高风险结构变更，建议同步准备回滚脚本并在文档中说明恢复方式
