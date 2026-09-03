---
rule_id: doc-agents
version: 1.43.0
last_updated: 2026-09-03
dependencies: [agents-root]
---

# 文档规则

<!-- include: shared/doc/scope.md -->

<!-- include: shared/doc/common-tasks.md -->

---

<!-- include: shared/doc/advisory-dto.md -->

---

## 阶段 0 设计文档

### 阶段 0 — 设计文档（新功能强制前置）

**触发条件**：用户提出新功能需求

**执行方式**：由用户与 AI 对话协作产出，用户最终确认定稿

<!-- include: shared/doc/requirement-autogen.md -->

**产物**：当前业务模块/功能对应的 `doc/design/<module>/<feature>.md`，并关联已确认的 `doc/requirements/<module>/<feature>.md`；跨模块方案另记入 `doc/design/architecture.md`

**文档必须包含以下四个部分**：

| 部分 | 内容要求 |
| --- | --- |
| 需求追溯 | 关联 `REQ-` 编号、明确本次设计覆盖的范围和未覆盖项，不重复粘贴需求正文 |
| 技术设计 | 模块职责、数据流向、平台差异和技术决策说明 |
| 界面原型 | 每个页面的 ASCII 线框图，含关键状态（loading / empty / error），需标注移动端布局差异 |
| 交互与异常 | 操作流程、状态流转、边界场景、错误处理方式和平台差异说明 |

**门控规则**：
- 用户明确确认对应的需求文档和设计文档后，才允许进入阶段 1
- AI 在此阶段只输出文档内容，不写任何实现代码

<!-- include: shared/doc/design-checklist.md -->

---

## DTO 契约规范

<!-- include: shared/desktop/dto-contract.md -->

---

## 设计系统文件规范
`design-system.yaml` 是唯一设计系统文件，包含：
- 品牌与产品风格意图
- 语义化颜色 token
- 排版 token
- 间距、圆角、阴影 token
- 移动端布局规则（安全区域、触摸目标、适配断点）
- 页面状态规范
- 组件使用规则
- 明确的 `do / dont` 规则

---

## 文档更新要求
以下内容发生变化时，必须同步更新相关文档：
<!-- include: shared/doc/doc-update-core.md -->
- API 地址、平台权限声明、最低版本要求

优先更新已有文档，不新增重复说明。

<!-- include: shared/doc/doc-directory.md -->

<!-- include: shared/doc/requirement.md -->

<!-- include: shared/doc/retro-format.md -->

---
