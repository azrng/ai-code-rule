## [2026-07-31]：Electron/.NET IPC 路由、契约生成与流式超时规范

跟踪状态表：

| 文件夹名称 | 当前子任务状态 | 备注 |
|-----------|----------------|------|
| `electronDotNetExe` | `已完成` | 补充经 SmartVault 验证的单通道 IPC、路由表、契约生成和流式超时模式 |

### 修改原因

原模板只规定使用 stdin/stdout JSON-RPC，以及耗时操作要处理超时和取消，未定义 method 路由、跨语言契约的单一事实来源和流式请求的总时长 / 空闲时长语义。实际项目因此曾出现主进程总时长先于 sidecar 业务超时触发的问题。

### 主要修改内容

- 规定渲染进程经单一通用 IPC 通道转发 `method + params`，.NET 使用精确匹配的路由注册表分发。
- 规定以 .NET 路由条目和 DTO 为事实来源，构建期生成 TypeScript method 常量、类型和基础 IPC SDK，禁止两端手抄契约。
- 规定流式 JSON-RPC notification 按 requestId 续期空闲计时；sidecar 管理总时长，用户审批期间暂停空闲计时并在裁决后恢复。

### 修改文件

- `electronDotNetExe/backend-AGENTS.md`
- `electronDotNetExe/frontend-AGENTS.md`
- `electronDotNetExe/doc-AGENTS.md`
- `doc/AI-Rules-Audit-Log.md`

### 当前子任务状态

- `已完成`
