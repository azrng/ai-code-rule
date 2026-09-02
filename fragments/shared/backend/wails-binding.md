### Wails 绑定规则
- 服务结构体通过 `application.NewService` 注册后，其导出方法（首字母大写）自动暴露给前端；不需要 `//export` 之类 cgo 注释
- 暴露给前端的方法必须有 Go doc 注释（会生成为前端 JSDoc 提示），注释内容统一使用中文
- 方法签名变更后必须运行 `wails3 generate bindings` 重新生成前端绑定（TypeScript 加 `-ts`，输出目录由项目 Taskfile 的 `-d` 配置决定）
- 前端绑定目录（如 `src/bindings/`）下的文件由 Wails 自动生成，禁止手动修改
- 方法参数与返回值不支持 chan、func、unsafe.Pointer 等不可序列化类型；返回 error 时前端 Promise 以 JS Error 对象 reject
