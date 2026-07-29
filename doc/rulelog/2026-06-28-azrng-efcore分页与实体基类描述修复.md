## [2026-06-28 至 2026-07-29]：azrng-efcore API 示例与查询规则准确性修复

跟踪状态表：

| 文件夹名称 | 当前子任务状态 | 备注 |
|-----------|----------------|------|
| `skills/azrng-efcore` | `已完成` | 修复分页、实体基类、复杂查询与扩展方法的准确性问题（均以 `C:\Work\gitee\nuget-packages` 源码为准核对） |

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
