---
rule_id: infra-agents
version: 1.14.0
last_updated: 2026-08-27
dependencies: [agents-root]
---

# 基础设施规则

<!-- include: shared/infra/scope.md -->

<!-- include: shared/infra/common-tasks.md -->

---

## 部署方式

- 默认部署方式：Docker + Docker Compose
- 生产镜像优先采用多阶段构建
- 部署链路要可追踪，可定位到镜像版本和配置来源

<!-- include: shared/application/tech-choice.md -->

---

<!-- include: shared/infra/advisory-docker.md -->

---

## 默认端口

| 服务 | 容器内端口 | 宿主机映射端口 |
| --- | --- | --- |
| 后端（.NET） | 8080 | 8080 |
| 数据库（PostgreSQL） | 5432 | 5432 |
| 缓存（Redis，启用时） | 6379 | 6379 |

- Redis 仅在项目实际启用 `Common.Cache.Redis` 时才纳入 Compose 编排，并同样补充健康检查或启动等待策略。
- 若宿主机端口冲突，优先调整 Compose 映射端口（宿主机侧），容器内端口保持不变。

---

## Docker 规则

### 总体要求
- 优先复用现有 Docker 与 Compose 文件。
- 首次创建 `Dockerfile`、`docker-compose.yml`、`.dockerignore` 或其他部署文件前，必须把"将新增部署文件"作为独立一项向用户确认并获得同意；不得因为方案骨架或阶段计划里提及过就默认生成，用户确认后再创建。
- 不要把密钥写入镜像。
- 清理旧镜像时要保留最小回滚窗口。
- 依赖数据库或其他基础服务时，应补充健康检查或等价启动等待策略。

### Docker 交付规则
- 只要本次任务新增或修改了 Dockerfile、`docker-compose.yml`、运行端口、环境变量或启动命令，完成实现后应主动执行一次镜像构建和容器化启动验证；若因环境限制无法执行，必须明确说明。
- 容器化验证至少包括：
  - 镜像构建是否成功
  - Compose 服务是否成功启动
  - 关键服务是否处于可用状态
  - API 核心接口是否可访问
- 若发现宿主机端口冲突，不要直接停止未知本地进程；优先调整 Compose 映射端口，并同步更新文档。
- 新增镜像构建链路时，应同步补充 `.dockerignore`。


<!-- include: shared/infra/pre-commit-regression.md -->

<!-- include: shared/no-test-fallback.md -->

---
