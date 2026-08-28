### React Hooks 规范
- 函数组件 + Hooks 是唯一写法，禁止 class 组件
- 响应式数据使用 `useState`，复杂状态使用 `useReducer`
- 计算属性使用 `useMemo`，回调缓存使用 `useCallback`
- 副作用使用 `useEffect`，清理逻辑在返回函数中处理
- 可复用逻辑封装为自定义 Hook（`useXxx`），放在 `hooks/` 目录
