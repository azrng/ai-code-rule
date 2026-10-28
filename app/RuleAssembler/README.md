# RuleAssembler — 交互式 AI Rule 拼接器

把一套完整 AI rule 释放到当前目录：双击启动（先打印当前规则版本）→ 自动检测当前项目给出推荐组合（或走问答决策树 / 经典大类 → 兼容样例）→ Q5 能力开关 → 确认后直接释放。默认不使用命令行参数完成业务选择。

## 入口流程

1. **检测优先**：`ProjectDetector` 浅层扫描目标目录（深度 ≤2，跳过 node_modules/bin/obj 等），把 sln/csproj、package.json 依赖、wails.json、go.mod、src-tauri、pubspec、fastapi 依赖等信号映射为维度证据。证据按维度归并：同维度唯一才确认，冲突维度留待人工；候选 = 与已确认维度相容的兼容样例（`compatibilityCases` 是组件口径事实来源，样例未携带的维度视为通配）。候选唯一时给出推荐组合与判断依据；证据不足时输出部分结果（已确认维度、待确认维度、候选组合），不伪造推荐。
2. **问答决策树与补答（P2 阶段 3 起走组合内核）**：问答由 `SelectionEngine` 驱动——候选是 manifest 的 `components` 而非 Profile 列表，问题顺序沿用 `wizard.steps`（项目形态 → 前端/界面 → 桌面壳 → 后端 → 细分项）。只提问「存续组件之间仍有分歧」的维度：唯一取值自动推导（配方来源 `derived`），选项经可行性剪枝（先过钳制约束，再做结构锚定校验——已回答的非 absent 维度必须仍被存续组件表达，兼容样例不参与剪枝，新组件接入后其组合即可经问答到达）。输入 0 可返回上一步。检测推荐与补答同样产出 `Selection` 走 `CompositionPlanner` 组合路径。
3. **经典选择**：技术大类 → 兼容样例的浏览式兜底入口（样例携带 label / desc / family 展示元数据）；选中样例的维度选择直接进入组合内核渲染与配方。
4. **Q5 能力开关**：组合确定后统一出现，只列出当前组合可用的能力（doc / infra / design-system / ai-capability，默认勾选 = 当前组合；不可用能力隐藏）。`ai-capability` 的启用由 AI 维度的问答选择这一唯一事实来源决定，Q5 只展示不调整。关闭能力会同步移除对应输出文件与根导航引用（含 reading-order 编号重排与根模板行级条件）。
5. **交互层**：`IConsoleUi` 两个实现——`SpectreConsoleUi`（交互终端，方向键 + 回车）与 `PlainConsoleUi`（编号输入，stdin 重定向的管道/CI 场景自动使用）。实现选择集中在 `Program.RunInteractive` 入口一处；启动菜单提供规则生成、Skills 导入和只读组合检测；生成模式下已存在配方记录时优先提供「按配方重放」（与 `--upgrade` 共享 `RunUpgrade` 同一实现）；`--check` / `--emit` / `--detect` 始终纯文本输出。
6. **收尾**：所有路径共用「列出实际生成文件 → 汇总确认 → 生成」；写入成功后 `RecipeRecorder` 在目标项目生成 `.ai-rule-profile.json` 配方记录（mode / `profile_id` / `selection` 维度选择 / `component_ids` 命中组件 / `answer_sources` 回答来源 / `features` 能力开关 / skills / 版本 / 文件清单），并确保目标项目 `.gitignore` 忽略该记录、生成器本体（`use-ai-rule*` 通配，覆盖 exe、无扩展名与带 RID 后缀的副本）、规则效果反馈记录（`.ai-rule-feedback.md`，本地产物不随项目提交）与 AI 工具本地目录（`.zcode/` 等七项，见 `GitignoreTemplates.AiToolEntries`）；没有 `.gitignore` 时按所选技术栈创建默认模板（精选自 github/gitignore 标准内容）。

## 设计

`fragments/` 是唯一维护源，生成器按维度组合把公共模块、组件根变量与组件规则片段拼装成目标文件：

```text
fragments/
├── root/AGENTS.md         # 公共根骨架，含变量槽
├── pointers/              # 公共入口指针
├── shared/                # 跨架构共享小节，供 include 指令引用
├── arch/<id>/             # 当前架构的专属规则片段
├── manifest.json          # 模块、家族、组件、约束、兼容样例、wizard（维度空间）和输出配方
└── ...                    # design-system 等能力片段
```

