---
rule_id: backend-agents
version: 1.39.0
last_updated: 2026-09-02
dependencies: [agents-root]
---

# 后端规则

## 适用范围

- 作用域：IPC handler、.NET 服务、数据访问、本地系统能力与后端测试
- 触发场景：涉及 `src-dotnet/`、子进程 IPC、SQLite、文件系统、系统权限、异常处理或测试时阅读

### 常见任务入口
- 改前后端调用契约：先看 IPC Handler 规则与前后端契约规则
- 改业务流程或状态流转：先看 Service 规则与异常处理规范
- 改本地数据库、文件、配置读写：先看 Repository / 数据访问规则与本地能力规则
- 改子进程启动、健康检查或关闭行为：先看进程生命周期规则
- 补 .NET 回归：先看 `提交前最小回归` 与测试规则

---

## 技术栈

### Runtime
- Electron（主进程拉起并管理子进程）
- .NET 8/9 控制台程序，**自包含部署（self-contained）**，目标机无需预装 .NET Runtime
- 默认**不**使用 NativeAOT（自包含已满足体积与启动需求；AOT 有反射 / 动态代码限制，仅在确有需要并评估兼容性后再启用）
- 异步使用 `Task` / `async-await` 与 `System.Threading.Channels`
- 序列化使用 `System.Text.Json`

### 数据与本地能力
- 本地持久化默认推荐：SQLite
- 数据访问默认推荐：`Microsoft.Data.Sqlite`（或 `Dapper` 作为轻量 Micro-ORM）
- 若仓库已稳定使用 `EF Core` 且迁移收益不高，可沿用现有实现，不强行替换
- 配置、日志、缓存、用户目录读写统一在 .NET 侧封装处理

### 通信原则
- Electron 渲染进程通过 preload 暴露的 IPC SDK 调用主进程；主进程与 .NET 子进程通过 **stdin/stdout 的 JSON-RPC** 通信
- .NET 子进程由 Electron 主进程用 `child_process.spawn` 拉起，生命周期由主进程管理
- 默认**不**引入本地 HTTP 服务作为桌面应用内部桥接层
- 仅在以下场景才考虑额外本地服务层：
  - 明确需要流式下载或上传代理
  - 需要本地 WebSocket / SSE
  - 需要对接必须通过 HTTP 语义运行的第三方模块
  - 已证明子进程 IPC 无法满足能力要求

<!-- include: shared/application/tech-choice.md -->

### JSON-RPC 路由与契约
- 渲染进程到主进程优先维护单一通用 IPC 通道（如 `sidecar:invoke`），以 `method + params` 转发；不要为每个 sidecar 方法平铺注册独立的 `ipcMain.handle`。
- .NET sidecar 的 method 分发使用显式路由注册表（`method -> RpcRoute / Handler`），按完整 method 精确匹配并在启动时校验重复项；不要维护随业务域增长的 `if` / `StartsWith` 分发链。
- 路由条目同时声明 method、请求 DTO、响应 DTO、是否流式及调用委托。Dispatcher 只负责路由和协议错误映射，Handler 只负责参数校验与调用 Service。
- .NET 路由和 DTO 是跨语言契约的事实来源。若项目具备构建期生成能力，应从路由表 / DTO 生成 TypeScript method 常量、请求响应类型与基础 IPC SDK；生成文件禁止手改。
- 新增或修改 method 时，依次更新 DTO、路由条目、业务实现和测试，执行契约生成，再使用生成的 method 常量封装渲染层调用；不得在 C# 与 TypeScript 两端手抄 method 字符串或重复维护字段。

### 流式 JSON-RPC 规则
- 流式方法由 sidecar 通过 JSON-RPC notification 推送进度事件，协议主循环必须能继续接收取消、审批等后续请求，不能因等待流式最终响应而阻塞读取。
- 流式任务必须区分总时长和空闲时长：sidecar 负责可取消的整轮业务上限并返回稳定错误；主进程按 requestId 监测进度 notification，每次匹配事件都刷新空闲计时，连续无事件时发送取消。
- 等待用户审批、确认或其他显式交互输入时，主进程暂停空闲计时；收到用户裁决后恢复。该等待仍受 sidecar 的整轮上限约束。
- 普通 RPC 使用短默认超时；不得为了流式任务把全局默认超时无差别放宽。主进程可保留略长于 sidecar 总时长的失联兜底，并在触发时发送取消。

