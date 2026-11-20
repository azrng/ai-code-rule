# 认证与授权（JWT）装配细则

> 由 SKILL.md「认证与授权（JWT）」节下沉：设计或改造登录 / 鉴权方案时通读本文件。包选型红线与 FallbackPolicy 硬约束见 SKILL.md。

登录 / 鉴权是与全局包装、分页同级的横切约定。**设计认证方案时通读本文件**，不要按通用经验选裸 `Microsoft.AspNetCore.Authentication.JwtBearer` 再自写 `JwtTokenProvider`——那会绕开包内置的密钥校验、统一响应与 token 生命周期管理，产生一次设计返工。

## 包选型与用法

- 认证包用 `Azrng.AspNetCore.Authentication.JwtBearer`（Service.csproj 引用，当前基线 1.5.0），**禁止**裸引 `Microsoft.AspNetCore.Authentication.JwtBearer` + 自写 token 生成器。
- 生成 / 校验 token 走 `IBearerAuthService`：登录用例所在 AppService 注入它发 token；它**没有自动注册**，需在 Startup 手动 `services.AddScoped<IBearerAuthService, JwtBearerAuthService>()`。
- JWT 密钥要求 **≥32 位且 ≥8 种字符**（旧包文档「≥16」口径已过期，以锁定版本包内校验器实测为准）。
- 需要「操作者从已认证身份解析」时，直接用 `Azrng.Core` 的 `ICurrentUser<TSubject>`（见 `layer-responsibilities.md → 当前用户上下文`），禁止自造同名抽象。

## Startup 装配（实测形态）

```csharp
// RegisterServices 内
services.Configure<JwtTokenConfig>(options =>
{
    options.JwtSecretKey = configuration["Jwt:SecretKey"]!;   // ≥32 位且 ≥8 种字符
    options.ValidTime = TimeSpan.FromMinutes(120);
});
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearerAuthentication(jwtBearerEventsAction: events =>
    {
        events.UseJwtBearerDefaultResponses();                 // 401/403 统一响应形态
    });
services.AddScoped<IBearerAuthService, JwtBearerAuthService>();
```

签名与配置节名以锁定版本为准（见本文件「包 API 以锁定版本为准」段）。

## 授权默认拒绝（FallbackPolicy 全局兜底）

「默认拒绝」不靠逐端点挂 `[Authorize]`——逐点标注属「默认放开、显式收紧」，新增端点忘标即静默匿名。用全局回退策略兜底：

```csharp
services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
```

- 匿名例外端点（登录、健康探针）显式 `[AllowAnonymous]`；整控制器均无鉴权需求（如 HealthController）标注上移到控制器类级，不逐动作重复挂。
- 认证方案就绪后 Swagger 的 `showJwtToken` 才传 `true`（见「Swagger 与静态文件装配」）。