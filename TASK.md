# TASK：生成器问答向导 + 项目检测 + 配方记录（已完成）｜Spectre.Console 交互层（已完成）

> 本文件是当前任务的 AI 交接清单：中断后接手的 AI 从「剩余任务」继续即可，全部完成后可删除本文件。
> 分支：`guided-wizard-detect`（基于 `remove-snapshots` 创建）。不要动 `fragments/` 下任何规则片段内容。

## 主任务状态：全部完成（验证记录见文末）

问答决策树、项目检测、配方记录 + gitignore、20 Profile traits/wizard、单元测试 29/29、--check 仿真、--emit 回归零 diff、文档与 rulelog 均已完成。原「剩余任务 #1-#8」已全部落地；其中 #2（ConsoleWizard + Program 集成）按手写编号输入完成，因此下方 Spectre.Console 任务按既定口径执行「交互层整体替换」。

## 目标（用户已确认的方案）

1. 新增「受限决策树」问答向导：启动时按依赖顺序问「形态 → 后端 → 前端 → 壳 → 细分项」，结合回答生成规则文件；选项永远从当前候选 Profile 推导，保证不会选出无法生成的组合。
2. 先检测再问答：扫描目标项目特征文件（sln/csproj/package.json/wails.json/tauri/pubspec/pyproject 等）给出推荐组合，用户确认后生成；确认不了再进问答树。经典「大类 → Profile」入口保留。
3. 生成后写入配方记录 `.ai-rule-profile.json`（记录 mode/profile/answers/版本），并自动把该记录加入目标项目 `.gitignore`（目标项目没有 .gitignore 时按技术栈创建一份，C# 参考 `D:\Work\gitee\daily-examples\.gitignore`，Python 等参考 `D:\Work\ai\mcp_server\.gitignore`）。
4. 20 个预设 Profile 全部保留不动（内容零变化），`--check` / `--emit` 行为保持；新增决策树仿真校验。

## 硬约束

- 编码：所有新文件 UTF-8 无 BOM、LF 行尾、注释用中文。
- 不修改 `fragments/` 规则片段；只改 `app/RuleAssembler/` 代码、`fragments/manifest.json`（追加 traits/wizard/版本号）、README、根 `AGENTS.md`、`doc/rulelog/`。
- 回归标准：改动后 `--emit` 全部 Profile 输出与改动前对比，差异只允许出现在 `version:` / `last_updated:` 行（版本从 1.35.0 提到 1.36.0）。
- manifest `schema_version` 保持 2（本次是可选字段的追加式变更）。

## 已完成

1. 调研：manifest 结构（8 家族/20 Profile/subrules/rootDefaults）、ConsoleWizard 两级选择、RuleRenderer/OutputWriter/ManifestLoader、`fragments/shared/` 已按 backend/frontend/doc/infra 分片。
2. 建分支 `guided-wizard-detect`。
3. `app/RuleAssembler/Models.cs`：新增 `WizardConfig`/`WizardStep`/`WizardOption`、`ProfileTraits`（trait 键：form/backend/frontend/shell/arch-style/blazor-flavor/ai-capability，`*` 通配）、`RecipeRecord`；`Manifest.Wizard`、`Profile.Traits`。
4. `app/RuleAssembler/WizardEngine.cs`（新文件，纯逻辑可单测）：
   - `Filter`/`Matches`（`*` 通配匹配任意回答）、`NextQuestion`（先决条件满足且候选存在 ≥2 个取值才提问；选项按候选数降序）、`Resolve(answers)`、`SimulateAll()`（全路径仿真：每条路径必须收敛到 1 个 Profile 且全部 Profile 可达）、`IsApplicable`（internal 供检测器复用）。
