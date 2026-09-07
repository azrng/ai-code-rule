---
rule_id: infrastructure-agents
version: 1.60.0
last_updated: 2026-09-07
dependencies: [agents-root]
---

# 基础设施规则

<!-- include: shared/infrastructure/scope.md -->

### 常见任务入口
- 改数据访问、数据库结构或迁移：先看 Repository 层规则、数据访问规则与数据库迁移规则
- 改配置、连接串或环境依赖：先看配置与环境规则
- 改本地数据、文件路径或隐私数据处理：先看本地数据安全规则
- 改打包发布：先看发布规则与发布交付规则
- 补基础设施回归：先看 `提交前最小回归`

---

## 技术栈

### 数据与配置
- 数据访问：Dapper + SQLite
- 数据库：SQLite（本地默认）/ PostgreSQL（按项目需要扩展）
- 依赖注入：`Microsoft.Extensions.DependencyInjection`

### 发布
- 桌面应用打包：单文件发布
- 版本、发布目录和运行命令必须可追踪

### 依赖注入规范
- 使用 Azrng 标记接口声明服务生命周期（标记接口由 `Azrng.Core` 提供）：
  - `ITransientDependency`：临时注入
  - `IScopedDependency`：作用域注入
  - `ISingletonDependency`：单例注入
- 服务默认显式注册：在 `Program.cs` 中按标记接口对应生命周期调用 `AddTransient` / `AddScoped` / `AddSingleton`，注册方式与标记声明必须一致
- 批量扫描注册 `services.RegisterBusinessServices(...)` 由 `Azrng.AspNetCore.Core` 提供，属 Web 项目装配；桌面项目默认仅引用 `Azrng.Core`，不存在该扩展方法，禁止照抄

<!-- include: shared/application/tech-choice.md -->

---

<!-- include: shared/infrastructure/advisory.md -->

---

## 推荐目录结构

- 基础设施目录建议聚焦在 `src/AppName/Repositories/`、`src/AppName/Data/`、`src/AppName/Program.cs` 附近组织，优先复用现有结构，不强制迁移。

```text
src/AppName/
├── Repositories/
│   ├── Interfaces/           # 仓储接口定义
│   └── Implementations/      # 仓储实现
├── Data/
│   ├── Context/              # 数据库上下文 / 连接工厂
│   └── Migrations/           # 数据库迁移脚本
├── Helpers/                  # 基础设施工具类
└── Program.cs                # 依赖注入与启动配置
```

---

## 基础设施规则

### Repository 层规则
- 仓储类必须实现接口，接口定义在 `Repositories/Interfaces/` 目录。
- 仓储实现负责具体数据访问，不把 SQL、连接串或事务细节泄漏到 ViewModel / Service。
- 仓储方法必须优先采用异步形式，并支持取消令牌。
- 涉及事务时，使用 `IDbTransaction` 或等价机制统一管理。
- 仓储类实现 `ITransientDependency` / `IScopedDependency` / `ISingletonDependency` 接口表达生命周期，在 `Program.cs` 中按对应生命周期显式注册。

### 数据访问规则
- 使用 Dapper 进行数据访问。
- SQL 查询必须使用参数化查询，禁止字符串拼接构造可注入 SQL。
- 数据库连接字符串从配置文件读取，禁止硬编码。
- 数据库连接必须正确管理生命周期。
- 数据库存储字段建议使用 `snake_case`，映射通过 `Column` 特性或显式映射处理。

<!-- include: shared/desktop/db-migration.md -->

### 配置与环境规则
- 运行路径、数据库文件路径、连接串、环境依赖必须通过配置管理，禁止散落在代码常量中。
- 本地默认配置应尽量可直接运行，同时避免把密钥、真实凭据写入仓库。
- 新增配置项、命令或环境依赖时，必须同步更新相关文档。

### 本地数据安全规则
- 只读取和修改当前任务明确相关的本地文件、配置、数据库和导出目录；不得扫描或处理无关用户目录。
- 日志、错误信息、开发记录和提交说明不得写入密钥、真实凭据、连接串、个人隐私数据或完整敏感路径。
- 涉及本地数据库、用户配置、缓存、导出文件或发布产物清理时，必须先确认影响范围、备份方式和恢复方案。
- 迁移或初始化逻辑应保留可重复执行和可追踪记录，避免依赖人工准备的本地状态。

---

## 发布规则

<!-- include: shared/desktop/release.md -->

<!-- include: shared/desktop/release-delivery.md -->

---

## 测试与验证规则

<!-- include: shared/desktop/repository-regression.md -->

<!-- include: shared/no-test-fallback.md -->

<!-- include: shared/infrastructure/pre-commit-regression.md -->

---
