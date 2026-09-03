---
rule_id: doc-agents
version: 1.19.0
last_updated: 2026-08-31
dependencies: [agents-root]
---

# 文档规则

<!-- include: shared/doc/scope.md -->

---

<!-- include: shared/doc/advisory.md -->

---

## 阶段 0 设计文档

<!-- include: shared/doc/stage0-design-fullstack.md -->

---

## 契约说明规范

### 契约要求

- **唯一事实来源**：`src/schemas/` 中的 Zod schema 与共享类型定义
- **定义方式**：表单 schema、查询 schema、服务出参 schema 必须成对维护
- **变更规则**：字段或行为变化时，必须同步更新 schema、类型、页面和测试
- **回退规则**：后续阶段发现契约缺口时，按 `collaboration-AGENTS.md`「纠错、回退与重构」回到契约变更流程，不在实现层绕过契约。
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

<!-- include: shared/doc/retro-format.md -->

---
