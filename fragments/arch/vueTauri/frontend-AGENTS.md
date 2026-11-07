---
rule_id: frontend-agents
version: 1.54.0
last_updated: 2026-09-06
dependencies: [agents-root]
---

# 前端规则

## 适用范围

- 作用域：前端实现、页面、组件、路由、状态、样式与前端测试
- 触发场景：涉及 Vue 页面、布局、组件、交互、样式、前端 smoke test 时阅读

---

## 技术栈

### 前端
- Vue 3 + TypeScript + Vite
- Tailwind CSS（唯一样式体系）
- Radix Vue（reka-ui）Primitive + `class-variance-authority` + `clsx` / `tailwind-merge`（推荐的组件基础设施）
- Vue Router 4（页面与导航）
- TanStack Query for Vue（异步数据状态）
- Pinia（仅用于客户端跨组件共享状态）
- VeeValidate + Zod（表单与校验）
- `@tauri-apps/api`（与 Tauri Runtime 通信）

<!-- include: shared/frontend/testing.md -->

<!-- include: shared/frontend/design-system-usage.md -->
<!-- include: shared/ui/design-system-liveness.md -->

<!-- include: shared/ui/spec-lock.md -->

<!-- include: shared/frontend/advisory.md -->

---

## 推荐目录结构

若仓库尚未形成稳定结构，可优先参考以下组织方式；若仓库已有实现，以现状为准，不强制迁移。

```text
src/
├── app/                      # 路由与应用级入口
├── components/
│   ├── ui/                   # 基于 Radix Vue / cva 的基础组件
│   ├── layout/               # 布局组件
│   └── business/             # 业务组件
├── features/                 # 按业务域收敛
├── composables/              # 组合式函数
├── router/                   # 路由定义
├── lib/
│   ├── tauri/                # Tauri API 封装
│   ├── query/                # TanStack Query 封装
│   └── utils/                # 工具函数
├── stores/                   # Pinia store
├── schemas/                  # Zod schema
├── styles/                   # 全局样式与 token 映射
├── types/                    # TypeScript 类型
├── tests/                    # 前端测试辅助
├── main.ts
└── App.vue
```

---

## 阶段 1 — 前端实现

**入场要求**：阶段 0 设计文档已确认；原型已经用户确认，或本次改动命中原型跳过条件

HTML 原型即视觉基线，实现需逐页对照原型。

<!-- include: shared/ui/prototype-workflow.md -->

**工作内容**：
1. 基于 Vue 3 + Vite 实现页面、布局、组件与导航结构。
2. 页面内允许先用 mock 数据、假 Command 响应或本地 fixture 演示交互。
3. 同步产出前端契约说明：字段定义、筛选条件、分页参数、表单校验规则、权限要求。
4. 只要仓库未明确采用别的组件体系，优先走“Tailwind + Radix Vue / shadcn-vue 风格组件”的收敛路线。

**门控规则**：
- 用户确认页面符合设计文档预期。
- 页面交互逻辑经手工 smoke test 或自动化验证通过。
- 视觉验收按「视觉验收」小节执行，用户终审通过前不得进入阶段 2。

<!-- include: shared/ui/visual-acceptance.md -->

---

## 前端规则

<!-- include: shared/frontend/composition-api.md -->

<!-- include: shared/frontend/component-communication-vue.md -->

<!-- include: shared/frontend/routing-management-vue.md -->

<!-- include: shared/frontend/pinia.md -->

<!-- include: shared/frontend/desktop-query-state.md -->

<!-- include: shared/skeletons/tauri/frontend-invoke.md -->

### 表单规则
- 表单优先使用 VeeValidate + `schemas/` 中的 Zod schema 校验
- 字段校验以 schema 为单一事实来源，不在模板与组件里散落重复校验逻辑
- 提交按钮必须有 loading 与防重复提交

<!-- include: shared/frontend/styling.md -->

<!-- include: shared/frontend/component-rules.md -->

<!-- include: shared/frontend/external-request-desktop.md -->

<!-- include: shared/frontend/code-organization.md -->

---

## 测试规则

<!-- include: shared/frontend/pre-commit-regression.md -->

<!-- include: shared/frontend/frontend-testing-desktop.md -->

<!-- include: shared/no-test-fallback.md -->

<!-- include: shared/application/anti-patterns.md -->

---
