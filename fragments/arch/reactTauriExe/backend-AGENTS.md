---
rule_id: backend-agents
version: 1.14.0
last_updated: 2026-08-27
dependencies: [agents-root]
---

# 后端规则

## 适用范围

- 作用域：Tauri Command、Rust 服务、数据访问、本地系统能力与后端测试
- 触发场景：涉及 `src-tauri/`、SQLite、文件系统、系统权限、插件、异常处理或测试时阅读

### 常见任务入口
- 改前后端调用契约：先看 Command 规则与输入输出约束
- 改业务流程或状态流转：先看服务层规则与异常处理规范
- 改本地数据库、文件、配置读写：先看数据访问规则与本地能力规则
- 补 Rust / Tauri 回归：先看 `提交前最小回归` 与测试规则

---

## 技术栈

### Runtime
- Tauri 2 + Rust
- Tokio（异步运行时）
- Tauri Command / event / plugin 体系

### 数据与本地能力
- 本地持久化默认推荐：SQLite
- 数据访问默认推荐：`sqlx + SQLite`
- 若仓库已稳定使用 `rusqlite` 且迁移收益不高，可沿用现有实现，不强行替换
- 配置、日志、缓存、用户目录读写统一通过 Tauri 与 Rust 侧封装处理

### 通信原则
- 前后端通信优先使用 Tauri Command、event 和 plugin API
- 默认**不**引入内嵌 `axum` / 本地 HTTP 服务作为桌面应用内部桥接层
- 仅在以下场景才考虑额外本地服务层：
  - 明确需要流式下载或上传代理
  - 需要本地 WebSocket / SSE
  - 需要对接必须通过 HTTP 语义运行的第三方模块
  - 已证明 Command / plugin 无法满足能力要求

如果仓库已经有真实实现，以现有代码为准，不要强行重构或替换技术栈。

## 主动建议规则
- 发现 Tauri Command 与 Rust 服务职责混杂、SQLite 访问封装不一致或错误处理缺失时，应主动提醒
- 发现接口契约、绑定结构体、SQLite 表结构或状态流转可能影响历史数据、兼容性或权限安全时，必须先说明风险，不得直接扩大修改
- 发现可以复用既有 Tauri Command、Rust 服务、SQLite 访问封装或错误处理封装时，应优先建议复用
- 不确定业务规则、数据含义或系统能力边界时，应按根 `AGENTS.md` 的查证优先级处理，禁止按通用经验补写规则

技术债务与重构判断遵循根 `AGENTS.md` 的全局规则。

---

## 推荐目录结构

若仓库尚未形成稳定结构，可优先参考以下组织方式；若仓库已有实现，以现状为准，不强制迁移。

```text
src-tauri/
├── src/
│   ├── commands/            # Tauri Command 入口
│   ├── services/            # 业务服务
│   ├── repositories/        # 数据访问层
│   ├── models/              # 领域模型与 DTO
│   ├── db/                  # 数据库连接、migration、查询辅助
│   ├── integrations/        # 外部系统与系统能力封装
│   ├── errors/              # 统一错误类型
│   ├── state/               # AppState / 共享状态
│   ├── config/              # 配置加载
│   ├── events/              # event 名称与事件载荷
│   ├── lib.rs
│   └── main.rs
├── migrations/
├── Cargo.toml
└── tauri.conf.json
```

---

## 阶段 2 — Rust / Tauri 实现

**触发条件**：用户发出“开始后端开发”或等价指令

**入场要求**：前端页面结构已完成，schema 与契约清晰

**工作内容**：
1. 按 `Command -> Service -> Repository -> SQLite / Local Capability` 分层实现本地能力。
2. 根据需求维护本地数据库 schema、迁移脚本、配置读写与文件目录约束。
3. 完成权限、日志、缓存、状态流转、分页、筛选、排序等 Rust 侧逻辑。
4. 每个核心能力都必须可通过前端联调、测试或脚本独立验证。

**门控规则**：
- 核心服务与 Command 独立验证通过后，才允许进入阶段 3。

---

## Rust / Tauri 规则

### 分层依赖规则

