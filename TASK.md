# TASK：组合式 AI Rule 生成器改造（分期执行）

> 本文件是当前任务的 AI 交接清单。接手后从“当前任务”继续执行，完成后可删除。
>
> 分支：`simplify`
>
> 北极星方案：[doc/组合式AI-Rule生成器改造方案.md](doc/组合式AI-Rule生成器改造方案.md)
>
## 0. 当前维护任务

| 任务 ID | 任务名称 | 任务目标 | 当前阶段 | 负责人 AI | 任务状态 | 优先级 | 最近更新时间 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| RULE-20260915-DETECTFIX | 技术栈检测：桌面方案携带 Sdk.Web 辅助工程时误推荐通用项目 | 用户实测 D:\Work\github\ApixPress（src 下 Avalonia 主工程 + Core、tools 下 Sdk.Web 测试 API、共 4 csproj）检出「维度 项目形态 证据冲突（纯后端服务 / 桌面应用）」且推荐组合收敛到 generalProject——根因：CollectEvidence 对每个 csproj 独立发证据，TestApi 的 Microsoft.NET.Sdk.Web 给出 form=backend-only / backend=dotnet / arch-style=layered 方案级证据，与主工程 Avalonia 的 form=desktop 冲突把形态打回人工确认，而 confirmed 组合（frontend=avalonia + backend=dotnet + arch-style=layered）无任何真实 Profile 能全中，唯一通过过滤的是全通配的 generalProject，Count==1 被 Converged 误判为收敛；修复：csproj 证据发放改为「先归类再发放」（新增 DotnetProjectKind 分类器，分支优先级与旧 continue 链一致），MAUI / WPF / Avalonia / ElectronNET / Blazor 属能独立定义方案形态的 UI 工程，方案形态已被 UI 工程或桌面壳（Wails / Tauri / Electron）定义时 Sdk.Web 工程视为辅助工程（tests / tools 下的测试 API、Mock 服务）不再声明 form / backend / arch-style 方案级证据，纯 Web 方案行为不变；新增回归测试 2 个（ApixPress 真实布局收敛 avaloniaExe 且无冲突 note、WPF 对称场景收敛 wpfExe）。纯生成器行为变更按惯例保持 manifest 版本 1.75.0：277 测试通过（+2）；`--detect` 实测 ApixPress 目录：推荐组合 Avalonia 跨平台桌面应用（avaloniaExe），回答组合 form=desktop / backend=none / frontend=avalonia / arch-style=single，零冲突 note | 已完成验证 | ZCode | DONE | 高 | 2026-09-15 |
| RULE-20260915-AUDITFIX | 全面复审三发现修复：node backend 两节 / 桌面 DI / 桌面数据访问 | 承接全面复审（结构层全绿：--check、275 测试、34 产物零重复标题、include 无悬空、GPT-6 反模式零复发；残留核查确认刻意保留项均成立）后的三个内容层发现：①next/nuxt backend 的异常处理与第三方集成两节近逐字（差异仅框架路径词与 NUXT_PUBLIC 条）——细查发现 Service 主体与代码组织已被外部组件化的 web-node 骨架收走，实际剩余收敛面大缩水，新抽 `backend-exception-core.md`（4 条泛化：接口层并列 Route Handler / Server Action / Server Route）与 `backend-integration-core.md`（3 条，集成目录泛化到服务端），next / nuxt 各换 include + 错误目录 / 集成目录 / createError / NUXT_PUBLIC 增量行；代码组织首行与 Repository 路径行留给组件化槽位路线。②wpfExe/ui 与 avaloniaExe/infrastructure 的 DI 节仅注册入口词差（App.xaml.cs vs Program.cs）——抽 `shared/infrastructure/di-azrng-desktop.md`（Azrng 三接口 + 显式注册 + RegisterBusinessServices 桌面禁抄警示，注册位置泛化为「应用入口」），两处 include + 入口增量行；maui 的 MauiAppBuilder 模式语义不同保留。③桌面三兄弟 infra 数据访问内核同构（参数化 / 配置连接串 / 生命周期 / snake_case 映射）——抽 `data-access-desktop-core.md` 4 条，avalonia（+Dapper 首条）、wpf（零增量）、maui（+SQLite/EF 双轨、HttpClient/Refit、超时重试、离线缓存增量，「移动端注意及时释放资源」并入 core 排比句）三处收敛。规则内容变更 1.74.0 → 1.75.0：`--check` 34 Profile 全绿、275 测试通过、emit 差异逐一核验（6 文件 = 7 处替换与措辞统一，零计划外漂移）、重复标题扫描零叠加；`--recipe` 实测 wpfExe（版本 1.75.0、DI 警示与 App.xaml.cs 增量就位、参数化条展开） | 已完成验证 | ZCode | DONE | 高 | 2026-09-15 |
| RULE-20260915-ADVREVISIT | 主动建议规则保留项复审：next / nuxt / staticHtml 三处收敛 | 用户质疑第四批 3 处「刻意保留」，逐处 diff 复审后推翻原判定：next 与 nuxt 的 4 条规则仅一词之差（hook vs 组合式函数）——两份近乎逐字重复维护，正是改一处忘另一处的漂移源，新抽 `shared/frontend/advisory-fullstack.md`（App Router 语境 4 条：影响当前任务说明、复用不新增平行实现、设计稿冲突先说明、改动不扩大任务外；第 2 条 hook / 组合式函数并列涵盖两框架词），next / nuxt 换 include 零增量；staticHtml 复审确认是 shared/frontend/advisory 的纯子集变体（第 1/2/4 条为正典枚举子集、第 4 条即正典查证条），第四批以「无 hook/store/schema 词」保留过严——枚举句多出的词对静态站无害，include 后零增量且白得正典第 3 条（表单校验 / 页面状态 / 错误处理缺失提醒，静态页同样适用），换 include shared/frontend/advisory.md。原保留 5 处经复审收敛 3 处，仅剩 ai-AGENTS（AI 专属）与 mauiMobile/infrastructure（已是本地专属条 + include core 正确模式）2 处。规则内容变更 1.73.0 → 1.74.0：`--check` 34 Profile 全绿、275 测试通过、emit 差异逐一核验（3 文件 = staticHtml 子集换超集 9 行 + next/nuxt 并列词统一各 2 行，零计划外漂移）、重复标题扫描零叠加；`--recipe` 实测 nextFullstack（版本 1.74.0、advisory 单节、hook / 组合式函数并列就位） | 已完成验证 | ZCode | DONE | 高 | 2026-09-15 |
| RULE-20260915-STACKBATCH | 第五批收敛：技术栈按技术族抽取（4 片段 + 10 处） | 用户提出「## 技术栈 42 处可否按技术抽取」——全量聚类（子节签名 + 首条指纹）修正此前「Profile 专属不宜共享」的判定：同技术族内确有逐字副本。新抽 4 片段：`shared/frontend/stack-react-antd.md`（React+AntD+Router6+Zustand+Axios 6 条，首条并入「版本以仓库锁定为准」版本句，reactLinkdotNet / reactOnly 零增量，reactWailsExe 留 ECharts 与 Axios 走 Wails 绑定增量）；`stack-vue-element.md`（Vue3+Element Plus 6 条，vueLinkdotNet 零增量，vueOnly / vueWailsExe 各留 Axios 用途增量——正典无括号因 vueLinkdotNet 的 Axios 调自家后端 API）；`stack-react-radix.md`（Tailwind+Radix+TanStack+RHF 现代栈 7 条，reactTauriExe 留 `@tauri-apps/api` 增量，electron frontend-react 骨架留 Electron 渲染进程增量）；`shared/application/stack-mvvm.md`（CommunityToolkit.Mvvm + Azrng.Core 2 条，avalonia 留 Azrng.Core.Json 增量，maui 留 Syncfusion / CommunityToolkit.Maui 增量）。组件传导：electron-react 骨架改动同步 reactElectron / reactElectronDotNet，reactLinkdotNet / reactOnly / vueOnly 的前端组件改动同步 Single / Python / -ai 变体。刻意保留：vueTauri（Radix Vue 系孤本）、electron-vue 骨架（Element Plus 桌面版孤本，与 vueTauri 非同族）、dotnet-layered / core-single 后端子节（首条与 stack-backend-dotnet 正典逐字但第 2 条分层描述 9 项目 onion vs 单项目语义分叉，中途误改已回滚）、next / nuxt 部署端口（2 条收益且属 web-node 壳组件槽位领地）、其余单份族。规则内容变更 1.72.0 → 1.73.0：`--check` 34 Profile 全绿、275 测试通过、emit 差异逐一核验（12 文件 = 10 替换 + 组件传导，零计划外漂移）；`--recipe` 实测 reactOnly（版本 1.73.0、AntD 技术栈展开、Axios 增量就位） | 已完成验证 | ZCode | DONE | 高 | 2026-09-15 |
| RULE-20260915-ADVBATCH | 第四批收敛：主动建议规则专项（22 处，## 级标题漏网清查） | 用户发现 staticHtml/doc 的「主动建议规则」未抽取——此前盘点只扫 ### 级，## 级共 27 处本地副本、6 个 shared 域正典 40 处引用，逐处比对后收敛 22 处：backend 域 6 处（dotnet-layered + onion 依赖方向增量条、core-single / dotnetBlazor / flutter / python / generalProject 零增量，枚举子集与栈词由通用句涵盖）；doc 域 2 处（staticHtml 零增量——shared 第 1 条泛化补「页面结构、交互说明」吸收其词（16 个引用者同步获得），console + 命令说明增量）；frontend / web ui 域 4 处（vueWails、flutter/ui、dotnetBlazor/ui、reactLinkdotNet-ai/frontend——shared/frontend/advisory 5 条超集涵盖，ai 版 + shadcn 基线与 Agent 反馈增量 2 条）；桌面 XAML ui 3 处（新抽 shared/ui/advisory.md——ui 域此前 0 正典，wpf / avalonia / maui 三版近乎逐字，第 2 条泛化「平台样式资源或设计体系约束」，三处各留一行平台词增量：WPF Fluent / Semi.Avalonia / MAUI 平台设计指南）；application 域 2 处（console + 配置日志封装增量、flutter 零增量）；infra 域 5 处（dotnetBlazor / python / reactLinkdotNet / vueLinkdotNet / ai——shared/infra/advisory-docker 4 条涵盖，ai + 模型密钥增量 2 条）。shared 泛化 2 处：backend/advisory 第 1 条补「路由」（python 适配，5 引用者传导）、doc/advisory 第 1 条补词。刻意保留 5 处：staticHtml/frontend（静态精简版无 hook/store/schema 词）、next / nuxt frontend（App Router 语境：可访问性 / 设计稿冲突 / 不扩大条为正典没有的实质关切）、ai-AGENTS（AI 专属）、mauiMobile/infrastructure（已是「本地专属条 + include advisory-core」正确模式）。规则内容变更 1.71.0 → 1.72.0：`--check` 34 Profile 全绿（重复标题守卫零叠加）、275 测试通过、emit 差异逐一核验（30+ 文件 = 22 处替换 + 泛化传导 16 doc + 5 backend + WebApp 组件传导，零计划外漂移）；`--recipe` 实测 generalProject（版本 1.72.0、advisory 单节） | 已完成验证 | ZCode | DONE | 高 | 2026-09-15 |
| RULE-20260915-DOCBATCH | 第三批收敛：阶段 0 骨架 / web schema 契约 / 测试总则（10 处，零内容漂移收敛） | 七类节盘点的收尾批：①阶段 0 骨架渐进抽取——四处本地版（dotnetBlazor / dotnet-core-single / mauiMobile / dotnetConsole）与 ai 版的触发 / 执行 / 产物 / 门控与 shared 逐字一致、仅表格 2-3 行形态专属（界面原型 / 契约引用 / 命令与输入输出 / 平台差异 / AI 设计），拆 `shared/doc/stage0-head.md`（标题+触发+执行+产物）与 `stage0-gate.md`（门控 2 条）两个粒度片段，正典 `stage0-design.md` 同步重构为 head+表+gate 组合（emit 验证其 6 个引用 Profile 逐字节零漂移）；五处换 include 后产物内容零变化（纯结构收敛），此后门控语义演进改一处同步全部。②web schema 契约——next / nuxt 前四条逐字同构，抽 `shared/doc/contract-schema-web.md`（泛化「项目 schema 目录」），两处换 include + 各留目录与接口选择增量（Server Actions / Route Handlers vs server/api / server/routes / useFetch）。③测试总则——dotnetBlazor/ui、staticHtml/frontend、wpfExe/infra 三处微变体换 `shared/common/general-requirements.md`，第 3 条尾句统一到正典（真实业务行为），各自领域测试小节已涵盖原专属措辞。刻意保留：其余 6 处契约（onion shared 正典 / core-single / Blazor / python / generalProject / reactLinkdotNet 与 ai——「唯一来源」为栈特定事实声明）、generalProject/infra 总体要求（部署语境）。规则内容变更 1.70.0 → 1.71.0：`--check` 34 Profile 全绿、275 测试通过、emit 差异逐一核验（5 文件 = 契约泛化 2 + 测试总则统一 3，阶段 0 骨架零内容差异，零计划外漂移）；`--recipe` 实测 mauiMobile 到 D:\temp\rule-smoke-maui171（9 文件、版本 1.71.0、阶段 0 单节、门控与移动端差异条在）。七类节盘点至此全部处置完毕（49 处：收敛 20 处、语境保留 29 处） | 已完成验证 | ZCode | DONE | 高 | 2026-09-15 |
| RULE-20260915-DESKTOPSHARED | 第二批收敛：桌面 application 域与 dotnet Web backend 域抽新共享片段 | 盘点七类节（代码组织 / 契约 / Service / 数据访问 / 异常 / 阶段 0 / 总体要求）共 49 处 arch 本地副本，本批落地 backend / application 两域：新抽 `shared/application/service-layer-dotnet.md`（.NET 桌面 Service 内核 6 条：接口目录 / 异步 Task / 职责边界 / Azrng 异常 / 三生命周期显式注册与 RegisterBusinessServices 桌面不可用 / 禁主构造），wpfExe（全覆盖零增量）、avaloniaExe、mauiMobile 换 include——avalonia 顺带获得缺失的禁主构造条，maui 泛化措辞统一到 Azrng 正典；新抽 `shared/application/azrng-exception-types.md`（BaseException 五分类子清单、不带标题可挂任意 ### 下），wpf（ViewModel 捕获增量）、avalonia（禁随意自定义 + 定位信息增量）、dotnetConsole（顶层捕获 + 退出码增量，顺带补齐缺失的 ForbiddenException）三处换 include。dotnetBlazor / dotnet-core-single backend：代码组织换 `code-organization-dotnet` include + DTO 路径 / Razor 反模式增量，Blazor 数据访问（与 `data-access-rules` 逐字一致）与异常处理（+ErrorBoundary 增量）换 include，dotnetBlazorWebApp 经 web-backend-blazor 组件复用自动传导。刻意保留：dotnetBlazor Service 节（web 语境 Interfaces/ 目录与 RegisterBusinessServices 注册，与桌面片段第 5 条语义冲突）、dotnet-core-single 数据访问（azrng-efcore IBaseRepository 语境）、generalProject 全部（通用项目泛化版）、flutter / next / nuxt / python（Dart / Node / Python 语境）、dotnet-layered 代码组织（外部组件化已改的 onion 分层变体）。过程防误：两处初版保留本地标题被自查纠正为 include 独立成行（守卫上线后第一次实际防护）。规则内容变更 1.69.0 → 1.70.0：`--check` 34 Profile 全绿（重复标题守卫确认新片段零叠加）、275 测试通过、emit 差异逐一核验（7 文件 = 6 处收敛 + Blazor 组件传导，零计划外漂移）；`--recipe` 实测 wpfExe 到 D:\temp\rule-smoke-wpf170（9 文件、版本 1.70.0、Service / 异常节单份、禁主构造条在）。契约要求 8 处、阶段 0 5 处、总体要求 4 处留待下一批（契约需先抽 web 分离版新片段） | 已完成验证 | ZCode | DONE | 高 | 2026-09-15 |
| RULE-20260915-DUPGUARD | 产物重复标题守卫固化进 --check，清零存量 6 处同名标题 | 本会话两度踩 include 叠加缺陷（Next/Nuxt 双 Docker 节、ai 族 13 处标题叠加，均为产物级症状且逐字节等价比对漏检），按 ad78dd5 收尾建议把扫描固化：新增 `HeadingDuplicationValidator`（同文件内 ## / ### 标题重复即缺陷，跳过 fenced code block 内 # 行，未闭合围栏延伸到文末），--check 的 Profile 循环对全部渲染产物逐文件检测，错误并入输出契约失败段；5 个单元测试覆盖同检 / 围栏跳过 / 跨层级豁免 / 四级以下豁免 / 无重复。存量清零 6 处：vueLinkdotNet/backend 删重复 di-rules include（同文件两次 include 直接叠加）；reactLinkdotNet-ai/backend 删本地 DI 节并入 include + IChatClient 增量条；dotnetBlazor/backend 技术栈区删简版 DI 节、详版换 include di-rules + SignalR Scoped / @inject 增量（顺带修复缺 RegisterBusinessServices 的漂移，dotnetBlazorWebApp 经 web-backend-blazor 组件复用双修）；avaloniaExe/infrastructure 技术栈区「### 发布」改名「### 发布方式」与 release.md 的「### 发布」消歧；dotnetBlazorWebApp/ui「## 断线、恢复与状态」下改名「### Interactive Server 断线与恢复」与渲染模式定义区消歧。缺陷注入实测：向 docker-delivery-web.md 追加重复标题后 --check 逐 Profile 报错拦截、恢复后全绿。规则内容变更 1.68.0 → 1.69.0：--check 34 Profile 全绿、275 测试通过（+5）、emit 差异逐一核验（6 文件 = 本轮修复与传导，零计划外漂移）；--recipe 实测 dotnetBlazor 到 D:\temp\rule-smoke-blazor169（9 文件、版本 1.69.0、DI 单节、RegisterBusinessServices 与 @inject 均在） | 已完成验证 | ZCode | DONE | 高 | 2026-09-15 |
| RULE-20260914-AISHARED | reactLinkdotNet-ai 派生副本收敛共享源（13 节） | 承接审查清单的「更远候选」批次：ai 族 backend / doc / frontend 中与 shared 片段逐字或近似一致的节换 include，AI 专属差异转增量条——backend 5 处（主动建议 / 代码组织 / Redis / 代码注释 / 后端测试，保留 mock IChatClient、模型密钥注释、SQLite In-Memory 差异增量）、doc 3 处（常见任务入口 / 主动建议 / 设计自检清单，保留 Agent 交互、流式事件增量；阶段 0 表格与门控因 AI 行差异保留本地）、frontend 5 处（常见任务入口 / 路由 / Zustand / 请求 / 代码组织，保留 Agent 对话、独立 store、流式封装、shadcn 例外增量）。shared 泛化两处兼容措辞（advisory「Controller / 端点」、code-organization-dotnet 首条与反模式补端点），传导 4 份 web-dotnet backend。顺带修复一处历史漂移：ai/frontend 代码组织规范缺后加的中文注释条，include 后自动获得。刻意保留本地：单项目整洁架构分层节、数据库迁移双轨（SQLite EF Migrations vs PG SqlMigration，与 shared「不用 EF Migrations」语义冲突）、shadcn 样式 / 组件节、AI 测试策略。过程失误自查：首批替换保留本地标题与 shared 自带标题叠加，产物扫描发现 13 处重复后删本地标题行修复（复用 Docker 双节教训：include 独立成行、标题由 shared 携带）；批量脚本误删原有「### 设计系统」父标题（shared 仅带 #### 子标题）已补回。规则内容变更 1.67.0 → 1.68.0：`--check` 34 Profile 等价全绿、270 测试通过、emit 差异逐一核验（7 文件 = ai 三份收敛 + 4 份泛化传导，零计划外漂移）、全部产物重复标题扫描零新增（余 6 处为既有同名分层）；`--recipe` 实测重放 reactLinkdotNet-ai 到 D:\temp\rule-smoke-ai168（10 文件、版本 1.68.0、无重复标题、include 展开与增量条就位、中文无乱码） | 已完成验证 | ZCode | DONE | 高 | 2026-09-14 |
| RULE-20260914-REGSHARED | 提交前最小回归三域收敛共享源 + Next/Nuxt infra 双 Docker 节修复 | 承接审查结论（frontend / ui / application 三域回归本地副本漂移）：shared/frontend/pre-commit-regression.md 第 3 条吸收 next / nuxt 本地版的量化定义（「微小调整……修改内容少于 10 行」替换压缩版「仅样式或文案微调」，10 个引用 Profile 同步获得量化口径）；shared/ui 版同位置吸收微小调整条（wpfExe 传导）；shared/application 版第 2 条泛化补 Provider（吸收 flutter）。本地副本换 include：nextFullstack / nuxtFullstack / reactLinkdotNet-ai 的 frontend 回归、avaloniaExe / mauiMobile / flutterMobile 的 ui 回归（flutter 留视觉根因与 Widget 措辞增量条）、dotnetConsole / flutterMobile 的 application 回归（flutter 留 `flutter test` 默认校验条）、reactLinkdotNet-ai/backend（留 mock IChatClient 增量条）。期间发现外部组件化提交（2c32e27）遗留缺陷：web-node 骨架 infra-delivery-tail 已含 docker-delivery-web，而 next / nuxt 的 arch 文件还残留上轮 Docker 抽取加的同名 include，产物出现两个「Docker 交付规则」节——删除 arch 冗余 include 行修复；另全量扫描产物重复小节标题，确认其余 6 处（Blazor DI 规范等）为同名分层非叠加。规则内容变更 1.66.0 → 1.67.0：`--check` 34 Profile 等价全绿、270 测试通过、emit 差异逐一核验（33 文件 = 回归统一与双节删除，零计划外漂移）；`--recipe` 实测重放到 D:\temp\rule-smoke-167（10 文件、版本 1.67.0、Docker 单节、量化回归条、中文无乱码、二次重放幂等） | 已完成验证 | ZCode | DONE | 高 | 2026-09-14 |
| RULE-20260914-PYBACK | python 组件 kind 归位为后端组件 | `python` 由整仓 form 组件对齐 DNW1 先例改为 kind=backend，when 从全 7 维收敛为 form=backend-only + backend=python 两维（python 无架构分层分叉，不需要第三维），desc 更新为「纯后端 + Python FastAPI 组合的后端组件」，root / files / slots / family 零改动。when 收敛暴露既有错位：此前纯后端路径 frontend=none 等维度全靠 python 全量 when 的副作用推导（dotnet-backend-layered / -single 自身 when 不含这些维度，一直被掩盖），收敛后 Plan 的约束校验按全严格语义对未回答维度抛「组合违反约束」，连 dotnet 纯后端样例也问答不可达——修复为 `ValidateConstraints` 的 require 只对已回答维度生效（与问答期 `SatisfiesConstraints` 语义对齐；未回答维度的钳制缺口由组件解析兜底：组合解析成功必须存在 when 全命中组件集，组件 when 自身携带维度取值）。270 个测试通过（新增 python 2 维选择可解析断言），34 个兼容样例 `--check` 全绿，`--plan form=backend-only backend=python` 正常推导 | 已完成验证 | ZCode | DONE | 中 | 2026-09-14 |
| RULE-20260914-FSHELL | 纯前端家族抽公共壳 | 盘点发现 `frontend-static-frontend-react` 与 `-vue` 组件 root 18 字段零差异、`generalFrontend` 仅 search-nouns / subrule-index / target-list / understanding-checklist 4 字段为框架词差异，同一份 root 维护了三份：抽 `frontend-static-web-shell`（kind=form 对齐 AI 壳先例、family=frontend-static、when form=web+backend=none+shell=none+ai-capability=no，root 持全量 18 字段并 4 处槽位化），react / vue / general 三组件降为 kind=frontend、root 清空、slots 供值（fs-search-nouns / fs-subr-frontend / fs-target-frontend / fs-understanding-frontend）；壳不持文件，doc / infra 仍由前端组件声明（对齐 web-node-shell 与 frontend-static-web-ai-shell 口径，保输出顺序与能力可用性推导）；AI 变体、staticHtml 不动，保留旧 Profile / compatibilityCase。269 个测试通过，34 个兼容样例 `--check` 全绿；manifest 顺带统一历史 8 空格缩进块（`git diff -w` 核验内容级净减 192 行） | 已完成验证 | ZCode | DONE | 高 | 2026-09-14 |
| RULE-20260914-NODECOMP | Next.js / Nuxt 全栈组件化 | 将 `nextFullstack` / `nuxtFullstack` 从全量 `form` 组件拆为共用 `web-node-shell`、Next/Nuxt 前端组件与对应后端组件；复用 `shared/skeletons/web-node/`，保留旧 Profile / compatibilityCase 并逐字节等价。组合计划断言与 34 个兼容样例验证通过 | 已完成验证 | Codex | DONE | 高 | 2026-09-14 |
| RULE-20260914-AICOMP | `reactLinkdotNet-ai` 能力组件化试点 | 将完整 AI Web 组合拆为可复用的 .NET Web 壳、React + Tailwind/shadcn 前端、单项目 .NET 后端与 AI capability/module；补齐 AI 组合钳制和计划组件断言，保留旧 Profile / compatibilityCase 并逐字节等价。264 个测试通过，34 个兼容样例 `--check` 全绿 | 已完成验证 | Codex | DONE | 高 | 2026-09-14 |
| RULE-20260914-COMPOSE-MERGE | 组合输出 append 能力与规则公共骨架抽取 | 组件增加 `moduleIds` 并集能力，组合计划保留带归属的文件贡献；渲染器按一个 replace 基底 + 多个 append 片段聚合，逐项执行 feature 与槽位处理，补齐重复 replace / 缺少基底 / append front matter 校验；`generalFrontend` 与 `generalProject` 前端规则抽出通用原则、组件结构、组件行为和测试规范四个共享片段，`nextFullstack` 与 `nuxtFullstack` 后端 / 基础设施抽出精确重合的建议、Service、代码组织、端口控制和交付尾部骨架，保持生成内容逐字节不变。263 个测试通过，34 个兼容样例 `--check` 全绿 | 已完成验证 | Codex | DONE | 高 | 2026-09-14 |
| RULE-20260914-DOCKERFULL | Docker 交付规则与提交前回归完整抽取共享源 | 用户指出 Docker 规则尚未完整抽取（上轮仅收敛总体要求），指定以 reactLinkdotNet-ai 版为准抽取、删除各项目本地副本：盘点发现 shared/infra/docker-delivery-web.md 已与 reactLinkdotNet-ai 交付规则的通用部分逐字一致（vueLinkdotNet / reactLinkdotNet 已 include），遂以它为共享交付规则唯一源——dotnet-layered（dotnet-core-single 经组件复用跟随）、dotnetBlazor（dotnetBlazorWebApp 跟随）、nextFullstack、nuxtFullstack 的本地「Docker 交付规则」删除换 include（Next.js / Nuxt 服务启动等框架专属验证点由「Compose 服务成功启动 + 关键服务可用」涵盖，统一获得健康检查与 `.dockerignore` 条目）；shared/infra/pre-commit-regression.md 按 reactLinkdotNet-ai 版泛化（补「模型密钥配置」「数据库 Provider」「或模型调用链路」），next / nuxt 本地 3 条精简回归与 reactLinkdotNet-ai 本地 4 条全删换 include，7 个引用该片段的 Profile 回归措辞同步泛化；reactLinkdotNet-ai 交付规则 include + 模型专属一条（模型对接配置触发 + 模型调用链路验证）。generalProject（部署含安装包）、python infra（任务清单转述非规则块）、桌面 infrastructure / skeletons（无 Docker 交付）不动。规则内容变更并入 1.66.0：`--check` 34 Profile 等价全绿、256 测试通过、emit 基线差异逐一核验（14 份 infra-AGENTS.md = 6 份交付规则统一 + 7 份回归泛化 + 1 份 ai 重组，零计划外漂移） | 已完成验证 | ZCode | DONE | 高 | 2026-09-14 |
| RULE-20260914-DOCKERSHARED | Docker 总体要求收敛为共享片段单一维护源 | 用户指出 Dockerfile 口径修复时 reactLinkdotNet-ai 是硬拷贝、其他 Profile 也可受益：盘点 infra「总体要求」形态——dotnet-layered / dotnetBlazor / vueLinkdotNet / reactLinkdotNet 及 Single / Python 变体已 `include shared/infra/general-requirements.md`；reactLinkdotNet-ai 为近似硬拷贝（含模型密钥括号、部署链路可追踪、SQLite 卷挂载专属条目），nextFullstack / nuxtFullstack 为独立措辞变体（无 Dockerfile 单列口径与回滚窗口条目）。收敛三处为 include + 专属条目追加（reactLinkdotNet-ai 保留部署链路可追踪 + SQLite 卷挂载；next / nuxt 保留 standalone / nuxt build 框架条目与「部署链路必须可追踪、可复现」），shared 密钥条目泛化为「不要把密钥（含数据库密码、模型密钥等）写入镜像」吸收 AI 版括号扩展；generalProject（通用部署语境含安装包）与 python（无 Docker 段）不动。规则内容变更并入 1.66.0：`--check` 34 Profile 等价全绿、256 测试通过、emit 基线差异逐一核验（13 份 infra-AGENTS.md = 10 份密钥措辞 + 3 份重组，零计划外漂移；中间态曾因 shared 自带标题与本地标题叠加产生重复「### 总体要求」，按仓库 include 惯例删本地标题行后消除） | 已完成验证 | ZCode | DONE | 中 | 2026-09-14 |
| RULE-20260914-GPT6AUDIT | 按 GPT-6 Astra 技能与提示词最佳实践修正规则与技能反模式 | 对照 OpenAI「重新思考 GPT-6 Astra 的技能与提示词」自查仓库命中项并修复：① staticHtml frontend-AGENTS.md「使用方式」从「每个 AI Agent 开始修改前都必须先阅读本文档 / design-system.yaml / 相关实现」的强制预读（与根模板「最小充分上下文」自相矛盾，为文章点名反模式）改为按需口径——小改动只读命中段落、`design-system.yaml` 按场景指向、修改前看相关页面现有实现；② infra 部署文件门槛从「首次创建 Dockerfile 等必须独立确认并获得同意」放宽为「新增部署文件在方案或最终说明单列说明、不静默生成，修改既有部署配置仍按高风险口径先确认」（shared/infra/general-requirements.md 与 reactLinkdotNet-ai 硬拷贝副本两处）；③ 自研技能描述收短保触发精度：avalonia / onion-architecture / azrng-efcore 删除 API 签名全文、控件级 Keywords 长尾与触发例句，保留 Use when 场景与栈级关键词；④ bootstrap-blazor（990 行单文件）拆为 56 行极简路由 + 9 个 references（setup / table / modal / forms / treeview-toast / layout-menu / misc-components / crud-template / troubleshooting，troubleshooting 交叉引用同步改指向 layout-menu.md），console-di（306 行）拆为 57 行路由 + 3 个 references（quickstart / configuration / runtime，根保留核心模型与验证清单），渐进式披露对齐 avalonia / onion-architecture 既有结构。规则内容变更 1.65.0 → 1.66.0：`--check` 34 Profile 等价全绿（含技能发现与 ID 冲突校验），256 测试通过，emit 基线差异逐一核验（195 文件 = 版本戳 + 10 份 infra Dockerfile 行 + reactLinkdotNet-ai 副本 + staticHtml 使用方式重写，零计划外漂移） | 已完成验证 | ZCode | DONE | 高 | 2026-09-14 |
| RULE-20260913-AVALWIN | 真实执行反馈：avalonia skill 补 UrsaWindow 托管窗口装饰与 Ursa 控件事实 | 承接目标项目反馈（缺失类，AzrngTool T023 把手工自绘标题栏整体替换为 UrsaWindow）：avalonia skill 的 `references/windows-flyouts-popups.md` 把 `WindowDecorations="None"` + `BeginMoveDrag` + 手写 WindowState 切换作为正典传授（恰为被替换的旧模式），Ursa 控件目录无 UrsaWindow/ThemeToggleButton/LoadingContainer 条目。补「窗口装饰：首选 UrsaWindow 托管」节（TitleBarContent/LeftContent/RightContent/IsFullScreenButtonVisible 用法与 Avalonia 12 `WindowDecorations` 枚举、`WindowDecorationProperties.ElementRole` 语义，手工模式降级为遗留备用）；controls-catalog 新增 Ursa 控件速记节——ThemeToggleButton 坑（IsThreeState=False 且请求变体为 Default 时首击抛 InvalidOperationException，配 Mode="Indicator" 或三态规避；控件只改 RequestedThemeVariant 不落盘，持久化自行监听 ActualThemeVariantChanged）、LoadingContainer 加载遮罩标准件、负面清单（无 TreeView/SearchBox/EmptyState/Segmented/独立 Tag）、Semi TextBox `Classes="clearButton"`；bindings-and-mvvm 补内置 BoolConverters/ObjectConverters（消费工程 3 个自定义转换器 30+ 引用已全量替换）；SKILL.md 决策入口补「选窗口基座」、frontmatter 关键词与版本记录同步。纯 skill 内容变更、fragments 与生成规则文件不变，版本保持 1.65.0：`--check` 34 Profile 等价全绿，256 测试通过，改动文件回读 UTF-8 无乱码 | 已完成验证 | ZCode | DONE | 中 | 2026-09-13 |
| RULE-20260913-SKILLSYNC | Skills 同名导入改为先删后写 | 用户口径确认：同名技能导入不再文件级覆盖——`SkillImporter.Import` 在写入前检测目标技能目录（以 SKILL.md 为准，与 `DetectConflicts` 同口径），存在即整目录删除后按内置内容重写，上游删改文件造成的残留随之清理；无 SKILL.md 的目录不算同名技能（保留用户文件并入写入），skipKeys 跳过与配方外技能一律不动。交互「覆盖已存在的」选项说明、`--recipe` / `--upgrade` 同步提示语与类注释同步；根 AGENTS.md / README ×2 更新口径；SkillImporterTests 重命名覆盖用例并补残留清理、无 SKILL.md 目录并入两个回归。纯生成器行为变更、规则文件内容不变，版本保持 1.65.0：`--check` 通过、256 测试通过 | 已完成验证 | ZCode | DONE | 中 | 2026-09-13 |
| RULE-20260913-TREEVIEW | 真实执行反馈：avalonia skill 补 TreeView 虚拟化边界 | 承接目标项目反馈（缺失类，T037 接口管理侧栏树卡顿）：avalonia skill 的 `references/layout-and-virtualization.md` 只写明 ItemsControl 默认 StackPanel 不虚拟化，未划清 TreeView 边界，只能自行查 12.1.2 源码确认。补「TreeView 的虚拟化边界」小节：只有顶层可能虚拟化（ItemsPanel 换 VirtualizingStackPanel），子层级宿主在 TreeViewItem 模板的 ItemsPresenter 内、无滚动视口，配 VirtualizingStackPanel 也不生效、展开即全量实例化（结构性限制，源码 + 上游 discussion #12436 / issue #6580 双重核实，升级不改变）；大数据树形展示按优先级给扁平化 ListBox+VSP / 子级懒加载 / TreeDataGrid 三条出路，节点总量不可控时显式接受全量实例化并压测；高度约束条目强化为「列表自身高度有界」，点名外层 StackPanel+ScrollViewer 夹层令任何虚拟化静默失效。纯 skill 内容变更、fragments 与生成规则文件不变，版本保持 1.65.0：`--check` 通过、255 测试通过 | 已完成验证 | ZCode | DONE | 中 | 2026-09-13 |
| RULE-20260913-AITOOLDIR | 生成器默认 gitignore 增补 AI 工具本地目录 | `GitignoreTemplates` 新增 `AiToolEntries`（`.zcode/`、`.claude/`、`.dotnet/`、`.codex/`、`.cursor/`、`.codegraph/`、`.learnings/`，口径与规则源仓库自身 .gitignore「ai忽略文件」块对齐），与生成器本地产物合并为统一忽略块 `GeneratorBlock`（注释改为「AI rule 本地产物与 AI 工具目录」）：新建 .gitignore 的全部技术栈默认模板与已存在 .gitignore 的追加路径（`AllGitignoreEntries`）都覆盖，重复执行仍幂等；SkillImporter 口径注释同步——技能不单独忽略，但工具目录默认忽略后需共享技能的项目自行取反（如 `!.claude/skills/`）；根 AGENTS.md / README ×2 / 仓库自身 .gitignore（补 `.dotnet` `.cursor` `.learnings`）/ RecipeRecorder 测试断言同步。纯生成器行为变更、规则文件内容不变，版本保持 1.65.0：`--check` 34 Profile 等价全绿，255 测试通过 | 已完成验证 | ZCode | DONE | 中 | 2026-09-13 |
| RULE-20260913-PARALLEL | 真实执行反馈：多 AI 并行会话共写同一工作区的隔离规则 | 承接目标项目反馈（冲突类，T034/T033 两会话并行：TASK.md 两次写入冲突、测试两次因对方未提交改动失败、运行实例锁 bin 迫使改独立输出目录）：`collaboration-AGENTS.md` 新增「并行会话隔离」小节——TASK.md 条目先读后写、ID 被占立即换号不覆盖他人条目；不共享修改同一批业务文件，重叠优先调整自身路径、避不开提请用户协调串行；测试失败先 `git diff HEAD` 归因、对照 HEAD 复跑定位引入方，不回退改写对方未提交文件；输出目录被运行实例锁定时改用独立输出目录等价验证（`dotnet build -o` + `dotnet vstest`），不停止对方实例。根模板「触发式协作规则」补一行并行会话触发词（TASK.md 出现非本会话条目 / 文件被会话外修改 / 输出目录被锁定时读该章节），根规则段预算 19 → 20 仍在 24 内。规则内容变更 1.64.0 → 1.65.0：`--check` 34 Profile 等价全绿，255 测试通过，emit 基线差异逐一核验（34 份 AGENTS.md + collaboration + 版本戳，零计划外漂移） | 已完成验证 | ZCode | DONE | 高 | 2026-09-13 |
| RULE-20260913-AVALONIA | 真实执行反馈：Avalonia 版本敏感 API 查证与反馈文件本地化 | 承接 avaloniaExe 目标项目反馈（缺失类，仓库实际用 Avalonia 12.1.2 而规则锚「11+」，动画 / 剪贴板 / BoxShadow 按 11.x 经验书写连续失败）：ui-AGENTS.md 技术栈行去掉「Avalonia 11+」大版本锚定，改为「大版本以仓库实际引用的 NuGet 包为准」；新增「版本敏感 API 查证」小节——动画（RenderTransform / 关键帧）、剪贴板（`IClipboard`）、阴影（`BoxShadow`）与平台集成等大版本间有破坏性变更的 API，先按根查证优先级确认项目实际大版本形态（仓库既有写法、NuGet 引用程序集、锁定版本源码或官方升级说明），禁止凭旧大版本经验书写；design-system.yaml 同步去版本锚。配套把 `.ai-rule-feedback.md` 加入生成器 gitignore 忽略块（本地产物不随项目提交、回传时显式复制），协作片段「长期保留并随项目提交」口径同步改为本地保留，README ×2 与 RecipeRecorder 测试同步。规则内容变更 1.63.0 → 1.64.0：`--check` 34 Profile 等价全绿，255 测试通过，emit 基线差异逐一核验（34 份 collaboration 一行 + avaloniaExe 两文件 + 版本戳，零计划外漂移） | 已完成验证 | ZCode | DONE | 高 | 2026-09-13 |
| RULE-20260912-SKILLS | 生成器内置 Skills 释放与选择导入 | 免去新项目手动拷贝 skills：`skills/` 整目录以 `Skills.*` 嵌入发布产物（README.md 除外），`SkillsStore` 按「含 SKILL.md 的最外层目录」动态发现（分组目录仅归档，技能 ID 取目录名，嵌套 SKILL.md 归外层）；交互生成在规则落盘后追加「选择 Skills 导入」——技能多选（description 支持单行 / YAML 块 / `>-` 折叠写法）→ 目标多选（按用户本地习惯 Codex → `.codex/skills`、ZCode → `.zcode/skills`、Claude Code → `.claude/skills`）；导入是追加语义：未选中的技能不动，`SkillImporter.DetectConflicts` 发现目标目录已有同名技能（以 SKILL.md 为准）时提示「覆盖」或「跳过」（跳过 = 保留本地版本且不记入配方，配方只记实际写入的技能）；字节级整体拷贝、不进 .gitignore；配方增 `skills` / `skill_targets`（未导入时不出现在 JSON，旧配方形状不变），`--recipe` / `--upgrade` 按配方同步重放、内置内容直接覆盖（技能已下线时告警跳过）；`--check` 新增技能校验（可发现、ID 唯一、description 齐全）。版本 1.61.0 → 1.62.0，管道冒烟覆盖「首次导入 / 同名覆盖 / 同名跳过」三分支 + `--recipe` 重放 + `--upgrade` 实测，252 测试全绿 | 已完成验证 | ZCode | DONE | 高 | 2026-09-12 |
| RULE-20260912-PKGNAME | 发布产物去重与 gitignore 通配覆盖 | 打包不再产出 `use-ai-rule-win-x64.exe` 副本：publish.ps1 / publish.sh 的 win 产物统一命名 `dist/use-ai-rule.exe`（linux/osx 仍带 RID 后缀）；目标项目 gitignore 生成器本体条目从精确名（`use-ai-rule.exe` / `use-ai-rule`）改为通配 `use-ai-rule*`，盖住带 RID 后缀的历史副本（精确名匹配不到 `use-ai-rule-win-x64.exe`，拷入目标项目会成为未跟踪文件）；同步 README 口径与 RecipeRecorderTests 断言。版本 1.60.0 → 1.61.0，已用新产物实测新建 / 追加两条 gitignore 路径与 `git check-ignore` 三名字变体 | 已完成验证 | ZCode | DONE | 中 | 2026-09-12 |
| RULE-20260907-DRIFT | 真实执行反馈：依赖漂移修复 | 承接 `D:\temp\rulereview`（wpfExe 1.59.0）反馈：WPF-UI 包名与 xmlns 修正（`Wpf.Ui`→NuGet ID `WPF-UI`、2024→2022/xaml，以程序集 XmlnsDefinition 为准）；桌面/控制台 DI 注册改为显式 `Add*` 并标注 `RegisterBusinessServices` 属 `Azrng.AspNetCore.Core`（Web 侧同步标注来源包）；统一结果包装对齐现行 Azrng.Core API（`ResultModelFactory` + 默认裸返回 / 业务异常取舍、`IResultModel<T>` 仅暴露 Data）；wpfExe 核心库不再固定版本号。版本 1.59.0 → 1.60.0 | 已完成验证 | ZCode | DONE | 高 | 2026-09-07 |
| RULE-20260907-COMPACT | 规则内容压缩与真实产物复核 | 压缩测试未执行说明、KISS 细则、协作任务与反馈段落；保持契约语义，生成版本 1.58.0 → 1.59.0，并在 `D:\temp\rulereview` 复核实际产物 | 实现与验证 | Codex | DOING | 中 | 2026-09-07 |
| RULE-20260907-COLLAB | 坦率反馈与规则退役机制 | 共享协作规则加入坦率反馈、常规细节判断与结论式过程更新；反馈闭环增加“冗余”类型；新增规则退役清单，并以固定行为规则段预算约束根文件膨胀；版本 1.57.0 → 1.58.0 | 已完成验证 | Codex | DONE | 中 | 2026-09-07 |
| RULE-20260903-COLLAB | 协作规则下沉与需求澄清 | 新需求和 `/plan` 进入结构化澄清；将根文件中的按需流程下沉，降低常驻上下文 | 已完成验证 | Codex | DONE | 高 | 2026-09-03 |
| RULE-20260904-DSBASELINE | 设计系统基线与定制口径 | `design-system.yaml` 从固定规范改为「内置基线 + 按产品品牌定制」：定制口径收敛到 `shared/ui/design-system-liveness.md` 并接入全部界面子规则；16 份 yaml 头部声明基线语义、`meta.status` 改「基线待定制」；生成器对已定制（或状态无法识别）的本地 `design-system.yaml` 保留不覆盖（`DesignSystemGuard`）。版本 1.48.0 → 1.49.0 | 已完成验证 | ZCode | DONE | 高 | 2026-09-04 |
| RULE-20260904-EOL | 渲染管线行尾归一 | `ResourceStore.Read` 读取口统一 LF：此前根模板按 `\n` 写死的连续空行收敛对 CRLF 检出不生效，同一片段在 CRLF / LF 工作区产出内容有差异；归一后 CRLF worktree 与 LF 主目录 `--emit` 逐字节一致。仅代码与测试变更，生成内容不变，版本保持 1.49.0 | 已完成验证 | ZCode | DONE | 中 | 2026-09-04 |
| RULE-20260904-JSTYPE | 静态 HTML Profile JS 类型纪律 | `staticHtml` frontend 规范新增「类型纪律（无构建的类型安全）」小节：保持 `.js` 不引入 TS 编译，共享模块强制 JSDoc 注解 + `// @ts-check`，可选 `jsconfig.json`（checkJs/noEmit）仅作检查；脚本规模超出时引导迁移到带构建的前端组合而非在本项目引入构建。版本 1.49.0 → 1.50.0 | 已完成验证 | ZCode | DONE | 中 | 2026-09-04 |
| RULE-20260906-P2S1 | 组合内核阶段 1 与 Electron 家族组件化 | 启动 P2：manifest 新增 `components` / `constraints` / `compatibilityCases` 节点；新增 `Selection` / `ComponentDefinition` / `CompositionPlan` 模型与 `CompositionPlanner`（kind 固定顺序解析、`{{slot:x}}` 槽位注入、family 基线继承、证据不足不隐式补全、约束校验）；`RuleRenderer` 抽出统一渲染核心，兼容路径与组合路径共用；Electron 家族 4 个组合组件化（骨架 `electron-desktop-shell` + 4 个前端组件 + 2 个后端组件）；`--check` 对 4 个兼容样例逐字节比对两条渲染路径，`CompositionEquivalenceTests` 额外覆盖能力子集。生成内容零漂移（`--emit` 与 1.51.0 基线 267 文件逐字节一致），版本保持 1.51.0 | 已完成验证 | ZCode | DONE | 高 | 2026-09-06 |
| RULE-20260906-P2S2 | 组合内核阶段 2 第一批：纯前端 / Wails / Web 分离三族组件化 | 兼容样例 4 → 12、组件 7 → 22：frontend-static 四变体（reactOnly / vueOnly / reactOnly-ai / vueOnly-ai，普通与 AI 各一个骨架，AI 变体的 ai-AGENTS.md 由前端组件补声明并保持「模块先于子规则」的渲染顺序）、Wails 两组合（react / vue 差异 14 处槽位化）、Web 前后端分离两组合（subrule-index 存在结构性差异，由前端组件整表持有）。组件根标量改为按 JSON 存在性判定伪声明（`DeclaredScalarFields` + `AnnotateComponentRootPresence`），修复「刻意声明且恰好等于类默认值」被误剥的问题（vueWailsExe destructive-nouns 覆盖 family 基线场景）。生成内容零漂移（`--emit` 与基线逐字节一致），版本保持 1.51.0 | 已完成验证 | ZCode | DONE | 高 | 2026-09-06 |
| RULE-20260906-P2S3 | 组合内核阶段 2 完成：剩余 16 Profile 组件化，28/28 兼容样例全覆盖 | 组件 22 → 37、兼容样例 12 → 28：剩余 Profile 以「单一组合 = 一个全量 form 组件」迁移（直接携带 Profile 完整 root / subrules / design-system，含 reactLinkdotNet-ai 的模块前置 ai-AGENTS.md）；fs-plain 三组件重组为两个单组件（原骨架缺 frontend 钳制会与 generalFrontend 冲突）；web-dotnet 五组件补 `ai-capability=no` 钳制（与 reactLinkdotNet-ai 解耦）。`D:\temp\ruletest` 真实生成验证：经典选择、检测推荐、Q5 能力开关（关 doc 后根导航 0 残留）、问答决策树四条链路全部通过，产物与基线逐字节一致。185 测试全绿，`--emit` 与基线逐字节一致，版本保持 1.51.0。阶段 2 完成，下一步进入阶段 3（向导/检测器切换 Selection 入口） | 已完成验证 | ZCode | DONE | 高 | 2026-09-06 |
| RULE-20260906-P2S4 | 组合内核阶段 3：问答/检测/汇总切换 Selection 入口 | 新增 `SelectionEngine`（组件驱动问答）：存续组件按已回答维度过滤，只问「存续组件间仍有分歧」的维度，唯一取值自动推导（配方来源 derived），选项经可行性剪枝（取值后至少剩一个兼容样例，与旧引擎「选项从 Profile 推导」的保证对齐），`SimulateAll` 全路径仿真接入 `--check`（每条交互路径可解析且唯一映射兼容样例，28 样例全部可达）。`Program` 交互切换：问答 / 检测推荐 / 检测补答均产出 `Selection` 走 `CompositionPlanner` + 组合渲染路径，经典大类 → Profile 保留为「经典选择（兼容）」；新增 `--plan` 命令（显式维度 → 自动推导 → 打印组件与文件清单，分歧维度报错不补全）。配方记录增补 `selection` + `component_ids`（唯一映射兼容样例时回填 profile 三件套，旧路径输出字节不变）。ruletest 实测：web+react+.NET（自动推导 3 维度）、desktop+react+go（Wails）、返回上一步改道 console 三条链路产物与基线一致。191 测试全绿，`--emit` 与基线逐字节一致，版本保持 1.51.0 | 已完成验证 | ZCode | DONE | 高 | 2026-09-06 |
| RULE-20260906-P2S5 | 组合内核阶段 4：配方重放与兼容命令收尾 | 新增 `RecipeReplayer` + `--recipe <配方.json> [目标目录]`：按配方记录重放生成，selection 重建选择（旧配方仅有 profile_id 时经兼容样例反查）、组件集合与当前 manifest 不一致显式报错不静默生成、能力开关按配方重放（空 = 默认集）、版本差异以提示说明、design-system 已定制保留语义与交互一致，回写配方刷新版本并补 .gitignore。`--emit-all-legacy` 作为旧路径批量预览的规范名落地（`--emit` 保留为别名）。ruletest 实测：交互生成 → 清空目录 → 按配方重放，产物与基线逐字节一致；伪造 1.40.0 旧配方（仅 profile_id）反查重放 reactOnly 成功。6 个重放单元测试（逐字节还原/旧配方反查/篡改报错/能力子集/约束拒绝/空配方报错），197 测试全绿，`--check` 通过，版本保持 1.51.0 | 已完成验证 | ZCode | DONE | 高 | 2026-09-06 |
| RULE-20260906-P2S6 | 组合内核阶段 5：子规则槽位机制与 electron 内容收敛 | 生成器支持子规则骨架槽位：`Subrule.slots`（文件级）/ `Profile.slots`（旧路径）/ `CompositionPlan.Slots`（组件并集）三级取值，`ApplyFileSlots` 渲染期替换并以残留占位硬报错兜底。electron 家族 8 份按 backend 味道分化的文件收敛为 4 份骨架（frontend-react/vue-AGENTS.md + design-system-react/vue.yaml，react 对 5+3 槽位、vue 对 5+8 槽位，共消除约 1100 行重复），dotnet/main 槽位值由前端组件与旧 Profile 提供。计划内内容变更：2 份 design-system 的 `use-rule` 旧名修正为 `use-ai-rule`（首次随 1.52.0 生成生效）。版本 1.51.0 → 1.52.0。验证：28 兼容样例等价全绿、emit 161 个差异文件逐一核验均为计划内差异（版本戳 + 旧名修正）、ruletest 真实生成（web 分离路径 + electron 骨架槽位路径）版本还原后与基线逐字节一致。197 测试全绿 | 已完成验证 | ZCode | DONE | 高 | 2026-09-06 |
| UPG-20260906-1 | 第二梯队：--upgrade 项目升级（本地修改检测） | 配方记录增补 `file_hashes`（生成内容 SHA256，向后兼容：交互/classic/wizard/重放四条路径统一写入）；`RecipeReplayer.PlanUpgrade` 比对磁盘哈希识别本地修改过的文件，默认保留只更新未修改文件（保留文件沿用旧哈希，下次升级仍可检测），`--force` 全部覆盖并重算哈希；早期配方无哈希时降级为「按配方直接重放」并明确提示。新增 `--upgrade [目录] [--force]` 命令。ruletest 实测完整闭环：生成 → 本地改 2 文件 → 升级保留修改写入其余 8 个 → force 覆盖并刷新哈希 → 再升级幂等。4 个新单元测试（未修改全更新/修改保留沿用旧哈希/旧配方不可检测/哈希一致性），201 测试全绿，生成内容零漂移，版本保持 1.52.0 | 已完成验证 | ZCode | DONE | 高 | 2026-09-06 |
| UPG-20260906-2 | 第二梯队：self-contained 交付（mac/linux 与 CI） | 发布从 win-x64 框架相关改为三平台 self-contained 单文件矩阵（win-x64 / linux-x64 / osx-x64，目标机无需 .NET 运行时）：`publish.ps1` 支持 `-Rids` 矩阵参数与 `-FrameworkDependent` 回退，产物命名 `dist/use-ai-rule-<rid>`（win-x64 额外保留 `dist/use-ai-rule.exe` 兼容别名）；新增 `app/RuleAssembler/publish.sh`（Linux/mac/CI 入口，RID 语义与 ps1 一致，`.gitattributes` 强制 `*.sh` LF）；`build-use-ai-rule.bat` 默认快速打 win-x64、传参 `all` 打全矩阵；Gitee Go CI 新增「self-contained 发布矩阵 + linux 产物跑 `--check` 冒烟」两步，push 触发补 `guided-wizard-detect`；目标项目 gitignore 模板增补无扩展名 `use-ai-rule` 条目（mac/linux 本体），规则文件内容不变、版本保持 1.52.0。验证：本地三平台发布产物齐全（各约 75MB），win 产物独立运行 `--check` 通过，201 测试全绿 | 已完成验证 | ZCode | DONE | 高 | 2026-09-06 |
| UPG-20260906-3 | 第二梯队：规则效果反馈闭环（命中/违反回传 fragments） | 生成规则内置反馈机制：`collaboration-AGENTS.md` 新增「规则效果反馈」小节（违反/冲突/缺失/有用四类条目，记入目标项目根目录 `.ai-rule-feedback.md`，长期登记不清理、随项目提交，与「重复问题回顾」的本地落实衔接），根文件触发式协作规则同步提示该章节；仓库侧新增 `doc/规则效果反馈承接.md`（收集 → 登记 → 按 §2.4 归属 → 改 fragments → 验证升版 → 回执到原反馈文件，`--upgrade` 闭环），仓库根 `AGENTS.md` 增承接段。新增 `EveryProfile_CollaborationRule_ContainsFeedbackLoop` 断言 28 Profile 全覆盖。规则内容变更 1.52.0 → 1.53.0 | 已完成验证 | ZCode | DONE | 高 | 2026-09-06 |
| RULE-20260906-TAURI | 家族组件化第一批：Tauri 家族拆解与 vueTauri 解锁 | reactTauriExe 从「整仓 form 组件」拆为维度组件（`tauri-desktop-shell` + `tauri-frontend-react` / `tauri-frontend-vue` + `tauri-backend-rust`；react / vue 差异仅 subrule-index 前端行 1 处槽位 `tr-subr-2`）。backend 维度新增 `rust` 取值：reactTauriExe traits 由权宜复用的 `go` 修正为 `rust`（存量配方 `--upgrade` 时按组件集不一致显式报错，引导重新生成而非静默错乱）。backend / doc / infra 三份片段迁至 `shared/skeletons/tauri/` 双路径共用；新增 vueTauri Profile（Vue 3 + Tailwind + Radix Vue + TanStack Query Vue + Pinia + VeeValidate）与 `arch/vueTauri` 前端、设计系统基线。检测器 `tauri.conf.json` 补 backend=rust 证据，react / vue 双向收敛到对应 Profile。组件 37 → 40、Profile 28 → 29、兼容样例 28 → 29；`--check` 新旧两条渲染路径逐字节等价全绿，207 测试通过。规则内容变更（新增 Profile + 全量版本戳）1.53.0 → 1.54.0 | 已完成验证 | ZCode | DONE | 高 | 2026-09-06 |
| RULE-20260906-DNW1 | dotnet Web 家族组件化第一批：纯后端分层/单层与 Blazor 全栈（保等价） | dotnet-layered / dotnet-core-single 由整仓 form 组件改为 backend 组件（`dotnet-backend-layered` / `dotnet-backend-single`，when 收敛为 form=backend-only + backend=dotnet + arch-style 三维，root 与文件零改动）；dotnetBlazor / dotnetBlazorWebApp 拆为 `web-frontend-blazor-traditional` / `web-frontend-blazor-webapp`（按 blazor-flavor 分叉，各持全量 root 与 ui-AGENTS.md）+ `web-backend-blazor`（两形态共用 backend / doc / infra 与 design-system）。组件 40 → 41（form 18 → 14、frontend 12 → 14、backend 8 → 9），Profile / 兼容样例保持 29；`--check` 两条渲染路径逐字节等价全绿，209 测试通过（新增纯后端 dotnet 与 Web Blazor 问答路径用例）。纯结构变更，生成内容零漂移，版本保持 1.54.0。下一步：reactLinkdotNet-ai 组件化与 web+layered 维度互换解锁 | 已完成验证 | ZCode | DONE | 高 | 2026-09-06 |
| RULE-20260906-DNW2 | dotnet Web 家族组件化第二批：arch-style 成为 web+dotnet 真实维度，解锁分层/单项目互换 | reactLinkdotNet / vueLinkdotNet 归位为 arch-style=layered（其后端内容本就是多项目 Clean Architecture，原 single 标签是维度引入时的占位，与 tauri 的 backend=go 同类；存量配方 --upgrade 按组件集不一致显式引导重新生成）；新增 reactLinkdotNetSingle / vueLinkdotNetSingle（单项目整洁架构后端，内容收敛在 shared/skeletons/web-dotnet/backend-single-AGENTS.md，前端组件跨架构复用）与 web-backend-react-single / web-backend-vue-single 组件；检测器对「web + csproj 数量」证据此前无法收敛（layered 证据与 single 样例冲突后按最接近处理），现 3+ csproj 收敛 reactLinkdotNet、单 csproj 收敛 single 变体（AI 与否留待补答）。组件 41 → 43、Profile / 兼容样例 29 → 31，--check 等价全绿，216 测试通过。规则内容变更（新 Profile + desc 更新）1.54.0 → 1.55.0 | 已完成验证 | ZCode | DONE | 高 | 2026-09-06 |
| RULE-20260906-DNW3 | web+python 分离组合解锁：react/vueLinkPython 落地 | 新增 reactLinkPython / vueLinkPython（React/Vue SPA + FastAPI 分离部署）与 web-python-shell、web-frontend-python-react/vue、web-backend-python-react/vue 五个组件；arch/python/backend-AGENTS.md 的「阶段」标题与入场要求槽位化（{{slot:py-stage-*}}），纯后端（阶段 1）与 web（阶段 2）两种阶段编号共用同一份后端内容，python Profile 与 form 组件补旧值保持逐字节等价；web 前端组件的后端口径槽位（wb-unde-1 / wb-sc-search_n / subrule-index backend 行）从前端组件迁移到后端组件持有，dotnet 四后端接收原值、python 后端提供 Python 措辞——前端措辞与后端措辞自此按组件归属分家，可跨后端栈复用。检测器对「前端工程 + pyproject」组合此前无候选可收敛，现直接命中 react/vueLinkPython。组件 43 → 48、Profile / 兼容样例 31 → 33，--check 等价全绿。规则内容变更（新 Profile）1.55.0 → 1.56.0 | 已完成验证 | ZCode | DONE | 高 | 2026-09-06 |
| RULE-20260906-CONST | 约束化可行性判定收尾：钳制关系写全并接入问答剪枝 | manifest constraints 从 2 条扩到 25 条，从全部 33 个兼容样例推导并程序化校验零冲突：壳 ↔ 后端（tauri→rust、wails→go、electron 仅 dotnet / 主进程并禁 go/rust/python/node）、形态钳制（纯后端无前端无壳、web / 移动无壳、静态站限定 html 无后端）、前端归位（blazor→web+dotnet、avalonia/wpf/console/flutter/maui 的形态与后端、layered 仅 dotnet）、桌面 react/vue 禁无壳。引擎：CompositionPlanner 抽出 SatisfiesConstraints（forbid 立即生效、require 只对已回答维度生效的部分选择语义），SelectionEngine.IsViable 先过钳制约束再做样例剪枝——约束成为不依赖样例清单的独立防线，--plan 与组合解析对违反钳制的组合保持显式报错。纯结构与代码变更（剪枝行为与样例推导一致，零行为回归），版本保持 1.56.0；--check 33 样例等价全绿，225 测试通过（新增 3 个约束违反用例） | 已完成验证 | ZCode | DONE | 高 | 2026-09-06 |
| RULE-20260907-NUXT | 前端×node 矩阵补格：Nuxt 3 全栈（nuxtFullstack） | 补齐 vue×node 缺格（react 侧已有 nextFullstack，node 选项语义即「Node 全栈」）：新增 nuxtFullstack Profile 与整仓 form 组件（Nuxt 3 文件路由 / Nitro server routes / server-utils 分层 / Prisma / VeeValidate+Zod / Pinia 轻状态 / @nuxt/fonts），五份片段在 arch/nuxtFullstack/ 独立成套（与 nextFullstack 同构互不共享，按 reactLinkdotNet-ai 同口径保留整仓，待第三个同构全栈成员出现再拆骨架）；检测器新增 nuxt.config.ts / package.json 依赖 nuxt 特征（web + vue + node 三证据直接收敛，PackageJsonWithoutBackend 排除 Nuxt）。Profile / 兼容样例 33 → 34、组件 48 → 49，--check 等价全绿。规则内容变更（新 Profile）1.56.0 → 1.57.0 | 已完成验证 | ZCode | DONE | 高 | 2026-09-07 |

