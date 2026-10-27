### Zustand 状态管理规范
- Store 按业务域拆分，放在 `src/stores/` 目录
- 使用 `create` 函数定义 store
- Store 命名：`use[Domain]Store`（如 `useUserStore`、`usePermissionStore`）
- 修改 store 状态直接通过 set 方法，使用 immer 中间件处理不可变更新
- 异步操作直接在 store actions 或组件中通过 async/await 处理
