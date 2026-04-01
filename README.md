# AI Code Rule

为不同技术栈的项目提供统一的 AI 代理开发规范。每个目录对应一种项目类型，包含完整的规则文件、设计系统和任务管理机制。

## 规则目录一览

| 目录 | 项目类型 | 技术栈 |
|------|----------|--------|
| [dotnetCore](dotnetCore/) | .NET 单层 API 后端 | .NET 10 + ASP.NET Core Web API + EF Core + Swagger |
| [dotnetLayered](dotnetLayered/) | .NET 分层 API 后端 | .NET 10 + ASP.NET Core Web API（9 项目分层架构）+ EF Core |
| [dotnetBlazor](dotnetBlazor/) | .NET Blazor 全栈 Web | .NET 10 + Blazor Server + BootstrapBlazor (Bootstrap 5) |
| [frontBackend](frontBackend/) | 前后端分离 Web 应用 | Vue 3 + Element Plus + TypeScript + .NET Web API |
| [vueLinkdotNet](vueLinkdotNet/) | Vue + .NET 前后端分离 | Vue 3 + Element Plus + TypeScript + .NET Web API（Clean Architecture） |
| [reactLinkdotNet](reactLinkdotNet/) | React + .NET 前后端分离 | React 18 + Ant Design 5 + TypeScript + Zustand + .NET Web API（Clean Architecture） |
| [nextFullstack](nextFullstack/) | Next.js 全栈 Web 应用 | Next.js (App Router) + React + TypeScript + Tailwind CSS + shadcn/ui |
| [python](python/) | Python API 后端 | Python 3.12+ + FastAPI + Pydantic V2 + SQLAlchemy 2.0 (async) |
| [wpfExe](wpfExe/) | WPF Windows 桌面应用 | .NET 10 + WPF + WPF UI + CommunityToolkit.Mvvm + Dapper/SQLite |
| [avaloniaExe](avaloniaExe/) | Avalonia 跨平台桌面应用 | .NET 10 + Avalonia 11 + Ursa.Avalonia + CommunityToolkit.Mvvm + Dapper/SQLite |
| [wailsExe](wailsExe/) | Wails 桌面应用 | Vue 3 + Element Plus + Go/Wails |
| [staticHtml](staticHtml/) | 纯静态页面 | HTML5 + Bootstrap 5 + Chart.js + Vanilla JS (ES6+) |

## 文件结构说明

每个规则目录遵循统一的文件组织模式：

```
<规则目录>/
├── AGENTS.md            # 核心规则文档（所有 AI 代理的唯一规则来源）
├── CLAUDE.md            # Claude 代理指针文件（内容：AGENTS.md）
├── GEMINI.md            # Gemini 代理指针文件（内容：AGENTS.md）
├── TASK.md              # 任务追踪文件（记录任务内容和进度）
└── design-system.yaml   # 设计系统文件（视觉 Token 与 UI 规范，部分目录包含）
```

### 各文件职责

- **AGENTS.md** — 定义技术栈、项目结构、开发规则、测试规范、Git 提交规范、Docker 规范、任务管理机制、禁止事项等。是 AI 代理工作的唯一规则来源。
- **CLAUDE.md / GEMINI.md** — 指针文件，内容为 `AGENTS.md`，确保不同 AI 工具读取同一份规则。
- **TASK.md** — 记录具体任务内容和进度，与规则文档分离。任务状态包括：`TODO`、`DOING`、`BLOCKED`、`REVIEW`、`DONE`。
- **design-system.yaml** — 定义颜色、排版、间距、组件规范等视觉 Token，是 UI 开发的唯一设计依据（部分目录包含此文件）。

## 使用方式

1. 根据项目技术栈选择对应的规则目录
2. 将规则目录中的文件复制到项目根目录
3. AI 代理启动后会自动读取 `CLAUDE.md` / `GEMINI.md`，进而加载 `AGENTS.md` 中的规则
4. 开发过程中通过 `TASK.md` 追踪任务进度

## 共同规则

所有规则目录共享以下核心原则：

- **先理解，再修改** — 不盲目动手，先理解业务逻辑和现有代码
- **先复用，再新增** — 优先复用已有组件和模式，避免重复创建
- **交付必须可直接使用** — 不能只停留在演示层，所有交互必须真实生效
- **不做无关重构** — 不做与当前任务无关的代码改动
- **每次改动后必须提交** — 通过 Git 追踪所有变更
