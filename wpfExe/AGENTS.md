# AGENTS.md

## 目标
职责划分如下：
- `AGENTS.md`：唯一协作规范来源，统一定义工作规则、任务机制、交付要求
- `CLAUDE.md` / `GEMINI.md`：各 Agent 的入口说明文件，仅负责提醒先读取 `AGENTS.md`，不维护平行规则内容
- `design-system.yaml`：设计 token 与 UI 规范
- `TASK.md`：具体任务内容与任务进度

---

## 工作方法

1. 行动前先思考，写代码前先阅读现有文件并理解业务目标。
2. 输出保持简洁，但分析与落地必须完整。
3. 优先编辑现有文件，而不是无差别重写。
4. 已确认无变更风险的文件无需重复阅读，但遇到上下文缺口时必须补读。
5. 完成前尽量执行可落地的校验，无法执行时明确说明原因和影响。
6. 避免奉承性开头或冗余结尾，直接进入有价值的信息。
7. 保持解决方案简单直接，避免过度设计。
8. 用户指令始终优先于本文件内容。

---
## 注意事项
1. 你需要理解用户提出的需求的业务逻辑然后再做设计和开发
2. 你需要根据设计的功能需求设计合适的工具导航和操作入口，避免无意义的层级复杂度
3. 功能模块必须有完整可操作的功能页面
4. 所有的功能至少要确保能够完成基本的流程操作，功能的按钮必须都是可以交互的，界面的内容尽量多填充一些信息
5. 整体系统应有默认应用图标，主界面左上角应有应用图标和应用名称。
6. 系统需要有一些克制但清晰的动效，界面简洁大方，操作流程清晰易懂。
7. 如系统内所有涉及到变更、新增、删除等业务逻辑，必须能够真实生效，而不是假的交互，确保重新进入页面、重新打开窗口或重新加载数据后仍能看到生效后的内容
8. 由于此系统的功能模块较多，实现的时候先把系统的框架搭起来，然后按照一个一个子模块的方式来实现，而不是一下子实现全部功能

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

---

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
│   ├── devlog/
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

## 开发流程

### 总体阶段划分

所有新功能开发必须严格经历以下四个阶段，阶段之间有明确的门控条件，不满足条件不得进入下一阶段：

```
阶段 0          阶段 1          阶段 2          阶段 3
设计文档   →   视图实现   →   业务逻辑实现   →   集成测试
（用户确认）   （Claude Code）  （Codex）       （协作）
```

**例外情况**（AI 自动判断，无需走阶段 0）：
- Bug 修复（功能行为不变，只修正错误）
- 已有页面的样式、文案微调
- 配置、环境变量、部署脚本调整
- 单个字段的增删（不涉及新页面或新业务流程）

---

### 阶段 0 — 设计文档（新功能强制前置）

**触发条件**：用户提出新功能需求

**执行方式**：由用户与 AI 对话协作产出，用户最终确认定稿

**产物**：`doc/design/设计文档.md`（全项目一份，新功能以新章节追加，不新建文件）

**文档必须包含以下四个部分**：

| 部分     | 内容要求                                                       |
| -------- | -------------------------------------------------------------- |
| 架构设计 | 模块划分、数据流向、涉及的技术决策说明                         |
| 功能需求 | 页面列表、功能点明细、业务规则、关键状态说明                   |
| 界面原型 | 每个页面的 ASCII 线框图，含关键状态（loading / empty / error） |
| 交互说明 | 操作流程、状态流转、边界场景、错误处理方式                     |

**门控规则**：
- 用户明确确认设计文档后，才允许进入阶段 1
- AI 在此阶段只输出文档内容，不写任何实现代码

---

### 阶段 1 — 视图实现（Claude Code 主导）

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

### 阶段 2 — 业务逻辑实现（Codex 主导）

**触发条件**：用户发出「开始后端开发」指令

**入场要求**：`src/AppName/Models/DTOs/` 接口契约文件已定稳

**工作内容**：
1. 严格按照 DTO 中的类型定义实现 Service 层和 Repository 层
2. 遵循 `ViewModel → Service → Repository → Model` 完整分层
3. 涉及数据库结构变更时，同步生成迁移脚本（若使用了数据库）
4. 每个服务方法必须可通过单元测试独立验证

**字段命名约定**：
- C# 属性：PascalCase
- 数据库字段：snake_case（若使用数据库）
- 转换层：Dapper 映射通过 Column 特性或自定义映射处理

