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

---

<!-- include: shared/infra/advisory-docker.md -->

---

<!-- include: shared/infra/default-ports.md -->

---

## Docker 规则

<!-- include: shared/infra/general-requirements-v1.md -->

### Docker 交付规则
- 只要本次任务新增或修改了 Dockerfile、`docker-compose.yml`、运行端口、环境变量或启动命令，完成实现后应主动执行一次镜像构建和容器化启动验证；若因环境限制无法执行，必须明确说明。
- 容器化验证至少包括：
  - 镜像构建是否成功
  - Compose 服务是否成功启动
  - 关键服务是否处于可用状态
  - API 核心接口是否可访问
- 若发现宿主机端口冲突，不要直接停止未知本地进程；优先调整 Compose 映射端口，并同步更新文档。
- 新增镜像构建链路时，应同步补充 `.dockerignore`。

### 本地数据安全规则
- 只读取和修改当前任务明确相关的本地文件、配置、数据库、容器挂载目录和导出目录；不得扫描或处理无关用户目录。
- 日志、错误信息、开发记录和提交说明不得写入密钥、真实凭据、连接串、个人隐私数据或完整敏感路径。
- 涉及数据库、卷、缓存、导出文件、镜像或容器清理时，必须先确认影响范围、备份方式和恢复方案。
- 迁移、初始化或容器化验证应保留可重复执行和可追踪记录，避免依赖人工准备的本地状态。


<!-- include: shared/infra/pre-commit-regression.md -->

<!-- include: shared/no-test-fallback.md -->

---