## 1. 当前目标

组合内核已全量落地：问答 / 检测 / 补答均产出 `Selection` 走 `CompositionPlanner` 与组合渲染路径；`Profile` 保留为旧渲染入口与兼容样例锚点（`--check` 对全部样例做两条渲染路径的逐字节等价比对）。manifest 的 `components` / `constraints` / `compatibilityCases` 是维度组合的事实来源，25 条钳制约束已接入问答剪枝；新增技术栈的边际成本是「加组件 + 证据映射 + 测试」。

当前交互口径：

```text
Q1 形态：Web 前后端分离 / 桌面 / 移动 / 纯后端 / 通用
Q2 前端：无 / React / Vue / Blazor / 静态 HTML（按形态追加已有技术栈，桌面追加 Avalonia / WPF / 控制台）
Q3 壳：桌面或混合应用时显示 Tauri / Wails / Electron
Q4 后端：无 / .NET 分层 / .NET 单层 / Python FastAPI / Go（Wails）/ Rust（Tauri）/ Node（Next.js / Nuxt 全栈）（选壳后仅问该壳下的分歧，如 Electron 的 主进程 vs .NET sidecar；Web 形态按前后端栈组合追问架构分层）
Q5 能力：doc、infra 默认勾选；AI、design-system 按能力和场景选择
    -> 汇总确认
    -> 列出实际生成文件
    -> 生成
```

