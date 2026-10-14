# Azrng EFCore Agent Skill 优化任务

你是一名资深 .NET / EF Core / 数据访问架构专家。

我已经向你提供了：

1. Azrng Common.EFCore 包家族完整源码：`C:\Work\gitee\nuget-packages`
2. 各包随 NuGet 发布的 README 与 XML 文档注释（`src/Shared/<包名>/README.md`）
3. 仓库内的示例项目（`src/Services/`）与 xUnit 测试项目（`test/`）
4. 现有 azrng-efcore skill：本仓库 `skills/dotnet-skills/azrng-efcore`（v1.1.0，含 SKILL.md + 5 个 references）

你的任务不是开发业务功能，而是**基于当前源码分析并优化现有 azrng-efcore Agent Skill**，把它做成高质量、可长期维护、与包版本同步的数据访问 Agent Skill。

目标使用环境包括：

* OpenAI Codex
* Claude Code
* Cursor 等支持 Agent Skills / SKILL.md 的编码 Agent

## 范围界定（已确认）

本 skill 覆盖：

* `Common.EFCore`（基础包）
* `Common.EFCore.PostgresSql` / `Common.EFCore.MySQL` / `Common.EFCore.SQLServer` / `Common.EFCore.SQLite` / `Common.EFCore.InMemory`（5 个 provider）
* `Azrng.EFCore.AutoAudit`（SaveChanges 自动审计）
* `Azrng.SqlMigration`（SQL 脚本版本化迁移）

不覆盖、后续单独成 skill：

* `Azrng.DataAccess`（Dapper 多库数据访问）
* `Azrng.AspNetCore.DbEnvConfig`（数据库配置源）

范围外的包在本 skill 中最多一句话指路，不展开 API。

---

## 一、核心原则

不要根据你已有的 EF Core 或其他 ORM 知识直接编写或修改 Skill。

必须优先以我提供的：

* 包源码（csproj、类定义、扩展方法签名、条件编译指令）
* 各包 README（`Common.EFCore/README.md` 最详尽，含快速开始、实体基类表、事务三方式、FAQ、版本历史）
* XML 文档注释
* `test/` 测试项目
* `src/Services/` 示例项目

作为事实来源。

如果你的已有知识与源码冲突，以当前源码为准。

禁止猜测不存在的：

* 扩展方法与重载
* 实体基类与属性
* 配置项（`EfCoreConnectOption` / `SqlMigrationOption` / 审计配置）
* 注册入口（`Add*` 扩展方法）
* 命名空间与 NuGet 包 id

特别注意：本包家族大量使用**条件编译**（`#if NET7_0_OR_GREATER`、`#if NET10_0_OR_GREATER`）和多目标框架（net6~net10）。任何 API 都必须同时记录"签名 + 最低目标框架"，否则 AI 会在低版本项目里生成编译不过的代码。

---

## 二、第一阶段：分析包家族

首先扫描源码，整理包家族的整体能力。

重点分析：

### 1. 包体系

| 包 | 版本（2026-08 探查） | 说明 |
|---|---|---|
| Common.EFCore | 1.7.0 | 核心：仓储 / 工作单元 / 实体基类 / 分页 |
| Common.EFCore.PostgresSql | 1.7.1 | Npgsql + 蛇形命名约定 |
| Common.EFCore.MySQL | 1.7.0 | Pomelo（net10 改用 Microting 驱动） |
| Common.EFCore.SQLServer | 1.6.2 | Microsoft.EntityFrameworkCore.SqlServer |
| Common.EFCore.SQLite | 1.6.2 | Microsoft.EntityFrameworkCore.Sqlite |
| Common.EFCore.InMemory | 1.6.0 | 测试用内存库 |
| Azrng.EFCore.AutoAudit | 1.0.0-beat1 | SaveChanges 拦截器自动审计 |
| Azrng.SqlMigration | 0.5.0 | SQL 脚本按版本号迁移（目前仅 PostgreSQL 实现） |

以上版本号必须逐一回 csproj 核实后再写入 Skill，不允许沿用旧记录。

### 2. 核心抽象逐项分析

