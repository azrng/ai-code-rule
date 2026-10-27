---
rule_id: backend-agents
version: 1.21.0
last_updated: 2026-08-31
dependencies: [agents-root]
---

# 后端规则

<!-- include: shared/backend/scope.md -->

---

<!-- include: shared/application/tech-choice.md -->

---

<!-- include: shared/backend/advisory.md -->

---

## 技术栈

<!-- include: shared/backend/stack-backend-dotnet.md -->

<!-- include: shared/backend/core-libs.md -->

<!-- include: shared/backend/di-rules.md -->

---

### 阶段 2 — 后端实现（后端实现角色主导）

**触发条件**：用户发出「开始后端开发」指令

**入场要求**：前端页面已完成，接口需求已明确

**工作内容**：
1. 按照接口需求实现 ASP.NET Core Web API 接口
2. 遵循 `Controller → Application → Domain → Infrastructure` 分层架构
3. 涉及数据库结构变更时，使用 Azrng.SqlMigration 生成迁移脚本
4. 每个接口必须可通过 Swagger 或 Postman 独立验证

**字段命名约定**：
- C# 属性：PascalCase
- JSON 输出：camelCase（通过 System.Text.Json 配置自动转换）
- 数据库字段：snake_case（通过 EF Core 命名约定配置）

**门控规则**：
- 核心接口独立测试（Swagger / Postman）通过后，才允许进入阶段 3

---

## 后端规则（Clean Architecture）

<!-- include: shared/backend/layering-rules.md -->

### 层内目录组织

各层内部按职责与类型组织；公开契约类（DTO、实体、接口等）默认一个类一个文件，文件名与类名一致。

| 类型 | 所在层 / 目录 | 命名约定 |
| --- | --- | --- |
| Controller | `API/Controllers/` | `{资源}Controller.cs` |
| 应用服务接口 | `Application/Interfaces/` | `I{业务域}Service.cs` |
| 应用层 DTO | `Application/DTOs/{业务域}/` | `CreateOrderRequest.cs`、`OrderDetailResponse.cs` |
| 应用服务实现 | `Application/Services/` | `{业务域}Service.cs` |
| 实体 | `Domain/Entities/` | `{实体名}.cs` |
| 值对象（record） | `Domain/ValueObjects/` | `{名称}.cs` |
| 枚举 | `Domain/Enums/` | `{枚举名}.cs` |
| DbContext / EF 配置 | `Infrastructure/Data/` | `{项目名}DbContext.cs`、`{实体名}Configuration.cs` |
| 仓储实现 | `Infrastructure/Data/Repositories/` | `{实体名}Repository.cs` |
| SQL 迁移脚本 | `API/MigrationSql/` | `版本号.sql` / `版本号.txt`（如 `1.0.0.sql`、`1.1.0.txt`，不含描述文字） |

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

<!-- include: shared/backend/time-contract-rules.md -->

<!-- include: shared/application/anti-patterns.md -->

<!-- include: shared/application/maintainability-check.md -->

---

## 测试规则

<!-- include: shared/backend/pre-commit-regression.md -->

<!-- include: shared/common/general-requirements.md -->

<!-- include: shared/backend/logging-rules.md -->

<!-- include: shared/backend/server-side-authorization.md -->

<!-- include: shared/backend/backend-testing.md -->

<!-- include: shared/backend/dotnet-build-isolation.md -->

<!-- include: shared/external-dependencies.md -->

<!-- include: shared/no-test-fallback.md -->

---
