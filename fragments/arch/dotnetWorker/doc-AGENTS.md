---
rule_id: doc-agents
version: 1.0.0
last_updated: 2026-11-20
dependencies: [agents-root]
---

# 文档规则

<!-- include: shared/doc/scope-api.md -->

---

<!-- include: shared/doc/advisory.md -->
- 发现实现方案与运维说明（安装步骤、配置说明、启停行为）不一致时，应主动提醒并建议同步修订
---

## 阶段 0 设计文档

<!-- include: shared/doc/stage0-head.md -->

**文档必须包含以下四个部分**：

| 部分 | 内容要求 |
| --- | --- |
| 需求追溯 | 关联 `REQ-` 编号、明确本次设计覆盖的范围和未覆盖项，不重复粘贴需求正文 |
| 技术设计 | 模块职责、作业清单（有哪些 Worker）、数据流向和技术决策说明 |
| 作业契约与配置项 | 每个作业的触发方式与节奏、输入输出、配置项（名称 / 默认值 / 是否必填）、启停与恢复行为 |
| 处理流程 | 核心处理链路、状态变化与游标、边界场景、异常与退避策略、验证方式 |

<!-- include: shared/doc/stage0-gate.md -->

<!-- include: shared/doc/design-checklist.md -->

---

<!-- include: shared/doc/doc-update.md -->

- 新增 / 变更 Worker、配置项、服务名、安装卸载步骤时，必须同步更新设计文档与 README 的运维小节；后台服务没有界面自证，文档与脚本是唯一的对外契约

<!-- include: shared/doc/readme.md -->

<!-- include: shared/doc/doc-directory-api.md -->

<!-- include: shared/doc/requirement-api.md -->

<!-- include: shared/doc/retro-format.md -->
