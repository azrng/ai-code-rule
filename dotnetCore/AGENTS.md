# AGENTS.md

## 目标
职责划分如下：
- `AGENTS.md` / `CLAUDE.md` / `GEMINI.md`：各 AI Agent 统一工作规则、任务管理机制、交付要求，内容保持同步
- `TASK.md`：具体任务内容与任务进度

---

## 注意事项
1. 你需要理解用户提出的需求的业务逻辑然后再做设计和开发
2. 每个接口必须可通过 Swagger 独立验证，返回真实数据
3. 数据变更操作（增、删、改）必须真实生效，刷新 Swagger 后能看到变化
4. 接口设计遵循 RESTful 风格，命名清晰，职责单一

---

## 技术栈

### 后端
- .NET 10 (2025 年已发布正式版) + ASP.NET Core Web API
- 单项目结构（不分层），按文件夹组织代码

### 核心库
- `Azrng.Core`：基础类库（实体基类、扩展方法、工具类、结果包装、异常体系）
- `Azrng.Core.Json`：JSON 序列化（基于 System.Text.Json）
- `Azrng.AspNetCore.Core`：API 基础设施（统一响应、模型校验、异常中间件）
- `Azrng.Swashbuckle`：Swagger 扩展（简化配置、自动 XML 注释、JWT 认证支持）
- `Azrng.SqlMigration`：SQL 脚本迁移执行
- `Common.Cache.Redis`：Redis 缓存封装（基于 StackExchange.Redis）
- `Common.Cache.MemoryCache`：内存缓存封装（基于 IMemoryCacheProvider）
- `Common.HttpClients`：HTTP 客户端封装（基于 IHttpClientFactory，支持依赖注入和弹性策略）

### 数据库访问库
- `Common.EFCore`：EF Core 基础封装（必须安装，所有 Provider 都依赖它）
- 根据项目使用的数据库，按需选择以下 **其中一个** Provider 包：
  - `Common.EFCore.PostgreSql`：PostgreSQL（默认推荐）
  - `Common.EFCore.MySQL`：MySQL / MariaDB
  - `Common.EFCore.SQLServer`：SQL Server
  - `Common.EFCore.SQLite`：SQLite
  - `Common.EFCore.InMemory`：内存数据库（仅用于测试）

### 依赖注入规范
- 使用 Azrng 标记接口方式：
  - `ITransientDependency`：临时注入
  - `IScopedDependency`：作用域注入
  - `ISingletonDependency`：单例注入
- 在 Program.cs 中调用 `services.RegisterBusinessServices(assemblies)` 扫描并注册服务
- 服务类实现对应标记接口，自动被扫描注册到 DI 容器

### 数据库
- 默认使用 PostgreSQL
- 可根据项目需求切换为其他数据库（MySQL、SQL Server、SQLite 等）
- ORM：Entity Framework Core，通过 `Common.EFCore` 系列包访问
- 测试环境可使用 `Common.EFCore.InMemory` 内存数据库

### 部署
- Docker + Docker Compose

### 默认端口
| 服务                 | 容器内端口 | 宿主机映射端口 |
| -------------------- | ---------- | -------------- |
| 后端（.NET）         | 8080       | 8080           |
| 数据库（PostgreSQL） | 5432       | 5432           |

- 若宿主机端口冲突，优先调整 Compose 映射端口（宿主机侧），容器内端口保持不变

如果仓库已经有真实实现，以现有代码为准，不要强行重构或替换技术栈。

---

## 推荐目录结构
若仓库尚未形成稳定结构，可优先参考以下组织方式；若仓库已有实现，以现状为准，不强制迁移。