```text
Tauri Command / Event
          ↓
       Service
          ↓
     Repository
          ↓
 SQLite / FileSystem / External Capability
```

### Command 规则
- Command 只负责参数解析、权限校验、调用服务与返回稳定结果
- Command 名称使用明确业务语义，不使用模糊缩写
- 前端可见的输入输出结构必须稳定、可序列化、可测试
- 不在 Command 中直接写复杂数据库查询或系统调用
- 统一在边界层把业务错误转换为稳定、可识别的前端可消费结果

### Service 规则
- 业务校验、权限判断、去重判断、状态流转、事务边界放在 Service 层
- 返回明确的类型结果，不返回未约束的 `serde_json::Value`
- 涉及耗时操作时显式处理超时、取消、失败重试或幂等
- 若需要跨多个仓储或能力协作，由 Service 负责组织，不把编排逻辑散到 Command

### Repository / 数据访问规则
- 数据访问不得出现在 Command 或前端代码中
- 复杂查询、分页、排序、条件拼装收敛到 Repository
- 默认使用 migration 管理 schema 变更，不依赖启动时隐式改表
- SQLite 表与字段命名统一使用 snake_case
- schema 变更后必须同步 migration、类型和文档

### 本地能力规则
- 文件系统、进程、更新、剪贴板、系统对话框、shell 调用等能力统一封装在 `integrations/` 或等价目录
- 所有本地路径都必须显式区分：
  - 应用配置目录
  - 应用数据目录
  - 缓存目录
  - 临时目录
- 禁止在代码中硬编码用户目录或绝对路径
- 涉及系统调用时必须说明权限边界、失败回退与日志

### 前后端契约规则
- 前端可见的 Command 返回值应先定义为明确 DTO
- DTO 与前端 schema 尽量一一对应，避免一边 `camelCase` 一边 `snake_case` 无规则混用
- 若仓库已经通过 TS 类型生成或共享 schema 维护契约，优先沿用，不重复造一套平行定义
- 新增或修改 Command 时，同步更新前端封装层与文档

### 异常处理规范
- 自定义业务异常统一收敛到 `errors/`
- 区分参数错误、权限错误、资源不存在、状态冲突、系统异常
- 禁止把原始数据库错误、IO 错误或底层插件错误直接透传给页面
- 日志中允许保留详细错误，但给前端的错误文案应稳定且可理解

### 不推荐的默认方案
- 不要默认把桌面应用实现成“前端 + 本地 HTTP 服务”的双套架构
- 不要在没有必要时把简单本地 CRUD 设计成过度复杂的微服务式模块
- 不要为了“看起来像 Web 后端”而引入多余的本地网关层

### 代码组织规范
- 一个文件只放一个主对象：Tauri command、service、repository、struct（数据模型 / DTO）、配置各自独立文件，文件名与主对象一致。
- 数据契约（请求 / 响应 / DTO struct）按业务域分文件定义，不合并到一个 `models.rs` / 大杂烩文件。
- 触发拆分的信号：单文件出现多个主 struct、函数或字段持续堆叠、模块跨多个不相关业务。
- 允许例外：紧耦合的常量、trait 分组、测试 fixture。
- 反模式：一个 `models.rs` 堆放所有 struct；把多个不相关 service 塞进同一文件。

---

## 测试规则

### 提交前最小回归
- 默认执行：项目现有的格式化、静态检查或编译校验
- Service 改动：至少补一项业务规则、权限判断或状态流转验证
- Command 改动：至少补一项输入输出、错误分支或前后端契约验证
- Repository / SQLite 改动：至少补一项查询结果、事务、migration 或路径写入验证
- 若仓库暂时缺少自动化测试基础，可使用前端联调、集成脚本或等价验证替代，但必须能证明核心逻辑真实生效

### Rust / Tauri 测试
- Service 测试：验证业务规则、权限校验、状态流转
- Repository 测试：验证查询条件、分页、排序、事务或数据库映射
- Command 测试：验证输入输出结构和错误分支
- 涉及文件系统或系统能力时，优先通过可控临时目录和测试替身验证

<!-- include: shared/no-test-fallback.md -->

---
