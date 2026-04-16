# AI规则审查经验总结

## 规则文件夹清单

用于汇总仓库内当前已有的规则模板文件夹；具体修改过程与子任务状态以下方任务记录为准。

- `nextFullstack`
- `avaloniaExe`
- `dotnetBlazor`
- `dotnetConsole`
- `dotnetCoreSingle`
- `dotnetLayered`
- `python`
- `reactLinkdotNet`
- `reactTauriExe`
- `reactWailsExe`
- `vueLinkdotNet`
- `vueWailsExe`
- `wpfExe`
- `staticHtml`

---

## 2026-04-16：全面审查与修复

跟踪状态表：

| 文件夹名称 | 当前子任务状态 | 备注 |
|-----------|----------------|------|
| `nextFullstack` | `已完成` | 完成首次全面审查与修复 |
| `avaloniaExe` | `已完成` | 已完成首轮审查与规则修复 |
| `dotnetLayered` | `已完成` | 已完成首轮审查与规则修复 |

### `nextFullstack`

#### 审查范围
- **审查文件**：AGENTS.md、frontend-AGENTS.md、backend-AGENTS.md、doc-AGENTS.md、infra-AGENTS.md、CLAUDE.md、GEMINI.md
- **审查重点**：逻辑矛盾、模糊指令、结构解析失败风险
- **审查方法**：AI工程师视角，逐行分析规则表述，识别潜在的AI理解和执行风险

#### 发现的问题类别
1. **逻辑矛盾问题**：同一规则文件或不同文件之间存在相互冲突的表述
2. **模糊指令问题**：关键判断标准缺乏量化指标
3. **结构解析失败风险**：规则文件结构设计不当，可能导致AI解析错误

#### 修复的问题详情
1. **逻辑矛盾修复**：统一了Zustand使用范围的表述，补充缓存策略优先级规则，并将阶段门控改为逻辑一致的递进关系（一般原则 → 例外情况 → 约束条件）。
2. **模糊指令量化**：补充“简单问题”“单点低风险改动”“微小调整”“BLOCKED”“REVIEW”等量化定义，新增技术债务评估框架与任务状态触发条件。
3. **结构解析优化**：统一引入 YAML front matter，补充 `rule_id`、`version`、`last_updated`、`dependencies` 字段，并为各规则文件添加唯一标识。

#### 修改的文件
- `AGENTS.md`：添加判断标准定义、阶段门控修复、规则修改记录要求
- `frontend-AGENTS.md`：统一 Zustand 使用范围、缓存策略优先级、技术债务评估框架与量化标准
- `backend-AGENTS.md`：补充技术债务评估框架
- `doc-AGENTS.md`：统一元数据格式
- `infra-AGENTS.md`：统一元数据格式
- `CLAUDE.md`：统一元数据格式
- `GEMINI.md`：统一元数据格式

#### 当前子任务状态
- `已完成`

### `avaloniaExe`

#### 审查范围
- **审查文件**：AGENTS.md、ui-AGENTS.md、application-AGENTS.md、infrastructure-AGENTS.md、doc-AGENTS.md、CLAUDE.md、GEMINI.md
- **审查重点**：逻辑矛盾、模糊指令、结构解析失败风险
- **审查方法**：对照 `nextFullstack` 已修复口径与当前审查基线，逐项检查元数据、阶段门控、量化标准与子规则边界表述

#### 发现的问题
1. **结构解析失败风险**：所有规则文件均未采用 YAML front matter，且多个子规则文件仍以统一 H1 `# AGENTS.md` 起始，缺少 AI 解析所需的唯一标识、版本信息和依赖关系。
2. **模糊指令问题**：根 `AGENTS.md` 中“简单问题”“单点低风险改动”“BLOCKED”“REVIEW”等关键判断标准未量化，任务状态触发条件也不够具体，容易导致不同 AI 执行口径漂移。
3. **逻辑歧义问题**：`ui-AGENTS.md` 同时保留 `ViewModels/Base/` 基类目录建议，并要求“所有 ViewModel 必须继承 `ObservableObject`”，但未明确允许经公共基类间接继承，容易造成实现理解分歧。
4. **决策框架缺失**：`ui-AGENTS.md`、`application-AGENTS.md`、`infrastructure-AGENTS.md` 仅强调“不要强行重构或替换技术栈”，但缺少统一的技术债务评估框架，难以在沿用、必须重构、可选重构、禁止重构之间做出一致判断。