**门控规则**：
- 核心业务逻辑单元测试通过后，才允许进入阶段 3

---

### 阶段 3 — 集成测试

**触发条件**：用户发出「开始联调」指令

**入场要求**：视图页面和业务逻辑均已独立完成验证

**工作内容**：
1. ViewModel 绑定真实 Service，验证数据流
2. 处理异常映射：后端错误结构与 Dialog / Toast 展示逻辑匹配
3. 执行核心业务路径 smoke test（至少覆盖：进入主界面 → 核心操作 → 数据回显）

**完成标志**：核心路径 smoke test 通过，`doc/devlog/` 已补充本阶段记录

---

## 使用方式
每个 AI Agent 在开始修改前都必须：
1. 先阅读本文档，理解核心规则和流程
2. 确认当前处于哪个开发阶段，检查该阶段的入场条件是否满足
3. 再阅读 `design-system.yaml`
4. 再阅读相关模块和现有实现
5. 优先复用当前结构，再决定是否新增文件

进行 UI 开发时：
1. `design-system.yaml` 是颜色、间距、圆角、排版、页面状态、组件使用规则的参考依据
2. 所有样式使用 WPF 资源字典 (ResourceDictionary)，禁止在控件中直接设置属性值
3. 颜色必须来自样式资源，使用 `{StaticResource}` 或 `{DynamicResource}` 引用
4. 图标优先使用 Wpf.Ui 内置图标（`ui:SymbolIcon`），其次使用矢量图标或素材图片
5. 全局状态（工具状态、通知、页面消息等）使用 Messenger 传递
6. ViewModel 本地状态使用 ObservableProperty 特性

进行后端开发时：
1. 保持 `ViewModel → Service → Repository → Model` 分层
2. 使用 CommunityToolkit.Mvvm 的 `[RelayCommand]` 特性
3. 涉及数据库结构变更时使用迁移脚本（若使用了数据库）

进行部署改动时：
1. 优先复用现有打包配置
2. 发布版本必须可追踪
3. 清理旧版本前要保留可回滚版本

---

## 核心规则
- 先理解，再修改。
- 先复用，再新增。
- 交付必须可直接使用，不能只停留在演示层。
- 不允许新增平行配置体系。
- 不做与当前任务无关的重构。
- 不要猜测问题，要实证排查：通过日志、断点、数据等实际证据定位原因，禁止凭猜测修改代码。
- 所有改动都必须可说明、可验证。

## 思考与实现原则
- 先理解业务目标、用户角色、核心流程，再进行设计和开发。
- 优先选择符合当前需求的最优可行方案，兼顾正确性、可维护性与实现成本。
- 尽可能复用现有代码、结构与组件；只有在明确存在复用价值时才新增抽象。
- 严禁过度设计、过度封装和无意义冗余，避免为单次需求引入不必要的层次和复杂度。
- 代码实现应保持清晰、简洁、稳定，优先保证业务正确性、可读性和后续维护性。
- 遇到需求不清、规则冲突或实现复杂度明显升高时，先说明判断与权衡，再继续执行。

## 编码规则
- 所有源码、配置、文档文件统一使用 `UTF-8` 编码。
- 读取或修改含中文文件时，若出现乱码，先判断是终端显示问题还是文件编码损坏；未确认前禁止覆盖原文件。
- 禁止使用可能隐式改变编码的方式直接改写源码文件，如 shell 重定向、`Out-File`、`Set-Content`。
- Windows / PowerShell 下读取中文文件时，必须显式使用 `UTF-8`。
- 修改含中文内容后，必须重新读取一次并确认关键中文显示正常。
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
- **ViewModel 基类**：所有 ViewModel 必须继承 `ObservableObject` (CommunityToolkit.Mvvm)
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

## 业务逻辑层规则

### Service 层规则
- 服务类必须实现接口，接口定义在 `Services/Interfaces/` 目录
- 服务方法必须是异步的（返回 `Task<T>` 或 `ValueTask<T>`）
- 服务层处理所有业务逻辑，不直接访问 Repository 以外的依赖
- 服务层异常必须统一封装，抛出 Azrng 异常体系的业务异常类型
- 服务类实现 `ITransientDependency`/`IScopedDependency`/`ISingletonDependency` 接口，通过 `RegisterBusinessServices` 批量注册

