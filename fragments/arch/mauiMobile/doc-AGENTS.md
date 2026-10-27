---
rule_id: doc-agents
version: 1.43.0
last_updated: 2026-09-03
dependencies: [agents-root]
---

# 文档规则

<!-- include: shared/doc/scope.md -->

---

<!-- include: shared/doc/advisory-dto.md -->

---

## 阶段 0 设计文档

<!-- include: shared/doc/stage0-head.md -->

**文档必须包含以下四个部分**：

| 部分 | 内容要求 |
| --- | --- |
| 需求追溯 | 关联 `REQ-` 编号、明确本次设计覆盖的范围和未覆盖项，不重复粘贴需求正文 |
| 技术设计 | 模块职责、数据流向、平台差异和技术决策说明 |
| 界面原型 | 每个页面的 ASCII 线框图，含关键状态（loading / empty / error），需标注移动端布局差异；原型列出的页面是原型阶段的输入范围 |
| 交互与异常 | 操作流程、状态流转、边界场景、错误处理方式和平台差异说明 |

<!-- include: shared/doc/stage0-gate.md -->

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
- API 地址、平台权限声明、最低版本要求
<!-- include: shared/doc/doc-update-core.md -->

优先更新已有文档，不新增重复说明。

<!-- include: shared/doc/doc-directory.md -->

<!-- include: shared/doc/requirement.md -->

<!-- include: shared/doc/retro-format.md -->

---
