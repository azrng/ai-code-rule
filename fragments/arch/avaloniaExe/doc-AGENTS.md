---
rule_id: doc-agents
version: 1.14.0
last_updated: 2026-08-27
dependencies: [agents-root]
---

# 文档规则

<!-- include: shared/doc/scope-v1.md -->

<!-- include: shared/doc/common-tasks.md -->

---

<!-- include: shared/doc/advisory-dto.md -->

---

## 阶段 0 设计文档

### 阶段 0 — 设计文档（新功能强制前置）

**触发条件**：用户提出新功能需求

**执行方式**：由用户与 AI 对话协作产出，用户最终确认定稿

- 若 `doc/requirement.md` 不存在，AI 必须先根据用户当前输入自动生成一版详细需求文档初稿，再进入设计文档整理。
- 自动生成的需求文档至少应包含：业务目标、用户角色、页面 / 模块范围、核心流程、关键字段、权限规则、边界场景、已知约束。
- 若用户输入不足以支撑完整需求，AI 必须显式写出“待确认项”与“当前假设”，可以先形成初稿，但不得将假设内容表述为已确认事实。

**产物**：`doc/design/设计文档.md`（全项目一份，新功能以新章节追加，不新建文件）

**文档必须包含以下四个部分**：

| 部分 | 内容要求 |
| ---- | -------- |
| 架构设计 | 模块划分、数据流向、涉及的技术决策说明 |
| 功能需求 | 页面列表、功能点明细、业务规则、关键状态说明 |
| 界面原型 | 每个页面的 ASCII 线框图，含关键状态（loading / empty / error） |
| 交互说明 | 操作流程、状态流转、边界场景、错误处理方式 |

<!-- include: shared/doc/design-checklist.md -->

---

## DTO 契约规范

### 契约要求

- **唯一来源**：`src/AppName/Models/DTOs/` 中最终定稿的 DTO / 契约类。
- **定义时机**：阶段 1 视图实现时同步产出，阶段 2 严格按其实现业务逻辑。
- **变更规则**：DTO 变更时，必须先修改契约类，再同步修改 ViewModel、Service、Repository 映射和相关文档。
- **文件结构建议**：按业务域拆分 DTO，避免单文件堆叠所有契约。
- **回退规则**：后续阶段发现 DTO 缺口时，按根 `AGENTS.md` 的纠错与回退机制回到契约变更流程，不在实现层绕过契约。

---

## 设计系统文件规范
`design-system.yaml` 是唯一设计系统文件，包含：
- 品牌与产品风格意图
- 语义化颜色 token
- 排版 token
- 间距、圆角、阴影 token
- 布局与断点规则
- 页面状态规范
- 组件使用规则
- 明确的 `do / dont` 规则

---

<!-- include: shared/doc/doc-update.md -->

<!-- include: shared/doc/doc-directory-v1.md -->

<!-- include: shared/doc/requirement.md -->

<!-- include: shared/doc/retro-format-v1.md -->

---
