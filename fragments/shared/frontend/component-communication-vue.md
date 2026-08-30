### 组件通信规范
- **父传子**：使用 `defineProps` 定义，只读不可修改
- **子传父**：使用 `defineEmits` 定义事件
- **跨级传值**：优先使用 Pinia，其次使用 `provide`/`inject`
- **兄弟组件**：通过共同父组件或 Pinia 中转
- 避免直接修改 props，需要修改时使用 v-model 或 emit
