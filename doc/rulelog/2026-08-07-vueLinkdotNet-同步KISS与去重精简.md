## [2026-08-07]：vueLinkdotNet 同步 KISS 可读性约束与去重精简（版本统一 1.10.0）

跟踪状态表：

| 文件夹名称 | 当前子任务状态 | 备注 |
|-----------|----------------|------|
| `vueLinkdotNet` | `已完成` | Vue + .NET 前后端分离；KISS/反模式/自检仅加 backend（.NET），frontend 只做精简 |

### 修改原因

`vueLinkdotNet` 停留在 2026-07-26，存在交付三节重叠、任务状态定义与动作描述重复、子规则阅读摘要与根索引重复、主动建议含与根重复的共性，且后端缺 KISS / 反模式 / 自检条款。本次同步 dotnetLayered 治理净效果并统一版本号 1.10.0。

### 主要修改内容

- 根 `AGENTS.md`：「根文件原则」新增 KISS 心法行；合并交付三节为「交付与验证」（前后端分离向：引用 frontend/backend/infra）；合并「任务状态触发条件」；agents-root 1.7.0→1.10.0
- `backend-AGENTS.md`：删「阅读摘要」；「主动建议规则」去共性加根指针；新增「核心逻辑可读性(KISS)」「反模式与修根因清单」「可维护性自检」；强化注释规范；backend-agents 1.6.1→1.10.0
- `doc-AGENTS.md`：删「阅读摘要」；「主动建议规则」去共性加根指针；阶段 0 新增「设计自检清单」；doc-agents 1.5.0→1.10.0
- `frontend-AGENTS.md`：删「阅读摘要」；「主动建议规则」去共性留页面/组件职责与 design-system 冲突两条领域特化；frontend-agents 1.5.0→1.10.0
- `infra-AGENTS.md`：删「阅读摘要」；「主动建议规则」去共性加根指针；infra-agents 1.5.0→1.10.0

### 适配差异

- **KISS / 反模式 / 自检仅加到 `backend-AGENTS.md`**：反模式清单（空 catch、`!`、`.Result` / `.Wait()`、`#pragma`）是 .NET C# 特有，前端 Vue/TS 不适用，故 `frontend-AGENTS.md` 只做精简（删阅读摘要 + 主动建议去共性），不引入这些条款。

### 修改文件

- `vueLinkdotNet/AGENTS.md`（agents-root 1.7.0 → 1.10.0）
- `vueLinkdotNet/backend-AGENTS.md`（backend-agents 1.6.1 → 1.10.0）
- `vueLinkdotNet/doc-AGENTS.md`（doc-agents 1.5.0 → 1.10.0）
- `vueLinkdotNet/frontend-AGENTS.md`（frontend-agents 1.5.0 → 1.10.0）
- `vueLinkdotNet/infra-AGENTS.md`（infra-agents 1.5.0 → 1.10.0）

### 当前子任务状态

- `已完成`
