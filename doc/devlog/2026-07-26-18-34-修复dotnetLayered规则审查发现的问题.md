# 修复 dotnetLayered 规则审查发现的问题

## 本次目标
- 对 `dotnetLayered` 规则模板做一次完整审查，修复规则矛盾、模板残留、重复内容与和 Azrng 技术栈不符的事实错误。

## 核心改动
- 修复自动提交与任务状态机死锁：单 Agent 会话完成验证后可直接置 `DONE` 并触发自动提交；`REVIEW` 仅用于用户要求评审或跨会话交接场景。
- 明确 Storage 层使用方式：接口定义在 Core、实现在 Storage，业务层依赖接口，Service 只装配。
- 数据访问对齐 Azrng 实际用法：默认注入 `IBaseRepository<T>`，自定义仓储仅复杂场景使用并明确接口 / 实现归属；软删除改为显式过滤 `!x.Deleted`（包不自动全局过滤）；实体配置类命名对齐 `{实体名}Etc.cs`。
- 统一响应格式改为全局包装三件套（`AddMvcResultPackFilter` + `AddMvcModelVerifyFilter` + `UseGlobalException`），禁止新代码手写 `ResultModel.Success / Error`；Controller 改为一行转发 `Task<T>`。
- 修正 `Azrng.SqlMigration` 事实错误：脚本目录改为 `Service/MigrationSql/`，版本表由包自动维护，已应用脚本不可修改。
- 修正 NuGet 包 id 拼写：`Common.EFCore.PostgresSql`。
- 清理全栈模板残留：删除 doc 规则中不存在的「设计系统」引用，删除各文件「纯页面样式 / 纯界面交互 / 页面联调」字样，阶段 2 改为「验证与交付」。
- 去重：根文件「快速命中卡片」并入子规则索引，「提交前最小回归」改为分流到子规则；backend 删除重复的依赖注入规范小节；删除 MediatR 提及。
- infra 默认端口表补充 Redis（启用时）；内存缓存默认过期时间不再写死具体数值。
- `TASK.md` 补充活跃任务骨架，根规则明确 `DONE` 归档仅保留精简字段。

## 修改文件
- `dotnetLayered/AGENTS.md`
- `dotnetLayered/backend-AGENTS.md`
- `dotnetLayered/doc-AGENTS.md`
- `dotnetLayered/infra-AGENTS.md`
- `dotnetLayered/TASK.md`

## 校验情况
- 与 `onion-architecture`、`azrng-efcore` 两个技术栈指南逐条核对了响应包装、仓储、软删除、迁移与包名等事实性内容。
- 使用 `UTF-8` 重新读取改动文件，确认关键中文正常显示。

## 风险或遗留项
- 实体配置类命名从 `{实体名}Configuration.cs` 对齐为 `{实体名}Etc.cs`，若存量项目使用旧命名，按模板「优先沿用现有实现」原则处理即可。
- `MemoryConfig` 默认过期时间未写具体数值，后续可在核对 `Common.Cache.MemoryCache` 实现后补充。
- 其他技术栈目录（如 reactLinkdotNet 系）可能存在同类「联调 / 设计系统」残留与响应包装旧写法，本次未处理。