```text
project-root/
├── src/
│   └── YourProject/                     # 单个 Web API 项目
│       ├── Controllers/                 # API 控制器
│       ├── Services/                    # 业务服务接口与实现
│       │   └── Interfaces/              # 服务接口定义
│       ├── Models/                      # 数据模型
│       │   ├── Entities/                # 数据库实体
│       │   ├── DTOs/                    # 数据传输对象
│       │   └── Enums/                   # 枚举
│       ├── Data/                        # 数据访问
│       │   ├── Context/                 # DbContext
│       │   └── Configurations/          # EF Core 实体配置
│       ├── Middleware/                  # 自定义中间件
│       ├── Filters/                     # 动作过滤器
│       ├── Extensions/                  # 扩展方法
│       ├── Migrations/                  # 数据库迁移脚本
│       ├── Program.cs                   # 应用入口
│       ├── appsettings.json             # 配置文件
│       └── YourProject.csproj
├── tests/
│   └── YourProject.Tests/               # 测试项目
├── doc/
│   ├── devlog/
│   ├── design/
│   └── requirement.md
├── docker-compose.yml
├── .env.example
├── .gitignore
├── AGENTS.md
├── CLAUDE.md
├── GEMINI.md
└── TASK.md
```

---

## 使用方式
每个 AI Agent 在开始修改前都必须：
1. 先阅读本文档，理解核心规则和流程
2. 再阅读相关模块和现有实现
3. 优先复用当前结构，再决定是否新增文件

进行后端开发时：
1. Controller 只负责 HTTP 处理，业务逻辑放在 Service 中
2. 服务类实现 `ITransientDependency`/`IScopedDependency`/`ISingletonDependency` 接口
3. 统一响应使用 `ResultModel<T>` 包装
4. 异常处理使用 Azrng 异常体系

进行部署改动时：
1. 优先复用现有 Dockerfile 和 Compose 链路
2. 镜像 tag 必须可追踪
3. 清理旧镜像前要保留可回滚版本

---

## 核心规则
- 先理解，再修改。
- 先复用，再新增。
- 交付必须可直接使用，不能只停留在演示层。
- 不允许新增平行配置体系。
- 不做与当前任务无关的重构。
- 所有改动都必须可说明、可验证。

---

## 后端规则

### Controller 规则
- Controller 只负责 HTTP 处理，不包含业务逻辑
- 使用 `[ApiController]` 特性，自动处理 400 响应
- 使用 `[Route("api/[controller]")]` 统一路由前缀
- 通过构造函数注入 Service，禁止在 Controller 中直接操作 DbContext
- 统一使用 `ActionResult<T>` 或 `IActionResult` 返回类型
- 所有响应统一使用 `ResultModel<T>` 包装

### Service 规则
- 服务类实现接口，接口定义在 `Services/Interfaces/` 目录
- 服务方法必须是异步的（返回 `Task<T>`）
- 使用 DTO 进行数据传递，不直接暴露实体
- 业务异常抛出继承自 Azrng 异常体系的自定义异常
- 服务类实现标记接口（`ITransientDependency`/`IScopedDependency`/`ISingletonDependency`）以自动注册

### 数据模型规则
- 实体类继承自 Azrng 基类（包含 Id、创建时间等公共属性）
- DTO 按用途区分：查询参数、创建请求、更新请求、响应结果
- 枚举放在 `Models/Enums/` 目录
- 禁止在 DTO 中暴露数据库内部字段

### 数据访问规则
- 使用 Entity Framework Core 进行数据访问
- DbContext 通过构造函数注入
- 使用 LINQ 查询，避免原生 SQL
- 涉及事务时使用 `IDbContextTransaction`
- 软删除通过全局查询过滤器实现
- EF Core 实体配置放在 `Data/Configurations/` 目录

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
- 禁止手动在 Program.cs 中逐一注册服务，应使用 `RegisterBusinessServices` 批量扫描注册

### 异常处理规范
- 业务异常继承 Azrng 异常体系：
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

### 内存缓存使用规范
- 使用 `Common.Cache.MemoryCache` 封装的 `IMemoryCacheProvider` 接口
- 通过 `services.AddMemoryCacheStore()` 注册内存缓存服务
- 适用于单实例部署、不需要分布式共享的缓存场景
- 可通过 `MemoryConfig` 配置默认过期时间（默认 5 秒）等参数
- 与 Redis 搭配使用时作为一级缓存，Redis 作为二级缓存

