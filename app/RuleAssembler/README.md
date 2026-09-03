# RuleAssembler — 交互式 AI Rule 拼接器

把一套完整 AI rule 释放到当前目录：双击启动 → 自动检测当前项目给出推荐组合（或走问答决策树 / 经典大类 → Profile）→ Q5 能力开关 → 确认后直接释放。默认不使用命令行参数完成业务选择。

## 入口流程

1. **检测优先**：`ProjectDetector` 浅层扫描目标目录（深度 ≤2，跳过 node_modules/bin/obj 等），把 sln/csproj、package.json 依赖、wails.json、go.mod、src-tauri、pubspec、fastapi 依赖等信号映射为 trait 证据。证据按维度归并：同维度唯一才确认，冲突维度留待人工；候选 = 匹配全部已确认维度的 Profile。候选唯一时给出推荐组合与判断依据；证据不足时输出部分结果（已确认维度、待确认维度、候选组合），不伪造推荐。
2. **问答决策树与补答**：`WizardEngine` 按 manifest 的 `wizard.steps` 顺序提问（项目形态 → 后端 → 前端/界面 → 桌面壳 → 架构风格 → Blazor 形态 → AI 能力模块）；每一步只列当前候选 Profile 真实支持的选项，候选在某 trait 上无差异则自动跳过该问题，输入 0 可返回上一步。检测部分结果进入补答流程：已确认维度保留且不再提问，只回答候选间仍有分歧的维度，回答来源（detected / manual）记入配方。因为选项从候选推导，交互路径上不存在"选出无法生成组合"的死胡同；未接入的技术栈组合表现为"选项不出现"。
3. **经典选择**：技术大类 → Profile，作为兜底入口保留。
4. **Q5 能力开关**：组合确定后统一出现，只列出当前组合可用的能力（doc / infra / design-system / ai-capability，默认勾选 = 当前组合；不可用能力隐藏）。`ai-capability` 的启用由 AI 问答（Profile 选择）这一唯一事实来源决定，Q5 只展示不调整。关闭能力会同步移除对应输出文件与根导航引用（含 reading-order 编号重排与根模板行级条件）。
5. **交互层**：`IConsoleUi` 两个实现——`SpectreConsoleUi`（交互终端，方向键 + 回车）与 `PlainConsoleUi`（编号输入，stdin 重定向的管道/CI 场景自动使用）。实现选择集中在 `Program.RunInteractive` 入口一处；`--check` / `--emit` / `--detect` 始终纯文本输出。
6. **收尾**：所有路径共用「列出实际生成文件 → 汇总确认 → 生成」；写入成功后 `RecipeRecorder` 在目标项目生成 `.ai-rule-profile.json` 配方记录（mode / profile / 回答组合 / `answer_sources` 回答来源 / `features` 能力开关 / 版本 / 文件清单），并确保目标项目 `.gitignore` 忽略该记录；没有 `.gitignore` 时按所选技术栈创建默认模板（精选自 github/gitignore 标准内容）。

## 设计

`fragments/` 是唯一维护源，生成器把公共模块、Profile 根变量和 Profile 专属规则组合成目标文件：

```text
fragments/
├── root/AGENTS.md         # 公共根骨架，含变量槽
├── pointers/              # 公共入口指针
├── shared/                # 跨架构共享小节，供 include 指令引用
├── arch/<id>/             # 当前 Profile 的专属规则片段
├── manifest.json          # 模块、家族、Profile、traits、wizard 和输出配方
└── ...                    # design-system 等能力片段
```

公共能力通过 `modules` 复用。例如 `core` 提供根入口和指针，`ai-capability` 提供 .NET 后端口径的 `ai-AGENTS.md`，`ai-capability-frontend` 提供浏览器侧口径的 `ai-AGENTS.md`（托管网关 / 代理、密钥不下发前端、流式处理）。现有技术栈专属文件暂按 Profile 保留，避免为一次模块化迁移改写大量规则正文；新增组合优先使用模块，不再复制整套 Profile。

每个 Profile 声明 `traits` 组合画像（form/backend/frontend/shell/arch-style/blazor-flavor/ai-capability，`*` 表示不适用），`wizard` 节点维护问题步骤与选项文案；问答向导与项目检测都基于 traits 过滤，因此新增 Profile 时必须同时补 traits 与 options 文案，否则 `--check` 会失败。

子规则支持 `sources` 数组（B 档骨架继承）：多个源按序拼接为一个输出文件，与 `source` 互斥，front matter 只能出现在首个源。跨 Profile 共享的整文件骨架放 `shared/skeletons/<族>/`，Profile 专属差异保留在各自 `arch/<id>/`；新增同族 Profile 时优先引用现有骨架，只写真正的差异文件。已收敛骨架：wails 族（reactWailsExe / vueWailsExe 的 backend/doc/infra 引用 `shared/skeletons/wails/`）、electron-dotnet 族（reactElectronDotNet / vueElectronDotNet 引用 `shared/skeletons/electron-dotnet/`）、web-dotnet 与 desktop-xaml（见 manifest）。

