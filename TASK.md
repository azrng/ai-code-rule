# TASK：组合式 AI Rule 生成器改造（分期执行）

> 本文件是当前任务的 AI 交接清单。接手后从“当前任务”继续执行，完成后可删除。
>
> 分支：`guided-wizard-detect`
>
> 北极星方案：[doc/组合式AI-Rule生成器改造方案.md](doc/组合式AI-Rule生成器改造方案.md)
>
> 旧批次（问答向导、项目检测、配方记录、Spectre.Console 交互层，1.36.0~1.39.0）已完成，历史记录见 `doc/rulelog/2026-09-02-生成器问答向导与项目检测.md`。

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
Q2 后端：无 / .NET 分层 / .NET 单层 / Python FastAPI / Go(Wails)
Q3 前端：无 / React / Vue / Blazor / 静态 HTML（按形态追加已有技术栈）
Q4 壳：桌面或混合应用时显示 Tauri / Wails / Electron
Q5 能力：doc、infra 默认勾选；AI、design-system 按能力和场景选择
    -> 汇总确认
    -> 列出实际生成文件
    -> 生成
```

“默认勾选”只适用于 Q5 能力，不代表存在默认技术栈组合。证据不足时不得隐式补出一个 Profile。

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
- 更新规则版本、README 和现有主题 rulelog；若只改代码且生成正文不变，按版本口径处理。
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
| P1-1 | 结构化根声明与 `--check` 一致性校验 | 未开始 | 无 |
| P1-2 | 检测部分结果、未确认字段和补答 | 未开始 | P1-1 的输出契约可复用，但代码上可并行 |
| P1-3 | Q5 能力开关与根导航联动 | 未开始 | P1-1 |
| P1-4 | 扩展约束、兼容回归和文档收口 | 未开始 | P1-1、P1-2、P1-3 |

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
- 更新 `README.md`、`app/RuleAssembler/README.md` 和本文件；同一主题只更新现有 rulelog，不新增平行审计记录。

## 4. P1 实施顺序和提交规则

P1 各项可以独立回滚，但不是互不依赖的平行改动。推荐顺序：

```text
P1-1 结构化输出契约
    -> P1-2 检测部分结果
    -> P1-3 能力开关和根导航
    -> P1-4 回归与文档
```

每个 P1 子任务单独提交，并同步更新本文件和 `doc/rulelog/2026-09-02-生成器问答向导与项目检测.md` 的主题记录状态。后续提交可以基于前一个提交，不要求每个提交脱离依赖后单独从主分支编译通过。

### 4.1 版本口径

- 仅代码校验、检测逻辑和测试变更，且生成内容不变时，不修改规则版本。
- Q5 能力开关导致生成路径增加或根导航生成逻辑变化时，更新 `fragments/manifest.json` 的 `vars.version` 和 `last_updated`；版本建议从当前 `1.39.0` 进入下一 minor 版本。
- 任何会改变已生成规则正文的调整，必须在 rulelog 中写明影响的 Profile、文件和差异原因。

### 4.2 回归命令

```powershell
dotnet run --project app/RuleAssembler -- --check
dotnet run --project app/RuleAssembler -- --emit D:/tmp/rules-before
dotnet test test/RuleAssembler.Tests
```

P1 新增能力选择不能直接用旧的“输出全部 Profile”命令表达，应增加测试入口或 recipe fixture，明确验证具体 Selection 和 `features`。不要为了测试能力开关而修改旧 `--emit` 语义。

### 5. P2 完整组合内核

P2 暂缓，但不是无限期挂起。满足以下任意两项，或出现一次无法通过现有骨架安全表达的跨族组合，即启动 P2：

- 新需求需要复制两个以上现有 Profile 的规则文件。
- 同一规则修改必须同步三个以上 Profile。
- 合法需求无法通过现有 Profile、`sources` 和 skeleton 表达。
- 出现跨族组合，例如 React + .NET 分层后端 + Tauri。
- 新增技术类型只能通过增加新的完整 Profile 才能接入。
- Profile 数量继续增长且重复规则占主要部分。

启动 P2 时按以下阶段执行：

1. 增加 `Selection`、`Component`、`Constraint`、`CompositionPlan` 和 `CompositionPlanner`，保留旧 Profile 渲染路径。
2. 将 `forms`、`choices`、`components`、`constraints` 和 `compatibilityCases` 接入 manifest。
3. 将向导、检测器和汇总界面切换到 Selection；Profile 列表移出主入口，仅保留兼容命令。
4. 配方记录使用 `selection + component_ids + features`，增加 `--recipe`；保留 `--emit-all-legacy` 做旧产物回归。
5. 逐个迁移 21 个兼容样例，比较文件路径、文件数量、版本元数据和正文，最后再评估删除 `profiles` 节点。

P2 的完成标准是：新增常规前端、后端、壳或能力模块时，不需要复制完整 Profile，也不需要修改渲染器核心分发流程。

## 6. 当前事实和注意事项

### 接手必读

- `ResourceStore` 把 `fragments/**` 以 `Fragments.` 前缀嵌入程序集；manifest 校验失败会直接抛异常。
- `ManifestLoader.MergeFamilyDefaults` 在 JSON 层做 family rootDefaults 继承，新的结构化输出字段不能破坏该过程。
- `OutputWriter.WriteFiles` 自带 staging、backup 和回滚；配方记录必须在规则文件写入成功后再写。
- 预设产物回归基线：`dotnet run --project app/RuleAssembler -- --emit D:/tmp/rules-before`；该命令当前只用于旧 Profile 预览，不写配方记录。
- 当前共有 21 个 Profile，旧渲染入口是 `RuleRenderer.Plan(manifest, profile)`；P1 保留它作为兼容路径。
- 当前只有 `core` 和 `ai-capability` 两个顶层 module，不能把现状 module 数量误认为已经完成组件化。
- 当前 `Subrule.sources` 已支持多源拼接，但只有显式声明时才能合并；重复输出不得静默覆盖。
- 允许修改 `fragments/manifest.json` 和新增模块元数据；禁止为了完成 P1 无计划地重写已有规则正文。
- Windows / PowerShell 文档和命令中禁止使用 `&&` 链式命令；新增注释统一使用中文。
