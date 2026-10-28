---
rule_id: doc-agents
version: 1.33.0
last_updated: 2026-09-02
dependencies: [agents-root]
---

# 文档规则

<!-- include: shared/doc/scope-api.md -->

---

<!-- include: shared/doc/advisory-layered.md -->

---

## 阶段 0 设计文档

<!-- include: shared/doc/stage0-design-api.md -->

<!-- include: shared/doc/design-checklist.md -->

---

## 接口契约规范

### 契约要求

- **唯一来源**：`app/schemas/` 下的 Pydantic Schema（请求 / 响应 / 查询模型）与 `app/services/interfaces/` 下的服务接口定义。
- **验证方式**：通过 FastAPI 自带的 Swagger UI（`/docs`）展示和验证契约，不把 Swagger 文档本身当作手工维护源。
- **变更规则**：接口契约变更时，必须先修改 Schema 或服务接口定义，再同步修改服务实现、数据访问映射、测试与相关文档。
- **文件结构建议**：按业务域拆分 Schema 和接口定义（`schemas/requests/`、`schemas/responses/`），避免将全部契约堆叠在单个文件中。

---

<!-- include: shared/doc/doc-update.md -->

<!-- include: shared/doc/doc-directory-api.md -->

<!-- include: shared/doc/requirement-api.md -->

---
