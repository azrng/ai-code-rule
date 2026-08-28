### Wails 绑定调用规范
- 通过 `src/bindings/` 下自动生成的模块调用 Go 方法
- 绑定方法返回 Promise，必须使用 `await` 或 `.then()/.catch()` 处理
- 禁止绕过 Wails 绑定直接通过 HTTP 请求与 Go 后端通信
- 绑定方法调用应封装在 hooks 或 store 中，不直接散落在页面组件内
- 修改 Go 方法签名后必须重新运行 `wails generate module`
