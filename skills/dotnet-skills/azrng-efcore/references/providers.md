# Providers 对照（多数据库注入）

各 provider 包的注入扩展都是 `Microsoft.Extensions.DependencyInjection` 命名空间下的 `AddEntityFramework` / `AddEntityFramework<T>`，公共配置参数形态一致，仅连接串与可选的驱动 builder 委托不同。非内存 provider 的配置类型是基础包中的 `Azrng.EFCore.EfCoreConnectOption`，不是 provider 专属 options 类型。

## 对照表

| 数据库 | 包 | 注入驱动 | 连接串示例 | 备注 |
|---|---|---|---|---|
| PostgreSQL | `Common.EFCore.PostgresSql` | `UseNpgsql` | `Host=lo;Port=5432;Database=db;Username=postgres;Password=pwd` | `Schema` 生效（默认 public） |
| MySQL | `Common.EFCore.MySQL` | `UseMySql` | `Server=lo;Port=3306;Database=db;User=root;Password=pwd` | 自动 `ServerVersion.AutoDetect` |
| SQLite | `Common.EFCore.SQLite` | `UseSqlite` | `Data Source=app.db` 或 `:memory:` | 文件或内存 |
| SQLServer | `Common.EFCore.SQLServer` | `UseSqlServer` | `Server=lo;Database=db;User Id=sa;Password=pwd;TrustServerCertificate=True` | |
| 内存库 | `Common.EFCore.InMemory` | `UseInMemoryDatabase` | （用数据库名，非连接串） | 仅测试/原型 |

## 注入签名差异

非内存库公共签名（以下以 PostgreSQL 为例，第二个参数随 provider 不同）：
```csharp
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

services.AddEntityFramework<TDbContext>(
    Action<Azrng.EFCore.EfCoreConnectOption> config,
    Action<NpgsqlDbContextOptionsBuilder>? builder = null,
    Action<IServiceProvider, DbContextOptionsBuilder>? dbOpt = null);
```

第二个参数是 provider 专属的 options builder：MySQL 使用 `MySqlDbContextOptionsBuilder`，SQLite 使用 `SqliteDbContextOptionsBuilder`，SQLServer 使用 `SqlServerDbContextOptionsBuilder`。没有 provider 特有配置时可以省略该参数。

**InMemory 签名不同**（无需连接串，传数据库名）：
```csharp
services.AddEntityFramework<TDbContext>(string dataBaseName = "db", int workId = 1,
    Action<InMemoryDbContextOptionsBuilder>? builder = null);
```

## 各 provider 注入示例

### PostgreSQL
```csharp
builder.Services.AddEntityFramework<AppDbContext>(config =>
{
    config.ConnectionString = builder.Configuration.GetConnectionString("Default")!;
    config.Schema = "public";
});
```

### MySQL
```csharp
builder.Services.AddEntityFramework<AppDbContext>(config =>
{
    config.ConnectionString = builder.Configuration.GetConnectionString("Default")!;
});
// 内部已调用 ServerVersion.AutoDetect(connectionString)
```

### SQLite
```csharp
builder.Services.AddEntityFramework<AppDbContext>(config =>
{
    config.ConnectionString = "Data Source=app.db";
});
```

### SQLServer
```csharp
builder.Services.AddEntityFramework<AppDbContext>(config =>
{
    config.ConnectionString = builder.Configuration.GetConnectionString("Default")!;
});
```

### InMemory（测试）
```csharp
builder.Services.AddEntityFramework<AppDbContext>("test-db");
```

## 各注册了什么

调用 `AddEntityFramework<TDbContext>` 后自动注册：
- `TDbContext`（Scoped）+ `DbContext`（转发到同一实例，保证同 Scope 一致）
- `IBaseRepository<T>` → 对应 provider 的 `XxxRepository<T>`（Scoped）
- `IBaseRepository<T, TDbContext>`（多上下文用，InMemory 不注册此泛型）
- `IUnitOfWork` → `UnitOfWork<TDbContext>`（Scoped）

需要 `IUnitOfWork<TDbContext>` 时追加 `.AddUnitOfWork<TDbContext>()`（来自 `Azrng.EFCore` 基础包）。

> 非内存 provider 的 `AddEntityFramework<TDbContext>` 默认使用 `EfCoreConnectOption` 的节点号和蛇形命名策略，并据最终的 `config.WorkId` 内部初始化 `IdHelper`。不要在普通注册示例中把 `WorkId` 固定为 `1`；只有已经明确节点号分配策略时才显式设置。只有独立于 EF 注册的 Console 或工具程序，才需要额外调用基础包提供的 `AddIdHelper(workId)`。

## 各 provider 的 DbTypeId（影响时间列类型）

每个 provider 注入时调用 `EfCoreGlobalConfig.SetConfig(DatabaseType.XXX, ...)` 设全局数据库类型。这决定基类配置 `EntityTypeConfigurationIdentityOperator` 里 `IsUnTimeZoneDateTime(EfCoreGlobalConfig.DbType == DatabaseType.PostgresSql)` 是否生效：

- **PostgreSQL**：DbType = `PostgresSql` → 审计时间列自动 `timestamp without time zone`（符合 PG 要求）。
- **MySQL/SQLite/SQLServer/InMemory**：DbType 为各自类型（`MySql`/`Sqlite`/`SqlServer`/`InMemory`）→ 该条件为 false → 审计时间列**不会**被强制无时区化，由各自驱动默认类型决定（MySQL→`datetime(6)`，SQLite→`TEXT`，SQLServer→`datetime2`）。

> 时间类型陷阱只针对 PostgreSQL。其它库的时间列驱动会自动处理，无需 `IsUnTimeZoneDateTime`。详见 postgres-time-pitfall.md。
> 使用时下载最新版本的包即可。

## 多 DbContext

同一应用多数据库时分别注入，用时指定上下文：
```csharp
services.AddEntityFramework<MainDb>(c => c.ConnectionString = mainConn).AddUnitOfWork<MainDb>();
services.AddEntityFramework<LogDb>(c => c.ConnectionString = logConn).AddUnitOfWork<LogDb>();

// 注入：
IBaseRepository<User, MainDb> _userRep;
IUnitOfWork<MainDb> _mainUow;
```
