### 状态与变更检测规范
- 组件局部状态用 `signal` / `computed`，派生状态一律 `computed`，副作用才用 `effect`
- 跨组件共享状态放信号服务 Store：`providedIn: 'root'` 的服务持有 signal / computed，按业务域拆分文件；对外 signal 用 `asReadonly()` 只读暴露，修改只经 Store 方法
- 异步数据（HTTP / 定时器 / 第三方回调）落地为 signal 更新，组件销毁时中止未完成请求（`takeUntilDestroyed` / `DestroyRef`）；非 Angular 渲染的第三方库（画布、地图、播放器）：zone.js 项目在 `NgZone.runOutsideAngular` 内挂载并接收高频事件，回写 Angular 状态一律用 signal 赋值
- 存量写法（≤ 18，zone.js）：模板优先 `| async`；手动订阅必须 `takeUntil(this.destroy$)` 并在 `ngOnDestroy` 里 `next()` + `complete()`，禁止留下未取消的订阅；共享状态用 `providedIn: 'root'` 服务 + `shareReplay(1)` 缓存流或 `BehaviorSubject`；新组件优先 `ChangeDetectionStrategy.OnPush`