“默认勾选”只适用于 Q5 能力，不代表存在默认技术栈组合。证据不足时不得隐式补出一个 Profile。

2026-09-04 问答顺序调整为「前端先于后端」：原生桌面（Avalonia / WPF / 控制台）与混合壳（Electron / Wails / Tauri）的分歧在界面维度，先问后端会让 C# 桌面用户的「.NET」回答进入 Electron 分支、Avalonia / WPF 选项永不出现；先问界面后，原生桌面组合直接收敛，后端问题只对混合壳与 Web 形态出现。

2026-09-06 reactLinkdotNet-ai 暂保留整仓 form 组件：其 root 发散覆盖 extra-sections / intro-quote / verify-quote / 阅读顺序等整套「AI Agent 产品 MVP」人设，前端/后端/doc/infra 文件也整体独立于 reactLinkdotNet；拆成 AI 骨架 + 组件只是换标签、无内容共享与互换收益。待出现第二个 AI 变体（如 vueLinkdotNet-ai）再抽 AI 骨架，届时两份人设的差异面才真实可见。与原生桌面家族「选前端即收敛、整仓合理」同一口径。

2026-09-06 问答顺序再调整为「壳先于后端」（形态 → 前端 → 壳 → 后端）：混合壳桌面下后端一答完候选即唯一，壳问题从未真正出现过，Electron 自 1.39 移除 frontend=electron 取值后在问答里没有任何显式选项，用户只能靠「无后端」间接落进去；把壳问题提前后，桌面 + React/Vue 会先显示 Electron / Tauri / Wails 选项，选 Electron 后后端问题只区分主进程（无后端）与 .NET sidecar。「无后端」文案同步补充桌面壳主进程口径。

