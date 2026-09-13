# 窗口、弹层与平台 API

## 自绘窗口（消费工程 MainWindow 模式）

```xml
<Window WindowDecorations="None"
        ExtendClientAreaToDecorationsHint="True"
        WindowState="Maximized" ...>
```

- 标题栏拖动：`window.BeginMoveDrag(e)`（在 `PointerPressed` 中，左键时触发）。
- 最大化/还原切换：`window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized`。
- 最小化：`window.WindowState = WindowState.Minimized`；关闭：`window.Close()`。

## Flyout / Popup 行为（含两个高频陷阱）

### Button.Flyout 标准接法

```xml
<Button x:Name="CaretButton" ToolTip.Tip="更多保存方式">
  <Button.Flyout>
    <Flyout Placement="BottomEdgeAlignedRight" ShowMode="Transient">
      <Border Classes="MenuCard"><!-- 自定义内容 --></Border>
    </Flyout>
  </Button.Flyout>
</Button>
```

- `Button.Flyout` 是 toggle 语义：点击打开 → 再点关闭（Button.OnClick 内 `_isFlyoutOpen` 分支，源码 Button.cs）。
- 菜单项点击后手动收层：`if (button.Flyout is Flyout f && f.IsOpen) f.Hide();`（业务放 Command，收层放 Click 处理器）。
- 图标字形（Segoe Fluent Icons / MDL2）必须显式设字体，否则渲染为空白：`FontFamily="Segoe Fluent Icons, Segoe MDL2 Assets"`。

### 陷阱一：窗口失活时的首击，弹层「打开即秒关」

源码事实（Popup.cs，12.1.2 核实）：`Popup` 在光熄灭模式（`IsLightDismissEnabled=true`，Flyout 默认）下订阅了宿主 Window 的 `Deactivated`、平台 `LostFocus` 与 `TopLevelLostPlatformFocus`，任一触发即 `Close()`。**窗口不在前台时的首次点击（切换窗口回来的第一击、自动化后台点击）伴随激活抖动，Transient 弹层显示后立即被关闭**——现象就是「点了 caret 没有菜单」。

处理模式（消费工程实测有效）：

```csharp
private void OnCaretButtonClick(object? sender, RoutedEventArgs e)
{
    if (sender is not Button { Flyout: Flyout flyout } caretButton) return;
    if (TopLevel.GetTopLevel(this) is not Window window || window.IsActive) return;

    // 窗口失活时的首击：等激活稳定后在后台优先级补一次显示
    Dispatcher.UIThread.Post(() =>
    {
        if (!flyout.IsOpen) flyout.ShowAt(caretButton);
    }, DispatcherPriority.Background);
}
```

### 陷阱二：ShowMode 差异

- `Transient`：点外面 / 失焦 / 滚动即关（默认推荐）。
- `TransientWithDismissOnPointerMoveAway`：指针离开弹层一定距离才关。
- `Standard`：只通过点击外部或其他交互关闭，且打开时会尝试把焦点移入弹层内容。

### 排查弹层问题时

`Popup` 双宿主：窗口化 `PopupRoot` 与 Overlay 内 `OverlayPopupHost`。窗口化弹层在 OS 层有独立 HWND（`WM_MOUSEACTIVATE` 返回 `MA_NOACTIVATE`，不抢前台）；弹层异常消失优先怀疑光熄灭链路（Deactivated / LostFocus / NonClientClick）而非渲染。

## 模态对话框模式（官方样例）

非模态弹层之外需要**模态确认 / 输入对话框**时，优先用项目既有方案（消费工程：Ursa `OverlayDialogHost`）；从零搭建或要理解 ViewModel 驱动对话框的范式，读官方样例：

- `Avalonia.Samples/src/Avalonia.Samples/ViewInteraction/MvvmDialogSample/`——Window 作为对话框、`window.ShowDialog()` 异步等待结果
- `Avalonia.Samples/src/Avalonia.Samples/ViewInteraction/DialogManagerSample/`——`DialogManager` + `IDialogParticipant` 的 ViewModel 驱动多对话框管理（含输入对话框）

危险操作必须二次确认；对话框结果用异步返回，不用隐式全局状态。

## 平台 API（12.x 真实签名）

### 剪贴板（11.x → 12.x 破坏性变更，重灾区）

12.x 的 `IClipboard` **接口本身只有** `ClearAsync / SetDataAsync(IAsyncDataTransfer?) / FlushAsync / TryGetDataAsync / TryGetInProcessDataAsync`（源码 `src/Avalonia.Base/Input/Platform/IClipboard.cs`，12.1.2 tag 核实）。11.x 的接口成员 `SetTextAsync / GetTextAsync` 已不存在。

文本写入的正确姿势（二选一）：

```csharp
using Avalonia.Input.Platform;   // ClipboardExtensions 在这里，漏 using 会报「IClipboard 不包含 SetTextAsync」

// 1. 扩展方法快捷方式
await clipboard.SetTextAsync(text);

// 2. 显式 DataTransfer 方式
await clipboard.SetValueAsync(DataFormat.Text, text);   // DataFormat 在 Avalonia.Input 命名空间
```

读取：`var? text = await clipboard.TryGetTextAsync();`（注意是 `TryGetTextAsync`，不是 11.x 的同步风格 `GetTextAsync`）。

获取剪贴板：`TopLevel.GetTopLevel(control)?.Clipboard`。

### 文件对话框（不是 Win32 OpenFileDialog）

```csharp
var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
{
    Title = "选择文件",
    AllowMultiple = false
});
```
