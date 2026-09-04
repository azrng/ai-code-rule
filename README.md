# AI Code Rule

用于沉淀和维护不同技术架构下的 AI 协作开发规则。`fragments/` 是唯一维护源，仓库根下的架构目录仅作为历史生成快照，后续不再手工维护。

## 当前统一标准

`fragments/` 是仓库内统一维护入口，生成后的 Profile 遵循“轻量入口 + 按需子规则”标准：

- 根 `AGENTS.md` 只保留硬约束、阶段门控和按需导航；需求澄清、任务、交付与回退流程统一放在生成的 `collaboration-AGENTS.md`，仅在命中场景读取
- 高频子规则优先补齐 `阅读摘要`、`常见任务入口`、`提交前最小回归`
- `TASK.md` 只维护活动任务与最近少量 `DONE` 任务

## Profile 一览

| Profile | 项目类型 | 典型子规则 | 使用场景 |
| --- | --- | --- | --- |
| dotnetConsole | .NET 控制台应用 | `application` / `doc` | CLI 工具、批处理服务、定时任务、数据迁移脚本、后台守护进程、交互式终端应用 |
| dotnetCoreSingle | .NET 单层 API 后端 | `backend` / `doc` / `infra` | 中小型 REST API、微服务中的单一职责服务、内部工具 API、MVP 快速验证、Serverless 本地开发模拟 |
| dotnetLayered | .NET 分层 API 后端 | `backend` / `doc` / `infra` | 中大型业务系统（ERP/CRM/OA）、DDD 项目、高单元测试覆盖率系统、多人协作长期维护的核心业务 |
| dotnetBlazor | .NET Blazor 全栈 Web | `ui` / `backend` / `doc` / `infra` | 企业内部管理系统、.NET 团队不想引入 JS 框架、实时交互仪表盘、已有 .NET 后端扩展 Web 界面 |
| vueLinkdotNet | Vue + .NET 前后端分离 | `frontend` / `backend` / `doc` / `infra` | Vue 3 企业级 SPA、前后端团队独立开发、数据可视化运营后台、已有 .NET 后端重建 Vue 前端 |
| reactLinkdotNet | React + .NET 前后端分离 | `frontend` / `backend` / `doc` / `infra` | React 18 企业应用、复杂表单/审批流系统、前后端团队独立开发、已有 .NET 后端选用 React 前端 |
| reactLinkdotNet-ai | React + .NET + AI Agent 全栈 | `frontend` / `backend` / `ai` / `doc` / `infra` | AI Agent 产品 MVP、模型调用、工具与流式交互 |
| nextFullstack | Next.js 全栈 Web 应用 | `frontend` / `backend` / `doc` / `infra` | SaaS 平台、电商网站、SEO 敏感的公众网站、创业项目快速迭代、全栈一人搞定（仓库参考标准模板） |
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
| reactElectronDotNet | Electron + .NET 桌面应用（React） | `frontend` / `backend` / `doc` / `infra` | Electron 桌面应用配 .NET 重度业务后端、复用已有 .NET 代码与生态、需要 Node 生态 + .NET 运行时、sidecar 子进程 IPC 架构，偏好 React 生态 |
| vueElectronDotNet | Electron + .NET 桌面应用（Vue） | `frontend` / `backend` / `doc` / `infra` | 同 reactElectronDotNet 但偏好 Vue 生态（Vue 3 + Element Plus + Pinia）、已有 Vue 组件复用到桌面 |
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
│   │   ├── Blazor → dotnetBlazor
│   │   └── 尚未锁定具体技术栈，先要通用协作规则 → generalProject
│   │
│   ├── 分离部署（前后端独立）
│   │   ├── Vue + .NET → vueLinkdotNet
│   │   └── React + .NET → reactLinkdotNet
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
│       │   ├── Electron + React + .NET → reactElectronDotNet
│       │   └── Electron + Vue + .NET → vueElectronDotNet
```

## 规则源与生成结构

规则源集中维护在 `fragments/`，生成到目标项目后形成“根入口 + 子规则 + 任务主表”的使用结构：

```text
fragments/
├── root/AGENTS.md         # 公共根骨架，使用 Profile 变量槽
├── pointers/              # 公共入口指针
├── shared/                # 跨架构共享小节，子规则用 include 指令引用
├── arch/<id>/             # 当前 Profile 的专属规则片段
├── manifest.json          # 模块、Profile、家族和输出配方
└── ...                    # design-system 等能力片段
```

## 使用方式

1. 根据项目技术栈准备目标项目目录。
2. 将 `use-rule.exe` 复制到目标项目根目录并双击启动。
3. 生成器先自动检测目录特征（sln/csproj、package.json、wails.json、tauri、pubspec 等）给出推荐组合：证据充分时直接推荐；证据不足时列出已确认维度、待确认维度与候选组合，走补答流程（已确认项保留，只回答缺失项），不伪造推荐。也可以改走问答选择（回答项目形态、前端/界面、后端、桌面壳等问题）或经典的大类 → Profile 选择。交互终端中菜单为方向键 + 回车选择，选中「0」项即取消或返回上一步；脚本/管道调用时自动退化为编号输入模式。
4. 组合确定后进入 Q5 能力开关：按需勾选文档 / 基础设施 / 设计系统等能力（默认 = 当前组合，不可用能力自动隐藏），关闭的能力不会生成文件、根导航也不会再引用。
5. 开始协作时先读根 `AGENTS.md`，再根据任务命中对应子规则，不默认预读全部文件。
6. 开发过程中通过 `TASK.md` 维护活动任务与最近少量已完成任务（`TASK.md` 由目标项目自己维护，生成器只引用不释放）。

文档维护按业务模块和功能拆分：`doc/requirements/<module>/<feature>.md` 记录业务需求，`doc/design/<module>/<feature>.md` 记录对应技术设计，`doc/index.md` 及各目录索引负责导航。设计文档通过 `REQ-` 编号引用需求，不重复维护需求正文；跨模块方案放在 `doc/design/architecture.md`。

### 一键导出（推荐）

仓库附带一个交互式拼接器，免去手动复制：

```powershell
# 构建（首次）
./app/RuleAssembler/publish.ps1

