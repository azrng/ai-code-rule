# 任务清单

> 只维护当前活跃任务和最近完成的任务，已完成超过 5 条自动删除最早的。

## 当前任务（进行中）

| ID | 任务名称 | 任务目标 | 当前阶段 | 负责人 | 状态 | 优先级 | 最近更新 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| （无） | | | | | | | |

## 最近完成任务

| ID | 任务名称 | 任务目标 | 当前阶段 | 负责人 | 状态 | 优先级 | 最近更新 |
| RULE-20261120-SKILL-RAWSQL | Azrng 可选包与原生 SQL 选型反馈批 | 承接 skillsTest 架构对标 2 条缺失（全采纳）：azrng-efcore 自定义仓储补「LINQ 之外的原生 SQL 选型」四级排序（LINQ→Common.Dapper→DynamicSqlBuilder+JSqlParser→行锁 FromSql，触发门槛/落层/注册点齐全，禁手拼条件串）；onion 何时该拆补「基础设施可选包速查」5 行表（Cache.Redis 3.1.0/S3Store 1.2.0/Dapper 0.2.0/DynamicSqlBuilder/SettingConfig.BasicAuth 1.3.0，均可选不进骨架）+ 文件存储节 S3Store 默认句；两技能 description 补检索关键词；版本基线经内部包仓库 README 逐包核实；补充批 3.19.1 脱敏内部参照工程名（azrng-efcore 两处 + layer-responsibilities 实测句 + 本档案绝对路径，skills/fragments 检索 0 残留）；升 3.19.0→3.19.1 | 已完成并验证：--check 37 样例、273 测试全绿；基线纯版本传导 | 主仓 | DONE | 高 | 2026-11-20 |
| RULE-20261119-SKILL-ONION | 洋葱技能实测反馈承接批 | 承接 skillsTest 实测 15 条反馈（全采纳）：自定义仓储单口径改死 Storage（依赖 +EF）；业务域目录 {域}Manager 跨工程共用命名空间；JWT 认证授权横切节（FallbackPolicy 全局兜底/密钥口径/包选型）；ICurrentUser<TSubject>；注入字段去 I 全名；禁表达式体与 LINQ query syntax；slnx 编号分组；迁移锁默认装配（?? throw）；敏感字面量脱敏排障；Startup 拆分阈值；升 3.15.0；补充批 3.15.1 技能包版本基线对齐 T-005（EFCore 1.8.0/Azrng.Core 2.0.0/SqlMigration 0.6.1/JwtBearer 1.5.0） | 已完成并验证：--check 37 样例、273 测试全绿（两批均）；基线 214 条全 AGENTS 家族（与 df8d1ba 升版签名一致）；emit 抽查 dotnet-layered 新条款全落地；详见 tasks/RULE-20261119-SKILL-ONION.md；测试项目已清反馈并 --upgrade 重放 | ZCode | DONE | 高 | 2026-11-19 |
| RULE-20261117-ARTIFACTS-SCOPE | artifacts 清理条款简化扩范围 | 承接目标项目反馈：清理范围「仓库根」改「全仓 artifacts/（含构建工具误落在项目目录内的）」，范围写全后存量积压自动被首次收尾覆盖（缺口③并入）；判废对照 TASK.md 不采纳（缺口①，时间窗口仍唯一依据）；条款约 210 字→约 150 字；升 3.8.2 | 已完成并验证：--check 37 样例、242 测试全绿；隔离缓存 emit 抽样 37/37 产物含新条款、旧条款 0 残留；基线刷新（版本哈希全量 + collaboration 家族内容哈希）；滚动淘汰 LIBRARY 行（无 tasks 文件） | ZCode | DONE | 高 | 2026-11-17 |
| RULE-20261115-ARTIFACTS-STALE | artifacts 废弃目录清理 | collaboration 收尾清理扩展：本会话目录之外，其余任务目录（含归属不明）按目录树最后写入时间超约 2 小时判废弃一并删除；2 小时内活跃目录不删只提示；升 3.7.1 | 已完成并验证：242 测试与基线刷新（214 条目）全绿、--check 37 样例通过；滚动淘汰 WORKER 行（无 tasks 文件） | ZCode | DONE | 高 | 2026-11-15 |
| RULE-20261115-PROTOTYPE | 原型沉淀型推全栈 | 决策表收敛为全栈默认沉淀型（正式工程+接口边界mock），堵原型目录另建工程第三态；HTML原型/快稿退场为正式栈对比稿（自包含、不进正式构建、确认后归档）；确认后接真实数据=仅换注入注册；16 Web 片段挂 mock 边界；升 3.7.0 | 已完成并验证：--check 37 样例、242 测试与基线刷新全绿；基线 214 条目变化＝version 3.7.0 全体生效＋新决策表 / mock 边界传导（界面组合 frontend/ui 产物、doc/collaboration 全组合）；旧术语（HTML 原型 / HTML 快稿 / 交互原型 / prototype/assets）残留 0；滚动淘汰 FIELDNAMING-SPREAD 行（无 tasks 文件） | ZCode | DONE | 高 | 2026-11-15 |
| RULE-20261120-WORKER | dotnetWorker 后台常驻服务组合 | 起因 sso-launcher 技术栈讨论，排查暴露空白：Worker Service/Windows 服务类项目无形态可落（form=backend-only 是 Web API 语义，dotnetConsole 命令解析口径误导）。新增 form=daemon（选项/家族/约束「daemon→frontend=none+shell=none+backend=dotnet」）、组件 dotnetWorker（when 钉全 7 维）+ 兼容样例 + arch/dotnetWorker 三片段（BackgroundService 作业/优雅停机/并发幂等、Windows 服务安装/日志落文件事件日志/发布链路、doc）；检测器强特征 Sdk.Worker 或 Hosting.WindowsServices（纯 Hosting 引用不算），UI/包仓库主导守卫与 WebService 同口径；RecipeRecorder daemon→dotnet gitignore；grill-me when 补 daemon；升 3.21.0 | 已完成并验证：--check 39 样例、279 测试全绿（含 4 新检测器测试）；--detect 实测命中 dotnetWorker 全维回答；--emit 抽查 8 产物契约一致；基线随版本戳全量轮换+dotnetWorker 8 产物；详见 tasks/RULE-20261120-WORKER.md | omp | DONE | 高 | 2026-11-20 |
