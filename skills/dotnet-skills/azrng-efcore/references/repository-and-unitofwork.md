# Repository 与 UnitOfWork API

## 目录
1. IBaseRepository 查询方法
2. IBaseRepository 操作方法
3. 批量条件更新（.NET 10+）
4. UnitOfWork 事务（三种方式）
5. 原生 SQL
6. 分页

---

## 1. IBaseRepository 查询方法

命名空间 `Azrng.EFCore`。注入 `IBaseRepository<TEntity> where TEntity : IEntity`。

| 方法 | 说明 |
|---|---|
| `Entities` | 可追踪 IQueryable |
| `EntitiesNoTacking` | 不追踪 IQueryable（只读性能优） |
| `GetByIdAsync(object id)` | 按主键查 |
| `GetAsync(predicate, isTracking=false)` | 条件查单个（默认不追踪） |
| `GetListAsync(predicate, isTracking=false)` | 条件查列表 |
| `GetPageListAsync<T>(IQueryable<T> query, GetPageSortRequest vm)` | IQueryable 分页 |
| `AnyAsync(predicate?)` | 是否存在 |
| `CountAsync(predicate?)` / `CountLongAsync(predicate?)` | 计数 |

```csharp
var user = await _repo.GetAsync(x => x.Account == "admin");
var actives = await _repo.GetListAsync(x => !x.Deleted);
var exists = await _repo.AnyAsync(x => x.Email == email);
// 自定义 LINQ（追踪/排序/连接）
var q = _repo.Entities.Where(x => x.IsValid).OrderByDescending(x => x.CreateTime);
```

## 2. IBaseRepository 操作方法

| 方法 | 说明 |
|---|---|
| `AddAsync(entity, submit=false)` | 添加；`submit=true` 立即保存 |
| `AddAsync(entities, submit=false)` | 批量添加 |
| `UpdateAsync(entity, submit=false)` | 更新整实体 |
| `UpdateAsync(entities, submit=false)` | 批量更新 |
| `UpdateAsync(predicate, setter)` | 条件批量更新（见 §3） |
| `DeleteAsync(entity, submit=false)` | 删除 |
| `DeleteAsync(entities, submit=false)` | 批量删除 |
| `DeleteAsync(predicate)` | 按条件删除（查后删，自动保存） |

> `submit` 默认 false：该仓储调用不立即执行 `SaveChanges`。工作单元内多步写入需显式调用 `_unitOfWork.SaveChangesAsync()`；独立操作可传 `submit: true` 立即保存。

> **`DeleteAsync` 系列全部是物理删除**（`Remove` + SaveChanges），不是软删除。软删除场景（实体继承 `IdentityOperatorStatusEntity`）：① 实体无关联时 `SetDeleted("operator")` + `UpdateAsync(entity, submit: true)`；② Blazor 电路等长寿命 DbContext 下同一实体先建后删会触发 1:1 关系 fixup 异常，改用条件批量更新 `UpdateAsync(predicate, x => x.SetProperty(u => u.Deleted, true)...)`；③ 有外键子表的实体直接物理删除会因外键约束 500，先处理子表或改级联软删。

```csharp
await _repo.AddAsync(user, submit: true);              // 立即落库
await _repo.UpdateAsync(user, submit: true);
await _repo.DeleteAsync(x => x.Id == id);              // 条件删除自带保存
```

## 3. 批量条件更新（.NET 10+）

`UpdateAsync(predicate, Action<UpdateSettersBuilder<T>>)`，链式 `SetProperty*`：

```csharp
// 仅当值不为 null 才更新
await _repo.UpdateAsync(
    x => x.Id == id,
    x => x.SetPropertyIfNotNull(u => u.Email, email)
         .SetPropertyIfNotNull(u => u.Phone, phone));

// 字符串非空白才更新 + 无条件更新
await _repo.UpdateAsync(
    x => x.Id == id,
    x => x.SetPropertyIfNotNullOrWhiteSpace(u => u.UserName, name)
         .SetProperty(u => u.UpdateTime, DateTime.Now.ToUnspecifiedDateTime()));
```

| 方法 | 条件 |
|---|---|
| `SetProperty(prop, value)` | 无条件 |
| `SetPropertyIfTrue(cond, prop, value)` | cond 为 true |
| `SetPropertyIfNotNull(prop, value)` | value 非 null（引用类型） |
| `SetPropertyIfNotNullOrWhiteSpace(prop, value)` | 字符串非空白 |
| `SetPropertyIf(Func<object,bool>, prop, value)` | 自定义条件 |

