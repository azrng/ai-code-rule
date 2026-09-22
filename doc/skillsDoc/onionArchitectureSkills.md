# Onion Architecture Agent Skill 优化任务

你是一名资深 .NET / ASP.NET Core / 洋葱（整洁）架构专家。

我已经向你提供了：

1. `Azrng.Core` 与 `Azrng.AspNetCore.Core` 包完整源码：`C:\Work\gitee\nuget-packages\src\Shared\`
2. 两包随 NuGet 发布的 README 与 XML 文档注释（`Azrng.Core/README.md` 约 1200 行、`Azrng.AspNetCore.Core/README.md` 约 780 行、`Azrng.AspNetCore.Core/ARCHITECTURE.md`、`*.xml` 文档文件）
3. 仓库内的示例项目（`src/Services/`，如 `APIStudy`、`ApiSettingConfig`）
4. 现有 onion-architecture skill：本仓库 `skills/dotnet-skills/onion-architecture`（v1.0.0，SKILL.md + 2 个 references）

你的任务不是开发业务功能，而是**基于当前源码分析并优化现有 onion-architecture Agent Skill**，把它做成高质量、可长期维护、与包版本同步的洋葱架构 Agent Skill。

目标使用环境包括：

* OpenAI Codex
* Claude Code
* Cursor 等支持 Agent Skills / SKILL.md 的编码 Agent

## 范围界定（已确认）

本 skill 覆盖两个基础包：

* `Azrng.Core`（结果模型 / 异常家族 / 分页请求响应模型 / 查询与通用扩展 / 依赖注入标记接口 / ICurrentUser / IJsonSerializer 接口 / Helpers）
* `Azrng.AspNetCore.Core`（全局返回包装三件套 / 模型校验 / 全局异常中间件 / 批量服务注册 RegisterBusinessServices / 审计日志中间件 / RequestId / CORS / 校验特性 / JSON 转换器）

不覆盖、按下列方式处理：

* `Azrng.AspNetCore.Inject`：**不纳入**。洋葱解决方案不用它做注入——`Azrng.AspNetCore.Core` 自带 `RegisterBusinessServices` 批量注入。skill 中最多一句话指路
* Common.EFCore 包家族、AutoAudit（EFCore 审计）、SqlMigration：归 azrng-efcore skill，本 skill 只保留「分页四件套使用模式」层面的引用，不展开仓储 / UoW / 实体基类 API
* `Azrng.Core.Json` / `Azrng.Core.NewtonsoftJson`（IJsonSerializer 实现包）、`Common.HttpClients`（IHttpHelper）、`Azrng.DataAccess`、`Azrng.SettingConfig`：范围外，最多一句话指路
* 周边基础设施包（认证 / 授权 / 定时任务 / 分布式锁 / 缓存，见「周边能力包速查」）：范围外，不展开 API，仅在装配指路处一句话速查
* 洋葱分层规则本身（Core/IDomain/IApplication/Domain/Application/EntityFramework/Service/Adapter/Storage 的职责与依赖方向）：**以现有 skill 为已确认基线**，本次只校准其中涉及类库 API 的部分，不重新推导分层规则

范围外的包在本 skill 中最多一句话指路，不展开 API。

---

## 一、核心原则

不要根据你已有的 ASP.NET Core 或其他框架（ABP、Clean Architecture 模板等）知识直接编写或修改 Skill。

必须优先以我提供的：

* 包源码（csproj、类定义、扩展方法签名、过滤器与中间件实现）
* 两包 README 与 ARCHITECTURE.md
* XML 文档注释
* `src/Services/` 示例项目

作为事实来源。

**分层规则以现有 skill 为基线**：现有 SKILL.md 的分层职责、落点、命名约定视为已确认结论，除非与源码冲突（以源码为准）或明显冗余，否则不改写。

如果你的已有知识与源码冲突，以当前源码为准。

禁止猜测不存在的：

* 过滤器 / 中间件 / 注册入口（`Add*` / `Use*` 扩展方法）
* 异常类型与其到 HTTP 状态码 / ErrorCode 的映射
* `ResultModel` / `GetQueryPageResult` 等契约类型的字段与构造函数
* 校验特性、JSON 转换器、配置项（`CommonMvcConfig` / `AuditLogOptions`）
* 命名空间与 NuGet 包 id

注意：两包均为多目标框架（net6~net10，`Azrng.Core` 另有 netstandard2.1）。若发现条件编译差异，必须随 API 一并记录"签名 + 最低目标框架"。

---

## 二、第一阶段：分析包家族

首先扫描源码，整理两个包的整体能力。

### 1. 包清单

| 包 | 版本（2026-08 探查） | 说明 |
|---|---|---|
| Azrng.Core | 1.20.0 | 基础契约包：ResultModel 家族、异常家族、分页模型、查询/通用扩展、DI 标记接口 |
| Azrng.AspNetCore.Core | 1.4.0 | Web 横切包：返回包装三件套、批量服务注册、审计日志、CORS、校验特性 |

以上版本号必须逐一回 csproj 核实后再写入 Skill，不允许沿用旧记录。

### 2. 全局返回包装体系（重点，现有 skill 已覆盖、需逐一校准）

* `ResultModel` / `IResultModel` / `ResultModelFactory` / `ResultModelExtensions` / `ResultModelExceptionExtensions`（`Azrng.Core.Results`）：字段形状（`isSuccess` / `code` / `message` / `data`）、构造与工厂方法、隐式转换
* `CustomResultPackFilter` + `NoWrapper` 特性 + `ignoreRoutePrefix` 前缀跳过（同文件定义）：包装行为逐分支核实（非 IResultModel → 包装；EmptyResult；已包装不重复；NoWrapper / 前缀跳过）
* 注册入口三个并存，理清关系与推荐场景：
  * `AddMvcResultPackFilter(params string[]? ignoreRoutePrefix)`——单独注册包装过滤器
  * `AddMvcModelVerifyFilter()`——单独注册模型校验过滤器
  * `AddDefaultControllers(Action<CommonMvcConfig>?)`——**一站式**：按 `CommonMvcConfig` 开关同时挂两个过滤器，并自动 `SuppressModelStateInvalidFilter = true`
* `AddMvcResultPackFilterFilter` 是 `[Obsolete]` 别名，skill 不得引导使用
* `CommonMvcConfig` 三个配置项逐项核实：`EnabledCustomerResultPack`（默认 true）、`EnabledModelVerify`（默认 true）、`UseHttpStateCode`（默认 true，控制失败时用真实 HTTP 状态码还是统一 200）

### 3. 异常体系（重点，现有 skill 存在失实项）

* `Azrng.Core.Exceptions` 全家族：`BaseException`、`ParameterException`、`LogicBusinessException`、`NotFoundException`（含 `ThrowIfNull`）、`UnauthorizedException`、`ForbiddenException`、`InternalServerException`、`RetryMarkException`
* `CustomExceptionMiddleware`（位于 `Middleware/CustomExceptionMiddlewareExtensions.cs`）逐 case 核实异常 → HTTP 状态码 → `ErrorCode` 的映射表，**特别核对 401 与 403 的归属**（2026-08 源码：`UnauthorizedException` → 401 未认证；`ForbiddenException` → 403 已认证但无权限——现有 skill 把 ForbiddenException 写成 401，属失实）
* `NotFoundException.ThrowIfNull` 的重载与用法
* `ResultModelExceptionExtensions` 与异常/结果互转能力（若有）

### 4. 模型校验体系

* `ModelVerifyFilter`：`ModelState` 错误 → `ResultModel(false, "参数格式不正确", "400", errors)` 的实际形状
* 自带校验特性（`Azrng.AspNetCore.Core/Attributes/`）：`CollectionNotEmptyAttribute`、`MinValueAttribute`——用法与示例
* `AddDefaultControllers` 开启校验时对 `ApiBehaviorOptions.SuppressModelStateInvalidFilter` 的处理（关闭 MVC 内置 400 自动响应，避免双份错误结构）

### 5. 分页与查询扩展（与 azrng-efcore skill 划清边界）

本 skill 只固化「四件套使用模式」，API 全量签名归 azrng-efcore skill：

* `Azrng.Core.Requests`：`GetPageRequest`（`PageIndex` 默认 1 / `PageSize` 默认 10）、`GetPageSortRequest`（+ `SortContents`）、`SortContent`、`FiledOrderParam`、`BaseRequestDto`、`OperatorDto`
* `Azrng.Core.CommonDto`：`RefAsync<T>`（含到 `T` 的隐式转换）、`PredicateExpressionVisitor`
* `Azrng.Core.Results`：`GetQueryPageResult<T>` 的构造重载（`RefAsync` 版与 PageIndex/PageSize/total 四参版）、`Rows` + `PageInfo` 形状
* `Azrng.Core.Extension/QueryableExtensions.cs`：`WhereIfNotNullOrWhiteSpace` / `WhereIfNotNull` / `WhereIfTrue` / `WhereAny`、动态 `OrderBy(SortContent/SortEnum/FiledOrderParam[])`、`SelectMapper`、`CountBy`、`PagedBy`——onion skill 引用模式即可，不重复罗列签名
* 动态排序（`GetPageSortRequest` + `OrderBy(SortContent)`）当前 onion skill 未提及，评估补一句指路还是补一小节

### 6. 依赖注入体系（重点，现有 skill 已覆盖、需补细节）

* `Azrng.Core.DependencyInjection`：`ITransientDependency` / `IScopedDependency` / `ISingletonDependency` 标记接口
* `RegisterBusinessServices` 两个重载逐项核实：
  * `RegisterBusinessServices(string assembleSearchPattern, string ignoreNameSpace = "")`——按程序集匹配模式扫描（如 `"App.*.dll"`）
  * `RegisterBusinessServices(params Assembly[] assemblies)`——显式传程序集
  * 默认忽略 `Microsoft.` / `System.` 命名空间；`ignoreNameSpace` 逗号分隔
  * `[RequiresUnreferencedCode]` 标注的 AOT / 裁剪警告含义
* 标记接口注册的匹配规则（类实现标记接口 + 对应业务接口的扫描注册方式）

### 7. 中间件与横切能力（现有 skill 基本未覆盖，重点补齐候选）

* 审计日志：`UseAutoAuditLog(List<string>? ignoreFilterRoutes)` / `UseAutoAuditLog(Action<AuditLogOptions>)`、`AuditLogMiddleware`、`AuditLogOptions` 全部配置项、`ILoggerService` / `DefaultLoggerService` / `LogInfo`
* `UseRequestIdMiddleware()`：请求 id 传播
* `UseShowAllServices()` / `UseShowAllServicesMiddleware()`：服务清单端点（开发环境调试用）
* `UseRequestBodyRepetitionRead()`：请求体可重复读
* `UseCorsPolicy(policyName = "DefaultCors")` 与 CORS 注册家族：`AddAnyCors`（任意来源，仅开发环境）、`AddCorsByOrigins`；`AddCorsPolicy` 已在 1.5.0 移除，高级场景直接用框架原生 `AddCors`/`AddPolicy`
* `HttpContextExtensions` / `HttpContextManager` / `ServiceProviderHelper` / `AppSettings`（`Helper/`）
* 上述能力哪些值得进 skill、哪些一句话带过，按「洋葱解决方案常用程度」判断：审计日志与 RequestId 是常用横切，候选补齐；ShowAllServices 等调试能力一句话即可

### 8. 其他待评估能力（分析后决定去留）

* `Azrng.Core`：`ICurrentUser`（当前用户抽象）、`IJsonSerializer` 接口、`CoreGlobalConfig` / `CommonCoreConst`、`RetryTask` 与重试家族、`BaseService`、常用 Helpers（`Guard` / `Snowflake` / `Expressionable` / `PredicateExtensions` / `TreeExtensions` 等，只挑洋葱项目高频的）
* `Azrng.AspNetCore.Core`：`LongToStringConverter`（long→string，防 JS 精度丢失，配合 JSON 序列化配置）、`JsonConverters` 与 `CustomContractResolver`、`PreConfigure`（`IObjectAccessor` / `PreConfigureActionList`）、校验以外的 `Model/`（`ShowServiceConfig`）
* 判断标准：现有 onion skill 的分层 / 装配 / 横切规则用得到的补齐；纯工具类堆不进 skill，防止 SKILL.md 膨胀

### 9. 周边能力包速查（一句话指路，不展开 API）

洋葱解决方案引入缓存 / 定时任务 / 分布式锁 / 认证授权时用的周边包。skill 中只在 Service.csproj 包清单与「该不该新建项目」决策处补一张一句话速查表，声明详细 API 不在本 skill、待各自成专项 skill：

| 包 | 用途 | 注册入口（2026-08 源码核实） |
|---|---|---|
| Azrng.AspNetCore.Authentication.JwtBearer | JWT 认证 | `AddJwtBearerAuthentication` |
| Azrng.AspNetCore.Authorization.Default | 基于权限的授权 | `AddMyAuthorization<TPermissionService>` / `AddPathBasedAuthorization<TPermissionService>` |
| Azrng.AspNetCore.Job.Quartz | 定时任务 | `AddQuartzService` |
| Azrng.DistributeLock.Redis | 分布式锁（Redis 实现，抽象在 `Azrng.DistributeLock.Core`） | `AddRedisLockProvider` |
| Common.Cache.Redis | Redis 缓存 | `AddRedisCacheService` / `AddRedisCacheStore` |
| Common.Cache.MemoryCache | 内存缓存 | `AddMemoryCacheExtension` / `AddMemoryCacheStore` |

注册入口写入 skill 前仍需回源码复核；速查表保持一句话粒度，不写配置项、不写示例代码。

---

## 三、分析示例与包 README

不要只分析包源码。

必须重点分析官方示例与 README，因为：

> Example / README 中的实际写法优先于 AI 自己推断出来的写法。

重点对象：

* `src/Services/APIStudy/Startup.cs`：`AddDefaultControllers()` 一站式注册 + `app.UseGlobalException()` + 注释掉的 `UseAutoAuditLog()` 示例
* `src/Services/ApiSettingConfig/Program.cs`：`.AddMvcResultPackFilter("/api/configDashboard")` 忽略前缀用法
* `Azrng.AspNetCore.Core/README.md` 与 `ARCHITECTURE.md`：三件套、批量注册、审计日志的官方说法
* `Azrng.Core/README.md`：扩展方法、异常、分页模型的官方示例
* 两包 `*.xml` 文档注释：签名级事实来源

注意：`src/Services` 下**没有完整的洋葱分层解决方案**，示例只能证明「包怎么用」，不能证明「分层怎么摆」——分层以现有 skill 为基线，不要从这些小示例反推分层规则。

对于重复示例，不需要全部复制。提取最典型、最值得 AI 学习的模式。

---

## 四、优化 Skill

现有结构：

```text
skills/dotnet-skills/onion-architecture/
├── SKILL.md
└── references/
    ├── layer-responsibilities.md
    └── code-style-and-file-organization.md
