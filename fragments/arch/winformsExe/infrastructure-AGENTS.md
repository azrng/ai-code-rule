---
rule_id: infrastructure-agents
version: 1.0.0
last_updated: 2026-11-20
dependencies: [agents-root]
---

# 基础设施规则

<!-- include: shared/infrastructure/scope.md -->

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
<!-- include: shared/infrastructure/data-access-desktop-core.md -->

### 依赖注入与配置规则
- Repository、基础设施服务和外部集成统一通过依赖注入提供
- 不要在 ViewModel、窗体或应用层手动 new 出数据库连接、仓储实现或外部客户端
- 配置项统一通过现有配置体系读取，不要新增平行配置入口
- 用户级配置（托盘常驻应用的数据根、凭据缓存）必须收敛到约定目录（`%APPDATA%\<应用名>` 或等价），禁止散落在程序目录与任意路径
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
- 若项目包含数据库文件、模板资源或本地依赖，需明确复制策略与目标目录；随 exe 分发的模板 / 配置优先嵌入资源（`EmbeddedResource`），避免外部文件丢失导致双击启动失败
- Windows / PowerShell 下构建、发布与安装脚本统一使用 `.bat` / `.ps1` 脚本承载，不依赖 shell 链式操作符

### 发布规则
<!-- include: shared/desktop/release.md -->
- 自包含单文件发布（`-p:SelfContained=true`）与框架依赖发布二选一并在发布脚本中固化，产物体积与目标机运行时要求在交付说明中写明
- 需要安装包的桌面应用：安装包脚本（Inno Setup / MSIX 等）纳入版本控制，升级安装必须保留用户配置与数据目录
- 发布链路要可追踪，至少能定位版本号、构建时间和核心变更来源

<!-- include: shared/desktop/release-delivery.md -->

---

## 测试规则

<!-- include: shared/common/general-requirements.md -->

### 基础设施测试
<!-- include: shared/desktop/repository-regression.md -->
- 配置绑定、迁移执行、连接初始化等关键基础设施逻辑发生变化时，应补充验证
- 若无法做自动化测试，至少补充一次可复现的手工验证步骤

<!-- include: shared/external-dependencies.md -->

<!-- include: shared/no-test-fallback.md -->

<!-- include: shared/infrastructure/pre-commit-regression.md -->

---
