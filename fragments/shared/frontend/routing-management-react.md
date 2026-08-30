### 路由管理规范
- 路由配置集中放在 `src/router/routes.tsx`
- 使用 React Router 6 的 `createBrowserRouter` + `RouterProvider`
- 路由守卫通过 `loader` 函数或 `ProtectedRoute` 包裹组件实现
- 路由跳转使用 `useNavigate()` hook
- 获取路由参数使用 `useParams()` 和 `useSearchParams()`
- 路由命名使用有意义的路径（如 `/users/list`、`/users/detail/:id`）
