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

<!-- include: shared/doc/advisory-dto.md -->

---

## 阶段 0 设计文档

<!-- include: shared/doc/stage0-design.md -->

<!-- include: shared/doc/requirement-autogen.md -->

<!-- include: shared/doc/design-checklist.md -->

---

## DTO 契约规范

### 契约要求

- **唯一来源**：`src/AppName/Models/DTOs/` 中最终定稿的 DTO / 契约类。
- **定义时机**：阶段 1 视图实现时同步产出，阶段 2 严格按其实现业务逻辑。
- **变更规则**：DTO 变更时，必须先修改契约类，再同步修改 ViewModel、Service、Repository 映射和相关文档。
- **文件结构建议**：按业务域拆分 DTO，避免单文件堆叠所有契约。
- **回退规则**：后续阶段发现 DTO 缺口时，按根 `AGENTS.md` 的纠错与回退机制回到契约变更流程，不在实现层绕过契约。

---

## 设计系统文件规范
`design-system.yaml` 是唯一设计系统文件，包含：
- 品牌与产品风格意图
- 语义化颜色 token
- 排版 token
- 间距、圆角、阴影 token
- 布局与断点规则
- 页面状态规范
- 组件使用规则
- 明确的 `do / dont` 规则

---

<!-- include: shared/doc/doc-update.md -->

<!-- include: shared/doc/doc-directory.md -->

<!-- include: shared/doc/requirement.md -->

<!-- include: shared/doc/retro-format.md -->

---
