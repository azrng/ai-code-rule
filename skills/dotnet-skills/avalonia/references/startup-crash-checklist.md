# 「编译通过但启动/运行崩溃」排查清单

Avalonia 的四大类崩溃都发生在**运行时**：样式附加期（启动即崩）、资源解析期（启动即崩）、动画启动期（界面首次触发时崩），以及增量构建掩盖编译错误导致的「假成功」（运行时才暴露）。`dotnet build` 全绿不代表能启动。以下前三类全部来自真实工程踩坑（消费工程，2026-09 一天内实际命中三类），第四类来自 2026-10 消费工程实测。

**排查前置**：如果应用还没有全局异常挂点（`AppDomain.CurrentDomain.UnhandledException` / `Dispatcher.UIThread.UnhandledException` / `TaskScheduler.UnobservedTaskException`）与文件日志，闪退原因会完全丢失——先按 logging-and-crash-report.md 把挂点补上再排查；历史崩溃可去事件查看器（`.NET Runtime` 来源条目）与 `%LOCALAPPDATA%\CrashDumps` 找回线索。

## 崩溃一：标量 token 赋给 Thickness 属性

**错误原文**：

```text
System.InvalidCastException: Setter value '8' is not a valid value for property 'Padding'.
   at Avalonia.Styling.Setter.Instance(IStyleInstance instance, StyledElement target)
```

**根因**：样式 Setter 的值在样式附加到控件时才实例化。`Space.2` 这类 token 定义为 `x:Double`（标量 8），Padding 需要 `Thickness`，类型转换失败直接抛异常——**启动即崩，且只在命中该样式的控件进入可视树时发生**。

**排查**：全局搜 `Property="Padding"` 的下一行 Value，凡 `{StaticResource Space.*}`（标量）都是雷。

**修复**：Padding / Margin / BorderThickness 一律用 Thickness token（`Pad.*`）或字符串形式 `"12,0"`。

## 崩溃二：BoxShadow 带 px 单位

**错误原文**：

```text
System.FormatException: The input string '-24px' was not in a correct format.
   at System.Double.Parse(...)
   at Avalonia.Media.BoxShadow.Parse(String s)
```

**根因**：`BoxShadow.Parse` 用 `double.Parse` 解析偏移量（源码 `src/Avalonia.Base/Media/BoxShadow.cs`），**只接受纯数值**，CSS 风格的 `px` 后缀直接 FormatException。CSS 经验照搬是主要来源。

**错误写法**：`<Setter Property="BoxShadow" Value="-24px 0 48px 0 #80000000" />`

**修复**：`-24 0 48 0 #80000000`（负偏移合法，单位非法）。建议进 Shadows.axaml 统一为 token。

## 崩溃三：动画属性无 Animator

**错误原文**：

```text
System.InvalidOperationException: No animator registered for the property RenderTransform.
   at Avalonia.Animation.Animation.InterpretKeyframes(Animatable control)
```

**根因**：KeyFrame 的 `Property="RenderTransform"` 写 `rotate(0deg)` 这类 TransformOperations 字符串时，动画系统按属性类型找 Animator；Transform 类型需要显式注册或换安全写法（源码 `Animation.cs` InterpretKeyframes，找不到 handler 即抛）。异常发生在**动画首次应用**（spinner 进入可视树时），常表现为启动崩或页面首次加载崩。

**修复**：见 animation.md 的 RotateTransform.Angle 安全写法。

## 崩溃四：增量构建掩盖 AVLN 编译错误

**错误原文**：

```text
No precompiled XAML found for view <视图类全名>.
```

**根因**：上一次全量编译失败后，后续 `dotnet build` 走增量构建、跳过 CoreCompile，axaml 的 AVLN 编译错误（如属性名写错：`u:Loading` 的 `IsRunning`，实际是 `IsLoading`）完全不出现，报「已成功生成 0 警告 0 错误」，给人虚假信心。运行到该视图时才发现 DLL 内根本没有预编译 XAML。

**排查**：运行时报 `No precompiled XAML found`，优先怀疑上一次全量编译失败被增量掩盖；grep 产物 DLL 是否含 `XamlIl` 字符串可快速确认（无字符串 = 没有预编译 XAML）。

**修复**：axaml 改动后的首次验证用 `dotnet build --no-incremental`（或删除主工程 obj/bin 后重建），让 AVLN 错误真实暴露；修复至全量构建通过后再启动。

## 通用排查流程

```text
1. 前台启动进程抓 stderr（Windows：exe 2>crash.log，禁止 && 链式命令）
2. 看异常类型与顶层栈帧：
   InvalidCastException + Setter.Instance → 样式 Setter 值类型错（崩溃一）
   FormatException + BoxShadow.Parse → 阴影单位（崩溃二）
   InvalidOperationException + No animator registered → 动画（崩溃三）
   No precompiled XAML found → 增量构建掩盖编译错误（崩溃四）
3. 定位样式：异常发生在哪类控件附加时 → 全局搜对应 Selector
4. 修复后必须：dotnet build --no-incremental + 实际启动 + 走一遍触发该样式的界面
```

## 预防规则（写代码时）

1. 新样式 Setter 用资源时，先确认 token 的 XAML 类型（`x:Double` / `Thickness` / `CornerRadius` / `BoxShadows` / `SolidColorBrush`）
2. 阴影、动画这类「CSS 直觉强」的属性，写之前回本清单或源码核对格式
3. 样式文件改动后，至少启动一次应用并冒烟到会渲染新样式的界面
4. axaml 改动后的首次构建验证用 `--no-incremental` 全量构建（增量跳过 CoreCompile 时 AVLN 错误不出现），通过后再启动冒烟
