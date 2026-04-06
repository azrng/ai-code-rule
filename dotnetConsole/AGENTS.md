# AGENTS.md

## 目标
职责划分如下：
- `AGENTS.md` / `CLAUDE.md` / `GEMINI.md`：各 AI Agent 统一工作规则、任务管理机制、交付要求，内容保持同步
- `TASK.md`：具体任务内容与任务进度

---

## 工作方法

1、行动前先思考。写代码前先阅读现有文件。
2、输出要简洁，但推理要彻底。
3、优先编辑而不是重写整个文件。
4、除非文件可能已变更，否则无需重复阅读已读文件。
5、完成前需测试代码。
6、避免奉承性开头或冗余结尾。
7、保持解决方案简单直接。
8、用户指令始终优先于本文件内容。

---

## 注意事项
1. 你需要理解用户提出的需求的业务逻辑然后再做设计和开发
2. 控制台应用必须能通过命令行参数独立运行，输出真实结果
3. 参数解析失败时必须给出清晰的错误提示和用法说明
4. 业务逻辑与入口分离，Program.cs 只负责解析参数和启动流程

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
│   ├── devlog/
│   ├── design/
│   └── requirement.md
├── .gitignore
├── AGENTS.md
├── CLAUDE.md
├── GEMINI.md
└── TASK.md
```

---

## 使用方式
每个 AI Agent 在开始修改前都必须：
1. 先阅读本文档，理解核心规则和流程
2. 再阅读相关模块和现有实现
3. 优先复用当前结构，再决定是否新增文件

进行控制台开发时：
1. Program.cs 只负责参数解析和启动流程，业务逻辑放在 Service 中
2. 每个子命令对应一个 Command 类，职责单一
3. 服务类实现 `ITransientDependency`/`IScopedDependency`/`ISingletonDependency` 接口
4. 异常处理使用 Azrng 异常体系

---

## 核心规则
- 先理解，再修改。
- 先复用，再新增。
- 交付必须可直接使用，不能只停留在演示层。
- 不允许新增平行配置体系。
- 不做与当前任务无关的重构。
- 不要猜测问题，要实证排查：通过日志、断点、数据等实际证据定位原因，禁止凭猜测修改代码。
- 所有改动都必须可说明、可验证。

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

### 数据模型规则
- DTO 按用途区分：输入参数、输出结果
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

---

## 测试规则

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
  - `feat(command): add export sub-command`
  - `fix(args): correct default value handling`
  - `chore(dotnet): pin NuGet package version`

### 交付前自查
- 编译（`dotnet build`）
- 测试（`dotnet test`）
- 命令行参数解析验证（`dotnet run -- --help`）
- 新增环境变量已同步更新相关文档

### 提交触发规则
- 每次修改完后都必须提交代码
- 若本次开发生成了本地运行产物、缓存、构建产物或依赖目录，应同步检查并更新 `.gitignore`

---

## 任务管理机制
`TASK.md` 是唯一任务记录文件，用于记录具体任务内容，不负责承载规则解释。

### 任务状态
- `TODO`：未开始
- `DOING`：进行中
- `BLOCKED`：被阻塞
- `REVIEW`：已完成开发，等待确认
- `DONE`：已完成

### 执行规则
1. 开始工作前，先查看 `TASK.md` 是否已有对应任务
2. 如果已有任务，优先更新原任务，不重复新建
3. 如果没有任务，在 `TASK.md` 中新增最小任务记录
4. 开始执行时，将任务状态更新为 `DOING`
5. 若任务无法继续，更新为 `BLOCKED` 并写明原因
6. 开发完成但仍需确认时，更新为 `REVIEW`
7. 验证完成并确认交付后，更新为 `DONE`

### 每个任务至少包含
- 任务 ID
- 任务名称
- 任务目标
- 任务状态
- 最近更新时间

### AI Agent 要求
- 开始任务前先同步任务状态
- 开发过程中及时更新 `TASK.md`
- 范围变化时同步补充
- 每次开发完成后，都需在 `doc/devlog/` 下新增一份本次开发简短说明的 Markdown 记录

---

## 文档更新要求
以下内容发生变化时，必须同步更新相关文档：
- 命令行参数或用法
- 环境变量
- 配置项
- 任务流程
- 对外使用方式或接口约定

优先更新已有文档，不新增重复说明。
每次开发完成后，必须在 `doc/devlog/` 下补充一份简短开发记录，作为本次工作的可追踪说明。

### 文档目录约定
- `doc/requirement.md`：用户编写的需求文档，AI 不应将设计说明或开发过程记录写入此文件
- `doc/design/设计文档.md`：项目设计文档，全项目一份。由用户与 AI 协作产出，用户确认后作为开发的需求基准。AI 可在文件中追加新功能章节，不删除已有内容，不新建平行文件
- `doc/devlog/`：每次开发完成后的简短开发日志

### devlog 要求
- 文件目录：`doc/devlog/`
- 文件格式：Markdown
- 文件命名建议：`YYYY-MM-DD-HHmm-任务简述.md`
- 内容保持简短，至少包含：
  - 本次目标
  - 核心改动
  - 修改文件
  - 校验情况
  - 风险或遗留项

---

## 交付收口规范
- 当用户请求"开始开发"而非"只出方案"时，AI Agent 在实现完成后应默认继续做交付收口，而不是停在代码编写阶段
- 交付收口至少应按实际情况依次检查：
  1. 编译（`dotnet build`）
  2. 测试（`dotnet test`）
  3. 命令行参数解析验证（`dotnet run -- --help` 确认帮助信息正确）
  4. 文档、`TASK.md`、`doc/devlog/`、忽略文件更新
- 若某项验证未执行，最终输出必须明确说明未执行项、原因、影响范围和建议下一步

---

## 常见禁止事项
- 禁止在 Program.cs 中编写业务逻辑，应放入 Service
- 禁止在异步代码中使用阻塞式等待（`.Result` / `.Wait()`）
- 禁止绕过既有提交校验流程，如 `git commit --no-verify`
- 禁止手动配置依赖注入，应使用标记接口方式（ITransientDependency/IScopedDependency/ISingletonDependency）
- 禁止抛出非 Azrng 体系的自定义异常
- 禁止使用 `Console.WriteLine` 输出业务信息，统一使用 logger
- 禁止在代码中硬编码配置值
- 禁止在异步方法中同步阻塞调用（如 `.Result`、`.Wait()`、`Thread.Sleep`）

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
