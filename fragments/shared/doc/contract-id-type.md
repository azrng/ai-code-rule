### 雪花 id 的契约类型
- 契约草案（`CON-`）与接口说明的字段表里，雪花 id 及其他可能超出 JS 安全整数（2^53）的 `long` 字段，类型一律记 `string`，不记 `number` / `long`：浏览器 `JSON.parse` 会把超出 2^53 的数字静默舍入（`…544` → `…500`），契约写成数字等于让前端按错误类型先行开发，联调才暴露且全链路返工。
- Swashbuckle 不反映 long→string 转换器，Swagger 会把这些 id 继续展示为 `integer`：按 Swagger 校准契约时，这类字段以实际响应为准，不得照 Swagger 回改为数字。
