---
rule_id: backend-agents
version: 1.14.0
last_updated: 2026-08-27
dependencies: [agents-root]
---

# 后端规则

<!-- include: shared/backend/scope.md -->

<!-- include: shared/backend/common-tasks.md -->

---

## 技术栈

<!-- include: shared/backend/stack-backend-v1.md -->

<!-- include: shared/backend/core-libs-v2.md -->

<!-- include: shared/backend/di-rules-v2.md -->

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

<!-- include: shared/backend/response-format-v2.md -->

<!-- include: shared/backend/di-rules-v3.md -->

<!-- include: shared/backend/exception-handling-v2.md -->

<!-- include: shared/backend/redis-cache.md -->

<!-- include: shared/backend/db-migration.md -->

### 代码注释规范
- 必须补 XML 注释的位置：
  - Controller action：用 `<summary>` 说明接口用途、关键参数、返回结果。
  - Application 层服务接口方法：说明应用能力，不写实现细节。
  - Domain 核心业务规则方法：说明规则意图。
  - 不来自接口的实现类自有方法（接口未约束的核心方法）：补 `<summary>` 说明该能力做什么。
- 接口已注释时，实现方法不重复注释，也不加 `/// <inheritdoc />`；只有实现类自有的、接口未定义的方法才需要补注释。
- 默认不写 `<remarks>`：实现类、实现方法上的 `<remarks>` 多为实现细节，按"不写实现细节"原则省略。
  - 例外：Controller action 需要向调用方暴露事件格式、鉴权要求、分页约定等对外契约信息时，可保留精简后的 `<remarks>`，但不写内部实现说明。
- 优先补行内注释的位置：涉及权限、状态流转、事务、缓存、幂等或降级的复杂业务分支，说明“为什么这样做”——包括 fail fast 的原因、为什么跳过某类脏数据、为什么保留某一层结构。
- 不要补的注释：普通属性的 get / set、简单 `if` / `return`、"返回结果"这类复述代码的低价值注释。
- 注释统一使用中文，不在注释里泄露密钥、token、连接串或真实生产地址。

<!-- include: shared/application/anti-patterns.md -->

<!-- include: shared/application/maintainability-check.md -->

---

## 测试规则

<!-- include: shared/backend/pre-commit-regression.md -->

<!-- include: shared/application/general-requirements.md -->

### 后端测试
- 单元测试使用 xUnit + Moq
- Controller 测试：验证状态码、响应结构、参数校验
- Service 测试：验证业务逻辑分支，使用 mock 隔离依赖
- Repository 测试：使用 In-Memory 数据库或 Testcontainers
- 集成测试：使用 WebApplicationFactory 测试完整请求管道

<!-- include: shared/external-dependencies.md -->

<!-- include: shared/no-test-fallback.md -->

---