## 2. 不可破坏的扩展口

后续新增或修改 AI Rule 时，必须优先扩展 manifest 和模块配置，不得把组合判断重新硬编码成大量 `if (profile.Id == ...)`。

### 2.1 Manifest 扩展口

完整组件化阶段预留以下结构；P1 可以先增加其中需要的最小字段，但不能设计成只能支持固定几个 ID：

```json
{
  "forms": {},
  "choices": {},
  "components": [],
  "constraints": [],
  "compatibilityCases": []
}
```

组件至少能够声明：

- `id`、`kind`、`label`、`desc`。
- 适用条件 `when`。
- 依赖条件 `requires` 和冲突条件 `conflicts`。
- 提供的输出文件和合并方式。
- 根变量或根导航贡献。
- 检测证据映射。

新增一个前端、后端、桌面壳或能力模块时，原则上只增加模块、约束、检测映射和测试，不复制一整套 Profile。

### 2.2 维度和能力不得写死

用户选择应逐步使用 `Dictionary<string, string>` 表示维度，使用能力 ID 集合表示 Q5；展示顺序和选项文案由 manifest 控制。当前保留 `ProfileTraits` 是迁移兼容，不得把它继续扩展成永久的固定字段清单。

P1 不要求一次性实现“新增任意维度完全不改 C#”，但新增能力不得增加新的 Profile 专属分支。P2 完成后，新增常规组件应无需修改渲染器核心流程。

