# RULE-20261119-SKILL-ONION 洋葱技能实测反馈承接批

来源：测试项目 `D:\temp\skillsTest`（dotnet-layered 组合，v3.14.0 规则 + onion-architecture / azrng-efcore 技能）实测 RBAC（T-001~T-005）产生的 `.ai-rule-feedback.md`，16 个条目标题（15 条实质 + 1 条汇总）。口径事实源 = 测试项目用户拍板整改后的最终形态；两处关键拍板：①自定义仓储落点单口径改死 Storage（Storage 依赖 Core+EntityFramework），②测试项目反馈文件由主仓侧直接清理并 `--upgrade` 重放。

## 逐条结论（15/15 采纳）

| # | 反馈主题 | 类型 | 结论与落点 |
| --- | --- | --- | --- |
| 1 | JWT 选型漏读技能（选成裸 MS 包） | 违反+可发现性 | 采纳：onion SKILL.md 新增「认证与授权（JWT）」节（包选型/IBearerAuthService/装配模板/密钥 ≥32 位且 ≥8 种字符/FallbackPolicy），引言横切约定 2→3，骨架清单补第 5 条，description 补关键词 |
| 2 | 包 API 细节需解包/反射核实 | 缺失 | 采纳：onion SKILL.md「包 API 以锁定版本为准」blockquote（含 AddMvcResultPackFilter 挂 IServiceCollection、Timeout 单位秒、IBearerAuthService 需手动 AddScoped、AddEntityFramework 已内含注册 4 实测事实）+ azrng-efcore 版本基线句同步 |
| 3 | 目录表缺自定义仓储导航行 | 缺失 | 采纳：dotnet-layered 层内目录组织表补实现行（`Storage/{业务域}Manager/`），文件/对象存储行标注「仅文件/对象存储」 |
| 4 | 用户拍板仓储落 Storage 与规则冲突 | 冲突 | 采纳（单口径改死 Storage）：职责表 Storage 行（+仓储实现、依赖 +EntityFramework、禁 Domain）、EF 规则落点改写、说明句改写；onion 分层表/分层图/依赖链同步 |
| 5 | BaseRepository 不带 IScopedDependency 照抄即崩 | 缺失 | 采纳：EF 规则 + azrng-efcore 新增「自定义仓储」节（必显式 IScopedDependency、接口收窄取舍、移植坑） |
| 6 | 业务域目录 XxxManager 未成文 | 冲突 | 采纳：dotnet-layered 目录表/DTO 组织条款全量 Manager 化 + 命名空间口径（`根.{域}Manager` 跨工程共用，Entities/Adapter 基础设施目录除外）；onion 两文件示例同步（namespace 去工程段） |
| 7 | 仓储字段命名 _xxxRepository 未成文 | 缺失 | 采纳：field-naming.md（删「仓储复数名」旧口径） |
| 8 | 未禁主构造/表达式体 | 缺失 | 采纳：Service 规则补块体条款（主构造禁令已有）；onion 示例全量改显式 ctor + 块体（含控制器/Adapter/HealthController） |
| 9 | 未禁 LINQ 查询表达式 | 缺失 | 采纳：EF 规则 + azrng-efcore repository-and-unitofwork.md；doc 组件无 LINQ 表述，无需改 |
| 10 | slnx 缺分组规范 | 缺失 | 采纳：dotnet-layered 项目结构节 + onion 两文件 slnx 示例改编号分组（含 Folder 嵌套非法坑） |
| 11 | 命名口径整体偏差汇总 | 冲突 | 并入 #6-#10，不单独立条 |
| 12 | FallbackPolicy 全局兜底未成文 | 缺失 | 采纳：server-side-authorization.md 补语言无关「默认拒绝走全局兜底机制」条 + dotnet-layered Service 规则补 ASP.NET Core 具体机制 + onion JWT 节/Startup 模板/HealthController 类级 AllowAnonymous |
| 13 | 包自带 ICurrentUser<TSubject> 未提及 | 缺失 | 采纳：layer-responsibilities.md 新增「当前用户上下文」节 + dotnet-layered Core 规则一条 |
| 14 | 注入字段命名（去 I 全名） | 缺失 | 采纳：与 #7 合并进 field-naming.md（统一 `_{接口类型去 I 全名}`，IBaseRepository<T> 按 `_{实体单数}BaseRepository`，单测 Fake 不受限）；onion 命名表同步 |
| 15 | 迁移锁默认装配 | 缺失 | 采纳：db-migration.md 补锁条款（锁源判据/?? throw/等待传长/锁 key）+ onion 骨架清单第 2 条与 Startup 模板 + azrng-efcore SQL 迁移节（含装配代码） |
| 16 | Startup 拆 Extension 阈值 | 缺失 | 采纳：layer-responsibilities.md「Program.cs 与 Startup.cs 分工」补阈值判据（约 120 行+ 或 ≥6 块且出现第二消费方才外提，低于阈值分节注释不预拆） |
| 17 | 工具层脱敏误诊（凭据写成 ***） | 冲突 | 采纳：dotnet-layered 后端测试节补敏感字面量传参与 hexdump 排障条款 |

