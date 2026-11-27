### 路由管理规范
- 路由配置集中：根路由表 `src/app/app.routes.ts`（`provideRouter`），各业务域的 `<domain>.routes.ts` 经 `loadChildren` / `loadComponent` 懒加载，页面组件不进首屏包
- 路由守卫统一用函数式守卫（`CanActivateFn` / `CanMatchFn` / `CanDeactivateFn`）；权限与菜单可见性在守卫与路由 `data` 里声明，不在页面组件里重复判断
- 路由跳转用 `routerLink` / `Router.navigateByUrl`；路由参数经 `withComponentInputBinding()` 绑定到组件 `input()`，或经 `ActivatedRoute` 读取
- 路由路径用有意义的小写 kebab-case（如 `/users/list`、`/users/:id`）；离开确认、页面保活（`RouteReuseStrategy`）等全局策略集中在一处注册并写明触发条件
- 存量写法（≤ 18）：沿用 `RouterModule.forRoot` / `forChild` 与既有守卫实现方式，不为统一写法批量迁移
