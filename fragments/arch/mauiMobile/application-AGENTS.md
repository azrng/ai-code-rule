---
rule_id: application-agents
version: 1.15.0
last_updated: 2026-08-28
dependencies: [agents-root]
---

# 应用规则

<!-- include: shared/application/scope.md -->

<!-- include: shared/application/common-tasks.md -->

---

## 技术栈

<!-- include: shared/application/layered-arch.md -->

### 核心库
- `CommunityToolkit.Mvvm`：`[ObservableProperty]`、`[RelayCommand]`、`IMessenger`
- `Syncfusion.Maui.Toolkit`：UI 控件库（图表、下拉刷新、卡片等）
- `CommunityToolkit.Maui`：移动端扩展能力（弹窗、转换器、动画等）
- `Azrng.Core`：实体基类、扩展方法、工具类、结果包装、异常体系（如项目已使用则沿用）

<!-- include: shared/application/tech-choice.md -->

---

<!-- include: shared/application/advisory.md -->

---

## 推荐目录结构

- 应用层目录建议聚焦在 `src/AppName/ViewModels/`、`src/AppName/Services/`、`src/AppName/Models/` 下组织，优先复用现有结构，不强制迁移。

```text
src/AppName/
├── ViewModels/
│   ├── Base/
│   ├── Dialogs/
│   └── [Module]/
├── Services/
│   ├── Interfaces/            # 服务接口定义
│   └── Implementations/       # 服务实现
└── Models/
    ├── Entities/              # 数据实体
    └── DTOs/                  # 数据传输对象
```

### 目录职责映射

按职责将类放到对应目录，文件命名与类名保持一致；View（`.xaml`）的组织见 `ui-AGENTS.md`。

| 目录 | 放什么类 | 命名约定 |
| --- | --- | --- |
| `ViewModels/Base/` | ViewModel 基类 | `*ViewModelBase.cs` |
| `ViewModels/[Module]/`、`ViewModels/Dialogs/` | 模块 / 对话框 ViewModel（状态 + 命令编排） | `{名称}ViewModel.cs` |
| `Services/Interfaces/` | 服务接口 | `I{业务域}Service.cs` |
| `Services/Implementations/` | 服务实现 | `{业务域}Service.cs` |
| `Models/Entities/` | 数据实体，一类一文件 | `{实体名}.cs` |
| `Models/DTOs/` | DTO（视图与业务层契约），一类一文件 | `{名称}Dto.cs` |

---

<!-- include: shared/application/stage2-impl.md -->

---

## 应用层规则

### 分层边界规则
- View 只负责展示，不承载业务逻辑
- ViewModel 负责状态管理、命令触发、调用 Service 与处理用户可见结果
- Service 负责业务逻辑、流程编排和规则校验，不直接操作 View
- Repository 是数据访问边界，具体实现细则见 `infrastructure-AGENTS.md`
- Model / DTO 负责承载业务数据，不在其中夹带 UI 行为

### ViewModel 规则
- ViewModel 中可以组织用户操作流程，但禁止直接写 SQL 或直接依赖存储细节
- 命令执行后的成功、失败、空状态必须显式反馈到界面状态
- 异常在 ViewModel 层转换为用户友好的提示信息，不把底层异常原样暴露给用户
- 跨 ViewModel 协作优先使用 `IMessenger` 或显式服务，不依赖静态全局状态
- 禁止在 ViewModel、Service、Repository、Model、DTO 等 C# 类中使用主构造函数（Primary Constructor），统一使用显式构造函数

### Service 层规则
- 服务类必须实现接口，接口定义在 `Services/Interfaces/` 目录
- 服务方法必须优先采用异步形式（返回 `Task<T>` 或 `ValueTask<T>`）
- 服务层处理所有业务逻辑，不直接访问 Repository 以外的数据依赖
- 服务层异常必须统一封装，抛出项目异常体系中的业务异常类型
- 若项目使用标记接口模式批量注册（如 `ITransientDependency`），服务类应实现对应接口

