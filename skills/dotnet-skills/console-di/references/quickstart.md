# 快速开始
## 前置条件

```bash
dotnet add package Azrng.ConsoleApp.DependencyInjection
```

## 核心组件

## IServiceStart 接口

所有控制台应用服务必须实现此接口：

```csharp
using Azrng.ConsoleApp.DependencyInjection;

public class MyService : IServiceStart
{
    private readonly ILogger<MyService> _logger;
    private readonly IConfiguration _config;

    public MyService(ILogger<MyService> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    public string Title => "我的控制台应用";

    public async Task RunAsync()
    {
        _logger.LogInformation("应用已启动");
        // 业务逻辑
    }
}
```

## ConsoleAppServer 构建器

核心构建器，自动完成：加载配置 → 创建服务容器 → 配置日志。

**构造函数行为**：
1. 从 `AppContext.BaseDirectory` 加载 `appsettings.json`（可选）
2. 检测 `DOTNET_ENVIRONMENT` 或 `ASPNETCORE_ENVIRONMENT` 环境变量，加载 `appsettings.{Environment}.json`
3. 加载 `ASPNETCORE_` 前缀的环境变量（自动移除前缀）
4. 加载命令行参数
5. 创建 ServiceCollection 并注册 IConfiguration
6. 配置日志：Console + Debug + 自定义文件日志（ExtensionsLoggerProvider）

**配置加载优先级**（低→高）：`appsettings.json` → 环境配置文件 → 环境变量 → 命令行参数

## 使用模式

## 模式一：简单使用

```csharp
// Program.cs
var builder = new ConsoleAppServer(args);
await using var sp = builder.Build<MyService>();
await sp.RunAsync();
```

## 模式二：注入额外服务

```csharp
var builder = new ConsoleAppServer(args);

// 通过 Services 属性注册服务
builder.Services.AddHttpClient();
builder.Services.AddScoped<IRepository, SqlRepository>();

await using var sp = builder.Build<MyService>();
await sp.RunAsync();
```

## 模式三：委托方式注册（AOT 友好）

```csharp
var builder = new ConsoleAppServer(args);
await using var sp = builder.Build<MyService>(services =>
{
    services.AddHttpClient();
    services.Configure<MyOptions>(builder.Configuration.GetSection("MyOptions"));
});
await sp.RunAsync();
```

## 完整开发步骤

### Step 1：创建控制台项目

```bash
dotnet new console -n MyConsoleApp
cd MyConsoleApp
dotnet add package Azrng.ConsoleApp.DependencyInjection
```

### Step 2：添加 appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

设置文件属性：复制到输出目录 → 如果较新则复制。

### Step 3：定义服务

```csharp
using Azrng.ConsoleApp.DependencyInjection;

public class AppService : IServiceStart
{
    public string Title => "我的应用";
    private readonly ILogger<AppService> _logger;

    public AppService(ILogger<AppService> logger) => _logger = logger;

    public async Task RunAsync()
    {
        _logger.LogInformation("开始执行");
        // 业务逻辑
    }
}
```

### Step 4：编写入口

```csharp
// Program.cs
var builder = new ConsoleAppServer(args);
await using var sp = builder.Build<AppService>();
await sp.RunAsync();
```
