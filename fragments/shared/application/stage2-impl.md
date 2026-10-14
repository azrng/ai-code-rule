## 阶段 2 — 业务逻辑实现（业务实现角色主导）

**触发条件**：用户发出「开始业务逻辑开发」指令

**入场要求**：前端视图已完成，`src/AppName/Models/DTOs/` 契约文件已明确

**工作内容**：
1. 严格按照 DTO 中的类型定义实现 ViewModel 命令编排与 Service 层逻辑。
2. 遵循 `ViewModel → Service → Repository → Model` 完整分层。
3. 涉及数据访问或数据库结构变更时，结合 `infrastructure-AGENTS.md` 同步补齐仓储实现与迁移脚本。
4. 每个关键服务方法和关键 ViewModel 命令都应能通过测试独立验证。

**字段命名约定**：
- C# 类型 / 属性：PascalCase
- DTO / 序列化字段：遵循当前协议约定并保持一致
- 数据库存储字段和映射规则见 `infrastructure-AGENTS.md`

**门控规则**：
- 核心业务逻辑测试通过后，才允许进入阶段 3。