## 主动建议规则
- 发现 IPC handler 与 .NET 服务职责混杂、SQLite 访问封装不一致或错误处理缺失时，应主动提醒
- 发现接口契约、DTO、SQLite 表结构或状态流转可能影响历史数据、兼容性或权限安全时，必须先说明风险，不得直接扩大修改
- 发现可以复用既有服务、数据访问封装、DTO、校验器或错误处理封装时，应优先建议复用
- 不确定业务规则、权限规则、数据含义或外部接口行为时，应按根 `AGENTS.md` 的查证优先级处理，禁止按通用经验补写规则

---

## 推荐目录结构

若仓库尚未形成稳定结构，可优先参考以下组织方式；若仓库已有实现，以现状为准，不强制迁移。

```text
src-dotnet/
├── Handlers/               # IPC handler（JSON-RPC method 入口）
├── Services/               # 业务服务
├── Repositories/           # 数据访问层
├── Models/                 # 领域模型与 DTO
├── Db/                     # 数据库连接、迁移、查询辅助
├── JsonRpc/                # JSON-RPC 协议封装（请求/响应/错误）
├── Integrations/           # 外部系统与系统能力封装
├── Errors/                 # 统一错误类型
├── State/                  # 应用状态 / 共享状态
├── Config/                 # 配置加载
├── Program.cs              # 入口（启动 stdin/stdout 循环）
└── YourApp.csproj          # 自包含部署配置
```

前端侧的 Electron 主进程建议有 `main/` 目录承载子进程拉起与 IPC 转发逻辑。

---

## 阶段 2 — Electron / .NET 实现

**触发条件**：用户发出“开始后端开发”或等价指令

**入场要求**：前端页面结构已完成，schema 与契约清晰

**工作内容**：
1. 按 `IPC Handler -> Service -> Repository -> SQLite / Local Capability` 分层实现本地能力。
2. 实现并验证 Electron 主进程与 .NET 子进程的 JSON-RPC 通道。
3. 根据需求维护本地数据库 schema、迁移脚本、配置读写与文件目录约束。
4. 完成权限、日志、缓存、状态流转、分页、筛选、排序等 .NET 侧逻辑。
5. 每个核心能力都必须可通过前端联调、测试或脚本独立验证。

**门控规则**：
- 核心服务、IPC 通道与 Handler 独立验证通过后，才允许进入阶段 3。

---

## Electron / .NET 规则

### 分层依赖规则

```text
IPC Handler / Event（JSON-RPC method）
          ↓
       Service
          ↓
     Repository
          ↓
 SQLite / FileSystem / External Capability
```

### IPC Handler 规则
- Handler 只负责参数解析、权限校验、调用服务与返回稳定结果
- JSON-RPC method 名称使用明确业务语义，不使用模糊缩写
- 前端可见的输入输出结构必须稳定、可序列化、可测试
- 不在 Handler 中直接写复杂数据库查询或系统调用
- 统一在边界层把业务错误转换为稳定、可识别的 JSON-RPC error 结果

### Service 规则
- 业务校验、权限判断、去重判断、状态流转、事务边界放在 Service 层
- 返回明确的类型结果，不返回未约束的 `JsonElement` / `object`
- 涉及耗时操作时显式处理超时、取消（`CancellationToken`）、失败重试或幂等
- 若需要跨多个仓储或能力协作，由 Service 负责组织，不把编排逻辑散到 Handler
- 禁止在 IPC Handler、Service、Repository、DTO、Model 等 C# 类中使用主构造函数（Primary Constructor），统一使用显式构造函数

### Repository / 数据访问规则
- 数据访问不得出现在 IPC handler 或前端代码中
- 复杂查询、分页、排序、条件拼装收敛到 Repository
- 默认使用迁移脚本管理 schema 变更，不依赖启动时隐式改表
- SQLite 表与字段命名统一使用 snake_case
- schema 变更后必须同步迁移脚本、类型和文档

### 进程生命周期规则
- .NET 子进程的启动、健康检查、异常退出重启与优雅关闭统一封装在 Electron 主进程侧（如 `main/ipc/` 或等价目录）
- 主进程必须在应用启动时拉起 .NET 子进程，并在应用退出前发送关闭信号、等待其正常退出
- 子进程异常退出时，主进程必须记录日志并按策略重启；连续失败超过阈值时通知用户，不静默吞掉
- stdin/stdout 仅承载 JSON-RPC 协议；.NET 侧的日志必须输出到 stderr 或文件，**禁止**把诊断日志混入 stdout 污染协议通道
- 大块载荷或二进制数据应通过约定换行 / 长度前缀正确分帧，避免半包粘包