子规则内部的共性小节通过 include 指令复用：片段中独占一行的 `<!-- include: shared/xxx.md -->`（路径相对 `fragments/`）会在生成时被展开为对应共享小节的内容，支持嵌套引用并做循环检测。当前 `shared/` 维护「提交前最小回归」「无法执行测试时」「外部依赖与数据」「主动建议规则」等小节，按 backend / frontend / ui / application / infra 等子规则类型分目录归档；只有逐字节相同或纯措辞漂移的小节才收敛进共享，含技术栈实质差异的版本保留在各架构片段本地。

目标项目的需求与设计文档不由生成器创建。生成的 `doc-AGENTS.md` 规定项目文档按业务模块/功能拆分：需求放在 `doc/requirements/`，设计放在 `doc/design/`，通过 `doc/index.md` 和目录索引按需定位；旧的 `doc/requirement.md` 与 `doc/design/设计文档.md` 仅作为迁移兼容入口。

### 输出契约与能力开关

输出文件的结构化归属维护在 manifest，`--check` 不通过解析根文件 Markdown 反推输出契约：

- `features` 注册能力 ID（doc / infra / ai-capability / design-system）；片段 `output` 可声明 `feature`（能力归属）与 `merge`（replace / append），Profile 可用显式 `outputs` 补充声明，全局 `outputs.referenced-existing` 维护「根规则提到但生成器不创建」的文件（如 TASK.md）。
- `RootConsistencyValidator` 按 `RuleRenderer.Plan` 同序遍历推导声明，与实际产物三向比对（声明未生成 / 实际未声明 / referenced-existing 被生成），`--check` 集成该校验。
- 根导航条目（target-list / subrule-index / dev-stage / reading-order / extra-sections）支持三种形态：纯文本、`{ "line": "...", "features": ["doc"] }`（能力条件）、`{ "when": [多档文案] }`（按启用能力取档）；reading-order 中连续的「1. 」编号行在能力行移除后自动重排。根模板支持行尾 `<!-- if-feature: doc -->` 行级条件。
- `FeatureSelection` 从输出声明推导每个组合的可用能力与默认勾选集；关闭能力后产物与导航联动收敛，根入口不会引用不存在的子规则文件。

### 新增模块清单

新增一个前端、后端、桌面壳或能力模块时，必须同时完成：规则源放入明确的 shared / skeleton / component / capability 位置；manifest 增加选项、traits、输出声明（含 feature 归属）与可用约束；项目检测增加证据映射（无法检测时保留未确认状态，不补默认组合）；增加至少一个检测/问答测试与一个生成结果回归样例；更新规则版本、README 和现有主题 rulelog；检查根导航、文件清单、输出声明与合并方式的一致性。禁止为一个新组合复制现有 Profile 全目录，禁止在 `Program` / `ProjectDetector` / `RuleRenderer` 中新增技术栈 ID 专属分支作为长期方案。

## 构建

```powershell
./publish.ps1
```

产物位于仓库根目录 `dist/use-rule.exe`（底层发布目录仍保留在 `bin/Release/net10.0/win-x64/publish/`）。当前发布为 Windows x64、框架相关单文件，目标机需要安装对应的 .NET 10 运行时；这是体积和部署依赖之间的明确取舍。

## 使用

```text
1. 把 use-rule.exe 拷贝到目标项目根目录
2. 双击启动（自动检测并给出推荐组合与判断依据；证据不足时展示待确认维度与候选，补答后继续）
3. 选「使用推荐组合」，或改走问答决策树 / 经典大类 → Profile
4. Q5 能力开关：按需勾选文档 / 基础设施 / 设计系统等能力（默认 = 当前组合）
5. 确认文件清单后自动释放，并写入 .ai-rule-profile.json 配方记录
```

目标目录已有同名文件时会直接覆盖。文件会先写入临时目录，全部准备成功后再提交；提交中途失败会回滚本次已经写入的文件。不会释放 `TASK.md`（配方中按 referenced-existing 声明，属于目标项目自己的运行时文件）、`rulelog` 或 `devlog`。

维护和 CI 可以使用只读校验与批量预览：

```powershell
# 只校验 manifest、片段、全部 Profile、输出契约一致性与问答决策树（全路径仿真），不写入文件
dotnet run --project app/RuleAssembler/RuleAssembler.csproj -- --check

# 只做项目检测，打印推荐组合与依据；证据不足时打印已确认 / 待确认维度与候选，不写入文件
dotnet run --project app/RuleAssembler/RuleAssembler.csproj -- --detect [目录]

# 把全部 Profile 的生成结果（默认能力集）输出到指定目录，用于批量对比验证
dotnet run --project app/RuleAssembler/RuleAssembler.csproj -- --emit <目录>
```

## 当前 Profile

manifest 当前包含 8 个技术大类、26 个 Profile（数量以 `fragments/manifest.json` 为准）。新增 Profile 时，在 `fragments/manifest.json` 增加配方，并在 `fragments/arch/<id>/` 增加对应片段；新增可复用能力时优先增加 `modules`，再由 Profile 通过 `moduleIds` 引用，并按「新增模块清单」补全输出声明、检测映射与测试。

## 版本与格式

- 规则版本和更新时间由 `manifest.json` 的 `vars` 统一控制。
- 所有生成文件统一使用 UTF-8 无 BOM 和 manifest 指定的 LF 行尾。
- 指针文件保留自身独立的 `rule_id` 和版本元数据。
- 根目录现有的 19 套架构目录仅是历史/生成快照，不是维护入口。