针对每个核心抽象分析：

* 类型名称、所在文件、命名空间
* 泛型参数与约束
* 全部公开方法签名（含默认参数语义，如 `AddAsync(entity, submit)` 的 `submit`）
* 条件编译重载（NET7+ / NET10+ 才有的批量更新等）
* 与原生 EF Core 的行为差异
* 官方示例中的标准用法
* 常见错误、推荐写法、不推荐写法

必须覆盖的抽象：

* `IBaseRepository<TEntity>` / `BaseRepository<TEntity>`（`virtual` 方法可重写）
* 多上下文版本 `IBaseRepository<TEntity, TDbContext>` / `BaseRepository<TEntity, TDbContext>`
* `RepositoryFactory`
* `IUnitOfWork` / `IUnitOfWork<TContext>` / `UnitOfWork<TContext>`（SaveChanges、`CommitTransaction(Async)`、`BeginTransactionScope(Async)`、原生 SQL 家族）
* `ITransactionScope`（Dispose 自动回滚、防重复提交）

### 3. 实体与配置体系

* 继承链：`IEntity` → `IdentityBaseEntity` → `IdentityOperatorEntity`（审计四字段 + `SetCreator`/`SetUpdater`）→ `IdentityOperatorStatusEntity`（`Deleted`/`Disabled` + `SetDeleted`/`SetDisabled`）
* 每级基类都有非泛型与泛型两个版本：非泛型默认 `long` 主键（如 `IdentityBaseEntity`），泛型版本自选主键类型（如 `IdentityBaseEntity<TKey>`，`TKey : IEquatable<TKey>`）。默认用 `long`（配合雪花 ID），确实需要其他主键类型才选泛型版本
* 无参构造已自动 `Id = IdHelper.GetLongId()`（雪花 ID，`Coldairarrow.Util`）
* `*Etc.cs` 实体配置基类家族（`EntityTypeConfigurationIdentity*`）
* **两条 DbContext 路径都要分析**：
  * provider 包内 `BaseDbContext`（自动对所有含 `IEntity` 实体的程序集 `ApplyConfigurationsFromAssembly`）
  * 自定义 `DbContext` + 手动 `ApplyConfigurationsFromAssembly` + `HasDefaultSchema`
* PostgreSQL 时间列**默认使用无时区时间**：列统一声明 `IsUnTimeZoneDateTime()`，配套赋值层 `ToUnspecifiedDateTime()`、DDL 层 `timestamp without time zone`（三层一致）

### 4. 注册体系

* provider 包的三个入口（均在 `Microsoft.Extensions.DependencyInjection` 命名空间）：`AddEntityFramework`（默认 BaseDbContext）/ `AddEntityFramework<TDbContext>` / `AddEntityFrameworkFactory<TDbContext>`
* 基础包的 `AddUnitOfWork<TContext>()`（泛型 UoW / 多上下文场景）与 `AddIdHelper(workId)`
* `EfCoreConnectOption` 全部配置项、默认值与含义（`ConnectionString` / `Schema` / `WorkId` / `IsSnakeCaseNaming` / `UseOldUpdateColumn`）
* 连接串**由调用方传入，包本身不读取任何配置键**——不得在 Skill 中臆造强制配置键名

### 5. 查询、分页与批量更新扩展

分属多个命名空间，必须逐个理清出处，防止 AI 用错 using：

* `Azrng.Core.Extension`：`WhereIfNotNullOrWhiteSpace` / `WhereIfNotNull` / `WhereIfTrue` / `WhereAny`、动态 `OrderBy(SortContent/SortEnum/FiledOrderParam[])`、`EqualWhere/LessWhere/GreaterWhere`、`SelectMapper`、`CountBy`、`PagedBy`
* `Azrng.EFCore.Extensions`：`ToPageListAsync` 家族
* 请求/结果模型：`GetPageRequest` / `GetPageSortRequest`（`Azrng.Core.Requests`）、`GetQueryPageResult`（`Azrng.Core.Results`）、`RefAsync<T>`（`Azrng.Core.CommonDto`）
* 条件批量更新（**仅 NET10+**）：`ConditionalUpdateExtensions` 的 `SetPropertyIfTrue` / `SetPropertyIfNotNull` / `SetPropertyIfNotNullOrWhiteSpace` / `SetPropertyIf`
* `IBaseRepository` 上 ExecuteUpdate 风格的批量更新重载（NET7+/NET10+ 条件编译，逐项核实）
* 表连接标准写法：同 DbContext 的 `EntitiesNoTacking.GroupJoin(...).SelectMany(...DefaultIfEmpty())` 左连接、`Join` 内连接、`GroupBy` 聚合
* 原生 SQL：`ExecuteSqlCommand` / `ExecuteScalar` / `SqlQueryDataTable` / `SqlQueryList` / `SqlQuery<T>` 与 `EFCoreExtension`

