# 组合式 AI Rule 生成器改造方案

> 文档状态：待实施
>
> 编写日期：2026-09-02
>
> 适用范围：`app/RuleAssembler`、`fragments/manifest.json`、`fragments/` 模块化规则源

## 1. 改造目标

当前生成器以 Profile 为最终选择单位。问答向导通过过滤 Profile 得到唯一结果，项目检测也会尝试推荐一个现成 Profile。这种方式会持续推动维护更多预设组合，无法真正支持“选择后再拼装”。

本次改造改为以下模型：

```text
用户选择 Selection
    -> 校验可用组合
    -> 解析基础模块、技术栈模块和可选能力模块
    -> 合并根规则与子规则
    -> 生成 CompositionPlan
    -> 汇总文件
    -> 用户确认后写入
```

目标是：

- 用户界面不再以 Profile 列表作为主要入口。
- 不维护大规模的技术栈笛卡尔积。
- 只为真正独立的技术能力维护模块。
- 不因证据不足而隐式选择一个默认技术栈组合。
- 生成前明确列出实际会生成的文件。
- 保留现有 Profile 作为迁移期兼容样本，确保规则内容不发生计划外变化。

## 2. 交互设计

### 2.1 主流程

无参数启动时采用以下流程：

```text
扫描当前项目
    |
    +-- 有高置信度证据 -> 回填部分选择，并允许修改
    |
    +-- 无足够证据 ----> 从 Q1 开始选择
    |
    v
Q1 项目形态
Q2 前端或界面
Q3 后端
Q4 桌面壳（条件显示）
Q5 可选能力
    |
    v
汇总选择、模块、生成文件和警告
    |
    v
确认生成
```

检测结果只作为选择建议，不直接生成。检测不完整时，后续问题只询问尚未确认或存在冲突的字段。

### 2.2 问题定义

#### Q1 项目形态

- Web 前后端分离
- 桌面应用
- 移动应用
- 纯后端服务
- 通用项目

必要时保留“纯前端/静态站点”作为 Web 的独立形态，避免把静态页面误当成前后端分离项目。

#### Q2 前端或界面

基础选项：

- 无独立前端
- React
- Vue
- Blazor
- 静态 HTML

为了覆盖当前已有规则，按照项目形态追加显示：

- 桌面：Avalonia、WPF、控制台、React、Vue、静态 HTML
- 移动：.NET MAUI、Flutter
- Web：React、Vue、Blazor、静态 HTML

界面先于后端提问（2026-09-04 调整）：原生桌面（Avalonia / WPF / 控制台）与混合壳（Electron / Wails / Tauri）的分歧在界面维度，先问后端会让 C# 桌面用户的「.NET」回答进入 Electron 分支、Avalonia / WPF 选项永不出现；先问界面后，原生桌面组合直接收敛，后端问题只对混合壳与 Web 形态出现。

#### Q3 后端

选项使用业务含义明确的值：

- 无后端
- .NET 分层
- .NET 单层
- Python FastAPI
- Go（Wails）
- Node.js/Next.js（现有全栈能力需要保留时启用）

选项由 Q1 / Q2 回答过滤。例如“纯后端服务”不显示“无后端”，“Web + React”保留“无后端”（纯前端 SPA 组合）。

#### Q4 桌面壳

只在桌面或混合应用显示：

- 无桌面壳
- Tauri
- Wails
- Electron

壳是宿主/桥接能力，不完全等价于业务后端：

- Wails 要求 Go 宿主模块。
- Tauri 引入 Rust 壳模块，但不把 Rust 误认为业务后端。
- Electron 可以与 .NET、Node.js 或无业务后端组合。

#### Q5 可选能力

Q5 使用复选框，初始状态只代表界面建议，最终选择必须写入配方：

- `[x]` `doc-AGENTS.md`
- `[x]` `infra-AGENTS.md`
- `[ ]` AI 能力模块
- `[ ]` `design-system.yaml`

没有界面模块时隐藏 `design-system.yaml`。AI 能力只在存在兼容能力模块时显示；暂不支持的组合不应通过生成阶段才报错。

## 3. 新的组合模型

### 3.1 Selection

新增用户选择对象，作为向导、检测器、命令行和配方记录的共同输入：

```text
Selection
  Form             项目形态
  Backend          后端选项
  Frontend         前端或界面选项
  Shell            桌面壳选项
  Features         doc、infra、ai、design-system 等能力
```

字段可以为空，表示尚未确认；只有经过组合校验且必要字段完整后，才可以生成。

### 3.2 Component

Manifest 中新增组件定义。组件只负责一个清晰职责，可以提供：

