---
rule_id: doc-agents
version: 1.19.0
last_updated: 2026-08-31
dependencies: [agents-root]
---

# 文档规则

<!-- include: shared/doc/scope.md -->

---

<!-- include: shared/doc/advisory.md -->

---

## 阶段 0 设计文档

<!-- include: shared/doc/stage0-design.md -->

---

## 设计系统文件规范
`design-system.yaml` 是唯一设计系统文件，包含：
- 品牌与产品风格意图
- 语义化颜色 token
- 排版 token
- 间距、圆角、阴影 token
- 布局规则
- Ant Design 主题配置映射
- 页面状态规范
- 组件使用规则
- 明确的 `do / dont` 规则

---

<!-- include: shared/doc/doc-update.md -->

<!-- include: shared/doc/doc-directory.md -->

### requirement 要求
- 若 `doc/requirement.md` 为模板初始状态，AI 应根据用户输入将占位内容替换为项目真实需求，而不是长期保留”待补充”空壳。
- 需求文档建议固定包含以下章节：项目背景、目标与范围、用户角色、页面或模块清单、核心业务流程、关键数据与字段、权限与可见范围、边界场景与异常、非功能要求、待确认项。
- 自动生成需求文档时，优先写清业务事实、流程与边界，不提前写入技术实现方案。

### 回顾分析记录格式
- 适用场景：同类错误因规则缺口连续两次阻塞交付，或跨任务重复出现时，触发简短回顾分析时使用
- 内容保持简短，建议至少包含：
  - 错误现象
  - 触发场景
  - 根因
  - 本次补充或修改的规则
  - 后续如何避免或验证不再复发
