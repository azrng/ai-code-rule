---
rule_id: frontend-agents
version: 1.21.0
last_updated: 2026-08-31
dependencies: [agents-root]
---

# 前端规则

## 适用范围

- 作用域：前端实现、页面、组件、路由、状态、样式与前端测试
- 触发场景：涉及 React 页面、布局、组件、交互、样式、前端 smoke test 时阅读

### 常见任务入口
- 新增页面或布局：先看 React / 路由规范与组件规则
- 改表单、筛选、交互：先看表单规则、状态管理与数据获取规则
- 改样式、主题、组件展示：先看样式规则与设计系统约束
- 补前端回归：先看 `提交前最小回归` 与测试规则

---

## 技术栈

### 前端
- React + TypeScript + Vite
- Tailwind CSS（唯一样式体系）
- Radix UI Primitive + `class-variance-authority` + `clsx` / `tailwind-merge`（推荐的组件基础设施）
- React Router（页面与导航）
- TanStack Query（异步数据状态）
- React Hook Form + Zod（表单与校验）
- Zustand（仅用于客户端跨组件共享状态）
- `@tauri-apps/api`（与 Tauri Runtime 通信）

<!-- include: shared/frontend/testing.md -->

<!-- include: shared/frontend/design-system-usage.md -->

<!-- include: shared/frontend/advisory.md -->

---

## 推荐目录结构

若仓库尚未形成稳定结构，可优先参考以下组织方式；若仓库已有实现，以现状为准，不强制迁移。

```text
src/
├── app/                      # 路由与应用级入口
├── components/
│   ├── ui/                   # 基于 Radix / cva 的基础组件
│   ├── layout/               # 布局组件
│   └── business/             # 业务组件
├── features/                 # 按业务域收敛
├── hooks/                    # 自定义 Hook
├── routes/                   # 路由定义
├── lib/
│   ├── tauri/                # Tauri API 封装
│   ├── query/                # TanStack Query 封装
│   └── utils/                # 工具函数
├── stores/                   # Zustand store
├── schemas/                  # Zod schema
├── styles/                   # 全局样式与 token 映射
├── types/                    # TypeScript 类型
├── tests/                    # 前端测试辅助
├── main.tsx
└── App.tsx
```

---

## 阶段 1 — 前端实现

**触发条件**：用户发出“开始前端开发”或等价指令

**入场要求**：阶段 0 设计文档已确认

**工作内容**：
1. 基于 React + Vite 实现页面、布局、组件与导航结构。
2. 页面内允许先用 mock 数据、假 Command 响应或本地 fixture 演示交互。
3. 同步产出前端契约说明：字段定义、筛选条件、分页参数、表单校验规则、权限要求。
4. 只要仓库未明确采用别的组件体系，优先走“Tailwind + Radix / shadcn 风格组件”的收敛路线。

**门控规则**：
- 用户确认页面符合设计文档预期。
- 页面交互逻辑经手工 smoke test 或自动化验证通过。

---

## 前端规则

<!-- include: shared/frontend/react-routing.md -->

### 数据获取与状态管理
- 异步数据默认优先使用 TanStack Query
- Query Key 使用稳定命名，禁止在组件内部临时拼字符串做缓存键
- 写操作优先封装成 mutation，再由 mutation 触发刷新或失效处理
- Zustand 仅用于主题、窗口状态、轻量全局筛选、全局通知、临时会话上下文等纯客户端状态
- 禁止把“可由 Query 管理的服务端 / 本地命令数据”长期放进 Zustand

### Tauri 前端调用规范
- 所有 Tauri 调用统一收敛在 `lib/tauri/` 或按业务域封装，不直接散落在页面组件中
- 优先使用 `invoke`、event、plugin API 的封装层，不在页面里直接拼 command 名称
- Command 参数与返回值应先经过 TypeScript 类型与 Zod schema 约束
- 除非明确需要，否则不要在 Tauri 桌面应用里再额外引入本地 HTTP 服务作为前后端桥接层
- 若确实需要流式处理、WebSocket、嵌入式代理等能力，再单独论证是否引入额外服务层

<!-- include: shared/frontend/form-rules.md -->

<!-- include: shared/frontend/styling.md -->

<!-- include: shared/frontend/component-rules.md -->

<!-- include: shared/frontend/external-request-desktop.md -->

<!-- include: shared/frontend/code-organization.md -->

---

## 测试规则

<!-- include: shared/frontend/pre-commit-regression.md -->

<!-- include: shared/frontend/frontend-testing-desktop.md -->

<!-- include: shared/no-test-fallback.md -->

---
