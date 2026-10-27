### Swagger 规范
- 使用 `Azrng.Swashbuckle` 包，禁止直接引用 `Swashbuckle.AspNetCore`。
- 服务注册：`services.AddDefaultSwaggerGen(title: "项目名称", showJwtToken: ...)`——`showJwtToken` 仅在认证方案就绪（已注册 JWT 认证并配置授权策略）后才传 `true`，无认证服务保持默认 `false`。`AddDefaultSwaggerGen` / `UseDefaultSwagger` 签名按锁定包版本核实（当前基线 Azrng.Swashbuckle 0.4.0），示例版本漂移时以锁定版本公开 API 与编译结果为准
- 中间件启用：`app.UseDefaultSwagger(onlyDevelopmentEnabled: true)`。
- 项目必须启用 XML 文档生成：`<GenerateDocumentationFile>true</GenerateDocumentationFile>`。
- Controller 和公开方法必须添加 XML 注释，Swagger 会自动展示；XML 注释内容统一使用中文。
- AppService / DomainService 的方法 XML 注释按 `代码注释规范` 处理（不依赖 Swagger 自动展示，但需保证可读与可维护）。
- 接口需通过 Swagger UI 独立验证，确保请求 / 响应结构正确。
