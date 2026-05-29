# 12 - API 通信层

## 概述

Claude Code 通过 `@anthropic-ai/sdk` 与 Claude API 通信，支持流式和非流式两种模式，内置重试、降级和限流处理。

## 核心模块

### 1. API 客户端（`src/services/api/claude.ts`）

```typescript
// 创建 Anthropic 客户端
// maxRetries: 0 — 禁用 SDK 自动重试，使用自定义重试逻辑
const client = new Anthropic({ maxRetries: 0 })

// 流式查询
async function queryModelWithStreaming(params): AsyncGenerator<Event>

// 非流式查询
async function queryModelWithoutStreaming(params): Promise<Response>
```

### 2. 重试引擎（`src/services/api/withRetry.ts`）

```typescript
interface RetryContext {
  maxTokensOverride?: number
  model: string
  thinkingConfig: ThinkingConfig
  fastMode?: boolean
}

interface RetryOptions {
  maxRetries?: number
  model: string
  fallbackModel?: string         // 降级模型
  thinkingConfig: ThinkingConfig
  fastMode?: boolean
  signal?: AbortSignal
  querySource?: QuerySource
  initialConsecutive529Errors?: number  // 预设 529 计数
}

async function* withRetry<T>(
  fn: (client: Anthropic) => Promise<T>,
  options: RetryOptions
): AsyncGenerator<SystemAPIErrorMessage, T>
```

#### 重试策略

```
请求失败
  ├── 429 (Rate Limit) → 指数退避重试
  │   └── 连续 3 次 529 → 触发模型降级
  ├── 5xx (Server Error) → 指数退避重试
  ├── ECONNRESET / EPIPE → 立即重试
  ├── 网络超时 → 指数退避重试
  └── 其他错误 → CannotRetryError
```

#### 降级机制

```typescript
class FallbackTriggeredError extends Error {
  constructor(
    public readonly fallbackModel: string,
    public readonly retryContext: RetryContext,
  )
}
```

- 连续 529 错误超过阈值时触发
- 切换到 `fallbackModel`（通常是更轻量的模型）
- Fast Mode 降级有 30 分钟冷却期

### 3. 流式处理

#### Stream 工具类（`src/utils/stream.ts`）

```typescript
class Stream<T> implements AsyncIterator<T> {
  private queue: T[]
  private readResolve?: (value: IteratorResult<T>) => void
  private readReject?: (error: unknown) => void
  private isDone: boolean

  enqueue(value: T): void    // 生产者写入
  done(): void               // 正常结束
  error(error: unknown): void // 异常结束
  next(): Promise<IteratorResult<T>>  // 消费者读取
}
```

**设计特点**：
- 单次迭代（`started` 标志防止重复消费）
- Promise-based 背压（消费者未就绪时挂起生产者）
- 错误传播（`error()` 设置后，后续 `next()` reject）

#### 流式事件处理

```typescript
// 流式 API 响应 → Stream<Event>
async function* queryModelWithStreaming(params) {
  const stream = await client.messages.create({ ...params, stream: true })
  for await (const event of stream) {
    // 处理各种事件类型
    yield processEvent(event)
  }
}
```

#### 流清理

```typescript
function cleanupStream(stream: Stream<Event> | undefined): void {
  if (!stream) return
  try {
    if (!stream.controller.signal.aborted) {
      stream.controller.abort()
    }
  } catch { /* 忽略已关闭的流 */ }
}
```

## Prompt 缓存

### 机制

```typescript
// 使用 SYSTEM_PROMPT_DYNAMIC_BOUNDARY 标记缓存边界
const messages = [
  {
    role: 'system',
    content: [
      { type: 'text', text: staticPrompt, cache_control: { type: 'ephemeral' } },
      { type: 'text', text: dynamicPrompt }
    ]
  },
  ...conversationHistory
]
```

### 缓存策略

- **静态部分**：系统提示词、CLAUDE.md 内容 — 标记为 `ephemeral` 缓存
- **动态部分**：对话历史、工具结果 — 不缓存
- **TTL**：5 分钟（Anthropic 服务端）
- **命中条件**：相同前缀 + 相同 cache_control 标记

## Token 计数

```typescript
// 获取模型的最大输出 token 数
function getMaxOutputTokensForModel(model: string): number {
  const maxOutputTokens = getModelMaxOutputTokens(model)

  // Slot-reservation cap：默认降到 8k
  // BQ p99 输出 = 4,911 tokens；32k/64k 默认值过度预留
  const defaultTokens = isMaxTokensCapEnabled()
    ? Math.min(maxOutputTokens.default, CAPPED_DEFAULT_MAX_TOKENS)
    : maxOutputTokens.default

  // 环境变量覆盖
  return validateBoundedIntEnvVar(
    'CLAUDE_CODE_MAX_OUTPUT_TOKENS',
    process.env.CLAUDE_CODE_MAX_OUTPUT_TOKENS,
    defaultTokens,
    maxOutputTokens.upperLimit,
  ).effective
}
```

### Task Budget

```typescript
interface TaskBudgetParam {
  type: 'tokens'
  total: number
  remaining?: number
}

function configureTaskBudgetParams(taskBudget, outputConfig, betas): void
```

- 子代理可以有独立的 token 预算
- `remaining` 字段跟踪剩余预算
- 需要 `TASK_BUDGETS_BETA_HEADER` beta 标志

## 限流系统（Rate Limiting）

### ClaudeAILimits（`src/services/claudeAiLimits.ts`）

