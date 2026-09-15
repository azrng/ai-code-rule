---
rule_id: doc-agents
version: 1.43.0
last_updated: 2026-09-03
dependencies: [agents-root]
---

# 文档规则

<!-- include: shared/doc/scope-api.md -->

### 常见任务入口
- 新增功能方案：先看阶段 0 设计文档
- 更新字段、接口、DTO 或 schema：先看契约说明规范
- 更新通用说明：先看文档目录约定

---

## 主动建议规则
- 发现需求描述缺少角色、流程、字段、边界场景、验收标准或非功能要求时，应主动补充为待确认项
- 发现实现方案与需求文档、命令说明或设计文档不一致时，应主动提醒并建议同步修订
- 维护文档时优先更新已有事实来源，不新增平行说明；确需新增文件时，应说明新增原因和归档位置
- 对不确定的业务事实、接口字段和运行方式，只能标记为待确认或引用已验证来源，禁止写成确定结论

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
