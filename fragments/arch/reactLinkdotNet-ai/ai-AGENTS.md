---
rule_id: ai-agents
version: 1.21.0
last_updated: 2026-08-31
dependencies: [agents-root]
---

# AI 规则

> 本规则规定模型调用、Agent 编排、工具 / Skills、Prompt、流式与可选扩展能力（向量库、记忆、多 Agent、MCP）；选型理念为「核心做扎实，可选按需引入，先闭环再扩张」。

## 适用范围

- 作用域：模型调用、Agent、Function Calling、Skills、Prompt、流式输出、向量与记忆等可选能力、AI 相关测试
- 触发场景：涉及任何模型对接、Agent 运行、工具调用、Prompt 编写或流式交互时阅读

### 常见任务入口
- 接入或切换模型（DeepSeek / Azure OpenAI / GitHub Models / 本地模型）：先看模型调用规范
- 新建一个 Agent 或扩展多步能力：先看 Agent 规范
- 让模型调用业务方法或外部能力：先看工具 / Skills 规范
- 调输出格式或行为：先看 Prompt 规范
- 做打字机效果或长任务进度：先看流式与长任务规范
- 引入知识检索或长期记忆：先看可选扩展能力

---

## 核心 vs 可选（务必先读）

| 能力 | 分层 | 第一版是否需要 | 说明 |
| --- | --- | --- | --- |
| `IChatClient`（Microsoft.Extensions.AI） | 核心 | ✅ 需要 | 统一模型调用底座，所有模型交互的唯一入口 |
| OpenAI 兼容客户端（DeepSeek 等） | 核心 | ✅ 需要 | 通过 OpenAI 风格接入，受 `IChatClient` 抽象 |
| Microsoft Agent Framework | 核心 | ✅ 需要 | 工具调度、Skills、运行时；从单 Agent 起步 |
| Function Calling / 工具 | 核心 | ✅ 视需要 | Agent 需要调用业务能力时引入 |
| 流式输出 | 核心 | ✅ 视需要 | 对话或长文本生成建议启用 |
| 向量存储 / 检索（VectorData） | 可选 | ❌ 暂不 | 出现 RAG / 语义检索需求时再引入 |
| Agent 记忆（Memory） | 可选 | ❌ 暂不 | 需要跨会话上下文累积时再引入 |
| 多 Agent 编排 | 可选 | ❌ 暂不 | 单 Agent 边界稳定后再拆 |
| MCP（Model Context Protocol） | 可选 | ❌ 暂不 | 需要对接外部工具生态时再引入 |

> 判断准则：第一版只为跑通“用户输入 → Agent 运行 → 结果展示 → 用户确认 → 下一步”。除此之外的能力一律先记为待确认，不默认引入。

---

## 技术栈

### 核心层（包名与类型已对照官方源码核对）
- 模型调用抽象：`Microsoft.Extensions.AI`（`IChatClient`、`IEmbeddingGenerator` 等）
- OpenAI 兼容接入：`Microsoft.Extensions.AI.OpenAI`（DeepSeek、OpenAI、GitHub Models 等 OpenAI 兼容接口统一走此通道，产出 `IChatClient`）
- Agent 框架：Microsoft Agent Framework，.NET 包根 `Microsoft.Agents.AI.*`
  - 核心包 `Microsoft.Agents.AI`：`AIAgent`（抽象基类）、`ChatClientAgent`（基于 `IChatClient` 的具体 Agent，**本项目主用**）、`AIAgentBuilder`
  - Skills：`AgentSkillsProvider` + `AgentSkillsProviderBuilder`
  - 运行：`agent.RunAsync(...)`（非流式）、`agent.RunStreamingAsync(...)`（流式，`IAsyncEnumerable`）
  - 工具：`Microsoft.Extensions.AI` 的 `AIFunctionFactory.Create(...)`，方法用 `[Description]`（`System.ComponentModel`）标注语义
