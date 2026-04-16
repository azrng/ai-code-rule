---
rule_id: backend-agents
version: 1.2.0
last_updated: 2026-04-16
dependencies: [agents-root]
---

# AGENTS.md

## 适用范围

- 作用域：Route Handlers、Server Actions、Service、Repository、Prisma、缓存、鉴权与服务端测试
- 触发场景：涉及服务端逻辑、数据库、缓存、权限、异常处理时阅读

### 阅读摘要
- 建议阅读：改接口、改 Server Action、改 Service、改 Repository、改 Prisma、改鉴权与服务端测试
- 可先跳过：纯页面样式、纯文档整理、仅 Docker 或端口调整
- 优先查看：`阶段 2`、`服务端规则`、`测试规则`

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

### 技术选择原则
如果仓库已经有真实实现，以现有代码为准，不要强行重构或替换技术栈。

**技术债务评估框架**：
- **沿用现有实现**：代码可正常运行、无明显性能问题、维护成本可接受、团队熟悉度高
- **必须重构**：存在安全漏洞、严重影响性能、阻碍新功能开发、维护成本过高
- **可选重构**：代码风格不一致、存在更好的替代方案、但不紧急
- **禁止重构**：仅为个人偏好、追赶技术潮流、非关键路径的过度优化

**重构决策流程**：
1. 先评估现有代码的技术债务等级（高/中/低）
2. 判断重构的紧急程度和业务价值
3. 评估重构风险和工作量
4. 高风险或大规模重构需先与团队确认

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

---

## 测试规则

### 提交前最小回归
- 默认执行：`lint`、`typecheck`
- Service 改动：至少补一项业务规则、权限判断或状态流转验证
- Route Handler / Server Action 改动：至少补一项输入输出、状态码或异常分支验证
- Repository / Prisma 改动：至少补一项查询结果、分页排序、事务或 migration 同步验证
- 若仓库暂时缺少自动化测试基础，可使用接口调用、页面联调或脚本验证替代，但必须能证明核心逻辑真实生效

### 服务端测试
- Service 测试：验证业务规则、权限校验、状态流转
- Route Handler / Server Action 测试：验证输入、输出、状态码与异常分支
- Repository 测试：验证查询条件、分页、排序、事务或数据库映射

### 外部依赖与数据
- 测试中不应真实调用第三方服务，统一使用 mock
- 测试数据应最小化、可读、可重复执行
- 不要让测试依赖本地人工状态

### 无法执行测试时
- 必须说明未执行的测试类型
- 必须说明未执行原因
- 必须说明潜在影响范围与风险

---

文件结束。
