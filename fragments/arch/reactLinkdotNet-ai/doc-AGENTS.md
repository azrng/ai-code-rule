---
rule_id: doc-agents
version: 1.43.0
last_updated: 2026-09-03
dependencies: [agents-root, ai-agents]
---

# 文档规则

> 本规则的设计系统基于 Tailwind + shadcn 体系；含 **AI 产品交互补充**（Agent 输出展示、流式契约、用户确认节点、人工审核）。

## 适用范围

- 作用域：需求文档、设计文档、契约说明、设计系统、AI 产品交互补充与开发文档维护
- 触发场景：涉及阶段 0 设计、契约说明、Agent 交互流程、`doc/index.md`、`doc/requirements/` 或 `doc/design/` 时阅读

---

<!-- include: shared/doc/advisory.md -->
- 需求描述与设计文档中的 AI 部分：缺少 Agent 交互节点、流式事件约定时，同样补充为待确认项并检查与 `ai-AGENTS.md` 的一致性

---

## 阶段 0 设计文档

<!-- include: shared/doc/stage0-head.md -->

**文档必须包含以下部分**：

| 部分 | 内容要求 |
| --- | --- |
| 需求追溯 | 关联 `REQ-` 编号、明确本次设计覆盖的范围和未覆盖项，不重复粘贴需求正文 |
| 技术设计 | 模块职责、数据流向、技术决策和 AI 层 / Agent 编排边界 |
| 界面原型 | 每个页面的 ASCII 线框图，含关键状态（loading / streaming / empty / error）；原型列出的页面是原型阶段的输入范围 |
| 交互与异常 | 操作流程、状态流转、边界场景、错误处理方式、Agent 交互节点和验证方式 |
| AI 设计（如涉及） | 模型选择、Agent 职责、工具 / Skills 清单、Prompt 关键约束、可选能力是否引入 |

<!-- include: shared/doc/design-checklist.md -->

<!-- include: shared/doc/stage0-gate.md -->
- 涉及 AI 能力时，"AI 设计"部分需对照 `ai-AGENTS.md` 的核心 vs 可选清单，说明哪些是核心、哪些暂不引入

---

## AI 产品交互补充（本规则集增量）

Agent 产品的前端不能只是一个聊天框。设计 Agent 交互时，必须至少覆盖以下要点：

### 1. Agent 输出结构化展示
- 明确 Agent 会输出哪些结构化产物（候选列表、大纲、草稿、图表引用、步骤进度等）
- 每类产物对应一种展示组件（卡片 / 列表 / 步骤条 / 预览区），不能全塞进一段文本
- 线框图需画出每种产物的关键状态

### 2. 用户确认节点（Human-in-the-Loop）
- 标注哪些步骤需要用户确认 / 选择 / 编辑后才能继续
- 高风险动作（删除、外发、计费、不可逆操作）必须有人工确认，不得由 Agent 自动执行
- 确认节点在交互说明中显式画出，包含确认前 / 确认后 / 拒绝后的流转

### 3. 流式与进度反馈
- 标注哪些输出是流式的（打字机）、哪些是多步长任务（进度 / 阶段）
- 流式中断、超时、空响应的兜底展示
- 流式事件结构需与后端约定一致（见契约说明规范）

### 4. 状态完整性
- Agent 相关页面同样必须完整覆盖 loading / streaming / empty / error / no-permission
- 错误信息可读、可重试，不暴露模型 SDK 细节给用户

### 5. 边界与降级
- 模型不可用、限流、超长输入、空输出、工具调用失败时的降级路径
- 成本 / 延迟敏感场景的处理策略

---

## 接口契约规范

### 契约要求
- **最终事实来源**：已实现的普通 REST 接口以 Swagger 自动生成为准；阶段 0 的新增接口先按契约生命周期创建 `CON-` 草案，草案不替代 Swagger
- **定义时机**：阶段 0 确认契约草案，后端实现完成后通过 Swagger UI 校准最终接口定义
- **变更规则**：接口变更时，必须先修改 DTO，再同步修改前端类型定义
- **文件结构建议**：前端按业务域拆分类型定义文件

### 流式 / Agent 契约补充
- 流式接口（SSE / 流式响应）的事件结构（事件类型、字段、终止信号）必须单独成文，作为前后端共同基准
- 流式事件字段变更时，按"先改契约 → 同步前后端 → 同步文档"顺序处理，不得前端私自解析
- Agent 中间产物（候选、草稿、步骤）的字段结构同样纳入契约管理

---

## 设计系统文件规范
`design-system.yaml` 是唯一设计系统文件，包含：
- 品牌与产品风格意图
- 语义化颜色 token（落地为 Tailwind 主题与 shadcn CSS 变量）
- 排版 token
- 间距、圆角、阴影 token
- 布局与断点规则
- shadcn / Tailwind 主题映射
- 页面状态规范（含 streaming）
- 组件使用规则
- 明确的 `do / dont` 规则

---

## 文档更新要求
以下内容发生变化时，必须同步更新相关文档：
- 命令、脚本或启动方式
- 环境变量（含模型密钥、Base URL、模型名）
- 配置项
- 构建、发布或部署流程
- 数据库结构、迁移或初始化方式、数据库切换（SQLite ↔ PostgreSQL）
- 模型对接方式、Agent 职责、工具 / Skills 清单、Prompt 关键约束
- 流式事件结构或对外接口约定

文档同步是命中上述清单的任务的完成前置条件：置 `DONE` 前先比对本次改动，命中则在同一任务内更新文档，不得跨任务事后补录；AI 相关变化同步 `ai-AGENTS.md` 对应小节。

优先更新已有文档，不新增重复说明。

<!-- include: shared/doc/contract-lifecycle.md -->

<!-- include: shared/doc/doc-directory.md -->

<!-- include: shared/doc/requirement.md -->

<!-- include: shared/doc/retro-format.md -->

---