### 2.3 输出文件必须有结构化归属

不能只通过输出文件名推断能力归属。每个输出声明应能区分：

- `generated`：本次确实生成的文件。
- `referenced-existing`：根规则提到但生成器不负责创建的文件，例如目标项目自己的 `TASK.md`。
- `feature`：该输出属于 `doc`、`infra`、`ai`、`design-system` 或其他能力。
- `merge`：`replace` 或显式 `append`。

根文件中的 Markdown 文案可以继续展示给用户，但 `--check` 不得通过解析自然语言来推断输出契约。

推荐的输出声明形态如下，字段名可以按现有模型习惯调整，但语义必须保留：

```json
{
  "path": "doc-AGENTS.md",
  "kind": "generated",
  "feature": "doc",
  "merge": "replace",
  "rootReferences": ["doc-AGENTS.md"]
}
```

### 2.4 后续 AI Rule 变更归属

以后修改规则时，先按影响范围选择维护入口：

| 变更类型 | 首选维护位置 | 处理要求 |
| --- | --- | --- |
| 所有项目都适用的协作规则 | `fragments/shared/` 或根骨架 | 评估所有受影响 Profile，补全回归说明 |
| 某一项目形态或架构族共用规则 | 对应 family / skeleton 模块 | 只影响该族，禁止复制到每个 Profile |
| 单一技术栈规则 | `backend` / `frontend` / `shell` 组件 | 在 manifest 中声明适用条件和输出归属 |
| 可选能力规则 | `capability` 模块 | 用 feature ID 控制，不把能力写死进 Profile |
| 只有一个组合的真实差异 | 显式 delta 或兼容层 | 记录原因；若重复出现则转为独立组件 |

