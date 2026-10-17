---
rule_id: frontend-agents
version: 1.41.0
last_updated: 2026-09-02
dependencies: [agents-root]
---

# 前端规则

<!-- include: shared/frontend/scope.md -->

<!-- include: shared/frontend/common-tasks.md -->

---

## 技术栈

<!-- include: shared/frontend/stack-react-antd.md -->
- Axios 仅用于第三方 / 远端 API，同站数据无后端可调

### 数据来源
<!-- include: shared/frontend/no-backend-data.md -->

### 设计系统
<!-- include: shared/ui/design-system-liveness.md -->
- 使用 Ant Design 主题系统（ConfigProvider + Design Token），所有视觉规范在 `design-system.yaml` 中定义
- 优先使用 Ant Design 组件和样式，禁止 inline style，禁止硬编码颜色/间距/圆角

<!-- include: shared/ui/spec-lock.md -->

<!-- include: shared/application/tech-choice.md -->

---

## 阶段 1 — 页面与交互实现

**触发条件**：用户发出“开始前端开发”或等价指令

**入场要求**：阶段 0 设计文档已确认；原型已经用户确认，或本次改动命中原型跳过条件

HTML 原型即视觉基线，实现需逐页对照原型。

<!-- include: shared/ui/prototype-workflow.md -->

**工作内容**：
1. 基于 React + Vite 实现页面、布局、组件和导航结构。
2. 先搭建页面骨架、数据状态和交互闭环，再补细节优化。
3. 数据层按无后端口径执行：本地静态 / mock 数据、本地存储或第三方 / 远端 API，页面交互必须完整可演示。
4. 同步产出数据口径说明：字段定义、筛选条件、分页参数、表单校验规则。

**门控规则**：
- 用户确认页面符合设计文档预期。
- 页面交互逻辑经手工 smoke test 验证通过。
- 数据口径、字段说明和类型定义已初步定稳；后续发现缺口时按 `collaboration-AGENTS.md`「纠错、回退与重构」处理。
- 视觉验收按「视觉验收」小节执行，用户终审通过前不得进入阶段 2。

<!-- include: shared/ui/visual-acceptance.md -->

---

<!-- include: shared/frontend/advisory.md -->

## 推荐目录结构

若仓库尚未形成稳定结构，可优先参考以下组织方式；若仓库已有实现，以现状为准，不强制迁移。

```text
project-root/
├── public/
├── src/
│   ├── api/               # 按业务域拆分的请求模块（第三方 / 远端 API）
│   ├── components/
│   │   ├── common/        # 通用业务组件
│   │   └── layout/        # 布局组件
│   ├── pages/             # 页面级组件（按业务域拆分）
│   ├── router/
│   │   ├── index.tsx      # 路由配置
│   │   └── routes.tsx     # 路由定义
│   ├── stores/            # Zustand stores（按业务域拆分）
│   ├── hooks/             # 自定义 Hooks（useXxx）
│   ├── types/             # TypeScript 类型定义
│   ├── utils/             # 纯工具函数
│   ├── config/
│   │   └── antd-theme.ts  # Ant Design 主题配置
│   ├── App.tsx
│   └── main.tsx
├── index.html
├── package.json
├── vite.config.ts
└── tsconfig.json
```

---

## 前端规则

<!-- include: shared/frontend/react-hooks.md -->

<!-- include: shared/frontend/component-communication-react.md -->

<!-- include: shared/frontend/routing-management-react.md -->

<!-- include: shared/frontend/zustand.md -->

### 样式规则
- 优先使用 Ant Design 组件内置样式和 Design Token
- 自定义样式使用 CSS Modules 或全局 CSS，变量来自 `design-system.yaml`
- 禁止 inline style，禁止硬编码颜色值（如 `#1677ff`）
- 主题定制统一通过 `ConfigProvider` 的 `theme` 属性配置
- 响应式设计遵循移动优先原则，使用 Ant Design 的 Grid 系统（Row / Col）；存量样式文件已是桌面优先写法（max-width 查询）时，增量改动沿用该文件既有约定，不整体重写扩大 diff，新增文件仍遵循移动优先

<!-- include: shared/frontend/component-rules.md -->

<!-- include: shared/frontend/request-rules.md -->

<!-- include: shared/frontend/code-organization.md -->

---

## 测试规则

<!-- include: shared/frontend/pre-commit-regression.md -->

<!-- include: shared/common/general-requirements.md -->

<!-- include: shared/frontend/frontend-testing-minimal.md -->

<!-- include: shared/external-dependencies.md -->

<!-- include: shared/no-test-fallback.md -->

---
