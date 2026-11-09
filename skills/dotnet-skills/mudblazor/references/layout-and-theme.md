# Layout 与 Theme

事实来源：`src/MudBlazor/Components/{Layout,AppBar,Drawer,Main,ThemeProvider,BreakpointProvider,Hidden,Grid,Stack,Container,Paper,Divider,Spacer,Card,Tabs,Stepper,ExpansionPanel}/` + `src/MudBlazor.Docs/Pages/Getting Started/Layouts/Examples/` + `Customization/Theming/Examples/`。参数默认值出自 `[Parameter]` + XML `<remarks>`。

## Layout 骨架

`MudLayout : MudDrawerContainer`，构造函数里 `Fixed=true`。标准组合：`MudLayout > MudAppBar + MudDrawer + MudMainContent`（完整骨架见 `project-setup.md`）。

### MudAppBar

| 参数 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `Fixed` | bool | **true** | 固定顶部；false 随页面滚动 |
| `Bottom` | bool | false | 放屏幕底部 |
| `Dense` | bool | false | 紧凑 |
| `Elevation` | int | **4** | 阴影 0–25 |
| `Gutters` | bool | true | 左右内边距 |
| `Color` | Color | Default | 主题色 |
| `WrapContent` | bool | false | 内容换行 |

### MudDrawer

| 参数 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `Open` | bool | false | `@bind-Open` 双向绑定，抽屉自关闭会回写 |
| `Anchor` | Anchor | Start | Left/Right/Start/End/Top/Bottom |
| `Variant` | DrawerVariant | Responsive | Temporary/Responsive/Persistent/Mini |
| `Breakpoint` | Breakpoint | Md | Responsive/Mini 在此断点以下变 Temporary |
| `Fixed` | bool | true | **仅当容器是 MudLayout 时生效**（`IsFixed => Fixed && DrawerContainer is MudLayout`） |
| `ClipMode` | DrawerClipMode | Never | Never=盖在 AppBar 上；Docked=压到 AppBar 下方推内容；Always=AppBar 下方盖内容。仅直接位于 MudLayout 内生效 |
| `Elevation` | int | 1 | |
| `Overlay` / `OverlayAutoClose` | bool | true / true | 遮罩与点遮罩自动关 |
| `OpenMiniOnHover` | bool | false | Mini 变体悬停展开 |
| `Width` / `MiniWidth` | string? | null | 裸容器时才用 Width；MudLayout 内改宽度用主题 `LayoutProperties.DrawerWidthLeft/Right`（默认 240px） |

陷阱：
- `Breakpoint.None` / `Breakpoint.Always` 是哨兵值：None=永远关闭、Always=永远打开
- persistent drawer 放进裸 `MudDrawerContainer` 时不占位（Fixed 失效）
- `MudLayout.ClampFixedHeader` 参数不存在，控制关系的是 `ClipMode`

### 预渲染下 `Open` 初值即首屏状态

`MudDrawer` 经 `IBrowserViewportService`（JS 视口观察）获知断点（本地源码 `MudDrawer.razor.cs:19/28/107`，初值 `Breakpoint.None`），预渲染期不可知断点，Responsive 变体的自动判定同样要等交互启动。因此预渲染输出直接取 `Open` 初值：`Open` 初值 false 时每次整页加载（刷新 / 直达 / 断线重连）先渲染抽屉关闭态，交互启动后抽屉滑出、顶栏与正文整体横移 DrawerWidth（默认 240px）——首屏必有横向布局跳动。桌面优先产品 `Open` 初值取 true、窄屏在交互启动后再收起；不要依赖 `Breakpoint` 首帧生效，也不要把初值置 false 等 JS 补偿（T013 曾因断点时序异常弃用 `MudDrawer.Breakpoint` 自适应）。

### MudMainContent

仅 `ChildContent`；`.mud-main-content` 自带 `padding-top: var(--mud-appbar-height)`（xs 竖屏 / dense AppBar 时为 calc 折减）为固定 AppBar 留出首屏空间。自定义 `padding` 简写或 `pa-*` 工具类（含 `!important`）会整体覆盖该顶栏留白，导致首行内容被固定顶栏遮挡；需自定义内边距时改用 `pt-*` 之外维度的工具类（如 `px-*` / `pb-*`）或在自定义样式里显式重设 `padding-top: var(--mud-appbar-height)`。

## Theme 与暗色模式

### MudThemeProvider 真实参数

| 参数 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `Theme` | MudTheme? | null | 主题实例；**换主题 = 换这个参数的实例** |
| `IsDarkMode` | bool | false | ParameterState；`@bind-IsDarkMode` 切深浅 |
| `ObserveSystemDarkModeChange` | bool | true | 默认跟随系统深色偏好 |
| `CurrentPalette` | Palette? | null | `@bind-CurrentPalette` 运行时换调色板 |
| `DefaultScrollbar` | bool | false | |

