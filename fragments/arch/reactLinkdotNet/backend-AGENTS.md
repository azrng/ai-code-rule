---
rule_id: backend-agents
version: 1.18.0
last_updated: 2026-08-30
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

### Application 层规则
- 服务类实现接口，接口定义在 `Interfaces/` 目录
- 服务方法必须是异步的（返回 `Task<T>`）
- 使用 DTO 进行数据传递，不直接暴露 Domain 实体
- 使用 AutoMapper 进行对象映射（如需要）
- 禁止在 Controller、Service、Repository、Entity、DTO 等 C# 类中使用主构造函数（Primary Constructor），统一使用显式构造函数
- 业务异常抛出继承自 `BaseException` 的自定义异常

<!-- include: shared/backend/domain-layer.md -->

<!-- include: shared/backend/infrastructure-layer.md -->

### 代码组织规范
- 一个文件只放一个主类型：Controller、Service 接口与实现、Entity、值对象、DTO 等，默认一个类一个文件，文件名与类名一致。
- DTO 目录组织：应用层 DTO 放 `Application/DTOs/{业务域}/`，Request 与 Response 各自独立文件；领域事件放 `Domain/Events/`，不合并到 `Dtos.cs`。
- 触发拆分的信号：职责混杂、同文件出现多个主类型、参数或字段持续堆叠、方法跨多个不相关业务时，应主动拆分。
- 允许例外：`private` / `internal` 且只服务当前文件的小辅助类型、与主类型强绑定的局部 mapping extension、测试文件中只服务当前测试类的小型 fixture。
- 反模式：`Dtos.cs` 长期堆放十几个 Request / Response；把 Controller 专用 request 内联写在 Controller 文件里；业务逻辑放到 Infrastructure。

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
