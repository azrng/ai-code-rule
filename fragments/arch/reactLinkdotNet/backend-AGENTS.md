---
rule_id: backend-agents
version: 1.21.0
last_updated: 2026-08-31
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

```text
backend/
├── src/
│   ├── YourProject.API/                 # 表现层
│   │   ├── Controllers/                 # API 控制器
│   │   ├── Middleware/                  # 自定义中间件
│   │   ├── Filters/                     # 动作过滤器
│   │   ├── MigrationSql/                # SQL 迁移脚本（版本号.sql / 版本号.txt）
│   │   ├── Program.cs                   # 应用入口
│   │   ├── appsettings.json             # 配置文件
│   │   └── YourProject.API.csproj
│   ├── YourProject.Application/         # 应用层
│   │   ├── Services/                    # 应用服务接口与实现
│   │   ├── DTOs/                        # 数据传输对象
│   │   ├── Interfaces/                  # 应用服务接口
│   │   ├── Mappings/                    # AutoMapper 配置
│   │   └── YourProject.Application.csproj
│   ├── YourProject.Domain/              # 领域层（核心业务模型）
│   │   ├── Entities/                    # 领域实体
│   │   ├── ValueObjects/                # 值对象
│   │   ├── Enums/                       # 枚举
│   │   ├── Events/                      # 领域事件
│   │   ├── Exceptions/                  # 领域异常
│   │   └── YourProject.Domain.csproj
│   └── YourProject.Infrastructure/      # 基础设施层
│       ├── Data/
│       │   ├── Context/                 # DbContext
│       │   ├── Configurations/          # EF Core 配置
│       │   └── Repositories/            # 仓储实现
│       ├── Services/                    # 外部服务实现
│       └── YourProject.Infrastructure.csproj
├── tests/
│   ├── YourProject.UnitTests/
│   └── YourProject.IntegrationTests/
├── Dockerfile
└── backend.sln
```

---

## 后端规则（Clean Architecture）

<!-- include: shared/backend/layering-rules.md -->

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

<!-- include: shared/backend/backend-testing.md -->

<!-- include: shared/external-dependencies.md -->

<!-- include: shared/no-test-fallback.md -->

---
