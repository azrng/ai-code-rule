# 任务清单

> 只维护当前活跃任务和最近完成的任务，已完成超过 5 条自动删除最早的。

## 当前任务（进行中）

| ID | 任务名称 | 任务目标 | 当前阶段 | 负责人 | 状态 | 优先级 | 最近更新 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| （无） | | | | | | | |

## 最近完成任务

| ID | 任务名称 | 任务目标 | 当前阶段 | 负责人 | 状态 | 优先级 | 最近更新 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| RULE-20261027-SIMPLIFY | 规则体系复杂度精简 | 全删 target-list 机制；.NET 并行隔离下沉 .NET 载体片段；契约回退、POC 门控、暂存禁令、dev-stage 原型行去重瘦身；删 2 死 infra 文件；升 2.6.0 | 已完成并验证：238 项测试与基线全绿、--check 34 样例通过、emit 产物抽查口径正确 | Codex | DONE | 高 | 2026-10-27 |
| RULE-20261027-FEEDBACK | 承接 2026-10-27 六条规则效果反馈 | collaboration#Git、验证与交付：构造索引底稿取编辑后工作区（编辑前快照仅归因）；#并行会话隔离：check-ignore 排查静默忽略、隔离仅改 OutputPath 不动 obj；core-libs 存量例外（未引 Common.HttpClients 沿用原生 HttpClient）；ui 新增 browser-walkthrough 片段接两个 ui-AGENTS；mudblazor skill 补 MudDatePicker @bind-Date 陷阱；升 2.5.5 | 已完成并验证：--check 34 样例、238 项测试、基线刷新全绿、产物传导抽查通过 | Codex | DONE | 高 | 2026-10-27 |
| RULE-20261024-SKILL-DEL | 删除 proactive-agent 技能 | 删 skills/agentSkills/proactive-agent 及 manifest 登记、README 段落；升 2.5.4；--check 33 技能、238 测试、基线刷新通过 | 已完成并验证 | Codex | DONE | 中 | 2026-10-24 |
| RULE-20261024-FEEDBACK | 承接 2026-10-24 三条规则效果反馈 | 修 azrng-efcore 执行时序方向（IStartupFilter 按注册顺序执行）并补 42P01 症状映射；collaboration 补 OutputPath 相对目录限定与构造索引后禁 git add；升 2.5.3 | 已完成并验证：--check 34 样例、238 项测试、基线刷新后全绿 | Codex | DONE | 高 | 2026-10-24 |
| RULE-20261022-02 | collaboration 规则语言简化 | 8 节 40 条逐条压缩措辞不删约束，5114→4190 字；升版 2.5.2、基线刷新、计划关卡断言随措辞更新；34 组合与 238 测试通过 | 已完成并验证 | Codex | DONE | 高 | 2026-10-22 |
| RULE-20261022-01 | TASK.md 单元格内容边界约束 | 任务管理节新增名称 ≤25 字、目标 ≤80 字条目，升版 2.5.1 并刷新 emit 基线 | 已完成并验证 | Codex | DONE | 高 | 2026-10-22 |
