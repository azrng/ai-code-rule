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
- 发现环境变量、端口、Docker、构建脚本、启动命令或部署链路存在不一致、不可复现或文档缺失时，应主动提醒
- 发现可以复用现有 Dockerfile、Compose、环境变量命名、发布脚本或健康检查方式时，应优先建议复用，不新增平行部署链路
- 不确定端口占用、外部服务地址、密钥来源或运行命令时，应按根 `AGENTS.md` 的查证优先级处理，禁止凭经验写死配置
- 涉及清理环境、删除数据、停止未知进程、修改部署配置等高风险操作时，只能提出建议，必须等待明确确认后执行

---

## Docker 规范

### Dockerfile

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

### Docker Compose

- 包含 `app`、`db`（PostgreSQL）、`redis` 服务
- 配置健康检查（`healthcheck`）
- 端口冲突时调整宿主机端口映射
- 实现后必须执行构建并验证容器启动

---

<!-- include: shared/infra/pre-commit-regression.md -->

<!-- include: shared/no-test-fallback.md -->
