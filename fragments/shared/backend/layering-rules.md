### 分层依赖规则
```
API → Application → Domain ← Infrastructure
                ↑_______________|
```
- `Domain` 层：无外部依赖，包含核心业务模型
- `Application` 层：依赖 Domain，定义服务接口和 DTO
- `API` 层：依赖 Application，处理 HTTP 请求
- `Infrastructure` 层：依赖 Application 和 Domain，实现具体技术细节
