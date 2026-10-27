---
rule_id: infra-agents
version: 1.32.0
last_updated: 2026-09-02
dependencies: [agents-root]
---

# 基础设施规则

<!-- include: shared/infra/scope.md -->

---

## 部署方式

- 默认部署方式：Docker + Docker Compose
- 生产镜像优先采用多阶段构建
- 部署链路要可追踪，可定位到镜像版本和配置来源

<!-- include: shared/infra/advisory-docker.md -->

---

<!-- include: shared/infra/default-ports.md -->

---

## Docker 规则

<!-- include: shared/infra/general-requirements.md -->

<!-- include: shared/infra/docker-delivery-web.md -->

<!-- include: shared/infra/local-data-safety.md -->

---

## 阶段 2 — 验证与交付

**触发条件**：用户发出「开始验证」「开始交付」或等价指令；未明确发出时，阶段 1 门控全部满足后按 `collaboration-AGENTS.md`「Git、验证与交付」继续收口

**入场要求**：阶段 1 门控已全部满足；发现的契约缺口均已按 `collaboration-AGENTS.md`「纠错、回退与重构」处理完毕

**工作内容**：
1. 按改动类型执行本文件与 `backend-AGENTS.md` 的「提交前最小回归」对应项。
2. 构建与启动验证：构建通过、应用可正常启动、关键接口可访问。
3. 本次任务涉及 Dockerfile、Compose、端口、环境变量或启动命令时，按「Docker 交付规则」补容器化验证。
4. 按 `collaboration-AGENTS.md`「Git、验证与交付」汇总交付说明：已执行与未执行的校验、风险与待确认项。

**门控规则**：
- 构建、启动与关键链路验证通过；未执行的验证项已说明原因、影响范围和建议下一步。
- 文档、`TASK.md` 与示例配置已同步，环境变量新增项已同步到 `.env.example` 或对应配置示例。
- 按 `collaboration-AGENTS.md`「Git、验证与交付」执行提交口径：验证通过后默认提交本任务改动且不自动推送，用户明确要求暂缓或禁止提交时不提交。

---

<!-- include: shared/infra/pre-commit-regression.md -->

<!-- include: shared/no-test-fallback.md -->

---