每次新增技术类型或能力都必须完成以下清单：

- 规则源文件放入明确的 shared、skeleton、component 或 capability 目录。
- Manifest 增加选项、组件、适用条件、依赖/冲突关系和输出声明。
- 项目检测增加证据映射；无法检测时保留未确认状态，不补默认组合。
- 增加至少一个 Selection/recipe 测试和一个生成结果回归样例。
- 更新规则版本、README；若只改代码且生成正文不变，按版本口径处理。
- 检查根导航、文件清单、模块引用和输出合并是否一致。

禁止以下做法：

- 为了一个新组合复制现有 Profile 全目录。
- 在 `Program`、`ProjectDetector` 或 `RuleRenderer` 中增加技术栈 ID 专属分支作为长期方案。
- 只修改规则正文而不更新 manifest 的输出声明、版本和回归样例。
- 通过删除根导航文字掩盖实际模块缺失。

## 3. P1 本期任务

P1 目标是低风险增强现有 Profile 管道，不提前伪装成完整组合内核。

| 编号 | 任务 | 状态 | 依赖 |
| --- | --- | --- | --- |
| P1-1 | 结构化根声明与 `--check` 一致性校验 | 已完成 | 无 |
| P1-2 | 检测部分结果、未确认字段和补答 | 已完成 | P1-1 的输出契约可复用，但代码上可并行 |
| P1-3 | Q5 能力开关与根导航联动 | 已完成 | P1-1 |
| P1-4 | 扩展约束、兼容回归和文档收口 | 已完成 | P1-1、P1-2、P1-3 |

