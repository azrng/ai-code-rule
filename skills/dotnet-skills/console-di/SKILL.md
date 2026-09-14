---
name: console-di
description: "Azrng.ConsoleApp.DependencyInjection 库开发指南。.NET 控制台应用依赖注入框架，支持 appsettings.json 配置、多日志输出（Console/Debug/文件）、环境配置、AOT 编译。Keywords: ConsoleAppServer, IServiceStart, ConsoleApp, 依赖注入, DependencyInjection, Console DI, Azrng.ConsoleApp."
---

# Azrng.ConsoleApp.DependencyInjection 开发指南

Azrng.ConsoleApp.DependencyInjection 为 .NET 控制台应用提供类似 ASP.NET Core 的依赖注入体验。命名空间：`Azrng.ConsoleApp.DependencyInjection`。目标框架：net8.0 / net9.0 / net10.0，支持 Native AOT。

**适用场景**：
- 使用 Azrng.ConsoleApp.DependencyInjection 构建 .NET 控制台应用
- 控制台应用需要依赖注入、配置文件、日志
- 替代手动搭建 ServiceProvider 的场景

**不适用场景**：
- ASP.NET Core Web 应用（已有内置 DI）
- 不使用此库的原生控制台应用

## 核心模型

最小心智模型（细节见参考文档）：

```csharp
// 1. 业务服务实现 IServiceStart（Title + RunAsync）
public class MyService : IServiceStart { /* ... */ }

// 2. ConsoleAppServer 一行完成：配置加载 → 服务容器 → 日志
var builder = new ConsoleAppServer(args);
await using var sp = builder.Build<MyService>();
await sp.RunAsync();
```

## 按需查阅

本文件只做路由，按当前任务读对应参考文档即可，不必全部加载。

| 任务场景 | 参考文档 |
| --- | --- |
| 首次上手：安装、IServiceStart / ConsoleAppServer 细节、三种 Build 模式、完整开发步骤 | `references/quickstart.md` |
| appsettings.json、环境变量（`ASPNETCORE_` 前缀 / `__` 层级）、命令行参数、选项注入、三种日志输出 | `references/configuration.md` |
| RunAsync 执行流程、ConsoleAppServer API 表、Native AOT 发布与注意事项 | `references/runtime.md` |

## 验证清单

- [ ] 服务类实现 `IServiceStart` 接口（Title + RunAsync）
- [ ] `appsettings.json` 设为"复制到输出目录"
- [ ] 使用 `await using` 确保 ServiceProvider 正确释放
- [ ] 环境变量加 `ASPNETCORE_` 前缀
- [ ] 环境变量层级用 `__`（双下划线）代替 `:`
- [ ] AOT 场景使用委托方式注册服务，避免反射

## 参考资源

- [NuGet 包](https://www.nuget.org/packages/Azrng.ConsoleApp.DependencyInjection)
- [GitHub 源码](https://github.com/azrng/nuget-packages)
- [Microsoft.Extensions.DependencyInjection 文档](https://learn.microsoft.com/dotnet/core/extensions/dependency-injection)
