# RULE-20261120-SKILL-RAWSQL Azrng 生态可选包与原生 SQL 选型反馈批

来源：内部测试项目（dotnet-layered 组合，v3.15.x 技能）架构对标与内部包盘点产生的 `.ai-rule-feedback.md`，2 条「缺失」。

## 逐条结论（2/2 采纳）

| # | 反馈主题 | 类型 | 结论与落点 |
| --- | --- | --- | --- |
| 1 | 原生 SQL 缺动态 SQL 构建器可选指引（Azrng.DynamicSqlBuilder / Azrng.JSqlParser） | 缺失 | 采纳：azrng-efcore「自定义仓储」新增「LINQ 之外的原生 SQL 选型」四级排序（LINQ → Common.Dapper → DynamicSqlBuilder+JSqlParser（运行时可配置 SQL/解析改写，触发门槛：独立工程体量大、纯 CRUD 不引入，连接池收敛 Storage/组合根）→ 行锁 FromSql 维持原口径）+ NEVER 手拼条件字符串；onion「何时该拆」补「LINQ 之外的原生 SQL 怎么走？」指针条 |
| 2 | Azrng/Common 生态可选能力包缺统一选型清单（Cache.Redis/S3Store/Dapper/SettingConfig.BasicAuthorization） | 缺失 | 采纳：onion「何时该拆/不该拆」新增「基础设施可选包速查」表（5 行：需求/选型+版本基线/注册与落层/触发门槛，均可选项不进骨架清单，注册在组合根，包 API 以锁定版本解包为准）；「文件上传与外部存储」节补 S3Store 默认落地句；两技能 description 补检索关键词（Dapper/IDapperRepository/DynamicSqlBuilder/JSqlParser/动态SQL、对象存储/S3Store/ICacheProvider） |

## 证据核实（2026-11-20 逐一比对内部包仓库源码）

- 内部包仓库 `src/Shared/`：`AddRedisCacheStore(Action<RedisCacheOptions>)` 注册 `IRedisProvider`/`ICacheProvider`（后者源 `Azrng.Cache.Core`）单例 ✓；`AddS3Store(连接串|Action<S3StoreConfig>)` 双重载、`IStore` 上传/下载落盘/预签名/桶策略 ✓；`AddDapper(Action<DapperRepositoryOptions>?)` 注册 `IDapperRepository`（Scoped），QueryAsync/QueryPagedAsync/QueryMultiple/ExecuteInTransactionAsync ✓；BasicAuthorization README 1.3.0 徽章、`options.Authorization = new[] { new BasicAuthAuthorizationFilter(...) }` ✓
- 版本基线核对：Common.Cache.Redis 3.1.0 / Azrng.S3Store 1.2.0 / Common.Dapper 0.2.0 与 README「版本更新记录」一致 ✓
- `Azrng.DynamicSqlBuilder` 为内部参照项目的独立工程（ProjectReference，非 NuGet 包）；`Azrng.JSqlParser` 为包（JSqlParser 5.4 移植，NuGet 版本仍 1.0.0-rc 段，故技能不写版本基线）；连接池等基础设施细节实测在该项目 Storage 层 ✓

## 影响面

- 仅技能（skills）：onion-architecture SKILL.md（2 处内联 + description）、azrng-efcore SKILL.md（1 处内联 + description）；fragments 零改动（可选包口径属技能面，不进规则基线内容）
- 用户指令：技能发布内容不携带内部参照工程名——azrng-efcore 两处「参照工程 <内部项目>」删除/改中性「引用形态」句，layer-responsibilities.md「实测 <内部项目>」句改「实测存量典型解决方案」，本档案内部绝对路径与私有工程名脱敏为「内部包仓库 / 内部参照项目 / 测试项目」；`skills/` 全目录内部工程名检索 0 残留
- 版本 3.18.0 → 3.19.0；emit 基线刷新（214 条纯版本传导，签名与前批一致）

## 验证

- `--check` 37 样例全过（含技能登记校验 53 技能）；`dotnet test` 273/273 全绿
- 基线 diff 214 条 = 全 AGENTS 家族版本传导，与前批（3.15.1/3.16.0 纯技能批）签名一致

## 回执

2 条全采纳，版本 3.19.0；测试项目 `.ai-rule-feedback.md` 主仓侧直接回写处理标记并清空条目，`--upgrade` 重放同步新技能。