### 6. SqlMigration 深度分析

* 注册：`AddSqlMigrationService(name, config)` / `AddSqlMigrationService<T>(...)` + `.AddAutoMigration()`（IStartupFilter 启动执行）
* `SqlMigrationOption` 全部配置项：`ConnectionBuilder` / `LockProvider`（分布式锁）/ `SqlRootPath` / `VersionPrefix` / `Schema` / `VersionLog` / `SetInitVersionSetter<T>`
* 脚本命名与版本排序规则（文件名 = 前缀 + 版本号 + `.sql`/`.txt`，不含描述文字）
* 已应用脚本不可修改；多库 = 多次注册不同名字
* `IMigrationHandler` 扩展点（示例：`WebDbSqlMigrationSample/DetailtMigrationHandler.cs`）
* 与 EF Core Code-First Migrations 的关系：**本家族走 SQL 脚本迁移，不用 EF Migrations**

### 7. AutoAudit 深度分析（现有 skill 完全未覆盖，重点补齐）

* 定位：**可选能力、按需开启**——仅当用户明确提出审计需求时才接入并配置；用户未提及时不主动加、不主动推荐
* 入口：`AddEFCoreAutoAudit(configAction, DatabaseType)`
* `IAuditConfigBuilder`：`WithUserIdProvider` / `WithUnmodifiedProperty` / `WithStore` / `WithEntityFilter` / `WithPropertyFilter`
* 两种存储：`WithAuditRecordsDbContextStore`（审计表 + `AuditRecordsDbContext`）与文件存储 `AuditFileStore`
* 过滤能力：`IgnoreEntity` / `IgnoreTable` / `IgnoreProperty` / `IgnoreColumn`
* 审计表实体 `AuditRecord` 与建表 SQL（见该包 README）
* 测试参考：`test/Azrng.EFCore.AutoAudit.Test`（SQLite + `AddAuditInterceptor` + 过滤演示）

---

## 三、分析示例、测试与包 README

不要只分析包源码。

必须重点分析官方示例与测试，因为：

> Example / Test 中的实际写法优先于 AI 自己推断出来的写法。

重点对象：

* `src/Services/PostgresSqlSample`：最全的集成示例——`Program.cs`（注册全流程含 AutoAudit）、`SelectController`（查询/连接/动态排序）、`AddController` / `UpdateController`、`TranController`（事务）、`OrigionSqlController`（原生 SQL）
* `src/Services/WebDbSqlMigrationSample`：SqlMigration 注册、多库、`IMigrationHandler`
* `test/Common.EFCore.PostgreSql.Test`：单上下文与多上下文下仓储/UoW 共享同一 DbContext 的验证、原生 SQL 测试
* `test/Azrng.EFCore.AutoAudit.Test`：审计接入与过滤
* 各包 README：`Common.EFCore/README.md`、`Common.EFCore.PostgresSql/README.md`、`Azrng.EFCore.AutoAudit/README.md`、`Azrng.SqlMigration/README.md`

对于重复示例，不需要全部复制。提取最典型、最值得 AI 学习的模式。

---

## 四、优化 Skill

现有结构：

```text
skills/dotnet-skills/azrng-efcore/
├── SKILL.md
└── references/
    ├── entities-and-context.md
    ├── postgres-time-pitfall.md
    ├── providers.md
    ├── querying-and-extensions.md
    └── repository-and-unitofwork.md
```

