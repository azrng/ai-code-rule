# 任务清单

> 只维护当前活跃任务和最近完成的任务，已完成超过 5 条自动删除最早的。

## 当前任务（进行中）

| ID | 任务名称 | 任务目标 | 当前阶段 | 负责人 | 状态 | 优先级 | 最近更新 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| （无） | | | | | | | |

## 最近完成任务

| ID | 任务名称 | 任务目标 | 当前阶段 | 负责人 | 状态 | 优先级 | 最近更新 |
| RULE-20261111-FIELDNAMING-SPREAD | 私有依赖字段条款传导至全部含 DI 后端模板 | 反馈「_domain/_service/_sso 泛称无约定」已于 3.0.3 落地（field-naming.md 第 5 行 + onion 命名表），但片段只挂 dotnet-layered/core-single 两模板，其余含 di-rules 的 .NET 后端组合零覆盖致复发感知；field-naming include 补挂 reactLinkdotNet / vueLinkdotNet / reactLinkdotNet-ai / dotnetBlazor（BlazorWebApp 共用其源）/ web-dotnet backend-single 骨架，删除 vueLinkdotNet 阶段2 逐字节旧版内联拷贝（已漂移）；条款在全部含 DI 后端产物 11/11 组合生效；升 3.3.5 | 已完成并验证：--check 36 样例、测试与基线刷新全绿；干净缓存 emit 复核条款 11 组合命中、内联旧拷贝残留 0 | omp | DONE | 高 | 2026-11-11 |
| RULE-20261109-FEEDBACK | 承接 2026-11-09 九条规则效果反馈 | onion 补拆分既有项目三态；collaboration 补导入比对与 version 语义；di-rules 组合根以 onion Startup 分工为准；mudblazor 补审计/预渲染/弹层/样式落点；backend-testing 补空库 smoke；升 3.3.4 | 已完成并验证：--check 36 样例、241 测试与基线刷新全绿；emit 传导核对（collaboration 36、组合根 11、空库 smoke 9 组合，旧口径残留 0） | omp | DONE | 高 | 2026-11-09 |
| RULE-20261104-RCSI-FENCE | 读事务判据补 RCSI 限定与浓缩版栅栏条款 | 全量版（shared/backend/read-transaction-rules.md）与浓缩版（data-access-rules.md）的单语句快照口径补数据库限定：PG/MySQL 恒成立、SQL Server 需开 RCSI（默认锁定读长扫描仍可能撕裂）；浓缩版补「NEVER 重读栅栏首读与复检同快照事务」对齐全量版五条判据；onion skill 速查①同步限定；升 3.0.5 | 已完成并验证：--check 36 样例、240 测试与基线刷新全绿；emit 复核 RCSI 限定与禁同快照各落 11 组合（2 全量 + 9 浓缩），与读事务挂载面一致 | ZCode | DONE | 中 | 2026-11-04 |
| RULE-20261104-DEDUP-HO | 读事务判据抽 shared 去重 + Adapter Ho 红线上移 rule | dotnet-layered / dotnet-core-single 两处逐字节相同的读事务全量判据抽为 shared/backend/read-transaction-rules.md，两 arch 改 include 引用（产物逐字节等价，判据从 4 处维护收敛为 shared 单处）；dotnet-layered Adapter 小节补「Ho 不得离开 Adapter」红线一行（B 组口径：基线不因未导入技能失效，skill 侧保留 Bo↔Ho 转换操作细节）；升 3.0.4 | 已完成并验证：--check 36 样例、测试与基线刷新全绿；emit 复核读事务判据仍 11 组合且内容逐字节不变、core-single 产物哈希未漂移（include 等价性实证）、Ho 红线落 dotnet-layered 组合 | ZCode | DONE | 中 | 2026-11-04 |
| RULE-20261104-FEEDBACK | 承接 2026-11-04 四条规则效果反馈 | collaboration#Git、验证与交付：基线既有失败时以基线对照为等价交付门（worktree 复跑、列既有失败清单、不豁免不顺手修）+backend 回归片段指向；dotnet-layered/core-single EF 规则与 data-access-rules 补读事务判据（单语句读不包、跨语句一致优先折单语句/CAS/栅栏、RR 需说出不变量、禁外呼入读事务、禁首读复检同快照）；field-naming+onion 命名表补私有依赖字段禁泛称；onion skill 存储接口改 Core 口径+Ho 不出 Adapter 红线；升 3.0.3 | 已完成并验证：--check 36 样例、240 测试、基线刷新全绿、emit 传导抽查（collaboration 36、回归 19、读事务 11、字段命名 2 组合与挂载面一致） | omp | DONE | 高 | 2026-11-04 |