# Windows 下也可以在仓库根目录双击或执行
./build-use-rule.bat

# 产物位于 dist/use-rule.exe。把它拷到目标项目根目录后双击：
# 启动时自动检测当前项目并推荐组合（含判断依据）；证据不足时展示待确认维度与候选，
# 补答只问缺失项、已确认项保留。也可以选择问答决策树（项目形态 → 前端/界面 → 后端 → 桌面壳 → 细分项，
# 只出现已接入的选项）或经典的「技术大类 → Profile」。
# 组合确定后进入 Q5 能力开关（文档 / 基础设施 / 设计系统等，默认 = 当前组合），
# 随后列出实际生成文件并统一确认后释放（AGENTS + 子规则 + CLAUDE/GEMINI/GROK），同名文件会直接覆盖；
# 例外：已定制（meta.status=已定制）的 design-system.yaml 保留本地版本不覆盖，仍是基线时才刷新。
# 不释放 TASK.md（运行时任务清单，不随规则分发，配方中按 referenced-existing 声明）。
# 生成成功后会在目标项目写入 .ai-rule-profile.json 配方记录（记录本次选择、回答来源与能力开关），
# 并自动把该记录加入目标项目 .gitignore（没有 .gitignore 时按技术栈创建一份默认规则）。
```

当前 EXE 是 Windows x64 框架相关单文件，目标机器需要对应的 .NET 10 运行时。拼接器把公共入口、指针和可复用能力抽成模块，按所选 Profile 填入差异内容后输出；子规则中的共性小节（如提交前最小回归、无法执行测试时）集中维护在 `fragments/shared/`，由各架构片段通过 `<!-- include: ... -->` 指令引用，改一处即可同步全部 Profile。后续只维护 `fragments/`，重新构建即可让所有 Profile 生效（根目录原有的 19 套架构快照目录已于 2026-08-28 删除）。详见 `app/RuleAssembler/` 与 `fragments/`。

## 共同规则

所有模板共享以下基础约束：

- 先理解，再修改；不要基于猜测直接改代码或规则
- 先复用，再新增；避免无关重构和重复体系
- 交付必须真实可用；涉及新增、修改、删除的业务逻辑不能停留在伪交互层面
- 能验证的地方优先验证；无法验证时必须说明原因、影响与建议下一步
- 如果编写注释，统一使用中文
- 单个类、页面、组件、页面基类应保持职责单一，避免持续膨胀
- Windows / PowerShell 场景下禁止使用 `&&` 链式命令
