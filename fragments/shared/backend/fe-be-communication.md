### 前后端通信
- **Wails 原生绑定**：Go 方法通过 `wails:generate` 暴露给前端 JS/TS 调用
- 禁止使用 HTTP 请求与 Go 后端通信（Wails 不是 B/S 架构）
- 如需调用外部 API，使用 Axios 通过前端发起
