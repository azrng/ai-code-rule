---
rule_id: backend-agents
version: 1.0.0
last_updated: 2026-11-13
dependencies: [agents-root]
---

# 后端规则

## 适用范围

- 作用域：NuGet 包实现（类库工程）、示例宿主、数据访问、缓存、异常处理与逐包测试
- 本仓库主体是多包单仓库：多数改动对象是无宿主的类库工程，包的「契约」即公开 API（公开接口、DTO、Options、扩展方法）；公开面改动先评估破坏性，版本口径见 `infra-AGENTS.md`「发布纪律」
- 示例宿主（如有）只是包的真实消费方：结构保持最小（`Program.cs` / `Startup.cs` + 少量 Controller），不套用服务端分层模板、不承载业务
- 触发场景：涉及包公开 API、包实现、示例宿主、数据访问、缓存、权限、异常处理或测试时阅读

### 常见任务入口
- 改包公开 API：先看「包公开 API 契约」，版本影响评估见 `infra-AGENTS.md`「发布纪律」
- 新增包 / 新增测试工程：先看「包仓库组织」的登记口径
- 改示例宿主：先看「示例宿主规则」
- 补测试：先看「逐包测试」与「提交前最小回归」

---

## 技术栈

### 后端
- .NET 类库包按 csproj `TargetFramework(s)` 构建，需要覆盖多个运行时时用多目标（`TargetFrameworks`）；目标框架以仓库现有为准，不主动升级，收窄支持范围按破坏性变更评估
- 示例宿主（如有）用 ASP.NET Core，仅作包消费演示与启动 smoke 验证

---

## 主动建议规则
- 发现包公开 API 与实现职责混杂、包间依赖越界或循环、公开面泄漏内部实现、Options 缺启动期校验或 DI 扩展可重复注册出问题时，应主动提醒
- 发现公开 API、契约行为或序列化结果可能影响既有消费方兼容性时，必须先说明风险与版本影响，不得直接扩大修改
- 发现可复用的既有包内封装（扩展方法、工具类、抽象基类）时，应优先建议复用，不新增平行实现
- 不确定包的公开行为、依赖版本或消费方用法时，应按根 `AGENTS.md` 的查证优先级处理，禁止按通用经验补写规则

---

## 包仓库组织

### 目录与解决方案
- 打包工程按领域分组（如 `src/Shared/`、`src/Services/`），每个包一个 csproj，工程名即包工程标识；是否拆「接口包 + 实现包」以仓库既有先例为准，不新开口径
- 仓库按域拆多个解决方案（`*.slnx` / `*.sln`）避免全量加载；存在专门的打包解决方案决定发布面：新包必须登记进打包解决方案，否则 CI 不发布；发现「实现了但没人打」或「打包链路断链」时主动提醒
- 新增源工程 / 测试工程必须与登记解决方案同提交：`dotnet test` 按解决方案枚举工程，对未登记工程静默跳过（不报错，绿灯行数悄悄变小）；验证时把 `dotnet test` 输出的工程结果行与解决方案工程清单逐一核对，缺行即登记遗漏

### 逐包测试工程
- 每个包对应一个测试工程（`test/<包名>.Test` 或仓库既有等价组织），一一对应，不建跨包大而全测试工程
- 测试工程跟随包的最低目标框架；新包落地时同提交测试工程骨架与登记

### 新增包清单
新增一个包在同一次提交内完成：csproj 元数据（`PackageId` / `Version` / `RepositoryUrl` / `PackageReadmeFile`）、包目录 `README.md`、登记打包解决方案与域解决方案、测试工程骨架；文档口径见 `doc-AGENTS.md`，发布登记见 `infra-AGENTS.md`

---

## 包公开 API 契约

### 公开面纪律
- 包对外只通过公开 API 表达能力：公开接口、DTO、Options、扩展方法、异常类型；公开面即契约，先设计后实现
- 破坏性判定（升 major，细则见 `infra-AGENTS.md`「发布纪律」）：删除 / 重命名公开类型或成员、签名变化（参数、返回类型、可空性）、行为语义反转、目标框架收窄、配置项移除或改义
- 新增公开 API、新增注册入口、行为增强按 minor；缺陷修复按 patch；判定拿不准时按破坏性处理并说明理由
- `internal` 实现不进公开面；测试需要访问内部类型时用 `InternalsVisibleTo` 指向对应测试工程，不为测试放宽公开面
- 公开 API 不泄漏内部实现类型、数据库实体或宿主环境假设（如 `HttpContext`）；需要环境信息走显式参数或自有抽象

### 公开 API 设计规范
- 配置统一 Options 模式：`{核心词}Options` 命名、属性带中文 XML 注释与缺省值；启动期校验用 `IValidateOptions<T>` 或等价 Validator，配置错误在注册期暴露且错误信息可操作（说清缺什么、期望格式）
- DI 注册用扩展方法：`Add{能力}` 命名、放在 `{能力}ServiceCollectionExtensions`；内部注册优先 `TryAdd` 系列防重复；一个能力一个注册入口，不在多个扩展间拆散同一能力的注册
- 包不隐式注册服务：调用方显式调用扩展方法才算注册，编译通过不代表运行时可注入——新增可注入能力必须同步扩展方法、测试与文档示例
- 包内业务失败优先抛类型化异常（复用基础包异常体系或包内定义），禁止吞异常或返回 `default` 掩盖失败
- 异步公开 API 一律返回 `Task` / `Task<T>`（或确有理由的 `ValueTask`），不暴露同步阻塞版本；耗时公开 API 暴露 `CancellationToken` 参数并真实传递给底层

