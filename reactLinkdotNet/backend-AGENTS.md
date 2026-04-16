---
rule_id: backend-agents
version: 1.2.0
last_updated: 2026-04-16
dependencies: [agents-root]
---

# AGENTS.md

## 适用范围

- 作用域：服务端实现、接口、服务、数据访问、缓存、异常处理与后端测试
- 触发场景：涉及接口、Service、Repository、缓存、权限、异常处理或测试时阅读

### 阅读摘要
- 建议阅读：改接口、改 Service、改数据访问、改缓存、改权限、补后端验证
- 可先跳过：纯页面样式、纯文档整理、仅构建或部署配置调整
- 优先查看：服务端规则、数据访问 / 异常处理规范、测试规则

### 常见任务入口
- 改接口响应或状态码：先看接口 / 路由规则与异常处理规范
- 改业务流程或状态流转：先看 Service / 应用层规则与权限约束
- 改查询、分页、缓存、迁移：先看数据访问规则与验证要求
- 补服务端回归：先看 `提交前最小回归` 与测试规则

---

## 技术栈

### 后端
- .NET 10 (2025 年已经发布正式版) + ASP.NET Core Web API
- Clean Architecture 分层架构：
  - `API`（表现层）→ `Application`（应用层）→ `Domain`（领域层）← `Infrastructure`（基础设施层）
- ORM：Entity Framework Core
- 测试框架：xUnit + Moq

### 核心库
- `Azrng.Core` (1.15.8)：基础类库（实体基类、扩展方法、工具类、结果包装、异常体系）
- `Azrng.Core.Json` (1.3.1)：JSON 序列化（基于 System.Text.Json）
- `Azrng.AspNetCore.Core` (1.3.0)：API 基础设施（统一响应、模型校验、异常中间件）
- `Azrng.SqlMigration` (0.4.0)：SQL 脚本迁移执行
- `Common.Cache.Redis` (2.0.0)：Redis 缓存封装（基于 StackExchange.Redis）

### 依赖注入规范
- 使用 Azrng 标记接口方式：
  - `ITransientDependency`：临时注入
  - `IScopedDependency`：作用域注入
  - `ISingletonDependency`：单例注入
- 在 Program.cs 中调用 `services.RegisterBusinessServices(assemblies)` 扫描并注册服务
- 服务类实现对应标记接口，自动被扫描注册到 DI 容器

如果仓库已经有真实实现，以现有代码为准，不要强行重构或替换技术栈。

**技术债务评估框架**：
- **沿用现有实现**：代码可正常运行、无明显性能问题、维护成本可接受、团队熟悉度高
- **必须重构**：存在安全漏洞、严重影响性能、阻碍新功能开发、维护成本过高
- **可选重构**：代码风格不一致、存在更好的替代方案、但不紧急
- **禁止重构**：仅为个人偏好、追赶技术潮流、非关键路径的过度优化

**重构决策流程**：
1. 先评估现有代码的技术债务等级（高 / 中 / 低）。
2. 判断重构的紧急程度和业务价值。
3. 评估重构风险和工作量。
4. 高风险或大规模重构需先与团队确认。


---

## 推荐目录结构

- 后端目录建议聚焦在 `backend/` 下组织，优先复用现有结构，不强制迁移。

```text
backend/
├── src/
│   ├── YourProject.API/                 # 表现层
│   │   ├── Controllers/                 # API 控制器
│   │   ├── Middleware/                  # 自定义中间件
│   │   ├── Filters/                     # 动作过滤器
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
│       │   ├── Migrations/              # 迁移文件
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

### 分层依赖规则
```
API → Application → Domain ← Infrastructure
                ↑_______________|