5. `app/RuleAssembler/ProjectDetector.cs`（新文件）：
   - `Scan`：浅层扫描（深度 ≤2，跳过 node_modules/bin/obj/.git/dist 等），收集 csproj 内容、package.json 依赖（react/vue/next/electron）、wails.json、go.mod、src-tauri/tauri.conf.json、pubspec.yaml、fastapi 依赖、*.razor 等。
   - `CollectEvidence`：把信号映射为 trait 证据（含理由文案）。
   - `Detect`：沿决策树贪心走（有证据用证据，无证据用 step 的 `detect-default`，再否则取候选最多的选项），保证推荐一定可生成；返回 `DetectionResult(ProfileId, ProfileLabel, Answers, Reasons, Notes)`。

## 剩余任务（按顺序做）

### 1. `RecipeRecorder.cs` + gitignore 模板（新文件）
- `RecipeRecorder.Write(dir, RecipeRecord)`：UTF-8 无 BOM、LF 写 `.ai-rule-profile.json`（`generated_at` 用本地时间 ISO 8601 含时区）。
- `GitignoreManager.Ensure(dir, stackKey)`：已有 .gitignore → 检查无 `.ai-rule-profile.json` 行则追加「# AI rule 生成器配方记录（use-rule）」块；没有 → 按 stackKey 写内置模板（模板末尾自带配方记录块）。
- `GitignoreTemplates`（静态类，内置模板字符串）：`dotnet` / `python` / `node` / `go` / `flutter` / `minimal`，内容精选自上述两个参考 .gitignore，每份 ≤50 行，末尾统一带配方记录块。
- stackKey 映射：dotnet-backend、web-fullstack、desktop、mobile(maui) → dotnet；python-backend → python；hybrid-app → go+node 合并模板（可直接做一份 `hybrid`）；frontend-static → node；mobile(flutterMobile) → flutter；general → minimal。

### 2. `ConsoleWizard.cs` 改造 + `Program.cs` 集成
- `ConsoleWizard` 保留 `SelectProfile`（经典入口）。
- 新增问答循环：`engine.NextQuestion(candidates)` → 打印问题+选项（label — desc）→ 读编号；`0` 返回上一步（用栈保存候选集快照）；无问题即收敛，返回 `(profile, mode, answers)`。mode：`wizard`（问答）/ `detected`（推荐直接确认）/ `classic`（经典选择）。
- 交互主流程（无参数启动）：先 `ProjectDetector.Detect(CurrentDirectory)`；有推荐 → 打印证据 + 推荐组合 + 将生成文件列表，菜单「1 使用推荐并生成 2 问答微调 3 经典选择 0 取消」；无推荐 → 菜单「1 问答选择 2 经典选择 0 取消」。
- 写入成功后调用 RecipeRecorder + GitignoreManager，打印提示行。
- `RunCommand` 新增 `--detect [目录]`：非交互打印证据/推荐/回答；`--help` 同步更新。

### 3. `fragments/manifest.json` 写入数据（注意：UTF-8 无 BOM、保持 2 空格缩进、LF）
- `vars.version`: 1.35.0 → **1.36.0**（last_updated 已是 2026-09-02）。
- 20 个 Profile 各加 `"traits"`（映射表见下）。
- 顶层加 `"wizard"`：steps + options（trait → 值 → {label, desc}）。

steps（顺序固定）：
```json
[
  { "trait": "form", "question": "项目形态是什么？" },
  { "trait": "backend", "question": "后端（服务端）用什么技术栈？", "applies-when-trait": "form", "applies-when-values": ["backend-only", "web", "desktop"] },
  { "trait": "frontend", "question": "前端 / 界面用什么技术？", "applies-when-trait": "form", "applies-when-values": ["web", "desktop", "mobile"] },
  { "trait": "shell", "question": "桌面壳方案用哪个？" },
  { "trait": "arch-style", "question": "后端架构风格？", "detect-default": "single" },
  { "trait": "blazor-flavor", "question": "Blazor 用哪种形态？", "detect-default": "traditional" },
  { "trait": "ai-capability", "question": "是否需要 AI Agent 能力模块？", "detect-default": "no" }
]
```

