## [2026-08-07]：dotnetCoreSingle 同步 KISS 可读性约束与去重精简（版本统一 1.10.0）

跟踪状态表：

| 文件夹名称 | 当前子任务状态 | 备注 |
|-----------|----------------|------|
| `dotnetCoreSingle` | `已完成` | 同步 dotnetLayered 净效果；按用户要求版本号统一 1.10.0（backend 由 2.0.1 调整为 1.10.0） |

### 修改原因

`dotnetCoreSingle` 停留在 2026-07-26，结构与 `dotnetLayered` 旧版同构：缺 KISS / 设计自检 / 反模式清单，存在交付三节重叠、任务状态定义与动作描述重复、子规则阅读摘要与根索引重复、主动建议含与根重复的共性。本次同步 dotnetLayered 治理净效果，并按用户决策将版本号统一为 1.10.0。

### 主要修改内容

- 根 `AGENTS.md`：「根文件原则」新增 KISS 心法行；合并交付三节为「交付与验证」；合并「任务状态触发条件」；agents-root 1.7.0→1.10.0
- `backend-AGENTS.md`：删「阅读摘要」；「主动建议规则」去复用 / 查证共性加根指针；新增「核心逻辑可读性(KISS)」「反模式与修根因清单」「可维护性自检」；强化注释规范行内注释条款；backend-agents 2.0.1→1.10.0
- `doc-AGENTS.md`：删「阅读摘要」；「主动建议规则」去共性加根指针；阶段 0 新增「设计自检清单」；doc-agents 1.5.0→1.10.0
- `infra-AGENTS.md`：删「阅读摘要」；「主动建议规则」去共性加根指针；infra-agents 1.6.0→1.10.0
- 共性规则统一指向根「模型行为约束与主动建议」，子规则只留领域特化

### 修改文件

- `dotnetCoreSingle/AGENTS.md`（agents-root 1.7.0 → 1.10.0）
- `dotnetCoreSingle/backend-AGENTS.md`（backend-agents 2.0.1 → 1.10.0）
- `dotnetCoreSingle/doc-AGENTS.md`（doc-agents 1.5.0 → 1.10.0）
- `dotnetCoreSingle/infra-AGENTS.md`（infra-agents 1.6.0 → 1.10.0）

### 当前子任务状态

- `已完成`

### 备注

- 版本号按用户要求全模板统一 1.10.0；dotnetCoreSingle 的 backend 由 2.0.1 下调至 1.10.0，系统一基线决策，非语义化版本常规演进。
- 与 dotnetLayered / dotnetBlazor 8 月治理为同主题跨模板同步。
