---
rule_id: frontend-agents
version: 1.0.0
last_updated: 2026-11-27
dependencies: [agents-root]
---

# 前端规则

<!-- include: shared/frontend/scope.md -->

---

## 技术栈

<!-- include: shared/frontend/stack-angular-zorro.md -->

### 数据来源
<!-- include: shared/frontend/no-backend-data.md -->

<!-- include: shared/frontend/design-system-usage-ng-zorro.md -->
<!-- include: shared/ui/design-system-liveness.md -->

<!-- include: shared/ui/spec-lock.md -->

---

<!-- include: shared/frontend/advisory.md -->

---

## 阶段 1 — 页面与交互实现

**入场要求**：阶段 0 设计文档已确认；本次改动命中原型跳过条件时直接实现，否则沉淀型真栈原型在阶段 1 内实现，其用户确认是阶段 1 的首个门控（确认时点见「原型设计与确认」）

<!-- include: shared/ui/prototype-workflow.md -->

<!-- include: shared/ui/mock-boundary.md -->

**工作内容**：
1. 基于 Angular CLI 工程实现页面、布局、组件和导航结构。
2. 先搭建页面骨架、数据状态和交互闭环，再补细节优化。
3. 数据层按无后端口径执行：本地静态 / mock 数据、本地存储或第三方 / 远端 API，页面交互必须完整可演示。
4. 同步产出数据口径说明：字段定义、筛选条件、分页参数、表单校验规则。

**门控规则**：
- 用户确认页面符合设计文档预期。
- 页面交互逻辑经手工 smoke test 验证通过。
- 数据口径、字段说明和类型定义已初步定稳；后续发现缺口时按 `collaboration-AGENTS.md`「纠错、回退与重构」处理。
- 视觉验收按「视觉验收」小节执行，用户终审通过前不得进入阶段 2。

<!-- include: shared/ui/visual-acceptance.md -->

---

## 推荐目录结构

<!-- include: shared/common/structure-preamble.md -->

文件命名跟随项目所用 Angular 版本的 `ng generate` 默认约定（新式无后缀或存量 `.component.ts` 后缀），同一项目只用一套。

```text
project-root/
├── src/
│   ├── app/
│   │   ├── core/                 # 单例基础设施：拦截器、鉴权、环境配置、应用壳
│   │   │   ├── api/              # 请求封装（拦截器、响应包装、按业务域拆分的接口文件）
│   │   │   └── layout/           # 应用壳、菜单、面包屑
│   │   ├── features/             # 业务域（懒加载边界），每域一个目录
│   │   │   └── <domain>/
│   │   │       ├── <domain>.routes.ts     # 本域路由
│   │   │       ├── pages/                 # 页面级组件（ts / html / scss 同级）
│   │   │       ├── components/            # 域内组件
│   │   │       ├── <domain>-store.ts      # 域状态（信号服务 Store）
│   │   │       ├── <domain>.service.ts    # 数据服务（Fake / 真实实现经接口注入）
│   │   │       └── <domain>.model.ts      # 类型 / 模型
│   │   ├── shared/               # 跨域复用
│   │   │   ├── components/       # 通用业务组件
│   │   │   ├── directives/
│   │   │   ├── pipes/
│   │   │   ├── stores/           # 跨业务域共享的 Store
│   │   │   └── utils/            # 纯工具函数
│   │   ├── app.config.ts         # 应用配置（provideRouter / provideHttpClient / 提供者）
│   │   ├── app.routes.ts         # 根路由表
│   │   └── app.ts                # 根组件
│   ├── environments/             # environment.ts / environment.<env>.ts
│   ├── assets/
│   ├── styles.scss               # 全局样式 + token CSS 变量
│   ├── index.html
│   └── main.ts
├── angular.json
├── package.json
├── tsconfig.json
├── doc/
│   ├── design/
│   └── requirement.md
├── .gitignore
├── AGENTS.md
├── CLAUDE.md
├── GEMINI.md
├── GROK.md
├── design-system.yaml
└── TASK.md
```

---

## 前端规则

<!-- include: shared/frontend/angular-components.md -->

<!-- include: shared/frontend/angular-state.md -->

<!-- include: shared/frontend/angular-routing.md -->

### 样式规则
- 优先使用 NG-ZORRO 组件内置样式和 class
- 自定义样式使用组件级 SCSS，变量来自 `design-system.yaml`
- 禁止 inline style，禁止硬编码颜色值（如 `#1A90FF`）
- 响应式设计遵循移动优先原则，必须在不同断点下测试；存量样式文件已是桌面优先写法（max-width 查询）时，增量改动沿用该文件既有约定，不整体重写扩大 diff，新增文件仍遵循移动优先

<!-- include: shared/frontend/component-rules.md -->

<!-- include: shared/frontend/angular-http.md -->

<!-- include: shared/frontend/contract-type-rules.md -->

### 代码组织规范
- 一个文件只放一个主对象：组件、服务、Store、指令、管道、工具函数各自独立文件，文件名与导出的主对象一致
- 组件职责单一：容器组件只负责编排，展示型子组件、无 DOM 的纯函数（数据加工 / 校验）与数据服务分开；发现持续膨胀或多个不相关职责堆在同一组件时，必须拆分
- 触发拆分的信号：单组件出现多个不相关业务、输入输出持续堆叠、模板过长难以定位、阅读成本明显升高
- 允许例外：仅服务当前组件的局部子组件、紧耦合的类型定义、测试文件中的 fixture
<!-- include: shared/application/comment-discipline.md -->

---

## 测试规则

<!-- include: shared/frontend/pre-commit-regression.md -->
- Angular 模板类型错误只在 AOT 编译期暴露：涉及模板或组件元数据改动时，提交前执行 `ng build`；新项目同时执行 `ng lint`，存量项目沿用其既有 lint 脚本

<!-- include: shared/frontend/general-requirements.md -->

<!-- include: shared/frontend/frontend-testing-minimal.md -->

<!-- include: shared/external-dependencies.md -->

<!-- include: shared/no-test-fallback.md -->

<!-- include: shared/application/anti-patterns.md -->

---
