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
