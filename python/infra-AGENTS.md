# AGENTS.md

## 适用范围

- 作用域：Docker、运行环境与交付链路规则
- 触发场景：涉及 Docker、Compose、环境变量、运行方式与交付验证时阅读

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
