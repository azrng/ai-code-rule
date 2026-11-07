---
rule_id: doc-agents
version: 1.21.0
last_updated: 2026-08-31
dependencies: [agents-root]
---

# 文档规则

<!-- include: shared/doc/scope.md -->

---

<!-- include: shared/doc/advisory.md -->

---

<!-- include: shared/doc/stage0-design.md -->

---

### 接口契约规范

- **唯一来源**：`lib/data/database/`（drift 表定义 `tables.dart` 与生成的实体数据类）承载实体契约；`lib/data/models/` 承载视图 / 展示 DTO
- **定义时机**：阶段 0 设计文档确认后，阶段 1 开始前
- **变更规则**：模型字段变更时，必须同步修改 Provider、Service、Repository 及相关测试；drift 表变更后需重跑 `dart run build_runner build` 重新生成
- **文件结构建议**：实体契约由 drift 生成（不默认引入 freezed）；视图 DTO 按业务域分文件

---

<!-- include: shared/doc/design-system-file.md -->
- 移动端增量：颜色 token 映射 Material 3 ColorScheme；动效规范（页面转场、列表动画、交互反馈）；移动端模式（导航、列表、表单、反馈）与响应式设计规则
- 页面状态覆盖 loading / empty / error / no-permission

---

<!-- include: shared/doc/doc-update.md -->

<!-- include: shared/doc/doc-directory.md -->

<!-- include: shared/doc/requirement.md -->

<!-- include: shared/doc/retro-format.md -->
