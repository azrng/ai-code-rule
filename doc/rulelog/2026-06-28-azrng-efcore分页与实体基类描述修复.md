## [2026-06-28 至 2026-07-29]：azrng-efcore API 示例与查询规则准确性修复

跟踪状态表：

| 文件夹名称 | 当前子任务状态 | 备注 |
|-----------|----------------|------|
| `skills/dotnet-skills/azrng-efcore` | `已完成` | 修复分页、实体基类、复杂查询与扩展方法的准确性问题；2026-08-17 接入 AutoAudit / SqlMigration 并完成全量源码校准（均以 `C:\Work\gitee\nuget-packages` 源码为准核对） |

### `skills/azrng-efcore`

#### 修改原因
审查 `azrng-efcore` skill 时，以 nuget 源码（`D:\Gitee\nuget-packages`）为准逐符号核对，发现两处与源码不符的准确性问题：

1. **分页示例使用了不存在的属性**：`references/repository-and-unitofwork.md` 的分页示例写了 `new GetPageRequest { PageIndex=..., PageSize=..., OrderField="CreateTime", OrderType="desc" }`，但源码 `Azrng.Core\Requests\GetPageRequest.cs` 中 `GetPageRequest` 只有 `PageIndex`/`PageSize`/`Keyword` 三个属性，**没有** `OrderField`/`OrderType`。排序信息在子类 `GetPageSortRequest.SortContents`（`SortContent[]`，元素 `new SortContent(sortName, SortEnum)`），照原示例写会编译失败。
2. **实体基类描述措辞含糊**：`references/entities-and-context.md` 继承体系图中 `IEntity` 标注为"空标记接口/基类"，但源码 `Common.EFCore\Entities\IEntity.cs` 中是 `public class IEntity {}`（空**类**，非接口），"接口/基类"并列会让 AI 误判为接口并尝试 `: IEntity, IFoo` 多接口继承写法。

#### 主要修改内容
1. **分页示例改用真实 API**：将 `new GetPageRequest { OrderField, OrderType }` 改为 `new GetPageSortRequest { PageIndex, PageSize, SortContents = new[]{ new SortContent("CreateTime", SortEnum.Desc) } }`，并补注释说明 `GetPageRequest` 无排序字段、排序需用子类 `SortContents`。
2. **实体基类描述明确化**：`IEntity` 标注由"空标记接口/基类"改为"空基类 class，所有实体可继承；非接口"。

#### 修改的文件
- `skills/azrng-efcore/references/repository-and-unitofwork.md`
- `skills/azrng-efcore/references/entities-and-context.md`

#### 当前子任务状态
- `已完成`

#### 校验说明
- 上述两处修复后，全 `skills/` 目录复查 `OrderField`/`OrderType`/`空标记接口` 关键词，无残留。
- 源码核对结论：`azrng-efcore` skill 其余 API（包名/命名空间、`AddEntityFramework` 注册项、`EntitiesNoTacking` 拼写、`UpdateAsync(predicate,setter)` 的 .NET 10+ 限制、`app_version_log` 表名、`SetCreator/Updater/Deleted/Disabled` 签名等）均与源码一致，无需改动。

---

### 2026-07-29：复杂查询与扩展方法参考复审

#### 修改原因
新增 `references/querying-and-extensions.md` 后，以 `C:\Work\gitee\nuget-packages` 源码逐项复审，发现部分表述会误导调用方的排序语义、跨仓储连接前提和分页执行方式。

#### 主要修改内容
1. **多字段排序**：明确 `SortContent[]` 当前实现会重复调用 `OrderBy`，最终只保留最后一项排序；多字段排序改为仅推荐 `FiledOrderParam[]`。
2. **跨仓储连接**：补充 `Join` / `GroupJoin` 两侧查询必须来自同一个 `DbContext` 实例；明确 `GroupJoin` 需配合 `SelectMany(...DefaultIfEmpty())` 才构成左连接。
3. **查询与映射边界**：将 `EntitiesNoTacking` 从绝对限制调整为默认规则，说明脱离跟踪实体传入 `UpdateAsync` 会触发全字段更新；补充 `SelectMapper` 的无参构造、可写属性和类型兼容前提。
4. **分页与计数**：明确 `ToPageListAsync` 为两条 SQL，`CountBy` 使用同步 `Count()`。
5. **元数据**：为 `SKILL.md` 补充 `version: 1.1.0` 和 `last_updated: 2026-07-29`。

