---
rule_id: doc-agents
version: 1.15.0
last_updated: 2026-08-27
dependencies: [agents-root]
---

# 文档规则

<!-- include: shared/doc/scope-v1.md -->

<!-- include: shared/doc/common-tasks.md -->

---

<!-- include: shared/doc/advisory.md -->

---

## 阶段 0 设计文档

### 阶段 0 — 设计文档（新功能强制前置）

**触发条件**：用户提出新功能需求

**执行方式**：由用户与 AI 对话协作产出，用户最终确认定稿

**产物**：`doc/design/设计文档.md`（全项目一份，新功能以新章节追加，不新建文件）

**文档必须包含以下四个部分**：

| 部分 | 内容要求 |
| ---- | -------- |
| 架构设计 | 模块划分、页面与服务关系、涉及的技术决策说明 |
| 功能需求 | 页面列表、功能点明细、业务规则、权限矩阵 |
| 界面原型 | 每个页面的 ASCII 线框图，含关键状态（loading / empty / error） |
| 交互说明 | 操作流程、状态流转、边界场景、错误处理方式 |

<!-- include: shared/doc/design-checklist.md -->

---

## 接口契约规范

### 契约要求

- **唯一来源**：Blazor 服务层 DTO、独立 API 接口定义与相关请求 / 响应模型。
- **验证方式**：页面调用链路和 Swagger（如提供独立 API）共同用于验证契约是否正确。
- **变更规则**：接口契约变更时，必须先修改 DTO 或接口定义，再同步修改页面调用、服务实现、测试与相关文档。
- **文件结构建议**：按业务域拆分 DTO，避免将全部请求和响应堆叠在单个文件中。

---

## 设计系统文件规范
`design-system.yaml` 是唯一设计系统文件，包含：
- 品牌与产品风格意图
- 语义化颜色 token
- 排版 token
- 间距、圆角、阴影 token
- 布局与断点规则
- BootstrapBlazor 主题配置映射
- 页面状态规范
- 组件使用规则
- 明确的 `do / dont` 规则

---

<!-- include: shared/doc/doc-update.md -->

<!-- include: shared/doc/doc-directory.md -->

<!-- include: shared/doc/requirement.md -->

<!-- include: shared/doc/retro-format-v1.md -->

---
