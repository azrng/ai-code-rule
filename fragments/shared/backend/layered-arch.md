### 分层架构
```
Handler → Service → Repository → Model
```
- `Handler`：参数校验、调用 Service、返回结果
- `Service`：业务逻辑处理
- `Repository`：数据访问
- `Model`：GORM 实体与 DTO
