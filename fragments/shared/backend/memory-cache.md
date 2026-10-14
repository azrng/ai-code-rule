### 内存缓存使用规范
- 使用 `Common.Cache.MemoryCache` 封装的 `IMemoryCacheProvider` 接口。
- 通过 `services.AddMemoryCacheStore()` 注册内存缓存服务，配置项使用 `MemoryCacheOptions`。
- `MemoryCacheOptions.DefaultExpiry` 默认仅 5 秒，业务缓存必须显式传入过期时间，不依赖默认值。
- 适用于单实例部署、不需要分布式共享的缓存场景。
- 与 Redis 搭配使用时作为一级缓存，Redis 作为二级缓存。
