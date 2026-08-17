# 复杂查询（表连接 / 分组）与 IQueryable 扩展

> 本系列包自带大量 `IQueryable` 扩展方法。条件拼接、动态排序、DTO 映射、计数和分页在适用场景**必须优先使用扩展**，不要手写等价的 if-Where、switch-OrderBy、Count+Skip+Take 逻辑；只有字段重命名、计算或跨导航属性投影等扩展覆盖不了的场景，才用强类型 `Select` 手写。扩展分属两个命名空间：
> - `Azrng.Core.Extension`（`QueryableExtensions.cs`）—— 排序、动态 where、DTO 映射、CountBy、PagedBy
> - `Azrng.EFCore.Extensions`（`Common.EFCore/Extensions/`）—— 分页 ToPageListAsync、原生 SQL、条件更新、时间列配置

## 目录
1. 查询入口约定（Entities vs EntitiesNoTacking）
2. 表连接（GroupJoin 左连接）
3. 自动 DTO 映射（SelectMapper）
4. 动态排序（OrderBy）
5. 动态条件筛选（EqualWhere / LessWhere / GreaterWhere）
6. 条件 where（WhereIf 系列 / WhereAny）
7. 计数（CountBy）
8. 分页（PagedBy / ToPageListAsync）
9. 条件批量更新扩展（.NET 7-9 AppendIf / .NET 10+ SetPropertyIf*）

---

## 1. 查询入口约定（Entities vs EntitiesNoTacking）

`IBaseRepository<TEntity>` 暴露两个查询入口：

| 入口 | 实现 | 用途 |
|---|---|---|
| `Entities` | `_dbContext.Set<T>()` | **会追踪**。需要继续修改已加载实体、依赖 ChangeTracker，或当前 DbContext 已跟踪同一主键时用 |
| `EntitiesNoTacking` | `_dbContext.Set<T>().AsNoTracking()` | **不追踪**。只读 / 投影 / 连接 / 分组查询的默认首选（性能优） |

**规则：只读查询默认从 `EntitiesNoTacking` 出发。** 需要 ChangeTracker 时使用 `Entities`。脱离跟踪的实体也能传给 `UpdateAsync`，但该方法会将实体标记为全字段更新；部分更新优先使用追踪实体或条件批量更新。

```csharp
// ✅ 只读：无追踪
var list = await _userRep.EntitiesNoTacking.Where(x => !x.Deleted).ToListAsync();

// ✅ 读后改：追踪
var user = await _userRep.Entities.FirstAsync(x => x.Id == id);
user.Account = "new";
await _userRep.UpdateAsync(user, submit: true);
```

---

## 2. 表连接（GroupJoin 左连接）

**表连接的标准写法**：两个查询必须来自同一个 `DbContext` 实例。从主表仓储的 `EntitiesNoTacking` 出发，`GroupJoin` 关联另一个仓储的 `EntitiesNoTacking`（可先 `.Where(...)` 预筛选），再 `SelectMany(...DefaultIfEmpty())` 摊平成左连接。

`GroupJoin` 本身的结果是「主表行 + 匹配集合」；只有接上 `SelectMany(...DefaultIfEmpty())` 才是左连接。此时主表行全部保留，关联表无匹配时为 null。关联表常先过滤（如「只取当前用户的收藏」），既缩小连接范围又避免一主多从被放大。

```csharp
using Microsoft.EntityFrameworkCore;

// 模型列表 + 当前用户是否收藏：左连接，关联表先按用户筛选
var list = await _modelRepository.EntitiesNoTacking
    .GroupJoin(
        _favoriteRepository.EntitiesNoTacking.Where(t => t.UserId == currentUserId),  // 预筛选关联表
        model => model.Id,
        favorite => favorite.ModelId,
        (model, favorites) => new { model, favorites })   // favorites = 该 model 匹配到的收藏集合
    .SelectMany(
        x => x.favorites.DefaultIfEmpty(),                // 摊平成行；无匹配 → null
        (x, fav) => new ModelListDto
        {
            Id = x.model.Id,
            Name = x.model.Name,
            IsFavorited = fav != null                      // 左连接：无匹配为 false
        })
    .ToListAsync();
```

