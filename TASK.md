# TASK：组合式 AI Rule 生成器改造（分期执行）

> 本文件是当前任务的 AI 交接清单。接手后从“当前任务”继续执行，完成后可删除。
>
> 分支：`guided-wizard-detect`
>
> 北极星方案：[doc/组合式AI-Rule生成器改造方案.md](doc/组合式AI-Rule生成器改造方案.md)
>
## 0. 当前维护任务

| 任务 ID | 任务名称 | 任务目标 | 当前阶段 | 负责人 AI | 任务状态 | 优先级 | 最近更新时间 |
| --- | --- | --- | --- | --- | --- | --- | --- |
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

## 1. 当前目标

当前生成器仍然以 `Profile` 为组合和检测的最终单位。近期不立即删除现有 Profile，而是先完成低风险改造，并为完整组件化保留稳定入口。

本任务的目标顺序：

```text
现有 Profile 兼容基线
    -> 结构化输出声明
    -> 部分检测结果与补答
    -> doc / infra / AI / design-system 能力开关
    -> 完整 Selection -> Component -> CompositionPlan（按触发条件启动）
```

当前交互要逐步靠近：

```text
Q1 形态：Web 前后端分离 / 桌面 / 移动 / 纯后端 / 通用
Q2 前端：无 / React / Vue / Blazor / 静态 HTML（按形态追加已有技术栈，桌面追加 Avalonia / WPF / 控制台）
Q3 壳：桌面或混合应用时显示 Tauri / Wails / Electron
Q4 后端：无 / .NET 分层 / .NET 单层 / Python FastAPI / Go(Wails)（选壳后仅问该壳下的分歧，如 Electron 的 主进程 vs .NET sidecar）
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

P2 已于 2026-09-06 启动，阶段 1-5 全部完成（内核 → 28 Profile 组件化 → Selection 入口切换 → 配方重放 → 内容收敛），见 RULE-20260906-P2S1 至 P2S6；当前规则版本 1.53.0。收尾评估与后续方向：

1. ~~增加 `Selection`、`Component`、`Constraint`、`CompositionPlan` 和 `CompositionPlanner`，保留旧 Profile 渲染路径。~~（已完成）
2. ~~将全部 Profile 迁移为组件，`compatibilityCases` 覆盖 28/28。~~（已完成）
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
- 当前共有 28 个 Profile（1.43.0 批次与 reactElectron / vueElectron / dotnetBlazorWebApp 等后续新增），旧渲染入口是 `RuleRenderer.Plan(manifest, profile)`；组合内核阶段 1 新增 `RuleRenderer.Plan(manifest, plan, features)` 组合入口，两条路径共用统一渲染核心，`--check` 保证兼容样例逐字节等价。
- 当前有 `core`、`ai-capability`（.NET 后端口径）和 `ai-capability-frontend`（浏览器侧口径）三个顶层 module，不能把现状 module 数量误认为已经完成组件化。
- 组合内核阶段 1 已落地：manifest 的 `components` / `constraints` / `compatibilityCases` 由 `CompositionPlanner` 消费；组件根骨架通过 `{{slot:x}}` 槽位注入 backend 差异，组件间覆盖同一根字段、同名输出、重复槽位都会报错。为既有组合补组件时，必须先在 `compatibilityCases` 加样例，让 `--check` 的逐字节等价校验兜底。
- 当前 `Subrule.sources` 已支持多源拼接，但只有显式声明时才能合并；重复输出不得静默覆盖。
- 允许修改 `fragments/manifest.json` 和新增模块元数据；禁止为了完成 P1 无计划地重写已有规则正文。
- Windows / PowerShell 文档和命令中禁止使用 `&&` 链式命令；新增注释统一使用中文。
