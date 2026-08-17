# SQL 脚本迁移（Azrng.SqlMigration）

数据库 schema 变更用 SQL 脚本版本化管理，**不走 EF Core 的 Code-First Migrations**。应用启动时自动按版本顺序执行未应用的脚本，已执行版本记入版本日志表、不重复跑。

包：`Azrng.SqlMigration`（0.5.0，net8/net9/net10）。目前仅提供 PostgreSQL 实现（`PgSqlDbService`）；基于 Dapper。

## 目录
1. 注册
2. SqlMigrationOption 全配置项
3. SQL 脚本约定
4. IMigrationHandler 迁移回调
5. 多库迁移
6. 注意事项

---

## 1. 注册

`AddSqlMigrationService(名字, 配置)` 链 `.AddAutoMigration()`（IStartupFilter，启动时自动执行）：

```csharp
using Azrng.SqlMigration;
using Npgsql;

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
var migrationSqlPath = Path.Combine(builder.Environment.ContentRootPath, "MigrationSql");

builder.Services.AddSqlMigrationService("default", config =>
{
    config.Schema = "public";                  // 数据库 schema（默认 public）
    config.VersionPrefix = string.Empty;       // SQL 文件版本前缀（默认 "version"）
    config.SqlRootPath = migrationSqlPath;     // SQL 脚本目录（Service/MigrationSql）
    config.ConnectionBuilder = _ => new NpgsqlConnection(connectionString);
}).AddAutoMigration();
```

需要迁移回调时用泛型注册（见 §4）：`AddSqlMigrationService<MyMigrationHandler>("default", config => { ... })`。

## 2. SqlMigrationOption 全配置项

| 配置项 | 类型 / 默认值 | 说明 |
|---|---|---|
| `ConnectionBuilder` | `Func<IServiceProvider, IDbConnection>`，必填 | 每次执行创建数据库连接 |
| `SqlRootPath` | `string?` | SQL 脚本根目录 |
| `VersionPrefix` | `string`，默认 `"version"` | 脚本文件名版本前缀 |
| `Schema` | `string`，默认 `"public"` | 数据库 schema |
| `VersionLog` | `SqlVersionLogOption` | 版本日志表配置，默认表名 `app_version_log` |
| `LockProvider` | `Func<IServiceProvider, Task<IAsyncDisposable?>>?` | 可选分布式锁（多实例部署防并发迁移，如 Redis 锁）；返回的句柄迁移结束释放 |
| `SetInitVersionSetter<T>()` | `T : IInitVersionSetter` | 自定义"初始版本"判定（首次迁移从哪个版本起步） |

## 3. SQL 脚本约定

- 脚本放 `SqlRootPath`（约定 `Service/MigrationSql/`）。
- 文件名 = `VersionPrefix + 版本号 + 扩展名`，扩展名支持 `.sql` / `.txt`；如 `1.0.0.sql`、`1.1.0.txt`、`1.0.0.1.sql`（前缀为空时）。
- **文件名不含描述文字**——去掉前缀与扩展名后的剩余部分整体解析为版本号，带描述会破坏版本排序。
- 按版本号顺序执行；只执行尚未应用过的脚本。
- **已应用的脚本不可修改**（版本日志已记录其版本）——改结构就新增一个更高版本的脚本。
- 版本日志表默认 `app_version_log`（可通过 `VersionLog.TableName` 改）；重跑历史脚本需删该表对应记录（PostgreSQL 时间陷阱的"场景 3"用到了这一点）。

## 4. IMigrationHandler 迁移回调

实现 `IMigrationHandler`（`Azrng.SqlMigration`）并用泛型注册，可介入迁移生命周期（按迁移名字 keyed 解析）：

```csharp
public class MyMigrationHandler : IMigrationHandler
{
    public Task<bool> BeforeMigrateAsync(string oldVersion) { /* 整体迁移前；异常阻断升级 */ }
    public Task<bool> VersionUpdateBeforeMigrateAsync(string version) { /* 单版本执行前；返回 false 跳过该版本；异常阻断 */ }
    public Task VersionUpdateMigratedAsync(string version) { /* 单版本迁移完成 */ }
    public Task VersionUpdateMigrateFailedAsync(string version) { /* 单版本迁移失败 */ }
    public Task MigratedAsync(string oldVersion, string version) { /* 整体迁移完成 */ }
    public Task MigrateFailedAsync(string oldVersion, string version) { /* 整体迁移失败 */ }
}
```

示例参考：`src/Services/WebDbSqlMigrationSample/DetailtMigrationHandler.cs`。

## 5. 多库迁移

调多次 `AddSqlMigrationService(不同名字, ...)`，各自独立的脚本目录、连接与版本日志；迁移名字不可重复、不可为空（重复注册抛异常）。

## 6. 注意事项

- 仅 PostgreSQL 有实现，其他库暂不可用。
- 与 EF Core Migrations 二选一：用了本包就不要再 `dotnet ef migrations`，避免两套版本记录打架。
- `AddAutoMigration()` 在启动阶段（IStartupFilter）执行，脚本错误会导致应用启动失败——脚本要先在测试库验证。
