---
rule_id: frontend-agents
version: 1.41.0
last_updated: 2026-09-02
dependencies: [agents-root]
---

# 前端规则

<!-- include: shared/frontend/scope.md -->

<!-- include: shared/frontend/common-tasks.md -->

---

## 技术栈

<!-- include: shared/frontend/stack-vue-element.md -->
- Axios 仅用于第三方 / 远端 API，同站数据无后端可调

### 数据来源
<!-- include: shared/frontend/no-backend-data.md -->

<!-- include: shared/frontend/design-system-usage-element-plus.md -->
<!-- include: shared/ui/design-system-liveness.md -->

<!-- include: shared/application/tech-choice.md -->

---

<!-- include: shared/frontend/advisory.md -->

---

## 推荐目录结构

若仓库尚未形成稳定结构，可优先参考以下组织方式；若仓库已有实现，以现状为准，不强制迁移。

```text
project-root/
├── public/
├── src/
│   ├── api/               # 按业务域拆分的请求模块（第三方 / 远端 API）
│   ├── components/
│   │   ├── common/        # 通用业务组件
│   │   └── layout/        # 布局组件
│   ├── views/             # 页面级组件（按业务域拆分）
│   ├── router/
│   │   ├── index.ts       # 路由配置
│   │   └── routes.ts      # 路由定义
│   ├── stores/            # Pinia stores（按业务域拆分）
│   ├── types/             # TypeScript 类型定义
│   ├── utils/             # 纯工具函数
│   ├── directives/        # 自定义指令
│   ├── hooks/             # 组合式函数
│   ├── config/
│   │   └── element-theme.ts  # Element Plus 主题配置
│   ├── App.vue
│   └── main.ts
├── index.html
├── package.json
├── vite.config.ts
└── tsconfig.json
```

---

## 前端规则

<!-- include: shared/frontend/composition-api.md -->

<!-- include: shared/frontend/component-communication-vue.md -->

<!-- include: shared/frontend/routing-management-vue.md -->

<!-- include: shared/frontend/pinia.md -->

### 样式规则
- 优先使用 Element Plus 组件内置样式和 class
- 自定义样式使用 SCSS，变量来自 `design-system.yaml`
- 禁止 inline style，禁止硬编码颜色值（如 `#1A90FF`）
- 响应式设计遵循移动优先原则，必须在不同断点下测试；存量样式文件已是桌面优先写法（max-width 查询）时，增量改动沿用该文件既有约定，不整体重写扩大 diff，新增文件仍遵循移动优先

<!-- include: shared/frontend/component-rules.md -->

<!-- include: shared/frontend/request-rules.md -->

<!-- include: shared/frontend/code-organization.md -->

---

## 测试规则

<!-- include: shared/frontend/pre-commit-regression.md -->

<!-- include: shared/common/general-requirements.md -->

<!-- include: shared/frontend/frontend-testing-minimal.md -->

<!-- include: shared/external-dependencies.md -->

<!-- include: shared/no-test-fallback.md -->

---
