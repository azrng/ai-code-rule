## [2026-08-07]：全 .NET 模板同步 KISS 可读性约束、去重精简与版本统一 1.10.0

跟踪状态表：

| 文件夹名称 | 当前子任务状态 | 备注 |
|-----------|----------------|------|
| `dotnetCoreSingle` | `已完成` | 纯后端，KISS 加 backend；backend 由 2.0.1 下调到 1.10.0 |
| `dotnetConsole` | `已完成` | 控制台，KISS 加 application；doc 补阶段 0 设计文档章节 + 设计自检 |
| `vueLinkdotNet` | `已完成` | Vue + .NET，KISS 加 backend，frontend 只精简 |
| `reactLinkdotNet` | `已完成` | 任务管理简化口径，未做任务状态合并；KISS 加 backend |
| `electronDotNetExe` | `已完成` | Electron sidecar，任务管理简化口径；KISS 加 backend |
| `wpfExe` | `已完成` | 桌面三段式，KISS 加 application；顺带修正交付收口异常引号 |
| `mauiMobile` | `已完成` | MAUI 移动三段式，KISS 加 application |
| `avaloniaExe` | `已完成` | Avalonia 桌面三段式，KISS 加 application |
| `dotnetLayered` | `已完成` | 内容 8/6 已完成，本次仅版本号补齐到 1.10.0 |
| `dotnetBlazor` | `已完成` | 内容 8/6 已完成，本次仅版本号补齐到 1.10.0 |

### 修改原因（通用）

8/6 在 dotnetLayered / dotnetBlazor 完成「引入 KISS」与「去重精简」两轮治理后，8/7 将同一治理净效果同步到其余 8 个 .NET 模板，并将全 10 模板规则文件版本号统一为 1.10.0。本批属同一主题（同步 KISS 可读性约束与去重精简），按根 `AGENTS.md`「同一主题、同一批次只保留一条记录」规则，将当日原分散的 10 条记录合并为本条。

### 通用修改内容

- 根 `AGENTS.md`：「根文件原则」加 KISS 心法行；合并交付三节为「交付与验证」（按各模板维度适配）；任务状态合并（仅原有冗余小节的模板）
- 实现层（backend / application）：删阅读摘要；主动建议去复用 / 查证共性加根指针；新增「核心逻辑可读性(KISS)」「反模式与修根因清单」「可维护性自检」；强化注释规范行内注释
- `doc-AGENTS.md`：删阅读摘要；主动建议去共性；阶段 0 加「设计自检清单」
- 前端层（frontend / ui）：删阅读摘要；主动建议去共性留领域特化
- 基础设施层（infra / infrastructure）：删阅读摘要；主动建议去共性
- 全 10 模板规则文件版本统一 1.10.0；共性规则统一指向根「模型行为约束与主动建议」

### 各模板差异与状态

#### `dotnetCoreSingle`（纯后端，已完成）
- KISS / 反模式 / 自检加 backend；doc 加设计自检
- backend 由 2.0.1 下调到 1.10.0（用户明确要求全模板统一）
- 版本：agents-root / backend / doc / infra 均 1.10.0

#### `dotnetConsole`（控制台，已完成）
- KISS / 反模式 / 自检加 application（Command / Service 语境）
- doc 原无「阶段 0 设计文档」章节，补齐该章节（四部分适配：用「命令与输入输出」替代「接口契约」）+ 设计自检清单，消除「常见任务入口」悬空引用
- 版本：agents-root / application / doc 均 1.10.0

#### `vueLinkdotNet`（Vue + .NET，已完成）
- KISS 加 backend；frontend 只精简；doc 加设计自检
- 版本：全 5 文件 1.10.0

#### `reactLinkdotNet`（React + .NET，已完成）
- 任务管理为简化口径，无「任务状态触发条件」冗余小节，未做该项合并
- KISS 加 backend；frontend 主动建议留 3 条领域特化；doc 加设计自检
- 版本：全 5 文件 1.10.0

#### `electronDotNetExe`（Electron + .NET sidecar，已完成）
- 任务管理为简化口径，未做任务状态合并
- KISS 加 backend；frontend 只精简；doc 加设计自检；交付节保留 SQLite schema / 打包维度
- 版本：全 5 文件 1.10.0

#### `wpfExe`（WPF 桌面，已完成）
- KISS / 反模式 / 自检加 application（ViewModel / Service）；ui 只精简
- doc 加设计自检；顺带修正根文件交付收口异常引号（” → “”）
- 版本：全 5 文件 1.10.0

#### `mauiMobile`（MAUI 移动，已完成）
- KISS 加 application；ui 只精简；doc 加设计自检；交付节保留平台特定（Android / iOS）维度
- 版本：全 5 文件 1.10.0

#### `avaloniaExe`（Avalonia 桌面，已完成）
- KISS 加 application；ui 只精简；doc 加设计自检
- 版本：全 5 文件 1.10.0

#### `dotnetLayered` / `dotnetBlazor`（8/6 内容已完成，8/7 补齐版本）
- 内容（KISS / 去重 / 设计自检）在 8/6 两轮已完成，见当日两条 rulelog
- 8/7 仅将版本号由 1.8.0 / 1.9.0 / 1.7.0 补齐到 1.10.0（backend 已是 1.10.0 不动）
- 版本：全规则文件 1.10.0

### 修改文件

- `dotnetCoreSingle`：AGENTS、backend、doc、infra
- `dotnetConsole`：AGENTS、application、doc（含补阶段 0 章节）
- `vueLinkdotNet`：AGENTS、backend、doc、frontend、infra
- `reactLinkdotNet`：AGENTS、backend、doc、frontend、infra
- `electronDotNetExe`：AGENTS、backend、doc、frontend、infra
- `wpfExe`：AGENTS、application、doc、ui、infrastructure
- `mauiMobile`：AGENTS、application、doc、ui、infrastructure
- `avaloniaExe`：AGENTS、application、doc、ui、infrastructure
- `dotnetLayered`：AGENTS、doc、infra（版本）
- `dotnetBlazor`：AGENTS、doc、infra、ui（版本）

### 当前子任务状态

- 全部 `已完成`

### 备注

- 本记录合并 8/7 原分散的 10 条 rulelog（8 模板同步 + dotnetConsole 补阶段 0 + dotnetLayered / dotnetBlazor 版本统一），按根 `AGENTS.md`「同一主题只保留一条记录」规则。
- 各改动以独立 commit 落库（8 模板各 1 + dotnetConsole 补阶段 0 1 + 版本统一 1，共 10 个 commit），本记录为这批改动的唯一 rulelog 汇总。
- 8/6 的 dotnetLayered / dotnetBlazor 内容改动见当日两条 rulelog，未并入本记录。
