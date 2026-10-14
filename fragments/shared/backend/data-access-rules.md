### 数据访问规则
- 使用 Entity Framework Core 进行数据访问
- DbContext 通过构造函数注入
- 使用 LINQ 查询，避免原生 SQL
- 涉及事务时使用 `IDbContextTransaction`
- 软删除统一使用实体基类的 `Deleted` 字段；包不自动做全局过滤，默认在查询中显式过滤 `!x.Deleted`，或在实体配置类中统一配置查询过滤器
- 新增实体必须三处同步：实体类、DbContext 的 `DbSet<{实体名}>`、对应 `{实体名}Etc.cs` 实体配置类；表名以配置类 `ToTable` 为准，迁移 SQL 表名必须与之一致
