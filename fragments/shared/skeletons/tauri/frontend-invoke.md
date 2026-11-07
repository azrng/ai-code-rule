### Tauri 前端调用规范
- 所有 Tauri 调用统一收敛在 `lib/tauri/` 或按业务域封装，不直接散落在页面组件中
- 优先使用 `invoke`、event、plugin API 的封装层，不在页面里直接拼 command 名称
- Command 参数与返回值应先经过 TypeScript 类型与 Zod schema 约束
- 除非明确需要，否则不要在 Tauri 桌面应用里再额外引入本地 HTTP 服务作为前后端桥接层
- 若确实需要流式处理、WebSocket、嵌入式代理等能力，再单独论证是否引入额外服务层
