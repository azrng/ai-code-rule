# dotnetCoreSingle 重构为单项目整洁架构

## 本次目标
- 按用户意图（在单个项目内使用整洁架构）重构 `dotnetCoreSingle` 规则模板，并同步修复与 `dotnetLayered` 同源的规则矛盾、模板残留与技术栈事实错误。

## 核心改动
- 架构定位重写：由「单项目结构（不分层）」的 MVC 文件夹组织，改为「文件夹即层」的单项目整洁架构（`Domain/` / `Application/` / `Infrastructure/` / 宿主入口），词汇对齐 Azrng 栈（AppService / DomainService / `IBaseRepository<T>`）。
- 新增「依赖方向约定」章节：单项目无编译期隔离，明确各文件夹允许与禁止的引用方向，并纳入阶段 1 门控。
- 新增「按需使用与演进」章节：小型 CRUD 不建空文件夹、领域服务下沉门槛、拆分为 `dotnetLayered` 多项目的信号与文件夹到层的迁移映射。
- 数据访问对齐库实际用法：`IBaseRepository<T>` / `IUnitOfWork`、分页四件套、软删除显式过滤、`{实体名}Etc.cs` 配置类。
- 统一响应改为全局包装三件套，Controller 一行转发 `Task<T>`；异常处理禁止手工构造失败结果。
- 依据 `nuget-packages` 源码核实并修正：`Common.EFCore.PostgresSql` 包名拼写；内存缓存配置类实为 `MemoryCacheOptions`（规则原写 `MemoryConfig` 有误），`DefaultExpiry` 默认仅 5 秒，规则要求业务缓存显式传过期时间；Redis 配置类为 `RedisCacheOptions`。
- 迁移规则修正：脚本目录 `MigrationSql/`、版本表由包自动维护、已应用脚本不可修改。
- 修复自动提交与任务状态机死锁（同 dotnetLayered 修法）；根文件删除「快速命中卡片」与重复状态定义，「提交前最小回归」分流到子规则。
- 清理模板残留：「设计系统」「纯页面样式 / 纯界面交互 / 纯界面实现 / 页面联调」「联调」与各文件尾部「文件结束。」占行。
- infra 端口表补 Redis（启用时）；`TASK.md` 补活跃任务骨架并统一为新任务在上。
- 同步修正 `dotnetLayered/backend-AGENTS.md` 中 `MemoryConfig` 错误类名为 `MemoryCacheOptions`。

## 修改文件
- `dotnetCoreSingle/AGENTS.md`
- `dotnetCoreSingle/backend-AGENTS.md`（整文件重写，version 2.0.0）
- `dotnetCoreSingle/doc-AGENTS.md`
- `dotnetCoreSingle/infra-AGENTS.md`
- `dotnetCoreSingle/TASK.md`
- `dotnetLayered/backend-AGENTS.md`

## 校验情况
- 对照 `onion-architecture`、`azrng-efcore` 技术栈指南与 `C:\Work\gitee\nuget-packages` 库源码核实事实性内容（缓存配置类与默认值、包名、注册 API）。
- 全目录检索确认残留关键词（页面 / 界面 / 联调 / 设计系统 / MemoryConfig / 不分层 / ActionResult / 文件结束）与乱码字符零命中。

## 风险或遗留项
- backend 规则为大版本重写（2.0.0），若存量项目按旧 MVC 结构组织，按「以现有代码为准，不强制迁移」原则处理。
- 其余模板目录（vueLinkdotNet、reactLinkdotNet 系、dotnetConsole 等）仍可能存在同类残留与旧响应包装写法，待后续逐目录审查。
