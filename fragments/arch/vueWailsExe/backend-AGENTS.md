---
rule_id: backend-agents
version: 1.14.0
last_updated: 2026-08-27
dependencies: [agents-root]
---

# 后端规则

<!-- include: shared/backend/scope.md -->

<!-- include: shared/backend/common-tasks.md -->

---

<!-- include: shared/infra/tech-choice.md -->

---

## 主动建议规则
- 发现 Go 服务与数据访问职责混杂、绑定结构体定义不清或错误处理不一致时，应主动提醒
- 发现接口契约、绑定结构体、存储结构或状态流转可能影响历史数据、兼容性或权限安全时，必须先说明风险，不得直接扩大修改
- 发现可以复用既有 Go 服务、数据访问封装、绑定结构体或错误处理封装时，应优先建议复用
- 不确定业务规则、数据含义或 Wails 绑定行为时，应按根 `AGENTS.md` 的查证优先级处理，禁止按通用经验补写规则

---

## 技术栈

<!-- include: shared/backend/stack-backend-v2.md -->

<!-- include: shared/backend/fe-be-communication.md -->

---

### 阶段 2 — Go 后端实现（业务实现角色）

**触发条件**：用户发出「开始后端开发」指令

**入场要求**：前端页面已完成，接口需求已明确

**工作内容**：
1. 按照接口需求实现 Go Handler/Service/Repository
2. Handler 方法需要标记 `//export` 注释以暴露给前端
3. 运行 `wails generate module` 重新生成前端绑定
4. 涉及数据库结构变更时，使用 GORM AutoMigrate

**Go 方法导出规范**：
- 需要暴露给前端的方法必须在 App 结构体上定义
- 方法首字母大写（Go 导出规则）
- 方法参数和返回值必须是可序列化类型（string、int、struct、slice 等）
- 返回 error 时，前端会收到 JS Error 对象

**门控规则**：
- 核心方法独立测试通过后，才允许进入阶段 3

---

## Go 后端规则

<!-- include: shared/backend/layered-arch.md -->

<!-- include: shared/backend/handler-layer.md -->

<!-- include: shared/backend/service-layer.md -->

<!-- include: shared/backend/repository-layer.md -->

<!-- include: shared/backend/model-layer.md -->

<!-- include: shared/backend/database-rules.md -->

<!-- include: shared/backend/wails-binding.md -->

<!-- include: shared/backend/code-organization.md -->

---

## 测试规则

<!-- include: shared/backend/pre-commit-regression.md -->

<!-- include: shared/backend/general-requirements.md -->

### Go 后端测试（可选）
- 使用标准 `testing` 包
- Service 层测试 mock Repository
- Repository 层测试使用 SQLite 内存数据库

<!-- include: shared/no-test-fallback.md -->
