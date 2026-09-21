# AI Code Rule

用于沉淀和维护不同技术架构下的 AI 协作开发规则。`fragments/` 是唯一维护源，仓库根下的架构目录仅作为历史生成快照，后续不再手工维护。

## 当前统一标准

`fragments/` 是仓库内统一维护入口，生成后的规则组合遵循“轻量入口 + 按需子规则”标准：

- 根 `AGENTS.md` 只保留硬约束、阶段门控和按需导航；需求澄清、任务、交付与回退流程统一放在生成的 `collaboration-AGENTS.md`，仅在命中场景读取
- 高频子规则优先补齐 `阅读摘要`、`常见任务入口`、`提交前最小回归`
- `TASK.md` 只维护活动任务与最近少量 `DONE` 任务

## 组合一览

| 组合 | 项目类型 | 典型子规则 | 使用场景 |
| --- | --- | --- | --- |
| dotnetConsole | .NET 控制台应用 | `application` / `doc` | CLI 工具、批处理服务、定时任务、数据迁移脚本、后台守护进程、交互式终端应用 |
| dotnetCoreSingle | .NET 单层 API 后端 | `backend` / `doc` / `infra` | 中小型 REST API、微服务中的单一职责服务、内部工具 API、MVP 快速验证、Serverless 本地开发模拟 |
| dotnetLayered | .NET 分层 API 后端 | `backend` / `doc` / `infra` | 中大型业务系统（ERP/CRM/OA）、DDD 项目、高单元测试覆盖率系统、多人协作长期维护的核心业务 |
| dotnetBlazor | .NET Blazor 全栈 Web | `ui` / `backend` / `doc` / `infra` | 企业内部管理系统、.NET 团队不想引入 JS 框架、实时交互仪表盘、已有 .NET 后端扩展 Web 界面 |
| dotnetBlazorWebApp | .NET Blazor Web App | `ui` / `backend` / `doc` / `infra` | 支持 Static SSR、Interactive Server、Interactive WebAssembly 与 Interactive Auto 的现代 Blazor Web App |
| vueLinkdotNet | Vue + .NET 前后端分离（多项目分层后端） | `frontend` / `backend` / `doc` / `infra` | Vue 3 企业级 SPA、前后端团队独立开发、数据可视化运营后台、已有 .NET 后端重建 Vue 前端 |
| vueLinkdotNetSingle | Vue + .NET 前后端分离（单项目后端） | `frontend` / `backend` / `doc` / `infra` | 同 vueLinkdotNet 但后端为单项目整洁架构（文件夹分层）：中小型项目快速起步、后续可平滑拆分为多项目 |
| reactLinkdotNet | React + .NET 前后端分离（多项目分层后端） | `frontend` / `backend` / `doc` / `infra` | React 18 企业应用、复杂表单/审批流系统、前后端团队独立开发、已有 .NET 后端选用 React 前端 |
| reactLinkdotNetSingle | React + .NET 前后端分离（单项目后端） | `frontend` / `backend` / `doc` / `infra` | 同 reactLinkdotNet 但后端为单项目整洁架构（文件夹分层）：中小型项目快速起步、原型验证、后续可平滑拆分为多项目 |
| reactLinkPython | React + Python 前后端分离 | `frontend` / `backend` / `doc` / `infra` | React SPA + FastAPI（Pydantic + SQLAlchemy）：AI/ML 推理服务配前端、数据处理平台、Python 团队主导的分离项目 |
| vueLinkPython | Vue + Python 前后端分离 | `frontend` / `backend` / `doc` / `infra` | 同 reactLinkPython 但偏好 Vue 生态 |
| reactLinkdotNet-ai | React + .NET + AI Agent 全栈 | `frontend` / `backend` / `ai` / `doc` / `infra` | AI Agent 产品 MVP、模型调用、工具与流式交互 |
| nextFullstack | Next.js 全栈 Web 应用 | `frontend` / `backend` / `doc` / `infra` | SaaS 平台、电商网站、SEO 敏感的公众网站、创业项目快速迭代、全栈一人搞定（仓库参考标准模板） |
| nuxtFullstack | Nuxt 3 全栈 Web 应用 | `frontend` / `backend` / `doc` / `infra` | 同 nextFullstack 但偏好 Vue 生态：Nuxt 3 文件路由 + Nitro 服务路由 + Prisma，SEO 敏感站点与 Vue 团队全栈项目 |
| generalProject | 通用项目协作模板 | `frontend` / `backend` / `doc` / `infra` | 尚未最终确定技术栈的新项目、需要先统一 AI 协作口径再细化实现方案的项目、希望尽量少预设框架约束的通用业务项目 |
| reactOnly | React 纯前端 SPA | `frontend` / `doc` / `infra` | 无独立后端的 React 单页应用：纯展示 / 工具型前端、调第三方 API 的轻应用、先做界面后补后端的原型 |
| reactOnly-ai | React 纯前端 + AI 产品 | `frontend` / `ai` / `doc` / `infra` | 无独立后端的 AI 前端产品：模型调用走托管网关 / 代理、密钥不下发浏览器、流式交互体验 |
| vueOnly | Vue 纯前端 SPA | `frontend` / `doc` / `infra` | 无独立后端的 Vue 3 单页应用：纯展示 / 工具型前端、调第三方 API 的轻应用、先做界面后补后端的原型 |
| vueOnly-ai | Vue 纯前端 + AI 产品 | `frontend` / `ai` / `doc` / `infra` | 无独立后端的 AI 前端产品（Vue 版）：模型调用走托管网关 / 代理、密钥不下发浏览器、流式交互体验 |
| generalFrontend | 通用纯前端（技术栈无关） | `frontend` / `doc` / `infra` | 无独立后端且不锁定框架的前端项目：Svelte / Lit / 其他栈，或仓库已有实现、只要协作规则不要技术栈预设 |
| python | Python API 后端 | `backend` / `doc` / `infra` | AI/ML 模型推理服务、数据处理 API、与 .NET 配合的 Python 微服务、自动化运维接口、快速原型 API |
| wpfExe | WPF Windows 桌面应用 | `ui` / `application` / `infrastructure` / `doc` | 工业控制/医疗/金融等 Windows 桌面终端、WinForms 迁移现代化、离线优先本地工具、硬件设备交互 |
| avaloniaExe | Avalonia 跨平台桌面应用 | `ui` / `application` / `infrastructure` / `doc` | 跨 Windows/macOS/Linux 的桌面工具、Linux 运维工具、WPF 跨平台迁移、嵌入式设备界面 |
| mauiMobile | .NET MAUI 移动端应用 | `ui` / `application` / `infrastructure` / `doc` | 跨 Android/iOS 的移动应用、企业移动办公、数据采集与巡检、已有 .NET 后端扩展移动端、轻量级消费者 App |
| flutterMobile | Flutter 移动应用 | `ui` / `application` / `backend` / `doc` | 跨 Android/iOS 的 Flutter 移动客户端 |
| vueWailsExe | Wails 桌面应用（Vue + Go） | `frontend` / `backend` / `doc` / `infra` | 轻量桌面工具（替代 Electron）、Go 服务加桌面界面、需原生系统能力的 Vue 应用、本地数据管理 |
| reactWailsExe | Wails 桌面应用（React + Go） | `frontend` / `backend` / `doc` / `infra` | 同 vueWailsExe 但偏好 React 生态、已有 React 组件复用到桌面、Go CLI 工具加图形界面 |
| reactTauriExe | Tauri 桌面应用（React + Rust） | `frontend` / `backend` / `doc` / `infra` | 极致轻量桌面应用（几 MB 安装包）、安全敏感工具、Rust 高性能需求、嵌入式/IoT 配套工具 |
| vueTauri | Tauri 桌面应用（Vue + Rust） | `frontend` / `backend` / `doc` / `infra` | 同 reactTauriExe 但偏好 Vue 生态（Vue 3 + Tailwind + Radix Vue + Pinia）、已有 Vue 组件复用到桌面 |
| reactElectronDotNet | Electron + .NET 桌面应用（React） | `frontend` / `backend` / `doc` / `infra` | Electron 桌面应用配 .NET 重度业务后端、复用已有 .NET 代码与生态、需要 Node 生态 + .NET 运行时、sidecar 子进程 IPC 架构，偏好 React 生态 |
| vueElectronDotNet | Electron + .NET 桌面应用（Vue） | `frontend` / `backend` / `doc` / `infra` | 同 reactElectronDotNet 但偏好 Vue 生态（Vue 3 + Element Plus + Pinia）、已有 Vue 组件复用到桌面 |
| reactElectron | Electron 桌面应用（React，无独立后端） | `frontend` / `backend` / `doc` / `infra` | 纯 Electron + React 桌面应用：本地业务由主进程（Node）承担、无需 .NET / Go / Rust 后端、依赖 Node 生态与 electron-builder / electron-updater 交付链路 |
| vueElectron | Electron 桌面应用（Vue，无独立后端） | `frontend` / `backend` / `doc` / `infra` | 同 reactElectron 但偏好 Vue 生态（Vue 3 + Element Plus + Pinia） |
| staticHtml | 纯静态页面 | `frontend` / `doc` | 营销落地页、企业官网、静态文档站、HTML 邮件模板、微信 H5 活动页、设计稿交互原型 |

