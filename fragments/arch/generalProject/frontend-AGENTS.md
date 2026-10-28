---
rule_id: frontend-agents
version: 1.17.0
last_updated: 2026-08-30
dependencies: [agents-root]
---

# 前端规则

## 适用范围

- 作用域：界面、页面、视图、组件、交互、样式与前端测试
- 触发场景：涉及任何用户可见界面、前端交互、客户端状态、组件拆分、界面回归时阅读

---

## 技术栈

<!-- include: shared/frontend/general-principles.md -->

### 设计系统
<!-- include: shared/ui/design-system-liveness.md -->
- 所有视觉规范以 `design-system.yaml` 为唯一事实来源
- 优先复用项目已有组件、布局、样式变量与交互模式
- 禁止在无约束情况下随意硬编码颜色、间距、圆角、阴影、断点；若现有技术栈无法完全避免，必须优先抽成可复用 token 或常量

<!-- include: shared/ui/spec-lock.md -->

<!-- include: shared/application/tech-choice.md -->

---

<!-- include: shared/frontend/advisory.md -->

---

## 阶段 1 — 页面与交互实现

**入场要求**：阶段 0 设计文档已确认；原型已经用户确认，或本次改动命中原型跳过条件

HTML 原型即视觉基线，实现需逐页对照原型。

<!-- include: shared/ui/prototype-workflow.md -->

**工作内容**：
1. 基于当前项目使用的界面技术实现页面、布局、组件和导航结构。
2. 先搭建页面骨架、数据状态和交互闭环，再补细节优化。
3. 阶段内允许使用 mock 数据、假接口或静态数据文件，但页面交互必须完整可演示。
4. 同步产出契约说明：字段定义、筛选条件、分页参数、表单校验规则、权限要求。

**门控规则**：
- 用户确认页面符合设计文档预期。
- 页面交互逻辑经手工 smoke test 验证通过。
- 显示层契约、字段说明和 schema / 类型定义已初步定稳；后续发现缺口时按 `collaboration-AGENTS.md`「纠错、回退与重构」处理。
- 视觉验收按「视觉验收」小节执行，用户终审通过前不得进入阶段 2。

<!-- include: shared/ui/visual-acceptance.md -->

---

<!-- include: shared/frontend/component-structure.md -->

### 状态与数据流规则
- 服务端主数据源不要长期塞进纯客户端全局状态中
- 优先沿用项目已有的数据请求、缓存和失效策略
- 同一份数据只保留一个主要事实来源，避免页面本地状态、全局状态、缓存层互相打架
- 写操作成功后必须刷新受影响视图，禁止出现“提交成功但页面没更新”的伪完成状态

<!-- include: shared/frontend/component-behavior.md -->

### 请求与前端数据获取规则
- 客户端请求应通过项目统一封装、统一网关地址或现有调用方式发起，不要在页面里散落硬编码环境地址
- 前端只负责消费服务端提供的数据和权限结果，不在客户端补造安全边界
- 若当前技术栈支持服务端渲染、预取或本地缓存，优先沿用现有方案，不做无意义绕路请求

<!-- include: shared/frontend/code-organization.md -->

---

<!-- include: shared/frontend/test-guidance.md -->

---
