### 前端
- React + TypeScript + Vite；版本以仓库锁定为准，不主动升级，新项目先联网检索当前最新正式版
- Tailwind CSS 4（唯一样式体系：CSS-first 配置，主题 token 集中在 `src/styles/globals.css` 的 `@theme`，不使用 tailwind.config.js）
- shadcn/ui（基于 Radix Primitive 的复制式组件，组件源码落在 `src/components/ui/`）+ `class-variance-authority` + `clsx` / `tailwind-merge`
- shadcn/ui 组件优先用 CLI（`npx shadcn@latest add <component>`）拉取到本地后按需定制，不手写已被 shadcn 覆盖的基础组件
- lucide-react（唯一图标库）
- React Router（页面与导航）
- Zustand（全局状态管理：用户信息、权限、通知等跨组件共享状态）
- Axios（HTTP 客户端）
- 图表库（ECharts / recharts 等）仅在实际存在图表需求时引入，默认不安装；引入后统一封装并接入 `design-system.yaml` token 主题
