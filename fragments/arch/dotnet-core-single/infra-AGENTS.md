---
rule_id: infra-agents
version: 1.15.0
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

<!-- include: shared/infra/default-ports.md -->

---

## Docker 规则

<!-- include: shared/infra/general-requirements.md -->

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