### Repository 层规则（可选，仅在使用数据库时）
- 仓储类必须实现接口，接口定义在 `Repositories/Interfaces/` 目录
- 使用 Dapper 进行数据访问，SQL 语句放在 SQL 文件或常量中
- 仓储方法必须是异步的，支持取消令牌
- 涉及事务时，使用 IDbTransaction 统一管理
- 仓储类实现 `ITransientDependency`/`IScopedDependency`/`ISingletonDependency` 接口，通过 `RegisterBusinessServices` 批量注册

### 数据访问规则（可选，仅在使用数据库时）
- 数据库连接字符串从配置文件读取，禁止硬编码
- 数据库连接必须使用 `using` 语句管理生命周期
- SQL 注入防护：所有查询必须使用参数化查询

### 统一结果包装
- 服务层方法返回值统一使用 `ResultModel<T>` 包装
- 成功响应：`ResultModel<T>.Success(data)`
- 错误响应：`ResultModel<T>.Failure(message, errorCode)`
- 异常处理使用 Azrng.Core 异常体系

### 异常处理规范
- 业务异常继承 `BaseException` 或其子类：
  - `LogicBusinessException`：业务逻辑异常
  - `ParameterException`：参数校验异常
  - `NotFoundException`：资源不存在
  - `ForbiddenException`：禁止访问
  - `InternalServerException`：服务器内部错误
- 异常在 ViewModel 层捕获并转换为用户友好的提示信息

---

## 测试规则
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

## Git 与提交流程
### 分支命名
- `feat/<desc>`：新功能
- `fix/<desc>`：缺陷修复
- `refactor/<desc>`：无行为变更的重构
- `chore/<desc>`：工具、依赖、配置调整
- `docs/<desc>`：仅文档变更

### Commit 规范
- 提交信息优先采用 Conventional Commits：
  - `<type>(<scope>): <简短描述>`
- 示例：
  - `feat(auth): add refresh token endpoint`
  - `fix(viewmodel): correct table pagination reset`
  - `chore(dotnet): pin NuGet package version`

### 交付前自查
- 编译已执行，或已明确说明未执行原因
- 代码分析已执行，或已明确说明未执行原因
- 相关测试已执行，或已明确说明未执行原因
- 涉及数据库结构变更时已确认迁移脚本（若使用了数据库）
- 新增配置文件已同步更新相关文档

### 提交触发规则
- 每次修改完后都必须提交代码
- 若仓库尚未初始化为可用 Git 工作区，最终说明中必须明确写明无法执行提交校验
- 若本次开发生成了本地运行产物、缓存、测试数据库、构建产物或依赖目录，应同步检查并更新 `.gitignore`

---

## 发布规则
- 优先复用现有打包配置
- 生产发布优先采用单文件发布 (Single File Publish)
- 不要把密钥写入配置文件
- 发布链路要可追踪
- 清理旧版本时要保留最小回滚窗口

### 发布交付规则
- 只要本次任务涉及发布配置、版本号和启动命令，完成实现后应主动执行一次构建验证
- 若因环境限制无法执行，必须明确说明
- 发布验证至少包括：
  - 构建是否成功
  - 应用是否成功启动
  - 关键功能是否处于可用状态
  - 核心访问链路是否可用

---

## 任务管理机制
`TASK.md` 是唯一任务记录文件，默认只维护任务总表中的最小任务记录，不负责承载规则解释。

### 任务状态
- `TODO`：未开始
- `DOING`：进行中
- `BLOCKED`：被阻塞
- `REVIEW`：已完成开发，等待确认
- `DONE`：已完成

### 执行规则
1. 开始工作前，先查看 `TASK.md` 是否已有对应任务。
2. 如果已有任务，优先更新原任务，不重复新建。
3. 如果没有任务，在 `TASK.md` 中新增最小任务记录。
4. 开始执行时，将任务状态更新为 `DOING`。
5. 若任务无法继续，更新为 `BLOCKED` 并写明原因。
6. 开发完成但仍需确认时，更新为 `REVIEW`。
7. 验证完成并确认交付后，更新为 `DONE`。

### 每个任务至少包含
- 任务 ID
- 任务名称
- 任务目标
- 当前阶段
- 负责人 AI
- 任务状态
- 优先级
- 最近更新时间

