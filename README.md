# ai-code-rule

沉淀和维护不同技术架构下的 AI 协作开发规则模板：`fragments/` 是唯一维护源，生成器 `app/RuleAssembler/` 按维度组合把片段拼装为各技术栈的 `AGENTS.md` 与子规则文件，并全量打包内置 AI Skills。

## 项目简介

目标是给不同形态的项目（Web、桌面、移动、控制台、包仓库）生成一套「轻量入口 + 按需子规则」的 AI 协作规则：根 `AGENTS.md` 只保留硬约束与导航，领域细则放子规则，按任务命中阅读。规则在目标项目使用中发现问题可回传本仓库（`.ai-rule-feedback.md`），形成反馈闭环。

## 技术栈

- .NET 10（C#）控制台应用：规则生成器与内容打包
- xUnit：回归测试（产物 emit 基线守卫 214+ 条目）
- GitHub Actions：推送 master 自动发布 exe

## 支持的组合家族

| 家族 | 覆盖 |
| --- | --- |
| web-fullstack | 前后端分离 / 全栈（Vue、React、Blazor、Next.js、Nuxt，配 .NET / Python 后端） |
| dotnet-backend | .NET 控制台与 API 后端（单层 / 分层） |
| dotnet-package | .NET NuGet 包仓库（多包工程 + 逐包测试 + CI 发布链路） |
| python-backend | Python API 后端（FastAPI） |
| desktop | Windows / 跨平台桌面（WPF、Avalonia） |
| hybrid-app | 混合桌面（Electron、Tauri、Wails 配 Web 前端） |
| mobile | 移动客户端（.NET MAUI、Flutter） |
| frontend-static | 纯前端 SPA 与静态站点 |
| general | 技术栈未定的通用项目协作模板 |

共 37 个兼容组合。完整清单、维度约束与适用场景见 `fragments/manifest.json` 的 `compatibilityCases`，或直接运行 exe 的「检测当前目录组合」由证据推荐。

## 项目结构

```text
fragments/            # 唯一维护源：root 骨架、shared 共享小节、arch 架构片段、manifest.json
app/RuleAssembler/    # 生成器 exe（组合内核、交互问答、Skills 导入、更新检查）
skills/               # 内置 AI Skills 全量源（53 个，随 exe 发布）
test/                 # xUnit 回归 + emit 基线守卫
doc/                  # 反馈承接、规则退役、归属处理等仓库级文档
```

## 环境要求

- .NET SDK 10（构建；发布产物为 win-x64 框架依赖单文件，目标机自装 .NET 10 运行时）
- Windows（交互式生成与 Skills 导入的目录约定按 Windows 验证）

## 快速开始

目标项目使用（推荐，无需本仓库）：

1. 从 GitHub Release 下载 `use-ai-rule.exe`（或本地构建：`./app/RuleAssembler/publish.ps1`，产物在 `dist/`）。
2. 复制到目标项目根目录并运行，先选模式：生成规则文件（含可选 Skills 导入）、检测当前目录组合（仅查看）、仅导入 Skills。
3. 生成模式：exe 自动检测目录特征推荐组合，证据不足时走补答 / 问答决策树；组合确定后勾选能力开关（文档 / 基础设施 / 设计系统）与 Skills，统一确认后落盘。
4. 生成后目标项目根出现 `AGENTS.md` 与子规则（部分组合含 `design-system.yaml`）、配方记录 `.ai-rule-profile.json`（供 `--recipe` / `--upgrade` 重放）。
5. 升级：`use-ai-rule.exe --recipe .ai-rule-profile.json` 按记录重放；`--upgrade` 原地升级（本地修改默认保留，`--force` 全覆盖）。

本仓库开发：

```powershell
dotnet test                                    # 全量回归（含 emit 基线）
dotnet run --project app/RuleAssembler -- --check   # 校验 37 个兼容样例
dotnet run --project app/RuleAssembler -- --audit  # 规则源体检：不可达 / 重复 / 冲突候选 / Skill 引用 / 上下文体积报告
```

## 使用说明

- 协作入口：AI 在目标项目先读根 `AGENTS.md`，再按任务命中对应子规则，不默认预读全部文件。
- 改规则内容（fragments / skills）或生成器代码后必须升版：版本唯一事实源是 `fragments/manifest.json` 的 `vars.version`，推送 master 后 CI 自动发布并校验版本严格递增；规则内容变更还需刷新 emit 基线（`UPDATE_EMIT_BASELINE=1 dotnet test --filter EmitBaseline`）。
- 规则效果反馈：目标项目把命中 / 违反情况记入 `.ai-rule-feedback.md`，按 `doc/规则效果反馈承接.md` 的口径回传处理。

## 相关文档

- `AGENTS.md` — 本仓库维护规则与约束
- `app/RuleAssembler/README.md` — 生成器架构与维护说明
- `fragments/manifest.json` — 维度组合内核（组件 / 约束 / 兼容样例 / 技能登记）
- `doc/规则效果反馈承接.md` — 反馈承接流程
- `doc/规则退役清单.md`、`doc/规则技能归属处理清单.md` — 规则治理
