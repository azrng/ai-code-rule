# 08 - MCP 集成

## MCP 客户端架构

Claude Code 作为 MCP 客户端，连接到外部 MCP Server 获取工具、命令和资源。

```
Claude Code (Client)
  ├── MCP Server 1 (stdio)
  ├── MCP Server 2 (SSE)
  └── MCP Server 3 (WebSocket)
       ├── Tools (转换为 Tool 接口)
       ├── Commands (转换为 Skill)
       └── Resources (可读取资源)
```

## 核心模块

### MCP 客户端（`src/services/mcp/client.ts`）

```typescript
// 获取 MCP 工具、命令和资源
async function getMcpToolsCommandsAndResources(
  clients: MCPServerConnection[]
): Promise<{ tools: Tool[], commands: Command[], resources: Record<string, ServerResource[]> }>

// 调用 MCP 工具
async function callMCPTool(
  tool: Tool, input: unknown, context: ToolUseContext
): Promise<MCPToolCallResult>

// 重连 MCP 服务器
async function reconnectMcpServerImpl(
  serverName: string, config: ScopedMcpServerConfig
): Promise<void>
```

### MCP 工具命名约定

```
mcp__<server_name>__<tool_name>
```

- `normalizeNameForMCP(name)` — 将服务器名转为安全标识符
- `buildMcpToolName(server, tool)` — 构建完整工具名

### MCP 工具转换

外部 MCP 工具被转换为内部 `Tool` 接口：

```typescript
// src/services/mcp/client.ts
function isIncludedMcpTool(toolName: string, config: ScopedMcpServerConfig): boolean
```

MCP 工具的特殊属性：
- `isMcp: true` — 标识为 MCP 工具
- `mcpInfo: { serverName, toolName }` — 来源信息
- `inputJSONSchema` — 直接使用 JSON Schema（非 Zod）

## MCP 配置

### 配置来源

```typescript
// settings.json 中的 MCP 配置
{
  "mcpServers": {
    "my-server": {
      "type": "stdio",           // stdio | sse | websocket
      "command": "node",
      "args": ["server.js"],
      "env": { "API_KEY": "..." }
    }
  }
}
```

### 配置合并

多来源配置按优先级合并：
- `pluginSettings` — 插件提供的 MCP 服务器
- `userSettings` — 用户全局配置
- `projectSettings` — 项目配置
- `localSettings` — 本地配置

## MCP 工具池组装

```typescript
// src/tools.ts
function assembleToolPool(permissionContext, mcpTools): Tools {
  const builtInTools = getTools(permissionContext)
  const allowedMcpTools = filterToolsByDenyRules(mcpTools, permissionContext)

  // 内置工具前缀 + MCP 工具后缀（保序）
  return uniqBy(
    [...builtInTools].sort(byName).concat(allowedMcpTools.sort(byName)),
    'name'
  )
}
```

## MCP 认证

### OAuth 流程

```typescript
// src/tools/McpAuthTool/McpAuthTool.ts
function createMcpAuthTool(serverName, config): Tool {
  return {
    name: buildMcpToolName(serverName, 'authenticate'),
    async call(_input, context) {
      // 启动 OAuth 流程
      // 返回 auth URL 给用户
    }
  }
}
```

当 MCP 服务器需要认证时，自动创建 `mcp__<server>__authenticate` 工具。

## MCP 资源

```typescript
// 列出资源
const ListMcpResourcesTool: Tool = {
  name: 'ListMcpResources',
  async call() {
    return Object.entries(mcpResources).map(([server, resources]) => ...)
  }
}

// 读取资源
const ReadMcpResourceTool: Tool = {
  name: 'ReadMcpResource',
  async call({ server, uri }) {
    return readMcpResource(server, uri)
  }
}
```

## MCP 工具过滤

```typescript
// src/services/mcp/utils.ts
function filterToolsByServer(tools, serverName): Tool[]
function excludeToolsByServer(tools, serverName): Tool[]
function commandBelongsToServer(command, serverName): boolean
function isMcpTool(tool): boolean
function excludeStalePluginClients(mcp, configs): { stale: MCPServerConnection[] }
```

## 动态 MCP 服务器

MCP 服务器可在运行时动态添加/移除：
- `setupSdkMcpClients()` — SDK 模式下设置 MCP 客户端
- `excludeStalePluginClients()` — 清理过期的插件客户端
- 配置变更时自动重连

## MCP 指令注入

MCP 服务器可在系统提示词中注入指令：
- `isMcpInstructionsDeltaEnabled()` — 检查是否启用
- 通过 `mcpClients` 参数传递给 `fetchSystemPromptParts()`
