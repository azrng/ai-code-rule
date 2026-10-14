# Microsoft Agent Framework for .NET

目标项目为 C# 或其他 .NET 语言时使用本参考。

## 权威来源

- 仓库：<https://github.com/microsoft/agent-framework/tree/main/dotnet>
- Samples：<https://github.com/microsoft/agent-framework/tree/main/dotnet/samples>
- 入门 walkthrough：<https://learn.microsoft.com/agent-framework/get-started/your-first-agent>

## 安装

按模型后端装 provider 包（provider 会传递引用核心包 `Microsoft.Agents.AI`，不要单装核心包起步）。团队实战主路径（OpenAI / OpenAI 兼容网关）：

```bash
dotnet add package Microsoft.Agents.AI.OpenAI
```

接入形态（DI + `Microsoft.Extensions.AI`）：

```csharp
// OpenAIClient（可经 OpenAIClientOptions.Endpoint 指向兼容网关）→ 注册 IChatClient
builder.Services.AddChatClient(openaiClient.GetChatClient(modelId).AsIChatClient())
       .UseLogging();

// 消费方注入 IChatClient 后建 agent
AIAgent agent = _chatClient.AsAIAgent(instructions: "...");
var session = await agent.CreateSessionAsync();
var result = await agent.RunAsync("...", session);
```

- 其他 provider 包：`Microsoft.Agents.AI.Foundry`（官方 get-started 示例路径，配 `Azure.AI.Projects` + `Azure.Identity`）、`Microsoft.Agents.AI.Anthropic`、`Microsoft.Agents.AI.AzureAI` 等，完整选型见 `packages.md`。
- 多后端用 keyed services：`AddKeyedChatClient("name", client)`，见 `pitfalls.md`。
- Samples 当前目标框架为 `net10.0`。

## .NET 侧约定

- agent 与工作流操作一致使用 `async`/`await`。
- 遵循 .NET 类型安全与依赖注入约定。
- 服务注册、配置、认证对齐标准 .NET hosting 模式。
- middleware、context provider、编排组件按 .NET 应用模型的惯用法使用。
- 引入新 API 或工作流模式前，先查最新 samples（索引见 `samples-index.md`）。
