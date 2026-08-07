## [2026-08-06]：dotnetBlazor 同步 KISS 可读性约束与去重精简（模仿 dotnetLayered）

跟踪状态表：

| 文件夹名称 | 当前子任务状态 | 备注 |
|-----------|----------------|------|
| `dotnetBlazor` | `已完成` | 把 dotnetLayered 两轮改动的净效果一步套用到 dotnetBlazor（含 Blazor 特有 ui-AGENTS.md），措辞按 Clean Architecture 与 Blazor 适配 |

### 修改原因

`dotnetBlazor` 规则停留在 2026-07-26，结构与 `dotnetLayered` 旧版同构：同样缺 KISS / 设计自检 / 反模式清单，同样存在交付三节重叠、任务状态定义与动作描述重复、四子规则阅读摘要与根索引重复、主动建议含与根重复的共性。参照 `dotnetLayered` 8 月 6 日的「引入 KISS」与「去重精简」两次治理，把净效果一次性同步过来。

### 主要修改内容

- 根 `AGENTS.md`：「根文件原则」新增 KISS 心法行；合并交付三节为「交付与验证」；合并「任务状态触发条件」定义与动作描述；agents-root 1.7.0→1.9.0
- `backend-AGENTS.md`：删「阅读摘要」；「主动建议规则」去复用 / 查证共性加根指针；新增「核心逻辑可读性(KISS)」「反模式与修根因清单」「可维护性自检」；强化注释规范行内注释条款；backend-agents 1.7.1→1.10.0
- `doc-AGENTS.md`：删「阅读摘要」；「主动建议规则」去共性加根指针；阶段 0 新增「设计自检清单」（入口/流程/状态/分支/副作用/失败边界）；doc-agents 1.5.0→1.8.0
- `infra-AGENTS.md`：删「阅读摘要」；「主动建议规则」去共性加根指针；infra-agents 1.6.0→1.8.0
- `ui-AGENTS.md`：删「阅读摘要」；「主动建议规则」去复用 / 查证共性加根指针，保留 Razor / 组件职责与 design-system 冲突两条领域特化；ui-agents 1.6.0→1.7.0
- 共性规则统一指向根「模型行为约束与主动建议」，子规则只留领域特化

### 修改文件

- `dotnetBlazor/AGENTS.md`（agents-root 1.7.0 → 1.9.0，与 dotnetLayered 对齐）
- `dotnetBlazor/backend-AGENTS.md`（backend-agents 1.7.1 → 1.10.0，与 dotnetLayered 对齐）
- `dotnetBlazor/doc-AGENTS.md`（doc-agents 1.5.0 → 1.8.0，与 dotnetLayered 对齐）
- `dotnetBlazor/infra-AGENTS.md`（infra-agents 1.6.0 → 1.8.0，与 dotnetLayered 对齐）
- `dotnetBlazor/ui-AGENTS.md`（ui-agents 1.6.0 → 1.7.0，dotnetLayered 无 ui 对应）

### 当前子任务状态

- `已完成`

### 备注

- 与 dotnetLayered 8 月 6 日两次治理为同主题跨模板同步；本次按其净效果一步到位，未分两轮。
- 版本号按用户要求与 dotnetLayered 对齐（agents-root / backend / doc / infra 同号）；ui-AGENTS.md 在 dotnetLayered 无对应子规则，保持 1.7.0。
- ui-AGENTS.md 为 Blazor 特有子规则，一并按同逻辑处理（删阅读摘要 + 主动建议留领域特化）。