⚠️ 没有 `DarkMode` 参数；没有任何 `SetTheme` 方法（源码 0 命中）。读系统偏好：`await provider.GetSystemDarkModeAsync()`。

### 暗色切换标准写法（官方示例裁剪）

```razor
<MudThemeProvider @bind-IsDarkMode="_isDarkMode" Theme="_theme" />
<MudSwitch @bind-Value="_isDarkMode" T="bool" Label="深浅切换" Color="Color.Primary" />

@code {
    private bool _isDarkMode;
    private MudTheme _theme = new();
}
```

### MudTheme 结构

```csharp
private MudTheme _theme = new MudTheme()
{
    PaletteLight = new PaletteLight()
    {
        Primary = Colors.Blue.Default,
        Secondary = Colors.Green.Accent4,
        AppbarBackground = Colors.Red.Default,
    },
    PaletteDark = new PaletteDark() { Primary = Colors.Blue.Lighten1 },
    LayoutProperties = new LayoutProperties()
    {
        DrawerWidthLeft = "260px",
        DrawerWidthRight = "300px",
        DefaultBorderRadius = "6px",
        AppbarHeight = "64px",
    }
};
```

- `PaletteLight/PaletteDark` 属性：Primary/Secondary/Info/Success/Warning/Error/Dark、各 `*ContrastText`、TextPrimary/TextSecondary/TextDisabled、Surface/Background/DrawerBackground/AppbarBackground、HoverOpacity 等；色值用 `Colors.Blue.Default`、`Colors.Red.Lighten1` 常量
- 颜色一律走主题色或 CSS 变量 `--mud-palette-*` / `--mud-elevation-*`，禁止硬编码色值
- `MudTheme` 没有 `Breakpoints` 属性（断点像素模型是独立的 `Breakpoints` 类）

## 断点与响应式

### Breakpoint 枚举

`Xs(<600) Sm(600-960) Md(960-1280) Lg(1280-1920) Xl(1920-2560) Xxl(≥2560)` + 别名 `SmAndDown / MdAndUp / LgAndUp ...` + 哨兵 `None / Always`。

### MudHidden

```razor
<MudHidden Breakpoint="Breakpoint.Lg" Invert="true">
    <MudText>仅在小屏（XS/SM/MD）渲染</MudText>
</MudHidden>
```

语义：`Breakpoint="Breakpoint.Lg"` = 达到 LG 及以上**不渲染**（不是 display:none）；`Invert="true"` 反转为显示。纯 CSS 隐藏用响应式 display 类（`d-none d-lg-block`）。参数：`Breakpoint`、`Invert`(false)、`Hidden`(true，`@bind-Hidden`)、`ChildContent`。

### 服务直订（图表联动等）

实现 `IBrowserViewportObserver`，`OnAfterRenderAsync` 中 `await BrowserViewportService.SubscribeAsync(this, fireImmediately: true)`，回调读 `BrowserViewportEventArgs.BrowserWindowSize`；常用查询方法 `GetCurrentBreakpointAsync()` / `IsBreakpointWithinWindowSizeAsync(Breakpoint)`。`<MudBreakpointProvider>` 可向子级级联当前 `Breakpoint`（子组件不订阅 JS，纯服务端判断）。

## 栅格与排列

### MudGrid / MudItem（12 列栅格）

```razor
<MudGrid Spacing="4">
    <MudItem xs="12" sm="6"><MudPaper Class="pa-4">左</MudPaper></MudItem>
    <MudItem xs="12" sm="6"><MudPaper Class="pa-4">右</MudPaper></MudItem>
</MudGrid>
```

- `MudItem` 断点参数**全小写** `xs/sm/md/lg/xl/xxl`；默认 0 = 不生成类（不写 sm 时小屏以下继承 xs 值）
- `MudGrid.Spacing` 默认 6（4px 步进）

### MudStack（flex）

```razor
<MudStack Row="true" Justify="Justify.SpaceBetween" AlignItems="AlignItems.Center" Spacing="3">
    <MudText>标题</MudText>
    <MudButton>操作</MudButton>
</MudStack>
```

| 参数 | 默认值 | 说明 |
| --- | --- | --- |
| `Row` | false | true=水平 |
| `Spacing` | 3 | gap，4px 步进；负值=无 gap |
| `Justify` / `AlignItems` / `Wrap` | null（可空枚举） | null=不输出类 |
| `Breakpoint` | None | 该断点处 Row/列互换（纯 CSS 实现） |

