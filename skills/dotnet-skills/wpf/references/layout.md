# 布局

## 布局原则

WPF 布局两阶段：**测量**（容器逐个问子元素期望尺寸）→ **排列**（容器安排位置）。理解两阶段才能解释「为什么套太多层嵌套会慢」「为什么 `ActualHeight` 在布局完成前是 0」。

- 不显式设定元素尺寸：让元素适配内容，用 `Min*/Max*` 限制范围
- 不用屏幕坐标定位：位置由容器决定，间隔用 `Margin`
- 子元素共享可用空间：`*` 比例分配剩余空间
- 容器可嵌套：典型骨架是外层 `Grid` 定区域，区域内放 `StackPanel` / `DockPanel` / 业务控件

## 面板选型表

| 面板 | 排列方式 | 典型场景 | 注意 |
|---|---|---|---|
| `Grid` | 行 × 列表格 | 窗口骨架、表单对齐 | 功能最强；行列定义三种宽度（固定 / `Auto` / `*`） |
| `StackPanel` | 单向顺序堆叠 | 一组按钮、一行输入 | 不裁剪也不换行，内容超出直接溢出；别拿它当列表容器（无虚拟化） |
| `DockPanel` | 停靠边缘 + 余量填充 | 工具栏 / 状态栏 / 主内容区 | `LastChildFill` 默认 true，最后一个子元素占剩余全部；`Dock` 无 Fill 值 |
| `WrapPanel` | 流式排列，满了换行/列 | 卡片墙、标签云 | `ItemWidth`/`ItemHeight` 统一单元格尺寸，超尺寸子元素被截断 |
| `Canvas` | 绝对坐标 | 刻度绘制、流程图节点、拖拽画板 | 不参与自适应；窗口小于画布内容时直接不可见 |
| `UniformGrid` | 等分单元格 | 键盘按键、九宫格 | 无 `Row/Column` 附加属性；可只设 `Rows` 或 `Columns`，另一个方向自动扩展 |

## Grid 要点

```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="40"/>   <!-- 固定 -->
        <ColumnDefinition Width="Auto"/> <!-- 按内容最小需要 -->
        <ColumnDefinition Width="2*"/>   <!-- 占剩余宽 2/3 -->
        <ColumnDefinition Width="*"/>    <!-- 占剩余宽 1/3 -->
    </Grid.ColumnDefinitions>
    <Button Content="btn" Grid.Column="1" Grid.ColumnSpan="2"/> <!-- 跨列 -->
</Grid>
```

- 行用 `Grid.RowDefinitions` / `RowDefinition Height`，子元素 `Grid.Row` / `Grid.RowSpan`
- 同时给了 `Grid.Column` 与 `Grid.ColumnSpan` 时从 Column 起跨 Span 列
- `GridSplitter` 可拖动调整行列宽度，配 `*` 长度使用才有拖动效果

## DockPanel 要点

```xml
<DockPanel>
    <Menu DockPanel.Dock="Top"/>
    <StatusBar DockPanel.Dock="Bottom"/>
    <Grid/>                     <!-- 最后的子元素填充剩余空间 -->
</DockPanel>
```

停靠顺序有语义：先 `Dock="Top"` 再 `Dock="Left"`，Left 是从 Top 之下的剩余区域起算。`LastChildFill="False"` 时最后子元素也按 Dock 停靠。

## 典型上位机窗口骨架

```xml
<DockPanel>
    <Menu DockPanel.Dock="Top"/>                          <!-- 菜单 -->
    <StackPanel DockPanel.Dock="Left" Width="80"/>        <!-- 侧边导航 -->
    <StatusBar DockPanel.Dock="Bottom">                   <!-- 连接状态、端口信息 -->
        <TextBlock x:Name="statusInfoTextBlock" Text="就绪"/>
    </StatusBar>
    <Grid>                                                <!-- 主工作区 -->
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="2*"/>                <!-- 监控曲线 / 画面 -->
            <ColumnDefinition Width="*"/>                 <!-- 参数面板 -->
        </Grid.ColumnDefinitions>
    </Grid>
</DockPanel>
```

## 常见错误

- `StackPanel` 里放超大列表期望滚动：`StackPanel` 给子元素无限高度，`ScrollViewer` 包它会让虚拟化失效、整表实例化——大列表用 `ListBox` / `ListView`（自带 `ScrollViewer` + 虚拟化）
- `ScrollViewer` 直接塞多个子元素：只允许一个子元素，多个先包一层面板
- 窗口缩放后控件消失 / 挤压：把最小可用尺寸写进 `MinWidth` / `MinHeight`（窗口与关键控件都要）
- 在 `Canvas` 上做表单然后用 `Canvas.Left` 微调：换 `Grid` + `Margin`
- `Width` 写死导致语言切换 / DPI 缩放下截断：用 `*` / `Auto` / `Min*`
