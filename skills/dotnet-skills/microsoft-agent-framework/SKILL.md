---
name: microsoft-agent-framework
description: "Microsoft Agent Framework (.NET) 开发指南：agent、工作流编排、宿主部署、包选型与升级。Keywords: Agent Framework, MAF, Microsoft.Agents.AI, AIAgent, ChatClientAgent, Workflow, Executor, MCP, A2A, AG-UI, Harness, Declarative, Semantic Kernel, AutoGen 迁移."
---

# Microsoft Agent Framework 开发指南

Microsoft Agent Framework 是 Semantic Kernel 与 AutoGen 的统一后继者。**迭代很快**（核对基线 2026-09-19 / 最新包 1.22.0）：包自 1.0.0 起按正式版号发布（无 prerelease 后缀），但**官方文档仍将产品标注为 public preview——API 仍会引入 breaking 变更**。API 级结论一律以官方文档与 samples 为准，本 skill 提供选型、索引与升级预警。

**适用场景**：
- 基于 `Microsoft.Agents.*` 包的 .NET agent / 工作流应用
- SK / AutoGen 项目迁移到 Agent Framework
- maf 版本升级影响评估

**不适用场景**：
- Python 侧（本 skill 只覆盖 .NET；Python samples 在仓库 `python/` 下）
- 仍维护在 Semantic Kernel / AutoGen 上、无迁移计划的项目

## 按需查阅

本文件只做路由，按当前任务读对应参考文档即可，不必全部加载。

| 任务场景 | 参考文档 |
| --- | --- |
| 安装、provider 接入、.NET 侧约定 | `references/dotnet.md` |
| 装哪个包（provider / hosting / 工作流进阶 / 工具集成） | `references/packages.md` |
| 找参考实现：官方 samples 按场景索引 | `references/samples-index.md` |
| 版本升级前的 breaking 变更预警 | `references/breaking-changes.md` |
| 团队实战踩坑（Experimental/MCP 无会话/会话持久化/多后端） | `references/pitfalls.md` |

典型组合：新项目从 `packages.md` 选包 → `samples-index.md` 找同型示例抄结构；升级现有项目先读 `breaking-changes.md`。

## 核心约定（任何任务都适用）

- agent 与 workflow 的操作一律 `async`/`await`。
- agent 用于自主决策、规划、对话、工具调用、MCP 交互；workflow 用于多步编排、预定义执行图、长任务、人在环。
- 显式错误处理与日志；强类型、清晰接口。
- Azure 认证场景用 `DefaultAzureCredential`（生产环境注意凭据回退问题，改用具体凭据类型）。
- 旧 SK / AutoGen 模式只作为迁移输入，不是默认实现模型。

## 迁移指引

- 从 Semantic Kernel 迁移：<https://learn.microsoft.com/agent-framework/migration-guide/from-semantic-kernel/>
- 从 AutoGen 迁移：<https://learn.microsoft.com/agent-framework/migration-guide/from-autogen/>
- 先保行为一致，再渐进采用原生模式。

## 高频陷阱速览

详情与佐证见 `references/pitfalls.md`（实战提炼）与 `references/breaking-changes.md`（preview 期很多"坑"其实是 breaking 变更）。

- Experimental API 编译警告：按来源加 `[Experimental("MEAI001"/"OPENAI001"/"MAAI001")]`，别绕路
- MCP C# SDK 2.x 默认无会话（2026-07-28 协议）：没有 initialize 握手和 `Mcp-Session-Id`；直连时 `Accept` 双值、`Mcp-Method`/`Mcp-Name`/`_meta` 缺一不可
- maf 全家桶版本要对齐，新旧版本并存的工程先统一再排查行为差异
- 会话持久化用 `SerializeSessionAsync`/`DeserializeSessionAsync`，不是存 thread 对象

## 完成标准

- 包名与安装方式与 `packages.md` 一致，不凭记忆引用不存在的 API。
- 参考实现优先指向官方 samples 的具体目录，而不是现场编造代码。
- 迁移建议只在项目确有 SK / AutoGen 历史时展开。

## 参考资源

- [官方文档](https://learn.microsoft.com/agent-framework/overview/agent-framework-overview)
- [官方仓库 .NET](https://github.com/microsoft/agent-framework/tree/main/dotnet)
- [入门 walkthrough](https://learn.microsoft.com/agent-framework/get-started/your-first-agent)
