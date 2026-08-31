---
rule_id: infra-agents
version: 1.21.0
last_updated: 2026-08-31
dependencies: [agents-root]
---

# 基础设施规则

<!-- include: shared/infra/scope.md -->

<!-- include: shared/infra/common-tasks.md -->

---

<!-- include: shared/application/tech-choice.md -->

---

## 主动建议规则
- 发现环境变量、端口、Docker 配置、构建脚本、部署脚本或发布链路存在不一致、不可复现或文档缺失时，应主动提醒
- 涉及清理环境、删除数据、停止未知进程、修改部署配置等高风险操作时，只能提出建议，必须等待明确确认后执行
- 发现可以复用现有启动脚本、Dockerfile、Compose 或发布脚本时，应优先建议复用，不新增平行部署链路
- 不确定端口占用、外部服务地址或密钥来源时，应按根 `AGENTS.md` 的查证优先级处理，禁止凭经验写死配置

## 技术栈

### 部署
- Docker + Docker Compose
- 数据库：PostgreSQL

### 默认端口
| 服务                 | 容器内端口 | 宿主机映射端口 |
| -------------------- | ---------- | -------------- |
| 前端（React）        | 3000       | 3000           |
| 后端（.NET）         | 8080       | 8080           |
| 数据库（PostgreSQL） | 5432       | 5432           |
| 缓存（Redis，启用时） | 6379       | 6379           |

- Redis 仅在项目实际启用 `Common.Cache.Redis` 时才纳入 Compose 编排，并同样补充健康检查或启动等待策略。
- 若宿主机端口冲突，优先调整 Compose 映射端口（宿主机侧），容器内端口保持不变
- 前端 API 请求统一指向 `http://localhost:8080`，通过环境变量 `VITE_API_BASE_URL` 配置，不允许硬编码

---

## Docker 规则

<!-- include: shared/infra/general-requirements.md -->

<!-- include: shared/infra/docker-delivery-web.md -->

<!-- include: shared/infra/pre-commit-regression.md -->

<!-- include: shared/no-test-fallback.md -->

---
