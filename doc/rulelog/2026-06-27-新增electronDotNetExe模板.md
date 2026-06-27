## [2026-06-27]：新增 electronDotNetExe 模板

跟踪状态表：

| 文件夹名称 | 当前子任务状态 | 备注 |
|-----------|----------------|------|
| `electronDotNetExe` | `已完成` | 新建模板，含 9 个模板内规则文件 + 仓库级 README/审计日志/rulelog 同步 |

### 修改原因
仓库缺少 Electron + .NET 桌面应用架构模板。该组合适用于：Electron 桌面前端 + .NET 重度业务后端的场景，通过子进程 stdio JSON-RPC 进行 IPC 通信，.NET 采用自包含部署（不用 NativeAOT）。参考现有 IPC 桌面模板（reactTauriExe / vueWailsExe）与最新简化口径标准模板（nextFullstack）创建。

### 主要技术决策
- **目录命名**：`electronDotNetExe`（对齐现有 Exe 命名规律）
- **前端**：React + TS + Vite（复用 reactTauriExe/frontend 骨架）
- **后端**：.NET 8/9 自包含部署（self-contained，不用 NativeAOT）
- **IPC 通道**：Electron 主进程用 `child_process.spawn` 拉起 .NET 子进程，通过 stdin/stdout 传 JSON-RPC；渲染进程通过 preload `contextBridge` 暴露的 `window.ipc` 调用主进程
- **分层**：轻量分层 IPC Handler → Service → Repository → SQLite
- **子规则命名口径**：frontend / backend / doc / infra（与其它 Web 技术驱动桌面模板一致）

### 主要修改内容

1. **新建 `electronDotNetExe/` 模板目录**，含 9 个标准规则文件：
   - `AGENTS.md`（根入口，version 1.5.0）：基于 nextFullstack 简化口径骨架（4 字段任务、有"上下文加载规则"标题、无多余"任务状态触发条件"段），技术栈段落改写为 Electron + .NET
   - `backend-AGENTS.md`（version 1.3.1）：基于 reactTauriExe 改写，含 sidecar 架构特有的"进程生命周期规则"（子进程拉起/健康检查/异常重启/优雅关闭、日志走 stderr 不污染 stdout 协议通道）
   - `frontend-AGENTS.md`（version 1.3.1）：Tauri `invoke` → `window.ipc`（contextBridge）；`lib/tauri/` → `lib/ipc/`；补充 preload 与 main/ 目录结构
   - `infra-AGENTS.md`（version 1.3.1）：构建链路 `vite build` + `dotnet publish --self-contained`（不加 PublishAot）+ `electron-builder`；保留签名/更新章节
   - `doc-AGENTS.md`（version 1.5.0）：技术栈措辞改写（Tauri/Rust 边界 → Electron/.NET 边界，Tauri Command → IPC handler）
   - `design-system.yaml`：`local_bridge` 改为 `Electron IPC + .NET sidecar (stdio JSON-RPC)`，do/dont 改写
   - `CLAUDE.md` / `GEMINI.md`（version 1.2.0）：入口指针文件
   - `TASK.md`：初始模板形态（空表）

   > 不保留 `doc/devlog/`、`doc/task-archive/` 目录占位，与已简化的 nextFullstack、staticHtml、flutterMobile 等模板口径一致（devlog 与任务归档机制已在 2026-06-10 简化批次中移除）。

2. **更新 `README.md`**：
   - "模板目录一览"表格加一行 electronDotNetExe
   - "模板选择决策指南"决策树"桌面应用→跨平台（Web 技术驱动）"分支加一条 `Electron + .NET → electronDotNetExe`

3. **更新 `doc/AI-Rules-Audit-Log.md`**：
   - "规则文件夹清单"加一行 `- electronDotNetExe`
   - "规则变更记录索引"加一条链接

### 版本号基线
- 根 `AGENTS.md` / `doc-AGENTS.md`：`1.5.0`（对齐仓库最新标准）
- `frontend` / `backend` / `infra` / `CLAUDE` / `GEMINI`：`1.3.1`（对齐现有 IPC 桌面模板）

### 关键范本来源
- 根 AGENTS.md 骨架 → `nextFullstack/AGENTS.md`（最新简化口径）
- frontend / backend / infra / doc / design-system → `reactTauriExe/`（IPC 桌面应用最佳范本）

### 当前子任务状态
- `已完成`
