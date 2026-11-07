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

<!-- include: shared/doc/contract-schema-web.md -->
- **唯一事实来源目录**：`src/schemas/`
- **接口选择原则**：
  - 同站点表单提交、状态切换、管理后台操作：优先 Server Actions
  - 需被外部系统访问、Webhook、文件流、第三方调用：使用 Route Handlers
  - Server Component 内部取数：优先直接调用服务层，不要绕路请求自己的 `/api`

---

<!-- include: shared/doc/design-system-file.md -->
- 页面状态规范按 Next.js App Router 场景落地（含流式与挂起态）

---

<!-- include: shared/doc/doc-update.md -->

<!-- include: shared/doc/doc-directory.md -->

<!-- include: shared/doc/requirement.md -->

<!-- include: shared/doc/retro-format.md -->

---