#### 修改的文件
- `skills/azrng-efcore/SKILL.md`
- `skills/azrng-efcore/references/querying-and-extensions.md`
- `doc/AI-Rules-Audit-Log.md`
- `doc/rulelog/2026-06-28-azrng-efcore分页与实体基类描述修复.md`

#### 当前子任务状态
- `已完成`

---

### 2026-08-17：接入 AutoAudit / SqlMigration 详情与源码校准优化

#### 修改原因
用户要求优化 azrng-efcore skill 并明确四点口径：AutoAudit 为可选能力（用户明确提出审计需求才接入）、PostgreSQL 默认使用无时区时间（列声明 `IsUnTimeZoneDateTime`）、Extensions 在适用场景必须优先使用、实体基类默认 `long` 主键且提供泛型 `TKey` 版本。先产出任务书 `doc/skillsDoc/azrngEfcoreSkills.md`（模仿 blazorSkills.md），再按"Gap Report → 修订"流程以 `C:\Work\gitee\nuget-packages` 源码逐项核对后执行。

#### 主要修改内容
1. **新增 `references/auto-audit.md`**：`AddEFCoreAutoAudit` 标准接入（枚举成员拼写为 `DatabaseType.PostgresSql`，包 README 的 `PostgreSql` 是笔误）、审计表启动自动创建（IStartupFilter 幂等建表，无需手动）、`IAuditConfigBuilder` / `AuditExtensions` 全量配置表、敏感属性自动脱敏清单、文件存储 `AuditFileStore` 的启动风险提示。
2. **新增 `references/sql-migration.md`**：`SqlMigrationOption` 全配置项（含 `LockProvider` 分布式锁、`SetInitVersionSetter<T>`、`VersionLog` 默认表名 `app_version_log`）、`IMigrationHandler` 六个生命周期回调、多库注册约定。
3. **失实修正 3 处**：`repository-and-unitofwork.md` 分页返回值 `page.List / page.Total` → `page.Rows / page.PageInfo.Total`（源码 `GetQueryPageResult<T>` 无 List/Total 属性）；`providers.md` 连接串示例 `Host=lo` / `Server=lo` → `localhost`；`entities-and-context.md` 继承树方向改为"非泛型版本继承该级 `<long>` 泛型版本"。
4. **口径强化（用户四点）**：Extensions 必须优先使用（SKILL.md 分页节 + querying-and-extensions.md 开头）；PostgreSQL 默认无时区时间（SKILL.md 步骤 3 + entities-and-context.md §3）；基类默认 `long` 主键、可选泛型 `TKey`（SKILL.md 决策指引 + entities-and-context.md §1）；AutoAudit 可选、用户提及才接入（SKILL.md 新节 + auto-audit.md 定位说明）。
5. **`providers.md` 补缺口**：非泛型 `AddEntityFramework`（内部转发 `AddEntityFramework<BaseDbContext>` 自动扫描）、`AddEntityFrameworkFactory<T>`、`RepositoryFactory.GenerateRepository`、net6~net10 多目标与 net10 MySQL 驱动差异、连接串由调用方传入（包不读配置键）。
6. **`SKILL.md`**：新增"自动审计（可选）"一节、SqlMigration 节指路 sql-migration.md、决策指引补主键 / schema 变更 / 审计三行、`Common.EFCore.PostgresSql` 版本号 1.7.0 → 1.7.1、front matter `version: 1.2.0` / `last_updated: 2026-08-17`、description 补审计触发词。
7. **配套新增任务书** `doc/skillsDoc/azrngEfcoreSkills.md`。