## 模板选择决策指南

根据项目特征快速定位对应模板：

```
项目有前端界面吗？
├── 否 → 纯后端 API
│   ├── .NET 技术栈
│   │   ├── 无 Web 框架（CLI/批处理）→ dotnetConsole
│   │   ├── 单层结构（所有代码在一个项目）→ dotnetCoreSingle
│   │   └── 分层结构（Controller/Service/Repository 拆分）→ dotnetLayered
│   └── Python 技术栈 → python
│
├── 是 → 前后端是否同框架？
│   ├── 无独立后端（纯前端）
│   │   ├── React → reactOnly（需要 AI 能力 → reactOnly-ai）
│   │   ├── Vue → vueOnly（需要 AI 能力 → vueOnly-ai）
│   │   ├── 其他框架 / 不锁定 → generalFrontend
│   │   └── 无框架无构建（原生 HTML/CSS/JS）→ staticHtml
│   │
│   ├── 同框架（全栈）
│   │   ├── Next.js → nextFullstack
│   │   ├── Nuxt → nuxtFullstack
│   │   ├── Blazor → dotnetBlazor
│   │   ├── Blazor Web App（SSR / Server / WebAssembly / Auto）→ dotnetBlazorWebApp
│   │   └── 尚未锁定具体技术栈，先要通用协作规则 → generalProject
│   │
│   ├── 分离部署（前后端独立；.NET 后端多项目分层 → 默认，单项目 → *Single）
│   │   ├── Vue + .NET → vueLinkdotNet / vueLinkdotNetSingle
│   │   ├── React + .NET → reactLinkdotNet / reactLinkdotNetSingle
│   │   ├── Vue + Python（FastAPI）→ vueLinkPython
│   │   └── React + Python（FastAPI）→ reactLinkPython
│   │
│   ├── 移动端应用
│       └── .NET MAUI → mauiMobile
│
│   └── 桌面应用
│       ├── 仅 Windows
│       │   └── WPF → wpfExe
│       ├── 跨平台（原生 UI 框架）
│       │   └── Avalonia → avaloniaExe
│       ├── 跨平台（Web 技术驱动）
│       │   ├── Wails + Vue → vueWailsExe
│       │   ├── Wails + React → reactWailsExe
│       │   ├── Tauri + React → reactTauriExe
│       │   ├── Tauri + Vue → vueTauri
│       │   │   ├── Electron + React + .NET → reactElectronDotNet
│       │   │   ├── Electron + Vue + .NET → vueElectronDotNet
│       │   │   ├── Electron + React（无独立后端）→ reactElectron
│       │   │   └── Electron + Vue（无独立后端）→ vueElectron
```

