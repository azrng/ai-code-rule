# Providers 对照（多数据库注入）

各 provider 包的注入扩展都是 `Microsoft.Extensions.DependencyInjection` 命名空间下的 `AddEntityFramework` / `AddEntityFramework<T>`，签名一致，仅连接串与可选的驱动 builder 委托不同。

## 对照表

| 数据库 | 包 | 注入驱动 | 连接串示例 | 备注 |
|---|---|---|---|---|
| PostgreSQL | `Common.EFCore.PostgresSql` | `UseNpgsql` | `Host=localhost;Port=5432;Database=db;Username=postgres;Password=pwd` | `Schema` 生效（默认 public） |
| MySQL | `Common.EFCore.MySQL` | `UseMySql` | `Server=localhost;Port=3306;Database=db;User=root;Password=pwd` | 自动 `ServerVersion.AutoDetect` |
| SQLite | `Common.EFCore.SQLite` | `UseSqlite` | `Data Source=app.db` 或 `:memory:` | 文件或内存 |
| SQLServer | `Common.EFCore.SQLServer` | `UseSqlServer` | `Server=localhost;Database=db;User Id=sa;Password=pwd;TrustServerCertificate=True` | |
| 内存库 | `Common.EFCore.InMemory` | `UseInMemoryDatabase` | （用数据库名，非连接串） | 仅测试/原型 |

## 注入签名差异

非内存库统一签名：
```csharp
services.AddEntityFramework<TDbContext>(
    Action<EfCoreConnectOption> config,
    Action<ProviderOptionsBuilder>? builder = null,          // 驱动特有，如 MigrationsAssembly
    Action<IServiceProvider, DbContextOptionsBuilder>? dbOpt = null);
```

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
    config.WorkId = 1;
});
```

### MySQL
```csharp
builder.Services.AddEntityFramework<AppDbContext>(config =>
{
    config.ConnectionString = builder.Configuration.GetConnectionString("Default")!;
    config.WorkId = 1;
});
// 内部已调用 ServerVersion.AutoDetect(connectionString)
```

### SQLite
```csharp
builder.Services.AddEntityFramework<AppDbContext>(config =>
{
    config.ConnectionString = "Data Source=app.db";
    config.WorkId = 1;
});
```

### SQLServer
```csharp
builder.Services.AddEntityFramework<AppDbContext>(config =>
{
    config.ConnectionString = builder.Configuration.GetConnectionString("Default")!;
    config.WorkId = 1;
});
```

### InMemory（测试）
```csharp
builder.Services.AddEntityFramework<AppDbContext>("test-db", workId: 1);
```

## 各注册了什么

调用 `AddEntityFramework<TDbContext>` 后自动注册：
- `TDbContext`（Scoped）+ `DbContext`（转发到同一实例，保证同 Scope 一致）
- `IBaseRepository<T>` → 对应 provider 的 `XxxRepository<T>`（Scoped）
- `IBaseRepository<T, TDbContext>`（多上下文用，InMemory 不注册此泛型）
- `IUnitOfWork` → `UnitOfWork<TDbContext>`（Scoped）

需要 `IUnitOfWork<TDbContext>` 时追加 `.AddUnitOfWork<TDbContext>()`（来自 `Azrng.EFCore` 基础包）。

## 其它注册入口（同一命名空间）

除 `AddEntityFramework<TDbContext>` 外，每个 provider 还暴露：

| 入口 | 用途 |
|---|---|
| `AddEntityFramework(config, builder?, dbOpt?)`（非泛型） | 用包内 `BaseDbContext`——自动对所有含 `IEntity` 实体的程序集 `ApplyConfigurationsFromAssembly`，免自定义 DbContext；内部就是转发 `AddEntityFramework<BaseDbContext>` |
| `AddEntityFrameworkFactory<T>(config, builder?, dbOpt?)` | `AddDbContextFactory<T>` 工厂模式注册（需要自己管理 DbContext 生命周期时） |
| `RepositoryFactory.GenerateRepository<TEntity>(DbContext)`（基础包静态类） | 不走 DI 时从一个 DbContext 实例直接生成仓储 |

## 目标框架与驱动版本

包按 net6~net10 多目标发布，不同 TFM 引用的驱动版本不同，**net10 的 MySQL provider 改用 `Microting.EntityFrameworkCore.MySql`**（net6~net9 用 Pomelo）。解释编译/行为差异时先确认项目 TargetFramework，再对照源码 `src/Shared/<包名>/*.csproj` 的条件引用。

## 连接串来源

provider 包**不读取任何配置键**，连接串完全由调用方赋值 `config.ConnectionString`（InMemory 传数据库名）。示例里的 `GetConnectionString("Default")` 只是宿主项目的自定义约定，键名任意。

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