公共能力通过 `modules` 复用。例如 `core` 提供根入口、`collaboration-AGENTS.md` 和指针；需求澄清、任务、交付、回退等协作流程只在命中场景读取。`ai-capability` 提供 .NET 后端口径的 `ai-AGENTS.md`，`ai-capability-frontend` 提供浏览器侧口径的 `ai-AGENTS.md`（托管网关 / 代理、密钥不下发前端、流式处理）。现有技术栈专属文件按复用价值逐步组件化：`reactLinkdotNet-ai` 已拆为 Web 壳、React 前端、单项目 .NET 后端与 AI capability/module，族组合走「骨架 + 槽位 + 前端/后端组件」，未拆分的组合以单一组件整体承载；新增组合优先使用模块与组件，不再整目录复制。

`wizard.steps` 是维度空间的唯一事实来源（组件 `when`、约束与兼容样例引用的维度都必须在此声明，并配 `options` 取值文案），`compatibilityCases` 是维度组合的唯一事实来源：项目检测、组件问答、经典入口与配方重放全部按兼容样例收敛，样例携带 label / desc / family 展示元数据（装载校验拦截 ID 重复与元数据缺失），否则 `--check` 会失败。`profiles` 节点与旧 Profile 渲染路径已于 2026-09-16 整体退役（schema_version 3），样例是组合空间的唯一注册表。

子规则支持 `sources` 数组（B 档骨架继承）：多个源按序拼接为一个输出文件，与 `source` 互斥，front matter 只能出现在首个源。跨组合共享的整文件骨架放 `shared/skeletons/<族>/`，架构专属差异保留在各自 `arch/<id>/`；新增同族组合时优先引用现有骨架，只写真正的差异文件。已收敛骨架：wails 族（reactWailsExe / vueWailsExe 的 backend/doc/infra 引用 `shared/skeletons/wails/`）、electron 族（backend/doc/infra 按 electron-dotnet / electron 两口径，frontend 与 design-system 经内容收敛为 `frontend-react-AGENTS.md` / `frontend-vue-AGENTS.md` / `design-system-react.yaml` / `design-system-vue.yaml` 四份骨架，backend 味道差异经 `{{slot:}}` 槽位注入）、web-dotnet 与 desktop-xaml（见 manifest）。

子规则正文同样支持槽位：`Subrule.slots`（文件级）与 `CompositionPlan.Slots`（组件并集）提供取值，渲染期替换、残留占位即报错。

子规则内部的共性小节通过 include 指令复用：片段中独占一行的 `<!-- include: shared/xxx.md -->`（路径相对 `fragments/`）会在生成时被展开为对应共享小节的内容，支持嵌套引用并做循环检测。当前 `shared/` 维护「提交前最小回归」「无法执行测试时」「外部依赖与数据」「主动建议规则」等小节，按 backend / frontend / ui / application / infra 等子规则类型分目录归档；只有逐字节相同或纯措辞漂移的小节才收敛进共享，含技术栈实质差异的版本保留在各架构片段本地。

目标项目的需求、设计和契约草案不由生成器创建。生成的 `doc-AGENTS.md` 规定项目文档按业务模块/功能拆分：需求放在 `doc/requirements/`，设计放在 `doc/design/`，新契约草案放在 `doc/contracts/`，通过 `doc/index.md` 和目录索引按需定位；旧的 `doc/requirement.md` 与 `doc/design/设计文档.md` 仅作为迁移兼容入口。

### 组合内核（P2）

manifest 的 `components` / `constraints` / `compatibilityCases` 节点承载组件化组合模型，`CompositionPlanner` 把维度选择（`Selection`，`Dictionary<string, string>`）解析为确定性的 `CompositionPlan`：

