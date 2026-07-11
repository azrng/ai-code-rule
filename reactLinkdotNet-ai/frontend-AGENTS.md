---
rule_id: frontend-agents
version: 1.0.0
last_updated: 2026-07-11
dependencies: [agents-root]
---

# 前端规则

> 与 `reactLinkdotNet` 的差异：UI 从 **Ant Design 5 换为 Tailwind CSS + shadcn/ui**；图标从 `@ant-design/icons` 换为 **lucide-react**；图表改为 **recharts（可选）**。更契合 AI Agent 产品中"自定义结果卡、流式输出、非标准交互"的场景，且 AI Coding 对这套组合高度熟悉。

## 适用范围

- 作用域：前端实现、页面、组件、路由、状态、样式与前端测试
- 触发场景：涉及页面、布局、组件、交互、样式、流式展示、前端 smoke test 时阅读

### 阅读摘要
- 建议阅读：新增页面、改表单、改交互、改路由、改组件样式、做流式 / Agent 输出展示、补前端验证
- 可先跳过：纯服务端逻辑、纯数据访问、仅文档整理、仅部署配置调整
- 优先查看：技术栈与 shadcn 用法、组件 / 状态规则、流式展示规则、测试规则

### 常见任务入口
- 新增页面或布局：先看页面 / 组件规则与目录约定
- 改表单、筛选、交互：先看状态管理、请求规则与校验约定
- 改样式、组件展示：先看样式规则与设计系统约束（`design-system.yaml`）
- 做 Agent 对话 / 流式输出展示：先看流式展示规则与组件组织
- 补前端回归：先看 `提交前最小回归` 与测试规则

---

## 技术栈

### 前端
- React 18（或 19）+ TypeScript + Vite
- Tailwind CSS（原子化样式，唯一样式引擎）
- shadcn/ui（基于 Radix UI 的复制式组件，不是 npm 安装的组件库，组件代码落在 `src/components/ui/`）
- lucide-react（唯一图标库）
- React Router 6（路由管理）
- Zustand（全局状态管理：用户信息、权限、通知、Agent 会话等跨组件共享状态）
- Axios（HTTP 客户端）
- class-variance-authority + clsx + tailwind-merge（shadcn 标配的变体与类名合并工具）

### 设计系统
- 视觉 token 定义在 `design-system.yaml`，落地为 Tailwind 主题（`tailwind.config.ts`）与 shadcn 的 CSS 变量（`src/styles/globals.css`）
- 优先复用 `src/components/ui/` 下 shadcn 组件与 Radix 能力，再考虑自定义组件
- 禁止 inline style 硬编码颜色 / 间距 / 圆角；颜色统一走语义化 Tailwind 类或 shadcn CSS 变量

### 关于 shadcn/ui 的工作方式（重要）
- shadcn/ui **不是依赖包**，而是把组件源码"复制进"项目（`src/components/ui/`），由项目持有与定制
- 新增组件优先用 CLI（`npx shadcn@latest add <component>`）拉取到本地，再按需定制；不要手写已被 shadcn 覆盖的基础组件
- 复制进来的组件仍受本规则与 `design-system.yaml` 约束（颜色用 token、尺寸用 Tailwind 工具类）

如果仓库已经有真实实现，以现有代码为准，不要强行重构或替换技术栈。
技术债务与重构判断遵循根 `AGENTS.md` 的全局规则。

---

## 主动建议规则
- 发现页面、组件、状态管理或请求逻辑职责混杂，导致文件膨胀、复用困难或测试困难时，应主动提醒并建议最小拆分点
- 发现样式与 `design-system.yaml`、shadcn CSS 变量或现有交互模式冲突，或绕过 shadcn 手写重复基础组件时，应先说明差异和影响，不直接引入平行视觉体系
- 发现表单校验、权限展示、页面状态、流式展示、刷新策略或错误处理缺失时，应主动提醒
- 发现可以复用已有组件、Hook、Store、请求封装、DTO / 类型定义或 shadcn 组件时，应优先建议复用
- 发现 Agent 输出展示缺少 loading / 流式中断 / error / empty 反馈时，应主动提醒

## 推荐目录结构

- 前端目录聚焦在 `frontend/` 下组织，优先复用现有结构，不强制迁移。

