## [2026-08-07]：dotnetConsole 同步 KISS 可读性约束与去重精简（版本统一 1.10.0）

跟踪状态表：

| 文件夹名称 | 当前子任务状态 | 备注 |
|-----------|----------------|------|
| `dotnetConsole` | `已完成` | 控制台模板适配：doc 无阶段 0 设计文档章节，未加设计自检清单；KISS/反模式/自检加在 application-AGENTS.md（Command/Service 语境） |

### 修改原因

`dotnetConsole` 停留在 2026-07-26，存在交付三节重叠、任务状态定义与动作描述重复、子规则阅读摘要与根索引重复、主动建议含与根重复的共性，且缺 KISS / 反模式 / 自检条款。本次同步 dotnetLayered 治理净效果并按用户决策统一版本号 1.10.0。

### 主要修改内容

- 根 `AGENTS.md`：「根文件原则」新增 KISS 心法行；合并交付三节为「交付与验证」（控制台向：命令级 smoke test、引用 application-AGENTS.md）；合并「任务状态触发条件」；agents-root 1.7.0→1.10.0
- `application-AGENTS.md`：删「阅读摘要」；「主动建议规则」去复用 / 查证共性加根指针；新增「核心逻辑可读性(KISS)」「反模式与修根因清单」「可维护性自检」；强化注释规范行内注释条款（"为什么保留某一步骤"）；application-agents 1.6.0→1.10.0
- `doc-AGENTS.md`：删「阅读摘要」；「主动建议规则」去共性加根指针；doc-agents 1.6.0→1.10.0
- 共性规则统一指向根「模型行为约束与主动建议」，子规则只留领域特化

### 适配差异

- `doc-AGENTS.md` 无「阶段 0 设计文档」章节（控制台模板仅维护需求 / 文档目录），故**未引入「设计自检清单」**；该清单依赖四部分设计文档结构，控制台模板不适用。
- KISS / 反模式 / 自检条款放在 `application-AGENTS.md`（该模板的核心实现规则所在），反模式清单的 `.Result` / `.Wait()` 对控制台异步命令同样适用。

### 修改文件

- `dotnetConsole/AGENTS.md`（agents-root 1.7.0 → 1.10.0）
- `dotnetConsole/application-AGENTS.md`（application-agents 1.6.0 → 1.10.0）
- `dotnetConsole/doc-AGENTS.md`（doc-agents 1.6.0 → 1.10.0）

### 当前子任务状态

- `已完成`