## 规则源与生成结构

规则源集中维护在 `fragments/`，生成到目标项目后形成“根入口 + 子规则 + 任务主表”的使用结构：

```text
fragments/
├── root/AGENTS.md         # 公共根骨架，使用组件变量槽
├── pointers/              # 公共入口指针
├── shared/                # 跨架构共享小节，子规则用 include 指令引用
├── arch/<id>/             # 当前架构的专属规则片段
├── manifest.json          # 模块、家族、组件、约束、兼容样例与输出配方
└── ...                    # design-system 等能力片段
```

组合内核：manifest 的 `components` / `constraints` / `compatibilityCases` 节点是维度组合的唯一事实来源（schema 3，`profiles` 节点与旧渲染路径已退役），`CompositionPlanner` 把「形态 + 壳 + 前端 + 后端」的维度选择解析为组件集合并渲染产物；34 个组合全部注册为兼容样例（携带 label / desc / family 展示元数据，经典入口、检测候选展示与配方三件套都按样例取值），其中 `reactLinkdotNet-ai` 已拆为「.NET AI Web 壳 + React AI 前端 + 单项目 .NET 后端 + AI capability/module」，其他组合按复用价值逐步收敛。组件还可以声明 `moduleIds`，组合始终从 `core` 开始按组件顺序合并模块；同一输出支持一个 `replace` 基底和多个按顺序拼接的 `append` 贡献，能力过滤与槽位替换逐项执行，重复基底或缺少基底会显式报错。`--check` 对每个样例做组合口径的输出契约校验（声明 vs 产物）、组件问答决策树仿真与内置技能校验；`constraints` 承载壳 / 形态 / 前端 / 后端的钳制关系（如 tauri→rust、wails→go、electron 仅 dotnet/主进程、纯后端无前端），问答剪枝先过钳制约束，再做结构锚定校验（已回答维度必须仍被存续组件表达），违反钳制或缺组件锚定的组合在问答与 `--plan` 中直接拒绝。新增技术类型 = 扩展组件而非复制整套组合。

