---
rule_id: backend-agents
version: 1.55.0
last_updated: 2026-09-06
dependencies: [agents-root]
---

# 后端规则

<!-- include: shared/backend/scope.md -->

<!-- include: shared/backend/common-tasks.md -->

---

## 技术栈

<!-- include: shared/backend/stack-backend-dotnet.md -->

<!-- include: shared/backend/core-libs.md -->

<!-- include: shared/backend/di-rules.md -->

---

<!-- include: shared/backend/advisory.md -->

## 推荐目录结构

- 后端目录建议聚焦在 `backend/` 下组织，优先复用现有结构，不强制迁移。
- 单项目整洁架构：一个 Web API 项目内以文件夹划分层，层职责与依赖方向和多项目结构保持一致。

```text
backend/
├── src/
│   └── YourProject/                 # 单项目整洁架构（以文件夹划分层）
│       ├── Controllers/             # API 控制器（HTTP 编排，不含业务逻辑）
│       ├── Application/             # 应用层
│       │   ├── Services/            # 应用服务实现
│       │   ├── Interfaces/          # 应用服务接口
│       │   ├── DTOs/                # 数据传输对象（按业务域子目录）
│       │   └── Mappings/            # AutoMapper 配置
│       ├── Domain/                  # 领域层（核心业务模型）
│       │   ├── Entities/            # 领域实体
│       │   ├── ValueObjects/        # 值对象
│       │   ├── Enums/               # 枚举
│       │   ├── Events/              # 领域事件
│       │   └── Exceptions/          # 领域异常
│       ├── Infrastructure/          # 基础设施层
│       │   ├── Data/
│       │   │   ├── Context/         # DbContext
│       │   │   ├── Configurations/  # EF Core 配置
│       │   │   └── Repositories/    # 仓储实现
│       │   └── Services/            # 外部服务实现
│       ├── Middleware/              # 自定义中间件（按需）
│       ├── Filters/                 # 动作过滤器
│       ├── MigrationSql/            # SQL 迁移脚本（版本号.sql / 版本号.txt）
│       ├── Program.cs               # 应用入口
│       ├── appsettings.json         # 配置文件
│       └── YourProject.csproj
├── tests/
│   ├── YourProject.UnitTests/
│   └── YourProject.IntegrationTests/
├── Dockerfile
└── backend.sln
```

---

## 后端规则（Clean Architecture）

<!-- include: shared/backend/layering-rules.md -->

### 依赖方向约定（单项目关键防线）

单项目内没有项目引用做编译期隔离，依赖方向完全靠约定维持。AI 每次新增或修改类时必须自查引用方向：

- `Domain/`：无外部依赖，只包含核心业务模型；禁止引用 `Application/`、`Infrastructure/`、`Controllers/` 的类型
- `Application/`：依赖 Domain，定义服务接口和 DTO；禁止引用 `Controllers/`、`Middleware/`，禁止出现 `HttpContext` 等 HTTP 相关类型
- `Infrastructure/`：依赖 Application 和 Domain，实现具体技术细节；禁止被 Domain 引用
- `Controllers/`：只依赖 Application 的服务接口；禁止直接操作 DbContext 或仓储实现

### 按需使用与演进

- 小型 CRUD 不强制建满所有文件夹：`Events/`、`Exceptions/`、`Middleware/`、`Filters/`、外部服务实现都按需出现，不为架构仪式感创建空文件夹
- 出现以下信号时，考虑拆分为多项目结构（多宿主如 Web API + Worker、领域逻辑需要被其他服务复用、团队并行开发频繁冲突、需要编译期强制依赖方向）：文件夹与多项目各层一一对应（`Application/` → Application、`Domain/` → Domain、`Infrastructure/` → Infrastructure、`Controllers/` → API），拆分时可平滑迁移

<!-- include: shared/backend/controller-layer.md -->

<!-- include: shared/backend/application-layer.md -->

<!-- include: shared/backend/domain-layer.md -->

<!-- include: shared/backend/infrastructure-layer.md -->

<!-- include: shared/backend/code-organization-dotnet.md -->

<!-- include: shared/application/kiss.md -->

<!-- include: shared/backend/data-access-rules.md -->

<!-- include: shared/backend/response-format.md -->

<!-- include: shared/backend/exception-handling.md -->

<!-- include: shared/backend/redis-cache.md -->

<!-- include: shared/backend/db-migration.md -->

<!-- include: shared/backend/comment-rules.md -->

<!-- include: shared/application/anti-patterns.md -->

<!-- include: shared/application/maintainability-check.md -->

---

## 测试规则

<!-- include: shared/backend/pre-commit-regression.md -->

<!-- include: shared/common/general-requirements.md -->

<!-- include: shared/backend/backend-testing.md -->

<!-- include: shared/external-dependencies.md -->

<!-- include: shared/no-test-fallback.md -->

---
