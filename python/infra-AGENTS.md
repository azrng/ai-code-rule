---
rule_id: infra-agents
version: 1.5.0
last_updated: 2026-07-26
dependencies: [agents-root]
---

# 基础设施规则

## 适用范围

- 作用域：端口、环境变量、Docker、构建发布与交付验证规则
- 触发场景：涉及 Docker、端口、环境变量、启动方式、构建打包、部署链路时阅读

### 阅读摘要
- 建议阅读：改 Dockerfile、Compose、环境变量、运行端口、启动方式、发布链路
- 可先跳过：纯业务逻辑、仅设计文档维护
- 优先查看：默认端口、交付规则、`提交前最小回归`

### 常见任务入口
- 改容器或构建配置：先看 Docker / 构建规则
- 改端口、环境变量、连接串：先看默认端口与配置约束
- 改启动命令、发布或打包链路：先看交付规则与验证要求
- 补交付回归：先看 `提交前最小回归`

---

## 技术选择原则
如果仓库已经有真实实现，以现有代码为准，不要强行重构或替换技术栈。
技术债务与重构判断遵循根 `AGENTS.md` 的全局规则。

---

## 主动建议规则
- 发现环境变量、端口、Docker、构建脚本、启动命令或部署链路存在不一致、不可复现或文档缺失时，应主动提醒
- 发现可以复用现有 Dockerfile、Compose、环境变量命名、发布脚本或健康检查方式时，应优先建议复用，不新增平行部署链路
- 不确定端口占用、外部服务地址、密钥来源或运行命令时，应按根 `AGENTS.md` 的查证优先级处理，禁止凭经验写死配置
- 涉及清理环境、删除数据、停止未知进程、修改部署配置等高风险操作时，只能提出建议，必须等待明确确认后执行

---

## 六、Docker 规范

### 6.1 Dockerfile

- 使用多阶段构建
- 基础镜像选择 `python:3.12-slim`
- 使用 `non-root` 用户运行
- 不在镜像中包含密钥或敏感信息
- 安装依赖时使用 `--no-cache-dir`

```dockerfile
FROM python:3.12-slim AS builder
WORKDIR /app
COPY pyproject.toml ./
RUN pip install --no-cache-dir -e .
# 或使用 uv：COPY --from=ghcr.io/astral-sh/uv:latest /uv /usr/local/bin/uv
# RUN uv sync --no-dev --frozen

FROM python:3.12-slim
WORKDIR /app
COPY --from=builder /usr/local/lib/python3.12/site-packages /usr/local/lib/python3.12/site-packages
COPY --from=builder /usr/local/bin /usr/local/bin
COPY app/ ./app/
COPY migrations/ ./migrations/
EXPOSE 8000
CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "8000"]
```

### 6.2 Docker Compose

- 包含 `app`、`db`（PostgreSQL）、`redis` 服务
- 配置健康检查（`healthcheck`）
- 端口冲突时调整宿主机端口映射
- 实现后必须执行构建并验证容器启动

---

### 提交前最小回归
- 修改 Dockerfile、Compose、启动命令、打包脚本或端口映射时：至少执行一次构建与启动验证
- 修改环境变量、配置项或连接串时：确认示例配置、文档和运行方式同步更新
- 修改数据库、仓储、依赖注入或外部服务集成时：至少补一项真实链路或等价验证
- 若本次改动同时影响应用访问链路，应至少补一次核心链路检查
