# 07 - Bridge 远程控制

## 架构概述

Bridge 模式允许本地 Claude Code 实例作为"环境"连接到云端（claude.ai），实现远程控制。

```
┌─────────────────┐         ┌──────────────────┐
│  claude.ai      │◄───────►│  Local Bridge    │
│  (CCR Backend)  │  SSE/WS │  (本机 Claude)   │
└─────────────────┘         └──────────────────┘
       │                            │
       ▼                            ▼
  用户浏览器 UI              本地文件系统/终端
```

## 核心组件

### 1. ReplBridgeHandle（`src/bridge/replBridge.ts`）

Bridge 连接句柄，提供消息发送和控制接口：

```typescript
type ReplBridgeHandle = {
  bridgeSessionId: string
  environmentId: string
  sessionIngressUrl: string
  writeMessages(messages: Message[]): void
  writeSdkMessages(messages: SDKMessage[]): void
  sendControlRequest(request: SDKControlRequest): void
  sendControlResponse(response: SDKControlResponse): void
  sendControlCancelRequest(requestId: string): void
  sendResult(): void
  teardown(): Promise<void>
}

type BridgeState = 'ready' | 'connected' | 'reconnecting' | 'failed'
```

### 2. ReplBridgeTransport（`src/bridge/replBridgeTransport.ts`）

传输层抽象，支持 v1 和 v2 两种协议：

```typescript
type ReplBridgeTransport = {
  write(message: StdoutMessage): Promise<void>
  writeBatch(messages: StdoutMessage[]): Promise<void>
  close(): void
  isConnectedStatus(): boolean
  getStateLabel(): string
  setOnData(callback: (data: string) => void): void
  setOnClose(callback: (closeCode?: number) => void): void
  setOnConnect(callback: () => void): void
  connect(): void
  getLastSequenceNum(): number
  reportState(state: SessionState): void
  reportMetadata(metadata: Record<string, unknown>): void
  reportDelivery(eventId: string, status: 'processing' | 'processed'): void
  flush(): Promise<void>
}
```

## 传输层实现

### v1：HybridTransport

```
读取：WebSocket ← CCR Backend
写入：HTTP POST → Session-Ingress
```

- `createV1ReplTransport(hybrid)` — 包装 `HybridTransport`

### v2：SSETransport + CCRClient

```
读取：SSE (Server-Sent Events) ← CCR Backend
写入：CCRClient → /v1/code/sessions/* endpoints
```

- `SSETransport`（`src/cli/transports/SSETransport.ts`）— 支持自动重连、Last-Event-ID 恢复
- `WebSocketTransport`（`src/cli/transports/WebSocketTransport.ts`）— WebSocket 传输

## 初始化流程

```
initReplBridge(options)
  ├── 1. isBridgeEnabledBlocking() — GrowthBook gate 检查
  ├── 2. getBridgeAccessToken() — OAuth token 检查
  ├── 3. isPolicyAllowed('allow_remote_control') — 组织策略检查
  ├── 4. 版本兼容性检查
  ├── 5. 选择 v1 或 v2 路径
  │   ├── v1: bridgeMain() — 完整 bridge 循环
  │   └── v2: initEnvLessBridgeCore() — 轻量级初始化
  └── 返回 ReplBridgeHandle
```

## v2 远程 Bridge 核心（`src/bridge/remoteBridgeCore.ts`）

```typescript
async function initEnvLessBridgeCore(params: EnvLessBridgeParams) {
  // 1. 创建 session (POST /v1/code/sessions)
  const sessionId = await createCodeSession(baseUrl, accessToken, title)

  // 2. 获取 bridge 凭证 (POST /bridge → worker_jwt)
  const credentials = await fetchRemoteCredentials(sessionId, baseUrl, accessToken)

  // 3. 构建 v2 传输层 (SSETransport + CCRClient)
  const transport = createV2ReplTransport(sessionUrl, credentials)

  // 4. 返回句柄
  return { bridgeSessionId, writeMessages, teardown, ... }
}
```

## Session 管理（`src/bridge/sessionRunner.ts`）

Bridge 模式下，每个会话通过子进程运行：

```typescript
function createSessionSpawner(deps: SessionSpawnerDeps): SessionSpawner {
  return {
    spawn(opts, dir): SessionHandle {
      // 生成子进程参数
      const args = ['--print', '--sdk-url', opts.sdkUrl, '--session-id', opts.sessionId, ...]
      const child = spawn(deps.execPath, args, { stdio: ['pipe', 'pipe', 'pipe'] })
      // 管理子进程生命周期
      return { pid, write, kill, ... }
    }
  }
}
```

## 认证流程

```
用户登录 claude.ai (OAuth)
  → getClaudeAIOAuthTokens()
  → getBridgeAccessToken()
  → 创建 bridge session
  → 获取 worker JWT
  → 使用 JWT 进行后续通信
```

## 消息处理

```typescript
// src/bridge/bridgeMessaging.ts
function handleIngressMessage(message, handlers): void
function handleServerControlRequest(request, transport, handlers): void

type ServerControlRequestHandlers = {
  onPermissionResponse: (response) => void
  onInterrupt: () => void
  onSetModel: (model) => void
  onSetPermissionMode: (mode) => void
  // ...
}
```

## Bridge 状态管理

```
BridgeState:
  'ready'      → 初始化完成
  'connected'  → 已连接到 CCR
  'reconnecting' → 断线重连中
  'failed'     → 连接失败
```
