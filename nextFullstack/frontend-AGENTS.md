# AGENTS.md

## 适用范围

- 作用域：Next.js 页面、组件、状态、路由、样式与前端测试规则
- 触发场景：涉及前端实现、页面交互、前端 smoke test 时阅读

---

## 技术栈

### 前端
- Next.js（App Router）+ React + TypeScript
- Tailwind CSS（唯一样式体系）
- `next/font`（统一字体接入）
- shadcn/ui + Radix UI（如仓库已使用则沿用；未使用时优先复用现有组件实现）
- lucide-react（唯一图标库，尺寸统一 18-24px）
- React Hook Form + Zod（表单与校验）
- Zustand（仅用于客户端跨组件共享状态）

---

## 推荐目录结构
若仓库尚未形成稳定结构，可优先参考以下组织方式；若仓库已有实现，以现状为准，不强制迁移。

```text
project-root/
├─ app/
│  ├─ (auth)/
│  │  └─ login/
│  │     └─ page.tsx
│  ├─ (dashboard)/
│  │  ├─ layout.tsx
│  │  ├─ page.tsx
│  │  └─ users/
│  │     ├─ page.tsx
│  │     └─ [id]/
│  │        └─ page.tsx
│  ├─ api/
│  │  └─ users/
│  │     └─ route.ts
│  ├─ loading.tsx
│  ├─ error.tsx
│  ├─ not-found.tsx
│  └─ globals.css
├─ src/
│  ├─ components/
│  │  ├─ common/
│  │  ├─ layout/
│  │  ├─ charts/
│  │  └─ ui/
│  ├─ features/               # 按业务域组织的页面片段、表单、表格、查询区
│  ├─ hooks/
│  ├─ stores/                 # Zustand，仅客户端共享状态
│  ├─ schemas/                # Zod schema
│  ├─ lib/
│  │  ├─ utils.ts
│  │  ├─ auth/
│  │  ├─ request/
│  │  └─ permissions/
│  ├─ server/
│  │  ├─ db/
│  │  ├─ repositories/
│  │  ├─ services/
│  │  ├─ actions/
│  │  ├─ integrations/
│  │  └─ errors/
│  ├─ types/
│  └─ config/
├─ prisma/
│  ├─ schema.prisma
│  └─ migrations/
├─ public/
├─ doc/
│  ├─ design/
│  ├─ devlog/
│  └─ requirement.md
├─ middleware.ts
├─ next.config.ts
├─ Dockerfile
├─ docker-compose.yml
├─ .env.example
├─ .gitignore
├─ AGENTS.md
├─ CLAUDE.md
├─ GEMINI.md
├─ design-system.yaml
├─ TASK.md
└─ TaskHistory.md
```

---

### 阶段 1 — 页面与交互实现（Claude Code 主导）
**触发条件**：用户发出“开始前端开发”或等价指令  
**入场要求**：阶段 0 设计文档已确认

**工作内容**：
1. 基于 Next.js App Router 实现页面、布局、组件和导航结构
2. 优先使用 Server Component 搭建页面骨架，必要时再下沉到 Client Component
3. 阶段内允许使用 mock 数据、假接口或静态数据文件，但页面交互必须完整可演示
4. 同步产出契约说明：字段定义、筛选条件、分页参数、表单校验规则、权限要求

**产物**：
- 可在浏览器中访问的页面
- 基础交互与页面状态
- 服务端开发所需的 schema / 契约说明

**门控规则**：
- 用户确认页面符合设计文档预期
- 页面交互逻辑经手工 smoke test 验证通过
- 满足以上条件后，才允许进入阶段 2

---

## 前端规则

### React / App Router 规范
- 页面、布局、模板统一使用 App Router 目录规范
- 默认使用 Server Component；只有需要交互时才使用 Client Component
- `use client` 只写在最小必要边界，不要整页下沉为客户端组件
- 表单优先使用 React Hook Form + Zod
- 页面状态优先使用 `loading.tsx`、`error.tsx`、`not-found.tsx`
- 复用逻辑使用 hooks，放在 `src/hooks/` 或按业务域收敛

### 组件通信规范
- 父传子：props
- 子传父：回调函数
- 跨组件共享的客户端状态：Zustand
- 仅 UI 级共享配置（主题、弹窗容器、局部上下文）可使用 React Context
- 避免 props drill 过深；但不要为了少量传值滥用全局状态

### 路由管理规范
- 页面路由即文件系统路由，目录命名必须表达业务语义
- 页面跳转优先使用 `next/link`
- 客户端跳转使用 `useRouter`（来自 `next/navigation`）
- 服务端重定向优先使用 `redirect`
- Route Group、Parallel Route、Intercepting Route 仅在明确有必要时使用

### 状态管理规范
- 服务端数据默认不存入 Zustand
- Server Component 优先直接读取服务层结果
- 客户端缓存若仓库已使用 TanStack Query，则沿用；否则按现有实现处理
- Zustand 仅用于以下场景：
  - 侧边栏折叠状态
  - 全局筛选条件
  - 通知、弹窗、轻量会话上下文
  - 跨页面但不适合放入 URL 的纯客户端状态

### 样式规则
- 统一使用 Tailwind CSS 与设计 token
- 字体统一通过 `next/font` 接入
- 优先复用项目现有 `ui` 组件、`cn()`、`cva` 或等价封装
- 禁止 inline style
- 禁止硬编码颜色值、阴影值、圆角值、z-index、断点
- 响应式遵循移动优先原则

### 组件规则
- 优先复用 `src/components/` 与 `src/features/` 现有组件
- 只有确认存在复用价值时，才新增共享组件
- 页面状态必须完整：`loading`、`empty`、`error`、`no-permission`
- 不要为单次需求过度抽象组件
- 除非仓库已在使用，否则不要新增新的 UI 组件库

### 请求与数据获取规则
- Server Component 内禁止为了取同站点数据再请求自己的 `/api`
- 页面读操作优先直接调用服务层函数
- 浏览器端访问同站点接口统一使用相对地址 `/api`
- 业务写操作优先 Server Action，成功后使用 `revalidatePath` / `revalidateTag`
- 客户端不得直接持有数据库连接、服务端密钥或服务端专用环境变量

---

## 测试规则

### 总体要求
- 影响行为的改动应优先补充或更新测试
- 若本次改动未补测试，必须在最终说明中写明原因和风险
- 测试应覆盖真实业务路径，而不是只验证静态渲染

### 前端测试
- 组件测试优先使用 Vitest + Testing Library
- 关键页面至少应完成一次手工或 Playwright smoke test
- Client Component 的交互、校验、禁用态、错误态应有验证

### 外部依赖与数据
- 测试中不应真实调用第三方服务，统一使用 mock
- 测试数据应最小化、可读、可重复执行
- 不要让测试依赖本地人工状态

### 无法执行测试时
- 必须说明未执行的测试类型
- 必须说明未执行原因
- 必须说明潜在影响范围与风险

---
