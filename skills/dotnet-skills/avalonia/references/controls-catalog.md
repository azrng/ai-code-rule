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

### Semi 主题能力：可清除输入框（一行类名）

Semi 的 TextBox ControlTheme 模板内置 `PART_ClearButton`，挂类即得清除按钮，**不用自造「输入框 + 清除图标」组合**：

```xml
<TextBox Classes="clearButton" Text="{Binding SearchKeyword}" ... />
```

- 显示条件（源码 TextBox.axaml `^.clearButton` 样式核实）：获得焦点或悬停、文本非空、`AcceptsReturn=False`、非只读；
- 清除按钮 `Command="{Binding $parent[TextBox].Clear}"`，清空的是 TextBox 自身 Text 属性，变化沿 TwoWay 绑定回传 ViewModel，无需额外处理；
- 类名 `clearButton` 与 `ClearButton` 均可，可与业务样式类并存（如 `Classes="modern clearButton"`）。

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

## Ursa 控件速记（Irihi.Ursa 2.2.0 源码核实）

**没有这些控件，别凭经验捏造**：Ursa 无 TreeView / SearchBox / EmptyState / Segmented / 独立 Tag / Card / SelectableCard（全仓库 grep 核实，2.2.0）。树形下拉用 `TreeComboBox`，卡片式选择用 `SelectionList`/`SelectionListItem`，枚举分段选择用 `EnumSelector`；空态仍需自建（Ursa 只有 `Skeleton` / `Shimmer` / `Loading` 家族可辅助）。Semi.Avalonia 是纯主题库，没有自定义控件类。导航选 `NavMenu`，表单布局选 `Form` / `FormItem` / `FormGroup`，键值对展示选 `Descriptions`，消息通知用 `WindowToastManager` / `MessageBox` / `Dialog` / `Drawer` / `PopConfirm` 全家桶。

| 控件 | 高频易错点 |
|---|---|
| UrsaWindow / TitleBar | 托管窗口装饰与标题栏槽位，用法见 windows-flyouts-popups.md「窗口装饰」节 |
| ThemeToggleButton | Default 请求变体首击抛异常坑，见下 |
| Loading / LoadingIcon / LoadingContainer | 加载状态标准件，见下 |
| EnumSelector | 已验证可直接替换枚举 RadioButton 组（消费工程数据库工作台在用） |

### ThemeToggleButton：Default 请求变体首击异常坑

源码事实（`ThemeToggleButton.cs`，2.2.0 核实）：点击时 `_state` 为 `null` 且 `IsThreeState=False` 直接 `throw new InvalidOperationException("Invalid state")`。`_state` 由 `SyncThemeFromScope` 设置：Dark→false、Light→true、**Default/null→null**。默认 `Mode=Controller` 从 `Application.RequestedThemeVariant` 同步——应用初始请求变体为 Default（跟随系统的常见配置）时，首击即崩。

安全写法（消费工程实测）：

```xml
<u:ThemeToggleButton Mode="Indicator" IsThreeState="False" />
```

- `Mode=Indicator` 从 `ActualThemeVariant` 同步（实际变体永不为 Default），两态循环 Light↔Dark；
- `IsThreeState="True"`（Ursa demo用法）走 Default→Light→Dark 三态循环，同样安全；
- 控件只设置 `Application.Current.RequestedThemeVariant`，**不做持久化**——偏好落盘需自行监听 `Application.Current.ActualThemeVariantChanged` 后写入设置服务（事件里读 `RequestedThemeVariant`）。

### LoadingContainer：加载遮罩标准件

```xml
<u:LoadingContainer IsLoading="{Binding IsLoading}">
    <u:LoadingContainer.LoadingMessage>
        <StackPanel>
            <TextBlock Text="正在加载..." />
        </StackPanel>
    </u:LoadingContainer.LoadingMessage>
    <!-- 任意内容 -->
</u:LoadingContainer>
```

- `IsLoading=True` 时盖罩（`LoadingMaskBackground`）+ 旋转指示器 + `LoadingMessage`；`:loading` 伪类自动给内容加 `blur(5)` 并由盖罩屏蔽交互。替代手写「IsVisible 遮罩 Border + ProgressBar」样板；
- 行内加载指示（工具条、按钮旁）用 `u:LoadingIcon`，尺寸类 `Small` / `Large`。

## 状态四态（消费工程 UI 约定）

列表/面板类界面必须完整覆盖：`loading`（`u:LoadingContainer` 包裹内容出遮罩）、`empty`（占位插画+文案）、`error`（危险横幅）、`no-permission`。用 ViewModel 布尔属性组合控制 IsVisible。
