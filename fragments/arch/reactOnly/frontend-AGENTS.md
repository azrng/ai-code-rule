---
rule_id: frontend-agents
version: 1.42.0
last_updated: 2026-11-15
dependencies: [agents-root]
---

# 前端规则

<!-- include: shared/frontend/scope.md -->

---

## 技术栈

<!-- include: shared/frontend/stack-react-tailwind.md -->
- Axios 仅用于第三方 / 远端 API，同站数据无后端可调

### 数据来源
<!-- include: shared/frontend/no-backend-data.md -->

### 设计系统
<!-- include: shared/ui/design-system-liveness.md -->
- 使用 Tailwind CSS 4 + shadcn/ui 体系：所有视觉规范在 `design-system.yaml` 中定义，落地为 `src/styles/globals.css` 的 `@theme` token 与 shadcn CSS 变量
- 优先复用 `src/components/ui/` 下 shadcn 组件与 Radix 能力，再考虑自定义组件；禁止 inline style，禁止硬编码颜色/间距/圆角

<!-- include: shared/ui/spec-lock.md -->

<!-- include: shared/application/tech-choice.md -->

---

## 阶段 1 — 页面与交互实现

**入场要求**：阶段 0 设计文档已确认；本次改动命中原型跳过条件时直接实现，否则沉淀型真栈原型在阶段 1 内实现，其用户确认是阶段 1 的首个门控（确认时点见「原型设计与确认」）

<!-- include: shared/ui/prototype-workflow.md -->

<!-- include: shared/ui/mock-boundary.md -->

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

<!-- include: shared/common/structure-preamble.md -->

```text
project-root/
├── public/
├── src/
│   ├── api/               # 按业务域拆分的请求模块（第三方 / 远端 API）
│   ├── components/
│   │   ├── ui/            # shadcn/ui 组件（复制进来，可定制）
│   │   ├── common/        # 通用业务组件
│   │   └── layout/        # 布局组件
│   ├── pages/             # 页面级组件（按业务域拆分）
│   ├── router/
│   │   ├── index.tsx      # 路由配置
│   │   └── routes.tsx     # 路由定义
│   ├── stores/            # Zustand stores（按业务域拆分）
│   ├── hooks/             # 自定义 Hooks（useXxx）
│   ├── types/             # TypeScript 类型定义
│   ├── utils/             # 纯工具函数（含 cn 类名合并）
│   ├── styles/
│   │   └── globals.css    # Tailwind CSS 4 @theme + shadcn CSS 变量
│   ├── App.tsx
│   └── main.tsx
├── components.json        # shadcn 配置
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
- 唯一样式引擎为 Tailwind CSS 4；颜色、间距、圆角、阴影统一使用 Tailwind 工具类或 shadcn CSS 变量，禁止硬编码
- 组件变体统一用 `class-variance-authority`，类名合并统一用 `cn()`（clsx + tailwind-merge）
- 确需全局或复杂样式时写进 `globals.css`，变量来自 `design-system.yaml`
- 禁止 inline style，禁止硬编码颜色值（如 `#1677ff`）
- 主题定制统一通过 `globals.css` 的 `@theme` token 与 shadcn CSS 变量配置
- 响应式设计遵循移动优先原则，使用 Tailwind 断点（`sm:` / `md:` / `lg:` ...）；存量样式文件已是桌面优先写法（max-width 查询）时，增量改动沿用该文件既有约定，不整体重写扩大 diff，新增文件仍遵循移动优先

<!-- include: shared/frontend/component-rules.md -->

<!-- include: shared/frontend/request-rules.md -->

<!-- include: shared/frontend/code-organization.md -->

---

## 测试规则

<!-- include: shared/frontend/pre-commit-regression.md -->

<!-- include: shared/frontend/general-requirements.md -->

<!-- include: shared/frontend/frontend-testing-minimal.md -->

<!-- include: shared/external-dependencies.md -->

<!-- include: shared/no-test-fallback.md -->

<!-- include: shared/application/anti-patterns.md -->

---
