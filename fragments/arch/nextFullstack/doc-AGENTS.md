---
rule_id: doc-agents
version: 1.15.0
last_updated: 2026-08-28
dependencies: [agents-root]
---

# 文档规则

<!-- include: shared/doc/scope-v3.md -->

---

<!-- include: shared/doc/advisory.md -->

---

## 阶段 0 设计文档

### 阶段 0 — 设计文档（新功能强制前置）

**触发条件**：用户提出新功能需求

**执行方式**：由用户与 AI 协作产出，用户最终确认定稿

**产物**：`doc/design/设计文档.md`（全项目一份，新功能以新增章节方式补充）

**文档必须包含以下四个部分**：

| 部分 | 内容要求 |
| --- | --- |
| 架构设计 | 模块划分、服务边界、数据流向、缓存与权限方案 |
| 功能需求 | 页面清单、功能点明细、业务规则、权限矩阵 |
| 界面原型 | 每个页面的 ASCII 线框图，包含 `loading` / `empty` / `error` / `no-permission` 状态 |
| 交互说明 | 操作流程、状态流转、边界场景、错误提示与恢复方式 |

**门控规则**：
- 用户明确确认设计文档后，才允许进入阶段 1
- AI 在此阶段只输出文档内容，不编写实现代码

---

## 契约说明规范

### 契约要求

- **唯一事实来源**：`src/schemas/` 中的 Zod schema 与共享类型定义
- **定义方式**：表单 schema、查询 schema、服务出参 schema 必须成对维护
- **变更规则**：字段或行为变化时，必须同步更新 schema、类型、页面和测试
- **回退规则**：后续阶段发现契约缺口时，按根 `AGENTS.md` 的纠错与回退机制回到契约变更流程，不在实现层绕过契约。
- **接口选择原则**：
  - 同站点表单提交、状态切换、管理后台操作：优先 Server Actions
  - 需被外部系统访问、Webhook、文件流、第三方调用：使用 Route Handlers
  - Server Component 内部取数：优先直接调用服务层，不要绕路请求自己的 `/api`

---

## 设计系统文件规范
`design-system.yaml` 是唯一设计系统文件，包含：
- 品牌与产品风格意图
- 颜色、排版、间距、圆角、阴影等 token
- 布局与断点规则
- Next.js 页面状态规范
- 表单与反馈组件规则
- 明确的 `do / dont`

---

<!-- include: shared/doc/doc-update.md -->

<!-- include: shared/doc/doc-directory.md -->

<!-- include: shared/doc/requirement.md -->

<!-- include: shared/doc/retro-format-v1.md -->

---
