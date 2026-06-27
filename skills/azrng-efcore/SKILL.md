---
name: azrng-efcore
description: Guide for using the Azrng Common.EFCore family of NuGet packages to operate databases in .NET apps. Use when building or modifying data-access code that relies on these packages — installing/injecting the EFCore provider (PostgreSQL, MySQL, SQLite, SQLServer, InMemory), defining entities with the IdentityBaseEntity/IdentityOperatorEntity base classes, calling IBaseRepository CRUD and IUnitOfWork transactions, configuring SQL migrations, or diagnosing PostgreSQL timestamp errors like "Cannot write DateTime with Kind=Unspecified to timestamp with time zone".
---

# Azrng EFCore (Common.EFCore) 数据访问指南

指导如何用 `Common.EFCore` 系列包在 .NET 项目中操作数据库（仓储 / 工作单元 / 迁移）。

## 包与适用范围

`Common.EFCore`（基础包）+ 按数据库选一个 provider 包。所有 provider 共享同一套 `IBaseRepository<T>` / `IUnitOfWork` 抽象，provider 包只负责注入与底层驱动。

| 数据库 | Provider 包 | 命名空间 | 驱动 |
|---|---|---|---|
| PostgreSQL | `Common.EFCore.PostgresSql` | `Azrng.EFCore.PostgresSql` | Npgsql |
| MySQL | `Common.EFCore.MySQL` | `Azrng.EFCore.MySQL` | Pomelo |
| SQLite | `Common.EFCore.SQLite` | `Azrng.EFCore.SQLite` | Microsoft.Data.Sqlite |
| SQLServer | `Common.EFCore.SQLServer` | `Azrng.EFCore.SQLServer` | Microsoft.Data.SqlClient |
| 内存库(测试) | `Common.EFCore.InMemory` | `Azrng.EFCore.InMemory` | EFCore.InMemory |

> 包名注意：实际 NuGet 包 id 是 `Common.EFCore.PostgresSql`（非 PostgreSQL），按上表原样引用。

## 核心工作流（5 步）

### 1. 安装包

数据访问项目（含 DbContext 的项目）引用：基础包 `Common.EFCore` + 对应 provider 包。实体所在项目引用 `Common.EFCore`（取基类/时间扩展）即可。

```xml
<!-- EntityFrameworkal 项目 -->
<PackageReference Include="Common.EFCore" Version="1.7.0" />
<PackageReference Include="Common.EFCore.PostgresSql" Version="1.7.0" />
<!-- IDomain 项目（仅实体） -->
<PackageReference Include="Common.EFCore" Version="1.7.0" />
```

### 2. 注入（Program.cs）

provider 包的 `AddEntityFramework<TDbContext>` 扩展在 `Microsoft.Extensions.DependencyInjection` 命名空间下，注册 DbContext + 仓储 + 工作单元。按需追加 `.AddUnitOfWork<TDbContext>()`。

```csharp
using Azrng.EFCore;                       // AddUnitOfWork
// 按数据库引入 provider 命名空间（AddEntityFramework<T> 来自它）
// PostgreSQL 无需显式 using（扩展在 Microsoft.Extensions.DependencyInjection）

builder.Services.AddEntityFramework<AppDbContext>(config =>
{
    config.ConnectionString = builder.Configuration.GetConnectionString("Default")!;
    config.Schema = "public";             // PostgreSQL schema；其它库可留空
    config.WorkId = 1;                    // 分布式 Id 生成机器号
    config.IsSnakeCaseNaming = true;      // 是否蛇形命名（默认 true）
    config.UseOldUpdateColumn = false;    // 是否用旧字段名 creator/modifyer/modify_time
}).AddUnitOfWork<AppDbContext>();
```

> 自定义 DbContext 时不要忘记重写 `OnModelCreating` 调用 `ApplyConfigurationsFromAssembly`，或继承 `BaseDbContext`（自动应用所有 `IEntityTypeConfiguration`）。详见 references/entities-and-context.md。

### 3. 定义实体

优先用包提供的基类，自动带主键（long，IdHelper 雪花 ID）和审计字段：

```csharp
using Azrng.EFCore.Entities;
using Azrng.Core.Extension;   // ToUnspecifiedDateTime

public class User : IdentityOperatorStatusEntity   // Id+审计+软删除/禁用
{
    public string Account { get; set; } = string.Empty;

    public User() { }   // EF 需要
    public User(string account) { Account = account; SetCreator("system"); }
}
```

**时间字段统一用无时区值**：`DateTime.Now.ToUnspecifiedDateTime()`，配合 `IsUnTimeZoneDateTime()` 列声明。完整基类对照与选择见 references/entities-and-context.md。

### 4. CRUD（注入 IBaseRepository<T>）

```csharp
public class UserService
{
    private readonly IBaseRepository<User> _repo;
    public UserService(IBaseRepository<User> repo) => _repo = repo;

    public Task<User?> GetAsync(long id) => _repo.GetAsync(x => x.Id == id);
    public Task AddAsync(User user) => _repo.AddAsync(user, submit: true);   // submit=true 立即保存
    public Task<List<User>> ActiveAsync() => _repo.GetListAsync(x => !x.Deleted);
    public Task<bool> ExistsAsync(string account) => _repo.AnyAsync(x => x.Account == account);
}
```

> `AddAsync/UpdateAsync/DeleteAsync(entity, submit)` 第二参 `submit=false` 时不立即保存，配合工作单元统一提交。完整 API（查询/分页/批量更新/原生 SQL）见 references/repository-and-unitofwork.md。

### 5. 事务（注入 IUnitOfWork）

多写操作原子化用工作单元，**推荐显式作用域**（Dispose 自动回滚未提交事务）：

```csharp
await using var scope = await _unitOfWork.BeginTransactionScopeAsync();
try {
    await _orderRep.AddAsync(order);
    await _itemRep.AddRangeAsync(items);
    await scope.CommitAsync();
} catch { await scope.RollbackAsync(); throw; }
```

三种事务方式对比见 references/repository-and-unitofwork.md。

## 决策指引（AI 必读）

- **选 provider**：按目标数据库选包，注入方式几乎一致（仅连接串与可选 builder 委托不同）。对照表见 references/providers.md。
- **要不要 BaseDbContext**：实体用了配置类（`IEntityTypeConfiguration`）就继承 `BaseDbContext`（自动加载配置）；否则普通 DbContext 手写 `OnModelCreating`。
- **软删除**：用 `IdentityOperatorStatusEntity`（带 `Deleted`/`Disabled`），查询手动加 `!x.Deleted` 过滤（包不自动全局过滤）。
- **遇到 PostgreSQL 时间报错**：先看 references/postgres-time-pitfall.md —— 这是本系列包最常见的崩溃根因，三层（模型/赋值/DDL）必须一致。

## 关键陷阱（高频）

**PostgreSQL `Cannot write DateTime with Kind=Unspecified to timestamp with time zone`** —— 导致所有 UPDATE（含读回再写）崩溃。根因：Npgsql 默认把 `DateTime` 当 `timestamptz`，而实体时间值是 Unspecified。**三层必须一致**：
1. 模型层：`builder.Property(x => x.CreateTime).IsUnTimeZoneDateTime();`
2. 赋值层：`DateTime.Now.ToUnspecifiedDateTime()`
3. DDL 层：迁移脚本列用 `timestamp without time zone`

诊断与完整修复步骤见 references/postgres-time-pitfall.md。
