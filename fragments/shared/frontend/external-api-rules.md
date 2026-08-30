### 外部 API 请求规则
- 仅在需要调用外部服务时使用 Axios
- 禁止使用 Axios 调用本机 Go 后端（必须通过 Wails 绑定）
- Axios 实例统一配置，拦截器处理错误
- API 函数命名规范：`getXxx`、`createXxx`、`updateXxx`、`deleteXxx`
