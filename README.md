# AI Code Rule

用于沉淀和维护不同技术架构下的 AI 协作开发规则模板。每个一级目录都可以独立复制到目标项目中使用。

## 当前统一标准

`nextFullstack` 目前是仓库内最先完成”轻量入口 + 按需子规则 + 月度归档机制”升级的标准模板，其它模板已按同一口径同步：

- 根 `AGENTS.md` 只保留全局协作规范、阶段门控、任务机制、交付要求与子规则导航
- 高频子规则优先补齐 `阅读摘要`、`常见任务入口`、`提交前最小回归`
- `TASK.md` 只维护活动任务与最近少量 `DONE` 任务
- 较早的 `DONE` 任务统一归档到 `doc/task-archive/YYYY-MM.md`

## 模板目录一览

| 目录 | 项目类型 | 典型子规则 | 使用场景 |
| --- | --- | --- | --- |
| [dotnetConsole](dotnetConsole/) | .NET 控制台应用 | `application` / `doc` | CLI 工具、批处理服务、定时任务、数据迁移脚本、后台守护进程、交互式终端应用 |
| [dotnetCoreSingle](dotnetCoreSingle/) | .NET 单层 API 后端 | `backend` / `doc` / `infra` | 中小型 REST API、微服务中的单一职责服务、内部工具 API、MVP 快速验证、Serverless 本地开发模拟 |
| [dotnetLayered](dotnetLayered/) | .NET 分层 API 后端 | `backend` / `doc` / `infra` | 中大型业务系统（ERP/CRM/OA）、DDD 项目、高单元测试覆盖率系统、多人协作长期维护的核心业务 |
| [dotnetBlazor](dotnetBlazor/) | .NET Blazor 全栈 Web | `ui` / `backend` / `doc` / `infra` | 企业内部管理系统、.NET 团队不想引入 JS 框架、实时交互仪表盘、已有 .NET 后端扩展 Web 界面 |
| [vueLinkdotNet](vueLinkdotNet/) | Vue + .NET 前后端分离 | `frontend` / `backend` / `doc` / `infra` | Vue 3 企业级 SPA、前后端团队独立开发、数据可视化运营后台、已有 .NET 后端重建 Vue 前端 |
| [reactLinkdotNet](reactLinkdotNet/) | React + .NET 前后端分离 | `frontend` / `backend` / `doc` / `infra` | React 18 企业应用、复杂表单/审批流系统、前后端团队独立开发、已有 .NET 后端选用 React 前端 |
| [nextFullstack](nextFullstack/) | Next.js 全栈 Web 应用 | `frontend` / `backend` / `doc` / `infra` | SaaS 平台、电商网站、SEO 敏感的公众网站、创业项目快速迭代、全栈一人搞定（仓库参考标准模板） |
| [generalProject](generalProject/) | 通用项目协作模板 | `frontend` / `backend` / `doc` / `infra` | 尚未最终确定技术栈的新项目、需要先统一 AI 协作口径再细化实现方案的项目、希望尽量少预设框架约束的通用业务项目 |
| [python](python/) | Python API 后端 | `backend` / `doc` / `infra` | AI/ML 模型推理服务、数据处理 API、与 .NET 配合的 Python 微服务、自动化运维接口、快速原型 API |
| [wpfExe](wpfExe/) | WPF Windows 桌面应用 | `ui` / `application` / `infrastructure` / `doc` | 工业控制/医疗/金融等 Windows 桌面终端、WinForms 迁移现代化、离线优先本地工具、硬件设备交互 |
| [avaloniaExe](avaloniaExe/) | Avalonia 跨平台桌面应用 | `ui` / `application` / `infrastructure` / `doc` | 跨 Windows/macOS/Linux 的桌面工具、Linux 运维工具、WPF 跨平台迁移、嵌入式设备界面 |
| [mauiMobile](mauiMobile/) | .NET MAUI 移动端应用 | `ui` / `application` / `infrastructure` / `doc` | 跨 Android/iOS 的移动应用、企业移动办公、数据采集与巡检、已有 .NET 后端扩展移动端、轻量级消费者 App |
| [vueWailsExe](vueWailsExe/) | Wails 桌面应用（Vue + Go） | `frontend` / `backend` / `doc` / `infra` | 轻量桌面工具（替代 Electron）、Go 服务加桌面界面、需原生系统能力的 Vue 应用、本地数据管理 |
| [reactWailsExe](reactWailsExe/) | Wails 桌面应用（React + Go） | `frontend` / `backend` / `doc` / `infra` | 同 vueWailsExe 但偏好 React 生态、已有 React 组件复用到桌面、Go CLI 工具加图形界面 |
| [reactTauriExe](reactTauriExe/) | Tauri 桌面应用（React + Rust） | `frontend` / `backend` / `doc` / `infra` | 极致轻量桌面应用（几 MB 安装包）、安全敏感工具、Rust 高性能需求、嵌入式/IoT 配套工具 |
| [electronDotNetExe](electronDotNetExe/) | Electron + .NET 桌面应用 | `frontend` / `backend` / `doc` / `infra` | Electron 桌面应用配 .NET 重度业务后端、复用已有 .NET 代码与生态、需要 Node 生态 + .NET 运行时、sidecar 子进程 IPC 架构 |
| [staticHtml](staticHtml/) | 纯静态页面 | `frontend` / `doc` | 营销落地页、企业官网、静态文档站、HTML 邮件模板、微信 H5 活动页、设计稿交互原型 |

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
│       │   └── Electron + .NET → electronDotNetExe
│       └── 无框架（纯静态）→ staticHtml
```

## 统一文件结构

每个模板目录都遵循“根入口 + 子规则 + 任务主表 + 月度归档”的组织方式：

```text
<模板目录>/
├── AGENTS.md            # 轻量入口：全局规则、阶段门控、任务机制、子规则导航
├── *-AGENTS.md          # 按作用域拆分的子规则文件，如 frontend / backend / doc / infra / ui / application
├── CLAUDE.md            # 指针文件：只提醒先读 AGENTS.md
├── GEMINI.md            # 指针文件：只提醒先读 AGENTS.md
├── TASK.md              # 活动任务主表，仅保留当前任务与最近少量 DONE
├── doc/
│   ├── devlog/          # 每次规则或开发改动后的简短记录
│   └── task-archive/    # 按月归档较早的 DONE 任务
└── design-system.yaml   # 设计系统文件，按模板需要存在
```

## 使用方式

1. 根据项目技术栈选择对应模板目录。
2. 将该目录中的规则文件复制到目标项目根目录。
3. 开始协作时先读根 `AGENTS.md`，再根据任务命中对应子规则，不默认预读全部文件。
4. 开发过程中通过 `TASK.md` 维护活动任务，通过 `doc/task-archive/` 按月归档较早的 `DONE` 任务。

## 共同规则

所有模板共享以下基础约束：

- 先理解，再修改；不要基于猜测直接改代码或规则
- 先复用，再新增；避免无关重构和重复体系
- 交付必须真实可用；涉及新增、修改、删除的业务逻辑不能停留在伪交互层面
- 能验证的地方优先验证；无法验证时必须说明原因、影响与建议下一步
- 如果编写注释，统一使用中文
- 单个类、页面、组件、页面基类应保持职责单一，避免持续膨胀
- Windows / PowerShell 场景下禁止使用 `&&` 链式命令
- 修改规则模板后，必须在根目录 `doc/devlog/` 下新增一份简短变更记录
