---
rule_id: application-agents
version: 1.13.0
last_updated: 2026-08-27
dependencies: [agents-root]
---

# 应用规则

## 适用范围

- 作用域：命令、Service、业务编排、结果包装与应用层测试
- 触发场景：涉及业务流程、命令逻辑、状态流转、应用服务或应用层测试时阅读

### 常见任务入口
- 改命令流程或状态切换：先看命令规则与结果处理约束
- 改 Service 或业务规则：先看应用层职责边界与异常处理规范
- 改 DTO、映射、结果包装：先看输入输出结构与边界约定
- 补应用层回归：先看 `提交前最小回归` 与测试规则

---

## 技术栈

### 运行时
- .NET 10 (2025 年已发布正式版)
- 控制台应用程序（Console App）

### 核心库
- `Azrng.Core`：基础类库（实体基类、扩展方法、工具类、结果包装、异常体系）
- `Azrng.Core.Json`：JSON 序列化（基于 System.Text.Json）

### 命令行解析
- `System.CommandLine`：微软官方命令行解析库
- 支持子命令（verb/command）、选项（option）、参数（argument）
- 支持帮助信息自动生成、参数校验、默认值

### 依赖注入
- `Microsoft.Extensions.DependencyInjection`：依赖注入容器
- `Microsoft.Extensions.Configuration`：配置管理（支持 appsettings.json、环境变量、命令行参数）
- `Microsoft.Extensions.Logging`：日志抽象
- 使用 Azrng 标记接口方式：
  - `ITransientDependency`：临时注入
  - `IScopedDependency`：作用域注入
  - `ISingletonDependency`：单例注入
- 在 Program.cs 中调用 `services.RegisterBusinessServices(assemblies)` 扫描并注册服务

### 日志
- `Serilog`：结构化日志
- 输出到控制台和文件

### 部署
- 本地运行，无需 Docker
- 支持发布为单文件（`dotnet publish -c Release`）

如果仓库已经有真实实现，以现有代码为准，不要强行重构或替换技术栈。
技术债务与重构判断遵循根 `AGENTS.md` 的全局规则。

---

## 主动建议规则
- 发现命令解析、Service、配置、日志或业务逻辑职责混杂，导致文件膨胀、复用困难或测试困难时，应主动提醒并建议最小拆分点
- 发现配置、日志、异常处理、DTO 或命令参数与现有封装不一致时，应先说明差异和影响，不直接引入平行体系
- 复用、查证优先级等共性规则见根 `AGENTS.md`「模型行为约束与主动建议」

---

## 推荐目录结构
若仓库尚未形成稳定结构，可优先参考以下组织方式；若仓库已有实现，以现状为准，不强制迁移。

```text
project-root/
├── src/
│   └── YourProject/                     # 控制台项目
│       ├── Commands/                     # 命令处理（每个子命令一个类）
│       ├── Services/                     # 业务服务接口与实现
│       │   └── Interfaces/              # 服务接口定义
│       ├── Models/                      # 数据模型
│       │   ├── DTOs/                    # 数据传输对象
│       │   └── Enums/                   # 枚举
│       ├── Extensions/                  # 扩展方法
│       ├── Helpers/                     # 工具类
│       ├── Program.cs                   # 应用入口（参数解析 + 启动）
│       ├── appsettings.json             # 配置文件
│       └── YourProject.csproj
├── tests/
│   └── YourProject.Tests/               # 测试项目
├── doc/
│   ├── design/
│   └── requirement.md
├── .gitignore
├── AGENTS.md
├── CLAUDE.md
├── GEMINI.md
└── TASK.md
```

### 目录职责映射

按职责将类放到对应目录，文件命名与类名保持一致。

| 目录 | 放什么类 | 命名约定 |
| --- | --- | --- |
| `Commands/` | 命令处理（每个子命令一个类），仅做参数解析与 Service 调度 | `{动词}{资源}Command.cs`（如 `ExportOrderCommand.cs`） |
| `Services/` | 业务服务实现（业务逻辑、外部调用编排） | `{业务域}Service.cs` |
| `Services/Interfaces/` | 服务接口 | `I{业务域}Service.cs` |
| `Models/DTOs/` | 输入参数 / 输出结果 DTO，一类一文件 | `{名称}Input.cs`、`{名称}Result.cs` |
| `Models/Enums/` | 枚举；跨命令复用的单独成文件 | `{枚举名}.cs` |
| `Extensions/` | 扩展方法、DI 注册扩展 | `{类型}Extensions.cs` |
| `Helpers/` | 工具类（纯函数、无业务逻辑） | `{名称}Helper.cs` |
| `Program.cs` | 应用入口：构建配置、注册依赖、调度命令 | — |

---

## 控制台规则

### Program.cs 规则
- Program.cs 只负责三件事：构建配置、注册依赖、解析参数并调度命令
- 禁止在 Program.cs 中编写业务逻辑
- 使用 `System.CommandLine` 的 `RootCommand` 组织命令结构
- 入口方法保持简洁，核心逻辑委托给 Command 类

