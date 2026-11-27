# 任务清单

> 只维护当前活跃任务和最近完成的任务，已完成超过 5 条自动删除最早的。

## 当前任务（进行中）

| ID | 任务名称 | 任务目标 | 当前阶段 | 负责人 | 状态 | 优先级 | 最近更新 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| （无） | | | | | | | |

## 最近完成任务

| ID | 任务名称 | 任务目标 | 当前阶段 | 负责人 | 状态 | 优先级 | 最近更新 |
| RULE-20261127-FEEDBACK4 | 界面文案克制反馈批 | 承接目标项目 1 条缺失反馈（病案上传页自加说明性长文案返工移除）：shared/ui/visual-acceptance.md 新增「界面文案克制」小节——写界面时即生效不待验收，界面只留操作必需文案（按钮/标签/占位符/校验错误提示/状态结果），禁把业务规则、政策、服务端机制复述成界面说明段（四处高发位置点名），此类口径写设计与接口文档；与既有「文案与现状一致」互补；升 3.25.0→3.26.0 | 已完成并验证：片段被 100% 带 UI 组合 include 一处生效全组合同步；--check 40 样例、298 测试全绿；基线 460 行版本戳全量轮换 + ui 家族内容哈希；反向语义（design-system 空态/错误提示基线）已声明不冲突；逐条判定与证据见 tasks/RULE-20261127-FEEDBACK4.md | 主仓 | DONE | 高 | 2026-11-27 |
| RULE-20261120-SKILL-RAWSQL | Azrng 可选包与原生 SQL 选型反馈批 | 承接 skillsTest 架构对标 2 条缺失（全采纳）：azrng-efcore 自定义仓储补「LINQ 之外的原生 SQL 选型」四级排序（LINQ→Common.Dapper→DynamicSqlBuilder+JSqlParser→行锁 FromSql，触发门槛/落层/注册点齐全，禁手拼条件串）；onion 何时该拆补「基础设施可选包速查」5 行表（Cache.Redis 3.1.0/S3Store 1.2.0/Dapper 0.2.0/DynamicSqlBuilder/SettingConfig.BasicAuth 1.3.0，均可选不进骨架）+ 文件存储节 S3Store 默认句；两技能 description 补检索关键词；版本基线经内部包仓库 README 逐包核实；补充批 3.19.1 脱敏内部参照工程名（azrng-efcore 两处 + layer-responsibilities 实测句 + 本档案绝对路径，skills/fragments 检索 0 残留）；升 3.19.0→3.19.1 | 已完成并验证：--check 37 样例、273 测试全绿；基线纯版本传导 | 主仓 | DONE | 高 | 2026-11-20 |
| RULE-20261120-DSYAML | design-system.yaml 语法损坏修复 | skillsOutput 全量 emit 排查发现 6 产物 YAML 解析失败，生成器按不透明文本拷贝不校验语法故全绿漏过。源损坏 2 片段 3 处：vueLinkdotNet:437 el_size 行粘连+引号未闭合（传导 5 样例）、staticHtml:560 值后全角括号、:595 值后裸「或」文本；修复：拆行对照 vueWailsExe、行内说明改 # 注释；升 3.21.1；守卫 DesignSystemYamlTests 全样例 .yaml 产物 YamlDotNet 反序列化（注入损坏验证非恒真），测试项目 +YamlDotNet 18.1.0 | 已完成并验证：重 emit 33 产物 0 解析失败、版本戳 3.21.1；--check 39 样例、280 测试全绿；include/槽位/索引双向/front matter 排查面同步核过；详见 tasks/RULE-20261120-DSYAML.md | omp | DONE | 高 | 2026-11-20 |
| RULE-20261120-WORKER | dotnetWorker 后台常驻服务组合 | 起因 sso-launcher 技术栈讨论，排查暴露空白：Worker Service/Windows 服务类项目无形态可落（form=backend-only 是 Web API 语义，dotnetConsole 命令解析口径误导）。新增 form=daemon（选项/家族/约束「daemon→frontend=none+shell=none+backend=dotnet」）、组件 dotnetWorker（when 钉全 7 维）+ 兼容样例 + arch/dotnetWorker 三片段（BackgroundService 作业/优雅停机/并发幂等、Windows 服务安装/日志落文件事件日志/发布链路、doc）；检测器强特征 Sdk.Worker 或 Hosting.WindowsServices（纯 Hosting 引用不算），UI/包仓库主导守卫与 WebService 同口径；RecipeRecorder daemon→dotnet gitignore；grill-me when 补 daemon；升 3.21.0 | 已完成并验证：--check 39 样例、279 测试全绿（含 4 新检测器测试）；--detect 实测命中 dotnetWorker 全维回答；--emit 抽查 8 产物契约一致；基线随版本戳全量轮换+dotnetWorker 8 产物；详见 tasks/RULE-20261120-WORKER.md | omp | DONE | 高 | 2026-11-20 |
| RULE-20261125-FEEDBACK | 规则效果反馈承接批（6 条） | 承接目标项目 6 条反馈：#1 委派子代理结论按待验证假设逐条复核+提示词附行号证据（collaboration「坦率反馈与过程沟通」）；#2 新增「文件编辑纪律」——markdown 表格整段替换优先（collaboration）；#3 走查门控显式化组件库迁移/主题类任务同属界面交付（browser-walkthrough，违反类本体已覆盖）；#4 prototype-workflow 新增「原型废弃」（触发情形/废弃一次做全/编号式引用扫描）+ doc-directory prototype 条目指向；#5 共享文件暂存注明 add -p 交互前提与 -U0 筛块落点风险、构造后核对落点（collaboration）；#6 置 DONE 自检补行为增删 grep 文案残留 + visual-acceptance 补界面文案一致性验收；升 3.21.1→3.22.0 | 已完成并验证：--check 39 样例全绿；280 测试通过；emit 基线刷新（版本全量轮换 + collaboration/ui/doc 家族内容哈希）；反向语义检索 4 处口径已声明；逐条判定与证据见 tasks/RULE-20261125-FEEDBACK.md | 主仓 | DONE | 高 | 2026-11-25 |