> `GroupJoin` 第 4 参结果是「主表行 + 匹配到的关联集合」。要摊平成左连接就接 `SelectMany + DefaultIfEmpty`；若想保留「每行一个关联集合」直接在结果里取 `x.favorites` 即可，不必 `SelectMany`。

### 2.1 内连接（Join）

只要两侧都有匹配的行（丢掉不匹配的）用 `Join`：
```csharp
var list = await _userRep.EntitiesNoTacking
    .Join(_userAddressRep.EntitiesNoTacking, u => u.Id, a => a.UserId,
          (u, a) => new { Account = u.Account, Address = a.Address })
    .ToListAsync();
```

### 2.2 聚合分组（GroupBy）

统计 / 汇总用 `GroupBy(...).Select(...)`，聚合函数（Count/Sum/Max…）翻译成 SQL：
```csharp
// 单表分组
var daily = await _orderRep.EntitiesNoTacking
    .Where(o => !o.Deleted)
    .GroupBy(o => o.CreateTime.Date)
    .Select(g => new { Day = g.Key, Count = g.Count(), Sum = g.Sum(o => o.Amount) })
    .ToListAsync();

// 连表后再分组：先 Join 再 GroupBy
var deptStats = await _orderRep.EntitiesNoTacking
    .Join(_userRep.EntitiesNoTacking, o => o.UserId, u => u.Id, (o, u) => new { o, u })
    .GroupBy(x => x.u.DepartmentId)
    .Select(g => new { DepartmentId = g.Key, OrderCount = g.Count(), TotalAmount = g.Sum(x => x.o.Amount) })
    .ToListAsync();
```

### 2.3 导航属性（有关系时最简）

实体配置了导航属性时，直接在 `Select` 里跨表取字段，EF 自动生成 JOIN，不用手写 `Join`/`GroupJoin`：
```csharp
var orderDtos = await _orderRep.EntitiesNoTacking
    .Where(o => !o.Deleted)
    .Select(o => new OrderListDto
    {
        Id      = o.Id,
        Amount  = o.Amount,
        Account = o.User.Account,            // 导航属性 → 自动 JOIN
        Dept    = o.User.Department.Name     // 多级导航 → 自动 JOIN
    })
    .ToListAsync();
```

> **注意**：连接 / 分组后都是只读投影，务必从 `EntitiesNoTacking` 出发，不要追踪这些中间匿名对象。

---

## 3. 自动 DTO 映射（SelectMapper）

`Azrng.Core.Extension`，`using Azrng.Core.Extension;`。按**同名、类型兼容的可写属性**把 `T` 映射成 `Tm`。实现会在构造查询表达式时读取类型属性；对 EF Core 关系型 provider，生成的 `Select` 可翻译为 SQL 列投影，而非逐行物化后再映射：

```csharp
// User → UserDto（只映射两边同名的属性）
var dtos = await _userRep.EntitiesNoTacking
    .Where(u => !u.Deleted)
    .SelectMapper<User, UserDto>()
    .ToListAsync();
```

| 适用 | 不适用 |
|---|---|
| 实体与 DTO 字段名大量一致 | 需要重命名 / 计算 / 导航属性取值 |
| 一次取整张表的同名字段 | 只取少数字段（用 `Select` 手写更省） |

> 局限：目标类型必须可无参构造，目标属性必须可写，且同名属性的类型必须兼容；名字不同、需要转换或来自关联表的字段，用 `Select` 手写投影。

---

## 4. 动态排序（OrderBy）

`Azrng.Core.Extension`，`using Azrng.Core.Extension;`。支持字符串列名；多字段排序使用 `FiledOrderParam[]`，适合前端传排序字段 / 方向的场景。

```csharp
// 字符串列名 + 是否升序
var q = _userRep.EntitiesNoTacking.OrderBy(nameof(User.CreateTime), isAsc: false);

// 强类型列 + SortEnum
var q2 = _userRep.EntitiesNoTacking.OrderBy(u => u.CreateTime, SortEnum.Desc);
// 或 bool：OrderBy(u => u.CreateTime, isAsc: false)

// 多字段 FiledOrderParam（字符串列名 + IsAsc）
var q3 = _userRep.EntitiesNoTacking.OrderBy(
    new FiledOrderParam { PropertyName = nameof(User.Deleted), IsAsc = true },
    new FiledOrderParam { PropertyName = nameof(User.CreateTime), IsAsc = false });
```

