# 安装与启动配置

BootstrapBlazor 10.9.2 的接入要点：一个 NuGet 包 + 一行注册 + 一个根组件 + 两份静态资源。

## 包与目标框架

- NuGet 包：`BootstrapBlazor`（核心，10.9.2）。核心库多目标 `net6.0;net7.0;net8.0;net9.0;net10.0`（`src/BootstrapBlazor/Directory.Build.props:21`）。
- 可选图标包（独立 NuGet，按需选其一）：`BootstrapBlazor.FontAwesome` / `BootstrapBlazor.MaterialDesign` / `BootstrapBlazor.BootstrapIcon`。
- 可选能力包：`BootstrapBlazor.Middleware`（中间件 `UseBootstrapBlazor`）、`BootstrapBlazor.TableExport`、`BootstrapBlazor.Html2Pdf`。

```xml
<PackageReference Include="BootstrapBlazor" Version="10.9.2" />
<!-- 可选图标 -->
<PackageReference Include="BootstrapBlazor.FontAwesome" Version="10.*" />
```

## Program.cs 最小模板（.NET 10 Blazor Web App, Interactive Server）

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// 一键注册全部 BootstrapBlazor 服务（Dialog/Toast/Message/Swal/Drawer/Notification/...）
builder.Services.AddBootstrapBlazor();

var app = builder.Build();

app.UseStaticFiles();        // .NET 10 也可用 app.MapStaticAssets();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
```

> `AddBootstrapBlazor` 在命名空间 `Microsoft.Extensions.DependencyInjection`（`BootstrapBlazorServiceCollectionExtensions.cs:25`），签名：
> `AddBootstrapBlazor(this IServiceCollection, Action<BootstrapBlazorOptions>? configureOptions = null, Action<JsonLocalizationOptions>? localizationConfigure = null)`。

带认证授权（Cookie，参考 rbac-blazor `Startup.cs`）：

```csharp
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Account/Login";
        o.LogoutPath = "/Account/Logout";
        o.AccessDeniedPath = "/access-denied";
        o.ExpireTimeSpan = TimeSpan.FromDays(1);
        o.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();
builder.Services.AddBootstrapBlazor();
// 大 SignalR 报文场景解除限制
builder.Services.Configure<HubOptions>(o => o.MaximumReceiveMessageSize = null);
```

页面级授权：`@attribute [Authorize]`；code-behind 类级 `[Authorize(Roles = "...")]`。

## App.razor（静态资源 + 渲染模式）

`Routes` 直接放 body（加 `@rendermode`）。**不要**在这里用 `<BootstrapBlazorRoot>` 包裹——它必须放 Layout（见下一节）。

```razor
<!DOCTYPE html>
<html lang="zh-CN">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <base href="/" />
    <link rel="stylesheet" href="_content/BootstrapBlazor.FontAwesome/css/font-awesome.min.css" />
    <link rel="stylesheet" href="_content/BootstrapBlazor/css/bootstrap.blazor.bundle.min.css" />
    @* 可选主题 *@
    <link rel="stylesheet" href="_content/BootstrapBlazor/css/motronic.min.css" />
    <HeadOutlet @rendermode="InteractiveServer" />
</head>
<body>
    <Routes @rendermode="InteractiveServer" />

    <script src="_content/BootstrapBlazor/js/bootstrap.blazor.bundle.min.js"></script>
    <script src="@Assets["_framework/blazor.web.js"]"></script>
</body>
</html>
```

> `@rendermode="InteractiveServer"`（由 `_Imports.razor` 的 `@using static Microsoft.AspNetCore.Components.Web.RenderMode` 提供）开启 Blazor Server 交互渲染。**缺失则静态模式**——交互组件（Table 数据加载、按钮点击、Modal）全部不工作。

## Layout：BootstrapBlazorRoot 放这里（最易踩的坑，必读）

`<BootstrapBlazorRoot>` 必须放在**交互渲染的 Layout 内**（包裹 `@Body`），**不能**放在 App.razor 包 Routes。

```razor
@* MainLayout.razor（默认布局） *@
@inherits LayoutComponentBase

<BootstrapBlazorRoot>
    @* 侧栏 / 顶栏等 *@
    <main>@Body</main>
