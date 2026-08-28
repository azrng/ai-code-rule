### Wails 绑定规则
- 暴露给前端的方法必须有 Go doc 注释，注释内容统一使用中文
- 方法签名变更后必须运行 `wails generate module`
- 前端 `src/bindings/` 目录下的文件由 Wails 自动生成，禁止手动修改
- 方法参数不支持 chan、func、unsafe.Pointer 等类型
