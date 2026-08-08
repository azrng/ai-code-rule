## [2026-08-08]：reactLinkdotNet-ai 同步 KISS 可读性约束、去重精简与版本统一 1.10.0

跟踪状态表：

| 文件夹名称 | 当前子任务状态 | 备注 |
|-----------|----------------|------|
| `reactLinkdotNet-ai` | `已完成` | 停在 7-26（1.1.x），错过 8/7 全模板批次；镜像姊妹模板 reactLinkdotNet（commit `9bdf3c6`）治理一步到位，含本模板独有 `ai-AGENTS.md` |

### 修改原因（通用）

`reactLinkdotNet-ai`（AI Agent 产品向的 React + .NET 全栈模板）停在 2026-07-26 的 1.1.x 版本，完全错过 8/7「全 .NET 模板同步 KISS + 版本统一 1.10.0」那批治理（8/7 合并记录跟踪表中无本模板）。其姊妹模板 `reactLinkdotNet`（同技术栈、非 AI 向）已在 commit `9bdf3c6` 完成 KISS 治理。本次把同一治理净效果镜像到 `reactLinkdotNet-ai`，版本统一到 1.10.0。参考 `skills/human-first-kiss-engineering` 工程心法，措辞照 `9bdf3c6`，本地化保留 AI / Agent 维度，不删语义。

### 通用修改内容

- 根 `AGENTS.md`：「根文件原则」加 KISS 心法行；合并交付三节为「交付与验证」（保留 AI 维度）
- `backend-AGENTS.md`：删阅读摘要；主动建议去复用 / 查证共性加根指针；新增「核心逻辑可读性(KISS)」「反模式与修根因清单」「可维护性自检」；强化注释规范行内注释条款
- `ai-AGENTS.md`（本模板独有）：删阅读摘要；主动建议去共性加根指针；**不加** KISS 三件套
- `doc-AGENTS.md`：删阅读摘要；主动建议去共性加根指针；阶段 0 新增「设计自检清单」+ 设计阶段禁忌
- `frontend-AGENTS.md`：删阅读摘要；主动建议删复用条加根指针，留领域特化
- `infra-AGENTS.md`：删阅读摘要；主动建议去共性加根指针
- 共性规则统一指向根「模型行为约束与主动建议」，子规则只留领域特化；6 文件版本统一 1.1.x → 1.10.0

### 各模板差异与状态

#### `reactLinkdotNet-ai`（已完成，一步到位）

- 根文件原则加 KISS 心法行；合并交付三节为「交付与验证」，分层验证清单保留 AI 维度（AI 改动按 `ai-AGENTS.md` 执行 / 模型对接确认密钥来源·流式·工具调用·异常分支 / smoke test 含 Agent 核心链路 / 环境变量含模型相关）
- backend：删阅读摘要；主动建议保留 3 条领域特化（业务放错层 / 接口契约风险 / 模型调用越界 `IChatClient`）；新增 KISS / 反模式 / 自检；强化行内注释保留「模型调用风险」维度
- ai-AGENTS（独有）：删阅读摘要；主动建议保留 4 条 AI 领域特化（绕过 `IChatClient` / 密钥硬编码 / Prompt 散落 / 堆 Skills 未闭环）
- doc：删阅读摘要；主动建议保留 2 条（需求缺项 / 方案不一致）；阶段 0 加设计自检清单
- frontend：删阅读摘要；主动建议删复用条加指针，保留样式冲突 / 职责拆分 / 表单校验 / Agent 输出展示等领域特化
- infra：删阅读摘要；主动建议删复用 + 查证条加指针，保留不一致 / 密钥硬编码 / 高风险确认领域特化
- 版本：agents-root 1.1.0→1.10.0、backend 1.1.1→1.10.0、ai 1.1.0→1.10.0、doc 1.1.0→1.10.0、frontend 1.1.0→1.10.0、infra 1.1.0→1.10.0

### 适配

- KISS / 反模式 / 自检**仅加 backend**，沿用 commit `9bdf3c6`「KISS/反模式/自检仅加 backend」决策；`ai-AGENTS.md` 只做去重
- 任务管理为简化口径（与 reactLinkdotNet 同），无「任务状态触发条件」冗余小节，未做该项合并
- 本模板相对 reactLinkdotNet 多 `ai-AGENTS.md`，该文件只做去重（删阅读摘要 + 主动建议加根指针），不与 reactLinkdotNet 强行对齐

### 修改文件

- `reactLinkdotNet-ai`：AGENTS、backend、ai、doc、frontend、infra（6 个规则文件）

### 当前子任务状态

- 全部 `已完成`

### 补充：AI 进步驱动的小修正（同批落地）

核查 AI 进步下是否有规则已过时（按「查证优先级」对照官方文档核实：`Microsoft.Extensions.AI` + `IChatClient`、`Microsoft.Agents.AI` 含 `AIAgentBuilder` / `AgentSkillsProvider` 均为当前，Agent Skills 已 GA，Tailwind + shadcn / .NET 10 当前）。结论是 AI 专属规则全部命中当前生态、且文档已有「API 演进较快、禁止凭记忆写死」hedge 兜底漂移，不删任何规则；仅在 `frontend-AGENTS.md` 做两处小修正：

- React 版本：`React 18（或 19）` → `React 19`（当前稳定线 19.2.x，「（或 19）」犹豫已过时）
- 前端测试：`前端暂时不考虑单元测试` → 鼓励用 AI 辅助生成关键交互与组件单测（Vitest + React Testing Library），仍以手动 smoke test 兜底核心路径（AI 让单测成本大降，原「暂时不考虑」前提弱化）

### 备注

- 镜像参照：commit `9bdf3c6`（reactLinkdotNet「同步 KISS 可读性约束与去重精简，版本统一 1.10.0」）+ `skills/human-first-kiss-engineering`。
- 8/7 合并记录 `2026-08-07-dotnet全模板-同步KISS去重精简与版本统一.md` 跟踪表未含本模板，本记录为补齐。
