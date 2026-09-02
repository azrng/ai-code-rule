---
rule_id: infra-agents
version: 1.38.0
last_updated: 2026-09-02
dependencies: [agents-root]
---

# 基础设施规则

<!-- include: shared/infra/scope.md -->

<!-- include: shared/infra/common-tasks.md -->

---

<!-- include: shared/application/tech-choice.md -->

---

## 主动建议规则
- 发现环境变量、端口、Docker、构建打包脚本、CI/CD 或发布链路存在不一致、不可复现或文档缺失时，应主动提醒
- 涉及清理环境、删除数据、停止未知进程、修改部署配置等高风险操作时，只能提出建议，必须等待明确确认后执行
- 发现可以复用现有启动脚本、Dockerfile、Compose 或发布脚本时，应优先建议复用，不新增平行部署链路
- 不确定端口占用、外部服务地址或密钥来源时，应按根 `AGENTS.md` 的查证优先级处理，禁止凭经验写死配置

---

## 技术栈

### 部署
- Docker + Docker Compose
- 数据库：PostgreSQL

---