</BootstrapBlazorRoot>
```

**根因**：`Modal`/`Toast`/`Dialog`/`Swal`/`Drawer` 等弹窗组件的 DOM 通过 `<BootstrapBlazorRootContent>` **portal** 到 `BootstrapBlazorRoot`（见 `Components/Modal/Modal.razor:5`）。`BootstrapBlazorRoot` 必须与页面组件处于**同一交互 circuit** 才能承接 portal。

**故障特征（极难排查）**：放错位置时，`Modal.Show()` / `ToastService.Success()` 的 C# 调用正常执行、**无任何异常**，但弹窗就是不显示——因为 portal 跨 rendermode 失效，C#→JS 的 `execute('show')` 找不到 DOM 元素，JS 端静默失败。

| 写法 | 结果 |
|---|---|
| ❌ App.razor 内 `<BootstrapBlazorRoot><Routes/></BootstrapBlazorRoot>` | BootstrapBlazorRoot 在静态层，弹窗 portal 失效，Modal/Toast 不显示（无异常） |
| ❌ `<BootstrapBlazorRoot @rendermode="InteractiveServer">` | `ChildContent` 是 `RenderFragment`（委托），跨 rendermode 不可序列化，启动即报 `Cannot pass the parameter 'ChildContent' ... cannot be serialized` |
| ✅ BootstrapBlazorRoot 放 MainLayout 内包裹 `@Body` | 与页面同 circuit，portal 正常，弹窗工作 |

> 官方示例 `BootstrapBlazor.Server` 的 `Components/Layout/BaseLayout.razor:3` 即把 `<BootstrapBlazorRoot>` 放在 Layout 内。`.NET 10` 的 `blazor.web.js` 经指纹化（如 `blazor.web.wyu7y4jcvb.js`），用 `@Assets["_framework/blazor.web.js"]` 引用。

## _Imports.razor

```razor
@using BootstrapBlazor.Components
@using System.ComponentModel.DataAnnotations
@using static Microsoft.AspNetCore.Components.Web.RenderMode   @* 提供 InteractiveServer 常量 *@
```

`@using BootstrapBlazor.Components` 开放全部组件命名空间。`System.ComponentModel.DataAnnotations` 启用 `[Required]`/`[Range]`（`ValidateForm` 依赖）。

> **坑**：加了 `@using BootstrapBlazor.Components` 后，`Console` 指向 BootstrapBlazor 的 `Console` 组件，与 `System.Console` 二义性（编译报 `CS0104`）。代码里写日志要用全限定 `System.Console.WriteLine(...)`。

## 中间件（可选，独立包）

`UseBootstrapBlazor()` 来自 `BootstrapBlazor.Middleware` 包，**不是核心库**。仅当需要其提供的能力时安装：

```csharp
// 需 <PackageReference Include="BootstrapBlazor.Middleware" Version="10.0.0" />
app.UseBootstrapBlazor();
```

纯组件使用（绝大多数页面）**不需要**任何中间件。

## appsettings.json（可选配置）

`BootstrapBlazorOptions` 段可配置全局行为（参考 rbac-blazor）：

```json
{
  "BootstrapBlazorOptions": {
    "ToastDelay": 4000,
    "SwalDelay": 0,
    "MessageDelay": 4000,
    "EnableErrorLogger": true,
    "IgnoreLocalizerMissing": true,
    "DefaultCultureInfo": "zh-CN",
    "LogoutUrl": "/Account/Logout"
  }
}
```

## 本地化（无 LocalizationService 类）

源码中**没有** `LocalizationService`。本地化由 `AddJsonLocalization` 接入，注入 `IStringLocalizer<T>` 使用（`BootstrapBlazorServiceCollectionExtensions.cs:30`）。资源 JSON 放 `Locales/` 目录（如 `zh-CN.json`）。

## 常见接入故障

| 现象 | 根因 |
|---|---|
| `DialogService.Show` 抛 `InvalidOperationException` | 未放 `<BootstrapBlazorRoot>` |
| 页面无样式/无交互 | 漏引 `bootstrap.blazor.bundle.min.css` / `.js` |
| 组件标红找不到 | `_Imports.razor` 缺 `@using BootstrapBlazor.Components` |
| 弹窗一闪而过 | `ToastDelay`/`MessageDelay` 配成 0 或过小 |
| 校验不生效 | 模型缺 `[Required]` 等，或 `ValidateAllProperties` 未开 |
