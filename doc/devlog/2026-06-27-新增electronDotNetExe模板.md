# 2026-06-27 新增 electronDotNetExe 模板

## 修改范围
- 新建 `electronDotNetExe/` 模板目录（9 个规则文件，不保留 doc/devlog 目录）
- 更新 `README.md`、`doc/AI-Rules-Audit-Log.md`

## 大概变更内容
新增 Electron + .NET 桌面应用 AI 协作规则模板。技术栈：React + TS + Vite 前端、.NET 8/9 自包含部署后端（不用 AOT）、Electron 主进程通过 `child_process.spawn` 拉起 .NET 子进程、子进程 stdio JSON-RPC 进行 IPC、轻量分层 IPC Handler → Service → Repository → SQLite。

模板内含 9 个标准规则文件：AGENTS.md、frontend-AGENTS.md、backend-AGENTS.md、infra-AGENTS.md、doc-AGENTS.md、design-system.yaml、CLAUDE.md、GEMINI.md、TASK.md。不保留 `doc/devlog/`、`doc/task-archive/` 目录占位，与已简化的 nextFullstack、staticHtml 等模板口径一致。

根 AGENTS.md 基于 nextFullstack 最新简化口径（4 字段任务），子规则基于 reactTauriExe（IPC 桌面范本）改写。backend 新增了 sidecar 架构特有的"进程生命周期规则"。

## 详细记录
详见 `doc/rulelog/2026-06-27-新增electronDotNetExe模板.md`。