### AI Agent 要求
- 开始任务前先同步任务状态。
- 开发过程中及时更新 `TASK.md`。
- 范围变化时同步补充任务说明、验收标准和风险。
- 每次开发完成后，都需要在 `doc/devlog/` 下新增一份本次开发简短说明的 Markdown 记录。
---

## 设计系统文件规范
`design-system.yaml` 是唯一设计系统文件，包含：
- 品牌与产品风格意图
- 语义化颜色 token
- 排版 token
- 间距、圆角、阴影 token
- 布局与断点规则
- 组件使用规则
- 页面状态规范
- 明确的 `do / dont` 规则

---

## 文档更新要求
以下内容发生变化时，必须同步更新相关文档：
- 命令、脚本或启动方式
- 环境变量
- 配置项
- 构建、发布或部署流程
- 数据库结构、迁移或初始化方式
- 对外使用方式或接口约定

优先更新已有文档，不新增重复说明。
每次开发完成后，必须在 `doc/devlog/` 下补充一份简短开发记录。

### 文档目录约定
- `doc/requirement.md`：需求文档。若文件已存在，AI 应基于用户新增需求进行补充或修订；若文件不存在，AI 必须根据用户输入先自动生成详细初稿。该文件仅承载业务需求，不写入设计说明或开发过程记录
- `doc/design/设计文档.md`：项目设计文档，全项目一份。包含架构设计、功能需求、界面原型（如适用）与交互说明。由用户与 AI 协作产出，用户确认后作为开发的唯一需求基准。AI 可在文件中追加新功能章节，不删除已有内容，不新建平行文件
- `doc/devlog/`：每次开发完成后的简短开发日志

### requirement 要求
- 若 `doc/requirement.md` 为模板初始状态，AI 应根据用户输入将占位内容替换为项目真实需求，而不是长期保留“待补充”空壳。
- 需求文档建议固定包含以下章节：项目背景、目标与范围、用户角色、页面或模块清单、核心业务流程、关键数据与字段、权限与可见范围、边界场景与异常、非功能要求、待确认项。
- 自动生成需求文档时，优先写清业务事实、流程与边界，不提前写入技术实现方案。

### devlog 要求
- 文件目录：`doc/devlog/`
- 文件格式：Markdown
- 文件命名建议：`YYYY-MM-DD-HH-mm-ss-中文任务简述.md`
- 文件名中的任务简述必须使用中文，不使用英文 slug、拼音缩写或中英混杂描述
- 内容保持简短，至少包含：
  - 本次目标
  - 核心改动
  - 修改文件
  - 校验情况
  - 风险或遗留项
---

## 交付收口规范
- 当用户请求“开始开发”而非“只出方案”时，AI Agent 在实现完成后应默认继续做交付收口。
- 交付收口至少应按实际情况依次检查：
  1. 编译 / 测试
  2. 构建
  3. 发布构建与启动验证
  4. 应用级 smoke test
  5. 文档、`TASK.md`、`doc/devlog/`、忽略文件更新
- 若某项验证未执行，最终输出必须明确说明未执行项、原因、影响范围和建议下一步。
---

## 常见禁止事项
- 禁止在 ViewModel 中直接操作 View 控件
- 禁止在 Service 层编写数据访问逻辑，应放入 `Repository` 层
- 禁止修改数据模型后不补迁移脚本（若使用了数据库）
- 禁止在异步代码中使用阻塞式等待（.Result / .Wait()）
- 禁止绕过既有提交校验流程，如 `git commit --no-verify`
- 禁止在控件中直接设置样式属性（Background、Margin 等）
- 禁止硬编码颜色值（如 `#1A90FF`）、间距值（如 `Margin="20"`）
- 禁止在 Code-behind (.xaml.cs) 中编写业务逻辑
- 禁止使用静态全局类存储业务状态
- 禁止返回非 ResultModel 包装的响应格式
- 禁止抛出非 Azrng 体系的自定义异常
- 禁止在同一个 C# 源文件中声明多个顶层类型；一个 `.cs` 文件只允许包含一个顶层 `class`、`interface`、`abstract class`、`record`、`enum` 等定义。

---

## 交付输出要求
最终输出优先使用中文，并至少说明：
- 本次改了什么
- 核心实现方式
- 修改了哪些文件
- 已执行和未执行的校验
- 当前任务状态
- 风险、阻塞或假设
- 是否已更新 `doc/devlog/`

---

文件结束。
