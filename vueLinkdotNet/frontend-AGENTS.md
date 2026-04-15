# AGENTS.md

## 适用范围

- 作用域：Vue 页面、组件、状态、路由、样式与前端测试规则
- 触发场景：涉及前端实现、页面交互、前端 smoke test 时阅读

---

## 技术栈

### 前端
- Vue 3 + TypeScript + Vite
- Element Plus 组件库
- Element Plus Icons（唯一图标库，尺寸统一 18-24px）
- Vue Router 4（路由管理）
- Pinia（全局状态管理：用户信息、权限、通知等跨组件共享状态）
- Axios（HTTP 客户端）

### 设计系统
- 使用 Element Plus 主题系统，所有视觉规范在 `design-system.yaml` 中定义
- 优先使用 Element Plus 组件和样式，禁止 inline style，禁止硬编码颜色/间距/圆角

如果仓库已经有真实实现，以现有代码为准，不要强行重构或替换技术栈。

---

## 推荐目录结构
若仓库尚未形成稳定结构，可优先参考以下组织方式；若仓库已有实现，以现状为准，不强制迁移。

```text
project-root/
├── frontend/
│   ├── public/
│   ├── src/
│   │   ├── api/               # 按业务域拆分的请求模块
│   │   ├── components/
│   │   │   ├── common/        # 通用业务组件
│   │   │   └── layout/        # 布局组件
│   │   ├── views/             # 页面级组件（按业务域拆分）
│   │   ├── router/
│   │   │   ├── index.ts       # 路由配置
│   │   │   └── routes.ts      # 路由定义
│   │   ├── stores/            # Pinia stores（按业务域拆分）
│   │   ├── types/             # TypeScript 类型定义
│   │   ├── utils/             # 纯工具函数
│   │   ├── directives/        # 自定义指令
│   │   ├── hooks/             # 组合式函数
│   │   ├── config/
│   │   │   └── element-theme.ts  # Element Plus 主题配置
│   │   ├── App.vue
│   │   └── main.ts
│   ├── Dockerfile
│   ├── package.json
│   ├── vite.config.ts
│   └── tsconfig.json
├── doc/
│   ├── devlog/
│   ├── design/
│   └── requirement.md
├── docker-compose.yml
├── .env.example
├── .gitignore
├── AGENTS.md
├── CLAUDE.md
├── GEMINI.md
├── design-system.yaml
├── TASK.md
└── TaskHistory.md
```

---

### 阶段 1 — 前端实现（Claude Code 主导）

**触发条件**：用户发出「开始前端开发」指令

**入场要求**：阶段 0 设计文档已由用户确认

**工作内容**：
1. 按设计文档实现页面和组件，遵循 `design-system.yaml` 和 Element Plus 规范
2. 数据层使用 mock（静态 mock 数据），不依赖真实后端接口
3. 同步输出接口说明文档，供后端开发参考

**产物**：
- 可在浏览器中运行的前端页面
- 接口需求说明（用于后端开发）

**门控规则**：
- 用户确认前端页面符合设计文档预期
- 前端页面交互逻辑已验证通过
- 满足以上两点后，才允许进入阶段 2

---

## 前端规则

### Composition API 规范
- **必须使用** `<script setup lang="ts">` 语法糖
- 响应式数据使用 `ref`（基础类型）和 `reactive`（对象类型）
- 计算属性使用 `computed`，副作用使用 `watch` 和 `watchEffect`
- 生命周期钩子使用 `onMounted`、`onUnmounted` 等
- 可复用逻辑封装为组合式函数（`useXxx`），放在 `hooks/` 目录

### 组件通信规范
- **父传子**：使用 `defineProps` 定义，只读不可修改
- **子传父**：使用 `defineEmits` 定义事件
- **跨级传值**：优先使用 Pinia，其次使用 `provide`/`inject`
- **兄弟组件**：通过共同父组件或 Pinia 中转
- 避免直接修改 props，需要修改时使用 v-model 或 emit

### 路由管理规范
- 路由配置集中放在 `src/router/routes.ts`
- 路由守卫统一在 `src/router/index.ts` 中配置
- 路由跳转使用 `router.push` 和 `router.replace`
- 获取路由参数使用 `useRoute().params` 或 `useRoute().query`
- 路由命名使用有意义的名称（如 `UserList`、`OrderDetail`）

### Pinia 状态管理规范
- Store 按业务域拆分，放在 `src/stores/` 目录
- 使用 `defineStore` 定义 store
- State 定义数据，Getters 定义计算属性，Actions 定义修改逻辑
- 修改 state 必须通过 actions，禁止在组件中直接修改 store 状态
- 异步操作放在 actions 中，同步计算放在 getters 中

### 样式规则
- 优先使用 Element Plus 组件内置样式和 class
- 自定义样式使用 SCSS，变量来自 `design-system.yaml`
- 禁止 inline style，禁止硬编码颜色值（如 `#1A90FF`）
- 响应式设计遵循移动优先原则，必须在不同断点下测试

### 组件规则
- 优先复用 `src/components/` 下已有组件，禁止重复创建
- 只有确实有复用价值时才新增共享组件，避免为单次需求过度抽象
- 页面状态必须完整：`loading`、`empty`、`error`、`no-permission`
- 除非仓库已在使用，否则不要引入新的组件库或样式体系

### 请求规则
- 禁止在组件中直接调用后端 API，统一通过 `src/api/` 下的请求模块封装
- Axios 实例统一配置，拦截器处理 token、错误
- 业务常量不要散落在页面组件内部
- API 函数命名规范：`getXxx`、`createXxx`、`updateXxx`、`deleteXxx`

---

## 测试规则

### 总体要求
- 影响行为的改动应优先补充或更新测试
- 若本次改动未补测试，必须在最终说明中写明原因和风险
- 测试应覆盖真实业务行为

### 前端测试
- 前端暂时不考虑单元测试
- 但必须进行手动 smoke test 验证核心功能

### 外部依赖与数据
- 测试中不要真实调用第三方服务，统一使用 mock
- 测试数据应尽量最小化、可读、可重复执行
- 不要让测试依赖本地人工状态

### 无法执行测试时
- 必须说明未执行的测试类型
- 必须说明未执行原因
- 必须说明潜在影响范围和风险

---
