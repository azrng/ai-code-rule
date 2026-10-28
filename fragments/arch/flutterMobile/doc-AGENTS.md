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

## 设计系统文件规范
`design-system.yaml` 是唯一设计系统文件，包含：
- 品牌与产品风格意图
- 语义化颜色 token（Material 3 ColorScheme 映射）
- 排版 token
- 间距、圆角、阴影 token
- 动效规范（页面转场、列表动画、交互反馈）
- 移动端模式（导航、列表、表单、反馈）
- 页面状态规范（loading / empty / error / no-permission）
- 组件使用规则
- 响应式设计规则
- 明确的 `do / dont` 规则

---

<!-- include: shared/doc/doc-update.md -->

<!-- include: shared/doc/doc-directory.md -->

<!-- include: shared/doc/requirement.md -->

<!-- include: shared/doc/retro-format.md -->
