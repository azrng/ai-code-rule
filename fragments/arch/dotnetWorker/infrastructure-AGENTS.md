---
rule_id: infrastructure-agents
version: 1.0.0
last_updated: 2026-11-20
dependencies: [agents-root]
---

# 基础设施规则

<!-- include: shared/infrastructure/scope.md -->

---

<!-- include: shared/infrastructure/advisory.md -->

---

## 基础设施规则

### Repository 层规则（可选，仅在使用数据库时）
- 仓储类必须实现接口，接口定义在 `Repositories/Interfaces/` 目录
- 仓储只负责数据访问与查询拼装，不承担业务流程编排
- 仓储方法必须是异步的，支持取消令牌
- 涉及事务时，使用 `IDbTransaction` 或等价事务边界统一管理
- 仓储实现注册到 DI 容器时，统一复用现有批量注册方式
<!-- include: shared/infrastructure/data-access-desktop-core.md -->

### 数据访问补充（后台服务专属）
- 长周期作业内的数据库连接按「一次作业一个作用域一批连接」处理，禁止跨轮次长期持有连接或 DbContext
- 数据库瞬时故障（网络抖动、主备切换）按可重试错误处理，接入退避策略；约束冲突、语法错误等确定性错误不重试直接上报
- 长事务必须拆分为可恢复的小批次，并记录批次游标，服务重启后从游标续跑而不是从头再来

### 依赖注入与配置规则
- 依赖注入配置集中在 `Extensions/`，Program.cs 只调用注册扩展
- 必须补充默认配置说明与默认值（如 `appsettings.Development.json`），运行参数必须可配置，禁止硬编码
- 配置变更（新增 Options 项）必须同步更新配置文档与示例文件，后台服务无 UI 提示，配置文档就是运维界面
- 启动时输出关键配置摘要（脱敏后），便于从日志确认服务实际生效的配置

### Windows 服务安装与运维规则
- 注册为 Windows 服务统一使用 `sc create` / `sc config` 或安装脚本封装，脚本随仓库维护（如 `installer/`、`scripts/`），禁止只存在于口头步骤
- 服务账户：默认 LocalSystem / NetworkService 需在文档显式写明；需要最小权限时说明所取权限与理由
- 服务恢复策略必须配置（`sc failure` 或脚本设置）：首次 / 第二次失败重启、重启间隔、达到上限后的行为
- 启动类型（自动 / 自动延迟 / 手动）在发布文档中显式声明；依赖其他服务时用 `sc config depend=` 声明依赖
- 卸载服务必须先停止再 `sc delete`，卸载脚本不得遗留停止中状态的服务；卸载后数据与日志目录的保留 / 清理口径写入文档
- 冒烟验证：至少执行一次「安装 → 启动 → 观察日志 → 停止 → 卸载」完整链路，不以「能启动」代替「能优雅停止」

### 构建发布链路
- 发布口径与运行形态匹配：目标机无 .NET 运行时 → 自包含单文件（`-p:SelfContained=true -p:PublishSingleFile=true -p:RuntimeIdentifier=win-x64`）；有运行时 → framework-dependent
- Windows 服务形态发布目录必须完整（自包含单文件除外）：`appsettings.json` 等内容文件标记 `CopyToOutputDirectory`，发布脚本不得只拷 exe
- 工作目录陷阱：Windows 服务的默认工作目录是系统目录（`C:\Windows\System32`），相对路径配置 / 数据 / 日志一律基于 `AppContext.BaseDirectory` 解析，禁止基于 `Directory.GetCurrentDirectory()`
- 用户数据目录：可写数据不放在安装目录（服务账户可能无写权限、升级会覆盖），统一落 `%ProgramData%\<服务名>` 或 `%LOCALAPPDATA%`（用户态服务），路径解析集中在 Helper 一处
- 版本与回滚：发布产物带版本号（InformationalVersion），安装脚本支持版本记录与回滚到上一产物

### 日志与可观测性
- 日志目录按「运行用户可写」原则选址，服务账户权限与目录 ACL 在安装脚本中对齐
- 滚动策略必须配置（按天或按大小 + 保留份数），后台服务 7×24 运行，无上限日志是事故隐患
- 崩溃 / 未处理异常必须留痕：宿主启动失败与停止超时的现象写入文档的排障小节

<!-- include: shared/infrastructure/pre-commit-regression.md -->
