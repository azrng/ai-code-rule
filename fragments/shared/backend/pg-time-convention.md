### PostgreSQL 时间约定
- Npgsql 默认把 `DateTime` 映射为带时区的 `timestamptz`，写入 `Kind=Unspecified` 的时间会抛 `Cannot write DateTime with Kind=Unspecified`；本项目统一使用无时区时间
- 时间赋值统一 `DateTime.Now.ToUnspecifiedDateTime()`（`Azrng.Core.Extension`，经 `Common.EFCore` 传递引用可达），不混用 `DateTime.UtcNow`
- 业务自声明的时间列在实体配置中手动 `.IsUnTimeZoneDateTime()`（`timestamp without time zone`）；继承 Azrng 实体基类与配置基类的审计时间列已自动处理
