---
rule_id: backend-agents
version: 1.51.0
last_updated: 2026-09-04
dependencies: [agents-root]
---

# 后端规则

## 适用范围

- 作用域：Electron 主进程、IPC handler、主进程服务、数据访问、本地系统能力与后端测试
- 触发场景：涉及 `main/`、preload、IPC、SQLite、文件系统、系统权限、异常处理或测试时阅读

---

## 技术栈

### Runtime
- Electron 主进程（Node.js + TypeScript），本地业务逻辑统一收敛在主进程
- 渲染进程保持纯前端，不开启 Node 能力（见安全基线规则）
- 异步统一使用 `async / await` 与 `Promise`，不在 handler 中裸用回调风格 API
- 进程间与前后端数据交换使用可序列化对象（结构化克隆兼容；JSON 序列化边界显式处理 `Date`、`Map`、`Buffer` 等类型）

### 数据与本地能力
- 本地持久化默认推荐：SQLite（`better-sqlite3`）
- 轻量键值配置默认推荐：`electron-store`（或等价的用户目录 JSON 封装），不与业务数据混库
- 使用原生模块（如 `better-sqlite3`）时，必须保证与 Electron ABI 匹配（`electron-rebuild` 或等价机制），并在依赖变更后重新验证
- 配置、日志、缓存、用户目录读写统一在主进程封装处理

### 通信原则
- Electron 渲染进程通过 preload `contextBridge` 暴露的 IPC SDK 调用主进程；主进程通过 `ipcMain.handle` 提供请求-响应能力
- 事件通知或后台状态推送使用主进程 `webContents.send` 向渲染进程单向推送，渲染进程只订阅不回写
- 默认**不**引入本地 HTTP 服务作为桌面应用内部桥接层
- 仅在以下场景才考虑额外本地服务层：
  - 明确需要流式下载或上传代理
  - 需要本地 WebSocket / SSE
  - 需要对接必须通过 HTTP 语义运行的第三方模块
  - 已证明 IPC 无法满足能力要求

### IPC 通道与契约
- IPC channel 名称使用 `域:动作` 命名（如 `project:open`、`task:list`），语义明确，不使用模糊缩写
- channel 注册统一收敛在 `main/ipc/`（或等价目录），按业务域拆分注册模块，不在主进程入口平铺堆积
- 请求参数与返回值在主进程侧先定义为明确 TypeScript 类型；渲染进程调用侧通过共享类型或 schema 保持一致
- 主进程向渲染进程推送的事件同样要声明事件名与载荷类型，收口在统一的事件封装中
- 新增或修改 channel 时，依次更新类型定义、handler、业务实现与测试，再同步渲染层封装；不在两端手抄 channel 字符串

## 主动建议规则
- 发现 IPC handler 与业务服务职责混杂、SQLite 访问封装不一致或错误处理缺失时，应主动提醒
- 发现接口契约、类型定义、SQLite 表结构或状态流转可能影响历史数据、兼容性或权限安全时，必须先说明风险，不得直接扩大修改
- 发现可以复用既有服务、数据访问封装、类型定义、校验器或错误处理封装时，应优先建议复用
- 不确定业务规则、权限规则、数据含义或外部接口行为时，应按根 `AGENTS.md` 的查证优先级处理，禁止按通用经验补写规则

---

## 推荐目录结构

若仓库尚未形成稳定结构，可优先参考以下组织方式；若仓库已有实现，以现状为准，不强制迁移。

```text
main/
├── index.ts                 # 主进程入口（窗口创建、app 生命周期）
├── ipc/                     # IPC handler 注册（按业务域拆分）
├── services/                # 业务服务
├── repositories/            # 数据访问层
├── db/                      # 数据库连接、迁移、查询辅助
├── integrations/            # 外部系统与系统能力封装
├── errors/                  # 统一错误类型
├── windows/                 # 窗口创建与管理（窗口多时可再拆）
├── config/                  # 配置加载
└── types/                   # IPC 入参出参与领域类型
preload/
└── index.ts                 # contextBridge 暴露 window.ipc
```

