---
rule_id: frontend-agents
version: 1.13.0
last_updated: 2026-08-27
dependencies: [agents-root]
---

# 前端规则

## 适用范围

- 作用域：界面、视图、页面、组件、交互、样式与前端测试
- 触发场景：涉及任何用户可见界面、前端交互、客户端状态、组件拆分、界面回归时阅读

### 常见任务入口
- 新增页面或布局：先看 `阶段 1`、`页面与组件规范`
- 改表单、筛选、交互：先看 `状态与数据流规则`、`表单规则`
- 改样式、组件展示：先看 `样式规则`、`组件规则`
- 补前端回归：先看 `提交前最小回归`、`前端测试`

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

### 技术选择原则
如果仓库已经有真实实现，以现有代码为准，不要强行重构或替换技术栈。
技术债务与重构判断遵循根 `AGENTS.md` 的全局规则。

如果仓库已经有真实实现，以现有代码为准，不要强行重构或替换技术栈。
技术债务与重构判断遵循根 `AGENTS.md` 的全局规则。

---

## 主动建议规则
- 发现页面、视图、组件、状态管理或请求逻辑职责混杂，导致文件膨胀、复用困难或测试困难时，应主动提醒并建议最小拆分点
- 发现样式与 `design-system.yaml`、现有主题、组件库或交互模式冲突时，应先说明差异和影响，不直接引入平行视觉体系
- 发现表单校验、权限展示、页面状态、刷新策略或错误处理缺失时，应主动提醒
- 发现可以复用已有页面基座、组件、hook、store、请求封装、schema 或工具函数时，应优先建议复用
- 不确定交互规则、字段含义、视觉 token 或前端数据来源时，应按根 `AGENTS.md` 的查证优先级处理，禁止凭经验补写规则

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

### 阶段 1 — 界面与交互实现

**触发条件**：用户发出"开始前端开发"或等价指令

**入场要求**：阶段 0 设计文档已确认

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

### 代码组织规范
- 一个文件只放一个主对象：页面组件、布局组件、通用组件、Hook、Store 各自独立文件，文件名与导出的主对象一致。
- 组件职责单一：一个组件只做一件事，发现持续膨胀或多个不相关职责堆在同一文件时，应拆分为更小的子组件、Hook 或工具函数。
- 触发拆分的信号：单文件出现多个主组件、props 或状态持续堆叠、组件跨多个不相关业务、阅读成本明显升高。
- 允许例外：仅服务当前组件的局部子组件、紧耦合的类型定义、测试文件中的 fixture。
- 反模式：一个文件同时导出页面、表单、列表、弹窗等多个组件；把多个不相关 Hook 或工具函数堆在同一文件。

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

---
