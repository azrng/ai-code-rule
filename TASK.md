# 任务清单

> 只维护当前活跃任务和最近完成的任务，已完成超过 5 条自动删除最早的。

## 当前任务（进行中）

| ID | 任务名称 | 任务目标 | 当前阶段 | 负责人 | 状态 | 优先级 | 最近更新 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| （无） | | | | | | | |

## 最近完成任务

| ID | 任务名称 | 任务目标 | 当前阶段 | 负责人 | 状态 | 优先级 | 最近更新 |
| RULE-20261115-ARTIFACTS-STALE | artifacts 废弃目录清理 | collaboration 收尾清理扩展：本会话目录之外，其余任务目录（含归属不明）按目录树最后写入时间超约 2 小时判废弃一并删除；2 小时内活跃目录不删只提示；升 3.7.1 | 已完成并验证：242 测试与基线刷新（214 条目）全绿、--check 37 样例通过；滚动淘汰 WORKER 行（无 tasks 文件） | ZCode | DONE | 高 | 2026-11-15 |
| RULE-20261115-PROTOTYPE | 原型沉淀型推全栈 | 决策表收敛为全栈默认沉淀型（正式工程+接口边界mock），堵原型目录另建工程第三态；HTML原型/快稿退场为正式栈对比稿（自包含、不进正式构建、确认后归档）；确认后接真实数据=仅换注入注册；16 Web 片段挂 mock 边界；升 3.7.0 | 已完成并验证：--check 37 样例、242 测试与基线刷新全绿；基线 214 条目变化＝version 3.7.0 全体生效＋新决策表 / mock 边界传导（界面组合 frontend/ui 产物、doc/collaboration 全组合）；旧术语（HTML 原型 / HTML 快稿 / 交互原型 / prototype/assets）残留 0；滚动淘汰 FIELDNAMING-SPREAD 行（无 tasks 文件） | ZCode | DONE | 高 | 2026-11-15 |
| RULE-20261115-ARTIFACTS | artifacts 产物守卫与失败止损 | dotnet-build-isolation 补目录复用与膨胀红线；collaboration 补收尾清理（直接删除无需报清单）与连续失败 3 次止损；升 3.5.0 刷 emit 基线 | 已完成并验证：--check 36 样例、241 测试与基线刷新全绿；基线 209 条目变化＝version 3.5.0 全体生效＋新条款传导（collaboration 全组合、build-isolation 10 架构）；门控反检索：回滚窗口类清理为发布产物不受影响、重试类条款均为运行时代码规范不冲突 | ZCode | DONE | 高 | 2026-11-15 |
| RULE-20261113-ROLLBACK | 承接 2026-11-13 两条任务滚动反馈 | collaboration#任务管理新增「任务收尾与滚动一致性」条款（置 DONE 前核对两表无 DONE 残留、最近完成表满同批滚动、ls tasks/ 反向核对孤儿文件、逐项报删除清单即视为授权、并行会话维护 TASK.md 时只移表不淘汰留下次收尾）；根模板删除确认条款补任务清单滚动清理豁免；升 3.4.1 | 已完成并验证：--check 36 样例、241 测试与基线刷新全绿；emit 传导核对滚动条款与根豁免各 36/36 组合落产物、destructive-nouns 各组合占位替换正常、version 3.4.1 全体生效、旧条款残留 0 | omp | DONE | 高 | 2026-11-13 |
| RULE-20261113-LIBRARY | 新增「类库 / 包仓库」形态与 .NET NuGet 包仓库组合 | 新增 form=library（类库 / 包仓库）与组件 dotnet-nuget-library（arch/dotnet-nuget-library 三片段：backend=包实现/公开 API 契约/逐包测试、doc=README/CHANGELOG/版本一致性、infra=打包发布链路/发布纪律/环境变量）+ 家族 dotnet-package + 约束（library 钳制 dotnet、无前端、无壳、single）+ 兼容样例（第 37 个）；ProjectDetector 增加「启用 NuGet 打包的类库工程多于 Web 宿主」检测证据；grill-me 预选面补 library；README 组合计数 36→37；升 3.5.0 | 已完成并验证：--check 37 样例全绿、242 测试全绿、emit 基线刷新（diff = 新组合 8 文件 + 全体 version 3.5.0 盖写）；干净缓存 emit 复核新组合产物：根文件 extra-sections「包版本与变更记录」、包仓库三阶段、跨包职责占位替换正确，include 全展开无残留 | omp | DONE | 高 | 2026-11-13 |
