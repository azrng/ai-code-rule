# TreeView 组件（树形选择）

**使用场景**：角色权限分配、菜单树选择、部门树选择。

```razor
<TreeView Items="@TreeItems"
          OnTreeItemChecked="OnTreeItemChecked"
          IsAccordion="false"
          ShowCheckbox="true"
          ClickToggleNode="true"
          AutoCheckChildren="true"
          AutoCheckParent="true"/>
```

**构建树节点**：
```csharp
private List<TreeViewItem<MenuNode>> BuildTreeItems(List<MenuNode> menus)
{
    return menus.Select(menu =>
    {
        var isChecked = CheckedIds.Contains(menu.Id);
        var item = new TreeViewItem<MenuNode>(menu)
        {
            Text = menu.Name,
            Icon = menu.Icon ?? "fa-solid fa-folder",
            HasChildren = menu.Children?.Count > 0,
            IsExpand = true,
            CheckedState = isChecked ? CheckboxState.Checked : CheckboxState.UnChecked
        };
        if (menu.Children?.Count > 0)
            item.Items = BuildTreeItems(menu.Children);
        return item;
    }).ToList();
}
```

**勾选事件处理**：
```csharp
private Task OnTreeItemChecked(List<TreeViewItem<MenuNode>> items)
{
    SelectedItems = new List<TreeViewItem<MenuNode>>(items);
    return Task.CompletedTask;
}
```

**CRITICAL - 强制重建 TreeView 防止状态污染**：
```csharp
private string TreeViewKey = Guid.NewGuid().ToString();
private List<TreeViewItem<MenuNode>> TreeItems = new();
private HashSet<string> CheckedIds = new();

public async Task ShowAsync(Entity entity)
{
    CurrentEntity = entity;

    // 1. 清除状态
    SelectedItems = new();
    CheckedIds = new();
    TreeItems = new();

    // 2. 触发渲染清除旧 TreeView
    StateHasChanged();

    // 3. 加载数据
    await LoadCheckedIds();
    await LoadTreeData();

    // 4. 加载后更改 Key（强制重建）
    TreeViewKey = Guid.NewGuid().ToString();

    // 5. 触发渲染重建 TreeView
    StateHasChanged();

    // 6. 模态框在渲染周期完成后显示
    await InvokeAsync(async () =>
    {
        if (Modal != null) await Modal.Show();
    });
}
```

```razor
<!-- Key 绑定确保强制重建 -->
<TreeView Key="@TreeViewKey" Items="@TreeItems" .../>
```

**模型要求**：数据模型必须实现 `Equals` 和 `GetHashCode`：
```csharp
public override bool Equals(object? obj) =>
    obj is MenuNode other && Id == other.Id;
public override int GetHashCode() => Id?.GetHashCode() ?? 0;
```

---

# Toast 通知

```csharp
// 简写方法（推荐）
await ToastService.Success("消息通知", "操作成功");
await ToastService.Error("消息通知", "操作失败");
await ToastService.Warning("消息通知", "请注意");
await ToastService.Information("消息通知", "提示信息");

// 完整配置
await ToastService.Show(new ToastOption
{
    PreventDuplicates = true,
    Category = ToastCategory.Success,
    Title = "消息通知",
    Content = "操作成功"
});
```

**自定义位置**：
```csharp
[CascadingParameter] private BootstrapBlazorRoot? Root { get; set; }
private ToastContainer? ToastContainer { get; set; }

protected override void OnInitialized()
{
    ToastContainer = Root.ToastContainer;
}

// 设置位置
ToastContainer?.SetPlacement(Placement.TopCenter);
```

