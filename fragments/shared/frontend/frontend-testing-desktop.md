### 前端测试
- 组件测试优先使用 Vitest + Testing Library
- Query、表单、交互、禁用态、错误态应有验证
- 仅在需要模拟外部请求时才引入 MSW
- 对外壳桥接调用（Electron IPC / Tauri Command）统一使用 mock 封装层，不直接 mock 页面内部实现细节