```

建议目标结构（按分析结论可调整）：

```text
skills/dotnet-skills/onion-architecture/
├── SKILL.md
└── references/
    ├── layer-responsibilities.md            # 分层基线，只校准其中类库 API 引用
    ├── code-style-and-file-organization.md  # 注释/拆文件规则，基本不动
    ├── result-packing-and-exceptions.md     # 新增候选：三件套细节 + 异常映射表 + ResultModel 家族
    └── program-setup-and-crosscutting.md    # 新增候选：RegisterBusinessServices 细节 + 审计日志/RequestId/CORS 装配
```

约束：

**不要把所有内容全部塞进 SKILL.md。**

SKILL.md 只负责：

* Skill 的适用范围与触发场景（front matter description 同步维护）
* 分层决策入口（现有基线保留）
* 三件套 / 异常 / 分页 / 批量注入的关键约束与易错点
* Program.cs 标准装配模板（校准后）
* references 导航与 azrng-efcore skill 的边界声明
* 禁止事项

详细 API 签名、异常映射全表、审计日志配置项等放 references。

修改带 YAML front matter 的文件时，必须同步更新 `version`（现有 skill 尚无 `last_updated` 字段，本次一并补上，格式与 dotnet-skills 其他 skill 一致）。

---

## 五、SKILL.md 必须告诉 AI 的事情

当用户在洋葱架构解决方案里开发功能时，涉及这两个包的能力，优先用包提供的：

* 返回值走全局包装：action / AppService 成功直接 `return Task<T>`，失败 `throw new ParameterException(msg)` 等业务异常，**不手写 `ResultModel.Error`**
* 三件套装配缺一不可：`AddMvcResultPackFilter`（或 `AddDefaultControllers`）+ `AddMvcModelVerifyFilter` + `app.UseGlobalException()`；漏 `UseGlobalException()` 时业务异常会变成裸 500
* 服务注册用标记接口 + 扫描：实现类标 `IScopedDependency` 等标记接口 + `RegisterBusinessServices("Xxx.*.dll")`，**不逐个手写 `AddScoped`**
* 分页用四件套：`GetPageRequest` 子类 + `WhereIfNotNullOrWhiteSpace` + `ToPageListAsync` + `GetQueryPageResult<T>`，不手写 `CountAsync + Skip/Take` 与自定义 `{List,Total}` 类
* JSON 序列化注入 `IJsonSerializer`，不散落各处直接 `JsonSerializer` / `JsonConvert`
* 需要请求审计 / RequestId 等横切能力时，优先用包内中间件（`UseAutoAuditLog` / `UseRequestIdMiddleware`），不自行实现
* 引入缓存 / 定时任务 / 分布式锁 / 认证授权等基础设施时，优先用速查表中的周边包（如 `Common.Cache.Redis`、`Azrng.AspNetCore.Job.Quartz`），不手写 StackExchange.Redis / Quartz 封装

---

## 六、禁止幻觉 API

这是整个 Skill 最重要的规则之一。

当 AI 不确定某个 API 时：

1. 搜索 references
2. 搜索包 README 与 ARCHITECTURE.md
3. 搜索源码与示例
4. 确认签名、默认值与目标框架

之后才能生成代码。

禁止把以下经验直接套用到本包家族：

* ABP / ABP vNext 的统一返回与异常体系（`WrapResult` / `UserFriendlyException` / 错误码字典）
* 其他 Result 包装库（如 `CloudNative.CloudEvents`、各类 `CommonResult` / `R<T>` 模板）的方法名
* ASP.NET Core 内置 `[ApiController]` 自动 400 行为（本家族用 `ModelVerifyFilter` 替代，`AddDefaultControllers` 已抑制内置行为）
* 手写 `AddScoped<IXxx, Xxx>()` 的注册习惯（本家族是标记接口 + 扫描注册）

易错细节必须显式写进 Skill：

* `ForbiddenException` → **403**、`UnauthorizedException` → **401**，不得混写（现有 skill 此处失实，本次必须修正）
* `AddMvcResultPackFilterFilter` 是已过时别名，不得生成
* `UseHttpStateCode` 默认 true：失败也返回真实 HTTP 状态码（400/404/403/500），不是统一 200
* `RegisterBusinessServices` 的忽略命名空间参数是逗号分隔字符串，且 `Microsoft.` / `System.` 永远默认忽略
* `GetPageRequest` 默认 `PageIndex = 1`、`PageSize = 10`，不是 0

---

## 七、代码生成原则

生成洋葱架构功能代码时，优先顺序：

```text
包提供的横切能力（三件套 / 异常 / 标记接口注册 / 审计中间件）
        ↓
