# 配置与日志
## 配置系统

## appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  },
  "AppSettings": {
    "ConnectionString": "localhost",
    "Timeout": 30
  }
}
```

## 环境变量

环境变量必须加 `ASPNETCORE_` 前缀，用 `__` 代替 `:` 层级：

```bash
# Linux/Mac
export ASPNETCORE_AppSettings__ConnectionString="prod-server"
export DOTNET_ENVIRONMENT="Production"

# Windows
set ASPNETCORE_AppSettings__ConnectionString=prod-server
```

读取：`Configuration["AppSettings:ConnectionString"]`

## 命令行参数

```bash
dotnet run --AppSettings:ConnectionString "override-server"
```

## 访问配置

```csharp
public class MyService : IServiceStart
{
    private readonly IConfiguration _config;

    public MyService(IConfiguration config) => _config = config;

    public async Task RunAsync()
    {
        var connStr = _config["AppSettings:ConnectionString"];
        var timeout = _config.GetValue<int>("AppSettings:Timeout");
    }
}
```

## 访问 Configuration 注入选项

```csharp
// 在 Build 委托中注册
builder.Build<MyService>(services =>
{
    services.Configure<AppOptions>(builder.Configuration.GetSection("App"));
});

// 在服务中注入
public class MyService : IServiceStart
{
    private readonly AppOptions _options;

    public MyService(IOptions<AppOptions> options) => _options = options.Value;
}
```

## 日志系统

自动配置三种日志输出：

| 输出目标 | 说明 |
|---------|------|
| Console | 控制台输出 |
| Debug | Visual Studio 调试窗口 |
| ExtensionsLogger | 本地文件（通过 `LocalLogHelper`） |

日志级别由 `appsettings.json` 的 `Logging:LogLevel:Default` 控制，文件日志额外受 `CoreGlobalConfig.MinimumLevel` 约束。

```csharp
public class MyService : IServiceStart
{
    private readonly ILogger<MyService> _logger;

    public MyService(ILogger<MyService> logger) => _logger = logger;

    public async Task RunAsync()
    {
        _logger.LogInformation("处理用户 {UserId}", userId);
        _logger.LogError(ex, "处理失败 {Error}", ex.Message);
    }
}
```
