## [2026-08-07]：electronDotNetExe 同步 KISS 可读性约束与去重精简（版本统一 1.10.0）

跟踪状态表：

| 文件夹名称 | 当前子任务状态 | 备注 |
|-----------|----------------|------|
| `electronDotNetExe` | `已完成` | Electron + .NET sidecar 桌面；任务管理为简化口径，无「任务状态触发条件」冗余小节，故未做该项合并 |

### 修改原因

`electronDotNetExe` 存在交付三节重叠、子规则阅读摘要与根索引重复、主动建议含与根重复的共性，且后端缺 KISS / 反模式 / 自检条款。本次同步 dotnetLayered 治理净效果并统一版本号 1.10.0。

### 主要修改内容

- 根 `AGENTS.md`：「根文件原则」新增 KISS 心法行；合并交付三节为「交付与验证」（Electron 向：引用 frontend/backend/infra，保留 SQLite schema / 打包维度）；agents-root 1.7.0→1.10.0
- `backend-AGENTS.md`：删「阅读摘要」；「主动建议规则」去共性加根指针；新增「核心逻辑可读性(KISS)」「反模式与修根因清单」「可维护性自检」；强化注释规范行内注释条款；backend-agents 1.6.1→1.10.0
- `doc-AGENTS.md`：删「阅读摘要」；「主动建议规则」去共性加根指针；阶段 0 新增「设计自检清单」；doc-agents 1.6.1→1.10.0
- `frontend-AGENTS.md`：删「阅读摘要」；「主动建议规则」去共性留页面/组件职责与 design-system 冲突两条领域特化并加根指针；frontend-agents 1.6.1→1.10.0
- `infra-AGENTS.md`：删「阅读摘要」；「主动建议规则」去共性加根指针；infra-agents 1.5.0→1.10.0

### 适配差异

- **未做「任务状态触发条件」合并**：electronDotNetExe 任务管理为简化口径（4 字段任务记录），状态未展开定义，无重复可合并。
- **KISS / 反模式 / 自检仅加到 `backend-AGENTS.md`**（.NET C# 特有），`frontend-AGENTS.md` 只做精简。
- 反模式清单的 `.Result` / `.Wait()` 对 .NET sidecar 的 stdin/stdout 异步主循环同样适用。

### 修改文件

- `electronDotNetExe/AGENTS.md`（agents-root 1.7.0 → 1.10.0）
- `electronDotNetExe/backend-AGENTS.md`（backend-agents 1.6.1 → 1.10.0）
- `electronDotNetExe/doc-AGENTS.md`（doc-agents 1.6.1 → 1.10.0）
- `electronDotNetExe/frontend-AGENTS.md`（frontend-agents 1.6.1 → 1.10.0）
- `electronDotNetExe/infra-AGENTS.md`（infra-agents 1.5.0 → 1.10.0）

### 当前子任务状态

- `已完成`
