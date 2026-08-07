## [2026-08-07]：mauiMobile 同步 KISS 可读性约束与去重精简（版本统一 1.10.0）

跟踪状态表：

| 文件夹名称 | 当前子任务状态 | 备注 |
|-----------|----------------|------|
| `mauiMobile` | `已完成` | MAUI 移动端三段式（application/infrastructure/ui）；KISS/反模式/自检加 application，ui 只精简 |

### 修改原因

`mauiMobile` 存在交付三节重叠、任务状态定义与动作描述重复、子规则阅读摘要与根索引重复、主动建议含与根重复的共性，且应用层缺 KISS / 反模式 / 自检条款。本次同步 dotnetLayered 治理净效果并统一版本号 1.10.0。

### 主要修改内容

- 根 `AGENTS.md`：「根文件原则」新增 KISS 心法行；合并交付三节为「交付与验证」（MAUI 移动向：引用 ui/application/infrastructure，保留平台特定 Android/iOS 维度）；合并「任务状态触发条件」；agents-root 1.7.0→1.10.0
- `application-AGENTS.md`：删「阅读摘要」；「主动建议规则」去共性加根指针；新增「核心逻辑可读性(KISS)」「反模式与修根因清单」「可维护性自检」；强化注释规范行内注释条款；application-agents 1.6.0→1.10.0
- `doc-AGENTS.md`：删「阅读摘要」；「主动建议规则」去共性加根指针；阶段 0 新增「设计自检清单」；doc-agents 1.6.0→1.10.0
- `ui-AGENTS.md`：删「阅读摘要」；「主动建议规则」删复用共性、留页面/布局/样式一致性、design-system 冲突、共享改动不扩大三条领域特化并加根指针；ui-agents 1.4.0→1.10.0
- `infrastructure-AGENTS.md`：删「阅读摘要」；「主动建议规则」去复用 / 查证共性加根指针；infrastructure-agents 1.4.0→1.10.0

### 适配差异

- **KISS / 反模式 / 自检加到 `application-AGENTS.md`**（ViewModel/Service 业务逻辑层，.NET C#）；`ui-AGENTS.md` 以 XAML / 绑定为主，只做精简。

### 修改文件

- `mauiMobile/AGENTS.md`（agents-root 1.7.0 → 1.10.0）
- `mauiMobile/application-AGENTS.md`（application-agents 1.6.0 → 1.10.0）
- `mauiMobile/doc-AGENTS.md`（doc-agents 1.6.0 → 1.10.0）
- `mauiMobile/ui-AGENTS.md`（ui-agents 1.4.0 → 1.10.0）
- `mauiMobile/infrastructure-AGENTS.md`（infrastructure-agents 1.4.0 → 1.10.0）

### 当前子任务状态

- `已完成`
