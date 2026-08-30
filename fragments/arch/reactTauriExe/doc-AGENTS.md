---
rule_id: doc-agents
version: 1.17.0
last_updated: 2026-08-30
dependencies: [agents-root]
---

# 文档规则

<!-- include: shared/doc/scope.md -->

### 常见任务入口
- 新增功能方案：先看阶段 0 设计文档
- 更新 Command、DTO、schema 或事件载荷：先看契约说明规范
- 更新设计系统或通用说明：先看设计系统文件规范与文档目录约定

---

<!-- include: shared/doc/advisory.md -->

---

## 阶段 0 设计文档

### 阶段 0 — 设计文档（新功能强制前置）

**触发条件**：用户提出新功能需求

**执行方式**：由用户与 AI 协作产出，用户最终确认定稿

**产物**：`doc/design/设计文档.md`（全项目一份，新功能以新增章节方式补充）

**文档必须包含以下四个部分**：

| 部分 | 内容要求 |
| --- | --- |
| 架构设计 | 模块划分、Tauri / Rust / 前端边界、数据流向、缓存与权限方案 |
| 功能需求 | 页面清单、功能点明细、业务规则、权限矩阵 |
| 界面原型 | 每个页面的 ASCII 线框图，包含 `loading` / `empty` / `error` / `no-permission` 状态 |
| 交互说明 | 操作流程、状态流转、边界场景、错误提示与恢复方式 |

**门控规则**：
- 用户明确确认设计文档后，才允许进入阶段 1
- AI 在此阶段只输出文档内容，不编写实现代码

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
