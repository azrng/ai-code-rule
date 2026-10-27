---
rule_id: infra-agents
version: 1.31.0
last_updated: 2026-09-02
dependencies: [agents-root, ai-agents]
---

# 基础设施规则

> 本规则：数据库 **SQLite 默认、PostgreSQL 可选**；Redis 不默认；含 **模型相关环境变量**（密钥、Base URL、模型名）。理念：第一版本地零依赖即可跑通，按需升级。

## 适用范围

- 作用域：端口、环境变量、模型密钥、Docker、构建发布与交付验证规则
- 触发场景：涉及 Docker、端口、环境变量、模型对接配置、启动方式、构建打包、部署链路时阅读

### 常见任务入口
- 改容器或构建配置：先看 Docker 规则与 Docker 交付规则
- 改端口、环境变量、模型密钥、连接串：先看默认端口与模型相关环境变量
- 切换数据库（SQLite ↔ PostgreSQL）：先看数据库与缓存策略
- 改启动命令、发布或打包链路：先看 Docker 交付规则
- 补交付回归：先看 `提交前最小回归`

---

## 技术选择原则
第一版优先保证本地零依赖可跑通；生产级基础设施（PostgreSQL、Redis、对象存储等）按需引入，不在 MVP 强加。

---

<!-- include: shared/infra/advisory-docker.md -->
- 发现环境变量、模型密钥、端口、Docker 配置、构建脚本、部署脚本或发布链路存在不一致、不可复现或文档缺失时，应主动提醒（含模型对接配置）
- 发现模型密钥、Base URL 硬编码在源码或下发前端时，必须提醒迁移到环境变量
## 技术栈

### 部署
- Docker + Docker Compose
- 数据库：SQLite 默认（文件持久化）；PostgreSQL 可选（生产 / 并发场景）

### 数据库与缓存策略
- **默认 SQLite**：无额外服务依赖，数据文件挂载到宿主机卷即可持久化；适合本地开发与 MVP
- **可选 PostgreSQL**：出现并发、并发写入、大容量或生产部署需求时切换；切换通过 EF Core Provider 与连接串完成
- **缓存（Redis）**：默认不部署；确有热点 / 会话缓存需求时再作为可选 Compose 服务加入
- Compose 中数据库服务按需启用；默认配置应让 `docker compose up` 只起前后端 + SQLite 文件即可用

### 默认端口
| 服务                 | 容器内端口 | 宿主机映射端口 |
| -------------------- | ---------- | -------------- |
| 前端（React + Vite） | 3000       | 3000           |
| 后端（.NET）         | 8080       | 8080           |
| 数据库（PostgreSQL，可选） | 5432  | 5432           |
| 数据库（SQLite，默认）    | —     | 文件卷挂载，无端口 |
| 缓存（Redis，启用时） | 6379       | 6379           |

- Redis 仅在项目实际启用 `Common.Cache.Redis` 时才纳入 Compose 编排，并同样补充健康检查或启动等待策略。
- 若宿主机端口冲突，优先调整 Compose 映射端口（宿主机侧），容器内端口保持不变
- 前端 API 请求统一指向 `http://localhost:8080`，通过环境变量 `VITE_API_BASE_URL` 配置，不允许硬编码

### 模型相关环境变量（AI 产品核心配置）
- 模型密钥、端点、模型名一律通过环境变量注入，不得入库、不得入镜像、不得下发前端
- 建议变量命名（实际以 `appsettings` 与 `AIOptions` 为准）：

| 变量名（示例） | 作用 | 是否必填 |
| -------------- | ---- | -------- |
| `AI__Provider` | 模型提供方（OpenAI / DeepSeek / Azure / GitHub / 本地） | 是 |
| `AI__Endpoint` | 模型 Base URL（兼容 OpenAI 接口） | 是 |
| `AI__ApiKey` | 模型密钥 | 是 |
| `AI__ModelId` | 默认模型名 | 是 |
| `AI__Temperature` | 采样温度 | 否 |
| `AI__MaxOutputTokens` | 单次最大输出 token | 否 |
| `AI__Timeout` | 调用超时（秒） | 否 |

- 新增模型相关变量时，必须同步更新 `.env.example`、`appsettings` 与本文件
- 多模型场景（对话模型 + Embedding 模型）按 `AI__*` / `Embedding__*` 前缀分组管理

---

## Docker 规则

<!-- include: shared/infra/general-requirements.md -->
- 部署链路要可追踪
- SQLite 默认场景下，数据文件通过卷挂载持久化，避免写进容器可写层

<!-- include: shared/infra/docker-delivery-web.md -->
- 模型对接配置的新增或修改同样触发上述容器化验证；验证清单额外包括：模型调用链路是否可达（密钥已注入，非真实业务调用可通过健康检查 / 日志确认配置加载）

<!-- include: shared/infra/pre-commit-regression.md -->

<!-- include: shared/no-test-fallback.md -->

---
