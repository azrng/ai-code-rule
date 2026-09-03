---
rule_id: doc-agents
version: 1.43.0
last_updated: 2026-09-03
dependencies: [agents-root]
---

# 文档规则

<!-- include: shared/doc/scope.md -->

### 常见任务入口
- 新增功能方案：先看阶段 0 设计文档
- 更新 IPC method、DTO、schema 或事件载荷：先看契约说明规范
- 更新设计系统或通用说明：先看设计系统文件规范与文档目录约定

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
| 技术设计 | 模块职责、Electron / .NET / 前端边界、数据流向、缓存与权限方案 |
| 界面原型 | 每个页面的 ASCII 线框图，包含 `loading` / `empty` / `error` / `no-permission` 状态 |
| 交互与异常 | 操作流程、状态流转、边界场景、错误提示、恢复方式和验证方式 |

**门控规则**：
- 用户明确确认对应的需求文档和设计文档后，才允许进入阶段 1
- AI 在此阶段只输出文档内容，不写任何实现代码

<!-- include: shared/doc/design-checklist.md -->

---

## 契约说明规范

### 契约要求

- **前端唯一事实来源**：`schemas/` 中的 schema 与共享类型定义（schema 库按前端栈选型，React 栈常用 Zod，Vue 栈可用 Valibot 等同类方案）
- **.NET 边界事实来源**：`src-dotnet/` 中的 DTO / IPC 入参出参定义
- **定义方式**：表单 schema、Query schema、IPC 入参 schema、IPC 出参结构必须成对维护
- **变更规则**：字段或行为变化时，必须同步更新 schema、类型、页面、IPC 封装与测试
- **生成式契约**：若 sidecar 以路由表和 DTO 声明 method，应以其为跨语言契约事实来源，在构建期生成 TypeScript method 常量、类型和基础 SDK；生成文件不作为手工维护入口。
- **流式契约**：流式 method 要记录最终响应、进度 notification、requestId 关联键、取消 method、总时长、空闲时长与审批等待期间的计时策略。
- **调用选择原则**：
  - 桌面应用内部业务交互：优先 IPC handler（stdio JSON-RPC）
  - 事件通知或后台状态推送：使用 Electron 主进程向渲染进程的推送
  - 外部服务访问：使用前端统一请求层或 .NET 侧 `Integrations/`，不新增本地 HTTP 桥接

---

<!-- include: shared/doc/design-system-file.md -->

---

<!-- include: shared/doc/doc-update-desktop.md -->

<!-- include: shared/doc/doc-directory.md -->

<!-- include: shared/doc/requirement.md -->

<!-- include: shared/doc/retro-format.md -->

---