Profile traits 映射表（必按此写入）：

| profile | form | backend | frontend | shell | arch-style | blazor-flavor | ai-capability |
|---|---|---|---|---|---|---|---|
| dotnet-layered | backend-only | dotnet | none | none | layered | none | no |
| dotnet-core-single | backend-only | dotnet | none | none | single | none | no |
| python | backend-only | python | none | none | single | none | no |
| reactLinkdotNet | web | dotnet | react | none | single | none | no |
| vueLinkdotNet | web | dotnet | vue | none | single | none | no |
| reactLinkdotNet-ai | web | dotnet | react | none | single | none | yes |
| nextFullstack | web | node | react | none | single | none | no |
| dotnetBlazor | web | dotnet | blazor | none | single | traditional | no |
| dotnetBlazorWebApp | web | dotnet | blazor | none | single | webapp | no |
| avaloniaExe | desktop | none | avalonia | none | single | none | no |
| wpfExe | desktop | none | wpf | none | single | none | no |
| dotnetConsole | desktop | none | console | none | single | none | no |
| electronDotNetExe | desktop | dotnet | electron | electron | single | none | no |
| reactTauriExe | desktop | go | react | tauri | single | none | no |
| reactWailsExe | desktop | go | react | wails | single | none | no |
| vueWailsExe | desktop | go | vue | wails | single | none | no |
| mauiMobile | mobile | dotnet | maui | none | single | none | no |
| flutterMobile | mobile | none | flutter | none | single | none | no |
| staticHtml | static | none | html | none | single | none | no |
| generalProject | general | * | * | * | * | * | * |

options 文案（中文 label，desc 一句话）：
- form：backend-only=纯后端服务（只提供 API，没有界面）；web=Web 应用（浏览器访问，前后端分离或全栈）；desktop=桌面应用（Windows/跨平台桌面客户端）；mobile=移动应用（iOS/Android/跨平台移动端）；static=纯前端/静态站点（无后端的静态页面）；general=通用项目（暂不确定技术栈）
- backend：dotnet=.NET / ASP.NET Core；python=Python FastAPI；go=Go；node=Node.js；none=无后端
- frontend：react=React；vue=Vue；blazor=Blazor；avalonia=Avalonia；wpf=WPF；console=控制台界面；electron=Electron（HTML 界面）；maui=.NET MAUI；flutter=Flutter；html=静态 HTML
- shell：tauri=Tauri（Rust 壳）；wails=Wails（Go 壳）；electron=Electron（Node 壳）
- arch-style：layered=分层架构（多项目拆分，适合中大型系统）；single=单层简洁（单一 API 项目，适合中小型）
- blazor-flavor：traditional=Blazor Server（传统）；webapp=Blazor Web App（新模板）
- ai-capability：yes=需要（附加 AI 产品能力模块）；no=不需要

### 4. `ManifestLoader.Validate` 扩展
- wizard.steps：trait 非空、不重复、必须是 ProfileTraits 支持的键、question 非空；applies-when-trait 合法且 values 非空。
- 每个 Profile 的 traits：非空；每个 step trait 的值（除 `*`）必须在 `wizard.options[trait]` 中有文案；options 里不许出现无用键（宽松处理：出现未知 trait 值报错即可）。

### 5. `--check` 扩展（Program.RunCommand）
- 现有全 Profile 渲染校验保留；追加 `WizardEngine.SimulateAll()` 错误输出（有错则 --check 失败退出码 1）。

### 6. 单元测试（test/RuleAssembler.Tests/）
- `WizardEngineTests`：SimulateAll 无错误；样例回答解析（如 form=desktop+backend=go+frontend=react → 未定 shell 前两候选；shell=wails → reactWailsExe）；`*` 通配匹配。
- `ProjectDetectorTests`：临时目录造 wails.json+go.mod+package.json(react) → 推荐 reactWailsExe；造 csproj(UseWPF) → wpfExe；空目录 → null。
- `GitignoreManagerTests`：无 .gitignore 创建；已有则只追加配方记录块；重复运行幂等。