- 依赖注入：通过 `Microsoft.Extensions.AI` 的 DI 扩展注册 `IChatClient`，再由 `ChatClientAgent` 等消费；Agent 注册到 DI，业务层注入抽象

### 可选层（按需）
- 向量存储 / 检索：`Microsoft.Extensions.VectorData` + 任意向量库实现
- 记忆：Agent Framework 提供的记忆能力（参考源码 samples `04_memory` / `Hosted-MemoryAgent`）
- 多 Agent / 工作流：`Microsoft.Agents.AI.Workflows`（多 Agent 编排，参考 `05_first_workflow`）
- MCP：`Microsoft.Agents.AI.Mcp`（MCP 工具与 Skills 接入）
- 托管 / 持久化：`Microsoft.Agents.AI.Hosting.AspNetCore`、`Microsoft.Agents.AI.DurableTask`、`Microsoft.Agents.AI.CosmosNoSql` / `Valkey`

> 上述包名与类型已对照官方源码（Microsoft Agent Framework，`dotnet/src`）核对。仍以项目锁定的 `*.csproj` 版本为准；类型随版本可能有调整，落地时按官方文档校准，禁止凭记忆写死 API。

<!-- include: shared/application/tech-choice.md -->

---

## 主动建议规则
- 发现业务代码绕过 `IChatClient` 直接依赖具体模型 SDK（如直接 `new OpenAIClient().Get...()` 并手拼请求）时，必须提醒并建议收敛到抽象层
- 发现模型密钥、Base URL、模型名硬编码在源码或前端时，必须提醒迁移到环境变量 / 配置
- 发现 Prompt 散落在 Service / Controller 字符串里、难以维护或测试时，建议沉淀到 Prompt 目录
- 发现 Agent 一开始就堆 Skills、记忆、多 Agent、向量库而尚未跑通基础闭环时，建议先回退到最小 Agent
- 发现可以复用既有 Prompt、工具注册、模型客户端封装或流式处理模式时，应优先建议复用
- 不确定模型能力、工具边界或包版本行为时，应按根 `AGENTS.md` 的查证优先级处理，禁止按通用经验补写规则

---

## 模型调用规范

### 抽象优先原则
- **业务代码只依赖 `IChatClient`**，不直接依赖具体模型 SDK 或手写 `HttpClient` 调兼容接口
- 通过 DI 注册 `IChatClient`，业务层构造函数注入；测试中用 mock `IChatClient` 替换
- 严禁在前端直接持有模型密钥或直连模型接口；模型调用一律走后端

### 配置规范
- 模型相关配置统一走 `IOptions<AIOptions>` 或等价强类型配置，来源为环境变量 / `appsettings.json`
- 必须可配置项：`Provider`、`Endpoint`（Base URL）、`ApiKey`、`ModelId`；可选：`Temperature`、`MaxOutputTokens`、`Timeout`
- `ApiKey` 不得写入仓库、不得写入镜像；通过环境变量注入，示例见 `infra-AGENTS.md` 与 `.env.example`
- 切换模型（DeepSeek ↔ Azure OpenAI ↔ GitHub Models ↔ 本地）只改配置与 DI 注册，不改业务层

### 调用层级选择
- 单次输入单次输出、不需要工具、不需要多步推理：直接用 `IChatClient` 即可，不要硬上 Agent
- 需要工具调用、多轮执行、上下文组织、Skills、运行时控制：在 `IChatClient` 之上加 Agent Framework
- 先判断需求层级，再选抽象，避免"用 Agent 框架包一个补全调用"的过度设计

### 目录结构建议（落在后端单项目内）
```text
backend/src/YourProject/
└── AI/
    ├── Clients/            # IChatClient 注册与模型客户端封装
    ├── Agents/             # AIAgent 定义与编排
    ├── Tools/              # Function Calling 工具（[Description] 标注的方法）
    ├── Skills/             # AgentSkillsProvider 挂载的技能集合
    ├── Prompts/            # Prompt 模板与片段（集中管理）
    └── Options/            # AIOptions 等强类型配置
```