## 4. UnitOfWork 事务（三种方式）

注入 `IUnitOfWork`（或 `IUnitOfWork<TDbContext>`，需 `AddUnitOfWork<T>()`）。

### 方式 1：Lambda（简单场景）
```csharp
await _unitOfWork.CommitTransactionAsync(async () =>
{
    await _orderRep.AddAsync(order);
    await _itemRep.AddAsync(item);
    await _unitOfWork.SaveChangesAsync();
});   // 异常自动回滚并抛出
```

### 方式 2：显式作用域（推荐）
```csharp
await using var scope = await _unitOfWork.BeginTransactionScopeAsync();
try
{
    await _orderRep.AddAsync(order);
    await _itemRep.AddAsync(item);
    await _unitOfWork.SaveChangesAsync();
    await scope.CommitAsync();
}
catch { await scope.RollbackAsync(); throw; }
// 未提交即 Dispose 会自动回滚
```

### 方式 3：手动事务
```csharp
await using var tran = await _unitOfWork.GetDatabase().BeginTransactionAsync();
try {
    await _orderRep.AddAsync(order);
    await _unitOfWork.SaveChangesAsync();
    await tran.CommitAsync();
} catch { await tran.RollbackAsync(); throw; }
```

> 方式 1/2 的事务提交不会自动调用 `SaveChanges`。无论仓储写操作使用默认 `submit:false` 还是显式 `submit:false`，都必须在事务提交前调用 `_unitOfWork.SaveChangesAsync()`；也可在写操作传 `submit:true`，使变更在事务内立即保存。仅调用事务的 `CommitAsync()` 会提交数据库事务，不会落库未保存的变更。

### InMemory provider 不支持事务（测试环境必读）

`BeginTransactionScopeAsync` / `CommitTransactionAsync` / `GetDatabase().BeginTransactionAsync()` 三种方式内部都走 `RelationalDatabaseFacadeExtensions`（关系型 facade）。测试项目用 `Common.EFCore.InMemory` 跑集成测试时，任何一种事务入口都会直接抛 `InvalidOperationException`（非关系型 provider 不支持事务），且 `ConfigureWarnings(x => x.Ignore(TransactionIgnoredWarning))` **只压告警、不解决 facade 抛错**（该告警配置针对的是 SaveChanges 时的事务忽略提示，不是 BeginTransaction 本身）。

应对（按优先级）：

1. **测试替身降级**：用 `DispatchProxy` 把 `ITransactionScope` 相关调用降级为无操作，业务代码保持真实事务写法。要点：拦截 `IUnitOfWork.BeginTransactionScopeAsync` 返回无操作 scope（`CommitAsync`/`RollbackAsync`/`DisposeAsync` 全部空转），业务层无需感知测试环境；
2. **换真实库**：Testcontainers / 本地 PostgreSQL 验证真实事务语义（含回滚隔离性），事务行为本身就是被测内容时必选；
3. 若测试必须保留 InMemory 且要验证多表写入顺序，只能把用例拆成单表断言（放弃跨表原子性验证）。

## 5. 原生 SQL（IUnitOfWork）

| 方法 | 说明 |
|---|---|
| `ExecuteSqlCommandAsync(sql, params)` | 执行非查询，返回影响行数 |
| `ExecuteScalarAsync(sql, params)` | 首行首列 |
| `SqlQuery<T>(FormattableString)` | 标量查询（.NET 7+） |
| `SqlQueryDataTableAsync(sql, params)` | 返回 DataTable |
| `SqlQueryListAsync<T>(sql, params)` | 映射到 List<T>（按属性名） |

```csharp
await _unitOfWork.ExecuteSqlCommandAsync("UPDATE users SET disabled=true WHERE id={0}", id);
var dt = await _unitOfWork.SqlQueryDataTableAsync("SELECT account, create_time FROM users");
```

## 6. 分页

通过 `GetPageSortRequest`/`GetPageRequest`（`Azrng.Core.Requests`）+ `GetPageListAsync`，或对 IQueryable 用 `ToPageListAsync` 扩展：

```csharp
// GetPageRequest 只有 PageIndex/PageSize/Keyword；需要排序用子类 GetPageSortRequest.SortContents
var req = new GetPageSortRequest
{
    PageIndex = 1,
    PageSize = 20,
    SortContents = new[] { new SortContent("CreateTime", SortEnum.Desc) }
};
var page = await _repo.GetPageListAsync(_repo.Entities.Where(x => !x.Deleted), req);
// page.List / page.Total
```