### 数据库迁移规则
- 使用 `Azrng.SqlMigration` 进行数据库脚本迁移
- 迁移脚本放在 `Migrations/` 目录，按版本号命名
- 迁移脚本必须幂等，可重复执行
- 应用启动时自动执行迁移（可选配置）
- 回滚脚本必须与正向脚本配套提供
- 每次迁移后更新数据库版本记录表

### Swagger 规范
- 使用 `Azrng.Swashbuckle` 包，禁止直接引用 Swashbuckle.AspNetCore
- 服务注册：`services.AddDefaultSwaggerGen(title: "项目名称", showJwtToken: true)`
- 中间件启用：`app.UseDefaultSwagger(onlyDevelopmentEnabled: true)`
- 项目必须启用 XML 文档生成：`<GenerateDocumentationFile>true</GenerateDocumentationFile>`
- Controller 和公开方法必须添加 XML 注释，Swagger 会自动展示
- 接口需通过 Swagger UI 独立验证，确保请求/响应结构正确

### 字段命名约定
- C# 属性：PascalCase
- JSON 输出：camelCase（通过 System.Text.Json 配置自动转换）
- 数据库字段：snake_case（通过 EF Core 命名约定配置）

---

## 测试规则

### 总体要求
- 影响行为的改动应优先补充或更新测试
- 若本次改动未补测试，必须在最终说明中写明原因和风险
- 测试应覆盖真实业务行为

### 后端测试
- 测试框架：xUnit + Moq
- Controller 测试：验证状态码、响应结构、参数校验
- Service 测试：验证业务逻辑分支，使用 mock 隔离依赖
- 数据访问测试：使用 In-Memory 数据库或 Testcontainers
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

## Git 与提交流程

### 分支命名
- `feat/<desc>`：新功能
- `fix/<desc>`：缺陷修复
- `refactor/<desc>`：无行为变更的重构
- `chore/<desc>`：工具、依赖、配置调整
- `docs/<desc>`：仅文档变更

### Commit 规范
- 提交信息优先采用 Conventional Commits：
  - `<type>(<scope>): <简短描述>`
- 示例：
  - `feat(auth): add refresh token endpoint`
  - `fix(user): correct pagination count`
  - `chore(dotnet): pin NuGet package version`

### 交付前自查
- 后端：编译（dotnet build）、测试（dotnet test）
- 涉及数据库结构变更时已确认迁移脚本
- 新增环境变量已同步更新相关文档
- 涉及 Docker 时已确认构建或部署链路

### 提交触发规则
- 每次修改完后都必须提交代码
- 若本次开发生成了本地运行产物、缓存、测试数据库、构建产物或依赖目录，应同步检查并更新 `.gitignore`

---

## Docker 规则

### 总体要求
- 优先复用现有 Docker 与 Compose 文件
- 生产镜像优先采用多阶段构建
- 不要把密钥写入镜像
- 部署链路要可追踪
- 清理旧镜像时要保留最小回滚窗口

### Docker 交付规则
- 只要本次任务新增或修改了 Dockerfile、`docker-compose.yml`、运行端口、环境变量或启动命令，完成实现后应主动执行一次镜像构建和容器化启动验证；若因环境限制无法执行，必须明确说明
- 容器化验证至少包括：
  - 镜像构建是否成功
  - Compose 服务是否成功启动
  - 关键服务是否处于可用状态
  - API 核心接口是否可访问
- 若发现宿主机端口冲突，不要直接停止未知本地进程；优先调整 Compose 映射端口，并同步更新文档
- 对于依赖数据库或其他基础服务的容器，Compose 中应尽量补充健康检查或等价的启动等待策略
- 新增镜像构建链路时，应同步补充 `.dockerignore`

---

## 任务管理机制
`TASK.md` 是唯一任务记录文件，用于记录具体任务内容，不负责承载规则解释。

### 任务状态
- `TODO`：未开始
- `DOING`：进行中
- `BLOCKED`：被阻塞
- `REVIEW`：已完成开发，等待确认
- `DONE`：已完成