- 一个或多个规则源文件。
- 输出文件名和合并方式。
- 根变量贡献。
- 适用条件。
- 与其他组件的约束。
- 展示名称、说明和检测提示。

组件分类建议为：

```text
core
form:*
backend:*
frontend:*
shell:*
capability:doc
capability:infra
capability:ai
capability:design-system
```

### 3.3 CompositionPlan

新增组合解析结果，供 `RuleRenderer` 和汇总界面使用：

```text
CompositionPlan
  Selection
  SelectedComponents
  RootContext
  PlannedFiles
  Warnings
```

`CompositionPlan` 必须是确定性的：同一份 Selection、同一版本 manifest 必须得到同样的组件顺序和文件列表。

## 4. Manifest 设计

当前 `profiles` 不再作为新的用户入口。建议在迁移期增加以下节点：

```json
{
  "forms": {},
  "choices": {},
  "components": [],
  "constraints": [],
  "compatibilityCases": []
}
```

组件示意：

```json
{
  "id": "backend-dotnet-single",
  "kind": "backend",
  "label": ".NET 单层",
  "when": {
    "backend": "dotnet-single"
  },
  "files": [
    {
      "source": "shared/backend/dotnet-single.md",
      "output": "backend-AGENTS.md",
      "merge": "replace"
    }
  ]
}
```

约束示意：

```json
{
  "when": { "shell": "wails" },
  "require": { "backend": "go-wails" }
}
```

Manifest 中应明确声明合法关系，不再用“是否存在某个 Profile”间接表达组合关系。

## 5. 模块合并规则

### 5.1 文件合并

同一个输出文件可以由多个模块贡献，但必须声明合并方式：

- `replace`：该输出只能有一个提供者。
- `append`：按 manifest 顺序拼接多个片段。
- `sources`：继续复用当前已有的多源拼接机制。

禁止未声明的重复输出，禁止静默覆盖。front matter 只允许存在于最终文件的第一个源。

典型场景：

```text
backend:dotnet-single + shell:electron
    -> backend-AGENTS.md 的 .NET 基础规则 + Electron IPC 差异段
```

### 5.2 根变量合并

根 `AGENTS.md` 不应继续依赖完整 Profile 中预填好的全部变量。建议按以下优先级合并：

```text
form 基础值
    < backend / frontend / shell 领域值
    < 用户明确的高级覆盖值
```

合并规则：

- 列表类字段按声明顺序合并并去重。
- 标量字段同级重复贡献时直接校验失败。
- `target-list` 和 `subrule-index` 根据最终 PlannedFiles 自动生成，避免出现“根文件声明了不存在的子规则”。
- `dev-stage`、作用范围和高风险词等领域字段由组件贡献，不能由后加入的模块无提示覆盖。

## 6. 生成器改造范围

### 6.1 代码文件

- `Models.cs`：增加 `Selection`、组件定义、约束、`CompositionPlan` 和配方字段。
- 新增 `CompositionPlanner.cs`：解析选择、筛选组件、校验约束、合并输出。
- `WizardEngine.cs`：从“Profile 候选过滤”改为“选择项和合法选项计算”。
- `ConsoleWizard.cs`：实现 Q1-Q5、条件选项、返回上一步和最终汇总。
- `ProjectDetector.cs`：输出部分 Selection 和证据，不再以 Profile ID 作为核心结果。
- `RuleRenderer.cs`：接收 `CompositionPlan`，支持模块贡献合并。
- `Program.cs`：统一检测、问答、汇总、确认和生成流程。
- `RecipeRecorder.cs`：记录 Selection、组件 ID、生成文件和检测证据。
- `ManifestLoader.cs`：校验选项、组件、约束、合并冲突和兼容样例。

### 6.2 规则源和 Manifest

- 在 `fragments/shared/` 或新的组件目录中抽取真正可复用的规则骨架。
- 现有 `shared/skeletons/` 和 `Subrule.sources` 作为模块化迁移的基础设施继续使用。
- 将 doc、infra、AI、design-system 从 Profile 专属配置逐步提取为能力模块。
- 旧 Profile 的特有差异暂时保留，避免为了模型改造一次性重写规则正文。

## 7. 旧 Profile 迁移策略

不立即删除当前 20 个 Profile，采用兼容适配：

1. 为旧 Profile 增加 `legacy` 标记或单独的兼容节点。
2. 编写 Profile 到 Selection 的转换器。
3. 用旧 Profile 作为 `compatibilityCases`，验证新组合器可以生成相同文件和正文。
4. 新交互界面隐藏 Profile 列表。
5. 维护命令保留 `--emit-all-legacy`，用于历史产物回归。
6. 新增组合优先使用组件，不再新增完整 Profile。
7. 所有旧组合完成回归后，再评估是否删除 `profiles` 节点。