---

## 阶段 2 — Electron 主进程实现

**触发条件**：用户发出“开始后端开发”或等价指令

**入场要求**：前端页面结构已完成，schema 与契约清晰

**工作内容**：
1. 按 `IPC Handler -> Service -> Repository -> SQLite / Local Capability` 分层实现本地能力。
2. 实现并验证 preload 暴露面与 `ipcMain.handle` 通道。
3. 根据需求维护本地数据库 schema、迁移脚本、配置读写与文件目录约束。
4. 完成权限、日志、缓存、状态流转、分页、筛选、排序等主进程侧逻辑。
5. 每个核心能力都必须可通过前端联调、测试或脚本独立验证。

**门控规则**：
- 核心服务、IPC 通道与 Handler 独立验证通过后，才允许进入阶段 3。

---

## Electron 主进程规则

### 分层依赖规则

```text
IPC Handler / Event（ipcMain.handle / webContents.send）
          ↓
       Service
          ↓
     Repository
          ↓
 SQLite / FileSystem / External Capability
```

### IPC Handler 规则
- Handler 只负责参数解析、权限校验、调用服务与返回稳定结果
- channel 名称使用明确业务语义，不使用模糊缩写
- 前端可见的输入输出结构必须稳定、可序列化、可测试
- 不在 Handler 中直接写复杂数据库查询或系统调用
- 统一在边界层把业务错误转换为稳定、可识别的错误结果（错误码 + 可读信息），不把原始异常直接抛给渲染进程

### Service 规则
- 业务校验、权限判断、去重判断、状态流转、事务边界放在 Service 层
- 返回明确的类型结果，不返回未约束的 `any` / `unknown`
- 涉及耗时操作时显式处理超时、取消（`AbortSignal`）、失败重试或幂等
- 若需要跨多个仓储或能力协作，由 Service 负责组织，不把编排逻辑散到 Handler

### Repository / 数据访问规则
- 数据访问不得出现在 IPC handler 或渲染进程代码中
- 复杂查询、分页、排序、条件拼装收敛到 Repository
- 默认使用迁移脚本管理 schema 变更，不依赖启动时隐式改表
- SQLite 表与字段命名统一使用 snake_case
- schema 变更后必须同步迁移脚本、类型和文档

### 安全基线规则
- 所有 BrowserWindow 统一启用 `contextIsolation: true`、`nodeIntegration: false`，按需启用 `sandbox`
- preload 只通过 `contextBridge` 暴露显式白名单方法，不暴露 `ipcRenderer` 原始对象、`Node` 能力或任意 channel 转发
- 渲染进程禁止直接使用 Node 能力或 `child_process`；涉及原生能力的调用一律由主进程处理
- 加载远程内容（`http(s)://`）时必须说明理由并评估风险；本地打包应用默认只加载本地资源
- 禁止为了图方便关闭 `webSecurity` 或放宽同源策略；确有必要时先在阶段 0 说明风险与替代方案

### 主进程生命周期规则
- 窗口创建、关闭、全部关闭退出（macOS 保持激活惯例）等 `app` 生命周期事件统一收敛在入口或 `windows/` 模块
- 单实例锁（`app.requestSingleInstanceLock`）按产品需求决定；启用时第二次启动必须聚焦已有窗口
- 应用退出前必须释放资源：关闭数据库连接、结束进行中的写入、清理临时文件
- 主进程内的定时任务、文件监听、外部进程必须在退出路径上统一取消与关闭，避免句柄泄漏导致无法退出
- 未捕获异常与 `unhandledRejection` 必须记录日志；崩溃恢复策略（重启、提示用户）显式声明，不静默吞掉

### 本地能力规则
- 文件系统、系统对话框、剪贴板、shell 调用、自动更新等能力统一封装在 `integrations/` 或等价目录
- 所有本地路径都必须显式区分：
  - 应用配置目录
  - 应用数据目录（`app.getPath('userData')`）
  - 缓存目录
  - 临时目录
