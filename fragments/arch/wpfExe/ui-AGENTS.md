---
rule_id: ui-agents
version: 1.13.0
last_updated: 2026-08-27
dependencies: [agents-root]
---

# 界面规则

## 适用范围

- 作用域：界面、布局、导航、样式、状态、交互与界面测试
- 触发场景：涉及页面、窗口、布局、组件、交互、样式、UI smoke test 时阅读

### 常见任务入口
- 新增页面、窗口或布局：先看界面结构与组件规则
- 改表单、对话框、导航、交互：先看状态管理与事件处理规则
- 改样式、主题、视觉展示：先看样式规则与设计系统约束
- 补界面回归：先看 `提交前最小回归` 与测试规则

---

## 技术栈

- .NET 10 (2025 年发布正式版) + WPF + Wpf.Ui (WPF UI) + CommunityToolkit.Mvvm
- 分层架构：View (视图层) → ViewModel (视图模型层) → Service (应用服务层) → Repository (仓储层) → Model (领域模型层)
- 数据访问（可选）：Dapper + SQLite / PostgreSQL / 其他数据库
- 消息传递：CommunityToolkit.Mvvm IMessenger
- 依赖注入：Microsoft.Extensions.DependencyInjection

### 核心库
- `Azrng.Core` (1.15.8)：基础类库（实体基类、扩展方法、工具类、结果包装、异常体系）
- `Azrng.Core.Json` (1.3.1)：JSON 序列化（基于 System.Text.Json）

### WPF UI (Wpf.Ui)
- NuGet 包：`Wpf.Ui`（最新稳定版）
- 提供 Fluent Design 风格控件：NavigationView、FluentWindow、Dialog、Snackbar、Card 等
- 内置浅色/深色主题切换支持
- 控件前缀命名空间：`xmlns:ui="http://schemas.lepo.co/wpfui/2024/xaml"`

### 依赖注入规范
- 使用 Azrng 标记接口方式批量注册服务：
  - `ITransientDependency`：临时注入
  - `IScopedDependency`：作用域注入
  - `ISingletonDependency`：单例注入
- 在 App.xaml.cs 中调用 `services.RegisterBusinessServices(assemblies)` 扫描并注册服务
- 服务类实现对应标记接口，自动被扫描注册到 DI 容器

### 部署
- 桌面应用打包：单文件发布
- 数据库（可选）：SQLite (本地) / PostgreSQL / SQL Server / 其他

### 设计系统
- 使用 Fluent Design 风格，遵循 `design-system.yaml` 中定义的设计 token
- 样式必须使用 WPF 资源字典 (ResourceDictionary)，禁止硬编码在控件属性中
- 颜色、间距、圆角必须来自样式资源，使用 `{StaticResource}` 或 `{DynamicResource}` 引用
- Wpf.Ui 内置 Fluent 主题，在此基础上扩展自定义样式

如果仓库已经有真实实现，以现有代码为准，不要强行重构或替换技术栈。
技术债务与重构判断遵循根 `AGENTS.md` 的全局规则。

---

## 主动建议规则
- 发现窗口结构、页面布局、控件拆分、状态展示、样式资源、可访问性或交互一致性问题时，应主动提醒，并说明是否影响当前任务
- 发现设计稿、截图反馈或用户描述与 `design-system.yaml`、WPF 样式资源或 Fluent Design 约束冲突时，应先说明冲突点，再给出最小调整方案
- 涉及共享控件、全局样式、导航方案或跨 ViewModel 状态传递的建议改动，未经用户确认不得扩大到本次任务之外
- 复用、查证优先级等共性规则见根 `AGENTS.md`「模型行为约束与主动建议」

## 推荐目录结构
若仓库尚未形成稳定结构，可优先参考以下组织方式；若仓库已有实现，以现状为准，不强制迁移。