```text
frontend/
├── public/
├── src/
│   ├── api/               # 按业务域拆分的请求模块（含流式 / SSE 封装）
│   ├── components/
│   │   ├── ui/            # shadcn/ui 组件（复制进来，可定制）
│   │   ├── common/        # 通用业务组件
│   │   ├── layout/        # 布局组件
│   │   └── agent/         # Agent 输出相关组件（消息卡、流式打字机、步骤条、确认按钮）
│   ├── pages/             # 页面级组件（按业务域拆分）
│   ├── router/
│   │   ├── index.tsx      # 路由配置
│   │   └── routes.tsx     # 路由定义
│   ├── stores/            # Zustand stores（按业务域拆分）
│   ├── hooks/             # 自定义 Hooks（useXxx）
│   ├── types/             # TypeScript 类型定义
│   ├── utils/             # 纯工具函数（含 cn 类名合并等）
│   ├── styles/
│   │   └── globals.css    # Tailwind 指令 + shadcn CSS 变量
│   ├── lib/
│   │   └── utils.ts       # shadcn 约定的 cn() 等工具
│   ├── App.tsx
│   └── main.tsx
├── components.json        # shadcn 配置
├── tailwind.config.ts
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
- 路由命名使用有意义的路径（如 `/chat/:sessionId`、`/writing/outline`）

### Zustand 状态管理规范
- Store 按业务域拆分，放在 `src/stores/` 目录
- 使用 `create` 函数定义 store
- Store 命名：`use[Domain]Store`（如 `useUserStore`、`useChatStore`、`useAgentStore`）
- 修改 store 状态直接通过 set 方法，推荐使用 immer 中间件简化不可变更新
- 异步操作直接在 store actions 或组件中通过 async/await 处理
- Agent 会话与消息流建议独立 store，便于流式增量更新与多会话切换

### 样式规则
- 唯一样式引擎为 Tailwind CSS；颜色、间距、圆角、阴影统一使用 Tailwind 工具类或 shadcn CSS 变量，禁止硬编码
- 组件变体统一用 `class-variance-authority`（shadcn 标配），类名合并统一用 `cn()`（clsx + tailwind-merge）
- 自定义样式使用 Tailwind 工具类为主；确需全局或复杂样式时用 CSS Modules 或 `globals.css`，变量来自 `design-system.yaml`
- 禁止 inline style 硬编码颜色值（如 `style={{ color: '#1677ff' }}`）
- 暗色模式通过 shadcn 的 `dark` 类与 CSS 变量切换，不另建一套颜色
- 响应式设计遵循移动优先原则，使用 Tailwind 断点（`sm:` / `md:` / `lg:` ...）

### 组件规则
- 优先复用 `src/components/ui/` 下 shadcn 组件与 `src/components/` 下已有业务组件，禁止重复创建
- 只有 shadcn 未覆盖、且确实有复用价值时才新增自定义组件，避免为单次需求过度抽象
- 页面状态必须完整：`loading`（Skeleton / Spinner）、`empty`（Empty 占位）、`error`（错误提示 + 重试）、`no-permission`
- 除非仓库已在使用，否则不要引入新的组件库或 UI 体系（不与 shadcn / Tailwind 并存第二套）

### 流式与 Agent 输出展示规则
- 对话 / Agent 输出优先做流式打字机展示；增量片段追加到消息 store，不整包替换
- 必须处理：流式进行中（loading / 可取消）、流式完成、网络中断、空响应、超时
- Agent 中间产物（候选选题、草稿、步骤进度）用结构化卡片展示，让用户能"选择 / 确认 / 编辑"后继续，而不只是一段文字
- 涉及用户确认的关键节点（高风险动作、下一步分支）必须有明确的确认按钮，不靠用户猜
- 流式事件结构以后端契约为准（见 `doc-AGENTS.md`），前端只做渲染与状态管理

### 请求规则
- 禁止在组件中直接调用后端 API，统一通过 `src/api/` 下的请求模块封装
- Axios 实例统一配置，拦截器处理 token、错误
- 流式接口单独封装（基于 fetch + ReadableStream 或 EventSource），与普通 Axios 请求分层
- 业务常量不要散落在页面组件内部
- API 函数命名规范：`getXxx`、`createXxx`、`updateXxx`、`deleteXxx`、`streamXxx`

### 代码组织规范
- 一个文件只放一个主对象：页面组件、布局组件、通用组件、Hook、Store 各自独立文件，文件名与导出的主对象一致。
- 组件职责单一：一个组件只做一件事，发现持续膨胀或多个不相关职责堆在同一文件时，应拆分为更小的子组件、Hook 或工具函数。
- 触发拆分的信号：单文件出现多个主组件、props 或状态持续堆叠、组件跨多个不相关业务、阅读成本明显升高。
- 允许例外：仅服务当前组件的局部子组件、紧耦合的类型定义、测试文件中的 fixture；`src/components/ui/` 下的 shadcn 组件遵循 shadcn 自身的组织约定。
- 反模式：一个文件同时导出页面、表单、列表、弹窗等多个组件；把多个不相关 Hook 或工具函数堆在同一文件。

---

## 测试规则

### 提交前最小回归
- 默认执行：项目现有的静态检查、类型检查或编译校验
- 页面、路由、表单、交互、流式展示改动：至少补一次受影响页面或组件的 smoke test / 交互验证
- 仅样式或文案微调：至少确认受影响页面的关键状态、布局与核心交互未回退
- 若改动影响共享组件、布局或状态流转，优先验证影响范围最大的页面，而不是只看局部组件

### 总体要求
- 影响行为的改动应优先补充或更新测试
- 若本次改动未补测试，必须在最终说明中写明原因和风险
- 测试应覆盖真实业务行为

### 前端测试
- 前端暂时不考虑单元测试
- 但必须进行手动 smoke test 验证核心功能（含 Agent 输出与流式展示的关键路径）

### 外部依赖与数据
- 测试中不要真实调用第三方服务（含真实模型接口），统一使用 mock
- 测试数据应尽量最小化、可读、可重复执行
- 不要让测试依赖本地人工状态

### 无法执行测试时
- 必须说明未执行的测试类型
- 必须说明未执行原因
- 必须说明潜在影响范围和风险

---

文件结束。
