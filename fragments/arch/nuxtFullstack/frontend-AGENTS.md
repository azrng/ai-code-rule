---
rule_id: frontend-agents
version: 1.57.0
last_updated: 2026-09-07
dependencies: [agents-root]
---

# 前端规则

## 适用范围

- 作用域：Nuxt 页面、布局、组件、组合式函数、状态、样式与前端测试
- 触发场景：涉及文件路由、页面组件、布局、表单、交互、前端 smoke test 时阅读

---

## 技术栈

### 前端
- Nuxt 3 + Vue 3 + TypeScript（SSR 默认开启，按需关闭）
- Tailwind CSS（唯一样式体系）
- `@nuxt/fonts`（统一字体接入）
- shadcn-vue / Radix Vue（如仓库已使用则沿用）
- lucide-vue-next（唯一图标库）
- VeeValidate + Zod（表单与校验）
- Pinia（仅用于：侧边栏折叠、全局筛选、通知、轻量会话上下文）

### 设计系统
<!-- include: shared/ui/design-system-liveness.md -->
- 所有视觉规范以 `design-system.yaml` 为唯一事实来源
- 优先复用项目已有组件、布局、样式变量与交互模式
- 禁止 inline style，禁止硬编码颜色、间距、圆角、阴影、断点

<!-- include: shared/ui/spec-lock.md -->

<!-- include: shared/application/tech-choice.md -->

---

<!-- include: shared/frontend/advisory-fullstack.md -->

---

## 阶段 1 — 页面与交互实现

**入场要求**：阶段 0 设计文档已确认；原型已经用户确认，或本次改动命中原型跳过条件

HTML 原型即视觉基线，实现需逐页对照原型。

<!-- include: shared/ui/prototype-workflow.md -->

**工作内容**：
1. 基于 Nuxt 文件路由（`pages/`）实现页面、布局、组件和导航结构。
2. 页面默认保持 SSR；取数统一使用 `useFetch` / `useAsyncData`，交互逻辑再下沉到客户端。
3. 阶段内允许使用 mock 数据、假接口或静态数据文件，但页面交互必须完整可演示。
4. 同步产出契约说明：字段定义、筛选条件、分页参数、表单校验规则、权限要求。

**门控规则**：
- 用户确认页面符合设计文档预期。
- 页面交互逻辑经手工 smoke test 验证通过。
- 前端契约、字段说明和 schema 已初步定稳；阶段 2 若发现契约缺口，按 `collaboration-AGENTS.md`「纠错、回退与重构」先回到契约变更流程。
- 视觉验收按「视觉验收」小节执行，用户终审通过前不得进入阶段 2。

<!-- include: shared/ui/visual-acceptance.md -->

---

## 前端规则

### Nuxt / 文件路由规范
- 页面、布局统一使用文件系统路由：`pages/` 目录组织页面，`layouts/` 组织布局
- 页面默认 SSR 渲染；只有纯客户端交互块才使用 `.client.vue` 或客户端组件约束
- `app.vue` 只保留全局壳与 `NuxtLayout` / `NuxtPage`，不堆业务逻辑
- 页面元信息（布局、中间件、鉴权标记）统一用 `definePageMeta` 声明
- 表单优先使用 VeeValidate + Zod
- 复用逻辑使用组合式函数，放在 `composables/` 或按业务域收敛（目录内自动导入，不手写相对路径导入）
- 组件放 `components/`（自动导入），复杂业务组件可按 `components/<域>/` 分组

### 组件通信规范
- 父传子：props
- 子传父：emit 回调
- 跨组件共享的客户端状态：Pinia
- 仅 UI 级共享配置可使用 `provide` / `inject`
- 避免 props drill 过深；但不要为了少量传值滥用全局状态

### 路由管理规范
- 页面路由即文件系统路由，目录命名必须表达业务语义
- 页面跳转优先使用 `NuxtLink`
- 客户端编程式跳转使用 `navigateTo`
- 服务端重定向在 `server/` 路由或路由中间件中用 `navigateTo(..., { redirectCode })` 完成
- 高级路由能力（动态路由、嵌套布局、路由中间件）按需使用，不过度堆叠

### 状态管理规范
- 服务端数据默认交给 `useFetch` / `useAsyncData` 的缓存键管理，不存入 Pinia
- 客户端组件内表单与局部状态用 `ref` / `reactive` 组合式函数管理
- Pinia 仅用于侧边栏折叠、全局筛选、通知、轻量会话上下文等纯客户端状态
- `useState` 只用于跨组件的轻量 SSR 状态传递，不当作全局业务状态库使用

### 样式规则
- 统一使用 Tailwind CSS 与设计 token
- 字体统一通过 `@nuxt/fonts` 接入
- 优先复用项目现有 `components/ui` 组件、`cn()`、`cva` 或等价封装
- 禁止 inline style
- 禁止硬编码颜色值、阴影值、圆角值、z-index、断点
- 响应式遵循移动优先原则；存量样式文件已是桌面优先写法（max-width 查询 + 桌面基础样式）时，增量改动沿用该文件既有约定，不整体重写扩大 diff，新增文件仍遵循移动优先

### 组件规则
- 优先复用 `components/` 与 `components/<业务域>/` 现有组件
- 只有确认存在复用价值时，才新增共享组件
- 页面状态必须完整：`loading`、`empty`、`error`、`no-permission`
- 不要为单次需求过度抽象组件

### 请求与数据获取规则
- 页面级取数统一使用 `useFetch` / `useAsyncData`（自带 SSR 与缓存键），禁止在 setup 里裸写 `$fetch` 完成首屏取数
- 浏览器端事件内的写操作与补充请求使用 `$fetch('/api/...')` 相对地址
- 服务端路由内部逻辑直接复用 `server/utils/` 的服务层函数，不绕路请求自己的 `/api`
- 环境变量区分 `NUXT_PUBLIC_*`（可下发浏览器）与服务端私有变量；客户端不得读取服务端密钥

<!-- include: shared/frontend/code-organization.md -->

---

## 测试规则

<!-- include: shared/frontend/general-requirements.md -->

<!-- include: shared/frontend/pre-commit-regression.md -->

### 前端测试
- 组件测试优先使用 Vitest + Testing Library（@vue/test-utils）
- 关键页面至少应完成一次手工或 Playwright smoke test
- 交互、校验、禁用态、错误态应有验证

<!-- include: shared/no-test-fallback.md -->

---