- 禁止在代码中硬编码用户目录或绝对路径
- 涉及系统调用时必须说明权限边界、失败回退与日志

### 前后端契约规则
- 前端可见的 Handler 返回值应先定义为明确 TypeScript 类型
- 类型与前端 schema 尽量一一对应，统一序列化命名（默认保持字段原名校验一致性，与前端 TS 类型一致）
- 若仓库已经通过共享类型包或 schema 生成维护契约，优先沿用，不重复造一套平行定义
- 新增或修改 channel 时，同步更新渲染层 IPC 封装层与文档

### 代码组织规范
- 一个文件只放一个主类型：IPC handler 注册、Service、Repository、类型、Error 等，文件名与主类型一致。
- 类型目录组织：放 `types/`，按业务域或类型分子目录，一类一文件，不合并到 `types.ts`。
- 触发拆分的信号：职责混杂、同文件出现多个主类型、参数或字段持续堆叠、方法跨多个不相关业务时，应主动拆分。
- 允许例外：模块内部且只服务当前文件的小辅助类型、与主类型强绑定的局部 mapping 工具、测试文件中只服务当前测试用例的小型 fixture。
- 反模式：`types.ts` 长期堆放多个领域类型；把 Handler 专用 request 类型内联写在 handler 注册文件里；IPC 协议类型与业务模型混在同一文件。

<!-- include: shared/application/kiss.md -->

### 异常处理规范
- 自定义业务错误统一收敛到 `errors/`
- 区分参数错误、权限错误、资源不存在、状态冲突、系统异常
- 禁止把原始数据库错误、IO 错误或底层异常直接透传给页面
- 日志中允许保留详细错误，但给前端的错误文案应稳定且可理解

<!-- include: shared/backend/not-recommended.md -->

### 代码注释规范
- 必须补 TSDoc / JSDoc 注释的位置：
  - IPC handler 处理函数（前端可见契约）：说明 channel 用途、关键参数、返回结果。
  - Service 导出方法：说明该能力做什么，不写实现细节。
- 接口或类型已注释时，实现处不重复注释；只有未被类型定义说明的自有核心方法才需要补注释。
- 默认不写 `@remarks` / 实现说明块：多为实现细节，按“不写实现细节”原则省略。
  - 例外：IPC handler 需要向渲染进程说明调用约定、参数格式、错误码等对外契约信息时，可保留精简后的说明，但不写内部实现细节。
- 优先补行内注释的位置：权限校验、状态流转、事务边界、超时 / 重试 / 幂等等复杂分支，说明“为什么这样做”——包括 fail fast 的原因、为什么跳过某类输入、为什么保留某一步骤。
- 不要补的注释：简单赋值、简单 `if` / `return`、“返回结果”这类复述代码的低价值注释。
- 注释统一使用中文，不使用纯英文注释；不在注释里泄露密钥、token、连接串或真实生产地址。

<!-- include: shared/application/anti-patterns.md -->

<!-- include: shared/application/maintainability-check.md -->

---

## 测试规则

### 提交前最小回归
- 默认执行：项目现有的格式化、静态检查或 TypeScript 编译校验
- Service 改动：至少补一项业务规则、权限判断或状态流转验证
- IPC handler 改动：至少补一项输入输出、错误分支或前后端契约验证
- Repository / SQLite 改动：至少补一项查询结果、事务、迁移或路径写入验证
- 若仓库暂时缺少自动化测试基础，可使用前端联调、集成脚本或等价验证替代，但必须能证明核心逻辑真实生效

### 主进程测试
- Service 测试：验证业务规则、权限校验、状态流转
- Repository 测试：验证查询条件、分页、排序、事务或数据库映射
- Handler 测试：验证输入输出结构和错误分支；`ipcMain.handle` 相关逻辑通过抽取纯函数或注入 `ipcMain` 替身验证
- 涉及文件系统或系统能力时，优先通过可控临时目录和测试替身验证

<!-- include: shared/backend/logging-rules.md -->

<!-- include: shared/no-test-fallback.md -->

---
