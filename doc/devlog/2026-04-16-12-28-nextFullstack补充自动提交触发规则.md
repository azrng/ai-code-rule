# 本次修改范围
- `nextFullstack/AGENTS.md`
- `doc/AI-Rules-Audit-Log.md`

# 大概变更内容
- 在 `nextFullstack/AGENTS.md` 的 `Git 与交付` 章节补充“提交触发规则”，明确当代码、配置或规则改动已完成、验证已完成、`TASK.md` 对应任务状态已更新为 `DONE` 后，默认自动执行一次提交。
- 同步补充自动提交前置条件与禁止自动提交场景，避免在未验证、未更新任务状态或用户明确要求暂不提交时误触发提交。
- 在“交付收口”清单中增加“满足提交触发规则时完成自动提交”，使交付闭环与 Commit 规则保持一致。
- 在 `doc/AI-Rules-Audit-Log.md` 中补充本次规则优化记录，并更新 `nextFullstack` 状态跟踪备注，保持可追溯。
