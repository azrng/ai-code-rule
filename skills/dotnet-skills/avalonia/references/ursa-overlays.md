# Ursa 弹层：Dialog / Drawer / MessageBox 装配与高频用法

适用 Ursa.Avalonia 2.2.0（Irihi.Ursa / Irihi.Ursa.Themes.Semi）。API 形态在 `D:\SourceCode\avalonias\Ursa.Avalonia` 源码逐一核实，装配与关闭链路在消费工程弹窗迁移任务（11 个弹窗：新建项目 / 环境管理 / 设置 / 保存 / 生成代码 / 导入 / 确认类）中实测通过。

## 装配

1. 包：`Irihi.Ursa` + `Irihi.Ursa.Themes.Semi`（主题一起引）；关闭契约接口在传递依赖 `Irihi.Avalonia.Shared.Contracts` 里。
2. 主窗口放宿主：`<u:OverlayDialogHost />`。
3. **宿主定位**：`OverlayDialogManager.GetHost(hostId, topLevelHashCode)`。多窗口时传 `options.TopLevelHashCode = mainWindow.GetHashCode()` 指定宿主；不传取第一个注册的宿主。
4. **模态范围 = 宿主的视觉范围**：遮罩只盖宿主所在区域。自绘窗口（`WindowDecorations="None"`）必须把宿主放在**覆盖标题栏的最外层 Grid**；宿主在内容区时，弹窗打开期间标题栏仍可点，切标签 / 开新弹窗会留下过期浮层（消费工程修复实录）。

## 内容对话框（ShowStandardAsync）

```csharp
public async Task<DialogResult> ShowDialogAsync<TView>(object dataContext, string? title = null,
        bool canResize = false, bool showCloseButton = true) where TView : Control, new()
{
    return await OverlayDialog.ShowStandardAsync(new TView(), dataContext, null, new OverlayDialogOptions
    {
        Title = title,
        Buttons = DialogButton.None,      // 按钮区交给内容自带（保留主/次/危险按钮语义）
        IsCloseButtonVisible = showCloseButton,
        CanLightDismiss = false,          // 点遮罩不关闭，对齐传统模态
        CanResize = canResize,
        TopLevelHashCode = _windowHostService.MainWindow?.GetHashCode()
    });
}
```

- `OverlayDialogOptions` 关键成员：`Title`、`Buttons`（None/OK/OKCancel/YesNo/YesNoCancel，仅 Standard 模式生效）、`IsCloseButtonVisible`（仅 Custom 模式生效）、`CanLightDismiss`、`CanDragMove`、`CanResize`、`StyleClass`、`TopLevelHashCode`。
- 返回 `Task<DialogResult>`（None/OK/Cancel/Yes/No）。**带自定义负载的结果用 `ShowCustomAsync<TResult>`**：内部 `args.Result is T` 命中才返回该值，否则 default。

## 关闭契约：IDialogContext（核心）

`ShowStandardAsync` / `ShowCustomAsync` 创建的弹层控件检查 **DataContext 是否实现 `IDialogContext`**（命名空间 `Irihi.Avalonia.Shared.Contracts`）：

```csharp
public interface IDialogContext
{
    event EventHandler<object?>? RequestClose;   // args 即结果负载（DialogResult 或自定义对象）
    void Close();                                 // 约定实现为 RequestClose?.Invoke(this, null)
}
```

- VM 内关闭：`RequestClose?.Invoke(this, DialogResult.OK / Cancel / None)`，弹层关闭、`ShowStandardAsync` 的 await 恢复。
- chrome 右上角关闭钮（以及 `DialogControlBase.CanClose` 附加属性标记的元素）→ `Close()` → `RequestClose(null)` → `DialogResult.None`。
- **DataContext 未实现 IDialogContext** 时，chrome 关闭按 `Buttons` 枚举折算结果（如 OKCancel → Cancel）。
- 订阅时机：弹层控件 DataContext 一变即订阅（`OverlayFeedbackElement.OnDataContextChange`），关闭后不退订——同一 VM 实例反复开关安全（旧控件的 ShowAsync 处理器在首次关闭已移除，不存在重复 SetResult）。

## 高频坑

### 1. 叠加弹窗共用同一 DataContext = 一起被关

