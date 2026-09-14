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

<!-- include: shared/skeletons/web-node/infra-common-controls.md -->
- 外部服务地址、数据库连接串、鉴权密钥等必须通过环境变量配置；可下发浏览器的变量统一加 `NUXT_PUBLIC_` 前缀。

<!-- include: shared/application/tech-choice.md -->

---

<!-- include: shared/skeletons/web-node/infra-advisory.md -->

---

## Docker 规则

<!-- include: shared/infra/general-requirements.md -->
- Nuxt 生产构建使用 `nuxt build`（Nitro 输出 `.output/`），容器入口运行 `.output/server/index.mjs`
- 部署链路必须可追踪、可复现

<!-- include: shared/infra/docker-delivery-web.md -->

<!-- include: shared/skeletons/web-node/infra-delivery-tail.md -->

---