| 重载 | 入参 |
|---|---|
| `OrderBy(string sortField, bool isAsc = true)` | 字符串列名 + 升降序 |
| `OrderBy(keySelector, bool isAsc)` / `OrderBy(keySelector, SortEnum)` | 强类型列 |
| `OrderBy(SortContent)` | 单字段 `SortContent(SortName, SortEnum)` |
| `OrderBy(params FiledOrderParam[])` | `FiledOrderParam { PropertyName, IsAsc }` |

> `SortContent` / `FiledOrderParam` / `SortEnum` 在 `Azrng.Core.Requests`。`OrderBy(params SortContent[])` 虽可接收多个参数，但当前源码会重复调用 `OrderBy`，最终只保留最后一项排序，不能用于多字段排序。前端传入字段名时应先按业务允许列表校验，避免访问无效或不应暴露的属性。

---

## 5. 动态条件筛选（EqualWhere / LessWhere / GreaterWhere）

`Azrng.Core.Extension`，`using Azrng.Core.Extension;`。按**字符串列名**做等值 / 小于 / 大于筛选（适合列名也是动态传参的场景）：

```csharp
// 按 "Age" 大于 18
var q = _userRep.EntitiesNoTacking.GreaterWhere(nameof(User.Age), 18);
// 按 "Status" 等于 1
var q2 = _userRep.EntitiesNoTacking.EqualWhere(nameof(User.Status), 1);
// 按 "CreateTime" 小于某时间
var q3 = _userRep.EntitiesNoTacking.LessWhere(nameof(User.CreateTime), endTime);
```

| 扩展 | 含义 |
|---|---|
| `EqualWhere(string field, object value)` | 等于（`field == value`） |
| `LessWhere(string field, object value)` | 小于（`field < value`） |
| `GreaterWhere(string field, object value)` | 大于（`field > value`） |

> 列名已知 / 条件可控时，优先用强类型 `Where(x => x.Age > 18)`；只有列名是运行期字符串时才用这套。注意小于等于 / 大于等于没有对应扩展。

---

## 6. 条件 where（WhereIf 系列 / WhereAny）

`Azrng.Core.Extension`，`using Azrng.Core.Extension;`。替代手写 `if (cond) query = query.Where(...)`：

| 扩展 | 触发条件 | 典型场景 |
|---|---|---|
| `WhereIfNotNullOrWhiteSpace(string? value, predicate)` | 值非空 / 非空白 | 列表筛选框（最常用） |
| `WhereIfNotNull<T,F>(F? value, predicate)` | 值非 null | 值类型 / 对象筛选 |
| `WhereIfTrue(bool cond, predicate)` / `QueryableWhereIf(cond, predicate)` | 任意布尔 | 开关类条件 |
| `WhereAny(params predicates)` | 任一成立（OR） | 多条件或查询 |

```csharp
var q = _userRep.EntitiesNoTacking.Where(u => !u.Deleted)
    .WhereIfNotNullOrWhiteSpace(request.Account, u => u.Account.Contains(request.Account!))
    .WhereIfNotNull(request.DeptId, u => u.DepartmentId == request.DeptId)
    .WhereIfTrue(request.ActiveOnly, u => !u.Disabled);
```

---

## 7. 计数（CountBy）

`Azrng.Core.Extension`，`using Azrng.Core.Extension;`。`out int totalCount` 同步计算总数，同时返回 queryable 继续链式：

```csharp
var total = 0;
var page = await _userRep.EntitiesNoTacking
    .Where(u => !u.Deleted)
    .OrderByDescending(u => u.CreateTime)
    .CountBy(out total)        // 算总数，返回原 queryable
    .Skip(10).Take(20)
    .ToListAsync();
// total = 全部满足条件的行数
```

> `CountBy` 内部调用同步 `Count()`。异步接口通常用 `ToPageListAsync(request, total)` 执行 Count + 分页（见 §8）；`CountBy` 用于需要自己控制分页或后续再加工 queryable 的场景。

