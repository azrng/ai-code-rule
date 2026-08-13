# 服务清单与依赖注入

全部服务由 `AddBootstrapBlazor()`（`Extensions/BootstrapBlazorServiceCollectionExtensions.cs:25`，命名空间 `Microsoft.Extensions.DependencyInjection`）**一键注册**，无需手动逐个 Add。**注入时用具体类，无接口**（`@inject DialogService`）。

## 弹窗/工具服务（Scoped 具体类，`:109-125`）

| 服务 | 用途 | 关键 API |
|---|---|---|
| `DialogService` | 弹窗 | `Show(DialogOption)` + 扩展 |
| `MessageService` | 消息条 | `Show(MessageOption)` |
| `ToastService` | Toast 提示 | `Show(ToastOption)` + Success/Error/... 扩展 |
| `SwalService` | SweetAlert | `Show(SwalOption)` + `ShowModal()` |
| `DrawerService` | 抽屉 | `Show(DrawerOption)` |
| `MaskService` | 遮罩 | — |
| `FullScreenService` | 全屏 | `Toggle(FullScreenOption?)` |
| `PrintService` | 打印 | — |
| `TitleService` | 页面标题 | — |
| `DownloadService` | 下载 | — |
| `WebClientService` | Web 客户端 | — |
| `AjaxService` | Ajax | — |
| `ClipboardService` | 剪贴板 | — |
| `EyeDropperService` | 取色器 | — |
| `WebSpeechService` | 语音 | — |
| `DragDropService<>` | 拖拽（泛型） | — |
| `TabItemTextOptions` | Tab 文本配置 | — |
| `BootstrapBlazorRootRegisterService` | 根注册 | 内部 |

> 这些弹窗服务（除 Notification/FullScreen）继承 `BootstrapServiceBase<TOption>`（`Services/BootstrapServiceBase.cs`），由 `<BootstrapBlazorRoot>` 提供宿主；缺失则 `Show` 抛 `InvalidOperationException`（`:31-39`）。

## 接口 + 实现对（`BootstrapBlazorServiceCollectionExtensions.cs:33-107`）

| 接口 | 实现 | 生命周期 |
|---|---|---|
| `ICacheManager` | `CacheManager` | Singleton |
| `IComponentIdGenerator` | `DefaultIdGenerator` | — |
| `ILookupService` | `NullLookupService` | — |
| `IVersionService` | `DefaultJSVersionService` | — |
| `IZipArchiveService` | `DefaultZipArchiveService` | — |
| `IDispatchService<>` | `DefaultDispatchService<>` | Singleton（泛型） |
| `ITotpService` | `DefaultTotpService` | — |
| `IHtml2Pdf` | `DefaultHtml2PdfService` | — |
| `IHtml2Image` | `DefaultHtml2ImageService` | — |
| `ITableExport` | `DefaultTableExport` | — |
| `IThemeProvider` | `DefaultThemeProvider` | — |
| `IIpLocatorFactory` | `DefaultIpLocatorFactory` | — |
| `IIpLocatorProvider` | `BaiduIpLocatorProvider` | keyed |
| `ICalendarFestivals` / `ICalendarHolidays` | `Default...` | — |
| `IConnectionService` | `DefaultConnectionService` | — |
| `IThrottleDispatcherFactory` | `DefaultThrottleDispatcherFactory` | — |
| `IPinyinService` | `DefaultPinyinService` | — |
| `IDataService<>` | `NullDataService<>` | Scoped（泛型，可覆盖） |
| `IReconnectorProvider` | `ReconnectorProvider` | — |
| `IGeoLocationService` | `DefaultGeoLocationService` | — |
| `IComponentHtmlRenderer` | `ComponentHtmlRenderer` | — |
| `IBrowserFingerService` | `DefaultBrowserFingerService` | — |
| `ISerialService` | `DefaultSerialService` | — |
| `IBluetooth` | `DefaultBluetooth` | — |
| `IMediaDevices`/`IVideoDevice`/`IAudioDevice` | `Default...` | — |
| `INetworkMonitorService` | `DefaultNetowrkMonitorService` | — |
| `IIconTheme` | `DefaultIconTheme` | — |

> `NotificationService`（`Services/NotificationService.cs:14`，浏览器原生通知）不继承 `BootstrapServiceBase`，方法 `CheckPermission`/`Dispatch`，无需 Root。

## 扩展方法（`Extensions/`）

| 静态类 | 方法 | 说明 |
|---|---|---|
| `DialogServiceExtensions` | `Show<T>()`/`ShowSearchDialog<T>()`/`ShowEditDialog<T>()`/`ShowModal<T>()`→`Task<DialogResult>`/`ShowSaveDialog<T>()`/`ShowValidateFormDialog<T>()` | Dialog 场景化 |
| `ToastServiceExtensions` | `Success/Error/Information/Warning` | Toast 快捷 |
| `SwalExtensions` | `ShowModal()`→`Task<bool>` | Swal 确认框 |

## 本地化

源码**无 `LocalizationService` 类**。本地化通过 `AddJsonLocalization`（`BootstrapBlazorServiceCollectionExtensions.cs:30`）接入，注入 `IStringLocalizer<T>` 使用。资源 JSON 放 `Locales/`。

## 注册示例

```csharp
// 最小
builder.Services.AddBootstrapBlazor();

// 带配置
builder.Services.AddBootstrapBlazor(options =>
{
    options.ToastDelay = 4000;       // 也可在 appsettings.json 配 BootstrapBlazorOptions 段
});

// 可选能力包（独立 NuGet）
builder.Services.AddBootstrapBlazorTableExportService();   // BootstrapBlazor.TableExport
builder.Services.AddBootstrapBlazorHtml2PdfService();      // BootstrapBlazor.Html2Pdf
```

## 注入写法

```razor
@inject DialogService DialogService
@inject ToastService ToastService
@inject SwalService SwalService
@inject MessageService MessageService
@inject DrawerService DrawerService
```

或 code-behind：`[Inject] ToastService ToastService { get; set; } = null!;`。
