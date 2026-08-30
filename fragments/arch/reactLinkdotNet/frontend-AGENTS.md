---
rule_id: frontend-agents
version: 1.15.0
last_updated: 2026-08-28
dependencies: [agents-root]
---

# 前端规则

<!-- include: shared/frontend/scope.md -->

<!-- include: shared/frontend/common-tasks.md -->

---

## 技术栈

### 前端
- React 19 + TypeScript + Vite
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
- 发现可以复用已有页面基座、组件、hook / store、请求封装或工具函数时，应优先建议复用
- 不确定交互规则、字段含义或前端数据来源时，应按根 `AGENTS.md` 的查证优先级处理，禁止凭经验补写规则

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

<!-- include: shared/frontend/react-hooks.md -->

<!-- include: shared/frontend/component-communication-react.md -->

<!-- include: shared/frontend/routing-management-react.md -->

<!-- include: shared/frontend/zustand.md -->

### 样式规则
- 优先使用 Ant Design 组件内置样式和 Design Token
- 自定义样式使用 CSS Modules 或全局 CSS，变量来自 `design-system.yaml`
- 禁止 inline style，禁止硬编码颜色值（如 `#1677ff`）
- 主题定制统一通过 `ConfigProvider` 的 `theme` 属性配置
- 响应式设计遵循移动优先原则，使用 Ant Design 的 Grid 系统（Row / Col）

<!-- include: shared/frontend/component-rules.md -->

### 请求规则
- 禁止在组件中直接调用后端 API，统一通过 `src/api/` 下的请求模块封装
- Axios 实例统一配置，拦截器处理 token、错误
- 业务常量不要散落在页面组件内部
- API 函数命名规范：`getXxx`、`createXxx`、`updateXxx`、`deleteXxx`

<!-- include: shared/frontend/code-organization.md -->

---

## 测试规则

<!-- include: shared/frontend/pre-commit-regression.md -->

<!-- include: shared/common/general-requirements.md -->

### 前端测试
- 前端暂时不考虑单元测试
- 但必须进行手动 smoke test 验证核心功能

<!-- include: shared/external-dependencies.md -->

<!-- include: shared/no-test-fallback.md -->

---
