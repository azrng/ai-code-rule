# 版本与工程装配

## 版本矩阵（消费工程 csproj 核实，2026-09）

> 唯一维护处：加包 / 升级只改本表；SKILL.md 概述与 description 的基准组合摘要随本表同步，别处不另记版本号。

| 包 | 版本 | 说明 |
|---|---|---|
| Avalonia | 12.1.2 | 框架本体 |
| Avalonia.Desktop | 12.1.2 | 桌面平台后端 |
| Avalonia.Themes.Fluent | 12.1.2 | Fluent 基础主题 |
| Avalonia.Fonts.Inter | 12.1.2 | 字体（可选） |
| Semi.Avalonia | 12.1.0.1 | 主题库（覆盖 Fluent 的控件观感） |
| Irihi.Ursa | 2.2.0 | 控件库（Dialog / Badge / Loading 等） |
| Irihi.Ursa.Themes.Semi | 2.2.0 | Ursa 的 Semi 主题适配 |
| CommunityToolkit.Mvvm | 以 csproj 为准 | 源生成器 MVVM |

版本纪律：Avalonia 11 → 12 有破坏性差异（`IClipboard` 重构为 DataTransfer 体系、`PresentationSource` 引入、Obsolete 成员移除）。写平台 API 前先回源码对应 tag（`git show 12.1.2:<path>`）或 NuGet ref 程序集（`~/.nuget/packages/avalonia/12.1.2/ref/`）确认签名。

## 入口装配（消费工程实际写法）

Program.cs：

```csharp
public static AppBuilder BuildAvaloniaApp(this AppBuilder builder)
    => builder
        .UsePlatformDetect()
        .WithInterFont()
        .LogToTrace();
```

App.axaml 资源合并顺序（后合并的优先）：

```xml
<Application.Resources>
  <ResourceDictionary>
    <ResourceDictionary.MergedDictionaries>
      <ResourceInclude Source="avares://Semi.Avalonia/Themes/Index.axaml" />
      <ResourceInclude Source="avares://Ursa.Themes.Semi/Index.axaml" />
      <!-- 项目自有 token 与样式在主题之后，允许覆盖（avares:// 后为当前项目程序集名） -->
      <ResourceInclude Source="avares://MyApp/Assets/Styles/Colors.axaml" />
      <ResourceInclude Source="avares://MyApp/Assets/Styles/Spacing.axaml" />
      <ResourceInclude Source="avares://MyApp/Assets/Styles/Shadows.axaml" />
      <!-- 各领域 Styles.axaml ... -->
    </ResourceDictionary.MergedDictionaries>
  </ResourceDictionary>
</Application.Resources>
```

编译绑定默认开启：

```xml
<PropertyGroup>
  <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
</PropertyGroup>
```

## 设计 token 资源字典（类型纪律）

Spacing.axaml 示例——**标量与 Thickness 是两种类型，不可混用**：

```xml
<x:Double x:Key="Space.2">8</x:Double>          <!-- 标量：给 Spacing / 双精度属性 -->
<Thickness x:Key="Pad.2">8</Thickness>          <!-- Thickness：给 Padding / Margin / BorderThickness -->
<Thickness x:Key="Pad.Top2">0,8,0,0</Thickness>
```

Shadows.axaml 示例——**BoxShadow 只接受纯数值，禁止 px 后缀**：

```xml
<BoxShadows x:Key="Shadow.Dialog">0 24 60 0 #0F172A20</BoxShadows>
<BoxShadows x:Key="Shadow.Drawer.Left">-24 0 48 0 #0F172A28</BoxShadows>  <!-- 负偏移同样无单位 -->
```

引用规则：样式 Setter 与控件属性里 `Padding="{StaticResource Pad.2}"`；需要主题切换跟随的（颜色、画刷）用 `DynamicResource`。
