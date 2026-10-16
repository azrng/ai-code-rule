# 包选型地图

按需安装，不要一次全装。核心原则：**先选 provider（模型后端），provider 会传递引用核心包**；再按部署、编排、工具需求叠加。

以下分类以仓库 `dotnet/src` 为准（核实于 2026-09-19，包版本 1.22.0 正式版号发布），新包会持续出现，不确定时看仓库目录。

## 核心（provider 传递引用，通常无需手装）

| 包 | 说明 |
| --- | --- |
| `Microsoft.Agents.AI` | 核心功能：agent 抽象与实现（如 `ChatClientAgent`） |
| `Microsoft.Agents.AI.Abstractions` | 接口与抽象层 |
| `Microsoft.Agents.AI.Workflows` | 工作流编排（executor / edge / 图执行） |

## Provider（按模型后端二选一或多选）

| 包 | 适用后端 |
| --- | --- |
| `Microsoft.Agents.AI.Foundry` | Azure AI Foundry（官方 get-started 主路径，`AIProjectClient.AsAIAgent(...)`） |
| `Microsoft.Agents.AI.OpenAI` | OpenAI |
| `Microsoft.Agents.AI.Anthropic` | Anthropic Claude |
| `Microsoft.Agents.AI.AzureAI` | Azure AI 相关 provider |
| `Microsoft.Agents.AI.AzureAI.Persistent` | Azure AI Persistent Agents（旧 Foundry Agents v1） |
| `Microsoft.Agents.AI.GitHub.Copilot` | GitHub Copilot SDK |
| `Microsoft.Agents.AI.CopilotStudio` | Copilot Studio |

## 跨 agent 协议

| 包 | 说明 |
| --- | --- |
| `Microsoft.Agents.AI.A2A` | Agent2Agent（A2A）协议 |
| `Microsoft.Agents.AI.AGUI` | AG-UI（Agent UI Protocol）服务端/客户端 |

## Hosting（生产部署）

| 包 | 说明 |
| --- | --- |
| `Microsoft.Agents.AI.Hosting` | agent 宿主基础 |
| `Microsoft.Agents.AI.Hosting.AspNetCore` | ASP.NET Core 宿主 |
| `Microsoft.Agents.AI.Hosting.AzureFunctions` | Azure Functions 宿主 |
| `Microsoft.Agents.AI.Hosting.AzureStorage` / `.OpenAI` | 存储等宿主配套 |
| `Microsoft.Agents.AI.Hosting.A2A` / `.A2A.AspNetCore` / `.AGUI.AspNetCore` | 协议宿主配套 |
| `Microsoft.Agents.AI.Foundry.Hosting` | 用 Azure AI Agent Service 宿主 Foundry Agents（⚠ 仅支持 Foundry Responses 容器协议 v2.0，不兼容 v1） |
| `Aspire.Hosting.AgentFramework.DevUI` | .NET Aspire 集成 |

## 工作流进阶

| 包 | 说明 |
| --- | --- |
| `Microsoft.Agents.AI.Workflows.Declarative`（及 `.AzureAI` / `.Foundry` / `.Mcp`） | YAML 声明式工作流 |
| `Microsoft.Agents.AI.Workflows.Generators` | Roslyn 源生成器，编译期 executor 路由配置 |
| `Microsoft.Agents.AI.DurableTask` | 持久化任务（配套 samples：`04-hosting/DurableWorkflows`） |

## 工具与集成

| 包 | 说明 |
| --- | --- |
| `Microsoft.Agents.AI.Mcp` | MCP 支持，含 MCP Tasks 扩展（SEP-2663）客户端集成 |
| `Microsoft.Agents.AI.Tools.Shell` | 跨平台 shell 工具（`LocalShellExecutor` / `DockerShellExecutor`，审批在环） |
| `Microsoft.Agents.AI.Hyperlight` | Hyperlight 沙箱代码执行（CodeAct） |
| `Microsoft.Agents.AI.LocalCodeAct` | 本地 Python CodeAct（AST 校验；需外部沙箱） |
| `Microsoft.Agents.AI.Harness` | `HarnessAgent`：面向长任务的预配置 agent，内置 planning/todo/mode 工具 |
| `Microsoft.Agents.AI.AgentHooks` | Agent-Hooks 拦截协议（AGENT-HOOKS-0.1，fail-closed 策略拦截） |
| `Microsoft.Agents.AI.Mem0` | Mem0 记忆集成 |
| `Microsoft.Agents.AI.CosmosNoSql` | Cosmos DB NoSQL：`ChatHistoryProvider` / `CheckpointStore` |
| `Microsoft.Agents.AI.Valkey` | Valkey：聊天历史持久化 + 全文检索 context provider |
| `Microsoft.Agents.AI.Purview` | Microsoft Purview 合规对接 |
| `Microsoft.Agents.AI.DevUI` | 开发期调试 Web UI |
