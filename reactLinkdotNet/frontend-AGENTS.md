---
rule_id: frontend-agents
version: 1.3.1
last_updated: 2026-05-19
dependencies: [agents-root]
---

# 前端规则

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
- React 18 + TypeScript + Vite
- Ant Design 5 组件库（antd）
- Ant Design Icons（@ant-design/icons，唯一图标库）
- React Router 6（路由管理）
- Zustand（全局状态管理：用户信息、权限、通知等跨组件共享状态）
- Axios（HTTP 客户端）

### 设计系统
- 使用 Ant Design 主题系统（ConfigProvider + Design Token），所有视觉规范在 `design-system.yaml` 中定义
- 优先使用 Ant Design 组件和样式，禁止 inline style，禁止硬编码颜色/间距/圆角

如果仓库已经有真实实现，以现有代码为准，不要强行重构或替换技术栈。
技术债务与重构判断遵循根 `AGENTS.md` 的全局规则。

---

## 主动建议规则
- 发现页面、组件、状态管理或请求逻辑职责混杂，导致文件膨胀、复用困难或测试困难时，应主动提醒并建议最小拆分点
- 发现样式与 `design-system.yaml`、现有主题、组件库或交互模式冲突时，应先说明差异和影响，不直接引入平行视觉体系
- 发现表单校验、权限展示、页面状态、刷新策略或错误处理缺失时，应主动提醒
- 发现可以复用已有组件、Hook、Store、请求封装、DTO / 类型定义或工具函数时，应优先建议复用

## 推荐目录结构

- 前端目录建议聚焦在 `frontend/` 下组织，优先复用现有结构，不强制迁移。

```text
frontend/
├── public/
├── src/
│   ├── api/               # 按业务域拆分的请求模块
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
├── Dockerfile
├── package.json
├── vite.config.ts
└── tsconfig.json
```

---

## 前端规则

### React Hooks 规范
- 函数组件 + Hooks 是唯一写法，禁止 class 组件
- 响应式数据使用 `useState`，复杂状态使用 `useReducer`
- 计算属性使用 `useMemo`，回调缓存使用 `useCallback`
- 副作用使用 `useEffect`，清理逻辑在返回函数中处理
- 可复用逻辑封装为自定义 Hook（`useXxx`），放在 `hooks/` 目录

### 组件通信规范
- **父传子**：通过 Props 传递，子组件只读不可修改
- **子传父**：通过回调函数 Props（`onXxx` 命名）
- **跨级传值**：优先使用 Zustand，其次使用 React Context
- **兄弟组件**：通过共同父组件或 Zustand 中转
- 避免直接修改 props，需要双向绑定时使用受控组件模式（value + onChange）

### 路由管理规范
- 路由配置集中放在 `src/router/routes.tsx`
- 使用 React Router 6 的 `createBrowserRouter` + `RouterProvider`
- 路由守卫通过 `loader` 函数或 `ProtectedRoute` 包裹组件实现
- 路由跳转使用 `useNavigate()` hook
- 获取路由参数使用 `useParams()` 和 `useSearchParams()`
- 路由命名使用有意义的路径（如 `/users/list`、`/users/detail/:id`）

### Zustand 状态管理规范
- Store 按业务域拆分，放在 `src/stores/` 目录
- 使用 `create` 函数定义 store
- Store 命名：`use[Domain]Store`（如 `useUserStore`、`usePermissionStore`）
- 修改 store 状态直接通过 set 方法，推荐使用 immer 中间件简化不可变更新
- 异步操作直接在 store actions 或组件中通过 async/await 处理

### 样式规则
- 优先使用 Ant Design 组件内置样式和 Design Token
- 自定义样式使用 CSS Modules 或全局 CSS，变量来自 `design-system.yaml`
- 禁止 inline style，禁止硬编码颜色值（如 `#1677ff`）
- 主题定制统一通过 `ConfigProvider` 的 `theme` 属性配置
- 响应式设计遵循移动优先原则，使用 Ant Design 的 Grid 系统（Row / Col）

### 组件规则
- 优先复用 `src/components/` 下已有组件，禁止重复创建
- 只有确实有复用价值时才新增共享组件，避免为单次需求过度抽象
- 页面状态必须完整：`loading`（Spin / Skeleton）、`empty`（Empty）、`error`（Result）、`no-permission`
- 除非仓库已在使用，否则不要引入新的组件库或样式体系

### 请求规则
- 禁止在组件中直接调用后端 API，统一通过 `src/api/` 下的请求模块封装
- Axios 实例统一配置，拦截器处理 token、错误
- 业务常量不要散落在页面组件内部
- API 函数命名规范：`getXxx`、`createXxx`、`updateXxx`、`deleteXxx`

---

## 测试规则

### 提交前最小回归
- 默认执行：项目现有的静态检查、类型检查或编译校验
- 页面、路由、表单、交互改动：至少补一次受影响页面或组件的 smoke test / 交互验证
- 仅样式或文案微调：至少确认受影响页面的关键状态、布局与核心交互未回退
- 若改动影响共享组件、布局或状态流转，优先验证影响范围最大的页面，而不是只看局部组件

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

文件结束。
