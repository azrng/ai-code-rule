## [2026-08-07]：reactLinkdotNet 同步 KISS 可读性约束与去重精简（版本统一 1.10.0）

跟踪状态表：

| 文件夹名称 | 当前子任务状态 | 备注 |
|-----------|----------------|------|
| `reactLinkdotNet` | `已完成` | React + .NET 前后端分离；任务管理为简化口径，无「任务状态触发条件」冗余小节，故未做该项合并 |

### 修改原因

`reactLinkdotNet` 停留在 2026-07-26，存在交付三节重叠、子规则阅读摘要与根索引重复、主动建议含与根重复的共性，且后端缺 KISS / 反模式 / 自检条款。本次同步 dotnetLayered 治理净效果并统一版本号 1.10.0。

### 主要修改内容

- 根 `AGENTS.md`：「根文件原则」新增 KISS 心法行；合并交付三节为「交付与验证」（前后端分离向）；agents-root 1.6.0→1.10.0
- `backend-AGENTS.md`：删「阅读摘要」；「主动建议规则」去共性加根指针；新增「核心逻辑可读性(KISS)」「反模式与修根因清单」「可维护性自检」；强化注释规范；backend-agents 1.6.1→1.10.0
- `doc-AGENTS.md`：删「阅读摘要」；「主动建议规则」去共性加根指针；阶段 0 新增「设计自检清单」；doc-agents 1.5.0→1.10.0
- `frontend-AGENTS.md`：删「阅读摘要」；「主动建议规则」删复用共性、留页面/组件职责、design-system 冲突、表单校验/权限/状态缺失三条领域特化并加根指针；frontend-agents 1.5.0→1.10.0
- `infra-AGENTS.md`：删「阅读摘要」；「主动建议规则」去共性加根指针；infra-agents 1.5.0→1.10.0

### 适配差异

- **未做「任务状态触发条件」合并**：reactLinkdotNet 任务管理为简化口径，状态以一行「任务状态仅使用：…」带过，未展开状态定义，不存在"定义+动作描述"重复，故无该项可合并。
- **KISS / 反模式 / 自检仅加到 `backend-AGENTS.md`**（.NET C# 特有），`frontend-AGENTS.md` 只做精简。

### 修改文件

- `reactLinkdotNet/AGENTS.md`（agents-root 1.6.0 → 1.10.0）
- `reactLinkdotNet/backend-AGENTS.md`（backend-agents 1.6.1 → 1.10.0）
- `reactLinkdotNet/doc-AGENTS.md`（doc-agents 1.5.0 → 1.10.0）
- `reactLinkdotNet/frontend-AGENTS.md`（frontend-agents 1.5.0 → 1.10.0）
- `reactLinkdotNet/infra-AGENTS.md`（infra-agents 1.5.0 → 1.10.0）

### 当前子任务状态

- `已完成`