`<MudSpacer />` = `flex-grow-1` 占位（AppBar 内推右侧常用）。

### 其他

- `MudContainer`：`MaxWidth`(Large)、`Fixed`(false，按断点锁 max-width)、`Gutters`(true)
- `MudPaper`：`Elevation`(1)、`Outlined`(false)、`Square`(false)、`Width/Height` 字符串
- `MudCard`：`Elevation`(1)、`Outlined`、`Square`、`ContentPadding`(true)；子件 MudCardHeader/MudCardContent/MudCardActions/MudCardMedia
- `MudDivider`：`Vertical`(false)、`FlexItem`(false，flex 容器内竖线必须开)、`Light`

## Tabs / Stepper / ExpansionPanel

### MudTabs

```razor
<MudTabs Elevation="1" Rounded="true" @ref="_tabs">
    <MudTabPanel Text="一" ID="pn_one">内容一</MudTabPanel>
    <MudTabPanel Text="二">内容二</MudTabPanel>
</MudTabs>
<MudButton OnClick="() => _tabs.ActivatePanelAsync(0)">切到第一个</MudButton>
```

- 激活 API 是 **`ActivatePanelAsync`**（重载：int 索引 / object id / MudTabPanel），不存在 `ActivatePanel`
- `@bind-ActivePanelIndex` 双向绑定当前页
- `KeepPanelsAlive`(false)：false 时切走即销毁面板状态；`LazyLoadPanels` 仅在 KeepPanelsAlive=true 时生效
- `MudTabPanel`：`Text`、`Icon`、`Disabled`、`Visible`、`BadgeData/BadgeDot/BadgeColor`、`ToolTip`

### MudStepper

```razor
<MudStepper NonLinear="true" ShowResetButton="true" @bind-ActiveIndex="_index">
    <MudStep Title="基本信息">...</MudStep>
    <MudStep Title="明细" SecondaryText="可选" Skippable="true">...</MudStep>
</MudStepper>
```

- `ActiveIndex` 初始 **-1**，`@bind-ActiveIndex` + `ActiveIndexChanged`
- 步进控制：`PreviousStepAsync()` / `NextStepAsync()` / `ResetAsync()`；单步内命令 `SetCompletedAsync()` / `SetHasErrorAsync()` / `SetDisabledAsync()`
- 拦截切换：`OnPreviewInteraction`（`Func<StepperInteractionEventArgs,Task>`）里 `arg.Cancel = true`；`arg.Action` 为 `StepAction.Complete`（点 Next）/ `StepAction.Activate`（点标题）
- 非 `NonLinear` 时未完成当前步不能跳步；步内容默认仅活动步渲染

### MudExpansionPanels

```razor
<MudExpansionPanels MultiExpansion="false">
    <MudExpansionPanel Text="面板一" Expanded="true">内容</MudExpansionPanel>
    <MudExpansionPanel Text="面板二">内容</MudExpansionPanel>
</MudExpansionPanels>
```

- 组默认 `Outlined=true`、`Gutters=true`；一次展开一个，多开用 `MultiExpansion`
- 单面板：`@bind-Expanded`、`ToggleExpansionAsync()` / `ExpandAsync()` / `CollapseAsync()`、`KeepContentAlive`(true)、`MaxHeight`(像素)

## 样式覆盖落点

覆盖 Mud 组件样式时的 DOM 事实与修法（规则层判据见生成规则「CSS 隔离」节；此处是操作细节）：

1. **`MudSelect` 的 `Class` 落在内部 `MudInputControl`**（本地源码 `MudSelect.razor:14`，`Classname` 由 `.AddClass(Class)` 构成于 `MudSelect.razor.cs:84-86`），根 `.mud-select`（`:6`）拿不到该 class；flex 对齐须对根级直选（如 `.容器 > .mud-select`）。
2. **Mud 组件根元素不携带调用方的 CSS 隔离 scope 属性**：调用方 `.razor.css` 里挂在 Mud 组件 `Class` 上的规则永不匹配——堆 `!important` 也无效。修法三选一：自有祖先元素 `::deep` 穿透 / 挪全局样式 / 用 `mt-2` 等 utility 类。
3. **`MudTheme` 各 Typography 成员的 `FontFamily` 默认独立为 Roboto、不继承 Default**，整体换字体须逐成员覆盖；Typography 没有 Input 成员，输入控件回落 Default。
4. **同表单混排间距与标签形态**：`MudSelect` 根默认上下约 4px margin、`MudTextField` 根为 0，混排时间距交替；带 `Placeholder` 的字段强制浮动标签，同表单字段须统一 `ShrinkLabel="true"`（或都不用 Placeholder）。