建议目标结构（按分析结论可调整）：

```text
skills/dotnet-skills/azrng-efcore/
├── SKILL.md
└── references/
    ├── providers.md
    ├── entities-and-context.md
    ├── repository-and-unitofwork.md
    ├── querying-and-extensions.md      # 补：条件批量更新（标注 NET10+）、批量更新重载、原生 SQL
    ├── auto-audit.md                  # 新增
    ├── sql-migration.md               # 新增（或维持 SKILL.md 精简版 + 详情下沉）
    └── postgres-time-pitfall.md
```

约束：

**不要把所有内容全部塞进 SKILL.md。**

SKILL.md 只负责：

* Skill 的适用范围与触发场景（front matter description 同步维护）
* AI 工作原则（优先用包能力，不裸写 EF Core）
* 关键约束（包 id 拼写、条件编译 API 的 TFM 门槛、PostgreSQL 时间三层一致）
* 决策指引（选 provider、选实体基类、选查询入口、选事务方式）
* references 导航
* 禁止事项

详细 API 签名、大型示例放到 references。

修改带 YAML front matter 的文件时，必须同步更新 `version` 与 `last_updated`。

---

## 五、SKILL.md 必须告诉 AI 的事情

当用户在 .NET 项目中做数据访问，且项目引用 Common.EFCore 家族包时：

优先使用包提供的能力，而不是裸写 EF Core：

* 存在 `IBaseRepository<T>` 时，业务代码不直接依赖 `DbContext` 做 CRUD
* 存在 `ToPageListAsync` 时，不手写 `CountAsync() + Skip().Take()`
* 存在 `WhereIfNotNullOrWhiteSpace` 等条件扩展时，不堆 `if (xxx != null) query = query.Where(...)`
* 只读/连接/分组查询从 `EntitiesNoTacking` 出发，不用追踪的 `Entities`
* schema 变更走 `Azrng.SqlMigration` 脚本，不用 EF Code-First Migrations
* `Azrng.EFCore.AutoAudit` 是可选能力：**用户明确提出审计需求才接入**，未提及不主动配置；接入后优先用拦截器，不手写审计表填充
* PostgreSQL 数据库默认使用无时区时间：时间列统一声明 `IsUnTimeZoneDateTime()`（赋值 `ToUnspecifiedDateTime()`、DDL `timestamp without time zone`，三层一致）
* 包提供的 Extensions 在适用场景要充分使用：`WhereIfNotNullOrWhiteSpace` / `WhereIfTrue` / `OrderBy(SortContent)` / `SelectMapper` / `CountBy` / `ToPageListAsync` 等能用则必须用，不手写等价的 if-Where、switch-OrderBy、Count+Skip+Take 逻辑

---

## 六、禁止幻觉 API

这是整个 Skill 最重要的规则之一。

当 AI 不确定某个 API 时：

1. 搜索 references
2. 搜索包 README
3. 搜索源码与测试
4. 确认签名与目标框架

之后才能生成代码。

禁止把以下经验直接套用到本包家族：

* 原生 EF Core `DbContext.SaveChanges` 优先的习惯（本家族是仓储 + submit 语义）
* Dapper、SqlSugar、FreeSql、Chloe、ABP 的仓储 API
* 本家族的上游参考库 yrjw.ORM.Chimp 原版 API（有过改动，以当前源码为准）
* 其他分页库的请求/响应模型名

易错细节必须显式写进 Skill：

* NuGet 包 id 拼写是 `Common.EFCore.PostgresSql`（非 PostgreSQL）、`Common.EFCore.MySQL`（非 MySql）
* `SetPropertyIf*` 仅 NET10+；`IBaseRepository` 部分批量更新重载仅 NET7+/NET10+
* net10 下 MySQL provider 驱动与 net6~net9 不同
* 连接串没有强制配置键，由调用方传入

---

## 七、代码生成原则

生成数据访问代码时，优先顺序：

