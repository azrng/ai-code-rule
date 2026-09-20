# 官方 Samples 索引

找参考实现优先查官方 samples，别凭记忆写 API。仓库路径 `dotnet/samples`（核实于 2026-09-18）：
<https://github.com/microsoft/agent-framework/tree/main/dotnet/samples>

入门后进阶的推荐阅读顺序：`01-get-started` 全部 → `03-workflows/_StartHere`（按 01~07 编号顺序）→ 按下表跳转。

## 01-get-started（入门主线）

| 示例 | 内容 |
| --- | --- |
| `01_hello_agent` | 最小 agent：`AIProjectClient` + `AsAIAgent`，运行与流式 |
| `02_add_tools` | 给 agent 加工具 |
| `03_multi_turn` | 多轮对话 |
| `04_memory` | 记忆 |
| `05_first_workflow` | 第一个工作流 |
| `06_host_your_agent` | 宿主你的 agent |

## 02-agents（agent 能力全景）

| 想做什么 | 去 |
| --- | --- |
| 各 provider 建 agent（Foundry/OpenAI/Anthropic/Azure） | `AgentProviders/`（含 `azure/Agent_With_AzureOpenAIChatCompletion` 等） |
| 循序渐进的 agent 教程 | `Agents/`（README 有分步指引） |
| RAG | `AgentWithRAG/` |
| 记忆 | `AgentWithMemory/` |
| 沙箱代码执行（CodeAct/Hyperlight） | `AgentWithCodeAct/` |
| OpenTelemetry 可观测 | `AgentOpenTelemetry/` |
| MCP | `ModelContextProtocol/` |
| Agent Skills（含自动审批） | `AgentSkills/` |
| 长任务 Harness（内置 planning/todo/mode） | `Harness/` |
| YAML 声明式 agent | `DeclarativeAgents/` |
| AG-UI 协议 | `AGUI/` |
| 开发调试 UI | `DevUI/` |
| A2A | `A2A/` |
| 评测 | `Evaluation/` |

## 03-workflows（工作流，先看 `_StartHere`）

| 主题 | 去 |
| --- | --- |
| 核心概念（executor/edge/流式/agent 入图/子工作流/模式组合） | `_StartHere/` 01~07 按序 |
| 多 agent 协作、群聊 + 工具审批（人在环） | `Agents/`（`GroupChatToolApproval`、`WorkflowAsAnAgent` 等） |
| 并行 fan-out/fan-in | `Concurrent/` |
| 循环 | `Loop/` |
| 共享状态 | `SharedStates/` |
| 条件路由 / switch / 多选分发 | `ConditionalEdges/` 01~03 |
| 编排模式（如 magentic） | `Orchestration/` |
| 检查点与恢复 | `Checkpoint/` |
| 人在环 | `HumanInTheLoop/` |
| YAML 声明式工作流 | `Declarative/` |
| 工作流可观测 | `Observability/` |
| 评测 | `Evaluation/` |
| 可视化 | `Visualization/` |

## 04-hosting（部署形态）

| 主题 | 去 |
| --- | --- |
| 持久化 agent / 工作流 | `DurableAgents/`、`DurableWorkflows/` |
| Foundry 宿主 agent | `FoundryHostedAgents/` |
| AF hosting 基础 | `af-hosting/` |

## 05-end-to-end（端到端参考应用）

A2A 客户端/服务端、AGUI WebChat、AgentWebChat、AspNetCore agent 授权、DevUI+Aspire 集成、评测、M365 Agent 等——做完整应用前先扫一遍这个目录，很可能已有同型骨架。

## Python 侧

本 skill 只索引 .NET。Python samples 在仓库 `python/samples`，走 `samples-index` 同思路但需另行核对。
