# AGENTS.md

## 适用范围

- 作用域：React 页面、组件、状态、路由、样式与前端测试规则
- 触发场景：涉及视图实现、页面交互、状态管理、样式与前端测试时阅读

---

## 技术栈

### 前端
- React 18 + TypeScript + Vite
- Ant Design 5 组件库（antd）
- Ant Design Icons（@ant-design/icons，唯一图标库）
- React Router 6（路由管理）
- Zustand（全局状态管理：用户信息、权限、通知等跨组件共享状态）
- Axios（HTTP 客户端，仅用于外部 API 调用）

### 设计系统
- 使用 Ant Design 主题系统（ConfigProvider + Design Token），所有视觉规范在 `design-system.yaml` 中定义
- 优先使用 Ant Design 组件和样式，禁止 inline style，禁止硬编码颜色/间距/圆角

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
│   │   ├── pages/             # 页面级组件（按业务域拆分）
│   │   ├── router/
│   │   │   ├── index.tsx       # 路由配置
│   │   │   └── routes.tsx      # 路由定义
│   │   ├── stores/            # Zustand stores（按业务域拆分）
│   │   ├── hooks/             # 自定义 Hooks（useXxx）
│   │   ├── types/             # TypeScript 类型定义
│   │   ├── utils/             # 纯工具函数
│   │   ├── bindings/          # Wails 自动生成的 Go 绑定类型声明
│   │   ├── config/
│   │   │   └── antd-theme.ts   # Ant Design 主题配置
│   │   ├── App.tsx
│   │   └── main.tsx
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
├── TASK.md
└── TaskHistory.md
```

---

### 阶段 1 — 视图实现（Claude Code 主导）

**触发条件**：用户发出「开始视图开发」指令

**入场要求**：阶段 0 设计文档已由用户确认

**工作内容**：
1. 按设计文档实现页面和组件，遵循 `design-system.yaml` 和 Ant Design 规范
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

### Wails 绑定调用规范
- 通过 `src/bindings/` 下自动生成的模块调用 Go 方法
- 绑定方法返回 Promise，必须使用 `await` 或 `.then()/.catch()` 处理
- 禁止绕过 Wails 绑定直接通过 HTTP 请求与 Go 后端通信
- 绑定方法调用应封装在 hooks 或 store 中，不直接散落在页面组件内
- 修改 Go 方法签名后必须重新运行 `wails generate module`

### 样式规则
- 优先使用 Ant Design 组件内置样式和 Design Token
- 自定义样式使用 CSS Modules 或全局 CSS，变量来自 `design-system.yaml`
- 禁止 inline style，禁止硬编码颜色值（如 `#1677ff`）
- 主题定制统一通过 `ConfigProvider` 的 `theme` 属性配置
- Wails 窗口为固定大小，不需要考虑移动端响应式

### 组件规则
- 优先复用 `src/components/` 下已有组件，禁止重复创建
- 只有确实有复用价值时才新增共享组件，避免为单次需求过度抽象
- 页面状态必须完整：`loading`（Spin / Skeleton）、`empty`（Empty）、`error`（Result）、`no-permission`
- 除非仓库已在使用，否则不要引入新的组件库或样式体系

### 外部 API 请求规则
- 仅在需要调用外部服务时使用 Axios
- 禁止使用 Axios 调用本机 Go 后端（必须通过 Wails 绑定）
- Axios 实例统一配置，拦截器处理错误
- API 函数命名规范：`getXxx`、`createXxx`、`updateXxx`、`deleteXxx`

---

## 测试规则

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

---
