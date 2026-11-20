---
rule_id: ui-agents
version: 1.0.0
last_updated: 2026-11-20
dependencies: [agents-root]
---

# 界面规则

<!-- include: shared/ui/scope.md -->

---

## 技术栈

- .NET + Windows Forms + AntdUI + CommunityToolkit.Mvvm；版本以仓库现有 `TargetFramework` 为准，不主动升级，新项目先联网检索当前最新正式版
- 分层架构：View（窗体 / 自定义控件）→ ViewModel（视图模型层）→ Service（应用服务层）→ Repository（仓储层）→ Model（领域模型层）
- 消息传递：CommunityToolkit.Mvvm IMessenger
- 依赖注入：Microsoft.Extensions.DependencyInjection
- 目标框架：WinForms 仅支持 Windows，`TargetFramework` 使用 `net<TFM>-windows` 并启用 `UseWindowsForms`

### 核心库
- `Azrng.Core`：基础类库（实体基类、扩展方法、工具类、结果包装、异常体系、DI 标记接口），版本以 NuGet 最新正式版为准，不在规则中固定版本号
- `Azrng.Core.Json`：JSON 序列化（基于 System.Text.Json），版本以 NuGet 最新正式版为准

### UI 组件库选型
- 默认组件库：`AntdUI`（Ant Design 风格，纯 GDI 矢量绘制、支持 DPI 适配与 AOT，Apache-2.0），具体控件清单以所引包版本文档为准
- 复杂企业级界面（Ribbon / Docking / VS 风格停靠布局）：`Krypton.StandardToolkit`（Krypton-Suite 社区维护版，BSD-3），不与其他主题库混用
- 托盘常驻、对话框为主或零界面项目：优先官方原生控件与系统主题，不引入第三方组件库
- 许可证红线：引入任何 UI 组件库前必须核对 LICENSE——`SunnyUI` 为 GPL-3.0 且商业使用（含企业内部）需作者授权，`MaterialSkin` 已停更，默认不引入；确需使用时先向用户说明许可证风险并经确认
- 深色与系统主题：.NET 9+ 使用官方 `Application.SetColorMode` / `Application.SetThemeMode`（.NET 10 起为正式 API）；组件库自带主题与官方色模式必须统一由一个主题入口切换，禁止两套主题体系并存

### 依赖注入规范
<!-- include: shared/infrastructure/di-azrng-desktop.md -->
- 应用入口为 Program.cs（WinForms 无 App.xaml 启动体系）

### 部署
- 桌面应用打包：单文件发布
- 数据库（可选）：SQLite (本地) / PostgreSQL / SQL Server / 其他

### 设计系统
<!-- include: shared/ui/design-system-liveness.md -->
- 遵循 `design-system.yaml` 中定义的设计 token
- 颜色、字体、间距、圆角必须来自语义化样式资源，禁止散落在控件属性中硬编码
- 使用 AntdUI 时通过 `Style.TextColor`、`Style.BorderColor`、`Style.Primary` 等样式类与自定义 `Theme` 统一管理色值，禁止在控件属性中直接写 Color 字面量

<!-- include: shared/ui/spec-lock.md -->

<!-- include: shared/application/tech-choice.md -->

---

<!-- include: shared/ui/advisory.md -->
- 设计体系基线：WinForms 样式资源与 AntdUI 主题约束
## 推荐目录结构
<!-- include: shared/common/structure-preamble.md -->

```text
project-root/
├── src/
│   ├── AppName/
│   │   ├── Views/                    # 视图层
│   │   │   ├── Controls/             # 自定义控件（UserControl）
│   │   │   ├── Dialogs/              # 对话框窗体
│   │   │   └── Forms/                # 窗口窗体（按业务域拆分）
│   │   ├── ViewModels/               # 视图模型层
│   │   │   ├── Base/                 # ViewModel 基类
│   │   │   ├── Dialogs/              # 对话框 ViewModel
│   │   │   └── Pages/                # 窗体 ViewModel（按业务域拆分）
│   │   ├── Services/                 # 应用服务层
│   │   │   ├── Interfaces/           # 服务接口定义
│   │   │   └── Implementations/      # 服务实现
│   │   ├── Repositories/             # 仓储层（可选）
│   │   │   ├── Interfaces/           # 仓储接口定义
│   │   │   └── Implementations/      # 仓储实现
│   │   ├── Models/                   # 领域模型层
│   │   │   ├── Entities/             # 数据实体
│   │   │   └── DTOs/                 # 数据传输对象
│   │   ├── Helpers/                  # 工具类、扩展方法
│   │   ├── Assets/                   # 资源文件
│   │   │   ├── Icons/                # 图标资源
│   │   │   └── Images/               # 图片资源
│   │   ├── Data/                     # 数据访问（可选）
│   │   │   ├── Context/              # 数据库上下文
│   │   │   └── Migrations/           # 数据库迁移脚本
│   │   ├── Program.cs                # 应用入口（主题、DI、消息循环）
│   │   └── AssemblyInfo.cs           # 程序集信息
│   └── AppName.sln
├── doc/
│   ├── design/
│   └── requirement.md
├── .gitignore
├── AGENTS.md
├── CLAUDE.md
├── GEMINI.md
├── GROK.md
├── design-system.yaml
└── TASK.md
```

