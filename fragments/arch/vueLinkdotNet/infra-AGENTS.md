---
rule_id: infra-agents
version: 1.15.0
last_updated: 2026-08-28
dependencies: [agents-root]
---

# 基础设施规则

<!-- include: shared/infra/scope.md -->

<!-- include: shared/infra/common-tasks.md -->

---

<!-- include: shared/infra/tech-choice.md -->

---

## 主动建议规则
- 发现环境变量、端口、Docker、构建打包脚本、CI/CD 或发布链路存在不一致、不可复现或文档缺失时，应主动提醒
- 涉及清理环境、删除数据、停止未知进程、修改部署配置等高风险操作时，只能提出建议，必须等待明确确认后执行
- 发现可以复用现有启动脚本、Dockerfile、Compose 或发布脚本时，应优先建议复用，不新增平行部署链路
- 不确定端口占用、外部服务地址或密钥来源时，应按根 `AGENTS.md` 的查证优先级处理，禁止凭经验写死配置

---

## 默认端口

| 服务 | 容器内端口 | 宿主机映射端口 |
| --- | --- | --- |
| 前端（Vue） | 3000 | 3000 |
| 后端（.NET） | 8080 | 8080 |
| 数据库（PostgreSQL） | 5432 | 5432 |
| 缓存（Redis，启用时） | 6379 | 6379 |

- Redis 仅在项目实际启用 `Common.Cache.Redis` 时才纳入 Compose 编排，并同样补充健康检查或启动等待策略。
- 若宿主机端口冲突，优先调整 Compose 映射端口（宿主机侧），容器内端口保持不变，并同步更新 `.env.example` 和前端配置。
- 前端 API 请求统一指向 `http://localhost:8080`，通过环境变量 `VITE_API_BASE_URL` 配置，不允许硬编码。
- 端口与 API 基址的唯一事实来源是 `design-system.yaml`，两处修改必须同步。

---

## Docker 规则

<!-- include: shared/infra/general-requirements.md -->

### Docker 交付规则
- 只要本次任务新增或修改了 Dockerfile、`docker-compose.yml`、运行端口、环境变量或启动命令，完成实现后应主动执行一次镜像构建和容器化启动验证；若因环境限制无法执行，必须明确说明
- 容器化验证至少包括：
  - 镜像构建是否成功
  - Compose 服务是否成功启动
  - 关键服务是否处于可用状态
  - 前后端核心访问链路是否可用
- 若发现宿主机端口冲突，不要直接停止未知本地进程；优先调整 Compose 映射端口，并同步更新文档和前端配置
- 对于依赖数据库或其他基础服务的容器，Compose 中应尽量补充健康检查或等价的启动等待策略
- 新增镜像构建链路时，应同步补充 `.dockerignore`

---

<!-- include: shared/infra/pre-commit-regression.md -->

<!-- include: shared/no-test-fallback.md -->
