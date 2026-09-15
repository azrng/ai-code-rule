---
rule_id: infrastructure-agents
version: 1.29.0
last_updated: 2026-09-01
dependencies: [agents-root]
---

# 基础设施规则

<!-- include: shared/infrastructure/scope.md -->

### 常见任务入口
- 改数据访问、数据库或迁移：先看 Repository 层规则、数据访问规则与迁移与持久化规则
- 改依赖注入或配置：先看依赖注入与配置规则
- 改构建或发布：先看构建规则与发布交付规则
- 补基础设施回归：先看 `提交前最小回归`

---

<!-- include: shared/application/tech-choice.md -->

---

<!-- include: shared/infrastructure/advisory.md -->

---

## 基础设施规则

### Repository 层规则（可选，仅在使用数据库时）
- 仓储类必须实现接口，接口定义在 `Repositories/Interfaces/` 目录
- 仓储只负责数据访问与查询拼装，不承担业务流程编排
- 仓储方法必须是异步的，支持取消令牌
- 涉及事务时，使用 `IDbTransaction` 或等价事务边界统一管理
- 仓储实现注册到 DI 容器时，统一复用现有批量注册方式

### 数据访问规则（可选，仅在使用数据库时）
- 数据库连接字符串从配置文件读取，禁止硬编码
- 数据库连接必须使用 `using` 或等价机制管理生命周期
- Dapper 映射通过 `Column` 特性或集中式映射处理
- 所有查询必须使用参数化查询，禁止拼接 SQL
- 数据库字段命名使用 snake_case

### 依赖注入与配置规则
- Repository、基础设施服务和外部集成统一通过依赖注入提供
- 不要在 ViewModel、页面或应用层手动 new 出数据库连接、仓储实现或外部客户端
- 配置项统一通过现有配置体系读取，不要新增平行配置入口
- 若新增外部服务接入，必须补充对应配置说明与默认占位

### 迁移与持久化规则（可选，仅在使用数据库时）
- 涉及数据库结构变更时，同步生成并维护迁移脚本
- 迁移脚本必须进入版本控制，不允许只改实体或 SQL 而不补迁移说明
- 若包含初始化数据、回填逻辑或数据修复，必须在提交说明中说明影响范围
- 迁移执行顺序、回滚方式和版本号命名要保持一致

---

## 构建与发布规则

### 构建规则
- 优先复用现有解决方案、发布配置和构建脚本
- 构建前确认引用、配置文件、资源文件和迁移脚本已被正确纳入输出
- 若项目包含数据库文件、模板资源或本地依赖，需明确复制策略与目标目录

<!-- include: shared/desktop/release.md -->
- 发布链路要可追踪，至少能定位版本号、构建时间和核心变更来源

<!-- include: shared/desktop/release-delivery.md -->

---

## 测试规则

<!-- include: shared/common/general-requirements.md -->

### 基础设施测试
- Repository 层 SQL 查询、数据映射、事务处理发生变化时，应补充对应测试
- 配置绑定、迁移执行、连接初始化等关键基础设施逻辑发生变化时，应补充验证
- 若无法做自动化测试，至少补充一次可复现的手工验证步骤

### 外部依赖与数据
- 测试中不要真实调用不可控外部服务，统一使用 mock、stub 或本地替身
- 测试数据应尽量最小化、可读、可重复执行
- 不要让测试依赖本地人工状态或手工残留数据库

<!-- include: shared/no-test-fallback.md -->

<!-- include: shared/infrastructure/pre-commit-regression.md -->

---
