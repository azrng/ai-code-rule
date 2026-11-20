---
rule_id: application-agents
version: 1.0.0
last_updated: 2026-11-20
dependencies: [agents-root]
---

# 应用规则

<!-- include: shared/application/scope.md -->

---

<!-- include: shared/application/advisory.md -->
- 发现实现方案与服务行为说明（启动参数、配置文档、运维说明）不一致时，应主动提醒并建议同步修订
---

### 阶段 1 — 服务逻辑实现（服务实现角色主导）

**触发条件**：用户发出「开始实现」指令

**入场要求**：`src/AppName/Models/DTOs/` 数据契约与作业契约文件已定稳

**工作内容**：
1. 严格按照 DTO 中的类型定义实现 Worker 编排与 Service 层逻辑
2. 遵循 `Worker → Service → Repository` 的应用层职责边界，持久化与安装部署细则移交 `infrastructure-AGENTS.md`
3. Worker 只负责生命周期编排（启动、停止、调度、取消），不直接编写业务规则与数据访问细节
4. 每个服务方法都必须可通过单元测试独立验证，不依赖真实宿主进程

## 项目结构规范

```text
project-root/
├── src/
│   └── YourProject/                     # Worker 服务项目
│       ├── Workers/                      # 后台作业（每个作业一个 BackgroundService）
│       ├── Services/                     # 业务服务接口与实现
│       │   └── Interfaces/              # 服务接口定义
│       ├── Models/                      # 数据模型
│       │   ├── DTOs/                    # 数据传输对象
│       │   └── Enums/                   # 枚举
│       ├── Extensions/                  # 扩展方法、DI 注册扩展
│       ├── Helpers/                     # 工具类
│       ├── Program.cs                   # 宿主入口（Host 构建 + 注册）
│       ├── appsettings.json             # 配置文件
│       └── YourProject.csproj
├── tests/
│   └── YourProject.Tests/               # 测试项目
├── doc/
│   ├── design/
│   └── requirement.md
├── .gitignore
├── AGENTS.md
├── CLAUDE.md
├── GEMINI.md
├── GROK.md
└── TASK.md
```

### 目录职责映射

| 目录 | 放什么类 | 命名约定 |
| --- | --- | --- |
| `Workers/` | 后台作业，继承 `BackgroundService`，仅做调度与生命周期编排 | `{作业名}Worker.cs`（如 `SyncOrderWorker.cs`） |
| `Services/` | 业务服务实现（业务逻辑、外部调用编排） | `{业务域}Service.cs` |
| `Services/Interfaces/` | 服务接口 | `I{业务域}Service.cs` |
| `Models/DTOs/` | 输入 / 输出 DTO，一类一文件 | `{名称}Input.cs`、`{名称}Result.cs` |
| `Extensions/` | 扩展方法、DI 注册扩展 | `{类型}Extensions.cs` |
| `Helpers/` | 工具类（纯函数、无业务逻辑） | `{名称}Helper.cs` |
| `Program.cs` | 宿主入口：构建 Host、注册配置与依赖、挂载 Worker | — |

---

## Worker 服务规则

### Program.cs 规则
- Program.cs 只负责三件事：构建配置、注册依赖、挂载 Worker（`AddHostedService` / `AddWindowsService`）
- 禁止在 Program.cs 中编写业务逻辑
- 使用 `HostApplicationBuilder`（.NET 8+）构建宿主；Windows 服务部署时使用 `builder.Host.UseWindowsService()`

### BackgroundService 规则
- 每个后台作业一个独立的 `BackgroundService` 类，放在 `Workers/` 目录；一个类只做一类作业，禁止一个 Worker 内塞多个不相关作业
- `ExecuteAsync` 必须正确响应取消：
  - 循环体每轮检查 `cancellationToken.IsCancellationRequested`，等待一律用 `Task.Delay(delay, cancellationToken)`，禁止无令牌的 `Thread.Sleep` / `Task.Delay`
  - 捕获 `OperationCanceledException` 时区分取消来源：宿主停止导致的取消正常退出，不得当作错误吞掉或记录为 Error
- 长周期作业必须实现优雅停止：停止信号到达后完成当前单元工作或安全中断，禁止裸抛异常终止宿主
- Worker 内单个作业周期抛出的业务异常不得终止宿主：捕获、记录、按退避策略继续；不可恢复的配置错误才允许快速失败
- 定时触发使用可注入的时间抽象（如 `TimeProvider`），保证单测可注入虚拟时间，禁止在业务逻辑中直接 `DateTime.Now`
- 作业间的共享状态必须通过注入的单例服务承载，禁止 Worker 之间静态字段互访

### Service 规则
- 服务类实现接口，接口定义在 `Services/Interfaces/` 目录
- 耗时操作必须是异步的（返回 `Task<T>`），并透传取消令牌
- 使用 DTO 进行数据传递
- 业务异常抛出继承自 Azrng 异常体系的自定义异常
- 服务类实现标记接口（`ITransientDependency`/`IScopedDependency`/`ISingletonDependency`）以自动注册
- 需要作用域服务的 Worker 通过 `IServiceProvider` 按作业周期创建 `IServiceScope`，禁止把 Scoped 服务注入 Singleton Worker 字段长期持有
- 禁止在 Worker、Service、DTO、Options、Helper 等 C# 类中使用主构造函数（Primary Constructor），统一使用显式构造函数