```text
project-root/
├── src/
│   ├── AppName/
│   │   ├── Views/                    # 视图层
│   │   │   ├── Controls/             # 自定义控件
│   │   │   ├── Converters/           # 值转换器
│   │   │   ├── Dialogs/              # 对话框视图
│   │   │   ├── Pages/                # 页面视图（按业务域拆分）
│   │   │   └── Windows/              # 窗口视图
│   │   ├── ViewModels/               # 视图模型层
│   │   │   ├── Base/                 # ViewModel 基类
│   │   │   ├── Dialogs/              # 对话框 ViewModel
│   │   │   └── Pages/                # 页面 ViewModel（按业务域拆分）
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
│   │   │   ├── Styles/               # WPF 资源字典
│   │   │   │   ├── Theme.xaml        # Wpf.Ui 主题配置
│   │   │   │   ├── Colors.xaml       # 颜色资源
│   │   │   │   ├── Fonts.xaml        # 字体资源
│   │   │   │   └── Global.xaml       # 全局样式
│   │   │   ├── Icons/                # 图标资源
│   │   │   └── Images/               # 图片资源
│   │   ├── Data/                     # 数据访问（可选）
│   │   │   ├── Context/              # 数据库上下文
│   │   │   └── Migrations/           # 数据库迁移脚本
│   │   ├── App.xaml                  # 应用级资源和启动配置
│   │   ├── App.xaml.cs               # 应用入口代码
│   │   └── AssemblyInfo.cs           # 程序集信息
│   └── AppName.sln
├── doc/
│   ├── design/
│   └── requirement.md
├── .gitignore
├── AGENTS.md
├── CLAUDE.md
├── GEMINI.md
├── design-system.yaml
└── TASK.md
```

---

### 阶段 1 — 视图实现（视图实现角色主导）

**触发条件**：用户发出「开始视图开发」指令

**入场要求**：阶段 0 设计文档已由用户确认

**工作内容**：
1. 按设计文档实现页面和组件，遵循 `design-system.yaml` 和 WPF 样式规范
2. 数据层使用 mock（静态 mock 数据），不依赖真实服务
3. 同步输出接口契约文件 `src/AppName/Models/DTOs/`，定义所有数据传输对象

**产物**：
- 可运行的 WPF 视图页面
- `src/AppName/Models/DTOs/` 接口契约类

**门控规则**：
- 用户确认视图页面符合设计文档预期
- DTO 类中的类型已定稳，不再变动
- 满足以上两点后，才允许进入阶段 2

---

## WPF 视图层规则

### 样式规则
- 所有样式使用 WPF 资源字典 (`.xaml` 文件)，禁止在控件中直接设置 `Background`、`Margin` 等属性
- 所有颜色来自 `design-system.yaml` 中定义的语义化 token，通过资源字典引用
- 所有间距使用 Margin / Padding 工具类，禁止硬编码数值
- 响应式设计遵循窗口大小变化，必须在不同窗口尺寸下测试
- Wpf.Ui 主题通过 `App.xaml` 中的 `<ui:ThemesDictionary>` 和 `<ui:ControlsDictionary>` 引入

### 窗口规则
- 主窗口继承 `ui:FluentWindow`（Wpf.Ui 提供的现代窗口基类）
- 对话框窗口同样继承 `ui:FluentWindow`
- 窗口圆角和 Mica/Acrylic 背景效果通过 Wpf.Ui 内置支持实现

### 组件规则
- 优先复用 `src/AppName/Views/Controls/` 下已有组件，禁止重复创建
- 只有确实有复用价值时才新增共享组件，避免为单次需求过度抽象
- 页面状态必须完整：`loading`、`empty`、`error`、`no-permission`
- 优先使用 Wpf.Ui 控件（ui:Button、ui:TextBox、ui:DataGrid 等），必要时使用 WPF 原生控件

