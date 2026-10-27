- Redis 仅在项目实际启用 `Common.Cache.Redis` 时才纳入 Compose 编排，并同样补充健康检查或启动等待策略。
- 前端 API 请求统一指向 `http://localhost:8080`，通过环境变量 `VITE_API_BASE_URL` 配置，不允许硬编码。
- 端口与 API 基址的唯一事实来源是 `design-system.yaml`，两处修改必须同步。

---

## Docker 规则

<!-- include: shared/infra/general-requirements.md -->

<!-- include: shared/infra/docker-delivery-web.md -->

---

<!-- include: shared/infra/pre-commit-regression.md -->

<!-- include: shared/no-test-fallback.md -->
