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

<!-- include: shared/doc/advisory-layered.md -->

---

## 阶段 0 设计文档

### 阶段 0 — 设计文档（新功能强制前置）

**触发条件**：用户提出新功能需求

**执行方式**：由用户与 AI 对话协作产出，用户最终确认定稿

**产物**：`doc/design/设计文档.md`（全项目一份，新功能以新章节追加，不新建文件）

**文档必须包含以下四个部分**：

| 部分 | 内容要求 |
| ---- | -------- |
| 架构设计 | 模块划分、接口边界、数据流向、涉及的技术决策说明 |
| 功能需求 | 接口列表、核心业务规则、权限要求、关键状态说明 |
| 接口契约 | 请求参数、响应结构、错误码、示例数据 |
| 处理流程 | 核心处理链路、状态流转、边界场景、异常处理方式 |

**门控规则**：
- 用户明确确认设计文档后，才允许进入阶段 1
- AI 在此阶段只输出文档内容，不写任何实现代码

<!-- include: shared/doc/design-checklist.md -->

---

## 接口契约规范

### 契约要求

- **唯一来源**：`Application/{业务域}/Dto/` 下的 DTO、AppService 接口与 Controller 公开接口定义。
- **验证方式**：通过 Swagger UI 展示和验证契约，不把 Swagger 文档本身当作手工维护源。
- **变更规则**：接口契约变更时，必须先修改 DTO 或接口定义，再同步修改 AppService、数据映射、测试与相关文档。
- **回退规则**：后续阶段发现契约缺口时，按根 `AGENTS.md` 的纠错与回退机制回到契约变更流程，不在实现层绕过契约。
- **文件结构建议**：按业务域拆分 DTO，避免将全部请求和响应堆叠在单个文件中。

---

<!-- include: shared/doc/doc-update.md -->

<!-- include: shared/doc/doc-directory.md -->

<!-- include: shared/doc/requirement.md -->

<!-- include: shared/doc/retro-format.md -->

---
