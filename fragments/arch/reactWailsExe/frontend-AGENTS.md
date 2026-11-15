---
rule_id: frontend-agents
version: 1.35.0
last_updated: 2026-11-15
dependencies: [agents-root]
---

# 前端规则

## 适用范围

- 作用域：界面、视图、页面、组件、交互、样式与前端测试
- 触发场景：涉及任何用户可见界面、前端交互、客户端状态、组件拆分、界面回归时阅读

---

## 技术栈

<!-- include: shared/frontend/stack-react-tailwind.md -->
- 图表需求出现时优先选 ECharts + echarts-for-react，主题与使用规则见 `design-system.yaml`
<!-- include: shared/skeletons/wails/frontend-binding.md -->

### 设计系统
<!-- include: shared/ui/design-system-liveness.md -->
- 使用 Tailwind CSS 4 + shadcn/ui 体系：所有视觉规范在 `design-system.yaml` 中定义，落地为 `src/styles/globals.css` 的 `@theme` token 与 shadcn CSS 变量
- 优先复用 `src/components/ui/` 下 shadcn 组件与 Radix 能力，再考虑自定义组件；禁止 inline style，禁止硬编码颜色/间距/圆角

<!-- include: shared/ui/spec-lock.md -->

<!-- include: shared/application/tech-choice.md -->

---

<!-- include: shared/frontend/advisory.md -->

---

## 推荐目录结构
若仓库尚未形成稳定结构，可优先参考以下组织方式；若仓库已有实现，以现状为准，不强制迁移。

```text
project-root/
├── frontend/
│   ├── src/
│   │   ├── api/               # 外部 API 请求模块（仅用于外部服务调用）
│   │   ├── components/
│   │   │   ├── ui/            # shadcn/ui 组件（复制进来，可定制）
│   │   │   ├── common/        # 通用业务组件
│   │   │   └── layout/        # 布局组件
│   │   ├── pages/             # 页面级组件（按业务域拆分）
│   │   ├── router/
│   │   │   ├── index.tsx       # 路由配置
│   │   │   └── routes.tsx      # 路由定义
│   │   ├── stores/            # Zustand stores（按业务域拆分）
│   │   ├── hooks/             # 自定义 Hooks（useXxx）
│   │   ├── types/             # TypeScript 类型定义
│   │   ├── utils/             # 纯工具函数（含 cn 类名合并）
│   │   ├── bindings/          # Wails 自动生成的 Go 绑定类型声明
│   │   ├── styles/
│   │   │   ├── globals.css     # Tailwind CSS 4 @theme + shadcn CSS 变量
│   │   │   └── echarts-theme.ts # ECharts 图表主题配置（仅引入图表时）
│   │   ├── App.tsx
│   │   └── main.tsx
│   ├── package.json
│   ├── vite.config.ts
│   └── tsconfig.json
├── doc/
│   ├── design/
│   └── requirement.md
├── build/                     # Wails 构建配置
│   └── windows/               # Windows 图标等资源
├── wails.json                 # Wails 项目配置
├── .gitignore
├── AGENTS.md
├── CLAUDE.md
├── GEMINI.md
├── GROK.md
├── design-system.yaml
└── TASK.md
```

---

### 阶段 1 — 界面与交互实现

**入场要求**：阶段 0 设计文档已确认；本次改动命中原型跳过条件时直接实现，否则沉淀型真栈原型在阶段 1 内实现，其用户确认是阶段 1 的首个门控（确认时点见「原型设计与确认」）

<!-- include: shared/ui/prototype-workflow.md -->

<!-- include: shared/ui/mock-boundary.md -->

**工作内容**：
1. 按设计文档实现页面和组件，遵循 `design-system.yaml` 和 Tailwind + shadcn/ui 规范
2. 数据层使用 mock（静态 mock 数据），不依赖真实 Go 后端绑定
3. 同步输出接口需求说明，供 Go 后端开发参考

**产物**：见「Wails 前端调用纪律」的阶段 1 产物口径

**门控规则**：
- 用户确认视图页面符合设计文档预期
- 前端页面交互逻辑已验证通过
- 视觉验收按「视觉验收」小节执行，用户终审通过前不得进入阶段 2。
- 满足以上三点后，才允许进入阶段 2

<!-- include: shared/ui/visual-acceptance.md -->

---

## 前端规则

<!-- include: shared/frontend/react-hooks.md -->

<!-- include: shared/frontend/component-communication-react.md -->

<!-- include: shared/frontend/routing-management-react.md -->

<!-- include: shared/frontend/zustand.md -->

<!-- include: shared/frontend/wails-binding-call.md -->

### 样式规则
- 唯一样式引擎为 Tailwind CSS 4；颜色、间距、圆角、阴影统一使用 Tailwind 工具类或 shadcn CSS 变量，禁止硬编码
- 组件变体统一用 `class-variance-authority`，类名合并统一用 `cn()`（clsx + tailwind-merge）
- 确需全局或复杂样式时写进 `globals.css`，变量来自 `design-system.yaml`
- 禁止 inline style，禁止硬编码颜色值（如 `#1677ff`）
- 主题定制统一通过 `globals.css` 的 `@theme` token 与 shadcn CSS 变量配置
- 桌面窗口口径见「Wails 前端调用纪律」

<!-- include: shared/frontend/component-rules.md -->

<!-- include: shared/frontend/external-api-rules.md -->

<!-- include: shared/frontend/code-organization.md -->

---

## 测试规则

<!-- include: shared/frontend/pre-commit-regression.md -->

<!-- include: shared/backend/general-requirements.md -->

<!-- include: shared/frontend/frontend-testing-minimal.md -->

<!-- include: shared/no-test-fallback.md -->

<!-- include: shared/application/anti-patterns.md -->

---