包提供的契约模型（ResultModel / GetQueryPageResult / GetPageRequest）
        ↓
包提供的查询 / 通用扩展（WhereIf* / OrderBy(SortContent) / IJsonSerializer）
        ↓
洋葱分层落点规则（现有 skill 基线）
        ↓
原生 ASP.NET Core MVC / EF Core 能力
```

能够用包能力完成的横切功能，不重新造轮子。

---

## 八、洋葱架构标准模式

现有 skill 已固化「新增一个完整 CRUD 功能的标准流程」，本次校准后 AI 应能够根据：

> 新建一个商品管理功能。

自动推导（与 blazorSkills.md 的 CRUD 推导对齐）：

```text
IApplication/Product/Dto：ProductDto / CreateRequest / UpdateRequest + 列表 Request 继承 GetPageRequest（单业务域 DTO 不放 Core/Models，Core 仅收跨业务域通用 DTO/枚举/常量）
        ↓
IDomain/Entities：实体 + 标记接口注册的落点
        ↓
EntityFramework：Etc 配置类（细节走 azrng-efcore skill）
        ↓
IApplication / Application：IXxxAppService 接口 + 实现标 IScopedDependency
    成功 return Task<T>；失败 throw ParameterException / if (entity is null) throw NotFoundException（is null 原生收窄流分析，不要 ThrowIfNull 后中间变量/断言）
    列表：WhereIfNotNullOrWhiteSpace → ToPageListAsync → GetQueryPageResult<T>
        ↓