```text
包提供的仓储 / 工作单元抽象
        ↓
包提供的查询 / 分页 / 条件扩展
        ↓
provider 注册约定（EfCoreConnectOption）
        ↓
原生 EF Core（IQueryable 组合、Select 投影）
        ↓
原生 SQL（UoW 的 SqlQuery 家族）
        ↓
手写 ADO.NET
```

能够用包能力完成的功能，不重新造轮子。

适用场景遇到 Extensions 能做的事（条件筛选 / 动态排序 / 投影映射 / 分页 / 计数），必须优先用 Extensions，不允许手写等价逻辑。

---

## 八、数据访问标准模式

研究示例与测试后，固化本包家族的标准模式。AI 应能够根据：

> 查一个带条件筛选和分页的商品列表。

自动推导：

```text
Request 继承 GetPageRequest（PageIndex/PageSize）
        ↓
IBaseRepository<Product>.EntitiesNoTacking
        ↓
WhereIfNotNullOrWhiteSpace（条件筛选）
        ↓
OrderBy(SortContent) / OrderByDescending（排序）
        ↓
Select DTO 投影 / SelectMapper
        ↓
ToPageListAsync(request, RefAsync<int>)
        ↓
GetQueryPageResult(list, new GetQueryPageResult(request, total))
```

根据：

> 下单：扣库存 + 写订单 + 写明细，要么全成功要么全回滚。

自动推导：

```text
注入多个 IBaseRepository<T> + IUnitOfWork
        ↓
BeginTransactionScopeAsync 显式作用域
        ↓
各仓储 AddAsync/UpdateAsync(submit: false)
        ↓
CommitAsync（异常 RollbackAsync + throw）
```

同时保证使用的全部 API 与当前源码版本、项目目标框架一致。

---

## 九、版本意识

在 Skill 中明确记录：

```text
Common.EFCore Version:        （逐一回 csproj 核实）
Provider Versions:
AutoAudit Version:
SqlMigration Version:
Source Commit:                （nuget-packages 仓库当前 commit）
Skill Generated / Updated:
```

如果包源码升级，应能够重新扫描变化并更新 Skill：

* csproj 版本号与 TFM 列表
* 新增/删除的公开 API（含条件编译变化）
* 包 README 的版本历史章节
* 示例与测试的新用法

---

## 十、优化前先输出差距报告

不要立即开始改 SKILL.md。

第一阶段先输出：

```text
Azrng EFCore Skill Gap Report
```

对现有 skill 逐节审计，分三类列出：

1. **失实修正**：现有描述与当前源码不一致（签名、默认值、命名空间、版本号）
2. **缺口补齐**：源码有而 skill 未覆盖的能力
3. **冗余删减**：skill 有但源码已不存在、或与上游包 README 重复无增值的内容

每项标注证据（源码文件路径 / README 章节 / 测试文件）。

确认差距清单以后，再开始修改 skill 文件。

---

## 十一、当前已知差距（2026-08-17 初步探查）

> 2026-08-17 优化批次已处理下列全部条目（过程与结论见 `doc/rulelog/2026-06-28-azrng-efcore分页与实体基类描述修复.md` 的 2026-08-17 小节）。后续包升级重新优化时，按此清单逐项再核对。

以下为本次探查发现的候选差距，优化时逐项回源码核实后处理：

1. **AutoAudit 完全未覆盖**：`AddEFCoreAutoAudit`、`IAuditConfigBuilder` 全部 With* 配置、两种审计存储、Ignore 过滤家族、审计表 DDL 均不在 skill 中。补齐时明确其定位：可选能力，用户明确提出审计需求才接入
2. **NET10+ 条件批量更新未覆盖**：`ConditionalUpdateExtensions` 的 `SetPropertyIfTrue` / `SetPropertyIfNotNull` / `SetPropertyIfNotNullOrWhiteSpace` / `SetPropertyIf`
3. **批量更新重载覆盖情况待核实**：`IBaseRepository` 上 ExecuteUpdate 风格重载（NET7+/NET10+ 条件编译）
4. **注册入口不全**：非泛型 `AddEntityFramework`（默认 BaseDbContext 自动扫描）与 `AddEntityFrameworkFactory<TDbContext>` 未提及；`RepositoryFactory` 未提及
5. **多目标框架与驱动差异未提及**：net6~net10 多目标、net10 MySQL 改用 Microting 驱动，影响 AI 生成 csproj 与解释编译错误
6. **原生 SQL 深度待核实**：SKILL.md 仅一句带过，`SqlQueryDataTable` / `SqlQueryList` / `ExecuteScalar` 等在 references 的覆盖深度需对照 `OrigionSqlController` 与 `SqlQueryTest` 核实
7. **SqlMigration 高级能力未覆盖**：`LockProvider` 分布式锁、`SetInitVersionSetter<T>`、`IMigrationHandler` 扩展点
8. **连接串约定需澄清**：包不读配置键、由调用方传入（示例中键名各异），防止 AI 臆造强制键名

