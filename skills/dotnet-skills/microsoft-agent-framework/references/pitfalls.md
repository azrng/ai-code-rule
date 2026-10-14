# 实战陷阱（LLMSample 提炼）

来源：团队实战项目 `daily-examples/collectSample/LLMSample`（核对于 2026-09-19，基于 maf 1.15~1.18 + ModelContextProtocol 2.2.0）。每条都有代码或提交佐证，升级版本后注意复核是否仍成立。

## 1. Experimental 诊断 ID 编译警告

大量常用 API 处于实验期，消费方类型/方法上必须加 `[Experimental("...")]`，否则编译警告（`TreatWarningsAsErrors` 项目直接挂）。实战命中过的三类：

| 诊断 ID | 来源 | 命中场景 |
| --- | --- | --- |
| `MEAI001` | Microsoft.Extensions.AI | context provider、middleware、`InMemoryChatHistoryProvider`/ChatReducer |
| `OPENAI001` | OpenAI SDK | background response（`RunStreamingAsync` 后台响应） |
| `MAAI001` | Microsoft.Agents.AI | 部分示例 agent API |

见到" Experimental API "警告（`SYSLIB1250` 系列）别绕路实现，加对应标注即可。

## 2. MCP：ModelContextProtocol 2.x 默认无会话（2026-07-28 协议）

C# MCP SDK（`ModelContextProtocol` 包，独立于 maf 版本演进）2.x 默认 `HttpServerSessionMode.Stateless`：

- **没有 initialize 握手、没有 `Mcp-Session-Id`**，每个请求独立自描述。旧文档/旧示例的会话流程全部失效。
- 显式设置：`options.SessionMode = HttpServerSessionMode.Stateless;`
- 直接 HTTP 调用（不经 SDK client）时的请求头细则，错了全是协议层报错：
  - `Accept` 必须同时含 `application/json` 与 `text/event-stream`，否则 **406**
  - 必须带 `MCP-Protocol-Version: 2026-07-28` 与 `Mcp-Method: <方法名>`
  - `tools/call` 额外要求 `Mcp-Name: <工具名>` 且与 body 的 `name` 一致，不一致 **-32020**
  - JSON-RPC `params._meta` 必须含 `io.modelcontextprotocol/protocolVersion` 等元数据，缺失 **400**
- 需要会话语义（跨请求状态）时显式用 `Stateful` 并自行核对 initialize 流程。
- 工具按调用方过滤：无会话模式下过滤逻辑每请求执行一次（如按 JWT claim 过滤可见工具），别按"每会话一次"设计。

## 3. 会话持久化与上下文压缩的组合

- 长对话压缩用 `ChatClientAgentOptions.ChatHistoryProvider`：`InMemoryChatHistoryProvider` + `ChatReducer`（`MessageCountingChatReducer` 截断条数 / `SummarizingChatReducer` 超阈值自动摘要），触发时机由 `ReducerTriggerEvent` 控制。
- 会话序列化恢复：`agent.SerializeSessionAsync(session)` / `DeserializeSessionAsync(...)`——持久化不是存整个 thread 对象。
- 网关/多跳场景：会话标识要透传（如 mcp-agent 会话 ID 过网关），中间层别重写或不传会话头。

## 4. 多后端 IChatClient：keyed services

一个应用多个模型后端时：`builder.Services.AddKeyedChatClient("AzureOpenAI", client)` + `sp.GetRequiredKeyedService<IChatClient>("AzureOpenAI")`，agent 工厂内按键取。OpenAI 兼容网关用 `OpenAIClientOptions.Endpoint` + 自定义 `HttpClientPipelineTransport` 指过去。

## 5. 版本对齐

maf 是全家桶式发布：`Microsoft.Agents.AI.OpenAI`、`Hosting.*`、`Workflows.*` 等要一起升到同一版本号（实战项目曾并存 1.1.0/1.15.0/1.18.0，旧项目挂新包会出现行为差异）。升级前先过 `breaking-changes.md`。