### 本地能力规则
- 文件系统、系统对话框、剪贴板、shell 调用、自动更新等能力统一封装在 `Integrations/` 或等价目录
- 所有本地路径都必须显式区分：
  - 应用配置目录
  - 应用数据目录
  - 缓存目录
  - 临时目录
- 禁止在代码中硬编码用户目录或绝对路径
- 涉及系统调用时必须说明权限边界、失败回退与日志

### 前后端契约规则
- 前端可见的 Handler 返回值应先定义为明确 DTO
- DTO 与前端 schema 尽量一一对应，统一序列化命名策略（默认 `System.Text.Json` 用 `camelCase`，与前端 TS 类型一致）
- 若仓库已经通过 TS 类型生成或共享 schema 维护契约，优先沿用，不重复造一套平行定义
- 新增或修改 method 时，同步更新前端 IPC 封装层与文档

### 代码组织规范
- 一个文件只放一个主类型：IPC Handler、Service、Repository、DTO、Model、Error 等，默认一个类一个文件，文件名与类名一致。
- DTO / Model 目录组织：放 `Models/`，按业务域或类型分子目录，DTO 一类一文件，不合并到 `Dtos.cs`。
- 触发拆分的信号：职责混杂、同文件出现多个主类型、参数或字段持续堆叠、方法跨多个不相关业务时，应主动拆分。
- 允许例外：`private` / `internal` 且只服务当前文件的小辅助类型、与主类型强绑定的局部 mapping extension、测试文件中只服务当前测试类的小型 fixture。
- 反模式：`Models.cs` 长期堆放多个 DTO / Model；把 IPC Handler 专用 request 内联写在 Handler 文件里；JSON-RPC 协议类型与业务模型混在同一文件。

<!-- include: shared/application/kiss.md -->

### 异常处理规范
- 自定义业务异常统一收敛到 `Errors/`
- 区分参数错误、权限错误、资源不存在、状态冲突、系统异常
- 禁止把原始数据库错误、IO 错误或底层异常直接透传给页面
- 日志中允许保留详细错误，但给前端的错误文案应稳定且可理解

<!-- include: shared/backend/not-recommended.md -->

### 代码注释规范
- 必须补 XML 注释的位置：
  - IPC Handler 方法（前端可见契约）：用 `<summary>` 说明 method 用途、关键参数、返回结果。
  - Service 接口方法：说明该能力做什么，不写实现细节。
  - 不来自接口的实现类自有方法（接口未约束的核心方法）：补 `<summary>` 说明该能力做什么。
- 接口已注释时，实现方法不重复注释，也不加 `/// <inheritdoc />`；只有实现类自有的、接口未定义的方法才需要补注释。
- 默认不写 `<remarks>`：实现类、实现方法上的 `<remarks>` 多为实现细节，按“不写实现细节”原则省略。
  - 例外：IPC Handler 方法需要向前端说明调用约定、参数格式、错误码等对外契约信息时，可保留精简后的 `<remarks>`，但不写内部实现说明。
- 优先补行内注释的位置：权限校验、状态流转、事务边界、超时 / 重试 / 幂等等复杂分支，说明“为什么这样做”——包括 fail fast 的原因、为什么跳过某类输入、为什么保留某一步骤。
- 不要补的注释：普通属性的 get / set、简单 `if` / `return`、“返回结果”这类复述代码的低价值注释。
- 注释统一使用中文，不在注释里泄露密钥、token、连接串或真实生产地址。

<!-- include: shared/application/anti-patterns.md -->

<!-- include: shared/application/maintainability-check.md -->

---

## 测试规则

### 提交前最小回归
- 默认执行：项目现有的格式化、静态检查或 .NET 编译校验
- Service 改动：至少补一项业务规则、权限判断或状态流转验证
- IPC Handler 改动：至少补一项输入输出、错误分支或前后端契约验证
- Repository / SQLite 改动：至少补一项查询结果、事务、迁移或路径写入验证
- 若仓库暂时缺少自动化测试基础，可使用前端联调、集成脚本或等价验证替代，但必须能证明核心逻辑真实生效

### .NET 测试
- Service 测试：验证业务规则、权限校验、状态流转
- Repository 测试：验证查询条件、分页、排序、事务或数据库映射
- Handler 测试：验证输入输出结构和错误分支
- 涉及文件系统或系统能力时，优先通过可控临时目录和测试替身验证

<!-- include: shared/no-test-fallback.md -->

---