---

### 阶段 1 — 视图实现（视图实现角色主导）

**触发条件**：用户发出「开始视图开发」指令

**入场要求**：阶段 0 设计文档已由用户确认；本次改动命中原型跳过条件时直接实现，否则沉淀型真栈原型在阶段 1 内实现，其用户确认是阶段 1 的首个门控（确认时点见「原型设计与确认」）

WinForms 项目默认沉淀型真栈原型：窗体直接在正式工程内实现，数据经接口边界以模拟实现注入（见「mock 边界与数据注入」），运行项目预览；视觉方向未收敛、需多方案发散比对时可先做正式栈对比稿发散，需专项验证复杂交互（自绘控件、动画或原生行为）时可另建最小验证工程。

<!-- include: shared/ui/prototype-workflow.md -->

**工作内容**：
1. 按设计文档实现窗体和控件，遵循 `design-system.yaml` 和 WinForms 样式规范
2. 数据层按「mock 边界与数据注入」执行：接口定义 + Fake 实现，不依赖真实服务
3. 同步输出接口契约文件 `src/AppName/Models/DTOs/`，定义所有数据传输对象

**产物**：
- 可运行的 WinForms 窗体页面
- `src/AppName/Models/DTOs/` 接口契约类

**门控规则**：
- 用户确认窗体页面符合设计文档预期
- DTO 类中的类型已定稳，不再变动
- 视觉验收按「视觉验收」小节执行，用户终审通过前不得进入阶段 2
- 满足以上三点后，才允许进入阶段 2

---

<!-- include: shared/ui/mock-boundary.md -->

<!-- include: shared/ui/visual-acceptance.md -->

## WinForms 视图层规则

### 无窗体 / 托盘项目适用口径
- 本节「窗体规则」仅当项目存在窗体（`Form` 派生类或可视化控件布局）时适用；托盘常驻、无窗体项目跳过窗体样式、导航与对话框规则，但「托盘与消息循环规则」全量适用
- 项目是否存在窗体以当前代码为准（检索 `: Form`、`*.Designer.cs`、`Controls.Add` 等信号判定），不依赖规则生成时的记录；新增首个窗体时本节自动生效

### 窗体规则
- 主窗体与对话框窗体职责单一，一个窗体一类业务；复杂页面拆分为 UserControl 组合，禁止巨型窗体类
- 布局优先使用 `TableLayoutPanel` / `FlowLayoutPanel` 与锚定 / 停靠，禁止以绝对坐标 + 手工缩放适配多 DPI
- 高 DPI 口径：`ApplicationConfiguration.Initialize()` 生成 `app.manifest` / runtimeconfig 的 DPI 感知配置（`ApplicationHighDpiMode`），禁止只调 `AutoScaleFactor` 兼容旧布局
- 窗体圆角、无边框与阴影通过 AntdUI 无边框窗体能力或 Win32（DWM）实现，禁止自造第二套窗口特效体系

### 控件规则
- 优先复用 `src/AppName/Views/Controls/` 下已有 UserControl，禁止重复创建
- 只有确实有复用价值时才新增共享控件，避免为单次需求过度抽象
- 页面状态必须完整：`loading`、`empty`、`error`、`no-permission`
- 优先使用 AntdUI 控件（AntdUI.Button、AntdUI.Input、AntdUI.Table 等），必要时使用 WinForms 原生控件；同一窗体内同一类控件禁止两套库混排
- 控件尺寸与间距来自设计 token 对应常量，禁止在 `InitializeComponent` 之外散落魔法数值

