# AI Code Rule

用于沉淀和维护不同技术架构下的 AI 协作开发规则模板。每个一级目录都可以独立复制到目标项目中使用。

## 当前统一标准

`nextFullstack` 目前是仓库内最先完成“轻量入口 + 按需子规则 + 月度归档机制”升级的标准模板，其它模板已按同一口径同步：

- 根 `AGENTS.md` 只保留全局协作规范、阶段门控、任务机制、交付要求与子规则导航
- 高频子规则优先补齐 `阅读摘要`、`常见任务入口`、`提交前最小回归`
- `TASK.md` 只维护活动任务与最近少量 `DONE` 任务
- 较早的 `DONE` 任务统一归档到 `doc/task-archive/YYYY-MM.md`

## 模板目录一览

| 目录 | 项目类型 | 典型子规则 |
| --- | --- | --- |
| [dotnetConsole](dotnetConsole/) | .NET 控制台应用 | `application` / `doc` |
| [dotnetCoreSingle](dotnetCoreSingle/) | .NET 单层 API 后端 | `backend` / `doc` / `infra` |
| [dotnetLayered](dotnetLayered/) | .NET 分层 API 后端 | `backend` / `doc` / `infra` |
| [dotnetBlazor](dotnetBlazor/) | .NET Blazor 全栈 Web | `ui` / `backend` / `doc` / `infra` |
| [vueLinkdotNet](vueLinkdotNet/) | Vue + .NET 前后端分离 | `frontend` / `backend` / `doc` / `infra` |
| [reactLinkdotNet](reactLinkdotNet/) | React + .NET 前后端分离 | `frontend` / `backend` / `doc` / `infra` |
| [nextFullstack](nextFullstack/) | Next.js 全栈 Web 应用 | `frontend` / `backend` / `doc` / `infra` |
| [python](python/) | Python API 后端 | `backend` / `doc` / `infra` |
| [wpfExe](wpfExe/) | WPF Windows 桌面应用 | `ui` / `application` / `infrastructure` / `doc` |
| [avaloniaExe](avaloniaExe/) | Avalonia 跨平台桌面应用 | `ui` / `application` / `infrastructure` / `doc` |
| [vueWailsExe](vueWailsExe/) | Wails 桌面应用（Vue） | `frontend` / `backend` / `doc` / `infra` |
| [reactWailsExe](reactWailsExe/) | Wails 桌面应用（React） | `frontend` / `backend` / `doc` / `infra` |
| [reactTauriExe](reactTauriExe/) | Tauri 桌面应用（React） | `frontend` / `backend` / `doc` / `infra` |
| [staticHtml](staticHtml/) | 纯静态页面 | `frontend` / `doc` |

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