- **组件**：单一技术职责（form / shell / frontend / backend / capability），声明 `when` 维度条件、根变量贡献（可携带 `{{slot:x}}` 槽位占位）、`files` 输出贡献、可选 `moduleIds` 与 `designSystemSource`。解析顺序按 kind 固定排序（form → shell → frontend → backend → capability），同 kind 按 manifest 声明顺序；组合始终先加载 `core`，再按选中组件顺序合并模块并去重。
- **槽位机制**：骨架组件（如 `electron-desktop-shell`）持有完整的根变量条目顺序，backend 等差异内容经 `slots` 注入槽位；槽位必须有唯一提供者，替换后残留占位即报错，已声明但未被消费的槽位降级为告警。
- **根变量合并**：以组件声明的 `family` rootDefaults 为继承基线，组件值按「未写则继承、写了则覆盖」处理；组件之间覆盖同一字段才是冲突。组件未显式声明的标量字段携带类默认值，合并前会被剥离。
- **证据不足不隐式补全**：组件 `when` 引用了选择中缺失的维度即视为部分匹配并报错，列出待确认维度，与检测器口径一致。`constraints` 声明维度间合法关系（when 命中时已回答维度的 require 必须成立、forbid 必须不成立；未回答维度的钳制由组件解析兜底——组合解析成功必须存在 when 全命中组件集），当前 30 条覆盖壳 ↔ 后端（tauri→rust、wails→go、electron 仅 dotnet / 主进程）、壳 / Blazor 必须有实体取值（桌面壳 forbid frontend=none、blazor forbid flavor=none）、形态钳制（纯后端无前端、web / 移动无壳、静态站限定 html 无后端）、前端归位（blazor→web+dotnet、原生桌面 / 移动界面的形态与后端、layered 仅 dotnet）以及 AI 承接（web + .NET 的 AI 组合 forbid frontend=none——AI 能力模块须由实际界面承接）；`SelectionEngine` 剪枝先经 `CompositionPlanner.SatisfiesConstraints`（forbid 立即生效、require 只对已回答维度生效），再做结构锚定校验——已回答的非 absent（不适用标记，声明在 wizard step）维度必须仍被存续组件锚定，兼容样例不参与剪枝，新组件接入后其组合即可经问答到达。
- **样例注册表**：`compatibilityCases` 把全部组合固化为样例（携带 label / desc / family 展示元数据），`--check` 对每个样例做组合口径的输出契约校验（声明 vs 产物）与产物级重复标题扫描，`CompositionDeterminismTests` 保证同一选择两次解析与渲染逐字节一致。全部 36 个组合已组件化：族组合走「骨架 + 槽位 + 前端/后端组件」（Electron 4、Wails 2、Tauri 2、Web 分离 .NET 4 / Python 2、Blazor 全栈 2 + 其 AI 2（Blazor 界面 / 后端组件不 pin AI 维度，「不需要」由无产物锚点组件 `web-blazor-ai-off` 提供取值与锚定，AI 取值经 capability 组件接入公共 `ai-capability` 模块）、Node 全栈 2、纯前端普通 3 + AI 2——普通与 AI 各一个公共壳，react / vue / general 框架差异经槽位注入），`reactLinkdotNet-ai` 走「.NET AI Web 壳 + React AI 前端 + 单项目 .NET 后端 + AI capability/module」，`.NET 纯后端 2（分层 / 单层）` 与 `python` 纯后端走「组件 + family 基线」（kind=backend，when 收敛为形态 + 后端 + 分叉维度），其余为「单一组合 = 全量 form 组件」（直接携带完整 root / files / design-system）。新增技术类型 = 扩展组件 + 约束 + 样例（含展示元数据）。
- **组件根标量按 JSON 存在性判定**：组件根中未在 JSON 里真实声明的标量字段反序列化后携带类默认值，合并前按 `DeclaredScalarFields`（`ManifestLoader.AnnotateComponentRootPresence` 标注）剥离；刻意声明且恰好等于类默认值的覆盖（如回退通用措辞覆盖 family 基线）会被保留。
- **输出合并**：同一路径必须有且仅有一个 `replace` 基底，可有多个 `append` 贡献；贡献按模块、组件和声明顺序聚合，能力过滤与槽位替换对每项独立执行。重复 `replace`、无基底的 `append` 以及 append 片段携带 front matter 都会显式报错，`CompositionPlan.Files` 保留扁平兼容视图，内部同时保留贡献归属。

### 输出契约与能力开关

输出文件的结构化归属维护在 manifest，`--check` 不通过解析根文件 Markdown 反推输出契约：

- `features` 注册能力 ID（doc / infra / ai-capability / design-system）；片段 `output` 可声明 `feature`（能力归属）与 `merge`（replace / append），同一输出允许一个 replace 基底和多个 append 贡献，全局 `outputs.referenced-existing` 维护「根规则提到但生成器不创建」的文件（如 TASK.md）。
- `RootConsistencyValidator` 按 `RuleRenderer.Plan` 同序遍历推导声明，与实际产物三向比对（声明未生成 / 实际未声明 / referenced-existing 被生成），`--check` 集成该校验。
- 根导航条目（subrule-index / dev-stage / reading-order / extra-sections）支持三种形态：纯文本、`{ "line": "...", "features": ["doc"] }`（能力条件）、`{ "when": [多档文案] }`（按启用能力取档）；reading-order 中连续的「1. 」编号行在能力行移除后自动重排。根模板支持行尾 `<!-- if-feature: doc -->` 行级条件。
- `FeatureSelection` 从输出声明推导每个组合的可用能力与默认勾选集；关闭能力后产物与导航联动收敛，根入口不会引用不存在的子规则文件。

### 新增模块清单

新增一个前端、后端、桌面壳或能力模块时，必须同时完成：规则源放入明确的 shared / skeleton / component / capability 位置；manifest 增加选项（wizard steps / options）、输出声明（含 feature 归属）与可用约束，并注册兼容样例；项目检测增加证据映射（无法检测时保留未确认状态，不补默认组合）；增加至少一个检测/问答测试与一个生成结果回归样例；更新规则版本、README 和现有主题 rulelog；检查根导航、文件清单、输出声明与合并方式的一致性。禁止为一个新组合整目录复制现有组件，禁止在 `Program` / `ProjectDetector` / `RuleRenderer` 中新增技术栈 ID 专属分支作为长期方案。

