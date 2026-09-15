---
rule_id: doc-agents
version: 1.57.0
last_updated: 2026-09-07
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

<!-- include: shared/doc/contract-schema-web.md -->
- **唯一事实来源目录**：`src/schemas/`（或 `shared/`）
- **接口选择原则**：
  - 同站点表单提交、状态切换、管理后台操作：统一走 `server/api` 路由，前端用 `$fetch` / `useFetch` 调用
  - 需被外部系统访问、Webhook、文件流、第三方调用：使用 `server/routes`（自定义路径，不带 `/api` 前缀）
  - 页面首屏取数：优先 `useFetch` / `useAsyncData`，由 Nitro 在服务端完成，不绕路请求自己的 `/api`

---

## 设计系统文件规范
`design-system.yaml` 是唯一设计系统文件，包含：
- 品牌与产品风格意图
- 颜色、排版、间距、圆角、阴影等 token
- 布局与断点规则
- Nuxt 页面状态规范
- 表单与反馈组件规则
- 明确的 `do / dont`

---

<!-- include: shared/doc/doc-update.md -->

<!-- include: shared/doc/doc-directory.md -->

<!-- include: shared/doc/requirement.md -->

<!-- include: shared/doc/retro-format.md -->

---