### 与整洁架构的边界（方案 A：AI 作为平级 feature 文件夹）
- `AI/` 是与 `Domain/ Application/ Infrastructure/` 平级的 feature 文件夹，集中放置所有 AI 代码（Clients / Agents / Tools / Skills / Prompts / Options），便于整体理解、测试与演进
- 逻辑职责归属（物理上仍集中在 `AI/`，**不拆散**到其他层）：
  - `AI/Clients/`（`IChatClient` 注册、模型选择）偏基础设施职责，不含业务规则
  - `AI/Agents/`（Agent 编排、工具调度）偏应用层职责
  - `AI/Tools/` 中的工具是对 Repository / Domain 能力的薄封装，不重复业务规则
- 工具最终触达的数据访问走 Repository，业务规则走 Domain；工具本身只做调用与结果映射
- Prompt 是资源而非领域逻辑，统一放 `AI/Prompts/`
- 将来若 AI 代码膨胀，可把 `AI/` 整体升级为独立项目，代码几乎不动

---

## Agent 规范

### 从最小 Agent 起步
- 第一个 Agent 只承接一条主链路，先跑通闭环，再逐步加 Skills、记忆、中间件、多 Agent
- 不要一上来就搭复杂 Workflow 引擎或多 Agent 网络

### 创建与注册
- 本项目主用 `ChatClientAgent`（基于 `IChatClient` 的具体 Agent），构造签名：`new ChatClientAgent(IChatClient chatClient, string? instructions = null, string? name = null, string? description = null, IList<AITool>? tools = null, ...)`
- Agent 注册到 DI，业务层注入抽象，不直接 `new` 具体实现
- 一个 Agent 职责单一；职责膨胀时再拆分，而不是把所有能力塞进一个 Agent

### 最小示例（API 形态核对自官方 get-started 样例）
```csharp
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

// 1. IChatClient（OpenAI 兼容端点，如 DeepSeek / GitHub Models / 本地）由 DI 注入
// 2. 创建基于 IChatClient 的 Agent
AIAgent agent = new ChatClientAgent(
    chatClient,
    instructions: "你是一个写作助手，输出 JSON。",
    name: "WritingAgent");

// 3. 非流式调用
string reply = await agent.RunAsync("帮我列三个选题。");

// 4. 流式调用（前端打字机）
await foreach (var update in agent.RunStreamingAsync("帮我列三个选题。"))
{
    // 增量输出 update
}
```

### 工具 / Skills 挂载
- 工具用 `AIFunctionFactory.Create(...)` 包装，方法与参数用 `[Description]`（`System.ComponentModel`）清晰描述语义；描述质量直接影响模型调用准确度
- 工具通过 `ChatClientAgent` 的 `tools` 参数或 `ChatOptions.Tools` 挂载
- Skills 通过 `AgentSkillsProvider`（配 `AgentSkillsProviderBuilder`）挂载；只挂载当前 Agent 真正需要的，不为"以后可能用"预挂一堆
- 工具必须真实生效并可持久化（若涉及业务变更），不得返回 mock 占位而长期不补真实链路
- **工具审批（来自源码安全说明）**：框架默认对工具调用**不要求用户批准**，模型自行选择函数与参数。有副作用、访问敏感数据或不可逆的工具，必须显式启用审批（tool approval），见「AI 安全与成本规范」

### 运行时控制
- 涉及成本 / 安全的工具调用，必要时引入审查（中间件或人工确认），高风险动作不得由模型直接执行而无校验
- 长任务或循环执行要设置合理的迭代上限，防止失控循环

---

## Prompt 规范

### 管理方式
- Prompt 集中放 `AI/Prompts/`，不散落在业务代码字符串里
- 系统提示、角色设定、输出格式约束、少样本示例与业务变量分开组织，便于审查与测试
- Prompt 变更视为高风险改动（影响输出行为），需在最终说明中写清改动原因与验证方式