---

## 8. 分页（PagedBy / ToPageListAsync）

两种分页扩展，按「要不要同时算总数」选：

**`ToPageListAsync`**（`Azrng.EFCore.Extensions`，`using Azrng.EFCore.Extensions;`）—— 一个扩展调用内依次执行 `CountAsync` 和 `Skip/Take` 查询（共两条 SQL），**推荐**：
```csharp
var total = new RefAsync<int>();   // using Azrng.Core.CommonDto;
var list = await _userRep.EntitiesNoTacking
    .Where(u => !u.Deleted)
    .OrderByDescending(u => u.CreateTime)
    .ToPageListAsync(request, total);   // request: GetPageRequest；也算总数

// 不要总数：
var page = await query.ToPageListAsync(request);
var page2 = await query.ToPageListAsync(pageIndex, pageSize);
```

**`PagedBy`**（`Azrng.Core.Extension`）—— 只分页不算总数：
```csharp
var page = _userRep.EntitiesNoTacking.PagedBy(request);        // GetPageRequest
var page2 = _userRep.EntitiesNoTacking.PagedBy(pageIndex: 1, pageSize: 20);
// 要总数用 out 重载：PagedBy(request, out int total) / PagedBy(idx, size, out total)
```

---

## 9. 条件批量更新扩展

配合 `_repo.UpdateAsync(predicate, setter)`（见 repository-and-unitofwork.md §3）。按目标框架版本用不同扩展：

**.NET 10+**（`Azrng.EFCore.Extensions`，`using Azrng.EFCore.Extensions;`）—— `SetPropertyIf*` 链式（`UpdateSettersBuilder<T>` 在 `Microsoft.EntityFrameworkCore.Query` 命名空间）：
```csharp
// Email / Name 都是 string：字符串属性统一用 SetPropertyIfNotNullOrWhiteSpace（SetPropertyIfNotNull 约束 TProperty : class，不匹配 nullable string）
// Avatar 是引用类型（class）：可用 SetPropertyIfNotNull
// Disabled 是 bool：用 SetPropertyIfTrue
await _repo.UpdateAsync(
    x => x.Id == id,
    x => x.SetPropertyIfNotNullOrWhiteSpace(u => u.Email, email)   // 字符串非空白
         .SetPropertyIfNotNullOrWhiteSpace(u => u.Name, name)     // 字符串非空白
         .SetPropertyIfNotNull(u => u.Avatar, avatar)              // 引用类型非 null
         .SetPropertyIfTrue(needLock, u => u.Disabled, true));      // 布尔条件
```

**.NET 7-9**（`Azrng.EFCore.Extensions`）—— 表达式版本用 `AppendIf` 按条件拼 set：
```csharp
using Microsoft.EntityFrameworkCore.Query;   // SetPropertyCalls<T>
using System.Linq.Expressions;              // Expression<>
using Azrng.EFCore.Extensions;              // AppendIf
using Azrng.Core.Extension;                 // ToUnspecifiedDateTime / IsNullOrWhiteSpace

// .NET 7-9 的 UpdateAsync 第二参类型是 Expression<Func<SetPropertyCalls<T>, SetPropertyCalls<T>>>
// AppendIf 是该「表达式」上的扩展 → 必须在变量上链式调用，不能在 lambda 体内直接 .AppendIf()
Expression<Func<SetPropertyCalls<User>, SetPropertyCalls<User>>> setter =
    s => s.SetProperty(u => u.UpdateTime, DateTime.Now.ToUnspecifiedDateTime());

setter = setter.AppendIf(email is not null, s => s.SetProperty(u => u.Email, email!))
               .AppendIf(name.IsNotNullOrWhiteSpace(), s => s.SetProperty(u => u.Name, name!));

await _userRep.UpdateAsync(x => x.Id == id, setter);
```

> `.NET 10+` 用 `UpdateSettersBuilder` + `SetPropertyIf*`；`.NET 7-9` 用 `Expression<Func<SetPropertyCalls<T>,...>>` + `AppendIf`。两套互不兼容，按项目 TargetFramework 选。
