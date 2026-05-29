# 04 - Tool 系统

## Tool 接口定义（`src/Tool.ts`）

每个工具实现 `Tool<Input, Output, Progress>` 接口：

```typescript
type Tool<Input, Output, Progress> = {
  name: string                    // 唯一标识符
  aliases?: string[]              // 向后兼容的别名
  searchHint?: string             // ToolSearch 关键词提示

  // 核心方法
  inputSchema: Input              // Zod schema（输入校验）
  outputSchema?: z.ZodType        // 输出 schema（可选）

  description(input, options): Promise<string>   // 工具描述（注入系统提示词）
  prompt(): Promise<string>                      // 详细提示词

  call(args, context, canUseTool, parentMessage, onProgress?)
    : Promise<ToolResult<Output>>               // 执行工具

  checkPermissions(input, context)
    : Promise<PermissionDecision>               // 权限检查

  validateInput(input, context)
    : Promise<ValidationResult>                 // 输入验证

  // UI 渲染
  renderToolUseMessage(input, options): ReactNode
  renderToolResultMessage(output, options): ReactNode
  renderToolUseProgressMessage(progress, options): ReactNode
  renderToolUseRejectedMessage(input, options): ReactNode
  renderToolUseErrorMessage(error, options): ReactNode

  // 行为标志
  isEnabled(): boolean
  isReadOnly(): boolean
  isConcurrencySafe(input): boolean    // 是否可并行执行
  userFacingName(): string
  maxResultSizeChars: number
}
```

## 工具注册与组装（`src/tools.ts`）

### 工具池组装流程

```
getAllBaseTools()                    // 所有内置工具
  → filterToolsByDenyRules()        // 过滤被拒绝的工具
  → filterToolsForAgent()           // Agent 工具隔离
  → assembleToolPool()              // 合并内置 + MCP 工具
    → getTools(permissionContext)   // 获取允许的内置工具
    → filterToolsByDenyRules(mcpTools)  // 过滤 MCP 工具
    → 排序（内置工具前缀 + MCP 工具后缀）
    → uniqBy('name')                // 去重（内置优先）
```

### 内置工具清单

| 工具 | 文件 | 职责 |
|------|------|------|
| `BashTool` | `tools/BashTool/` | 执行 shell 命令 |
| `FileReadTool` | `tools/FileReadTool/` | 读取文件 |
| `FileEditTool` | `tools/FileEditTool/` | 编辑文件（diff） |
| `FileWriteTool` | `tools/FileWriteTool/` | 写入文件 |
| `GlobTool` | `tools/GlobTool/` | 文件模式匹配 |
| `GrepTool` | `tools/GrepTool/` | 内容搜索 |
| `AgentTool` | `tools/AgentTool/` | 子代理 |
| `SkillTool` | `tools/SkillTool/` | 技能调用 |
| `WebFetchTool` | `tools/WebFetchTool/` | 网页抓取 |
| `WebSearchTool` | `tools/WebSearchTool/` | 网页搜索 |
| `TodoWriteTool` | `tools/TodoWriteTool/` | 任务列表 |
| `TaskCreateTool` | `tools/TaskCreateTool/` | 创建任务 |
| `TaskOutputTool` | `tools/TaskOutputTool/` | 任务输出 |
| `TaskStopTool` | `tools/TaskStopTool/` | 停止任务 |
| `NotebookEditTool` | `tools/NotebookEditTool/` | Jupyter 编辑 |
| `AskUserQuestionTool` | `tools/AskUserQuestionTool/` | 向用户提问 |
| `EnterPlanModeTool` | `tools/EnterPlanModeTool/` | 进入计划模式 |
| `ExitPlanModeTool` | `tools/ExitPlanModeTool/` | 退出计划模式 |
| `EnterWorktreeTool` | `tools/EnterWorktreeTool/` | 进入 worktree |
| `ExitWorktreeTool` | `tools/ExitWorktreeTool/` | 退出 worktree |
| `ToolSearchTool` | `tools/ToolSearchTool/` | 工具搜索（延迟加载） |
| `LSPTool` | `tools/LSPTool/` | LSP 语言服务 |
| `ListMcpResourcesTool` | `tools/ListMcpResourcesTool/` | MCP 资源列表 |
| `ReadMcpResourceTool` | `tools/ReadMcpResourceTool/` | MCP 资源读取 |

## 并发控制：StreamingToolExecutor

`src/services/tools/StreamingToolExecutor.ts` 实现流式工具执行的并发控制：

```typescript
class StreamingToolExecutor {
  // 状态机：queued → executing → completed → yielded
  private tools: TrackedTool[] = []

  addTool(block, assistantMessage)      // 添加工具到队列
  getRemainingResults(): AsyncGenerator  // 获取结果（按顺序）

  private canExecuteTool(isConcurrencySafe): boolean
  private processQueue(): Promise<void>
  private executeTool(tool): Promise<void>
}
```

### 并发规则

1. **并发安全工具**（`isConcurrencySafe(input) === true`）可并行执行
2. **非并发工具**必须独占执行（exclusive access）
3. 结果按工具接收顺序缓冲输出（保序）
4. Bash 工具错误会触发 `siblingAbortController` 终止兄弟进程

### 工具执行流程

```
API 流式响应 → content_block_start (tool_use)
  → StreamingToolExecutor.addTool()
    → findToolByName() → 解析 inputSchema
    → isConcurrencySafe() 检查
    → processQueue()
      → canExecuteTool() → 并发条件检查
      → executeTool()
        → checkPermissions() → 权限校验
        → tool.call() → 实际执行
        → 结果存入 TrackedTool.results
  → getRemainingResults() → 按序 yield 结果
```

## 权限检查集成

每个工具在执行前必须通过 `checkPermissions()`：

```typescript
// 返回三种结果之一
type PermissionDecision =
  | { behavior: 'allow', updatedInput?, userModified? }
  | { behavior: 'ask', message, suggestions? }
  | { behavior: 'deny', message, decisionReason }
```

权限检查在 `StreamingToolExecutor.executeTool()` 中触发，`ask` 会暂停执行等待用户确认。

## ToolSearch 延迟加载

`ToolSearchTool` 实现工具的延迟发现：
- 当工具数量过多时，部分工具不直接暴露给模型
- 模型可通过 `ToolSearchTool` 按关键词搜索并动态发现工具
- `searchHint` 字段帮助关键词匹配