### DTO 与模型规则
- DTO 是视图层与业务逻辑层之间的稳定契约，变更时必须同步更新相关映射和调用方
- 若仓库已有真实实体或 DTO 结构，优先沿用现状，不为模板强行改名或重组
- 数据转换规则应集中放在 Service 或明确的映射层，不散落在 View 或 Repository 调用点

### 代码组织规范
- 一个文件只放一个主类型：ViewModel、Service 接口与实现、Entity、DTO 等，默认一个类一个文件，文件名与类名一致。
- DTO 目录组织：DTO 放 `Models/DTOs/`，一类一文件，不合并到 `Dtos.cs`。
- 触发拆分的信号：职责混杂、同文件出现多个主类型、参数或字段持续堆叠、方法跨多个不相关业务时，应主动拆分。
- 允许例外：`private` / `internal` 且只服务当前文件的小辅助类型、与主类型强绑定的局部 mapping extension、测试文件中只服务当前测试类的小型 fixture。
- 反模式：`Dtos.cs` 长期堆放多个 DTO；把 ViewModel 专用模型内联写在 ViewModel 文件里；业务逻辑塞进 View。

<!-- include: shared/application/kiss.md -->

### 统一结果包装
- 服务层方法返回值统一使用项目既有的结果包装类型（如 `ResultModel<T>`）
- 成功响应：`ResultModel<T>.Success(data)`
- 错误响应：`ResultModel<T>.Failure(message, errorCode)`
- 对于仅表示操作结果的方法，也应保持统一的结果语义，不返回随意结构

<!-- include: shared/application/exception-handling.md -->

### 移动端应用生命周期
- 应用生命周期事件（`OnStart`、`OnResume`、`OnSleep` / `OnStop`）中只做必要的初始化和状态保存
- 后台返回前台时，应根据业务需要刷新关键数据
- 网络状态变化时，应通过 `Connectivity.Current` 监听并给出适当提示
- 权限请求（相机、位置、存储等）必须在真正需要时才请求，禁止启动时一次性全部请求

### 代码注释规范
- 必须补 XML 注释的位置：
  - ViewModel 公共命令与关键属性：用 `<summary>` 说明用户操作意图与可观察状态。
  - Service 接口方法：说明该能力做什么，不写实现细节。
  - 不来自接口的实现类自有方法（接口未约束的核心方法）：补 `<summary>` 说明该能力做什么。
- 接口已注释时，实现方法不重复注释，也不加 `/// <inheritdoc />`；只有实现类自有的、接口未定义的方法才需要补注释。
- 默认不写 `<remarks>`：实现类、实现方法上的 `<remarks>` 多为实现细节，按"不写实现细节"原则省略。
  - 例外：ViewModel 公共命令需要向使用者说明绑定约定、关联属性、触发条件等对外信息时，可保留精简后的 `<remarks>`，但不写内部实现说明。
- 优先补行内注释的位置：复杂状态流转、命令分支、外部调用编排，说明“为什么这样做”——包括 fail fast 的原因、为什么跳过某类输入、为什么保留某一步骤。
- 不要补的注释：普通属性的 get / set、简单 `if` / `return`、"返回结果"这类复述代码的低价值注释。
- 注释统一使用中文，不在注释里泄露密钥、token、连接串或真实生产地址。

<!-- include: shared/application/anti-patterns.md -->

<!-- include: shared/application/maintainability-check.md -->

---

## 测试规则

<!-- include: shared/application/pre-commit-regression.md -->

<!-- include: shared/application/general-requirements.md -->

### 应用层测试
- ViewModel 命令执行、属性变更、消息发送发生变化时，应补充对应测试
- Service 层业务逻辑、数据转换、异常处理发生变化时，应补充对应测试
- 推荐使用 xUnit 作为测试框架

<!-- include: shared/desktop/external-dependencies.md -->

<!-- include: shared/no-test-fallback.md -->

---