> P1 已全部完成（版本 1.40.0）。2026-09-02 追加两个批次：1.41.0 纯前端组合 `reactOnly` / `vueOnly` / `generalFrontend`（`form=web + backend=none`，frontend-static 家族），问答 Q2 出现「无后端」选项、裸前端项目检测候选包含纯前端组合；1.42.0 纯前端 AI 变体 `reactOnly-ai` / `vueOnly-ai`，新增 `ai-capability-frontend` 模块（`shared/skeletons/frontend-ai/`：浏览器侧口径的 ai-AGENTS.md + frontend/doc/infra 三个追加段），密钥不下发浏览器、模型调用走托管网关 / 代理。两批均已完成（与 1.43.0 文档拆分同批提交，manifest 版本直接从 1.40.0 进入 1.43.0，git 历史中无独立的 1.41.0 / 1.42.0 版本点）。后续接手时：日常维护按 §2 的扩展口执行；P2 按 §5 的触发条件评估启动。

### P1-1 结构化根声明与校验

实施要求：

- 为 Profile 或其规则声明增加结构化输出元数据，不从 `target-list` / `subrule-index` Markdown 反向解析文件。
- 明确区分“本次生成文件”和“根规则引用的目标项目现有文件”。当前 `TASK.md` 不由生成器释放，不得被误判为生成缺失。
- 输出声明至少包含 `generated`、`referenced-existing`、能力归属和 `merge`（`replace` / 显式 `append`）。
- `RootConsistencyValidator` 对比结构化声明、`RuleRenderer.Plan` 实际产物和能力归属。
- 报错必须包含 Profile ID、输出路径、声明状态和实际状态。
- 根变量中的 Markdown 继续作为展示文案；`--check` 不得依赖自然语言正则推断输出契约。

验收标准：

