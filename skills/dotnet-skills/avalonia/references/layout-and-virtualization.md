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
- 虚拟化前提是列表**自身高度有界**：外层容器给 `Row Height="*"` 或显式 Height；把列表包进外层 `StackPanel + ScrollViewer` 会传入无界测量高度，任何虚拟化面板都静默退化为全量实例化——滚动交给列表自身模板内的 ScrollViewer（ListBox 默认内置），不要在外面再包一层。
- 集合变更用批量替换（单次 Reset 事件），避免逐项 Add 触发 N 次布局：

```csharp
public static void ReplaceWith<T>(this ObservableCollection<T> items, IEnumerable<T> nextItems)
{
    items.Clear();
    foreach (var item in nextItems) items.Add(item);
}
```

### TreeView 的虚拟化边界

- TreeView 只有**顶层**可能虚拟化（ItemsPanel 换成 `VirtualizingStackPanel` 时生效）；**子层级无法虚拟化**——子项宿主在 `TreeViewItem` 模板的 `ItemsPresenter` 内、没有自己的滚动视口，子层级再配 `VirtualizingStackPanel` 也不生效，展开即全量实例化。这是结构性限制而非版本 bug（Avalonia 12.1.2 源码 + 上游 discussion #12436 / issue #6580 核实），升级大版本不改变该行为。
- 大数据树形展示按优先级三选一：**扁平化为 `ListBox + VirtualizingStackPanel`**（层级并入数据行，深度用缩进 / Padding 表达）；**子级懒加载**（展开时才构建子集合，收起不保留实例化节点）；或大数据量直接用 **TreeDataGrid**（独立包 `Avalonia.Controls.TreeDataGrid`，层级数据全量虚拟化）。节点总量无法控制又必须用 TreeView 时，明确接受全量实例化的代价并压测确认。

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
