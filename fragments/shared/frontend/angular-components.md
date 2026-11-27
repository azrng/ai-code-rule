### 组件与模板规范
- 新组件一律 standalone，按需导入模板用到的指令 / 组件 / 管道；依赖注入统一 `inject()`，输入输出用 `input()` / `output()` / `model()`，不在组件内改写收到的输入对象
- 模板只做展示与事件转发：禁止在模板绑定里调用返回新对象 / 数组的 getter 或方法并交给列表渲染（会触发无限变更检测），派生数据预先落为字段；列表渲染带 `track` / `trackBy`
- 父子交互优先用输入状态 + 输出事件，`@ViewChild` 只用于输入输出表达不了的命令式调用（如打开弹窗、聚焦）；复用走组合（子组件、内容投影、服务、无 DOM 的纯函数），禁止组件类继承组件类
- 表单默认 Reactive Forms（类型化），同一表单不与 `ngModel` 混用；提交中按钮置 loading 并禁用
- 存量写法（≤ 18）：沿用所在业务域既有的 NgModule 声明或 `standalone: true` + `imports`、`*ngIf` / `*ngFor`、`@Input()` / `@Output()` 与构造器注入；变更检测策略随所在业务域，可变数据配 `OnPush` 时变更后 `markForCheck()`
