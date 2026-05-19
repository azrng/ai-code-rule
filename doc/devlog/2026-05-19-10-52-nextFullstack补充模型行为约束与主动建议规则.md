# nextFullstack 补充模型行为约束与主动建议规则

- 修改范围：`nextFullstack/AGENTS.md`、`nextFullstack/frontend-AGENTS.md`、`nextFullstack/backend-AGENTS.md`、`nextFullstack/doc-AGENTS.md`、`nextFullstack/infra-AGENTS.md`
- 变更内容：基于 Code 模型能力评估清单，补充抗幻觉、先学后写、最小扰动、修 Bug 根因定位、架构边界判断、主动建议等执行约束。
- 元数据同步：上述规则文件 `version` 更新为 `1.3.0`，`last_updated` 更新为 `2026-05-19`。
- 校验重点：规则以行为约束形式落地，不把模型评估清单原文直接堆入模板；主动建议限定为风险提示和最小方案，避免自动扩大任务范围。
