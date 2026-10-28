---
rule_id: doc-agents
version: 1.43.0
last_updated: 2026-09-03
dependencies: [agents-root]
---

# 文档规则

<!-- include: shared/doc/scope.md -->

---

<!-- include: shared/doc/advisory.md -->

---

## 阶段 0 设计文档

### 阶段 0 — 设计文档（新功能强制前置）

**触发条件**：用户提出新功能需求

**执行方式**：由用户与 AI 协作产出，用户最终确认定稿

**产物**：当前业务模块/功能对应的 `doc/design/<module>/<feature>.md`，并关联已确认的 `doc/requirements/<module>/<feature>.md`；跨模块方案另记入 `doc/design/architecture.md`

**文档必须包含以下四个部分**：

| 部分 | 内容要求 |
| --- | --- |
| 需求追溯 | 关联 `REQ-` 编号、明确本次设计覆盖的范围和未覆盖项，不重复粘贴需求正文 |
| 技术设计 | 模块职责、Tauri / Rust / 前端边界、数据流向、缓存与权限方案 |
| 界面原型 | 每个页面的 ASCII 线框图，包含 `loading` / `empty` / `error` / `no-permission` 状态；原型列出的页面是原型阶段的输入范围 |
| 交互与异常 | 操作流程、状态流转、边界场景、错误提示、恢复方式和验证方式 |

**门控规则**：
- 用户明确确认对应的需求文档和设计文档后，才允许进入阶段 1
- AI 在此阶段只输出文档内容，不写任何实现代码

---

## 契约说明规范

### 契约要求

- **前端唯一事实来源**：`schemas/` 中的 Zod schema 与共享类型定义
- **Rust 边界事实来源**：`src-tauri/` 中的 DTO / Command 输入输出定义
- **定义方式**：表单 schema、Query schema、Command 入参 schema、Command 出参结构必须成对维护
- **变更规则**：字段或行为变化时，必须同步更新 schema、类型、页面、Command 封装与测试
- **调用选择原则**：
  - 桌面应用内部业务交互：优先 Tauri Command
  - 事件通知或后台状态推送：使用 event
  - 外部服务访问：使用前端统一请求层或 Rust 侧 integration，不新增本地 HTTP 桥接

---

<!-- include: shared/doc/design-system-file.md -->

---

<!-- include: shared/doc/doc-update-desktop.md -->

<!-- include: shared/doc/doc-directory.md -->

<!-- include: shared/doc/requirement.md -->

<!-- include: shared/doc/retro-format.md -->

---
