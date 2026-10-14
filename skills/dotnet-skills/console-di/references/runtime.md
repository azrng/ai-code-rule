# 运行时行为与 AOT
## RunAsync 扩展方法

`ServiceProvider.RunAsync()` 执行流程：
1. 创建 AsyncScope
2. 解析 `IServiceStart` 实例
3. 打印标题分隔线（`ConsoleTool.PrintTitle`）
4. 调用 `service.RunAsync()`
5. 异常自动记录到本地日志文件

## ConsoleAppServer API

| 属性/方法 | 说明 |
|----------|------|
| `Services` | IServiceCollection，可手动注册服务 |
| `Configuration` | IConfiguration，已构建的配置 |
| `Build<T>()` | 注册 IServiceStart 并构建 ServiceProvider |
| `Build<T>(Action<IServiceCollection>)` | 委托注册服务后构建 |

## Native AOT 支持

项目已配置 AOT 兼容（`IsAotCompatible`、`TrimMode=link`）。发布：

```bash
dotnet publish -c Release -r win-x64 /p:PublishAot=true
```

AOT 注意事项：
- `Build<T>()` 使用 `DynamicallyAccessedMembers` 特性保证构造函数可用
- 避免反射、`Activator.CreateInstance`
- 使用委托方式 `Build<T>(Action<IServiceCollection>)` 注册服务