#### 修改的文件
- `skills/dotnet-skills/azrng-efcore/SKILL.md`
- `skills/dotnet-skills/azrng-efcore/references/auto-audit.md`（新增）
- `skills/dotnet-skills/azrng-efcore/references/sql-migration.md`（新增）
- `skills/dotnet-skills/azrng-efcore/references/providers.md`
- `skills/dotnet-skills/azrng-efcore/references/entities-and-context.md`
- `skills/dotnet-skills/azrng-efcore/references/repository-and-unitofwork.md`
- `skills/dotnet-skills/azrng-efcore/references/querying-and-extensions.md`
- `doc/skillsDoc/azrngEfcoreSkills.md`（新增）

#### 当前子任务状态
- `已完成`

#### 校验说明
- 新增 / 修改涉及的全部 API 均逐一回 nuget-packages 源码核实：`AddEFCoreAutoAudit` 签名与第三参必填、`IAuditConfigBuilder` 全部方法、`AuditExtensions` 全部扩展、`AutoAuditStartupFilter` 自动建表逻辑、`SqlMigrationOption` / `IMigrationHandler`、`AddEntityFramework` 非泛型与 `AddEntityFrameworkFactory<T>`、`GetQueryPageResult` 属性、`DatabaseType` 枚举成员。
- 反向确认未误改：`Deleted` / `Disabled` 属性名、`SetDeleted(name, dateTime)` / `SetDisabled(bool, name, dateTime)` 签名维持原文（探查阶段曾误报为 `IsDeleted`，源码核实后确认 skill 原文正确）。

---

### 2026-08-17（追加）：编译测试发现的 SetPropertyIfNotNull 限制修正

#### 修改原因
在 `D:\temp\AzrngEfcoreSkillTest`（net10）按 skill 编写测试工程并 `dotnet build`，发现 `SetPropertyIfNotNull` 对 `string?` 属性编译不过。回源码 `ConditionalUpdateExtensions.cs` 确认该泛型方法约束为 `where TProperty : class`，而 `string` 在 nullable 上下文里被当作 `string?` 时，编译器对 `TProperty` 推断与约束匹配与预期不同，导致 CS1061 / CS1929。

#### 主要修改内容
1. **`references/repository-and-unitofwork.md` §3**：示例改用引用类型属性（`Avatar`）演示 `SetPropertyIfNotNull`；新增"字符串属性统一用 `SetPropertyIfNotNullOrWhiteSpace`、自定义判定用 `SetPropertyIf<T,TProperty>(Func,...)`"提示；方法表补"约束"列。
2. **`references/querying-and-extensions.md` §9**：.NET 10+ 示例明确区分 string 属性（用 `SetPropertyIfNotNullOrWhiteSpace`）与引用类型属性（用 `SetPropertyIfNotNull`）；注明 `UpdateSettersBuilder<T>` 在 `Microsoft.EntityFrameworkCore.Query` 命名空间。
3. **`SetPropertyIf` 表格行**：参数类型 `Func<object,bool>` 修正为 `Func<TProperty,bool>`。

#### 修改的文件
- `skills/dotnet-skills/azrng-efcore/references/repository-and-unitofwork.md`
- `skills/dotnet-skills/azrng-efcore/references/querying-and-extensions.md`

#### 当前子任务状态
- `已完成`

#### 校验说明
- 测试工程路径 `D:\temp\AzrngEfcoreSkillTest\App`，net10.0，安装 `Common.EFCore` 1.7.0 + `Common.EFCore.PostgresSql` 1.7.1 + `Azrng.EFCore.AutoAudit` 1.0.0-beat1 + `Azrng.SqlMigration` 0.5.0（本地 nuget 源指向 nuget-packages 本地构建产物）；包含实体基类（long 与泛型 Guid 各一）、`*Etc` 配置类、自定义 DbContext（`ApplyConfigurationsFromAssembly` + `HasDefaultSchema`）、`UserService`（条件筛选 / 动态排序 / DTO 投影 / `ToPageListAsync` + `GetQueryPageResult` / `SetPropertyIf*` / 左连接 `GroupJoin`+`SelectMany(DefaultIfEmpty)` / `GroupBy` 聚合 / 显式事务作用域），Program.cs 接入 AutoAudit + SqlMigration。
- 修正后 `dotnet build`：0 错误，仅 1 个无关 CS8619 警告（仓储 `GetAsync` 返回值可空性，属包接口签名而非 skill 描述问题）。