这样可以同时满足两点：用户不再被迫认领预设组合，现有规则内容也不会因为生成器重构而整体漂移。

## 8. 检测与命令行

### 8.1 项目检测

检测器按维度产生证据：

```text
form=desktop
backend=dotnet-single
frontend=react
shell=electron
```

检测结果应包含：

- 已确认字段。
- 低置信度字段。
- 未确认字段。
- 每个字段的判断依据。

证据不足时保留空值，交给问答补充，不使用隐藏的默认组合兜底。

### 8.2 命令行

建议调整为：

```powershell
use-rule.exe --check
use-rule.exe --detect [目录]
use-rule.exe --emit <目录> --recipe <recipe.json>
use-rule.exe --emit-all-legacy <目录>
```

`--check` 校验组件和约束，并运行兼容样例；不再要求每条问答路径必须收敛到某个 Profile。

## 9. 配方记录

`.ai-rule-profile.json` 文件名可以暂时保持不变，但内容应改为组合配方：

```json
{
  "schema_version": 2,
  "manifest_version": "1.x.x",
  "mode": "wizard",
  "selection": {
    "form": "desktop",
    "backend": "dotnet-single",
    "frontend": "react",
    "shell": "electron",
    "features": ["doc", "infra"]
  },
  "component_ids": [
    "core",
    "form-desktop",
    "backend-dotnet-single",
    "frontend-react",
    "shell-electron",
    "capability-doc",
    "capability-infra"
  ],
  "files": []
}
```

旧字段 `profile_id` 在迁移期保留为可选字段，仅用于兼容历史记录，不作为新生成流程的必要字段。

## 10. 实施阶段

### 阶段 1：建立组合内核

- 增加 Selection、Component、Constraint、CompositionPlan 模型。
- 增加 `CompositionPlanner`。
- 保持现有 Profile 渲染路径不变。
- 为组合器增加最小的 Web、.NET、React、Electron、Wails 测试样例。

### 阶段 2：迁移 Manifest 和模块

- 增加 forms、choices、components、constraints 配置。
- 先迁移 core、doc、infra、React、Vue、.NET 单层、.NET 分层。
- 再迁移 Blazor、Electron、Tauri、Wails、移动端和历史特殊 Profile。
- 抽取模块时保留现有片段内容，优先调整 manifest 引用关系。

### 阶段 3：替换交互入口

- `ConsoleWizard` 改为 Q1-Q5 选择流程。
- 实现条件显示、返回上一步、冲突提示和汇总确认。
- 从主界面移除经典 Profile 选择。
- 经典 Profile 只保留为兼容命令或测试入口。

### 阶段 4：接入检测和配方

- `ProjectDetector` 改为输出 Selection 建议。
- 配方记录改为保存完整 Selection 和组件列表。
- `--detect` 输出未确认字段，不强行给出默认组合。
- `--emit` 改为接收显式 recipe。

### 阶段 5：回归和清理

- 对现有 20 个 Profile 建立兼容样例。
- 逐个比较新旧生成文件的数量、路径、版本元数据和正文。
- 完成文档、测试和 rulelog 更新。
- 兼容层稳定后，再决定是否移除旧 `profiles` 配置。

## 11. 验收标准

- 无检测证据时，程序不会自动选择一个完整技术栈组合。
- 用户可以独立选择项目形态、后端、前端、桌面壳和能力模块。
- 非法组合不会出现在选项中，或会在汇总阶段明确阻止生成。
- doc 和 infra 初始勾选，但取消后不会生成对应文件。
- 汇总阶段列出的文件与实际写入文件完全一致。
- 同一个输出文件的多模块贡献必须显式声明合并方式。
- `--check` 能发现无效组件、死组合、根变量冲突和输出冲突。
- 现有兼容样例生成结果与改造前一致，差异仅限于已批准的版本元数据变化。
- `.ai-rule-profile.json` 能完整还原本次 Selection、组件和生成文件。
- Windows / PowerShell 使用说明不引入 `&&` 链式命令，新增注释继续使用中文。

## 12. 最终设计结论

本次改造不应继续扩展“Profile 决策树”，而应将 Profile 降级为历史兼容数据，把 `Selection -> Component -> CompositionPlan` 作为新的主链路。

Q1-Q5 负责表达用户真实选择，Manifest 负责表达模块和合法组合，组合器负责确定最终生成结果，检测器负责提供证据，配方记录负责保存可复现输入。这样新增一种前端、壳或能力时，通常只需增加一个模块和少量约束，不需要再复制一整套 Profile。