### MVVM 模式规则
- **ViewModel 基类**：所有 ViewModel 最终都必须继承 `ObservableObject`（CommunityToolkit.Mvvm）；若项目已有 `BaseViewModel`，应由基类继承 `ObservableObject` 后统一复用
- **属性通知**：使用 `[ObservableProperty]` 特性自动生成 INotifyPropertyChanged
- **命令定义**：操作入口封装为 ViewModel 方法（`[RelayCommand]`），控件事件处理器只做转调，禁止在事件处理器内写业务逻辑
- **绑定方式**：WinForms 无 XAML 绑定体系，优先使用 `Binding` 对象或封装统一的绑定扩展；批量绑定收敛在窗体基类或扩展方法中，禁止逐控件手写订阅散落各处
- **消息传递**：ViewModel 间通信使用 `IMessenger` (CommunityToolkit.Mvvm)
- **依赖注入**：ViewModel 依赖通过构造函数注入，窗体通过 DI 容器解析（`Application.Run(container.Resolve<MainForm>())`）
- **服务注册**：服务类实现 `ITransientDependency`/`IScopedDependency`/`ISingletonDependency` 接口表达生命周期，在 Program.cs 中显式 `AddTransient`/`AddScoped`/`AddSingleton` 注册（`RegisterBusinessServices` 属 `Azrng.AspNetCore.Core`，桌面项目不使用）

### 线程与消息循环规则
- UI 控件只能由创建线程（UI 线程）访问：后台任务回调更新界面必须经 `IProgress<T>`、`SynchronizationContext.Post` 或 `Control.BeginInvoke` 编组回 UI 线程，禁止在 `Task` 延续中直接改控件
- `async void` 只允许出现在事件处理器；异步链路线程边界必须显式，异常必须有顶层兜底（`Application.ThreadException` / `TaskScheduler.UnobservedTaskException`）
- 耗时操作禁止阻塞消息循环；超过 300ms 的操作提供进度反馈与取消入口（CancellationToken）

### 托盘与消息循环规则
- `NotifyIcon` 必须与消息循环同线程：托盘图标在 STA 线程创建并 `Application.Run(ApplicationContext)` 驱动；托盘跑独立线程时必须通过该线程的上下文投递操作，禁止跨线程直接操作托盘图标与右键菜单
- 托盘右键菜单（`ContextMenuStrip`）项的启用/禁用状态与内部状态同步，退出动作必须统一走 `ApplicationContext.ExitThread` 并释放 `NotifyIcon`（防图标残留）
- 单实例常驻进程必须用 `Mutex` 或等价机制防多开，二次启动时激活已有实例而不是新进程
- 开机自启、后台静默运行与退出确认策略必须在需求文档中显式定义，不默认

### 导航规则
- 主窗体多模块切换优先侧栏 + 内容容器模式，内容区用 UserControl 替换，禁止窗体间互相 `new` 后 `Show` 造成生命周期失控
- 导航逻辑封装在 NavigationService 中，禁止在 ViewModel 中直接操作 View
- 页面参数通过导航参数对象或 IMessenger 传递，禁止使用静态全局状态
- 复杂 IDE 类停靠布局走 Krypton.Docking / DockPanel Suite，不自研停靠框架

### 对话框规则
- 对话框使用 AntdUI 模态能力（`AntdUI.Modal` / `Dialog.show`）或原生 `Form.ShowDialog` / `TaskDialog`；确认框优先官方 `TaskDialog`（现代样式、支持色模式）
- 对话框必须使用独立 ViewModel，禁止 Code-behind 写业务逻辑
- WinForms 的 `ShowDialog` 是阻塞调用：需要异步等待结果时使用事件 / TaskCompletionSource 封装，禁止在 UI 线程同步等待异步业务（`.Result` / `.Wait()` 死锁风险）
- 危险操作对话框必须突出后果与确认动作

### 数据绑定与状态管理规则
- 列表数据使用 `BindingList<T>` 或 `ObservableCollection<T>`（配合绑定适配），复杂集合变更使用批量刷新方式，避免逐项刷新导致界面闪烁
- 异步数据加载必须支持取消（CancellationToken）
- 禁止在 ViewModel 中直接持有并操作控件字段
- **本地状态**：使用 ObservableProperty 特性管理 ViewModel 状态
- **全局共享状态**：使用 IMessenger 传递跨 ViewModel 消息
- 禁止使用静态全局类（如 Singleton）存储业务状态

---

## 测试规则

<!-- include: shared/ui/pre-commit-regression.md -->
<!-- include: shared/frontend/general-requirements.md -->

<!-- include: shared/application/view-layer-testing.md -->

<!-- include: shared/application/application-layer-testing.md -->
<!-- include: shared/desktop/repository-regression.md -->

<!-- include: shared/external-dependencies.md -->

<!-- include: shared/no-test-fallback.md -->

---
