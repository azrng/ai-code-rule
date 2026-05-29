# 09 - Hooks 与插件系统

## Hooks 系统概述

Hooks 是事件驱动的生命周期钩子，在特定时机执行用户/插件定义的逻辑。

## Hook 事件类型

定义在 `src/utils/hooks/hookEvents.ts`：

| 事件 | 触发时机 | 用途 |
|------|---------|------|
| `PreToolUse` | 工具执行前 | 拦截、校验、修改输入 |
| `PostToolUse` | 工具执行后 | 处理结果、触发副作用 |
| `PostToolUseFailure` | 工具执行失败后 | 错误处理 |
| `SessionStart` | 会话开始 | 初始化 |
| `SessionEnd` | 会话结束 | 清理 |
| `Stop` | 停止时 | 收尾 |
| `SubagentStart` | 子代理启动 | 跟踪 |
| `SubagentStop` | 子代理停止 | 跟踪 |
| `PreCompact` | 压缩前 | 备份 |
| `PostCompact` | 压缩后 | 恢复 |
| `PermissionDenied` | 权限拒绝后 | 日志 |
| `Notification` | 通知时 | 自定义通知 |
| `UserPromptSubmit` | 用户提交输入 | 预处理 |

## Hook 配置结构

```typescript
type IndividualHookConfig = {
  event: HookEvent
  config: HookCommand
  matcher?: string              // 匹配器（如工具名）
  source: HookSource
}

type HookCommand =
  | { type: 'command', command: string }    // Shell 命令
  | { type: 'prompt', prompt: string }      // 提示词
  | { type: 'agent', agent: string }        // Agent 名称
  | { type: 'http', url: string }           // HTTP 请求
  | { type: 'function', fn: Function }      // TypeScript 函数

type HookSource =
  | 'userSettings'
  | 'projectSettings'
  | 'localSettings'
  | 'policySettings'
  | 'pluginHook'
  | 'sessionHook'
  | 'builtinHook'
```

## Hook 存储

在 `settings.json` 中配置：

```json
{
  "hooks": {
    "PreToolUse": [
      {
        "matcher": "Bash",
        "hooks": [
          {
            "type": "command",
            "command": "echo 'Bash called: $TOOL_INPUT' >> /tmp/hooks.log"
          }
        ]
      }
    ]
  }
}
```

## Hook 执行机制

### Shell 命令 Hook

- 通过 stdin 传入 JSON 格式的上下文
- 退出码控制行为：
  - `0` — 成功，继续执行
  - `2` — 阻止，stderr 内容显示给模型
  - 其他 — stderr 显示给用户，不阻止

### 函数 Hook（Session Hooks）

```typescript
// src/utils/hooks/sessionHooks.ts
function addSessionHook(config: IndividualHookConfig): string  // 返回 hook ID
function addFunctionHook(event, fn): string
function removeSessionHook(id: string): void
```

存储在 `AppState.sessionHooks`（`Map<sessionId, SessionStore>`）中。

### 异步 Hook

```typescript
// src/utils/hooks/AsyncHookRegistry.ts
// 跟踪异步 hook 的完成状态
Map<string, PendingAsyncHook>
```

## Hook 事件广播

```typescript
// src/utils/hooks/hookEvents.ts
function emitHookEvent(event: HookEvent, phase: 'started' | 'progress' | 'response', data): void

// 默认只广播 SessionStart 和 Setup
setAllHookEventsEnabled(true)  // 启用所有事件广播
```

## PostToolUse Hooks 结果

```typescript
// src/services/tools/toolHooks.ts
type PostToolUseHooksResult<Output> =
  | MessageUpdateLazy<AttachmentMessage | ProgressMessage<HookProgress>>
  | { updatedMCPToolOutput: Output }
```

Hook 可以：
- 附加消息（attachment）
- 报告进度
- 修改 MCP 工具输出

## 插件系统

### 内置插件

```typescript
// src/plugins/builtinPlugins.ts
type BuiltinPluginDefinition = {
  name: string
  description: string
  version: string
  skills?: BundledSkillDefinition[]
  hooks?: HooksSettings
  mcpServers?: Record<string, McpServerConfig>
}

function registerBuiltinPlugin(definition: BuiltinPluginDefinition): void
```

- 插件 ID 格式：`{name}@builtin`
- 通过 `enabledPlugins` 设置启用/禁用

### 技能系统（Skills）

技能是可被模型调用的命令，加载自多个来源：

```typescript
// src/skills/loadSkillsDir.ts
type LoadedFrom =
  | 'commands_DEPRECATED'   // 旧版 /commands/ 目录
  | 'skills'                // /skills/ 目录
  | 'plugin'                // 插件提供的技能
  | 'managed'               // 企业管理的技能
  | 'bundled'               // 内置技能
  | 'mcp'                   // MCP 提供的技能
```

#### 技能加载优先级

1. **内置技能**（`src/skills/bundled/`）— 编译到 CLI 中
2. **目录技能**（`~/.claude/skills/`、`.claude/skills/`）— 文件系统中的 `SKILL.md`
3. **插件技能** — 插件注册的技能
4. **MCP 技能** — MCP 服务器提供的 prompts
5. **动态技能** — 运行时发现的技能

#### 技能 Frontmatter

```markdown
---
name: my-skill
description: 技能描述
whenToUse: 当需要...时使用
model: sonnet
allowedTools:
  - Bash
  - Read
hooks:
  PostToolUse:
    - matcher: Bash
      hooks:
        - type: command
          command: echo "done"
paths:
  - "*.ts"
  - "src/**/*.tsx"
---

技能的提示词内容...
```

#### BundledSkillDefinition

```typescript
type BundledSkillDefinition = {
  name: string
  description: string
  aliases?: string[]
  whenToUse?: string
  argumentHint?: string
  allowedTools?: string[]
  model?: string
  disableModelInvocation?: boolean
  userInvocable?: boolean
  isEnabled?: () => boolean
  hooks?: HooksSettings
  context?: 'inline' | 'fork'
  agent?: string
  files?: Record<string, string>
  getPromptForCommand: (args, context) => Promise<ContentBlockParam[]>
}
```
