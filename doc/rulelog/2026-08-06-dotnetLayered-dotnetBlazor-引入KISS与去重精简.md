## [2026-08-06]：dotnetLayered / dotnetBlazor 引入 KISS 可读性约束与去重精简

跟踪状态表：

| 文件夹名称 | 当前子任务状态 | 备注 |
|-----------|----------------|------|
| `dotnetLayered` | `已完成` | 分两轮：引入 KISS（心法/KISS/反模式/自检/注释强化/设计自检）+ 去重精简（交付合并/任务状态合并/删阅读摘要/主动建议去共性） |
| `dotnetBlazor` | `已完成` | 模仿 dotnetLayered 两轮净效果一步到位（含 Blazor 特有 ui-AGENTS.md） |

### 修改原因（通用）

参考 `skills/human-first-kiss-engineering` 工程心法，把可读性 / KISS / 反过度抽象的可落地部分分发进现有规则文件，不新增文件。dotnetLayered 现有规则偏重分层与流程，缺乏把可读性 / KISS 作为可执行硬约束的条款，且存在交付三节重叠、任务状态定义与动作描述重复、阅读摘要与根索引重复、主动建议含与根重复的共性等冗余。先在 dotnetLayered 落地（引入 + 精简两轮），再将净效果同步到 dotnetBlazor。

### 通用修改内容

- 根 `AGENTS.md`：「根文件原则」加 KISS 心法行；合并交付三节为「交付与验证」；合并「任务状态触发条件」定义与动作描述
- `backend-AGENTS.md`：删阅读摘要；主动建议去复用 / 查证共性加根指针；新增「核心逻辑可读性(KISS)」「反模式与修根因清单」「可维护性自检」；强化注释规范行内注释条款（fail fast 原因 / 跳过脏数据 / 保留层结构）
- `doc-AGENTS.md`：删阅读摘要；主动建议去共性加根指针；阶段 0 新增「设计自检清单」（入口 / 流程 / 状态 / 分支 / 副作用 / 失败边界 + 设计阶段禁忌）
- `infra-AGENTS.md`：删阅读摘要；主动建议去共性加根指针
- 共性规则统一指向根「模型行为约束与主动建议」，子规则只留领域特化

### 各模板差异与状态

#### `dotnetLayered`（已完成，分两轮）

**第一轮 — 引入 KISS**：
- 根文件原则加 KISS 心法行
- backend 新增 KISS / 反模式与修根因清单 / 可维护性自检；强化注释规范（初版 KISS 小节含「先读再写」「范围最小扰动」等条目）
- doc 阶段 0 新增设计自检清单
- 版本：agents-root 1.7.0→1.8.0、backend 1.8.1→1.9.0、doc 1.6.0→1.7.0

**第二轮 — 去重精简**：
- 合并交付三节为「交付与验证」；合并任务状态触发条件
- 三子规则删阅读摘要、主动建议去共性加根指针
- 修正第一轮 KISS 小节混入的 2 条与根重复内容（先读再写 / 范围最小扰动），改为根指针
- 版本：agents-root 1.8.0→1.9.0、backend 1.9.0→1.10.0、doc 1.7.0→1.8.0、infra 1.7.0→1.8.0
- 净减约 36 行

#### `dotnetBlazor`（已完成，一步到位）

- 模仿 dotnetLayered 两轮净效果一次性套用，措辞按 Clean Architecture（Application / Domain / Infrastructure）与 Blazor 适配
- 含 Blazor 特有 ui-AGENTS.md：删阅读摘要、主动建议留 Razor / 组件职责与 design-system 冲突两条领域特化
- 版本（当时与 dotnetLayered 对齐）：agents-root 1.7.0→1.9.0、backend 1.7.1→1.10.0、doc 1.5.0→1.8.0、infra 1.6.0→1.8.0、ui 1.6.0→1.7.0

### 修改文件

- `dotnetLayered`：AGENTS、backend、doc、infra
- `dotnetBlazor`：AGENTS、backend、doc、infra、ui

### 当前子任务状态

- 全部 `已完成`

### 备注

- 本记录合并 8/6 原 3 条 rulelog（dotnetLayered 引入 KISS、dotnetLayered 去重精简、dotnetBlazor 同步），按根 `AGENTS.md`「同一主题只保留一条记录」规则。
- 8/7 将本批净效果进一步同步到其余 8 个 .NET 模板，并将全 10 模板版本统一为 1.10.0（dotnetLayered / dotnetBlazor 由 1.8.0 / 1.9.0 等补齐到 1.10.0），见 8/7 合并记录 `2026-08-07-dotnet全模板-同步KISS去重精简与版本统一.md`。
- 参考来源：`skills/human-first-kiss-engineering`。
