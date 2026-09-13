# 布局与虚拟化

## 面板家族

| 面板 | 用途 | 要点 |
|---|---|---|
| Grid | 结构化布局 | 紧凑语法定义 `"Auto,*,Auto"`；`Grid.SetColumn(child, i)` 代码挂列 |
| StackPanel | 线性堆叠 | **`Spacing` 属性是 Avalonia 独有**（WPF 没有），替代手动 Margin |
| WrapPanel / UniformGrid / DockPanel / Canvas / RelativePanel | 特殊排布 | 按需 |
| SplitView | 抽屉/侧滑 | `OpenPaneLength`、`DisplayMode` |
| GridSplitter | 分隔拖拽 | `ResizeDirection="Rows"/"Columns"`；宿主行高建议 `Auto/固定/*` 三段式（消费工程工作台上下分栏模式） |

## 对齐与裁剪

- `HorizontalAlignment` / `VerticalAlignment`：`Stretch` 是多数容器的默认；显式 `Left/Center/Right` 会取消拉伸。
- `MinWidth=0` + `ClipToBounds=True`：滚动视口与嵌套 Grid 的防溢出组合（消费工程标签视口写法）。
- 负 Margin 合法（分裂按钮拼边 `-1,0,0,0`），但注意命中测试区域随之偏移。

## 列表虚拟化（高频性能坑）

**`ItemsControl` 默认模板的 ItemsPanel 是普通 StackPanel，数据多时全部实例化，虚拟化静默失效。**

大数据列表必须用支持虚拟化的宿主与面板：

```xml
<ListBox ItemsSource="{Binding Rows}">
  <ListBox.ItemsPanel>
    <ItemsPanelTemplate>
      <VirtualizingStackPanel />
    </ItemsPanelTemplate>
  </ListBox.ItemsPanel>
</ListBox>
```

- 虚拟化宿主：ListBox / ItemsControl 需要模板内是 `ScrollViewer + VirtualizingStackPanel`（`VirtualizingCarouselPanel` 等也在家族里）。
- 外层容器给列表一个**高度约束**（`Row Height="*"` 或显式 Height），无界高度会让虚拟化退化（面板认为无限高，全部实例化）。
- 集合变更用批量替换（单次 Reset 事件），避免逐项 Add 触发 N 次布局：

```csharp
public static void ReplaceWith<T>(this ObservableCollection<T> items, IEnumerable<T> nextItems)
{
    items.Clear();
    foreach (var item in nextItems) items.Add(item);
}
```

## 标准骨架（消费工程 MainWindow 模式）

```xml
<Grid RowDefinitions="Auto,*">
  <views:TitleBar Grid.Row="0" />
  <Grid Grid.Row="1">
    <views:HomeView IsVisible="{Binding IsHomeTabActive}" />
    <ContentControl Classes="ProjectWorkspaceHost"
                    IsVisible="{Binding HasActiveProjectTab}"
                    Content="{Binding ActiveProjectTab}" />
    <u:OverlayDialogHost />
  </Grid>
</Grid>
```

按 `IsVisible` 互斥切换 + ContentControl 承载当前工作区；弹层宿主常驻顶层。
