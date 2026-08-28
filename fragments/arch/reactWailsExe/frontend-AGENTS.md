---
rule_id: frontend-agents
version: 1.14.0
last_updated: 2026-08-27
dependencies: [agents-root]
---

# 前端规则

## 适用范围

- 作用域：界面、视图、页面、组件、交互、样式与前端测试
- 触发场景：涉及任何用户可见界面、前端交互、客户端状态、组件拆分、界面回归时阅读

<!-- include: shared/frontend/common-tasks-v2.md -->

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

<!-- include: shared/frontend/advisory.md -->

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

<!-- include: shared/frontend/react-hooks.md -->

<!-- include: shared/frontend/component-communication-v1.md -->

<!-- include: shared/frontend/routing-management-v1.md -->

<!-- include: shared/frontend/zustand.md -->

<!-- include: shared/frontend/wails-binding-call.md -->

### 样式规则
- 优先使用 Ant Design 组件内置样式和 Design Token
- 自定义样式使用 CSS Modules 或全局 CSS，变量来自 `design-system.yaml`
- 禁止 inline style，禁止硬编码颜色值（如 `#1677ff`）
- 主题定制统一通过 `ConfigProvider` 的 `theme` 属性配置
- Wails 窗口为固定大小，不需要考虑移动端响应式

<!-- include: shared/frontend/component-rules-v2.md -->

### 外部 API 请求规则
- 仅在需要调用外部服务时使用 Axios
- 禁止使用 Axios 调用本机 Go 后端（必须通过 Wails 绑定）
- Axios 实例统一配置，拦截器处理错误
- API 函数命名规范：`getXxx`、`createXxx`、`updateXxx`、`deleteXxx`

<!-- include: shared/frontend/code-organization.md -->

---

## 测试规则

<!-- include: shared/frontend/pre-commit-regression.md -->

<!-- include: shared/backend/general-requirements.md -->

### 前端测试（可选）
- 暂时不考虑单元测试
- 但必须进行手动 smoke test 验证核心功能

<!-- include: shared/no-test-fallback.md -->

---
