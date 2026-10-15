# 其他常用组件

## 7.1 Card 卡片
```razor
<Card class="mb-4 shadow-sm border-0">
    <HeaderTemplate>
        <div class="d-flex align-items-center">
            <i class="fas fa-bolt me-2"></i><span>标题</span>
        </div>
    </HeaderTemplate>
    <BodyTemplate>
        <!-- 内容 -->
    </BodyTemplate>
</Card>
```

## 7.2 Badge 徽章
```razor
<Badge Color="@(enabled ? Color.Success : Color.Danger)">
    @(enabled ? "启用" : "禁用")
</Badge>
```

## 7.3 Drawer 抽屉
```razor
<Drawer Placement="Placement.Right" @bind-IsOpen="@IsOpen" IsBackdrop="true">
    <!-- 内容 -->
</Drawer>
```

**编程式关闭**：`Close()` 是**公开方法**（`Drawer.razor.cs:233`，带 `[JSInvokable]`，供 ESC/遮罩点击回调，也可经 `@ref` 直接调用）；它先过 `OnClosingAsync` 拦截（返回 false 则中止），再置 `IsOpen = false`、调 JS `execute(id,false)`，动画时 `await Task.Delay(300)`，最后触发 `OnCloseAsync` / `IsOpenChanged`。父组件把 `IsOpen` 置 `false` 同样可行：组件在 `OnAfterRenderAsync` 调 JS `execute(id,false)` 关闭。注意两点：
- 保存成功后建议「先刷列表再收抽屉 + 显式 `StateHasChanged()`」，刷新异常不拖累关闭，也防电路内丢渲染；
- 关闭动画依赖 `requestAnimationFrame`（320ms 帧循环移除根元素 `.show`），rAF 节流环境（后台标签页/webview/自动化受控页面）根类残留导致"关不掉"，详见 `troubleshooting.md`。

## 7.4 Alert 警告
```razor
<Alert Color="Color.Info">信息提示</Alert>
<Alert Color="Color.Danger" ShowBorder="true" Icon="fa-solid fa-circle-exclamation">错误警告</Alert>
```

**Alert 参数**（`AlertBase` + `Alert.razor.cs`）：`Color`(Primary)、`ShowDismiss`、`Icon`（字符串，**没有 `ShowIcon`**）、`ShowBar`、`ChildContent`、`OnDismiss`，以及 `Alert` 上的 `ShowBorder`(false)、`ShowShadow`(false)。

## 7.5 Avatar 头像
```razor
<Avatar Url="@userAvatar" IsCircle="true" Size="Size.ExtraLarge"/>
```

## 7.6 Logout 登出组件
```razor
<Logout ImageUrl="@avatar" DisplayName="@displayName" UserName="@userName">
    <LinkTemplate>
        <a href="/profile"><i class="fa-solid fa-suitcase"></i>个人中心</a>
        <LogoutLink></LogoutLink>
    </LinkTemplate>
</Logout>
```

## 7.7 GroupBox 分组框
```razor
<GroupBox Title="搜索条件">
    <div class="row g-3 form-inline">
        <!-- 搜索字段 -->
    </div>
</GroupBox>
```

## 7.8 DropdownWidget 下拉挂件
```razor
<DropdownWidget class="px-3">
    <DropdownWidgetItem Icon="fa-regular fa-envelope" BadgeNumber="5" BadgeColor="Color.Danger">
        <HeaderTemplate><span>您有 5 条未读消息</span></HeaderTemplate>
        <BodyTemplate><!-- 消息列表 --></BodyTemplate>
        <FooterTemplate><a href="#">查看全部</a></FooterTemplate>
    </DropdownWidgetItem>
</DropdownWidget>
```

## 7.9 Progress 进度条
```razor
<Progress IsAnimated="true" IsStriped="true" Value="75" Color="Color.Primary"/>
```

---

# CSS 隔离

使用 `::deep` 穿透到子组件样式：

```css
/* ComponentName.razor.css */
::deep .form-control {
    border-radius: 8px;
}

::deep .layout .layout-side {
    background-color: var(--bs-dark);
}
```

**CSS 隔离文件命名**：必须为 `{ProjectName}.styles.css`（如 `RbacAuth.Front.styles.css`）。
