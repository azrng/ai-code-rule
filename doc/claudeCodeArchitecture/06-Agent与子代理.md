# 06 - Agent 与子代理

## AgentTool 概述

`AgentTool`（`src/tools/AgentTool/AgentTool.tsx`）是 Claude Code 的核心工具，允许主会话创建子代理来并行处理任务。

## Agent 输入 Schema

```typescript
z.object({
  description: z.string(),           // 3-5 词任务描述
  prompt: z.string(),                // 子代理的完整任务指令
  subagent_type: z.string().optional(),  // 代理类型（内置或自定义）
  model: z.enum(['sonnet', 'opus', 'haiku']).optional(),  // 模型覆盖
  run_in_background: z.boolean().optional(),  // 后台运行
  name: z.string().optional(),       // 可命名，供 SendMessage 寻址
  team_name: z.string().optional(),  // 团队名称
  mode: z.string().optional(),       // 权限模式
  isolation: z.enum(['worktree']).optional(),  // 隔离模式
})
```

## 内置 Agent 类型

| 类型 | 描述 | 用途 |
|------|------|------|
| `general-purpose` | 通用代理 | 复杂多步骤任务 |
| `Explore` | 探索代理 | 快速代码搜索 |
| `Plan` | 计划代理 | 设计实现方案 |

内置类型定义在 `src/tools/AgentTool/built-in/` 目录下。

## 自定义 Agent

通过 `.claude/agents/` 目录定义（JSON 或 Markdown）：

```json
{
  "name": "my-agent",
  "description": "自定义代理描述",
  "model": "sonnet",
  "tools": ["Bash", "Read", "Edit"],
  "disallowedTools": ["Write"],
  "permissionMode": "plan",
  "prompt": "你是一个专门处理..."
}
```

加载逻辑在 `src/tools/AgentTool/loadAgentsDir.ts`：
- `getAgentDefinitionsWithOverrides()` — 加载并合并 agent 定义
- 支持 `built-in` 和 `custom` 两种 source
- Agent 定义支持 `tools`（允许工具白名单）和 `disallowedTools`（黑名单）

## 工具隔离（`filterToolsForAgent`）

```typescript
function filterToolsForAgent({ tools, isBuiltIn, isAsync, permissionMode }): Tools {
  return tools.filter(tool => {
    if (tool.name.startsWith('mcp__')) return true      // MCP 工具始终允许
    if (ALL_AGENT_DISALLOWED_TOOLS.has(tool.name)) return false
    if (!isBuiltIn && CUSTOM_AGENT_DISALLOWED_TOOLS.has(tool.name)) return false
    if (isAsync && !ASYNC_AGENT_ALLOWED_TOOLS.has(tool.name)) return false
    return true
  })
}
```

- **同步代理**：可使用大多数工具
- **异步代理**：仅允许受限工具集（`ASYNC_AGENT_ALLOWED_TOOLS`）
- **自定义代理**：额外禁用 `AgentTool` 等（防止递归）

## 执行模式

### 同步模式
```
主会话 → AgentTool.call() → runAgent() → 子代理执行 → 返回结果
                                   ↑
                              阻塞主会话
```

### 后台模式
```
主会话 → AgentTool.call(run_in_background: true)
       → registerAsyncAgent()
       → 立即返回（不阻塞）
       → 后台执行 → 完成通知
```

### Worktree 隔离
```
主会话 → createAgentWorktree()
       → 子代理在独立 worktree 中工作
       → 完成后合并或保留
```

## Coordinator Mode

协调器模式（`src/coordinator/coordinatorMode.ts`）：

- 主会话作为协调器，不直接执行工具
- 使用 `AgentTool` 和 `TaskStopTool` 管理子代理
- 子代理执行实际的工具操作
- 权限通过 `bubble` 模式冒泡到协调器

## 多代理通信

### SendMessage 工具
- 代理间通过命名寻址通信
- `SendMessage({ to: 'agent-name', message: '...' })`
- 支持 mailbox 模式（异步消息队列）

### 共享任务列表
- `TaskCreateTool` / `TaskOutputTool` 提供共享任务状态
- 代理可创建、认领、完成任务

## 代理生命周期

```
创建 → 注册（registerAsyncAgent）
  → 执行（runAgent）
    → 权限检查
    → 工具调用
    → 进度报告
  → 完成 / 失败
→ 注销（unregisterAsyncAgent）
→ 通知主会话
```
