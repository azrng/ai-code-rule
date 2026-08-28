### Android 发布规则
- 签名密钥通过安全渠道管理，禁止提交到仓库
- `AndroidManifest.xml` 中的权限声明必须与实际使用一致，不多申请无用权限
- Target SDK Version 和 Minimum SDK Version 需与目标市场要求匹配
- ProGuard / R8 混淆规则如启用，需确认反射和序列化类不被错误混淆
