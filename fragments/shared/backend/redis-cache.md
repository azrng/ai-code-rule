### Redis 缓存使用规范
- 使用 `Common.Cache.Redis` 封装的 `IRedisProvider` 接口
- 通过 `services.AddRedisCacheStore()` 注册 Redis 服务，配置项使用 `RedisCacheOptions`
- 缓存操作必须设置合理的过期时间
- 热点数据使用本地缓存 + Redis 二级缓存
- 发布订阅场景使用 `PublishAsync`/`SubscribeAsync`
- 禁止在循环中频繁调用 Redis
