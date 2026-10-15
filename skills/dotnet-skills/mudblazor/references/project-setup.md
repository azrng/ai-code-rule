# 项目初始化与服务配置

事实来源：MudBlazor.Templates 仓库（v4.4.0，net10.0）+ 主仓库 `src/MudBlazor/Extensions/ServiceCollectionExtensions.cs`。

## 两种开项目方式

### 方式一：官方模板（推荐新项目）

```bash
dotnet new install MudBlazor.Templates
dotnet new mudblazor -o MyMudApp                     # 默认 Server 交互
dotnet new mudblazor -o MyMudApp -int Auto -ai       # Auto + 全局交互
dotnet new mudblazor -o MyMudApp -au Individual      # 带 Identity 认证
dotnet new mudblazorwasm -o MyMudWasmApp             # 独立 WASM
dotnet new mudblazor -o MyMudApp -e                  # 空项目（无示例页）
```

常用选项（`-int` 是 mudblazor 独有；mudblazorwasm 无交互模式选项）：

| 选项 | 值 | 默认 |
| --- | --- | --- |
| `-int` | None / Server / WebAssembly / Auto | Server |
| `-ai` | 全局交互（根级渲染模式） | false |
| `-au` | None / Individual（WASM 另有 IndividualB2C / SingleOrg） | None |
| `-e` | 空项目（省示例页与演示样式） | false |
| `-uld` | LocalDB 代替 SQLite（仅 `-au Individual`） | false |

### 方式二：现有 Blazor 项目手动接入

1. 安装包（模板用浮动版本 `9.*`；生产建议锁定具体版本）：

```xml
<PackageReference Include="MudBlazor" Version="9.11.0" />
```

2. `Program.cs` 注册服务（注意 using 是 `MudBlazor.Services`，不是 `MudBlazor`）：

```csharp
using MudBlazor.Services;

builder.Services.AddMudServices();
// 或带配置：
builder.Services.AddMudServices(cfg =>
{
    cfg.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
    cfg.PopoverOptions.CheckForPopoverProvider = false;
});

// 页面 @inject HttpClient 时需另行注册（Blazor Web App 模板不自动提供）：
builder.Services.AddScoped<HttpClient>();
```

`AddMudServices()` 聚合注册（源码 `ServiceCollectionExtensions.cs:313`）：Dialog（`IDialogService`，scoped）、Snackbar（`ISnackbar`，scoped）、ResizeListener、ResizeObserver、KeyInterceptor、JsEvent、ScrollManager、ScrollListener、JsApi、ScrollSpy、Popover（`IPopoverService`）、PointerEventsNone、Localization。

3. `App.razor`（`<head>` 加 CSS，`blazor.web.js` 之后加 JS）：

```html
<link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
<script src="_content/MudBlazor/MudBlazor.min.js"></script>
```

4. `_Imports.razor`：

```
@using MudBlazor
@using MudBlazor.Services
```

## Provider 挂载（最易错点）

四个 Provider：`MudThemeProvider` / `MudPopoverProvider` / `MudDialogProvider` / `MudSnackbarProvider`。

**官方模板规则**（跟随 `-ai` 选项变化）：

- `-ai` 全局交互：四 Provider 全部放 `MainLayout.razor` 顶部（`MudThemeProvider` 最先）
- 默认（Per page/component 交互）：`MudThemeProvider` 仍在 MainLayout；**其余三个 Provider 必须每个交互页手补**，漏补则该页 popover/dialog/snackbar 静默失效
- 缺 `MudDialogProvider` 时 `ShowAsync` 记错误日志并阻塞 5 秒后失败（源码 `DialogService.cs` MissingProviderMessage）
- Provider 必须与调用方处于**同一交互渲染模式**；静态 SSR 页面不可用

`MainLayout.razor` 标准骨架（官方模板裁剪）：

```razor
@inherits LayoutComponentBase

<MudThemeProvider @bind-IsDarkMode="_isDarkMode" />
<MudPopoverProvider />
<MudDialogProvider />
<MudSnackbarProvider />

<MudLayout>
    <MudAppBar Elevation="1">
        <MudIconButton Icon="@Icons.Material.Filled.Menu" Color="Color.Inherit" Edge="Edge.Start"
                       OnClick="@((e) => _drawerOpen = !_drawerOpen)" />
        <MudText Typo="Typo.h6" Class="ml-2">My App</MudText>
        <MudSpacer />
    </MudAppBar>
    <MudDrawer @bind-Open="_drawerOpen">
        <MudDrawerHeader><MudText Typo="Typo.h6">导航</MudText></MudDrawerHeader>
        <MudNavMenu>...</MudNavMenu>
    </MudDrawer>
    <MudMainContent Class="pt-16 pa-4">
        @Body
    </MudMainContent>
</MudLayout>

@code {
    private bool _drawerOpen = true;
    private bool _isDarkMode;
}
```

布局组件细节（Drawer 变体、ClipMode、断点）见 `layout-and-theme.md`。

## WASM 独立应用差异

- `Program.cs`：`builder.Services.AddMudServices()`（显式 `using MudBlazor.Services`）；`RootComponents.Add<App>("#app")`
- 资源挂 `wwwroot/index.html`；`MudBlazor.min.js` 在 `blazor.webassembly.js` 之前
- `_Imports.razor` 只有 `@using MudBlazor`（模板缺 `MudBlazor.Services`，用到时自己补）
- 多工程（Auto/WASM 交互）时 **Client 工程与服务端各自调用 `AddMudServices()`**，漏一侧则该层服务缺失
- `MudStaticNavDrawerToggle` **不属于核心 MudBlazor 包**：它来自官方模板在 Individual 认证场景额外引入的第三方包 `Extensions.MudBlazor.StaticInput`（`Version="4.*"`，仅 `Condition="'$(IndividualLocalAuth)' == 'True'"` 时引用，命名空间 `MudBlazor.StaticInput`）；它支持静态 SSR 下开合抽屉，普通交互场景用 `MudIconButton` + 翻转 `_drawerOpen`。没有引该包却写 `<MudStaticNavDrawerToggle>` 会编译失败，不要当成核心组件

## 版本检查（生成代码前必做）

1. 读目标项目 `.csproj` / `Directory.Packages.props` 的 MudBlazor 版本
2. v9 破坏性变更速查（v8 代码迁移时逐条核对）：
   - Dialog 同步 `Show()` → `ShowAsync<T>()`；`ShowMessageBox` → `ShowMessageBoxAsync`
   - `MudForm.Validate()` → `ValidateAsync()`
   - `MudFormItem` 组件移除（嵌套直接用 `MudForm`）
   - `ChipSet` 的 `MultiSelection` 布尔 → `SelectionMode` 枚举
   - `Select` 移除 `SelectedItem`；`FileUpload` 的 `FileTypes` → `Accept`
   - Snackbar 的 `MaximumDuplicatedSnackbars` / `CloseButtonText` 移除 → `PreventDuplicates`+`key` / `SnackbarOptions.Action`
   - `MudTabs.ActivatePanel` → `ActivatePanelAsync`
3. 旧版本项目遇到 API 不确定时，以 https://mudblazor.com 对应版本文档为准，不要套用 dev 分支新 API
