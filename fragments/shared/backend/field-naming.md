### 字段命名约定
- C# 属性：PascalCase
- JSON 输出：camelCase（通过 `System.Text.Json` 配置自动转换）
- 数据库字段：snake_case（通过 EF Core 命名约定配置）
- 私有依赖字段（构造函数注入的服务 / 仓储 / 基础设施）：字段名必须携带依赖对象的类型语义，禁止只指示架构层的泛称（`_domain`、`_service`、`_sso`、`_helper`——同一泛称跨文件指代不同依赖，读代码与生成新类时无法从既有文件推断）。统一口径：注入字段取 `_{接口类型去前导 I、首字母小写}全名`，禁止截尾简写与复数简写——领域服务 `_trialDomainService`（`ITrialDomainService`，不写 `_trialDomain`）、注入的应用服务 `_{域}AppService`（`IBearerAuthService` → `_bearerAuthService`）、自定义仓储 `_{实体单数}Repository`（`IToolInfoRepository _toolInfoRepository`，不写 `_tools`）、仓储基类 `IBaseRepository<T>` 按 `_{实体单数}BaseRepository`（`_menuBaseRepository`，与自定义仓储字段天然消歧）、通用基础件保持语义名（`_logger`、`_unitOfWork`、`_currentUser`）。单测中手写 Fake 的私有字段不受限。新增代码先对齐所在文件的既有字段风格，同一构造内不混用。