## 使用方式

1. 根据项目技术栈准备目标项目目录。
2. 将 `use-ai-rule.exe` 复制到目标项目根目录并双击启动（启动时先打印当前规则版本），先选择模式：「生成规则文件（含可选 Skills 导入）」、「检测当前目录组合（仅查看）」或「仅导入 Skills」（不生成规则文件、不写配方记录）。
3. 选择生成规则文件时，生成器先自动检测目录特征（sln/csproj、package.json、wails.json、tauri、pubspec 等）给出推荐组合：证据充分时直接推荐；证据不足时列出已确认维度、待确认维度与候选组合，走补答流程（已确认项保留，只回答缺失项），不伪造推荐。问答走组件决策树（项目形态 → 前端/界面 → 桌面壳 → 后端 → 细分项）：每个问题只列「存续组件间仍有分歧」的选项并经可行性剪枝（不会选出无法生成的组合），唯一取值自动推导不提问；也可选「经典选择（兼容）」走技术大类 → 组合。交互终端中菜单为方向键 + 回车选择，选中「0」项即取消或返回上一步；脚本/管道调用时自动退化为编号输入模式。
4. 组合确定后进入 Q5 能力开关：按需勾选文档 / 基础设施 / 设计系统等能力（默认 = 当前组合，不可用能力自动隐藏），关闭的能力不会生成文件、根导航也不会再引用。
5. 规则文件落盘后可选择导入内置 AI Skills：生成器已全量打包仓库 `skills/`，多选技能与目标工具（Codex / ZCode / Claude Code / pi）后按项目级目录约定整体写入，不需要时选「跳过，不导入」；导入是追加语义，目标目录已有同名技能时会提示覆盖或跳过（覆盖 = 同名技能目录先删后写、按内置内容整体重写）；勾选结果记入配方，`--recipe` / `--upgrade` 按配方同步重放（同名技能先删后写）。
6. 开始协作时先读根 `AGENTS.md`，再根据任务命中对应子规则，不默认预读全部文件。
7. 开发过程中通过 `TASK.md` 维护活动任务与最近少量已完成任务（`TASK.md` 由目标项目自己维护，生成器只引用不释放）。

文档维护按业务模块和功能拆分：`doc/requirements/<module>/<feature>.md` 记录业务需求，`doc/design/<module>/<feature>.md` 记录对应技术设计，`doc/index.md` 及各目录索引负责导航。设计文档通过 `REQ-` / `DES-` 编号建立追溯，不重复维护需求正文；新增接口先用 `CON-` 契约草案承载完整字段，落地后以 DTO / Schema / 接口代码为最终事实；跨模块方案放在 `doc/design/architecture.md`。

### 一键导出（推荐）

仓库附带一个交互式拼接器，免去手动复制：

