# 安装与项目配置

```bash
# 安装 BootstrapBlazor NuGet 包
dotnet add package BootstrapBlazor
dotnet add package BootstrapBlazor.FontAwesome

# 可选：PDF 导出支持
dotnet add package BootstrapBlazor.TableExport
dotnet add package BootstrapBlazor.Html2Pdf
```

# 项目配置

## 1. 服务注册（Startup.cs）

```csharp
// 基础注册
services.AddBootstrapBlazor();

// 可选：表格导出和 PDF 服务
services.AddBootstrapBlazorTableExportService();
services.AddBootstrapBlazorHtml2PdfService();
```

## 2. 静态资源引用（App.razor 或 _Host.cshtml）

```html
<!-- CSS -->
<link href="_content/BootstrapBlazor.FontAwesome/css/font-awesome.min.css" rel="stylesheet"/>
<link href="_content/BootstrapBlazor/css/bootstrap.blazor.bundle.min.css" rel="stylesheet"/>
<!-- 主题：Motronic 风格 -->
<link href="_content/BootstrapBlazor/css/motronic.min.css" rel="stylesheet"/>

<!-- JS -->
<script src="_content/BootstrapBlazor/js/bootstrap.blazor.bundle.min.js"></script>
```

## 3. 全局导入（_Imports.razor）

```razor
@using BootstrapBlazor.Components
```

## 4. 根组件配置（Routes.razor）

```razor
<BootstrapBlazorRoot>
    <CascadingAuthenticationState>
        <Router AppAssembly="@typeof(Program).Assembly">
            <Found Context="routeData">
                <AuthorizeView>
                    <Authorized>
                        <CascadingValue Value="routeData">
                            <RouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)"/>
                        </CascadingValue>
                        <FocusOnNavigate RouteData="@routeData" Selector="h1"/>
                    </Authorized>
                    <NotAuthorized>
                        <LayoutView Layout="@typeof(LoginLayout)">
                            <Login />
                        </LayoutView>
                    </NotAuthorized>
                </AuthorizeView>
            </Found>
            <NotFound>
                <LayoutView Layout="@typeof(LoginLayout)">
                    <p>页面未找到</p>
                </LayoutView>
            </NotFound>
        </Router>
    </CascadingAuthenticationState>
</BootstrapBlazorRoot>
```
