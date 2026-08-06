## [2026-08-06]：dotnetLayered 引入 KISS 可读性、设计自检与修根因清单

跟踪状态表：

| 文件夹名称 | 当前子任务状态 | 备注 |
|-----------|----------------|------|
| `dotnetLayered` | `已完成` | 参考 `skills/human-first-kiss-engineering` 工程心法，把可读性 / KISS / 反过度抽象的可落地部分分发进现有规则文件，不新增文件 |

### 修改原因

`dotnetLayered` 现有规则偏重分层职责、流程门控与交付约定，但缺乏把"代码可读性 / KISS / 反过度抽象"作为可执行硬约束的条款：KISS 只在"技术债务"档次里零散提及；.NET 真正的可读性陷阱（内存层多层嵌套 LINQ、`GroupBy…ToDictionary` 链、过度 helper）无警告；注释规范偏 Swagger 形式，行内"为什么"薄弱；系统设计缺一个防抽象名词糊弄的自检清单；修根因缺 .NET 化黑名单。

参考 `skills/human-first-kiss-engineering` 的可落地部分（剔除 Polanyi 哲学表述和与现有规则重叠的内容），就地分发进现有三个规则文件，不新增文件——新增文件本身会违反 KISS。

### 主要修改内容

- 根 `AGENTS.md`「根文件原则」新增一行心法：代码先服务于人，再服务于机器；方案先按 KISS 收敛，核心逻辑优先直白写法，复杂分支用行内注释说明"为什么"。
- `backend-AGENTS.md` 新增「核心逻辑可读性（KISS）」小节：先读再写（当前文件 + 2-3 个相似实现 + 调用点）；聚合 / 分组取 Top N / 多层字典 / 失败边界控制流优先显式直写（`foreach` / `if` / 中间变量）；谨慎多层嵌套 LINQ 与一次性 `GroupBy + Select + ToDictionary + ToList` 链；helper / 拆分只在降低认知负担时成立；范围只做用户要的。
- `backend-AGENTS.md` 强化「代码注释规范」行内注释条款：补充 fail fast 原因、为什么跳过脏数据、为什么保留某一层结构。
- `backend-AGENTS.md` 新增「反模式与修根因清单」：作为根规则"修 Bug 定位根因"在 .NET 的具体禁止项——空 `catch` / `catch (Exception)` 吞异常、`!` 压制可空告警、`.Result` / `.Wait()` 同步阻塞、`#pragma warning disable` 掩盖、删测试、抽象重写掩盖复杂度；并给出"出现即回退"的倾向信号。
- `backend-AGENTS.md` 新增「可维护性自检（提交前）」：6 条问句，犹豫时默认选直白版。
- `doc-AGENTS.md` 阶段 0 新增「设计自检清单」：入口 / 核心流程 / 状态变化 / 关键分支 / 副作用 / 失败边界 6 要素；附设计阶段禁忌（禁用"增加一个聚合层""引入 orchestration 机制"等抽象名词替代设计）。
- 三文件 frontmatter `version` / `last_updated` 同步更新。

### 修改文件

- `dotnetLayered/AGENTS.md`（agents-root 1.7.0 → 1.8.0）
- `dotnetLayered/backend-AGENTS.md`（backend-agents 1.8.1 → 1.9.0）
- `dotnetLayered/doc-AGENTS.md`（doc-agents 1.6.0 → 1.7.0）

### 当前子任务状态

- `已完成`

### 备注

- 仅 `dotnetLayered` 单模板，未触及其他架构模板。
- 仓库根 `AGENTS.md` 当前工作区有一处未提交改动（淡化 `doc/AI-Rules-Audit-Log.md`、改以 `doc/rulelog/` 为主），与本次任务无关；因该流程调整尚未提交，本次未同步 `doc/AI-Rules-Audit-Log.md`，如需要可补。
