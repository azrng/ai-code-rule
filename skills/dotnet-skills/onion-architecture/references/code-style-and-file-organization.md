# 代码注释与文件组织规范

## 目录
1. 必要中文注释
2. 公开契约类拆分
3. 敏感字段与外部系统边界
4. SQL 说明

---

## 1. 必要中文注释

新增或修改代码时，优先补**能帮助后续维护者理解业务意图、接口契约和外部边界**的中文注释。注释应简短、准确，避免复述代码。

### 必须或优先补注释的位置

- **Controller action**：使用 XML 注释说明接口用途、关键参数、返回结果；Swagger 会消费这些说明。
- **IApplication / IDomain / Adapter 接口方法**：说明应用能力、领域能力或外部系统能力，不把实现细节写进接口注释。
- **外部系统边界**：Adapter 接口或实现中说明下游系统、源码位置、关键契约来源、统计/鉴权等待确认能力。
- **复杂业务分支**：涉及权限、状态流转、事务、缓存、幂等、降级、跨服务补偿时，说明为什么这样处理。
- **敏感字段处理**：涉及 `apiKey`、`token`、`Authorization`、`password`、连接串等字段时，说明是否只透传、是否落库、是否返回、是否日志脱敏。
- **SQL 表和关键字段**：新增表、关键索引、列表展示字段、派生字段来源，使用 `COMMENT ON TABLE/COLUMN` 说明。

### 不要补的注释

- 不要给普通属性、简单赋值、简单 `if`、简单 `return` 写“设置某字段”“返回结果”这类低价值注释。
- 不要在注释里泄露密钥、token、完整连接串、真实生产地址或用户隐私。
- 不要把已经过期的方案、临时调试信息、个人过程记录写进源码注释。
- 不要用注释替代清晰命名；能通过命名表达的，优先改命名。

### 示例

```csharp
/// <summary>
/// 查询 LLM 网关渠道详情。
/// </summary>
/// <param name="channelId">渠道 ID。</param>
/// <returns>渠道基础信息和渠道下模型列表，不返回 API Key。</returns>
[HttpGet("channels/{channelId}")]
public Task<GetLlmProviderChannelDetailResult> GetChannelAsync(string channelId)
{
    return _llmGatewayAppService.GetChannelAsync(channelId);
}
```

```csharp
/// <summary>
/// LiteLLM 提供商渠道适配器。涉及管理接口或调用统计时，需要联合查看 LiteLLM 网关源码。
/// </summary>
public interface ILiteLlmProviderAdapter
{
    /// <summary>
    /// 新增提供商渠道。一个渠道下的多个模型会展开为多条 LiteLLM model deployment。
    /// </summary>
    Task<LiteLlmProviderChannelOperationBo> AddProviderChannelAsync(LiteLlmProviderChannelBo channel,
        CancellationToken cancellationToken = default);
}
```

---

## 2. 公开契约类拆分

DTO、Request、Response、BO、HO、Options、Result 等公开契约类默认**一个类一个文件**。这些类型代表接口契约、跨层契约或外部系统契约，单文件组织能降低冲突、便于检索和审查。

### 默认目录

| 类型 | 推荐目录 | 文件名 |
|---|---|---|
| AppService 请求/响应 DTO | `IApplication/{Module}/Dto/` | `{ClassName}.cs` |
| Core 通用 DTO | `Core/Models/{Module}/` 或既有 Models 目录 | `{ClassName}.cs` |
| Adapter 业务对象 BO | `IDomain/Adapter/{Module}/` | `{ClassName}.cs` |
| Adapter HTTP 对象 HO | `Adapter/Ho/{Module}/` | `{ClassName}.cs` |
| Options / 配置模型 | 使用方所在项目的既有 Options/Ho 目录 | `{ClassName}.cs` |
| Entity / EF 配置 | `IDomain/{Module}/`、`EntityFramework/` | `{EntityName}.cs`、`{EntityName}Etc.cs` |

### 拆分规则

- 新增公开契约类时直接建独立文件，不先放进 `Dtos.cs` / `Models.cs` / `Bo.cs`。
- 修改大杂烩文件时，若任务本身涉及该契约或用户要求拆分，应按类拆出；不要借机大规模重排无关类型。
- 类名、文件名、主要类型名保持一致。
- namespace 沿用原目录对应命名空间，避免拆文件后改变公共契约。
- 拆分后用构建或定向测试确认没有遗漏 `using`、命名空间或可见性问题。

### 允许例外

- `private` / `internal` 且只被当前文件使用的小型辅助类型。
- 与主类型强绑定、无复用价值的局部 mapping extension。
- 测试文件中只服务当前测试类的小型 fixture、stub、fake。
- 枚举极少且只服务当前 DTO 时可随主 DTO；若被多个接口复用则单独文件。

### 反模式

- `LlmGatewayDtos.cs` 长期堆放十几个公开 Request/Response。
- `Bo.cs` 同时包含多个外部系统的 BO。
- `LiteLlmModels.cs` 同时包含登录、模型列表、更新、删除等多组 HO。
- 为了“方便”把 Controller 专用 request class inline 写在 Controller 文件内。

---

## 3. 敏感字段与外部系统边界

- `apiKey`、`token`、`Authorization`、`password`、连接串等敏感字段默认不返回前端、不写日志、不写任务记录、不写 SQL 注释明文。
- 若业务要求 chat.api 只透传密钥到下游，DTO 和注释要写清“不落库、不返回”；更新接口中空密钥通常不应覆盖下游已有密钥。
- Adapter 接口或实现中可记录下游源码路径、接口来源、待确认能力，例如“调用量统计依赖网关后续按 `channel_id` 聚合接口”。
- 外部系统 Ho 与内部 Bo 分离；外部字段变化优先收敛在 Adapter 层，不把下游原始协议散落到 Application 或 Controller。

---

## 4. SQL 说明

新增表或关键字段时，在初始化脚本或迁移脚本里补 `COMMENT ON TABLE/COLUMN`。

优先说明：
- 表的业务用途。
- 管理列表展示字段，如名称、类型、状态、创建时间。
- 非落库派生字段来源，如“今日调用量由 LiteLLM 按 `channel_id` 聚合返回”。
- 软删除、禁用、外部系统 ID、唯一索引相关字段含义。

示例：

```sql
COMMENT ON TABLE chat.llm_provider_channel IS 'LLM网关渠道表；管理列表展示渠道名称、渠道类型、启用状态、今日调用量和创建时间，今日调用量不落库，由LiteLLM按channel_id聚合返回';
COMMENT ON COLUMN chat.llm_provider_channel.channel_name IS '渠道名称，管理列表展示字段';
COMMENT ON COLUMN chat.llm_provider_channel.disabled IS '是否禁用；管理列表按该字段展示启用/禁用状态';
```