## 构建

```powershell
./publish.ps1
```

产物位于仓库根目录 `dist/`：`use-ai-rule.exe`（win-x64）与 `use-ai-rule-linux-x64`、`use-ai-rule-osx-x64`（底层发布目录仍保留在 `bin/Release/net10.0/<rid>/publish/`）。当前发布为三平台 self-contained 单文件矩阵，目标机无需安装 .NET 运行时；Windows 产物统一命名 `use-ai-rule.exe`，不再产出带 RID 后缀的副本。

## 使用

```text
1. 把 use-ai-rule.exe 拷贝到目标项目根目录
2. 双击启动：已存在 .ai-rule-profile.json 时优先生成「按配方重放」入口（沿用上次选择升级，可识别本地修改）；选「重新选择组合」则忽略配方
3. 无配方时自动检测并给出推荐组合与判断依据（证据不足时展示待确认维度与候选，补答后继续）
4. 选「使用推荐组合」，或改走问答决策树 / 经典大类 → 组合
5. Q5 能力开关：按需勾选文档 / 基础设施 / 设计系统等能力（默认 = 当前组合）
6. 确认文件清单后自动释放，并写入 .ai-rule-profile.json 配方记录
```

目标目录已有同名文件时会直接覆盖；唯一例外是 `design-system.yaml`：它释放后归产品所有，仅当本地不存在或 `meta.status` 仍为「基线待定制」时才用新基线刷新，已定制（或状态无法识别，含旧版生成的文件）时保留本地版本并提示。文件会先写入临时目录，全部准备成功后再提交；提交中途失败会回滚本次已经写入的文件。不会释放 `TASK.md`（配方中按 referenced-existing 声明，属于目标项目自己的运行时文件）、`rulelog` 或 `devlog`。

维护和 CI 可以使用只读校验与批量预览：

```powershell
# 只校验 manifest、片段、全部兼容样例的输出契约一致性（声明 vs 产物，含重复标题扫描）、
# 组件问答决策树（每条交互路径结构完备可解析，映射兼容样例时唯一，全部样例可达）
# 与内置技能（SKILL.md 齐全且 ID 无冲突），不写入文件
dotnet run --project app/RuleAssembler/RuleAssembler.csproj -- --check

# 只做项目检测，打印推荐组合与依据；证据不足时打印已确认 / 待确认维度与候选，不写入文件
dotnet run --project app/RuleAssembler/RuleAssembler.csproj -- --detect [目录]

# 按显式维度选择解析组合内核：唯一取值自动推导，仍有分歧的维度报错并列出可选值；
# 打印命中组件与文件清单，不写入文件
dotnet run --project app/RuleAssembler/RuleAssembler.csproj -- --plan form=web frontend=react backend=dotnet

# 按配方记录重放生成（升级规则或在新项目还原同一组合）：
# 旧配方仅有 profile_id 时经兼容样例反查；组件集合与当前规则不一致时显式报错；
# 能力开关按配方重放；design-system 已定制保留语义与交互一致；回写配方并补 .gitignore
dotnet run --project app/RuleAssembler/RuleAssembler.csproj -- --recipe <配方.json> [目标目录]

# 升级已有规则：读取目标目录的 .ai-rule-profile.json 重放生成。
# 配方记录了生成时的内容哈希，本地修改过的文件默认保留（只更新未修改文件），--force 全部覆盖；
# 回写配方刷新版本与哈希，可重复执行（幂等）
dotnet run --project app/RuleAssembler/RuleAssembler.csproj -- --upgrade [目录] [--force]

# 把全部兼容样例的组合路径生成结果输出到指定目录（目录名沿用样例 ID），用于批量预览与产物回归
dotnet run --project app/RuleAssembler/RuleAssembler.csproj -- --emit <目录>
```

## 当前组合

manifest 当前包含 8 个技术大类、34 个兼容样例（数量以 `fragments/manifest.json` 为准）。新增技术类型 = 在 `fragments/manifest.json` 加组件 + 约束 + 兼容样例（含 label / desc / family），并在 `fragments/arch/<id>/` 或 `shared/` 增加对应片段；新增可复用能力时优先增加 `modules`，再由组件通过 `moduleIds` 引用，并按「新增模块清单」补全输出声明、检测映射与测试。

## 版本与格式

- 规则版本和更新时间由 `manifest.json` 的 `vars` 统一控制。
- 所有生成文件统一使用 UTF-8 无 BOM 和 manifest 指定的 LF 行尾。
- 指针文件保留自身独立的 `rule_id` 和版本元数据。
- 仓库根目录的架构快照目录已于 2026-08-28 删除，`fragments/` 是唯一维护入口。
