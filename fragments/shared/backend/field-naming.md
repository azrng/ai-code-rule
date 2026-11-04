### 字段命名约定
- C# 属性：PascalCase
- JSON 输出：camelCase（通过 `System.Text.Json` 配置自动转换）
- 数据库字段：snake_case（通过 EF Core 命名约定配置）
- 私有依赖字段（构造函数注入的服务 / 仓储 / 基础设施）：字段名必须携带依赖对象的类型语义，禁止只指示架构层的泛称（`_domain`、`_service`、`_sso`、`_helper`——同一泛称跨文件指代不同依赖，读代码与生成新类时无法从既有文件推断）。口径：领域服务 `_{域}Domain`（`ITrialDomainService _trialDomain`）、注入的应用服务 `_{域}AppService`、仓储沿用实体复数名（`IToolInfoRepository _tools`）、通用基础件保持语义名（`_logger`、`_unitOfWork`、`_currentUser`）。新增代码先对齐所在文件的既有字段风格，同一构造内不混用。
