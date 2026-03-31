# AI Code Rule 仓库说明

本仓库定义了 AI Agent 协作开发医疗行业管理系统的统一规范、流程与技术栈。

---

## 目录结构

| 目录 | 技术栈 | 说明 |
|------|--------|------|
| `frontBackend/` | Vue 3 + .NET 10 + PostgreSQL | 前后端分离方案 |
| `nextFullstack/` | Next.js + Prisma + PostgreSQL | Next.js 全栈方案 |
| `staticHtml/` | HTML + Bootstrap 5 + Vanilla JS | 纯 HTML 静态页面方案 |
| `avaloniaExe/` | Avalonia UI | 桌面应用方案 |
| `wailsExe/` | Wails | 桌面应用方案 |

---

## 文档结构

各目录下均包含以下文件：

| 文件 | 说明 |
|------|------|
| `AGENTS.md` / `CLAUDE.md` / `GEMINI.md` | 各 AI Agent 统一工作规则（内容同步） |
| `design-system.yaml` | 设计 token 与 UI 规范 |
| `TASK.md` | 具体任务内容与进度 |
