---
rule_id: backend-agents
version: 1.15.0
last_updated: 2026-08-28
dependencies: [agents-root]
---

# 后端规则

## 适用范围

- 作用域：Route Handlers、Server Actions、Service、Repository、Prisma、缓存、鉴权与服务端测试
- 触发场景：涉及服务端逻辑、数据库、缓存、权限、异常处理时阅读

### 常见任务入口
- 改接口响应或状态码：先看 `Route Handler 规则`、`异常处理规范`
- 改表单提交或状态切换：先看 `Server Action 规则`、`缓存与重验证规则`
- 改业务逻辑或权限：先看 `Service 层规则`、`鉴权与权限规则`
- 改查询、分页、Prisma：先看 `Repository / Prisma 规则`、`提交前最小回归`

---

## 技术栈

### 服务端
- Next.js Route Handlers（`app/api/**/route.ts`）
- Next.js Server Actions（表单提交、状态变更、同站点业务写操作优先）
- Prisma ORM
- PostgreSQL
- Zod（入参与出参校验）

<!-- include: shared/application/tech-choice.md -->

---

## 主动建议规则
- 发现业务逻辑放错层、Service 与 Repository 职责混杂、权限校验缺失、事务边界不清或异常处理不一致时，应主动提醒
- 发现数据库结构、接口契约、缓存策略或状态流转可能影响历史数据、兼容性或权限安全时，必须先说明风险，不得直接扩大修改
- 发现可以复用既有 Service、Repository、schema、校验器、错误处理封装或第三方集成封装时，应优先建议复用
- 不确定业务规则、权限规则、数据含义或外部接口行为时，应先查证或列为待确认项，禁止按通用经验补写规则

---

## 阶段 2 — 服务端与数据实现

**触发条件**：用户发出“开始后端开发”或等价指令

**入场要求**：页面结构已完成，schema 与契约清晰

**工作内容**：
1. 按 `Route Handler / Server Action -> Service -> Repository -> Prisma` 分层实现服务端能力。
2. 根据需求维护 Prisma schema、迁移脚本、种子数据与数据库约束。
3. 完成认证、鉴权、日志、缓存、分页、筛选、排序等服务端逻辑。
4. 每个核心能力都必须可通过页面、接口工具或测试独立验证。

**门控规则**：
- 核心服务与接口独立验证通过后，才允许进入阶段 3。

---

## 服务端规则

### 分层依赖规则
```text
Route Handler / Server Action
              ↓
           Service
              ↓
         Repository
              ↓
 Prisma / External Service
```

### Route Handler 规则
- 文件位置：`app/api/**/route.ts`
- 只负责处理 HTTP method、参数解析、鉴权、响应码和响应结构
- 不直接承载业务逻辑或复杂 Prisma 查询
- 统一返回明确、稳定、可解析的 JSON 结构
- 对外开放接口要显式处理 400 / 401 / 403 / 404 / 409 / 500 等状态

### Server Action 规则
- 文件位置建议：`src/server/actions/` 或贴近业务域组织
- 适用于表单提交、状态切换、审核操作、增删改等同站点交互
- 必须在入口处完成鉴权和 Zod 校验
- 调用服务层后，按需执行 `revalidatePath` / `revalidateTag`
- 不要在 Action 中混杂大量 UI 拼装逻辑

### Service 层规则
- Service 方法命名使用业务语义，如 `createOrder`、`approveRequest`
- 业务校验、权限判断、去重判断、状态流转、事务边界放在 Service 层
- 返回明确的类型结果，不返回未约束的 `any`
- 对外部系统调用进行必要的超时、失败与重试控制

### Repository / Prisma 规则
- Prisma Client 统一从 `src/server/db/` 或等价目录导出
- Prisma 查询不得出现在页面组件、hooks 或浏览器端代码中
- 复杂查询、分页、排序、条件拼装收敛到 Repository 层
- 数据库字段命名使用 snake_case，应用层使用 camelCase
- Schema 变更后必须同步 migration、seed、类型与文档

### 鉴权与权限规则
- 粗粒度页面保护可放在 `middleware.ts`
- 细粒度业务权限必须在 Service 层再次校验
- 会话、Cookie、Token、用户身份解析统一在服务端处理
- 客户端只能消费鉴权结果，不负责任何安全边界的最终判定

### 缓存与重验证规则
- 使用 Next.js 原生缓存能力时，必须显式说明缓存策略
- 写操作后必须根据影响范围执行重验证
- 不允许“提交成功但页面不刷新”的伪完成状态
- 对权限相关、实时性强的数据默认谨慎缓存

### 异常处理规范
- 自定义业务异常统一收敛到 `src/server/errors/` 或等价目录
- 区分校验错误、权限错误、资源不存在、状态冲突、系统异常
- Route Handler / Server Action 负责把业务异常转换为稳定响应
- 禁止把原始数据库异常直接透传给页面

### 第三方集成规范
- 邮件、短信、对象存储、Webhook、支付等第三方能力统一放在 `src/server/integrations/`
- 第三方密钥只允许从服务端环境变量读取
- 所有第三方调用都必须具备错误处理与日志

### 代码组织规范
- 一个文件只放一个主对象：Route Handler、Server Action、Service、Repository、Prisma model、Schema 各自独立文件，文件名与主对象一致。
- 数据契约（请求 / 响应 / DTO / Schema）按业务域分文件定义，不合并到一个大杂烩文件。
- 触发拆分的信号：单文件出现多个主对象、函数或字段持续堆叠、模块跨多个不相关业务。
- 允许例外：紧耦合的常量、类型分组、测试 fixture。
- 反模式：一个文件堆放所有 Prisma model 或所有请求 / 响应定义；把多个不相关 Service 塞进同一文件。

---

## 测试规则

<!-- include: shared/backend/pre-commit-regression.md -->

### 服务端测试
- Service 测试：验证业务规则、权限校验、状态流转
- Route Handler / Server Action 测试：验证输入、输出、状态码与异常分支
- Repository 测试：验证查询条件、分页、排序、事务或数据库映射

<!-- include: shared/external-dependencies.md -->

<!-- include: shared/no-test-fallback.md -->

---
