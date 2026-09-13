# 绑定与 MVVM

## 编译绑定

`AvaloniaUseCompiledBindingsByDefault=true` 时：

- Window / UserControl / 每个 DataTemplate 都必须写 `x:DataType`，否则 `AVLN2000` 编译错误：

```xml
<UserControl x:DataType="vm:ProjectTabViewModel" ...>
  <ItemsControl ItemsSource="{Binding Workspace.VisibleWorkspaceTabs}">
    <ItemsControl.ItemTemplate>
      <DataTemplate x:DataType="vm:RequestWorkspaceTabViewModel">
        <TextBlock Text="{Binding HeaderText}" />
      </DataTemplate>
    </ItemsControl.ItemTemplate>
  </ItemsControl>
</UserControl>
```

- 绑定路径类型不匹配、属性不存在 → `AVLN2000`（编译期暴露，好事）。
- 模板内需要外层 DataContext 时用 `$parent` 强转，**括号与强转不能省**：

```xml
Command="{Binding $parent[Window].((vm:MainWindowViewModel)DataContext).ActiveProjectTab.Workspace.CloseWorkspaceTabCommand}"
CommandParameter="{Binding}"
```

- 逃生口：编译绑定覆盖不了的场景（如样式内绑定控件自身属性）用 `ReflectionBinding`：

```xml
<Setter Property="IsExpanded" Value="{ReflectionBinding IsExpanded, Mode=TwoWay}" />
```

- 即时更新文本框绑定：`Text="{Binding X, UpdateSourceTrigger=PropertyChanged}"`。

## CommunityToolkit.Mvvm 约定（消费工程硬规则）

```csharp
public partial class ProjectWorkspaceTabsViewModel : ViewModelBase   // 基类最终继承 ObservableObject
{
    [ObservableProperty]
    private bool isWorkspaceTabMenuOpen;          // 生成 IsWorkspaceTabMenuOpen 属性

    [RelayCommand]
    private void CloseWorkspaceTab(RequestWorkspaceTabViewModel? tab) { ... }   // 生成 CloseWorkspaceTabCommand

    [RelayCommand]
    private async Task OpenNewTabAsync() { ... }  // 生成 OpenNewTabCommand（异步）

    partial void OnActiveWorkspaceTabChanged(RequestWorkspaceTabViewModel? oldValue,
        RequestWorkspaceTabViewModel? newValue) { ... }   // 属性变更钩子
}
```

- 属性通知一律 `[ObservableProperty]`，命令一律 `[RelayCommand]`，禁止手写 `INotifyPropertyChanged` 样板。
- 依赖通过构造函数注入。
- 跨 ViewModel 通信用 `IMessenger` 弱引用消息（`WeakReferenceMessenger`），不用静态全局状态。

## 常见绑定错误对照

| 症状 | 原因 | 处理 |
|---|---|---|
| `AVLN2000: Unable to resolve property ... on type ...` | 路径在 x:DataType 类型上不存在，或DataContext 层级换了 | 检查 x:DataType；跨层级用 `$parent[...]` 强转 |
| 运行时绑定不生效但编译过 | 用了 ReflectionBinding 或字符串路径 | 核对路径与 DataContext |
| 双向绑定 UI 不回写 | 漏 `Mode=TwoWay`（SelectedIndex 等默认双向的除外） | 显式 `Mode=TwoWay` |
| DataTemplate 内绑定外层 VM 报错 | 模板已切到条目类型 | `$parent[Window].((vm:X)DataContext)` |
