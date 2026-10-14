# 近期 Breaking 变更摘要

框架处于 public preview，breaking 变更频繁。以下是 2026-07-27（本 skill 上次核对基线）之后 .NET 侧的主要 breaking/行为变更，**按提交标题与正文整理，未逐一验证代码级影响**——升级前务必对照官方 release notes 与迁移说明。

核对日期：2026-09-19（基于本地仓库 `agent-framework` @ 723256961，包版本 1.22.0——正式版号发布，但产品仍处 public preview，breaking 会持续出现）。

## 变更列表（新→旧）

| PR | 变更 | 升级影响 |
| --- | --- | --- |
| #8531 | `ChatClientAgent` 工具改为按 run 传递，不再预设置在 chat client 上 | 预挂在 chat client 上的工具写法失效，改为每次运行传入 |
| #8425 | provider 支撑的 MCP 会话按调用（per-invocation）划分 | 自定义 MCP client-provider 调用每次独立建/清会话；默认会话缓存行为保留 |
| #8375 | 重放（replay）支持改进：Approval Binding | 使用检查点重放 + 审批流的代码需对照新契约调整 |
| #8290 | MCP skill 归档仅支持 ZIP 格式 | 用其他压缩格式分发 skill 包的会加载失败 |
| #8032 | 澄清 A2A agent run modes | A2A run options 现在总是创建，与 agent options 解耦 |
| #7991 | `AgentSessionStore` 提升进 `Microsoft.Agents.AI.Abstractions` [PREVIEW BREAKING] | 命名空间/引用位置变化 |
| #7986 | 移除 `Azure.AI.OpenAI` 依赖 | Azure OpenAI 接入路径变化，端点规范化逻辑内移 |
| #7774 | MCP 长任务迁移到 2026-07-28 Tasks 扩展（SEP-2663） | 旧 MCP 长任务 API 失效 |
| #7671 | 新增 `file_access_read_lines`，行号契约移到 `AgentFileStore` | 文件访问工具与自定义 AgentFileStore 实现需跟进 |
| #7567 | 重命名为 `AgentIsolationKeyProvider` | 旧类型名失效 |

## 使用方式

- 本文件只做"升级预警索引"，不复制官方细节；每条的权威说明看 PR 链接与官方 release notes。
- 维护本文件时：`cd <maf 本地仓库> && git log --since="<上次核对日期>" --oneline -- dotnet | grep -iE "BREAKING"` 重新生成候选列表后人工筛选。