#### 需修复的文件
- `avaloniaExe/AGENTS.md`
- `avaloniaExe/ui-AGENTS.md`
- `avaloniaExe/application-AGENTS.md`
- `avaloniaExe/infrastructure-AGENTS.md`
- `avaloniaExe/doc-AGENTS.md`
- `avaloniaExe/CLAUDE.md`
- `avaloniaExe/GEMINI.md`

#### 修复的问题
1. **结构解析失败风险**：为 `AGENTS.md`、`ui-AGENTS.md`、`application-AGENTS.md`、`infrastructure-AGENTS.md`、`doc-AGENTS.md`、`CLAUDE.md`、`GEMINI.md` 统一补充 YAML front matter，增加 `rule_id`、`version`、`last_updated`、`dependencies` 元数据。
2. **模糊指令问题**：在根 `AGENTS.md` 中补充“简单问题”“单点低风险改动”“微小调整”“高风险内容”“BLOCKED”“REVIEW”等量化定义，并新增任务状态触发条件说明。
3. **逻辑歧义问题**：将阶段门控调整为“完整流程 + 可跳过阶段 0 的例外场景”，并在 `ui-AGENTS.md` 中明确 ViewModel 可通过公共基类间接继承 `ObservableObject`。
4. **决策框架缺失**：在 `ui-AGENTS.md`、`application-AGENTS.md`、`infrastructure-AGENTS.md` 中统一补充技术债务评估框架与重构决策流程。

#### 修改的文件
- `avaloniaExe/AGENTS.md`
- `avaloniaExe/ui-AGENTS.md`
- `avaloniaExe/application-AGENTS.md`
- `avaloniaExe/infrastructure-AGENTS.md`
- `avaloniaExe/doc-AGENTS.md`
- `avaloniaExe/CLAUDE.md`
- `avaloniaExe/GEMINI.md`

#### 当前子任务状态
- `已完成`

### `dotnetLayered`

#### 审查范围
- **审查文件**：AGENTS.md、backend-AGENTS.md、doc-AGENTS.md、infra-AGENTS.md、CLAUDE.md、GEMINI.md
- **审查重点**：逻辑矛盾、模糊指令、结构解析失败风险
- **审查方法**：对照 `nextFullstack` 与 `avaloniaExe` 已修复口径，逐项检查元数据、阶段门控、量化标准与技术债务决策表述

#### 发现的问题
1. **结构解析失败风险**：根规则、子规则与入口文件均未采用 YAML front matter，且多个文件仍以统一 H1 `# AGENTS.md` 起始，缺少 AI 解析所需的唯一标识、版本信息和依赖关系。
2. **模糊指令问题**：根 `AGENTS.md` 中“简单问题”“单点低风险改动”“BLOCKED”“REVIEW”“高风险内容”等关键判断标准未量化，任务状态也只有枚举没有触发条件，容易导致不同 AI 执行口径漂移。
3. **逻辑矛盾问题**：根 `AGENTS.md` 先写“新功能默认按以上阶段推进，不满足入场条件不得跳阶段”，随后又列出“可跳过阶段 0 的情况”，属于同一段落内的直接冲突，缺少统一的例外条件表达方式。
4. **决策框架缺失**：`backend-AGENTS.md` 与 `infra-AGENTS.md` 只有“不要强行重构或替换技术栈”这类原则性表述，缺少统一的技术债务评估框架，难以在沿用、必须重构、可选重构、禁止重构之间做出一致判断。