Service/Controllers：一行转发，不写 Ok() / 不写 ResultModel
        ↓
Program.cs：AddMvcResultPackFilter("/swagger") + AddMvcModelVerifyFilter
    + RegisterBusinessServices + app.UseGlobalException()
        ↓
全局包装过滤器 → ResultModel<T>(success) / 异常中间件 → 失败结构
```

同时保证使用的全部 API 与当前源码版本一致。

---

## 九、版本意识

在 Skill 中明确记录：

```text
Azrng.Core Version:            1.20.0（逐一回 csproj 核实）
Azrng.AspNetCore.Core Version: 1.4.0（逐一回 csproj 核实）
Source Commit:                 （nuget-packages 仓库当前 commit）
Skill Generated / Updated:
```

如果包源码升级，应能够重新扫描变化并更新 Skill：

* csproj 版本号与 TFM 列表
* 三件套 / 异常映射 / 注册入口的变化（含新增 `Use*` 中间件、Obsolete 标记）
* 包 README 的版本历史章节
* 示例项目的新用法

---

## 十、优化前先输出差距报告

不要立即开始改 SKILL.md。

第一阶段先输出：

```text
Onion Architecture Skill Gap Report
```

对现有 skill 逐节审计，分三类列出：

1. **失实修正**：现有描述与当前源码不一致（异常状态码映射、注册入口签名、配置项默认值、版本号）
2. **缺口补齐**：源码有而 skill 未覆盖、且洋葱解决方案常用的能力（一站式注册、审计日志、RequestId、校验特性、LongToStringConverter 等）
3. **冗余删减**：skill 有但源码已不存在、或与其他 skill 重复无增值的内容

每项标注证据（源码文件路径 / README 章节 / 示例文件）。

确认差距清单以后，再开始修改 skill 文件。

---

## 十一、当前已知差距（2026-08-20 初步探查）

以下为本次探查发现的候选差距，优化时逐项回源码核实后处理：

1. **ForbiddenException 状态码失实（必须修正）**：现有 skill 写 `ForbiddenException → 401`；当前源码 `CustomExceptionMiddleware` 是 `UnauthorizedException → 401`（未认证）、`ForbiddenException → 403`（已认证但无权限），且 skill 完全未提 `UnauthorizedException`、`RetryMarkException`、`BaseException`
2. **`AddDefaultControllers` 一站式注册未覆盖**：`CommonMvcConfig` 三开关（`EnabledCustomerResultPack` / `EnabledModelVerify` 默认 true、`UseHttpStateCode` 默认 true）+ 自动 `SuppressModelStateInvalidFilter`；`APIStudy/Startup.cs` 即此用法。与分开注册两个 `AddMvc*Filter` 的关系与取舍需写清
3. **审计日志中间件未覆盖**：`UseAutoAuditLog` 两个重载（`ignoreFilterRoutes` / `Action<AuditLogOptions>`）、`AuditLogOptions` 配置项、`ILoggerService` 自定义落日志
4. **RequestId 等横切中间件未覆盖**：`UseRequestIdMiddleware`、`UseRequestBodyRepetitionRead`；`UseShowAllServices` 调试能力一句话即可
5. **CORS 家族未覆盖**：`AddAnyCors`（仅开发环境）/ `AddCorsByOrigins` / `UseCorsPolicy`（`AddCorsPolicy` 已在 1.5.0 移除，勿再引用）
6. **校验特性未覆盖**：`CollectionNotEmptyAttribute`、`MinValueAttribute`（配合 `ModelVerifyFilter` 的统一错误结构）
7. **`LongToStringConverter` 未覆盖**：long→string 防 JS 精度丢失，与 JSON 序列化配置的关系
8. **`RegisterBusinessServices` 细节不足**：两个重载（匹配模式 / 显式 Assembly[]）、`ignoreNameSpace` 逗号分隔参数、默认忽略 `Microsoft.` / `System.`、`[RequiresUnreferencedCode]` AOT 警告
9. **动态排序未提**：`GetPageSortRequest` + `SortContent`（列表接口带排序参数的标准做法），评估在 onion skill 补一句还是指路 azrng-efcore skill
10. **`ICurrentUser` 未覆盖**：当前用户抽象的注入与用法（审计 / 创建人场景）
11. **ResultModel 家族不完整**：`ResultModelFactory` / `ResultModelExtensions` / `ResultModelExceptionExtensions` 未提，迁移期 `Task<IResultModel<T>>` 写法的边界需与其对齐
12. **周边能力包选型指路缺失**：缓存 / 定时任务 / 分布式锁 / 认证授权六包（见「周边能力包速查」）仅在 Service.csproj 包清单提过 JwtBearer 一处；按速查表补一句话指路，不展开 API

反向核实项（探查已确认、skill 现状是对的）：

* 三件套注册入口名：`AddMvcResultPackFilter` / `AddMvcModelVerifyFilter` / `UseGlobalException`，均真实存在且非 Obsolete
* `UseGlobalException` 应放管道最外层（最早）的说法与示例一致
* `RefAsync<int>` 到 `T` 隐式转换、`GetQueryPageResult` 双构造均真实存在
* `AppService` 实现标 `IScopedDependency` 自动注册的说法与 `RegisterBusinessServices` 实现一致

---

## 十二、最终验收

Skill 优化完成后，用它完成以下测试（可基于 nuget-packages 仓库示例项目或新建验证工程）：

### Test 1

最小接入：`AddMvcResultPackFilter("/swagger")` + `AddMvcModelVerifyFilter` + `UseGlobalException` + 一个 action 分别覆盖：成功返 DTO（自动包装）、`throw new ParameterException`（400 失败结构）、`if (entity is null) throw new NotFoundException`（404）。

### Test 2

鉴权失败双路径：`UnauthorizedException`（401）与 `ForbiddenException`（403）各一处，核对生成的状态码与 ErrorCode。

### Test 3

分页列表：`GetPageRequest`（或 `GetPageSortRequest` 带动态排序）子类 + `WhereIfNotNullOrWhiteSpace` + `ToPageListAsync` + `GetQueryPageResult<T>` 返回，前端拿到 `{ isSuccess, data: { rows, pageInfo } }`。

### Test 4

Program.cs 完整装配：`RegisterBusinessServices("Xxx.*.dll")` 标记接口自动注册 + `AddDefaultControllers` 一站式注册（对照分开注册写一份等价版本）+ `UseAutoAuditLog` + `UseRequestIdMiddleware`，并验证带 `[CollectionNotEmpty]` / `[MinValue]` 的请求校验失败返回统一结构。

检查生成代码是否：

* 使用真实存在的包 API，签名与重载正确
* 401 / 403 映射正确，没有生成 Obsolete 别名
* 可以编译（实际执行 `dotnet build`，Windows 环境下禁止 `&&` 链式命令）
* 没有虚构配置项、配置键、扩展方法
* 没有混入 ABP 或其他 Result 模板的 API

如果存在问题，修正 Skill，再重新测试。

最终交付：

```text
skills/dotnet-skills/onion-architecture/
```

完整目录（用户本机 `~\.zcode\skills\onion-architecture` 的安装副本另行同步，不作为修改目标）。

---

我建议你的工作流是：

```text
nuget-packages 两包源码 + README + 示例
        ↓