```
- `Domain` 层：无外部依赖，包含核心业务模型
- `Application` 层：依赖 Domain，定义服务接口和 DTO
- `API` 层：依赖 Application，处理 HTTP 请求
- `Infrastructure` 层：依赖 Application 和 Domain，实现具体技术细节

### Controller 层规则
- Controller 只负责 HTTP 处理，不包含业务逻辑
- 使用 `[ApiController]` 特性，自动处理 400 响应
- 使用 `[Route("api/[controller]")]` 统一路由前缀
- 注入 Application 层的服务接口，不直接依赖 Infrastructure
- 统一使用 `ActionResult<T>` 或 `IActionResult` 返回类型

### Application 层规则
- 服务类实现接口，接口定义在 `Interfaces/` 目录
- 服务方法必须是异步的（返回 `Task<T>`）
- 使用 DTO 进行数据传递，不直接暴露 Domain 实体
- 使用 AutoMapper 进行对象映射（如需要）
- 禁止在 Controller、Service、Repository、Entity、DTO 等 C# 类中使用主构造函数（Primary Constructor），统一使用显式构造函数
- 业务异常抛出继承自 `BaseException` 的自定义异常

### Domain 层规则
- 实体类继承自基类（包含 Id、创建时间等公共属性）
- 值对象使用 record 类型，实现值相等比较
- 领域事件继承自 `INotification`（MediatR）
- 禁止依赖外部库，保持纯净

### Infrastructure 层规则
- DbContext 实现数据上下文，配置连接和映射
- 使用 EF Core Migrations 管理数据库结构变更
- 仓储实现依赖注入，遵循仓储模式
- 外部服务（邮件、文件存储等）实现在此层

### 数据访问规则
- 使用 Entity Framework Core 进行数据访问
- DbContext 通过构造函数注入
- 使用 LINQ 查询，避免原生 SQL
- 涉及事务时使用 `IDbContextTransaction`
- 软删除通过全局查询过滤器实现

### 统一响应格式
- 所有 API 响应统一使用 `ResultModel<T>` 包装
- 成功响应：`ResultModel<T>.Success(data)`
- 错误响应：`ResultModel<T>.Failure(message, errorCode)`
- 状态码：200 表示成功，其他表示各类错误
- 异常处理由 Azrng.AspNetCore.Core 中间件统一捕获

### 依赖注入规范
- 服务类必须实现以下接口之一：
  - `ITransientDependency`：每次请求创建新实例
  - `IScopedDependency`：每个请求作用域内单例
  - `ISingletonDependency`：应用程序生命周期单例
- Controller 通过构造函数注入服务
- 禁止使用静态服务定位器 pattern

### 异常处理规范
- 业务异常继承 `BaseException` 或其子类：
  - `LogicBusinessException`：业务逻辑异常
  - `ParameterException`：参数校验异常
  - `NotFoundException`：资源不存在
  - `ForbiddenException`：禁止访问
  - `InternalServerException`：服务器内部错误
- 异常由中间件统一捕获并转换为 `ResultModel` 响应
- 禁止在 Controller 外部直接返回错误状态码

### Redis 缓存使用规范
- 使用 `Common.Cache.Redis` 封装的 `IRedisProvider` 接口
- 通过 `services.AddRedisCacheStore()` 注册 Redis 服务
- 缓存操作必须设置合理的过期时间
- 热点数据使用本地缓存 + Redis 二级缓存
- 发布订阅场景使用 `PublishAsync`/`SubscribeAsync`
- 禁止在循环中频繁调用 Redis

### 数据库迁移规则
- 使用 `Azrng.SqlMigration` 进行数据库脚本迁移
- 迁移脚本放在 `Migrations/` 目录，按版本号命名
- 迁移脚本必须幂等，可重复执行
- 应用启动时自动执行迁移（可选配置）
- 回滚脚本必须与正向脚本配套提供
- 每次迁移后更新数据库版本记录表

---

## 测试规则

### 提交前最小回归
- 默认执行：项目现有的静态检查、类型检查或编译校验
- 业务逻辑改动：至少补一项业务规则、权限判断或状态流转验证
- 接口 / 路由 / 控制器改动：至少补一项输入输出、状态码或异常分支验证
- 数据访问、缓存、迁移改动：至少补一项查询结果、缓存行为、事务或迁移同步验证
- 若仓库暂时缺少自动化测试基础，可使用接口调用、页面联调、脚本或命令行验证替代，但必须能证明核心逻辑真实生效

### 总体要求
- 影响行为的改动应优先补充或更新测试
- 若本次改动未补测试，必须在最终说明中写明原因和风险
- 测试应覆盖真实业务行为

### 后端测试
- 单元测试使用 xUnit + Moq
- Controller 测试：验证状态码、响应结构、参数校验
- Service 测试：验证业务逻辑分支，使用 mock 隔离依赖
- Repository 测试：使用 In-Memory 数据库或 Testcontainers
- 集成测试：使用 WebApplicationFactory 测试完整请求管道

### 外部依赖与数据
- 测试中不要真实调用第三方服务，统一使用 mock
- 测试数据应尽量最小化、可读、可重复执行
- 不要让测试依赖本地人工状态

### 无法执行测试时
- 必须说明未执行的测试类型
- 必须说明未执行原因
- 必须说明潜在影响范围和风险

---

文件结束。