### 编写原则
- 明确角色、任务、输入、输出格式与约束；输出格式尽量结构化（JSON / 固定字段），便于程序解析与前端展示
- 不在 Prompt 中放置密钥、真实生产地址或用户隐私
- 跨模型迁移时注意：不同模型对指令、JSON 输出、工具调用的遵循度不同，迁移后必须重新验证关键 Prompt

---

## 流式与长任务规范
- 对话或长文本生成优先用流式输出，前端呈现打字机效果；后端用框架提供的流式 API，不要自己拼 SSE 轮子（除非有明确理由）
- 流式响应需处理：中途取消、网络中断、超时、空响应
- 长任务（多步 Agent / 工具链）需向用户暴露进度或阶段，避免长时间无反馈；进度结构应前后端约定一致
- 流式接口的契约（事件类型、字段）变更时，按 `doc-AGENTS.md` 的契约流程同步前端

---

## 可选扩展能力规范（引入时再读）

> 以下能力默认不引入。只有当核心闭环已稳定、且出现明确需求时，按本节规范评估引入。

- **向量存储 / 检索**：出现 RAG 或语义检索需求时，通过 `Microsoft.Extensions.VectorData` 抽象接入；Embedding 走 `IEmbeddingGenerator`，与 `IChatClient` 同源管理；向量库选择与迁移成本需先与用户确认
- **Agent 记忆**：需要跨会话上下文累积时引入；记忆存储后端（DB / 向量库）需明确，记忆写入与淘汰策略需可配置
- **多 Agent 编排**：单 Agent 边界稳定、确有协作需求时再拆；编排要可观测、可回退，避免隐式循环依赖
- **MCP**：需要对接外部工具生态时引入；外部工具的能力、权限、计费边界需先确认

引入任何可选能力时：
1. 先在对应模块的 `doc/requirements/<module>/<feature>.md` 记录需求，并在 `doc/design/<module>/<feature>.md` 说明方案与影响
2. 评估对成本、延迟、复杂度的影响
3. 与用户确认后再实现，不默认扩大范围

---

## AI 安全与成本规范
- 模型密钥、Base URL 仅存于环境变量，不得入库、不得入镜像、不得下发前端
- 高风险工具调用（删除、外发、计费、不可逆操作）必须有人工确认或审查环节，禁止模型直接执行
- 用户输入与模型输出都需考虑注入与越权风险；模型生成的指令 / 参数仍需业务层校验，不盲信
- 关注 token 用量与调用频次；循环或批量调用需有上限与可观测指标

---

## 测试规则

### 提交前最小回归
- 默认执行：项目现有的静态检查、类型检查或编译校验
- 模型调用 / Agent 改动：至少用 mock `IChatClient` 验证一次输入输出或工具调度链路
- Prompt 改动：至少验证关键输出格式与边界（空输入、超长输入、异常输入）
- 工具 / Skills 改动：至少验证工具被正确选择并真实生效（可用等价验证替代真实模型调用）
- 若仓库暂时缺少自动化测试基础，可使用脚本 / 命令行 / 真实模型小样例验证替代，但必须能证明核心链路真实生效

<!-- include: shared/common/general-requirements.md -->

### AI 测试
- 单元 / 集成测试统一 mock `IChatClient`，不真实调用第三方模型服务（避免成本、不稳定与隐私问题）
- Prompt 测试关注输出格式与关键约束，不追求逐字一致
- Agent 测试覆盖：工具选择、多步执行、异常分支、流式中断
- 真实模型验证作为补充手段，不计入自动化回归；调用真实模型时注意密钥与数据安全

### 外部依赖与数据
- 测试中不要真实调用第三方服务，统一使用 mock
- 测试数据应尽量最小化、可读、可重复执行
- 不要让测试依赖本地人工状态或真实密钥

<!-- include: shared/no-test-fallback.md -->

---
