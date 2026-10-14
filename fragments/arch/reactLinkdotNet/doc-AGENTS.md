---
rule_id: doc-agents
version: 1.19.0
last_updated: 2026-08-31
dependencies: [agents-root]
---

# 文档规则

<!-- include: shared/doc/scope.md -->

<!-- include: shared/doc/common-tasks.md -->

---

<!-- include: shared/doc/advisory.md -->

---

## 阶段 0 设计文档

<!-- include: shared/doc/stage0-design.md -->

<!-- include: shared/doc/design-checklist.md -->

---

## 接口契约规范

### 契约要求

- **最终事实来源**：已实现的普通 REST 接口以 Swagger 自动生成为准；阶段 0 的新增接口先按契约生命周期创建 `CON-` 草案，草案不替代 Swagger
- **定义时机**：阶段 0 确认契约草案，后端实现完成后通过 Swagger UI 校准最终接口定义
- **变更规则**：接口变更时，必须先修改 DTO，再同步修改前端类型定义
- **文件结构建议**：前端按业务域拆分类型定义文件

---

<!-- include: shared/doc/design-system-file-web.md -->

---

<!-- include: shared/doc/doc-update.md -->

<!-- include: shared/doc/doc-directory.md -->

<!-- include: shared/doc/requirement.md -->

<!-- include: shared/doc/retro-format.md -->

---
