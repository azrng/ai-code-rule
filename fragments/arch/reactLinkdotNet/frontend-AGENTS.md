---
rule_id: frontend-agents
version: 1.22.0
last_updated: 2026-11-15
dependencies: [agents-root]
---

# 前端规则

<!-- include: shared/frontend/scope.md -->

---

## 技术栈

<!-- include: shared/frontend/stack-react-tailwind.md -->

### 设计系统
<!-- include: shared/ui/design-system-liveness.md -->
- 使用 Tailwind CSS 4 + shadcn/ui 体系：所有视觉规范在 `design-system.yaml` 中定义，落地为 `src/styles/globals.css` 的 `@theme` token 与 shadcn CSS 变量
- 优先复用 `src/components/ui/` 下 shadcn 组件与 Radix 能力，再考虑自定义组件；禁止 inline style，禁止硬编码颜色/间距/圆角

<!-- include: shared/ui/spec-lock.md -->

<!-- include: shared/application/tech-choice.md -->

---

<!-- include: shared/frontend/advisory.md -->

## 推荐目录结构

- 前端目录建议聚焦在 `frontend/` 下组织，优先复用现有结构，不强制迁移。

```text
frontend/
├── public/
├── src/
│   ├── api/               # 按业务域拆分的请求模块
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
├── Dockerfile
├── package.json
├── vite.config.ts
└── tsconfig.json
```

---

### 阶段 1 — 前端实现（前端实现角色主导）

**入场要求**：阶段 0 设计文档已由用户确认；本次改动命中原型跳过条件时直接实现，否则沉淀型真栈原型在阶段 1 内实现，其用户确认是阶段 1 的首个门控（确认时点见「原型设计与确认」）

<!-- include: shared/ui/prototype-workflow.md -->

<!-- include: shared/ui/mock-boundary.md -->

**工作内容**：
1. 按设计文档实现页面和组件，遵循 `design-system.yaml` 和 Tailwind + shadcn/ui 规范
2. 数据层使用 mock（静态 mock 数据），不依赖真实后端接口
3. 同步输出接口说明文档，供后端开发参考

**产物**：
- 可在浏览器中运行的前端页面
- 接口需求说明（用于后端开发）

**门控规则**：
- 用户确认前端页面符合设计文档预期
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

---
