### 外部请求规则
- 本地能力（.NET sidecar、Rust 后端等）优先通过外壳桥接（Electron IPC、Tauri Command / plugin）调用，不通过本地 HTTP 绕路
- 如果需要访问外部服务，可使用 `fetch` 或统一封装的请求层
- 外部请求必须统一处理超时、错误和取消逻辑
- 若仓库已经使用 MSW，测试时优先通过 MSW 模拟外部请求