### 7. 构建与回归验证
- `dotnet build app/RuleAssembler`（无 warning 保持）+ `dotnet test test/RuleAssembler.Tests`。
- 改 manifest 前先在临时目录跑一次基线：`dotnet run --project app/RuleAssembler -- --emit <tmp>/before`；改完跑 `<tmp>/after`；diff 两目录，差异只允许 version/last_updated 行。
- 交互冒烟：echo 管道模拟输入走一遍问答路径（如 1(web)→1(dotnet)→1(react)→…）确认能收敛到 reactLinkdotNet。

### 8. 文档与记录
- 根 `README.md` 生成器使用段（约 93-114 行）补问答/检测/配方记录说明。
- `app/RuleAssembler/README.md` 同步新架构（WizardEngine/ProjectDetector/RecipeRecorder）。
- 根 `AGENTS.md` 项目说明段补一句新入口。
- `doc/rulelog/2026-09-02-生成器问答向导与项目检测.md`：按仓库惯例（标题带日期+版本，跟踪状态表，修改原因/主要修改内容/验证）。

## Spectre.Console 交互层：已完成（并入同一 rulelog 记录）

> 实施结果：S1-S5 全部完成。引包 Spectre.Console 0.52.0（未引入 Spectre.Console.Cli）；
> 新增 `IConsoleUi` + `PlainConsoleUi`（编号输入，stdin 重定向自动使用）+ `SpectreConsoleUi`
> （SelectionPrompt 方向键选择，「0」项承载取消/返回上一步，回退栈逻辑不变）；
> 实现选择集中在 `Program.RunInteractive` 入口一处，`WizardEngine`/`ProjectDetector`/`RecipeRecorder` 纯逻辑未动；
> `--check/--emit/--detect` 保持纯文本。验证：构建 0 警告、测试 29/29、四条管道路径冒烟通过、
> `--emit` 零 diff（manifest 未再改动）、`publish.ps1` 单文件发布 + dist EXE 冒烟通过。
> 遗留：真实终端的方向键交互需人工确认一次。

> 用户已确认采用 Spectre.Console（官方调研结论：CommandLineParser / PowerArgs / McMaster.CommandLineUtils 均已停止积极维护，不采用）。
> 与剩余任务 #2 的关系：若 #2 尚未开始，问答/菜单/确认交互直接基于 Spectre.Console 实现，不要先写手写编号输入版再重写；若 #2 已按手写交互完成，本任务作为交互层整体替换。`WizardEngine` / `ProjectDetector` / `RecipeRecorder` 等纯逻辑一律不动，只换控制台 UI 层。

### S1. 引包
- `app/RuleAssembler/RuleAssembler.csproj` 添加 `Spectre.Console`（>= 0.52.0，取当前最新稳定版）。
- 暂不引入 `Spectre.Console.Cli`：现有参数面（`--check` / `--emit` / `--detect` / `--help`）手写解析够用；等出现子命令需求再评估（注意它已拆独立仓库、独立版本线）。
- 测试工程 `test/RuleAssembler.Tests` 按需加 `Spectre.Console.Testing`（用 `TestConsole` 做交互渲染快照测试）。

### S2. 交互替换（无参数启动主流程）
- 检测推荐确认菜单、问答循环选项、经典两级选择统一改用 `SelectionPrompt`（方向键 + 回车），确认类用 `ConfirmationPrompt`；取消语义保持「已取消、退出码 0」。
- 问答「返回上一步」保留：Spectre prompt 无内建回退，在选项列表中保留「0 返回上一步」项，回退栈（候选集快照）逻辑不变。
- 检测证据/推荐组合用 Markup + Table（或 Tree）渲染；「将生成的文件列表」用 Table；完成汇总保持现有纯文本行。
- 交互选项数据结构不变：问答结果仍收敛为 trait → 值字典交给 `WizardEngine.Resolve`。

