# 其它高频组件速查

本文覆盖未单独成章但常用的组件。**仅列出经源码核实的要点**；参数细节未尽的，去 `Components/<组件名>/<组件名>.razor(.cs)` 查 `[Parameter]`，切勿臆造。

## Upload（`Components/Upload/`）

文件上传组件 `<Upload>`。常见子项/参数方向（**用前查源码核实当前版本签名**）：`IsMultiple`、`ShowUploadList`、`Accept`、`Max`，以及 `OnChange`/`OnDelete`/`OnDownload` 等回调与 `IUploadFile` 项。绑定用 `Value`（`@bind-Value`）或 `Files`。

> 源码：`Components/Upload/Upload.razor.cs`、`UploadBase.cs`、`IUploadFile.cs`。rbac-blazor 未深度使用 Upload，参数以源码为准。

## TreeView / Tree（`Components/TreeView/`、`Components/Tree/`）

- `<TreeView>`：树形展示，数据项 `TreeItem`（`Text`/`Icon`/`IsActive`/`IsCollapsed`/子项 `Items`），`Items`、`OnClickItem`、模板。
- `<Tree>`：较新的树组件（10.x 引入），API 与 TreeView 类似但更现代。
- **用前查源码**：`Components/TreeView/TreeView.razor.cs`、`Components/Tree/Tree.razor.cs`，核对 `TreeItem<TItem>` 字段名（如子项是 `Items` 还是 `Children`）。

## 表单辅助组件

- `<Validate>`（`Components/Validate/`）：自定义校验容器，`ValidateRules: List<IValidator>?`。通常配合 `ValidateForm` 用，不单独手写校验。
- `<Display>`（`Components/Display/`）：只读展示字段值，`Value` + `FormatString`/`Formatter`。
- `<Label>`（`Components/Label/`）：字段标签，`Text`/`DisplayText`/`ShowRequiredMark`，通常由表单组件的 `ShowLabel` 自动渲染。

## 列表 / 数据展示

- `<Repeater>`（`Components/Repeater/`）：重复渲染模板，`Items` + `ChildContent`（`@context`）。
- `<ListView>`（`Components/ListView/`）：卡片式列表，`Items` + `ItemTemplate`。
- `<Timeline>`（`Components/Timeline/`）：时间线，`Items`（`TimelineItem`）。
- `<Carousel>`（`Components/Carousel/`）：轮播，`Items`（`CarouselItem`）/`Images`。
- `<Empty>`（`Components/Empty/`）：空数据占位，`Text`/`Image`/`Template`。Table 自带 `EmptyTemplate`。
- `<Progress>` / `<Circle>`（`Components/Progress/`、`Components/Circle/`）：进度条/环形进度，`Value`。

## 容器 / 反馈辅助

- `<GroupBox>`（`Components/GroupBox/`）：分组框容器，`Title` + `ChildContent`（rbac 搜索区常用）。
- `<Block>`（`Components/Block/`）：条件块，`Condition` + `ChildContent`。
- `<Stack>`（`Components/Stack/`）：栈布局，`IsVertical`/`AlignItems`/`JustifyContent`。
- `<Split>`（`Components/Split/`）：分栏，`<SplitPane>` 子项。
- `<Affix>`（`Components/Affix/`）：固钉。
- `<ErrorLogger>`（`Components/ErrorLogger/`）：全局异常捕获，包裹 `@Body`（rbac Layout 用）。
- `<Reconnector>` / `<ConnectionHub>`：断线重连提示。

## 工具组件

- `<CountUp>`：数字动画，`Value`/`Count`。
- `<Marquee>`：跑马灯。
- `<Watermark>`：水印，`Text`。
- `<Timer>`：定时器组件。
- `<Logout>`（`Components/Logout/`）：登出下拉，`ImageUrl`/`DisplayName`/`UserName`/`LogoutUrl` + `<LogoutLink>`（rbac 用法）。
- `<ThemeProvider>` / `<Light>`：主题切换。

## 完整组件清单（133 目录，按类别）

> 取自 `src/BootstrapBlazor/Components/` 真实目录。组件名即目录名（标签名一般同组件类名；`BootstrapInput*`/`BootstrapInputGroup*` 带 `Bootstrap` 前缀）。

**表单**：Affix(定位)、AutoComplete、AutoFill、Captcha、Cascader、Checkbox、CheckboxList、ClockPicker、ColorPicker、DateTimePicker、DateTimeRange、EditorForm、Filter/Filters、Handwritten、Input(BootstrapInput)、InputNumber(BootstrapInputNumber)、IpAddress、Mask、MultiSelect、QueryBuilder、Radio(RadioList)、Rate、Search、SearchForm、Searches、Select、SelectGeneric、Segmented、Slider、Switch、Textarea、TimePicker、Transfer、Typed、Upload、Validate、ValidateForm。

**反馈/弹窗**：Alert、ConnectionHub、Console、ContextMenu、Dialog、Drawer、Empty、ErrorLogger、FullScreen、Light、Logout、Marquee、Message、Modal、Notification、Popover、Print、Progress、Reconnector、Spinner、SweetAlert、Toast、Tooltip、Watermark。

**布局**：Affix、Block、BootstrapBlazorRoot、BootstrapBlazorRootOutlet、Card、Collapse、Divider、Footer、GroupBox、Layout、LazyLoad、Navbar、Nav、Row、Scroll、Skeleton、Split、Stack、Step、Tab、Title。

**导航**：Anchor、AnchorLink、AutoRedirect、Breadcrumb、Dropdown、DropdownWidget、GoTop、Menu、Pagination、Redirect、RibbonTab。

**数据展示**：Avatar、Badge、Carousel、Circle、CountUp、Display、FileIcon、FlipClock、IPLocator、ImageViewer、ImagePreviewer、Label、ListView、ListGroup、Marquee、Repeater、ShieldBadge、Tag、Timeline、Tree、TreeView、Waterfall。

**工具/服务**：Ajax、Camera、Clipboard、Download、EyeDropper、FullScreen、Geolocation、HtmlTag、IFrame、IntersectionObserver、NetworkMonitor、Responsive、SortableList、Speech、Theme、ThemeProvider、Timer、Toolbar、WebSpeech。

> 目录名严格按源码（含 `DragDrap` 等源码原样拼写）。部分是内部基础件（`BaseComponents`、`BootstrapBlazorRootOutlet`、`Display`、`Label`、`HtmlTag`、`FileIcon`），不直接用于业务页面。
