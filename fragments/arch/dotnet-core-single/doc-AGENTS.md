---
rule_id: doc-agents
version: 1.43.0
last_updated: 2026-09-03
dependencies: [agents-root]
---

# 文档规则

<!-- include: shared/doc/scope-api.md -->

<!-- include: shared/doc/common-tasks-api.md -->

---

<!-- include: shared/doc/advisory-layered.md -->

---

## 阶段 0 设计文档

<!-- include: shared/doc/stage0-head.md -->

**文档必须包含以下四个部分**：

| 部分 | 内容要求 |
| --- | --- |
| 需求追溯 | 关联 `REQ-` 编号、明确本次设计覆盖的范围和未覆盖项，不重复粘贴需求正文 |
| 技术设计 | 模块职责、接口边界、数据流向和技术决策说明 |
| 契约引用 | 标注相关接口 / DTO / Schema 的事实来源和契约 ID，不在设计文档中复制完整字段表 |
| 处理流程 | 核心处理链路、状态变化、边界场景、异常处理和验证方式 |

<!-- include: shared/doc/stage0-gate.md -->

<!-- include: shared/doc/design-checklist.md -->

---

## 接口契约规范

### 契约要求

- **唯一来源**：`Application/{业务域}/Dto/` 下的 DTO、AppService 接口与 Controller 公开接口定义。
- **验证方式**：通过 Swagger UI 展示和验证契约，不把 Swagger 文档本身当作手工维护源。
- **变更规则**：接口契约变更时，必须先修改 DTO 或接口定义，再同步修改 AppService、数据映射、测试与相关文档。
- **回退规则**：后续阶段发现契约缺口时，按 `collaboration-AGENTS.md`「纠错、回退与重构」回到契约变更流程，不在实现层绕过契约。
- **文件结构建议**：按业务域拆分 DTO，避免将全部请求和响应堆叠在单个文件中。

---

<!-- include: shared/doc/doc-update.md -->

<!-- include: shared/doc/doc-directory-api.md -->

<!-- include: shared/doc/requirement-api.md -->

<!-- include: shared/doc/retro-format.md -->

---
