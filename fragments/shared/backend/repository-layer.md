### Repository 层规则
- 使用 GORM 进行数据访问
- 数据库连接通过依赖注入传入
- 涉及事务时使用 `db.Transaction()`
- 复杂查询使用 GORM Scope 封装