反向核实项（探查曾误报、已按源码纠正，skill 现状是对的）：

* `IdentityOperatorStatusEntity` 的属性名就是 `Deleted` / `Disabled`，`SetDeleted(name, dateTime)` 带操作人参数——不要"修正"成 `IsDeleted`

---

## 十二、最终验收

Skill 优化完成后，用它完成以下测试（可基于 nuget-packages 仓库的 test/ 或示例项目建验证工程）：

### Test 1

最小 PostgreSQL 接入：实体（继承 `IdentityOperatorStatusEntity`）+ `Etc` 配置 + `AddEntityFramework<TDbContext>` 注册 + `IBaseRepository` 四则 CRUD。

### Test 2

分页列表：`GetPageRequest` 子类 + `WhereIfNotNullOrWhiteSpace` 筛选 + 动态 `OrderBy` + DTO 投影 + `ToPageListAsync` + `GetQueryPageResult` 返回。

### Test 3

多仓储事务：`BeginTransactionScopeAsync` + `submit: false` + `CommitAsync`，外加 `GroupJoin` 左连接与 `GroupBy` 聚合各一处。

### Test 4

`AddEFCoreAutoAudit` 接入（含一个 `IgnoreEntity`）+ `AddSqlMigrationService` + `.AddAutoMigration()` + 一个符合命名规则的迁移脚本。

检查生成代码是否：

* 使用真实存在的包 API，签名与重载正确
* 条件编译 API 只出现在满足 TFM 门槛的项目里
* 可以编译（实际执行 `dotnet build`）
* 没有虚构配置项、配置键、扩展方法
* 没有混入 Dapper / SqlSugar / 原生 EF Core Migrations 等范围外方案

如果存在问题，修正 Skill，再重新测试。

最终交付：

```text
skills/dotnet-skills/azrng-efcore/
```

完整目录（用户本机 `~\.zcode\skills\azrng-efcore` 的安装副本另行同步，不作为修改目标）。

---

我建议你的工作流是：

```text
nuget-packages 源码 + README + 测试/示例
        ↓
① Repository Exploration（第二阶段清单）
        ↓
② API 清单（含 TFM 门槛标注）
        ↓
③ 高频抽象深度分析（仓储 / UoW / 分页）
        ↓
④ Example / Test 最佳实践提取
        ↓
⑤ Gap Report（对照现有 skill）
        ↓
⑥ SKILL.md 修订
        ↓
⑦ references 修订 / 新增
        ↓
⑧ dotnet build 编译测试
        ↓
⑨ 修复 Skill
```

特别是**"编译测试"一定要执行**（Windows 环境下禁止 `&&` 链式命令）。数据访问类 Skill 最大的问题不是 AI 不懂 EF Core，而是很容易写出：

```csharp
await _repo.InsertAsync(user);        // 想当然的仓储方法名
query.ToPagedList(pageIndex, pageSize); // 其他库的分页 API
```

这种**看起来非常合理，但不是本包家族真实 API** 的代码。让 AI 在真实工程上 `dotnet build`，能把 Skill 质量提高一个档次。

另外，保持现有 skill 的"源码型 Skill"定位而不是"百科全书型 Skill"：SKILL.md 控制在小规模（适用范围 / 工作原则 / 关键约束 / 决策指引 / references 索引），详细 API 签名与大型示例放 `references/`。这样 Agent 不需要每次加载几万字文档。
