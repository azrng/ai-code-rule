---
rule_id: infra-agents
version: 1.21.0
last_updated: 2026-08-31
dependencies: [agents-root]
---

# 基础设施规则

<!-- include: shared/infra/scope.md -->

---

<!-- include: shared/infra/advisory-docker.md -->
---

## 部署方式

- 默认部署方式：Docker + Docker Compose
- 数据库：PostgreSQL（可选）
- 生产镜像优先采用多阶段构建
- 部署链路要可追踪，可定位到镜像版本和配置来源

---

## 默认端口

| 服务 | 容器内端口 | 宿主机映射端口 |
| --- | --- | --- |
| Blazor Server 应用 | 8080 | 8080 |
| 数据库（PostgreSQL） | 5432 | 5432 |
| 缓存（Redis，启用时） | 6379 | 6379 |

- Redis 仅在项目实际启用 `Common.Cache.Redis` 时才纳入 Compose 编排，并同样补充健康检查或启动等待策略。
- 若宿主机端口冲突，优先调整 Compose 映射端口（宿主机侧），容器内端口保持不变。

---

## Docker 规则

<!-- include: shared/infra/general-requirements.md -->

<!-- include: shared/infra/docker-delivery-web.md -->

<!-- include: shared/infra/pre-commit-regression.md -->

<!-- include: shared/no-test-fallback.md -->

---
