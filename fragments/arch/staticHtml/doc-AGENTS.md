---
rule_id: doc-agents
version: 1.15.0
last_updated: 2026-08-28
dependencies: [agents-root]
---

# 文档规则

<!-- include: shared/doc/scope-v1.md -->

<!-- include: shared/doc/common-tasks.md -->

---

<!-- include: shared/doc/stage0-design-v4.md -->

---

## 设计系统文件规范
`design-system.yaml` 是唯一设计系统文件，包含：
- 品牌与产品风格意图
- 语义化颜色 token
- 排版 token
- 间距、圆角、阴影 token
- 布局与断点规则
- Bootstrap 主题配置映射
- 页面状态规范
- 组件使用规则
- 明确的 `do / dont` 规则

---

## 主动建议规则
- 发现需求描述缺少角色、流程、页面结构、交互说明、验收标准或非功能要求时，应主动补充为待确认项
- 发现实现方案与设计文档、需求基准或设计系统不一致时，应主动提醒并建议同步修订
- 维护文档时优先更新已有事实来源，不新增平行说明；确需新增文件时，应说明新增原因和归档位置
- 对不确定的业务事实和页面运行方式，只能标记为待确认或引用已验证来源，禁止写成确定结论

---

<!-- include: shared/doc/doc-update.md -->

<!-- include: shared/doc/doc-directory.md -->

### requirement 要求
- 若 `doc/requirement.md` 为模板初始状态，AI 应根据用户输入将占位内容替换为项目真实需求，而不是长期保留”待补充”空壳。
- 需求文档建议固定包含以下章节：项目背景、目标与范围、用户角色、页面或模块清单、核心业务流程、关键数据与字段、权限与可见范围、边界场景与异常、非功能要求、待确认项。
- 自动生成需求文档时，优先写清业务事实、流程与边界，不提前写入技术实现方案。

<!-- include: shared/doc/retro-format-v1.md -->
