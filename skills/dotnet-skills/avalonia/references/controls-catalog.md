# 常用控件目录与易错点

全部以 12.1.2 源码与消费工程实际用法为准。查不到的控件先回 `src/Avalonia.Controls/` 确认存在再写。

## 输入与选择

| 控件 | 高频易错点 |
|---|---|
| TextBox | 水印是 `PlaceholderText`（不是 WPF 的 Tag 技巧）；多行 `AcceptsReturn="True"`；即时提交配 `UpdateSourceTrigger=PropertyChanged` |
| ComboBox | 用 `ItemsSource + SelectedItem`（对象引用匹配）；下拉打开伪类是 `:dropdownopen`；`iri:ClassHelper.Classes` 可动态挂配色类 |
| CheckBox | 三态用 `IsThreeState`；伪类 `:checked` |
| AutoCompleteBox | 异步建议用 `PopulateAsync`；注意 Text 绑定的提交时机 |
| Slider | 方向伪类 `:horizontal`/`:vertical`；拖动中 `:dragging` |

## 容器与列表

| 控件 | 高频易错点 |
|---|---|
| ItemsControl | 纯展示无选中；**默认不虚拟化**（见 layout-and-virtualization.md）；ItemTemplate 必须 `x:DataType` |
| ListBox | 选中项 `SelectedItem`；虚拟化需 `VirtualizingStackPanel` |
| TabControl / TabStrip | TabStrip 是纯选择器（无内容区），自绘标签条用它 + ItemTemplate |
| TreeView | 节点展开绑定 `IsExpanded` 需双向；层级模板 `TreeDataTemplate`（`ItemsSource` 表达子级） |
| SplitView | 抽屉场景（内容 + 可折叠侧板） |
| Expander | 展开态伪类 `:expanded`；箭头旋转用 Setter `RenderTransform="rotate(180deg)"`（静态变换合法） |

## 文本与展示

| 控件 | 说明 |
|---|---|
| SelectableTextBlock | 需要用户可复制文本（Body 查看器等） |
| TextBlock | `TextTrimming="CharacterEllipsis"` + `MaxWidth` 组合防溢出 |
| Border | `CornerRadius` 可四值字符串 `"8,8,0,0"`（左上,右上,右下,左下）；`BoxShadow` 无单位数值 |
| Path | 图标矢量；spinner 圆弧 `Data="M 7,1 A 6,6 0 1 1 1,7"` 配 RenderTransform 动画 |

## 菜单与弹层

| 控件 | 说明 |
|---|---|
| Button.Flyout | toggle 语义；内容自定义（Flyout）或菜单（MenuFlyout）；陷阱见 windows-flyouts-popups.md |
| ContextMenu | 任意控件可挂；`contextMenu.Open(button)` 可编程打开 |
| Menu / MenuFlyout | MenuItem 支持 Icon + InputGesture 显示（快捷键展示，不自动绑定按键） |

## 字体图标

Segoe Fluent Icons / MDL2 字形必须显式设字体，否则渲染空白或方块：

```xml
<TextBlock FontFamily="Segoe Fluent Icons, Segoe MDL2 Assets" Text="&#xE70D;" />
```

Windows 目标下同时声明两族（Fluent 缺字符回退 MDL2）。

## 状态四态（消费工程 UI 约定）

列表/面板类界面必须完整覆盖：`loading`（spinner 遮罩）、`empty`（占位插画+文案）、`error`（危险横幅）、`no-permission`。用 ViewModel 布尔属性组合控制 IsVisible。