### 包间依赖与版本
- 包间依赖用项目引用并对齐版本；禁止循环依赖，公共能力下沉到被依赖包，不互相引用
- `PackageReference` 用锁定版本，禁止浮动版本（`*`、`2.0-*`）进入主干；升级依赖逐包评估是否构成破坏性
- 最小依赖面：不引入用不到的传递依赖；诊断 / 测试专用依赖只进测试工程或按 `PrivateAssets` 声明，不进包的公开依赖
- 多目标工程用条件编译（`#if`）隔离框架差异，公共逻辑放无条件的源文件

### XML 注释
- 公开类型与公开成员必须有 `<summary>`：说清该能力做什么、关键参数含义与单位、可预期的异常与副作用；不写实现细节
- 接口已注释时实现不重复注释，也不加 `/// <inheritdoc />`
- `GenerateDocumentationFile` 保持开启：XML 注释是 IntelliSense 与文档站的唯一来源（文档口径见 `doc-AGENTS.md`）

---

## 阶段 1 — 包实现

**触发条件**：用户发出「开始开发」或「开始实现」指令

**入场要求**：阶段 0 的公开 API 设计与版本影响评估已明确；若为例外情况任务（Bug 修复、不动公开 API 的实现调整、文档 / 示例 / 版本号调整），可直接进入实现

**工作内容**：
1. 按公开 API 设计定位目标包与关联包（依赖方 / 被依赖方），确认复用点后实现。
2. 同步补逐包测试与示例宿主调用点；公开面变更同步 XML 注释与 README。
3. 所有增删改必须真实生效，并能通过测试工程或示例宿主独立验证。

**门控规则**：
- 受影响包的测试通过；公开 API 行为改动已补测试或在交付说明中解释未补原因。
- 公开面未被未设计的新成员污染；包间依赖方向未破坏。
- 实现中发现契约缺口时，按 `collaboration-AGENTS.md`「纠错、回退与重构」回到公开 API 变更流程，不在实现层绕过契约。

---

## 包实现规则

### 示例宿主规则
- 示例宿主只演示包的真实用法：注册入口、典型调用、配置样例；一个演示场景一个 Controller / 最小端点
- 示例宿主不写业务分层（AppService / Repository）、不承载包测试职责；包行为验证优先走测试工程，示例宿主只做启动 smoke
- 包 API 变更时同步示例调用点；示例编译失败即契约同步未完成的信号

### Controller 规则（仅示例宿主）
- Controller 只做 HTTP 编排：`[ApiController]` + `[Route("api/[controller]")]`，构造函数注入包提供的公开接口
- action 一行转发包调用，不在示例里堆业务逻辑；统一响应与异常格式按所用包自身的约定（异常口径见「异常处理规范」），示例不自造包装

<!-- include: shared/backend/code-organization-dotnet.md -->
- 包内公开契约类（Options、扩展方法容器、DTO）同样一个类型一个文件，文件名与类型名一致

<!-- include: shared/application/kiss.md -->

<!-- include: shared/backend/di-rules.md -->

<!-- include: shared/backend/exception-handling.md -->

<!-- include: shared/backend/field-naming.md -->

### 代码注释规范
- 必须补 XML 注释的位置：
  - 包公开接口方法、Options 属性、扩展方法：说明该能力做什么、关键参数含义与单位、可预期的异常，不写实现细节
  - 示例宿主 Controller action：说明演示场景（Swagger 会消费这些说明）
- 涉及时间字段的 DTO / 接口注释必须写明转换责任方：入参是本地墙钟还是 UTC、由哪一层完成转换（如“调用方传入 UTC”），而不是只标存储单位

<!-- include: shared/application/comment-discipline.md -->

<!-- include: shared/application/anti-patterns.md -->

---

## 测试规则

### 提交前最小回归
- 默认执行：受影响包测试工程的编译与既有测试（输出目录隔离见「.NET 并行隔离」）
- 公开 API 行为改动：至少补一项该包测试工程的验证（新用例或既有用例更新），并在交付说明写明覆盖点
- 缺陷修复：补回归用例锁定原始复现场景；无法自动化时用脚本 / 命令行等价验证并说明
- 涉及打包链路 / 版本号：按 `infra-AGENTS.md`「提交前最小回归」补 `dotnet pack` 等价验证与版本三处一致核对

<!-- include: shared/backend/general-requirements.md -->

<!-- include: shared/backend/logging-rules.md -->

### 逐包测试
- 测试框架：xUnit + Moq；测试放对应 `test/<包名>.Test`，不跨包混放
- 单元测试 mock 外部依赖（网络、时间、文件系统），不真实调用第三方服务；测试数据最小化、可读、可重复执行
- 涉及真实外部差异的包（数据库方言、消息代理语义、缓存行为、HTTP 行为）：用环境变量门控连真实实例，未设置即跳过对应用例并在输出中说明跳过原因；环境变量命名沿用仓库既有前缀（见 `infra-AGENTS.md`「环境变量」）
- 内存 Provider 只覆盖基础行为；PostgreSQL 时间语义、JSON 映射、SQL 方言差异必须连真实库验证，InMemory 无法暴露此类差异
- 消费方视角用例：公开 API 从组合根注册后真实调用，验证「注册即可用」，防止扩展方法与实现脱节

<!-- include: shared/backend/dotnet-build-isolation.md -->
- NuGet 打包验证输出独立目录：`dotnet pack <包>.csproj -o artifacts/packages/<任务名>`，不与其他会话共用输出目录（见 `infra-AGENTS.md`「提交前最小回归」）

<!-- include: shared/external-dependencies.md -->

<!-- include: shared/no-test-fallback.md -->

---
