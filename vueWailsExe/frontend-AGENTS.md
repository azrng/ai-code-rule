# AGENTS.md

## 适用范围

- 作用域：前端实现、页面、组件、路由、状态、样式与前端测试
- 触发场景：涉及页面、布局、组件、交互、样式、前端 smoke test 时阅读

### 阅读摘要
- 建议阅读：新增页面、改表单、改交互、改路由、改组件样式、补前端验证
- 可先跳过：纯服务端逻辑、纯数据访问、仅文档整理、仅部署配置调整
- 优先查看：实现规则、组件 / 状态规则、测试规则

### 常见任务入口
- 新增页面或布局：先看页面 / 组件规则与目录约定
- 改表单、筛选、交互：先看状态管理、请求规则与校验约定
- 改样式、组件展示：先看样式规则与设计系统约束
- 补前端回归：先看 `提交前最小回归` 与测试规则

---

## 技术栈

### 前端
- Vue 3 + TypeScript + Vite
- Element Plus 组件库
- Element Plus Icons（唯一图标库，尺寸统一 18-24px）
- Vue Router 4（路由管理）
- Pinia（全局状态管理：用户信息、权限、通知等跨组件共享状态）
- Axios（HTTP 客户端，仅用于外部 API 调用）

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
│   ├── src/
│   │   ├── api/               # 外部 API 请求模块（仅用于外部服务调用）
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
│   │   ├── hooks/             # 组合式函数
│   │   ├── bindings/          # Wails 自动生成的 Go 绑定类型声明
│   │   ├── config/
│   │   │   └── element-theme.ts  # Element Plus 主题配置
│   │   ├── App.vue
│   │   └── main.ts
│   ├── wails.js               # Wails 自动生成的绑定入口
│   ├── package.json
│   ├── vite.config.ts
│   └── tsconfig.json
├── doc/
│   ├── devlog/
│   ├── design/
│   └── requirement.md
├── build/                     # Wails 构建配置
│   └── windows/               # Windows 图标等资源
├── wails.json                 # Wails 项目配置
├── .gitignore
├── AGENTS.md
├── CLAUDE.md
├── GEMINI.md
├── design-system.yaml
└── TASK.md
```

---

### 阶段 1 — 视图实现（Claude Code 主导）

**触发条件**：用户发出「开始视图开发」指令

**入场要求**：阶段 0 设计文档已由用户确认

**工作内容**：
1. 按设计文档实现页面和组件，遵循 `design-system.yaml` 和 Element Plus 规范
2. 数据层使用 mock（静态 mock 数据），不依赖真实 Go 后端绑定
3. 同步输出接口需求说明，供 Go 后端开发参考

**产物**：
- 可在浏览器中运行的前端页面（通过 `wails dev` 预览）
- 接口需求说明（用于 Go 后端开发）

**门控规则**：
- 用户确认视图页面符合设计文档预期
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

### Wails 绑定调用规范
- 通过 `src/bindings/` 下自动生成的模块调用 Go 方法
- 绑定方法返回 Promise，必须使用 `await` 或 `.then()/.catch()` 处理
- 禁止绕过 Wails 绑定直接通过 HTTP 请求与 Go 后端通信
- 绑定方法调用应封装在 hooks 或 store 中，不直接散落在页面组件内
- 修改 Go 方法签名后必须重新运行 `wails generate module`

### 样式规则
- 优先使用 Element Plus 组件内置样式和 class
- 自定义样式使用 SCSS，变量来自 `design-system.yaml`
- 禁止 inline style，禁止硬编码颜色值（如 `#1A90FF`）
- Wails 窗口为固定大小，不需要考虑移动端响应式

### 组件规则
- 优先复用 `src/components/` 下已有组件，禁止重复创建
- 只有确实有复用价值时才新增共享组件，避免为单次需求过度抽象
- 页面状态必须完整：`loading`、`empty`、`error`、`no-permission`
- 除非仓库已在使用，否则不要引入新的组件库或样式体系

### 外部 API 请求规则
- 仅在需要调用外部服务时使用 Axios
- 禁止使用 Axios 调用本机 Go 后端（必须通过 Wails 绑定）
- Axios 实例统一配置，拦截器处理错误
- API 函数命名规范：`getXxx`、`createXxx`、`updateXxx`、`deleteXxx`

---

## 测试规则

### 提交前最小回归
- 默认执行：项目现有的静态检查、类型检查或编译校验
- 页面、路由、表单、交互改动：至少补一次受影响页面或组件的 smoke test / 交互验证
- 仅样式或文案微调：至少确认受影响页面的关键状态、布局与核心交互未回退
- 若改动影响共享组件、布局或状态流转，优先验证影响范围最大的页面，而不是只看局部组件

### 总体要求
- 测试暂时不作为强制要求
- 若补充测试，应覆盖真实业务行为

### 前端测试（可选）
- 暂时不考虑单元测试
- 但必须进行手动 smoke test 验证核心功能

### 无法执行测试时
- 必须说明未执行的测试类型
- 必须说明未执行原因
- 必须说明潜在影响范围和风险