- 当前 `fragments/manifest.json` 中的 21 个 Profile 全部通过 `--check`。
- 人为增加一个不存在的 `generated` 输出时，`--check` 失败并指出具体条目。
- `TASK.md` 按 `referenced-existing` 处理时校验通过，误标为 `generated` 时校验失败。
- `CLAUDE.md / GEMINI.md / GROK.md` 等组合展示文本不会导致错误的重复或缺失判断。
- 原有 Profile 渲染结果没有计划外变化。

### P1-2 检测部分结果和补答

当前 `DetectionResult` 必须能够表达尚未收敛的草稿状态，至少包含：

```text
confirmed：已确认的维度和能力
unconfirmed：没有足够证据的维度
candidates：当前仍可能的 Profile 或组合
reasons：已确认的检测证据
notes：冲突和待确认说明
legacyProfileId：迁移期可选，仅供旧路径兼容
```

实施要求：

- Electron + .NET 但没有 React/Vue 依赖时，`frontend` 必须是未确认，不得默认 React。
- `--detect` 对部分有效但未收敛的结果打印待确认字段和候选，不伪造完整 Profile 推荐。
- 无参数交互进入补答流程时保留已确认值，只询问未确认或冲突字段。
- 补答完成后的配方可以保留 `mode=detected`，但要记录检测确认和人工补答来源。
- 更新当前“无渲染层默认 React”的测试预期，增加部分检测和补答流程测试。

验收标准：

- Electron + .NET + 无渲染层依赖的目录，`--detect` 明确输出 `frontend` 未确认。
- 交互补答 Vue 后落到 Vue + Electron + .NET 对应组合。
- 已确认的 `form`、`backend`、`shell` 在补答过程中不被覆盖。
- 空目录仍然不生成文件。
- 部分 `DetectionResult` 不要求存在可反查的 Profile ID。

### P1-3 Q5 能力开关与根导航联动

统一使用 `features` 保存能力选择：

```text
doc
infra
ai-capability
design-system
```

默认行为：

- `doc`：适用时默认勾选。
- `infra`：适用时默认勾选。
- `ai-capability`：默认不勾选，只有存在兼容模块时显示。
- `design-system`：默认不勾选，只有存在界面模块和设计系统资源时显示。

实施要求：

- AI 不得同时由独立问答和 Q5 勾选作为两个事实来源。Q5 只负责是否启用，未来模型、供应商等配置再进入 AI 专属子问题。
- 能力归属挂在模块/输出声明上，不只依赖输出文件名或单个字符串标签。
- `RuleRenderer.Plan` 按 `features` 过滤最终输出。
- 取消能力后，根文件的 `target-list`、`subrule-index`、`reading-order`、阶段职责和其他导航文案都不得引用不存在的子规则。
- Q5 选择必须进入配方记录；旧 `profile_id` 保留为迁移兼容，同时增加 `features` 和选择来源字段。
- 不可用的 `design-system.yaml` 或 AI 模块不得因为默认值被选中。

验收标准：

- 取消 doc 后没有 `doc-AGENTS.md`，根入口也没有指向它的导航，配方可还原该状态。
- 取消 infra 后没有相关 infra 文件，根入口也没有指向它的导航。
- AI 选中后生成 AI 能力文件，未选中时不生成。
- 设计系统只在有界面和资源时可选。
- 默认适用能力的生成结果与当前 1.39.0 基线一致。
- 测试每个能力独立关闭、全部关闭、AI 开启、设计系统开启和不可用能力隐藏。

### P1-4 扩展约束、兼容回归和文档收口

- 将 `when`、`requires`、`conflicts`、`merge`、能力归属和检测证据纳入 manifest 校验。
- 至少覆盖 `.NET + React`、`.NET 分层 + Web`、`React + Wails`、`React + Electron + .NET`、纯静态 HTML、纯后端 Python。
- 21 个旧 Profile 继续作为兼容样例，默认路径与改造前比较，差异只允许出现在已批准的版本元数据或明确的能力开关结果。
- 任何新增模块必须同时补资源文件、manifest 条目、适用约束、输出声明、检测映射、单元测试和示例配方。
- 更新 `README.md`、`app/RuleAssembler/README.md` 和本文件；

### 文档规则维护：需求与设计按业务模块拆分

| 子任务 | 状态 | 说明 |
| --- | --- | --- |
| 需求、设计、契约职责拆分 | 已完成 | 需求文档只描述业务事实，设计文档通过 `REQ-` 编号追溯需求，契约字段不在需求/设计文档中重复维护 |
| 按模块/功能建立文档目录约定 | 已完成 | 新文档使用 `doc/index.md`、`doc/requirements/<module>/<feature>.md`、`doc/design/<module>/<feature>.md`，跨模块设计使用 `architecture.md` |
| 兼容旧文档路径并补充迁移规则 | 已完成 | 旧 `doc/requirement.md` 与 `doc/design/设计文档.md` 只作为迁移入口，不再继续追加新章节 |
| 模板、版本、回归验证与记录同步 | 已完成 | 更新共享片段、内联模板、manifest、README、测试 |

## 4. P1 实施顺序和提交规则

P1 各项可以独立回滚，但不是互不依赖的平行改动。推荐顺序：

```text
P1-1 结构化输出契约
    -> P1-2 检测部分结果
    -> P1-3 能力开关和根导航
    -> P1-4 回归与文档
```

### 4.1 版本口径

- 仅代码校验、检测逻辑和测试变更，且生成内容不变时，不修改规则版本。
- Q5 能力开关导致生成路径增加或根导航生成逻辑变化时，更新 `fragments/manifest.json` 的 `vars.version` 和 `last_updated`；版本建议从当前 `1.39.0` 进入下一 minor 版本。

### 4.2 回归命令

```powershell
dotnet run --project app/RuleAssembler -- --check
dotnet run --project app/RuleAssembler -- --emit D:/tmp/rules-before
dotnet test test/RuleAssembler.Tests
```

P1 新增能力选择不能直接用旧的“输出全部 Profile”命令表达，应增加测试入口或 recipe fixture，明确验证具体 Selection 和 `features`。不要为了测试能力开关而修改旧 `--emit` 语义。

### 5. P2 完整组合内核

P2 已于 2026-09-06 启动，阶段 1-5 全部完成（内核 → 34 Profile 组件化 → Selection 入口切换 → 配方重放 → 内容收敛），并在 2026-09-14 完成首批组件输出合并与前端公共片段抽取；当前规则版本 1.66.0。收尾评估与后续方向：

1. ~~增加 `Selection`、`Component`、`Constraint`、`CompositionPlan` 和 `CompositionPlanner`，保留旧 Profile 渲染路径。~~（已完成）
2. ~~将全部 Profile 迁移为组件，`compatibilityCases` 覆盖 34/34。~~（已完成）
3. ~~将向导、检测器和汇总界面切换到 Selection；Profile 列表移出主入口，仅保留兼容命令。~~（已完成）
4. ~~配方记录 `selection + component_ids + features`；`--recipe` / `--emit-all-legacy`。~~（已完成）
5. ~~内容收敛：electron 家族 8 份按味道分化文件 → 4 份骨架 + 槽位（约 1100 行重复消除）。~~（已完成；wails/web 分离的 frontend 差异是 react/vue 框架维度而非 backend 维度，属合理差异不强收敛）
6. `profiles` 节点评估：**保留**。它仍是三件事的事实来源——检测器候选过滤（ProjectDetector 按 traits 收敛推荐）、经典兼容入口、`--emit-all-legacy` 回归基线；组件等价校验保证两条路径产物一致，删除无收益且破坏回归能力。待检测器与经典入口也切换到组件口径后（远期）再评估。
7. 第二梯队（已全部完成，见 UPG-20260906-1 / UPG-20260906-2 / UPG-20260906-3）：~~目标项目 `--upgrade`（配方 + 内容哈希检测本地修改）~~（已完成）；~~self-contained 交付（覆盖 mac/linux 与 CI）~~（已完成：三平台 self-contained 单文件矩阵 + CI 发布与冒烟）；~~规则效果闭环~~（已完成：目标项目 `.ai-rule-feedback.md` 登记 + `doc/规则效果反馈承接.md` 承接，1.53.0）。

P2 的完成标准是：新增常规前端、后端、壳或能力模块时，不需要复制完整 Profile，也不需要修改渲染器核心分发流程。

## 6. 当前事实和注意事项

### 接手必读

- `ResourceStore` 把 `fragments/**` 以 `Fragments.` 前缀嵌入程序集；manifest 校验失败会直接抛异常。
- `ManifestLoader.MergeFamilyDefaults` 在 JSON 层做 family rootDefaults 继承，新的结构化输出字段不能破坏该过程。
- `OutputWriter.WriteFiles` 自带 staging、backup 和回滚；配方记录必须在规则文件写入成功后再写。
- 预设产物回归基线：`dotnet run --project app/RuleAssembler -- --emit D:/tmp/rules-before`；该命令当前只用于旧 Profile 预览，不写配方记录。
- 当前共有 34 个 Profile（1.43.0 批次与 reactElectron / vueElectron / dotnetBlazorWebApp 及后续 Tauri 双前端、web 单项目、web+python、Nuxt 等新增），旧渲染入口是 `RuleRenderer.Plan(manifest, profile)`；组合路径入口是 `RuleRenderer.Plan(manifest, plan, features)`，两条路径共用统一渲染核心，`--check` 保证兼容样例逐字节等价。
- 当前有 `core`、`ai-capability`（.NET 后端口径）和 `ai-capability-frontend`（浏览器侧口径）三个顶层 module，不能把现状 module 数量误认为已经完成组件化。
- 组合内核阶段 1-5 已全部落地：manifest 的 `components` / `constraints` / `compatibilityCases` 由 `CompositionPlanner` 消费；组件根骨架通过 `{{slot:x}}` 槽位注入 backend 差异，组件间覆盖同一根字段、同名输出、重复槽位都会报错。为既有组合补组件时，必须先在 `compatibilityCases` 加样例，让 `--check` 的逐字节等价校验兜底。
- 当前 `Subrule.sources` 已支持多源拼接，但只有显式声明时才能合并；重复输出不得静默覆盖。
- 允许修改 `fragments/manifest.json` 和新增模块元数据；禁止为了完成 P1 无计划地重写已有规则正文。
- Windows / PowerShell 文档和命令中禁止使用 `&&` 链式命令；新增注释统一使用中文。
