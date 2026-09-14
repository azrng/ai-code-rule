---
rule_id: infra-agents
version: 1.57.0
last_updated: 2026-09-07
dependencies: [agents-root]
---

# 基础设施规则

## 适用范围

- 作用域：端口、环境变量、Docker 与容器化交付规则
- 触发场景：涉及 Docker、端口、环境变量、部署链路、容器验证时阅读

---

## 技术栈

### 部署
- Docker
- Nuxt `nitro` 构建输出（`.output/`，按部署目标选择 preset）
- PostgreSQL

### 默认端口
| 服务 | 容器内端口 | 宿主机映射端口 |
| --- | --- | --- |
| Nuxt Web | 3000 | 3000 |
| PostgreSQL | 5432 | 5432 |

- 若宿主机端口冲突，优先调整 Compose 映射端口，容器内端口保持不变。
- 浏览器端同站点请求优先使用相对地址 `/api`，禁止在客户端代码中硬编码完整后端地址。
- 外部服务地址、数据库连接串、鉴权密钥等必须通过环境变量配置；可下发浏览器的变量统一加 `NUXT_PUBLIC_` 前缀。

<!-- include: shared/application/tech-choice.md -->

---

## 主动建议规则
- 发现环境变量、端口、Docker、构建脚本、启动命令或部署链路存在不一致、不可复现或文档缺失时，应主动提醒
- 发现可以复用现有 Dockerfile、Compose、环境变量命名或发布脚本时，应优先建议复用，不新增平行部署链路
- 不确定端口占用、外部服务地址、密钥来源或运行命令时，应先查证或列为待确认项，禁止凭经验写死配置
- 涉及清理环境、删除数据、停止未知进程、修改部署配置等高风险操作时，只能提出建议，必须等待明确确认后执行

---

## Docker 规则

<!-- include: shared/infra/general-requirements.md -->
- Nuxt 生产构建使用 `nuxt build`（Nitro 输出 `.output/`），容器入口运行 `.output/server/index.mjs`
- 部署链路必须可追踪、可复现

### Docker 交付规则
- 只要本次任务新增或修改了 Dockerfile、`docker-compose.yml`、运行端口、环境变量或启动命令，完成实现后应主动执行一次构建与启动验证；若环境限制无法执行，必须明确说明
- 容器化验证至少包括：
  - 镜像构建是否成功
  - Nuxt 服务是否成功启动
  - 数据库依赖是否可用
  - 核心访问链路是否可用
- 若发现端口冲突，不要直接停止未知本地进程；优先调整 Compose 映射端口，并同步更新文档和配置

<!-- include: shared/infra/local-data-safety.md -->

### 提交前最小回归
- 修改 Dockerfile、Compose、启动命令或端口映射时：至少执行一次构建与启动验证
- 修改环境变量时：确认 `.env.example`、文档和运行方式同步更新
- 若本次改动同时影响应用访问链路，应至少补一次容器内或启动后的核心链路检查

<!-- include: shared/no-test-fallback.md -->

---