### S3. 非交互与管道兼容（硬要求）
- `--check` / `--emit` / `--detect` 保持纯文本输出，不混入 ANSI 控制序列（管道/CI 下输出可 diff）。
- 冒烟测试的 echo 管道模拟输入必须仍可用：验证 prompt 在重定向 stdin 下的行为，必要时通过 `AnsiConsoleSettings`（InteractionBehavior 强制 Always）支持管道输入；该处理集中封装在交互入口一处，不散落到各步骤。

### S4. 回归与验证
- 产物零变化：本任务不碰 `RuleRenderer` / `fragments/`，`--emit` 前后 diff 确认无差异（manifest 版本号无需 bump）。
- `dotnet build app/RuleAssembler` 无 warning；`dotnet test test/RuleAssembler.Tests` 全绿。
- 交互冒烟：管道模拟输入 + 手工各走一遍「推荐确认 / 问答微调 / 经典选择 / 取消」四条路径。
- `publish.ps1` 单文件发布后运行冒烟（Spectre.Console 为纯托管程序集，预期无影响，确认即可）。

### S5. 文档与记录
- 根 `README.md` 生成器使用段补交互说明（方向键选择、取消方式）；`app/RuleAssembler/README.md` 架构段同步。
- `doc/rulelog/` 并入 `2026-09-02-生成器问答向导与项目检测.md` 同一条任务记录（同一分支同一批次，不另开平行条目），状态表增加「Spectre.Console 交互层」子任务行。

## 抽测任务：reactLinkdotNet-ai 生成与审查（已完成）

> 结论：向导分支正确（ai-capability=yes）、10 个文件齐全、结构与内容审查通过。
> 审查发现并已修复两个生成器缺陷：① 配方记录 .ai-rule-profile.json 在 Windows 下写成 CRLF → 归一为 LF 并加单测；
> ② web-fullstack 的 .gitignore 默认模板误用纯 Node 模板、漏 .NET 后端 bin/obj → 新增 webstack（Node+.NET）模板并改映射，单测补断言。
> 两处甄别为非问题：CLAUDE/GEMINI/GROK 独立版本元数据（README 明文设计）；frontend-AGENTS.md 中
> `style={{ color: '#1677ff' }}` 为 React 代码样例而非模板槽残留。
> 修复后已重建、29/29 测试过、重新发布 dist/use-rule.exe 并重新生成目标验证通过。

> 用户指示：更新任务状态后，挑一个技术栈走新交互流程生成 ai rule 并审查。
> 选择 reactLinkdotNet-ai（React + .NET + AI Agent 全栈）：此前已审过 mauiMobile、reactWailsExe、python、
> dotnet-layered、dotnetBlazor 五个，本条是唯一走 ai-capability=yes 问答分支的 Profile，
> 可验证新向导的 AI 模块提问分支 + modules 文件输出，之前未被审查覆盖。
> 审查口径：结构（文件清单/变量槽/编码行尾/版本元数据）、导航一致性（target-list、subrule-index、
> reading-order 与实际产物互指）、ai 模块内容、配方记录与 gitignore。

## 关键事实（接手必读）

- `ResourceStore` 把 `fragments/**` 以 `Fragments.` 前缀嵌入程序集；manifest 校验失败会直接抛异常。
- `OutputWriter.WriteFiles` 自带 staging+backup+回滚；配方记录要在 WriteFiles 成功之后写。
- `ManifestLoader.MergeFamilyDefaults` 在 JSON 层做 family rootDefaults 继承，解析 traits/wizard 不受影响。
- 预设产物回归对比基线命令：`dotnet run --project app/RuleAssembler -- --emit D:/tmp/rules-before`（emit 只用于预览，不写配方记录）。
- 当前 20 Profile 列表与家族归属见 `fragments/manifest.json`；渲染入口 `RuleRenderer.Plan(manifest, profile)` 未改动。