① Repository Exploration（第二阶段清单）
        ↓
② API 清单（三件套 / 异常映射 / 注册入口 / 中间件）
        ↓
③ 高频能力深度分析（全局包装 / 批量注入 / 分页模式）
        ↓
④ Example / README 最佳实践提取
        ↓
⑤ Gap Report（对照现有 skill）
        ↓
⑥ SKILL.md 修订（分层基线保留，只校准类库相关部分）
        ↓
⑦ references 修订 / 新增
        ↓
⑧ dotnet build 编译测试
        ↓
⑨ 修复 Skill
```

特别是**"编译测试"一定要执行**（Windows 环境下禁止 `&&` 链式命令）。这类 skill 最大的问题不是 AI 不懂洋葱架构，而是很容易写出：

```csharp
throw new ForbiddenException(msg);   // 以为映射 401，实际 403
services.AddMvcResultPackFilterFilter("/swagger"); // Obsolete 别名
```

这种**看起来非常合理，但与当前源码行为不符**的代码。让 AI 在真实工程上 `dotnet build` 并核对响应状态码，能把 Skill 质量提高一个档次。

另外，保持现有 skill 的"源码型 Skill"定位而不是"百科全书型 Skill"：SKILL.md 控制在小规模（适用范围 / 分层决策入口 / 关键约束 / 装配模板 / references 索引），详细 API 签名与异常映射全表放 `references/`。这样 Agent 不需要每次加载几万字文档。