#### 需修复的文件
- `dotnetLayered/AGENTS.md`
- `dotnetLayered/backend-AGENTS.md`
- `dotnetLayered/doc-AGENTS.md`
- `dotnetLayered/infra-AGENTS.md`
- `dotnetLayered/CLAUDE.md`
- `dotnetLayered/GEMINI.md`

#### 修复的问题
1. **结构解析失败风险**：为 `AGENTS.md`、`backend-AGENTS.md`、`doc-AGENTS.md`、`infra-AGENTS.md`、`CLAUDE.md`、`GEMINI.md` 统一补充 YAML front matter，增加 `rule_id`、`version`、`last_updated`、`dependencies` 元数据。
2. **模糊指令问题**：在根 `AGENTS.md` 中补充“简单问题”“单点低风险改动”“微小调整”“高风险内容”“BLOCKED”“REVIEW”等量化定义，并新增任务状态触发条件说明。
3. **逻辑矛盾问题**：将阶段门控调整为“完整流程 + 可跳过阶段 0 的例外场景 + 例外约束条件”的一致表述。
4. **决策框架缺失**：在 `backend-AGENTS.md` 与 `infra-AGENTS.md` 中统一补充技术债务评估框架与重构决策流程。

#### 修改的文件
- `dotnetLayered/AGENTS.md`
- `dotnetLayered/backend-AGENTS.md`
- `dotnetLayered/doc-AGENTS.md`
- `dotnetLayered/infra-AGENTS.md`
- `dotnetLayered/CLAUDE.md`
- `dotnetLayered/GEMINI.md`

#### 当前子任务状态
- `已完成`

---

## 2026-04-16：自动提交触发规则

跟踪状态表：

| 文件夹名称 | 当前子任务状态 | 备注 |
|-----------|----------------|------|
| `nextFullstack` | `已完成` | 已补充自动提交触发规则并完成回读校验 |
| `dotnetBlazor` | `已完成` | 已补充自动提交触发规则；模板整体仍待全面审查 |
| `dotnetLayered` | `已完成` | 已补充自动提交触发规则并完成回读校验 |
| `avaloniaExe` | `已完成` | 已补充自动提交触发规则并完成回读校验 |

### `nextFullstack`

#### 修改原因
- 原规则只有 `Commit 规范` 与 `交付收口`，但未明确在什么条件下应自动提交，容易导致 AI 在“实现完成后是否提交”上执行不一致。

#### 主要修改内容
- 在 `nextFullstack/AGENTS.md` 中新增“提交触发规则”，明确“代码、配置或规则改动完成 + 完成本次所需验证 + `TASK.md` 对应任务状态更新为 `DONE`”后默认自动提交。

#### 补充约束
- 明确自动提交前需同步 `doc/devlog/`。
- 补充不得自动提交的场景，包括未完成验证、未更新任务状态、仍存在阻塞或用户明确要求暂不提交。

#### 验证情况
- 已回读 `nextFullstack/AGENTS.md`，确认中文显示、章节结构与规则前后关系正常。

#### 当前子任务状态
- `已完成`

### `dotnetBlazor`

#### 修改原因
- 当前模板的 `Commit 规范` 与 `交付收口` 之间缺少“何时默认自动提交”的明确触发条件，和 `nextFullstack` 的最新口径不一致。

#### 主要修改内容
- 在 `dotnetBlazor/AGENTS.md` 中新增“提交触发规则”，明确“代码、配置或规则改动完成 + 完成本次所需验证 + `TASK.md` 对应任务状态更新为 `DONE`”后默认自动提交。
- 在“交付收口”中新增“满足‘提交触发规则’时完成自动提交”，使交付闭环与 Commit 规范保持一致。

#### 补充约束
- 自动提交前必须确认 `doc/devlog/` 已补充，且提交信息符合 `Commit 规范`。
- 若验证未完成、任务状态未更新、仍存在阻塞，或用户明确要求暂不提交，则不得自动提交。

#### 验证情况
- 已回读 `dotnetBlazor/AGENTS.md`，确认中文显示、章节结构与新增规则位置正常。

