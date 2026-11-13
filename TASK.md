# 任务清单

> 只维护当前活跃任务和最近完成的任务，已完成超过 5 条自动删除最早的。

## 当前任务（进行中）

| ID | 任务名称 | 任务目标 | 当前阶段 | 负责人 | 状态 | 优先级 | 最近更新 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| （无） | | | | | | | |

## 最近完成任务

| ID | 任务名称 | 任务目标 | 当前阶段 | 负责人 | 状态 | 优先级 | 最近更新 |
| RULE-20261113-ROLLBACK | 承接 2026-11-13 两条任务滚动反馈 | collaboration#任务管理新增「任务收尾与滚动一致性」条款（置 DONE 前核对两表无 DONE 残留、最近完成表满同批滚动、ls tasks/ 反向核对孤儿文件、逐项报删除清单即视为授权、并行会话维护 TASK.md 时只移表不淘汰留下次收尾）；根模板删除确认条款补任务清单滚动清理豁免；升 3.4.1 | 已完成并验证：--check 36 样例、241 测试与基线刷新全绿；emit 传导核对滚动条款与根豁免各 36/36 组合落产物、destructive-nouns 各组合占位替换正常、version 3.4.1 全体生效、旧条款残留 0 | omp | DONE | 高 | 2026-11-13 |
| RULE-20261111-WORKER | 后台任务（Worker）分层归属与多实例幂等基线 | 反馈「BackgroundService 编排归属 / scope 粒度 / 多实例幂等无判据」：新建 shared/backend/background-worker-rules.md（编排归应用层接口禁入 Worker、每处理单元独立 scope、多实例靠 DB CAS+部分唯一索引禁进程内锁、首轮等迁移完成，门控句「仅在引入进程内后台任务时适用」）挂全部 .NET 载体 12 处（与 background-write-races 同面）；dotnet-layered 层职责表 Service 行补「后台任务调度入口」+禁 Worker 内业务编排、层内目录表补 Service/HostedServices/ 行，dotnet-core-single 目录树+文件夹表补 Workers/ 行；onion skill 新增「后台任务 Worker」小节+references Worker/CAS 完整示例+Service 落点列与 description 关键词（技能变更日志已登记）；产物 frontmatter 版本经 AlignVersion 统一盖写实证无需改片段硬编码；升 3.3.6 | 已完成并验证：--check 36 样例、241 测试与基线刷新全绿（基线 diff 即审查清单）；干净 emit 复核「后台任务（Worker）基线」17 组合命中与 write-races 同面、两模板表格行落产物、dotnetLayered/core-single frontmatter 盖写 3.3.6 | omp | DONE | 高 | 2026-11-11 |
| RULE-20261111-FIELDNAMING-SPREAD | 私有依赖字段条款传导至全部含 DI 后端模板 | 反馈「_domain/_service/_sso 泛称无约定」已于 3.0.3 落地（field-naming.md 第 5 行 + onion 命名表），但片段只挂 dotnet-layered/core-single 两模板，其余含 di-rules 的 .NET 后端组合零覆盖致复发感知；field-naming include 补挂 reactLinkdotNet / vueLinkdotNet / reactLinkdotNet-ai / dotnetBlazor（BlazorWebApp 共用其源）/ web-dotnet backend-single 骨架，删除 vueLinkdotNet 阶段2 逐字节旧版内联拷贝（已漂移）；条款在全部含 DI 后端产物 11/11 组合生效；升 3.3.5 | 已完成并验证：--check 36 样例、测试与基线刷新全绿；干净缓存 emit 复核条款 11 组合命中、内联旧拷贝残留 0 | omp | DONE | 高 | 2026-11-11 |
| RULE-20261109-FEEDBACK | 承接 2026-11-09 九条规则效果反馈 | onion 补拆分既有项目三态；collaboration 补导入比对与 version 语义；di-rules 组合根以 onion Startup 分工为准；mudblazor 补审计/预渲染/弹层/样式落点；backend-testing 补空库 smoke；升 3.3.4 | 已完成并验证：--check 36 样例、241 测试与基线刷新全绿；emit 传导核对（collaboration 36、组合根 11、空库 smoke 9 组合，旧口径残留 0） | omp | DONE | 高 | 2026-11-09 |
| RULE-20261104-RCSI-FENCE | 读事务判据补 RCSI 限定与浓缩版栅栏条款 | 全量版（shared/backend/read-transaction-rules.md）与浓缩版（data-access-rules.md）的单语句快照口径补数据库限定：PG/MySQL 恒成立、SQL Server 需开 RCSI（默认锁定读长扫描仍可能撕裂）；浓缩版补「NEVER 重读栅栏首读与复检同快照事务」对齐全量版五条判据；onion skill 速查①同步限定；升 3.0.5 | 已完成并验证：--check 36 样例、240 测试与基线刷新全绿；emit 复核 RCSI 限定与禁同快照各落 11 组合（2 全量 + 9 浓缩），与读事务挂载面一致 | ZCode | DONE | 中 | 2026-11-04 |
| RULE-20261104-DEDUP-HO | 读事务判据抽 shared 去重 + Adapter Ho 红线上移 rule | dotnet-layered / dotnet-core-single 两处逐字节相同的读事务全量判据抽为 shared/backend/read-transaction-rules.md，两 arch 改 include 引用（产物逐字节等价，判据从 4 处维护收敛为 shared 单处）；dotnet-layered Adapter 小节补「Ho 不得离开 Adapter」红线一行（B 组口径：基线不因未导入技能失效，skill 侧保留 Bo↔Ho 转换操作细节）；升 3.0.4 | 已完成并验证：--check 36 样例、测试与基线刷新全绿；emit 复核读事务判据仍 11 组合且内容逐字节不变、core-single 产物哈希未漂移（include 等价性实证）、Ho 红线落 dotnet-layered 组合 | ZCode | DONE | 中 | 2026-11-04 |
