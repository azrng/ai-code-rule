## [2026-06-28]：azrng-efcore 分页示例与实体基类描述修复

跟踪状态表：

| 文件夹名称 | 当前子任务状态 | 备注 |
|-----------|----------------|------|
| `skills/azrng-efcore` | `已完成` | 修复分页示例使用了不存在的属性、实体基类描述措辞含糊两处准确性问题（均以 `D:\Gitee\nuget-packages` 源码为准核对） |

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