### 命令行参数规则
- 使用 `System.CommandLine` 定义命令、选项和参数
- 每个子命令（verb）对应一个 Command 类，放在 `Commands/` 目录
- 选项命名使用 kebab-case（如 `--output-dir`、`--max-count`）
- 必须为每个命令和选项提供描述（用于自动生成帮助信息）
- 必要参数设为必填，可选参数提供合理默认值
- 参数校验失败时输出清晰的错误提示和用法示例
- 示例结构：
  ```csharp
  var rootCommand = new RootCommand("应用描述");
  var command = new Command("action", "执行操作");
  var option = new Option<string>("--name", "名称") { IsRequired = true };
  command.AddOption(option);
  command.SetHandler(async (name) => { /* 调用 Service */ }, option);
  rootCommand.AddCommand(command);
  return await rootCommand.InvokeAsync(args);
  ```

### Service 规则
- 服务类实现接口，接口定义在 `Services/Interfaces/` 目录
- 耗时操作必须是异步的（返回 `Task<T>`）
- 使用 DTO 进行数据传递
- 业务异常抛出继承自 Azrng 异常体系的自定义异常
- 服务类实现标记接口（`ITransientDependency`/`IScopedDependency`/`ISingletonDependency`）以自动注册
- 禁止在 Command、Service、DTO、Options、Helper 等 C# 类中使用主构造函数（Primary Constructor），统一使用显式构造函数。

### 代码组织规范
- 一个文件只放一个主类型：Command、Service 接口与实现、DTO、Options、枚举等，默认一个类一个文件，文件名与类名一致。
- DTO 目录组织：输入 / 输出 DTO 放 `Models/DTOs/`，一类一文件，不合并到 `Dtos.cs`。
- 触发拆分的信号：职责混杂、同文件出现多个主类型、参数或字段持续堆叠、方法跨多个不相关业务时，应主动拆分。
- 允许例外：`private` / `internal` 且只服务当前文件的小辅助类型、与主类型强绑定的局部 mapping extension、测试文件中只服务当前测试类的小型 fixture。
- 反模式：`Dtos.cs` 长期堆放多个输入 / 输出模型；把命令专用参数内联写在 Command 文件里。

### 核心逻辑可读性（KISS）
- 以下场景优先显式直写，不要本能压成一条链：聚合、分组排序后取 Top N、多层字典构造、有失败边界的控制流、需要稳定输出顺序的逻辑。
- 优先形式：`foreach`、`if / else`、中间变量、明确命名的临时集合、显式排序 / 裁剪 / 显式异常。
- 谨慎使用：多层嵌套 LINQ、一次性 `GroupBy + Select + ToDictionary + ToList` 链、只为“看起来干净”而拆的 helper。
- helper / 拆分只在降低认知负担时才成立：让职责边界更清楚、真正复用、让调用方少理解细节；若拆完读者要在多个位置来回跳，或用 helper 隐藏本该直说的业务判断，就退回直白写法。
- 共性的“先读再写”“范围最小扰动”见根 `AGENTS.md`「理解检查清单」与「全局工作规则」

### 数据模型规则
- DTO 按用途区分（输入参数、输出结果），文件组织见 `代码组织规范`。
- 枚举放在 `Models/Enums/` 目录
- 输出模型需考虑控制台展示友好性

### 配置管理规则
- 使用 `Microsoft.Extensions.Configuration` 管理配置
- 配置来源优先级（高到低）：命令行参数 > 环境变量 > appsettings.json
- 敏感配置（密钥、密码）禁止写入 appsettings.json，通过环境变量或用户机密传入
- 配置项通过 Options 模式绑定到强类型类：
  ```csharp
  services.Configure<AppOptions>(configuration.GetSection("App"));
  ```
- 禁止在代码中硬编码配置值

### 日志规范
- 使用 `Microsoft.Extensions.Logging` 抽象，配合 Serilog 实现
- Program.cs 启动时配置 Serilog：
  ```csharp
  Log.Logger = new LoggerConfiguration()
      .ReadFrom.Configuration(configuration)
      .WriteTo.Console()
      .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day)
      .CreateLogger();
  ```
- 日志级别：Verbose > Debug > Information > Warning > Error > Fatal
- 正常业务流程使用 `Information` 级别
- 可预期的业务异常使用 `Warning` 级别
- 不可预期的系统异常使用 `Error` 级别
- 禁止使用 `Console.WriteLine` 输出业务信息，统一使用 logger

### 异常处理规范
- 顶层捕获所有未处理异常，输出友好错误信息后以非零退出码退出
- 业务异常继承 Azrng 异常体系：
  - `LogicBusinessException`：业务逻辑异常
  - `ParameterException`：参数校验异常
  - `NotFoundException`：资源不存在
  - `InternalServerException`：内部错误
