# AGENTS.md

## 适用范围

- 作用域：ViewModel、Service、命令、业务编排、结果包装与应用层测试
- 触发场景：涉及业务流程、命令逻辑、状态流转、应用服务或应用层测试时阅读

### 阅读摘要
- 建议阅读：改 ViewModel、改命令、改 Service、改业务规则、补应用层验证
- 可先跳过：纯界面样式、纯文档整理、仅发布配置调整
- 优先查看：应用层规则、业务流程 / 状态流转规则、测试规则

### 常见任务入口
- 改命令流程或状态切换：先看 ViewModel / 命令规则与结果处理约束
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
├── TASK.md
└── TaskHistory.md
```

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

### 提交前最小回归
- 默认执行：项目现有的编译、静态检查或单元测试
- ViewModel、命令、Service 改动：至少验证一项业务流程、状态流转或错误分支
- DTO、映射、结果包装改动：至少验证输入输出结构和边界条件
- 与数据访问、集成或配置相关的改动：至少补一项联调或等价验证，证明真实链路生效

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
