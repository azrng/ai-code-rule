# 自动审计（Azrng.EFCore.AutoAudit）

> **定位：可选能力、按需开启。** 仅当用户明确提出审计需求（操作留痕 / 审计日志 / 记录谁改了什么）时才接入；用户未提及时不主动配置、不主动推荐。
>
> 包：`Azrng.EFCore.AutoAudit`（1.0.0-beat1，net8/net9）。基于 SaveChanges 拦截器自动记录增删改。

## 目录
1. 标准接入（两步）
2. 审计表（自动创建）
3. 配置 API（IAuditConfigBuilder / AuditExtensions）
4. 常见用法
5. 敏感属性自动脱敏
6. 注意事项

---

## 1. 标准接入（两步）

```csharp
using Azrng.EFCore.AutoAudit;      // AddEFCoreAutoAudit
using Azrng.Core.Model;            // DatabaseType

// ① 业务库照常用 provider 包注册（AddEntityFramework<TDbContext>）
builder.Services.AddEntityFramework<AppDbContext>(config => { /* ... */ });

// ② 添加审计：第三参 DatabaseType 必填，指审计库类型
builder.Services.AddEFCoreAutoAudit(config =>
{
    config.WithAuditRecordsDbContextStore(options => options.UseNpgsql(auditConn));
}, DatabaseType.PostgresSql);
```

`WithAuditRecordsDbContextStore` 内部注册审计库 `AuditRecordsDbContext` + `AuditRecordsDbContextStore` 存储；审计库可与业务库不同（示例中 `auditConn` 独立）。

**自己管理 DbContext 时**（不走 provider 包注册），还要在业务 DbContext 的 options 上挂拦截器（`AuditExtensions` 扩展）：

```csharp
builder.Services.AddDbContext<OpenDbContext>((provider, options) =>
{
    options.UseNpgsql(conn);
    options.AddAuditInterceptor(provider);   // ← 手动挂审计拦截器
});
```

走 `AddEntityFramework<TDbContext>` 注册时无需这步（启动装配自动处理，见 `AutoAuditStartupFilter`）。

## 2. 审计表（自动创建）

使用 `WithAuditRecordsDbContextStore` 时，应用启动（IStartupFilter）会按 `DatabaseType` 自动执行幂等建表脚本（`create table if not exists`），**通常无需手动建表**；包 README 的"手动创建审计表"章节仅作表结构参考。支持 MySQL / SQLServer / SQLite / Oracle / PostgreSQL；InMemory 自动跳过；其余类型启动抛异常。

表结构（PG 为例，表名 `audit_record`）：

| 列 | 类型 | 说明 |
|---|---|---|
| `id` | varchar(50) PK | 记录 Id |
| `table_name` | varchar(120) | 业务表名 |
| `operation_type` | integer | 0查询 1添加 2修改 3删除（`DataOperationType`） |
| `object_id` | text | 业务数据主键 |
| `origin_value` / `new_value` | text | 改前 / 改后值 |
| `extra` | text | 扩展信息 |
| `updater` | text | 操作人（来自 `IUserIdProvider`） |
| `update_time` | timestamp with time zone | 操作时间（审计表自身约定，不影响业务表的无时区约定） |
| `is_success` | boolean | 是否成功 |

## 3. 配置 API（IAuditConfigBuilder / AuditExtensions）

`AddEFCoreAutoAudit(Action<IAuditConfigBuilder>, DatabaseType)`，链式配置：

| 方法 | 说明 |
|---|---|
| `WithAuditRecordsDbContextStore(Action<DbContextOptionsBuilder>)` | 审计存审计库（最常用，扩展方法） |
| `WithUserIdProvider(IUserIdProvider)` / `(Func<IServiceProvider, IUserIdProvider>)` / `<TUserIdProvider>()` | 操作人来源，默认无（`updater` 为空） |
| `WithUnmodifiedProperty(bool = true)` | 默认只记录修改过的属性；开启后记录全部 |
| `WithStore(IAuditStore)` / `WithStore<TStore>()` | 自定义存储 |
| `WithEntityFilter(Func<EntityEntry, bool>)` | 只审计满足条件的实体 |
| `WithPropertyFilter(Func<EntityEntry, PropertyEntry, bool>)` | 只审计满足条件的属性 |
| `IgnoreEntity<TEntity>()` / `IgnoreEntity(Type)` | 忽略指定实体类型 |
| `IgnoreTable(string tableName)` | 按表名忽略 |
| `IgnoreProperty<TEntity>(...)` / `IgnoreProperty(string)` | 忽略指定属性 |
| `IgnoreColumn(string)` / `IgnoreColumn(table, column)` | 按列名忽略 |

命名空间：构建器与扩展在 `Azrng.EFCore.AutoAudit`；`DatabaseType` 在 `Azrng.Core.Model`；`AuditFileStore` 在 `Azrng.EFCore.AutoAudit.Service`。

## 4. 常见用法

```csharp
builder.Services.AddEFCoreAutoAudit(config =>
{
    config.IgnoreEntity<OperationLog>()                       // 日志表自身不审计
          .WithPropertyFilter((e, p) =>                       // 只审计关键属性
              p.Metadata.Name is "Name" or "Email" or "Status")
          .WithUserIdProvider<HttpUserIdProvider>()           // 操作人取当前登录用户
          .WithUnmodifiedProperty(false)                      // 只记变更属性（默认）
          .WithAuditRecordsDbContextStore(o => o.UseNpgsql(auditConn));
}, DatabaseType.PostgresSql);
```

自定义操作人（实现 `IUserIdProvider.GetUserId()` 返回 `string?`，如从 HttpContext 取 `sub` claim）；自定义存储（实现 `IAuditStore.SaveAsync(ICollection<AuditEntryDto>)`，可写 ES / 消息队列）。

> **文件存储风险**：README 提供 `WithStore<AuditFileStore>()`，但启动过滤器会无条件解析 `AuditRecordsDbContext`（仅 `WithAuditRecordsDbContextStore` 会注册它），纯文件存储可能启动报错——使用前先实测，不确定时优先用审计库存储。

## 5. 敏感属性自动脱敏

以下名称的属性值不记录（自动识别，无需配置）：

`Password`、`Token`、`Secret`、`Key`、`ApiKey`、`AccessToken`、`RefreshToken`、`PrivateKey`、`PublicKey`、`CreditCard`、`SSN`、`SocialSecurityNumber`、`Pin`、`Otp`、`VerificationCode`

## 6. 注意事项

- **枚举拼写**：`DatabaseType.PostgresSql`（`Azrng.Core.Model`）。包 README 示例里的 `DatabaseType.PostgreSql` 是笔误，照抄编译不过；以 `src/Services/PostgresSqlSample/Program.cs` 的可编译写法为准。
- 审计的是 SaveChanges 通路（仓储的增删改）；原生 SQL（`ExecuteSqlCommand` 等）不经过拦截器，不会产生审计记录。
- 测试参考：`test/Azrng.EFCore.AutoAudit.Test`（SQLite 接入、IgnoreEntity / IgnoreColumn 演示）。