- Command 内捕获异常后决定退出码：
  - 0：成功
  - 1：一般错误
  - 2：参数错误
- 示例：
  ```csharp
  try
  {
      await service.DoWorkAsync();
      return 0;
  }
  catch (ParameterException ex)
  {
      logger.LogWarning(ex, "参数错误");
      return 2;
  }
  catch (Exception ex)
  {
      logger.LogError(ex, "执行失败");
      return 1;
  }
  ```

### 依赖注入规范
- 服务类必须实现以下接口之一：
  - `ITransientDependency`：每次请求创建新实例
  - `IScopedDependency`：每个作用域内单例
  - `ISingletonDependency`：应用程序生命周期单例
- Command 通过构造函数注入服务（在 SetHandler 中手动创建作用域解析）
- 禁止使用静态服务定位器 pattern
- 禁止手动在 Program.cs 中逐一注册服务，应使用 `RegisterBusinessServices` 批量扫描注册

### 退出码规范
| 退出码 | 含义 |
|--------|------|
| 0 | 成功 |
| 1 | 一般错误 |
| 2 | 参数错误 |

- 每个命令执行完毕必须返回明确的退出码
- 主入口将退出码传递给 `Environment.Exit`

### 字段命名约定
- C# 属性：PascalCase
- 命令行选项：kebab-case（`--output-dir`）
- JSON 配置键：camelCase

### 代码注释规范
- 必须补 XML 注释的位置：
  - Service 接口方法：说明该能力做什么，不写实现细节。
  - 公开 Command 类：用 `<summary>` 说明命令用途，便于生成帮助信息与维护。
  - 不来自接口的实现类自有方法（接口未约束的核心方法）：补 `<summary>` 说明该能力做什么。
- 接口已注释时，实现方法不重复注释，也不加 `/// <inheritdoc />`；只有实现类自有的、接口未定义的方法才需要补注释。
- 默认不写 `<remarks>`：实现类、实现方法上的 `<remarks>` 多为实现细节，按"不写实现细节"原则省略。
  - 例外：公开 Command 类需要向用户说明用法示例、退出码约定等对外信息时，可保留精简后的 `<remarks>`，但不写内部实现说明。
- 优先补行内注释的位置：复杂命令分支、外部调用编排、重试 / 降级等，说明“为什么这样做”——包括 fail fast 的原因、为什么跳过某类输入、为什么保留某一步骤。
- 不要补的注释：普通属性的 get / set、简单 `if` / `return`、"返回结果"这类复述代码的低价值注释。
- 注释统一使用中文，不在注释里泄露密钥、token、连接串或真实生产地址。

### 反模式与修根因清单
- 这是根 `AGENTS.md`「修 Bug 必须定位根因」在 .NET 中的具体禁止项，禁止用下列方式掩盖问题：
  - 空 `catch` 或 `catch (Exception)` 吞异常后静默继续
  - 用 `!`（null-forgiving）压制可空告警而非真正处理 null
  - 在异步上下文用 `.Result` / `.Wait()` 同步阻塞
  - `#pragma warning disable` 关闭编译 / 分析器告警掩盖问题
  - 删除或注释测试让构建通过
  - 用一层抽象重写掩盖原始复杂度，而不是简化它
- 出现这些倾向时立刻回退一步：复杂 LINQ 比业务逻辑本身更难读；helper 数量增加但理解成本没下降；核心逻辑缺注释只能靠猜。

### 可维护性自检（提交前）
提交前对核心逻辑至少问一遍：
- 维护者能否一遍看懂这段核心逻辑？
- 是否必须跳转多个 helper 才知道发生了什么？
- 关键方法和关键逻辑有没有注释？
- 这是不是当前需求的最简单可行实现？
- 我是不是为了“写得像高手”而牺牲了可读性？
- 犹豫写“抽象版”还是“直白版”时，默认选直白版。

---

## 测试规则

### 提交前最小回归
- 默认执行：项目现有的编译、静态检查或单元测试
- 命令、Service 改动：至少验证一项业务流程、状态流转或错误分支
- DTO、映射、结果包装改动：至少验证输入输出结构和边界条件
- 与数据访问、集成或配置相关的改动：至少补一项集成验证或等价验证，证明真实链路生效

### 总体要求
- 影响行为的改动应优先补充或更新测试
- 若本次改动未补测试，必须在最终说明中写明原因和风险
- 测试应覆盖真实业务行为

### 测试分类
- 测试框架：xUnit + Moq
- Service 测试：验证业务逻辑分支，使用 mock 隔离依赖
- Command 测试：验证参数解析、退出码、输出结果
- 集成测试：验证完整命令行执行流程

### 外部依赖与数据
- 测试中不要真实调用第三方服务，统一使用 mock
- 测试数据应尽量最小化、可读、可重复执行
- 不要让测试依赖本地人工状态

### 无法执行测试时
- 必须说明未执行的测试类型
- 必须说明未执行原因
- 必须说明潜在影响范围和风险

---
