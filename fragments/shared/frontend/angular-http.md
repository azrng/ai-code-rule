### 请求规则
- 禁止在组件中直接注入 `HttpClient`，统一经 `src/app/core/api/` 或业务域数据服务封装；接口按业务域拆分文件，函数命名 `getXxx` / `createXxx` / `updateXxx` / `deleteXxx`
- API 地址只在一处配置（`src/environments/environment.ts`），禁止在业务代码散落地址字面量；token、错误处理与超时统一由拦截器承担（`provideHttpClient(withInterceptors([...]))`），调用方只处理业务错误
- 响应包装类型（如 `ApiResponse<T>`）及其解包函数全项目只声明一次，放共享模型目录，禁止各服务重复声明
- 环境差异经 `fileReplacements`（`environment.<env>.ts`）注入，禁止把密钥写进客户端可见配置
- 业务常量不要散落在页面组件内部
