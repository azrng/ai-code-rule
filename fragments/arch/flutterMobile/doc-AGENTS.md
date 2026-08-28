---
rule_id: doc-agents
version: 1.14.0
last_updated: 2026-08-27
dependencies: [agents-root]
---

# 文档规则

<!-- include: shared/doc/scope-v1.md -->

### 常见任务入口
- 新增功能方案：先看阶段 0 设计文档
- 更新字段、接口、Model 或 schema：先看契约说明规范
- 更新设计系统或通用说明：先看设计系统文件规范与文档目录约定

---

<!-- include: shared/doc/advisory.md -->

---

<!-- include: shared/doc/stage0-design-v2.md -->

---

### 接口契约规范

- **唯一来源**：`lib/models/` 下的数据模型定义
- **定义时机**：阶段 0 设计文档确认后，阶段 1 开始前
- **变更规则**：模型字段变更时，必须同步修改 Provider、Service、Repository 及相关测试
- **文件结构建议**：按业务域拆分模型定义文件，使用 freezed 生成不可变数据类

---

## 设计系统文件规范
`design-system.yaml` 是唯一设计系统文件，包含：
- 品牌与产品风格意图
- 语义化颜色 token（Material 3 ColorScheme 映射）
- 排版 token
- 间距、圆角、阴影 token
- 动效规范（页面转场、列表动画、交互反馈）
- 移动端模式（导航、列表、表单、反馈）
- 页面状态规范（loading / empty / error / no-permission）
- 组件使用规则
- 响应式设计规则
- 明确的 `do / dont` 规则

---

<!-- include: shared/doc/doc-update.md -->

<!-- include: shared/doc/doc-directory-v1.md -->

### requirement 要求
- 若 `doc/requirement.md` 为模板初始状态，AI 应根据用户输入将占位内容替换为项目真实需求，而不是长期保留"待补充"空壳。
- 需求文档建议固定包含以下章节：项目背景、目标与范围、用户角色、页面或模块清单、核心业务流程、关键数据与字段、权限与可见范围、边界场景与异常、非功能要求、待确认项。
- 自动生成需求文档时，优先写清业务事实、流程与边界，不提前写入技术实现方案。

<!-- include: shared/doc/retro-format-v1.md -->