RequestClose 是 DataContext 上的一条事件通道，**所有** DataContext 相同的已打开弹层都会收到。确认弹窗叠在主弹窗上（典型：导入 → 检测冲突 → 覆盖确认）时，给上层弹窗独立 IDialogContext（薄包装 VM 持有主 VM + Confirm/Cancel 命令），否则确认/取消会连带关掉底层弹窗。同一时刻互斥（模态遮罩挡住彼此触发点）的弹窗才可共用通道。

### 2. await 弹窗别放进「finally 才释放状态」的作用域

```csharp
// 错误：确认弹窗在 try 内等待，执行导入时 IsBusy 仍为 true，
// 导入方法自身的 busy 守卫会拦截自己（现象：弹窗确认后什么都没发生）
try { ...; if (冲突) { await 确认弹窗; await 执行导入(); } } finally { IsBusy = false; }

// 正确：预览流程先 return（finally 释放 busy），确认与执行放独立异步方法
if (冲突) { 暂存待执行请求; _ = AwaitConfirmAndExecuteAsync(); return; }
```

### 3. 同步上下文打开弹窗

命令必须保持同步签名时 `_ = ShowAsync()` fire-and-forget（内部 try/catch 转用户可见反馈）。宿主缺失（单测 / 宿主未挂载）时 `OverlayDialog.*` 直接返回已完成任务（默认结果），不会挂起——VM 层单测因此可整体覆盖弹窗流程。

### 4. 宿主 API 边界

`OverlayDialogHost.AddModalDialog / AddDialog` 是 **internal**：外部无法直接驱动宿主，也拿不到弹层控件句柄（想「按内容找回控件关闭」此路不通），关闭一律走 IDialogContext 或 CanClose 附加属性。

## 抽屉与 MessageBox

- 抽屉：`OverlayDrawer.ShowStandardAsync(view, vm, null, new DrawerOptions { Buttons = DialogButton.None, Title = "标题", Position = Position.Right, MinWidth = 420, MaxWidth = 460, CanResize = true, TopLevelHashCode = ... })`。`DrawerOptions` 才有 MinWidth/MaxWidth，`OverlayDialogOptions` 没有（尺寸走内容样式）。
- MessageBox：`MessageBox.ShowOverlayAsync` 已 `[Obsolete]`（指向 `OverlayMessageBox.ShowAsync`）；`MessageBox.ShowAsync`（Window 模式）仍可用。

## Semi 主题渲染事实（StandardDialogControl 模板，Ursa.Themes.Semi）

- 内容区 `Margin="24,8"`，footer 区恒有 `Margin="24,0,24,24"`——`Buttons.None` 时底部仍有 24px 空白；内容视图不要再套卡片内边距。
- `Title = null` 且关闭钮隐藏时头部行（Auto 行）塌缩，不留空带。
- `CanResize = true` 时模板附加 `DialogResizer` 拖拽边。
- 卡片背景 / 圆角 / 阴影由主题 token（`DialogBackground` / `DialogCornerRadius` / `DialogBoxShadow`）提供，**业务卡片样式应收敛为纯尺寸**（Width / MaxHeight），否则出现双层卡片。

## 从「VM 布尔标志 + 手写遮罩」迁移的既定套路

- 手写宿主（Grid 叠 ContentControl + IsVisible 绑定 + 惰性实例化 + 遮罩/标题/主次按钮样式）整体删除；弹窗生命周期改命令式：打开命令 → 预填草稿 → `await ShowDialogAsync(...)` → 按结果恢复状态；关闭/取消命令只负责 `RequestClose`。
- 纯确认弹窗（删除 / 覆盖确认）：独立上下文 VM，Confirm/Cancel 只 raise RequestClose，业务在 await 之后编排。
- 单测：注入 Fake `IDialogService`（记录 ViewType/DataContext/Title + OnShow 钩子断言「弹窗展示时刻」的中间态 + 可编程 NextResult 默认 OK）；宿主缺失时流程自动走「弹窗即开即关」路径，取消分支照样可断言。注意 OnShow 钩子能拿到中间态，而 await 之后的断言只能看到流程结束态（确认成功会清掉 Pending 之类状态）。
