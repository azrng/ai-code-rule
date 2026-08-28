---
rule_id: doc-agents
version: 1.15.0
last_updated: 2026-08-28
dependencies: [agents-root]
---

# 文档规则

<!-- include: shared/doc/scope-v1.md -->

<!-- include: shared/doc/common-tasks.md -->

---

<!-- include: shared/doc/advisory.md -->

---

## 阶段 0 设计文档

<!-- include: shared/doc/stage0-design-v2.md -->

<!-- include: shared/doc/design-checklist.md -->

---

## 接口契约规范

### 契约要求

- **唯一来源**：Swagger 自动生成
- **定义时机**：后端实现完成后，通过 Swagger UI 查看接口定义
- **变更规则**：接口变更时，必须先修改 DTO，再同步修改前端类型定义
- **文件结构建议**：前端按业务域拆分类型定义文件

---

## 设计系统文件规范
`design-system.yaml` 是唯一设计系统文件，包含：
- 品牌与产品风格意图
- 语义化颜色 token
- 排版 token
- 间距、圆角、阴影 token
- 布局与断点规则
- Ant Design 主题配置映射
- 页面状态规范
- 组件使用规则
- 明确的 `do / dont` 规则

---

<!-- include: shared/doc/doc-update.md -->

<!-- include: shared/doc/doc-directory.md -->

<!-- include: shared/doc/requirement.md -->

<!-- include: shared/doc/retro-format-v1.md -->

---