### MVVM 模式规则
- **ViewModel 基类**：所有 ViewModel 最终都必须继承 `ObservableObject`（CommunityToolkit.Mvvm）；若项目已有 `BaseViewModel`，应由基类继承 `ObservableObject` 后统一复用
- **属性通知**：使用 `[ObservableProperty]` 特性自动生成 INotifyPropertyChanged
- **命令定义**：使用 `[RelayCommand]` 特性生成 ICommand，禁止手动创建 RelayCommand
- **消息传递**：ViewModel 间通信使用 `IMessenger` (CommunityToolkit.Mvvm)
- **依赖注入**：ViewModel 依赖通过构造函数注入
- **服务注册**：服务类实现 `ITransientDependency`/`IScopedDependency`/`ISingletonDependency` 接口，通过 `RegisterBusinessServices` 批量注册

### 导航规则
- 使用 Wpf.Ui 的 `ui:NavigationView` 进行页面切换
- 页面视图继承 `ui:UiPage` 或 `Page`
- 导航逻辑封装在 NavigationService 中，禁止在 ViewModel 中直接操作 View
- 页面参数通过导航消息传递，禁止使用静态全局状态
- 支持导航历史记录（前进/后退）

### 对话框规则
- 使用 Wpf.Ui 的 Dialog 组件或 `ui:MessageBox` 实现对话框
- 对话框内容必须使用 ViewModel，禁止 Code-behind 写业务逻辑
- 对话框结果通过 TaskCompletionSource 异步返回
- 危险操作对话框必须突出后果与确认动作

### 数据绑定规则
- 列表数据使用 `ObservableCollection<T>`
- 复杂集合变更使用批量更新方式，避免逐项添加导致频繁刷新
- 异步数据加载必须支持取消（CancellationToken）
- 绑定路径必须正确，禁止使用 `x:Name` 在 ViewModel 中访问控件
- 使用 `{Binding Path=Property, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}` 确保双向绑定及时更新

### 状态管理规则
- **本地状态**：使用 ObservableProperty 特性管理 ViewModel 状态
- **全局共享状态**：使用 IMessenger 传递跨 ViewModel 消息
- 禁止使用静态全局类（如 Singleton）存储业务状态
- Application.Current.Resources 仅用于主题、语言等低频全局配置

---

## 测试规则

### 提交前最小回归
- 默认执行：项目现有的编译、静态检查或等价校验
- 页面、窗口、导航、表单交互改动：至少补一次受影响界面或组件的 smoke test / 等价验证
- 仅样式或视觉改动：至少确认受影响界面的关键状态、布局与主要交互未回退
- 若影响共享组件、布局或状态流转，优先验证影响范围最大的界面，而不是只看局部组件
### 总体要求
- 影响行为的改动应优先补充或更新测试，而不是只修改实现代码
- 若本次改动未补测试，必须在最终说明中写明原因和风险
- 测试应覆盖真实业务行为，不要只覆盖静态渲染或无意义分支

### 视图层测试
- 页面交互、表单校验、列表行为、状态展示、异常状态发生变化时，应补充对应测试
- 至少关注以下关键状态：`loading`、`empty`、`error`、`no-permission`
- 若涉及数据请求、筛选、分页、提交等用户关键路径，应验证主要交互结果

### 业务逻辑层测试
- ViewModel 命令执行、属性变更、消息发送发生变化时，应补充对应测试
- Service 层业务逻辑、数据转换、异常处理发生变化时，应补充对应测试
- Repository 层 SQL 查询、数据映射、事务处理发生变化时，应补充对应测试
- 使用 xUnit 作为测试框架

### 外部依赖与数据
- 测试中不要真实调用外部服务，统一使用 mock、stub 或测试替身
- 测试数据应尽量最小化、可读、可重复执行
- 不要让测试依赖本地人工状态或不可控外部环境

### 无法执行测试时
- 必须说明未执行的测试类型
- 必须说明未执行原因
- 必须说明潜在影响范围和风险

---