### 执行规则
1. 开始工作前，先查看 `TASK.md` 是否已有对应任务
2. 如果已有任务，优先更新原任务，不重复新建
3. 如果没有任务，在 `TASK.md` 中新增最小任务记录
4. 开始执行时，将任务状态更新为 `DOING`
5. 若任务无法继续，更新为 `BLOCKED` 并写明原因
6. 开发完成但仍需确认时，更新为 `REVIEW`
7. 验证完成并确认交付后，更新为 `DONE`

### 每个任务至少包含
- 任务 ID
- 任务名称
- 任务目标
- 任务状态
- 最近更新时间

### AI Agent 要求
- 开始任务前先同步任务状态
- 开发过程中及时更新 `TASK.md`
- 范围变化时同步补充
- 每次开发完成后，都需在 `doc/devlog/` 下新增一份本次开发简短说明的 Markdown 记录

---

## 文档更新要求
以下内容发生变化时，必须同步更新相关文档：
- 命令或脚本
- 环境变量
- 构建或部署流程
- 任务流程
- 镜像 tag 或清理策略
- 对外使用方式或接口约定

优先更新已有文档，不新增重复说明。
每次开发完成后，必须在 `doc/devlog/` 下补充一份简短开发记录，作为本次工作的可追踪说明。

### 文档目录约定
- `doc/requirement.md`：用户编写的需求文档，AI 不应将设计说明或开发过程记录写入此文件
- `doc/design/设计文档.md`：项目设计文档，全项目一份。包含架构设计、接口设计、数据模型设计。由用户与 AI 协作产出，用户确认后作为开发的需求基准。AI 可在文件中追加新功能章节，不删除已有内容，不新建平行文件
- `doc/devlog/`：每次开发完成后的简短开发日志

### devlog 要求
- 文件目录：`doc/devlog/`
- 文件格式：Markdown
- 文件命名建议：`YYYY-MM-DD-任务简述.md`
- 内容保持简短，至少包含：
  - 本次目标
  - 核心改动
  - 修改文件
  - 校验情况
  - 风险或遗留项

---

## 交付收口规范
- 当用户请求"开始开发"而非"只出方案"时，AI Agent 在实现完成后应默认继续做交付收口，而不是停在代码编写阶段
- 交付收口至少应按实际情况依次检查：
  1. 编译（dotnet build）
  2. 测试（dotnet test）
  3. Docker 构建与启动验证（如涉及）
  4. 接口级 smoke test（Swagger 验证核心接口）
  5. 文档、`TASK.md`、`doc/devlog/`、忽略文件更新
- 若存在可本地验证的部署链路，AI Agent 应优先自行验证
- 若某项验证未执行，最终输出必须明确说明未执行项、原因、影响范围和建议下一步

---

## 常见禁止事项
- 禁止在 Controller 层编写业务逻辑，应放入 Service
- 禁止修改数据模型后不补迁移脚本
- 禁止在异步代码中使用阻塞式等待（.Result / .Wait()）
- 禁止绕过既有提交校验流程，如 `git commit --no-verify`
- 禁止直接使用 EF Core Migrations，应使用 Azrng.SqlMigration
- 禁止直接操作 Redis，必须通过 IRedisProvider 接口
- 禁止手动配置依赖注入，应使用标记接口方式（ITransientDependency/IScopedDependency/ISingletonDependency）
- 禁止返回非 ResultModel 包装的响应格式
- 禁止抛出非 Azrng 体系的自定义异常
- 禁止在 Service 中直接暴露实体，必须使用 DTO
- 禁止在异步方法中同步阻塞调用（如 .Result、.Wait()、Thread.Sleep）

---

## 交付输出要求
最终输出优先使用中文，并至少说明：
- 本次改了什么
- 核心实现方式
- 修改了哪些文件
- 已执行和未执行的校验
- 当前任务状态
- 风险、阻塞或假设
- 是否已更新 `doc/devlog/`

---

文件结束。
