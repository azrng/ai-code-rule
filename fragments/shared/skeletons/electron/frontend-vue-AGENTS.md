---
rule_id: frontend-agents
version: 1.39.0
last_updated: 2026-09-02
dependencies: [agents-root]
---

# 前端规则

## 适用范围

- 作用域：前端实现、页面、组件、路由、状态、样式与前端测试
- 触发场景：涉及 Electron 渲染进程页面、布局、组件、交互、样式、前端 smoke test 时阅读

### 常见任务入口
- 新增页面或布局：先看 Vue 组合式规范、路由与组件规则
- 改表单、筛选、交互：先看表单规则、状态管理规则
- 改样式、主题、组件展示：先看样式规则与设计系统
- 改前后端 IPC 调用：先看 IPC 前端调用规范
- 补前端回归：先看 `提交前最小回归` 与测试规则

---

## 技术栈

### 前端
- Electron（渲染进程）+ Vue 3 + TypeScript + Vite
- Element Plus 组件库（唯一组件体系）
- Element Plus Icons（唯一图标库，尺寸统一 18-24px）
- Vue Router 4（页面与导航）
- Pinia（仅用于客户端跨组件共享状态）
- Element Plus Form + Valibot（表单与校验；schema 统一收敛在 `schemas/`）
- 组件测试：Vitest + Vue Test Utils
{{slot:bk-fv-1}}

<!-- include: shared/frontend/design-system-usage-element-plus.md -->
<!-- include: shared/ui/design-system-liveness.md -->

<!-- include: shared/ui/spec-lock.md -->

<!-- include: shared/frontend/advisory.md -->

---

## 推荐目录结构

若仓库尚未形成稳定结构，可优先参考以下组织方式；若仓库已有实现，以现状为准，不强制迁移。

```text
src/
├── components/
│   ├── common/               # 通用业务组件
│   └── layout/               # 布局组件
├── views/                    # 页面级组件（按业务域拆分）
├── features/                 # 按业务域收敛（可选）
├── router/                   # Vue Router 配置与路由定义
├── stores/                   # Pinia store（按业务域拆分）
├── composables/              # 组合式函数（含 IPC 数据封装）
├── lib/
{{slot:bk-fv-2}}
│   └── utils/                # 纯工具函数
├── schemas/                  # Valibot / Zod schema
├── styles/                   # Element Plus 主题配置与 SCSS 变量（token 映射）
├── types/                    # TypeScript 类型
├── tests/                    # 前端测试辅助
├── App.vue
└── main.ts

# Electron 主进程与预加载脚本（与前端源码同级或放 main/ 目录）
main/
├── index.ts                  # 主进程入口
{{slot:bk-fv-3}}
preload/
└── index.ts                  # contextBridge 暴露 window.ipc
```

---

## 阶段 1 — 前端实现

**触发条件**：用户发出“开始前端开发”或等价指令

**入场要求**：阶段 0 设计文档已确认；原型已经用户确认，或本次改动命中原型跳过条件

HTML 原型即视觉基线，实现需逐页对照原型。

<!-- include: shared/ui/prototype-workflow.md -->

**工作内容**：
1. 基于 Vue 3 + Vite 实现页面、布局、组件与导航结构。
2. 页面内允许先用 mock 数据、假 IPC 响应或本地 fixture 演示交互。
3. 同步产出前端契约说明：字段定义、筛选条件、分页参数、表单校验规则、权限要求。
4. 只要仓库未明确采用别的组件体系，统一走 Element Plus 组件路线，不引入第二套组件库。

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

### 数据获取与状态管理
- 跨进程异步数据优先封装为组合式函数（`composables/`），页面只消费组合式函数，不在组件内直接调 IPC
- Pinia 仅用于主题、窗口状态、轻量全局筛选、全局通知、临时会话上下文等纯客户端状态
- 禁止把“可由 IPC 组合式函数管理的服务端数据”长期放进 Pinia
- 若确需引入 `@tanstack/vue-query` 管理复杂缓存，先在阶段 0 技术选型中说明，不在页面里零散使用

### IPC 前端调用规范
- 所有跨进程调用统一收敛在 `lib/ipc/` 或按业务域封装，不直接散落在页面组件中
- 渲染进程只通过 `window.ipc.<method>` 调用（由 preload `contextBridge` 暴露的封装），不在页面里直接拼 channel 名称
{{slot:bk-fv-4}}
- IPC method 的参数与返回值应先经过 TypeScript 类型与 schema（Valibot / Zod）约束
{{slot:bk-fv-5}}
- 除非明确需要，否则不要在 Electron 应用里再额外引入本地 HTTP 服务作为前后端桥接层
- 若确实需要流式处理、WebSocket、嵌入式代理等能力，再单独论证是否引入额外服务层

### 表单规则
- 表单优先使用 Element Plus Form + `schemas/` 中的 Valibot / Zod schema 校验
- 表单 schema、默认值、字段映射与 `schemas/` 中的定义保持一致
- 错误提示要区分字段校验错误、权限错误、状态冲突、系统异常
- 禁止在组件中手写大量分散校验逻辑替代 schema

### 样式规则
- 优先使用 Element Plus 组件内置样式和 class
- 自定义样式使用 SCSS，变量来自 `design-system.yaml`
- 禁止 inline style，禁止硬编码颜色值（如 `#1A90FF`）
- 以桌面窗口为主要目标，不强制移动端响应式；窗口是否可调整以项目配置为准，可调窗口需处理窄宽度下的布局

<!-- include: shared/frontend/component-rules.md -->

<!-- include: shared/frontend/external-request-desktop.md -->

<!-- include: shared/frontend/code-organization.md -->

---

## 测试规则

<!-- include: shared/frontend/pre-commit-regression.md -->

### 前端测试
- 组件测试优先使用 Vitest + Vue Test Utils
- 路由、表单、交互、禁用态、错误态应有验证
- 对外壳桥接调用统一使用 mock 封装层（mock `window.ipc`），不直接 mock 页面内部实现细节

<!-- include: shared/no-test-fallback.md -->

---
