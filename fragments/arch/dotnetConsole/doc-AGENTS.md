---
rule_id: doc-agents
version: 1.43.0
last_updated: 2026-09-03
dependencies: [agents-root]
---

# 文档规则

<!-- include: shared/doc/scope-api.md -->

---

<!-- include: shared/doc/advisory.md -->
- 发现实现方案与命令说明（CLI help、参数文档）不一致时，应主动提醒并建议同步修订
---

## 阶段 0 设计文档

<!-- include: shared/doc/stage0-head.md -->

**文档必须包含以下四个部分**：

| 部分 | 内容要求 |
| --- | --- |
| 需求追溯 | 关联 `REQ-` 编号、明确本次设计覆盖的范围和未覆盖项，不重复粘贴需求正文 |
| 技术设计 | 模块职责、命令结构、数据流向和技术决策说明 |
| 命令与输入输出 | 子命令、参数、选项、输出格式、退出码约定 |
| 处理流程 | 核心处理链路、状态变化、边界场景、异常处理和验证方式 |

<!-- include: shared/doc/stage0-gate.md -->

<!-- include: shared/doc/design-checklist.md -->

---

<!-- include: shared/doc/doc-update.md -->

<!-- include: shared/doc/doc-directory-api.md -->

<!-- include: shared/doc/requirement-api.md -->

<!-- include: shared/doc/retro-format.md -->
