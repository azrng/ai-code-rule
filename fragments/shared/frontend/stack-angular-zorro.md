### 前端
- Angular + TypeScript（Angular CLI 工程）；版本以仓库锁定为准，不主动升级，新项目先联网检索当前最新正式版
- 版本双轨，同一项目只用一套写法，禁止混搭或顺手迁移：`@angular/core` 主版本 ≥ 19 走现代写法（standalone、signals、`@if` / `@for` 新控制流、`inject()`、`input()` / `output()`）；≤ 18 沿用项目既有范式（NgModule 或早期 standalone、zone.js、RxJS 订阅，输入输出用 `@Input()` / `@Output()`），其中 17–18 可用 signals 与新控制流
- NG-ZORRO 组件库（唯一组件库）；项目已有公司 / 团队设计系统（如基于 NG-ZORRO 的内部包）时优先沿用
- 样式：组件级 SCSS + 全局 token，不引入 Tailwind 等其他样式体系
- Angular Router（页面与导航）；表单默认 Reactive Forms（类型化表单）
- 信号服务 Store（跨组件共享状态：用户信息、权限、通知等），不默认引入 NgRx / NGXS / Akita
- `HttpClient`（HTTP 客户端，Angular 内置，不引入 Axios）
- 图表库（ECharts + ngx-echarts）仅在实际存在图表需求时引入，默认不安装；引入后统一封装并接入 `design-system.yaml` token 主题
- 代码检查：新项目用 ESLint（`ng add @angular-eslint/schematics`），TSLint 已停止维护，不得用于新项目