```typescript
type QuotaStatus = 'allowed' | 'allowed_warning' | 'rejected'

type RateLimitType =
  | 'five_hour'       // 5 小时会话限制
  | 'seven_day'       // 7 天限制
  | 'seven_day_opus'  // Opus 专用 7 天限制
  | 'seven_day_sonnet' // Sonnet 专用 7 天限制
  | 'overage'         // 超额使用限制

type ClaudeAILimits = {
  status: QuotaStatus
  unifiedRateLimitFallbackAvailable: boolean  // Opus 降级提示
  resetsAt?: number
  rateLimitType?: RateLimitType
  utilization?: number          // 使用率 (0-1)
  overageStatus?: QuotaStatus
  overageResetsAt?: number
  overageDisabledReason?: OverageDisabledReason
  isUsingOverage?: boolean
  surpassedThreshold?: number
}
```

### 早期预警系统

```typescript
type EarlyWarningConfig = {
  rateLimitType: RateLimitType
  claimAbbrev: '5h' | '7d'
  windowSeconds: number
  thresholds: EarlyWarningThreshold[]
}

// 预警阈值配置
const EARLY_WARNING_CONFIGS: EarlyWarningConfig[] = [
  { rateLimitType: 'five_hour', thresholds: [...] },
  { rateLimitType: 'seven_day', thresholds: [...] },
]
```

- 基于使用率 + 时间百分比的双重阈值
- 服务端 `surpassed-threshold` header 优先
- 客户端计算作为降级方案

### Utilization 数据源（`src/services/api/usage.ts`）

```typescript
type Utilization = {
  five_hour?: RateLimit | null
  seven_day?: RateLimit | null
  seven_day_oauth_apps?: RateLimit | null
  seven_day_opus?: RateLimit | null
  seven_day_sonnet?: RateLimit | null
  extra_usage?: ExtraUsage | null
}

async function fetchUtilization(): Promise<Utilization | null> {
  // OAuth 认证检查
  // GET /api/oauth/usage
  // 5 秒超时
}
```

### 限流消息（`src/services/rateLimitMessages.ts`）

```typescript
function getRateLimitMessage(limits: ClaudeAILimits, model: string): RateLimitMessage | null {
  // 1. 超额使用场景（isUsingOverage）
  // 2. 错误状态（status === 'rejected'）
  // 3. 警告状态（status === 'allowed_warning'，使用率 > 70%）
  // 4. 无需消息
}
```

### Overage（超额使用）

```typescript
type OverageDisabledReason =
  | 'overage_not_provisioned'
  | 'org_level_disabled'
  | 'out_of_credits'
  | 'seat_tier_level_disabled'
  | 'member_level_disabled'
  | 'org_service_level_disabled'
  // ... 共 12 种原因
```

- 组织级别和成员级别的独立控制
- 信用额度检查
- 服务级别的细粒度控制

## API 元数据

```typescript
function getAPIMetadata() {
  return {
    user_id: JSON.stringify({
      device_id: getOrCreateUserID(),
      account_uuid: getOauthAccountInfo()?.accountUuid ?? '',
      session_id: getSessionId(),
      ...extraMetadata,  // CLAUDE_CODE_EXTRA_METADATA 环境变量
    }),
  }
}
```

## 认证方式

### 1. API Key

```typescript
async function verifyApiKey(apiKey: string): Promise<boolean>
```

### 2. OAuth

```typescript
// OAuth token 管理
getClaudeAIOAuthTokens()
isOAuthTokenExpired(expiresAt)
getAuthHeaders()
```

### 3. 第一方认证

```typescript
shouldIncludeFirstPartyOnlyBetas()  // 检查是否为第一方用户
```

## Beta 功能

```typescript
// src/utils/betas.ts
function shouldIncludeFirstPartyOnlyBetas(): boolean
function modelSupportsStructuredOutputs(model: string): boolean

// Bedrock 额外参数
function getBedrockExtraBodyParamsBetas(): string[]
```

## 环境变量

| 变量 | 用途 |
|------|------|
| `ANTHROPIC_MODEL` | 指定模型 |
| `CLAUDE_CODE_MAX_RETRIES` | 最大重试次数 |
| `CLAUDE_CODE_MAX_OUTPUT_TOKENS` | 最大输出 token 数 |
| `CLAUDE_CODE_EXTRA_METADATA` | 额外 API 元数据（JSON） |

## 错误处理流程

```
API 调用
  ├── 成功 → 返回结果
  ├── 429 Rate Limit
  │   ├── 更新 ClaudeAILimits
  │   ├── emitStatusChange() → UI 更新
  │   ├── 指数退避重试
  │   └── 连续 529 → FallbackTriggeredError → 模型降级
  ├── 5xx Server Error
  │   ├── 指数退避重试
  │   └── 超过最大重试 → CannotRetryError
  ├── 网络错误
  │   ├── ECONNRESET / EPIPE → 立即重试
  │   └── 超时 → 指数退避重试
  └── 其他错误 → CannotRetryError → 上层处理
```

## 设计要点

1. **手动重试**：`maxRetries: 0` 禁用 SDK 自动重试，获得完全控制
2. **模型降级**：连续 529 时自动切换到更轻量模型
3. **Slot 预算**：默认 8k 输出上限，避免过度预留
4. **缓存优化**：静态 prompt 部分缓存，减少重复传输
5. **限流感知**：实时跟踪使用率，提前预警
6. **超额控制**：组织/成员/服务三级独立控制
