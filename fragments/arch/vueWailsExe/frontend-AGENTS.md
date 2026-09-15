---
rule_id: frontend-agents
version: 1.34.0
last_updated: 2026-09-02
dependencies: [agents-root]
---

# 前端规则

<!-- include: shared/frontend/scope.md -->

<!-- include: shared/frontend/common-tasks.md -->

---

## 技术栈

### 前端
- Vue 3 + TypeScript + Vite
- Element Plus 组件库
- Element Plus Icons（唯一图标库，尺寸统一 18-24px）
- Vue Router 4（路由管理）
- Pinia（全局状态管理：用户信息、权限、通知等跨组件共享状态）
- Axios（HTTP 客户端，仅用于外部 API 调用）

<!-- include: shared/frontend/design-system-usage-element-plus.md -->
<!-- include: shared/ui/design-system-liveness.md -->

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
│   ├── design/
│   └── requirement.md
├── build/                     # Wails 构建配置
│   └── windows/               # Windows 图标等资源
├── wails.json                 # Wails 项目配置
├── .gitignore
├── AGENTS.md
├── CLAUDE.md
├── GEMINI.md
├── GROK.md
├── design-system.yaml
└── TASK.md
```

---

### 阶段 1 — 视图实现（界面实现角色）

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

<!-- include: shared/frontend/composition-api.md -->

<!-- include: shared/frontend/component-communication-vue.md -->

<!-- include: shared/frontend/routing-management-vue.md -->

<!-- include: shared/frontend/pinia.md -->

<!-- include: shared/frontend/wails-binding-call.md -->

### 样式规则
- 优先使用 Element Plus 组件内置样式和 class
- 自定义样式使用 SCSS，变量来自 `design-system.yaml`
- 禁止 inline style，禁止硬编码颜色值（如 `#1A90FF`）
- 以桌面窗口为主要目标，不强制移动端响应式；窗口是否可调整以项目配置为准，可调窗口需处理窄宽度下的布局

<!-- include: shared/frontend/component-rules.md -->

<!-- include: shared/frontend/external-api-rules.md -->

<!-- include: shared/frontend/code-organization.md -->

---

## 测试规则

<!-- include: shared/frontend/pre-commit-regression.md -->

<!-- include: shared/backend/general-requirements.md -->

<!-- include: shared/frontend/frontend-testing-minimal.md -->

<!-- include: shared/no-test-fallback.md -->