#### 当前子任务状态
- `已完成`

### `dotnetLayered`

#### 修改原因
- 当前模板虽已有 `Commit 规范`、`提交前最小回归` 与 `交付收口`，但未明确在完成验证和更新任务状态后应默认自动提交。

#### 主要修改内容
- 在 `dotnetLayered/AGENTS.md` 中新增“提交触发规则”，统一自动提交的触发条件、前置检查与禁止场景。
- 在“交付收口”中补充“满足‘提交触发规则’时完成自动提交”，避免交付闭环遗漏提交动作。

#### 补充约束
- 自动提交前必须同步 `doc/devlog/`，并确保提交信息符合当前模板的 `Commit 规范`。
- 若仍存在阻塞、验证未完成、`TASK.md` 状态未更新，或用户明确要求暂不提交，则只说明当前状态，不自动提交。

#### 验证情况
- 已回读 `dotnetLayered/AGENTS.md`，确认新增规则与原有后端交付流程衔接正常。

#### 当前子任务状态
- `已完成`

### `avaloniaExe`

#### 修改原因
- 当前模板的交付规则缺少“完成验证并更新任务状态后默认自动提交”的明确说明，和已更新模板的 Git 交付口径不一致。

#### 主要修改内容
- 在 `avaloniaExe/AGENTS.md` 中新增“提交触发规则”，统一自动提交的触发条件、前置检查与禁止场景。
- 在“交付收口”中补充“满足‘提交触发规则’时完成自动提交”，让界面与应用层场景的交付闭环一致。

#### 补充约束
- 自动提交前必须确认 `doc/devlog/` 已补充，且提交信息符合当前模板的 `Commit 规范`。
- 若验证未完成、任务状态未更新、存在明确阻塞，或用户明确要求暂不提交，则不得自动提交。

#### 验证情况
- 已回读 `avaloniaExe/AGENTS.md`，确认中文显示、章节结构与新增规则位置正常。

#### 当前子任务状态
- `已完成`

---

## 2026-04-16：审计日志记录去重

跟踪状态表：

| 文件夹名称 | 当前子任务状态 | 备注 |
|-----------|----------------|------|
| `root` | `已完成` | 已补充审计记录去重规则并合并重复任务 |

### `root`

#### 修改原因
- `doc/AI-Rules-Audit-Log.md` 中先后出现“自动提交触发规则”和“扩展自动提交触发规则”两条语义重复的任务记录，导致同一主题被拆成平行条目，不利于后续追踪与维护。

#### 主要修改内容
- 将两条自动提交相关任务合并为同一条 `2026-04-16：自动提交触发规则` 记录，统一在一处维护 `nextFullstack`、`dotnetBlazor`、`dotnetLayered`、`avaloniaExe` 的同步情况。
- 在根 `AGENTS.md` 中补充通用规则，明确同一主题、同一批次或可并入同一结论的规则调整，在 `doc/AI-Rules-Audit-Log.md` 中只保留一条任务记录。

#### 补充约束
- 后续若同一主题继续扩展，应优先并入已有任务记录，不再新增语义重复的平行任务标题。

#### 验证情况
- 已回读 `AGENTS.md` 与 `doc/AI-Rules-Audit-Log.md`，确认重复任务已合并，标题层级与当前任务结构保持一致。

#### 当前子任务状态
- `已完成`

---

## 修改记录模板

```markdown
## [YYYY-MM-DD]：[具体任务名称]

跟踪状态表：

| 文件夹名称 | 当前子任务状态 | 备注 |
|-----------|----------------|------|
| `[文件夹名称]` | `[未开始/进行中/已完成]` | `[备注]` |

### `[文件夹名称]`

#### 修复的问题
1. **[问题类型]**：[具体问题描述]
2. **[问题类型]**：[具体问题描述]

#### 修改的文件
- [文件1]
- [文件2]

#### 当前子任务状态
- `[未开始/进行中/已完成]`
```