### 代码组织规范
- 一个文件只放一个主类型：Worker、Service 接口与实现、DTO、Options、枚举等，默认一个类一个文件，文件名与类名一致。
- 触发拆分的信号：职责混杂、同文件出现多个主类型、`ExecuteAsync` 内流程持续堆叠时，应把业务规则下沉到 Service，Worker 只留调度骨架。
- 反模式：把重试、数据转换、外部调用全部内联在 `ExecuteAsync` 里。

<!-- include: shared/application/kiss.md -->

### 配置管理规则
- 使用 `Microsoft.Extensions.Configuration` 管理配置
- 配置来源优先级（高到低）：环境变量 > 平台配置（Windows 注册表 Services 可选注入）> appsettings.json
- 敏感配置（密钥、密码）禁止写入 appsettings.json，通过环境变量或用户机密传入
- 配置项通过 Options 模式绑定到强类型类：
  ```csharp
  services.Configure<SyncOptions>(configuration.GetSection("Sync"));
  ```
- 配置在启动时一次性校验（`IValidateOptions`），非法配置快速失败并输出明确原因，禁止运行中途才暴露
- 禁止在代码中硬编码配置值

### 日志规范
- 使用 `Microsoft.Extensions.Logging` 抽象，配合 Serilog 实现
- 日志输出目标：交互式运行写控制台 + 文件；注册为 Windows 服务后无控制台，必须落文件（滚动策略）或写 Windows 事件日志，禁止只依赖 Console
- 启动、停止、每次作业的「开始 / 结束 / 跳过」必须落在 `Information` 级别，保证运维可从日志重建行为
- 正常业务流程使用 `Information` 级别；可预期的业务异常使用 `Warning`；不可预期的系统异常使用 `Error`
- 禁止使用 `Console.WriteLine` 输出业务信息，统一使用 logger

### 异常处理规范
- 宿主级未处理异常会终止服务：启动前的不可恢复错误（配置非法、端口占用）快速失败；运行期作业错误按「BackgroundService 规则」捕获后退避重试
<!-- include: shared/application/azrng-exception-types.md -->
- 退避策略：连续失败按指数退避（如 5s → 10s → 60s，上限封顶），恢复后回到正常节奏；退避参数进配置，不硬编码

### 依赖注入规范
- 服务类必须实现以下接口之一：
  - `ITransientDependency`：每次请求创建新实例
  - `IScopedDependency`：每个作用域内单例
  - `ISingletonDependency`：宿主生命周期单例
- Worker 一律注册为 Singleton 宿主挂载；其依赖链必须能在单例上下文解析（Scoped 依赖经作用域获取）
- 禁止使用静态服务定位器 pattern

### 并发与幂等规则
- 同一作业禁止并发重入：跨轮次未完成的作业必须用信号量 / 状态标志防重入
- 可能重复执行的作业（定时任务、消息消费）必须幂等：以业务键判重，不依赖「上一轮没跑完就不会开始」
- 共享可变状态集中在单例服务内加锁或使用并发集合，禁止散落 `lock(this)`
- 多实例部署（或误装多份）必须有互斥保护（命名 Mutex / 分布式锁），默认单实例假设需在文档显式声明

### 无交互口径
- 后台服务运行于无交互桌面会话：禁止弹窗、`Console.ReadLine`、模拟键盘鼠标等一切交互依赖
- 需要人工决策的场景改为「写日志 + 状态标记 + 下次周期重试」或提供显式命令行 / API 触发入口

### 字段命名约定
- C# 属性：PascalCase
- JSON 配置键：camelCase
- Windows 服务名：与发布产物名一致（kebab-case，如 `sso-launcher`），安装脚本与文档统一引用同一名字

### 代码注释规范
- 必须补 XML 注释的位置：
  - Service 接口方法：说明该能力做什么，不写实现细节。
  - Worker 类：用 `<summary>` 说明作业用途、触发节奏、停止行为。
<!-- include: shared/application/comment-discipline.md -->

<!-- include: shared/application/anti-patterns.md -->

---

## 测试规则

<!-- include: shared/application/pre-commit-regression.md -->

<!-- include: shared/common/general-requirements.md -->

### 测试分类
- 测试框架：xUnit + Moq
- Service 测试：验证业务逻辑分支，使用 mock 隔离依赖
- Worker 测试：直接调用 `ExecuteAsync` 传入 `CancellationTokenSource`，验证取消响应、退避重试、防重入；不启动真实宿主
- 取消令牌测试是必测项：停止信号到达后 `ExecuteAsync` 必须及时返回且不记录错误级日志

<!-- include: shared/external-dependencies.md -->

<!-- include: shared/no-test-fallback.md -->

---