## 影响面

- 规则（fragments）：`arch/dotnet-layered/backend-AGENTS.md`（内联，仅 dotnet-backend-layered 组件）、`shared/backend/field-naming.md`（8 个 .NET 组合）、`shared/backend/server-side-authorization.md`（12 处跨语言，语言无关句）、`shared/backend/db-migration.md`（5 处 Azrng 组合）
- 技能（skills）：onion-architecture SKILL.md + layer-responsibilities.md、azrng-efcore SKILL.md + repository-and-unitofwork.md；无 front matter version 维持现状
- 版本 3.14.0 → 3.15.0；emit 基线刷新

## 验证

- `dotnet test` 273/273 全绿；`--check` 37 样例全过（含组件问答决策树与技能登记）
- 基线 diff 214 条 = 全部 AGENTS 家族 md（版本传导 + 内容），CLAUDE/GEMINI/GROK 与 design-system.yaml 0 变化——与上一次升版 df8d1ba（v3.14.0）签名完全一致
- `--emit` 抽查 dotnet-layered/backend-AGENTS.md：version 3.15.0、FallbackPolicy、Manager 目录 9 处、Storage 仓储行（职责表+目录表+说明句）、slnx 分组、LINQ 方法语法、ICurrentUser、迁移锁、敏感字面量条款全部落地

## 回执（已同步测试项目）

15 条全部采纳（汇总条并入），规则版本 3.15.0；测试项目 `.ai-rule-feedback.md` 已清空，`--upgrade` 重放同步新规则与技能。

## 补充批（3.15.1）：技能包版本基线对齐 T-005

测试项目 T-005（Azrng/Common 包对齐最新）后技能里的版本基线滞后，按测试项目 csproj 锁定版本 + `C:\Work\gitee\nuget-packages` 源码仓库核实订正：

- azrng-efcore：`Common.EFCore` 1.7.1 → **1.8.0**、`Common.EFCore.PostgresSql` 1.7.2 → **1.8.0**（安装示例 3 处 + 基线句，注明 2026-11 实测项目锁定验证）；`Azrng.SqlMigration` 安装处补基线 **0.6.1**（「0.6.0+」下限表述本就兼容）
- onion：ErrorCode blockquote 的传递依赖叙述改写（新基线 1.8.0 与 Azrng.Core 2.0.0 同批，1.20.1 下限坑只影响锁 1.7.x 的存量项目）；layer-responsibilities 示例 `Azrng.Core` 1.20.1 → **2.0.0**；JWT 节补 `Azrng.AspNetCore.Authentication.JwtBearer` 基线 **1.5.0**
- fragments 侧核对无需改（swagger.md 0.4.0、core-libs.md HttpClients 4.2.0 与实测一致）
- 纯技能变更：升 3.15.1 + 刷基线（214 条全 AGENTS 家族，签名与 3.15.0 批一致）；273 测试与 --check 37 样例全绿