```powershell
# 构建（首次）：默认发布三平台 self-contained 单文件矩阵（目标机无需安装 .NET 运行时）
./app/RuleAssembler/publish.ps1

# Windows 下也可以在仓库根目录双击或执行；默认只打 win-x64，传参 all 时打三平台矩阵
./build-use-ai-rule.bat all

# 产物位于 dist/：use-ai-rule.exe（win-x64）、use-ai-rule-linux-x64、use-ai-rule-osx-x64。
# Windows 产物双击启动；mac/linux 产物复制到目标项目根目录后用终端执行（如 ./use-ai-rule-linux-x64）：
# 启动时先选模式：生成规则文件（含可选 Skills 导入）、检测当前目录组合（仅查看），或仅导入 Skills（不生成规则文件、不写配方记录，gitignore 按 minimal 处理）。
# 生成模式：自动检测当前项目并推荐组合（含判断依据）；证据不足时展示待确认维度与候选，
# 补答只问缺失项、已确认项保留。也可以选择问答决策树（项目形态 → 前端/界面 → 桌面壳 → 后端 → 细分项，
# 只出现已接入的选项；桌面混合应用会先选壳 Electron/Tauri/Wails，再问该壳下的后端）或经典的「技术大类 → 组合」（兼容入口）。
# 组合确定后进入 Q5 能力开关（文档 / 基础设施 / 设计系统等，默认 = 当前组合），
# 随后列出实际生成文件并统一确认后释放（AGENTS + 子规则 + CLAUDE/GEMINI/GROK），同名文件会直接覆盖；
# 例外：已定制（meta.status=已定制）的 design-system.yaml 保留本地版本不覆盖，仍是基线时才刷新。
# 不释放 TASK.md（运行时任务清单，不随规则分发，配方中按 referenced-existing 声明）。
# 生成成功后会在目标项目写入 .ai-rule-profile.json 配方记录（记录维度选择、命中组件、回答来源与能力开关），
# 并自动把该记录、生成器本体（use-ai-rule* 通配，覆盖 .exe、无扩展名与带 RID 后缀的历史副本）、规则效果反馈记录（.ai-rule-feedback.md）与 AI 工具本地目录（.zcode/、.claude/、.dotnet/、.codex/、.pi/、.cursor/、.codegraph/、.learnings/）加入目标项目 .gitignore（没有 .gitignore 时按技术栈创建一份默认规则）。
# 规则文件落盘后还可选导入内置 AI Skills（生成器已全量打包仓库 skills/，无需再手动拷贝）：
# 多选技能（35+ 个，含 dotnet-skills / officeSkills 等分组下的技能）、多选目标工具后按项目级目录约定整体写入：
#   Codex → .codex/skills/<技能>/，ZCode → .zcode/skills/<技能>/，Claude Code → .claude/skills/<技能>/，pi → .pi/skills/<技能>/
# 导入是追加语义：未选中的技能不动；目标目录已有同名技能（含 SKILL.md）时提示「覆盖」或「跳过」（覆盖 = 同名技能目录先删后写、按内置内容整体重写；跳过 = 保留本地版本，且不记入配方）。
# 勾选结果记入配方（skills / skill_targets），--recipe / --upgrade 会按配方同步重放（内置内容直接覆盖，本地改过的技能先备份）。
# 已有配方时可用 --recipe 按记录重放生成（升级规则或在新项目还原同一组合）：
#   use-ai-rule.exe --recipe .ai-rule-profile.json [目标目录]
# 规则版本升级后用 --upgrade 原地升级：配方内容哈希识别本地修改，默认保留、--force 全部覆盖：
#   use-ai-rule.exe --upgrade [目录] [--force]
```

发布产物是 self-contained 单文件（win-x64 / linux-x64 / osx-x64 三平台矩阵，目标机器无需安装 .NET 10 运行时；本机需 .NET SDK 10 构建）。拼接器把公共入口、指针和可复用能力抽成模块，按所选组合填入差异内容后输出；子规则中的共性小节（如提交前最小回归、无法执行测试时）集中维护在 `fragments/shared/`，由各架构片段通过 `<!-- include: ... -->` 指令引用，改一处即可同步全部组合。后续只维护 `fragments/`，重新构建即可让所有组合生效（根目录原有的 19 套架构快照目录已于 2026-08-28 删除）。详见 `app/RuleAssembler/` 与 `fragments/`。

生成的规则内置效果反馈闭环：AI 在目标项目协作中发现规则被违反、与现实冲突或缺失时，任务收尾把条目记入项目根目录 `.ai-rule-feedback.md`（本地产物，已加入 `.gitignore` 不随项目提交）；把这些反馈回传本仓库，按 `doc/规则效果反馈承接.md` 的口径登记并转化为 `fragments/` 修改，目标项目再通过 `--upgrade` 拿到改进后的规则。

## 共同规则

所有模板共享以下基础约束：

- 先理解，再修改；不要基于猜测直接改代码或规则
- 先复用，再新增；避免无关重构和重复体系
- 交付必须真实可用；涉及新增、修改、删除的业务逻辑不能停留在伪交互层面
- 能验证的地方优先验证；无法验证时必须说明原因、影响与建议下一步
- 如果编写注释，统一使用中文
- 单个类、页面、组件、页面基类应保持职责单一，避免持续膨胀
- Windows / PowerShell 场景下禁止使用 `&&` 链式命令
