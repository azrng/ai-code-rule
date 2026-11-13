# 任务清单

> 只维护当前活跃任务和最近完成的任务，已完成超过 5 条自动删除最早的。

## 当前任务（进行中）

| ID | 任务名称 | 任务目标 | 当前阶段 | 负责人 | 状态 | 优先级 | 最近更新 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| （无） | | | | | | | |

## 最近完成任务

| ID | 任务名称 | 任务目标 | 当前阶段 | 负责人 | 状态 | 优先级 | 最近更新 |
| RULE-20261113-ROLLBACK | 承接 2026-11-13 两条任务滚动反馈 | collaboration#任务管理新增「任务收尾与滚动一致性」条款（置 DONE 前核对两表无 DONE 残留、最近完成表满同批滚动、ls tasks/ 反向核对孤儿文件、逐项报删除清单即视为授权、并行会话维护 TASK.md 时只移表不淘汰留下次收尾）；根模板删除确认条款补任务清单滚动清理豁免；升 3.4.1 | 已完成并验证：--check 36 样例、241 测试与基线刷新全绿；emit 传导核对滚动条款与根豁免各 36/36 组合落产物、destructive-nouns 各组合占位替换正常、version 3.4.1 全体生效、旧条款残留 0 | omp | DONE | 高 | 2026-11-13 |
| RULE-20261113-LIBRARY | 新增「类库 / 包仓库」形态与 .NET NuGet 包仓库组合 | 新增 form=library（类库 / 包仓库）与组件 dotnet-nuget-library（arch/dotnet-nuget-library 三片段：backend=包实现/公开 API 契约/逐包测试、doc=README/CHANGELOG/版本一致性、infra=打包发布链路/发布纪律/环境变量）+ 家族 dotnet-package + 约束（library 钳制 dotnet、无前端、无壳、single）+ 兼容样例（第 37 个）；ProjectDetector 增加「启用 NuGet 打包的类库工程多于 Web 宿主」检测证据；grill-me 预选面补 library；README 组合计数 36→37；升 3.5.0 | 已完成并验证：--check 37 样例全绿、242 测试全绿、emit 基线刷新（diff = 新组合 8 文件 + 全体 version 3.5.0 盖写）；干净缓存 emit 复核新组合产物：根文件 extra-sections「包版本与变更记录」、包仓库三阶段、跨包职责占位替换正确，include 全展开无残留 | omp | DONE | 高 | 2026-11-13 |
| RULE-20261111-WORKER | 后台任务（Worker）分层归属与多实例幂等基线 | 反馈「BackgroundService 编排归属 / scope 粒度 / 多实例幂等无判据」：新建 shared/backend/background-worker-rules.md（编排归应用层接口禁入 Worker、每处理单元独立 scope、多实例靠 DB CAS+部分唯一索引禁进程内锁、首轮等迁移完成，门控句「仅在引入进程内后台任务时适用」）挂全部 .NET 载体 12 处（与 background-write-races 同面）；dotnet-layered 层职责表 Service 行补「后台任务调度入口」+禁 Worker 内业务编排、层内目录表补 Service/HostedServices/ 行，dotnet-core-single 目录树+文件夹表补 Workers/ 行；onion skill 新增「后台任务 Worker」小节+references Worker/CAS 完整示例+Service 落点列与 description 关键词（技能变更日志已登记）；产物 frontmatter 版本经 AlignVersion 统一盖写实证无需改片段硬编码；升 3.3.6 | 已完成并验证：--check 36 样例、241 测试与基线刷新全绿（基线 diff 即审查清单）；干净 emit 复核「后台任务（Worker）基线」17 组合命中与 write-races 同面、两模板表格行落产物、dotnetLayered/core-single frontmatter 盖写 3.3.6 | omp | DONE | 高 | 2026-11-11 |
| RULE-20261111-FIELDNAMING-SPREAD | 私有依赖字段条款传导至全部含 DI 后端模板 | 反馈「_domain/_service/_sso 泛称无约定」已于 3.0.3 落地（field-naming.md 第 5 行 + onion 命名表），但片段只挂 dotnet-layered/core-single 两模板，其余含 di-rules 的 .NET 后端组合零覆盖致复发感知；field-naming include 补挂 reactLinkdotNet / vueLinkdotNet / reactLinkdotNet-ai / dotnetBlazor（BlazorWebApp 共用其源）/ web-dotnet backend-single 骨架，删除 vueLinkdotNet 阶段2 逐字节旧版内联拷贝（已漂移）；条款在全部含 DI 后端产物 11/11 组合生效；升 3.3.5 | 已完成并验证：--check 36 样例、测试与基线刷新全绿；干净缓存 emit 复核条款 11 组合命中、内联旧拷贝残留 0 | omp | DONE | 高 | 2026-11-11 |
| RULE-20261109-FEEDBACK | 承接 2026-11-09 九条规则效果反馈 | onion 补拆分既有项目三态；collaboration 补导入比对与 version 语义；di-rules 组合根以 onion Startup 分工为准；mudblazor 补审计/预渲染/弹层/样式落点；backend-testing 补空库 smoke；升 3.3.4 | 已完成并验证：--check 36 样例、241 测试与基线刷新全绿；emit 传导核对（collaboration 36、组合根 11、空库 smoke 9 组合，旧口径残留 0） | omp | DONE | 高 | 2026-11-09 |
