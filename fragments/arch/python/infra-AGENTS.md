---
rule_id: infra-agents
version: 1.33.0
last_updated: 2026-09-02
dependencies: [agents-root]
---

# 基础设施规则

<!-- include: shared/infra/scope.md -->

### 常见任务入口
- 改容器镜像或启动命令：先看 Docker 规范（Dockerfile / Docker Compose）
- 改端口、环境变量或连接串：先看默认端口与 Docker 规范，并同步 `.env.example` 与文档
- 补基础设施或交付回归：先看 `提交前最小回归`

---

<!-- include: shared/infra/advisory-docker.md -->
---

## 默认端口

| 服务 | 容器内端口 | 宿主机映射端口 |
| --- | --- | --- |
| 后端（uvicorn） | 8000 | 8000 |
| 数据库（PostgreSQL，启用时） | 5432 | 5432 |
| 缓存（Redis，启用时） | 6379 | 6379 |

- 数据库与 Redis 仅在项目实际启用对应能力时才纳入 Compose 编排，并同样补充健康检查或启动等待策略。
- 若宿主机端口冲突，优先调整 Compose 映射端口（宿主机侧），容器内端口保持不变。

---

## Docker 规范

### Dockerfile

- 使用多阶段构建
- 基础镜像选择 `python:<版本>-slim`，版本与项目 `pyproject.toml` 锁定的 Python 版本保持一致
- 使用 `non-root` 用户运行
- 不在镜像中包含密钥或敏感信息
- 安装依赖时使用 `--no-cache-dir`；builder 阶段只安装第三方依赖（`--no-install-project`），项目源码在运行阶段拷贝，避免 editable 安装在源码缺失时构建失败

```dockerfile
FROM python:3.12-slim AS builder
WORKDIR /app
COPY --from=ghcr.io/astral-sh/uv:latest /uv /usr/local/bin/uv
COPY pyproject.toml uv.lock ./
RUN uv sync --frozen --no-install-project --no-dev

FROM python:3.12-slim
WORKDIR /app
COPY --from=builder /app/.venv /app/.venv
ENV PATH="/app/.venv/bin:$PATH"
COPY app/ ./app/
# migrations 为可选目录：无数据库项目删除下一行
COPY migrations/ ./migrations/
RUN useradd -m appuser
USER appuser
EXPOSE 8000
CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "8000"]
```

### Docker Compose

- 默认包含 `app` 服务；`db`（PostgreSQL）与 `redis` 仅在项目实际启用数据库 / 缓存时纳入编排
- 纳入编排的服务配置健康检查（`healthcheck`）
- 端口冲突时调整宿主机端口映射
- 实现后必须执行构建并验证容器启动

---

## 阶段 2 — 验证与交付

**触发条件**：用户发出「开始验证」「开始交付」或等价指令；未明确发出时，阶段 1 门控全部满足后按 `collaboration-AGENTS.md`「Git、验证与交付」继续收口

**入场要求**：阶段 1 门控已全部满足；发现的契约缺口均已按 `collaboration-AGENTS.md`「纠错、回退与重构」处理完毕

**工作内容**：
1. 按改动类型执行本文件与 `backend-AGENTS.md` 的「提交前最小回归」对应项。
2. 构建与启动验证：构建通过、应用可正常启动、关键接口可访问。
3. 本次任务涉及 Dockerfile、Compose、端口、环境变量或启动命令时，执行镜像构建与容器化启动验证。
4. 按 `collaboration-AGENTS.md`「Git、验证与交付」汇总交付说明：已执行与未执行的校验、风险与待确认项。

**门控规则**：
- 构建、启动与关键链路验证通过；未执行的验证项已说明原因、影响范围和建议下一步。
- 文档、`TASK.md` 与 `.env.example` 等示例配置已同步。
- 按 `collaboration-AGENTS.md`「Git、验证与交付」执行提交口径：验证通过后默认提交本任务改动且不自动推送，用户明确要求暂缓或禁止提交时不提交。

---

<!-- include: shared/infra/pre-commit-regression.md -->

<!-- include: shared/no-test-fallback.md -->
