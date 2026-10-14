### 组件通信规范
- **父传子**：通过 Props 传递，子组件只读不可修改
- **子传父**：通过回调函数 Props（`onXxx` 命名）
- **跨级传值**：优先使用 Zustand，其次使用 React Context
- **兄弟组件**：通过共同父组件或 Zustand 中转
- 避免直接修改 props，需要双向绑定时使用受控组件模式（value + onChange）
