### 前端
- React + TypeScript + Vite
- Tailwind CSS 4（唯一样式体系：CSS-first 配置，主题 token 集中在 `src/styles/globals.css` 的 `@theme`，不使用 tailwind.config.js）
- Radix UI Primitive + `class-variance-authority` + `clsx` / `tailwind-merge`（推荐的组件基础设施）
- React Router（页面与导航）
- TanStack Query（异步数据状态）
- React Hook Form + Zod（表单与校验）
- Zustand（仅用于客户端跨组件共享状态）
- 图表库仅在实际存在图表需求时引入，默认不安装；引入后统一封装并接入 `design-system.yaml` token 主题
