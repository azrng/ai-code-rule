---
rule_id: infrastructure-agents
version: 1.25.0
last_updated: 2026-08-31
dependencies: [agents-root]
---

# 基础设施规则

## 适用范围

- 作用域：仓储、数据访问、依赖注入、平台服务、配置、构建发布与交付验证规则
- 触发场景：涉及 Repository、数据库、依赖注入、平台服务、配置、启动方式、打包发布时阅读

<!-- include: shared/backend/common-tasks-infra.md -->

---

## 技术栈

### 数据与配置
- 数据访问：SQLite（本地默认）/ EF Core（如项目已使用则沿用）/ REST API（远程服务）
- 数据库：SQLite（移动端本地默认）/ 远程 API（后端服务）
- 依赖注入：`Microsoft.Extensions.DependencyInjection`（MAUI 内置）
- HTTP 客户端：`HttpClient` / `Refit`（如项目已使用则沿用）
- 序列化：`System.Text.Json`

### 平台服务
- `Microsoft.Maui.Devices`：设备信息、传感器、电池、网络
- `Microsoft.Maui.ApplicationModel`：权限、launcher、share、App Actions
- `Microsoft.Maui.Storage`：Preferences、SecureStorage、FileSystem
- `CommunityToolkit.Maui`：平台扩展能力

### 依赖注入规范
- 使用 MAUI 内置 `MauiAppBuilder` 配置 DI 容器
- 服务注册统一在 `MauiProgram.cs` 中完成
- 若项目使用标记接口模式批量注册（如 `ITransientDependency`），沿用现有注册方式
- 服务生命周期：
  - `Transient`：轻量级、无状态服务
  - `Scoped`：需与页面生命周期绑定、或需共享同一实例的服务
  - `Singleton`：全局唯一服务（如配置、日志、HTTP 客户端）

<!-- include: shared/application/tech-choice.md -->

---

## 主动建议规则
- 发现 Repository、数据访问、依赖注入、配置、迁移脚本、构建脚本、发布链路或平台服务存在不一致、不可复现或文档缺失时，应主动提醒
<!-- include: shared/infrastructure/advisory-core.md -->

---

## 推荐目录结构

- 基础设施目录建议聚焦在 `src/AppName/Repositories/`、`src/AppName/Data/`、`src/AppName/Platforms/`、`src/AppName/MauiProgram.cs` 附近组织，优先复用现有结构，不强制迁移。

```text
src/AppName/
├── Repositories/
│   ├── Interfaces/            # 仓储接口定义
│   └── Implementations/       # 仓储实现
├── Data/
│   ├── Context/               # 数据库上下文 / 连接工厂
│   └── Migrations/            # 数据库迁移脚本
├── Platforms/
│   ├── Android/               # Android 平台特定实现
│   ├── iOS/                   # iOS 平台特定实现
│   ├── Windows/               # Windows 平台特定实现（按需）
│   └── MacCatalyst/           # macOS 平台特定实现（按需）
├── Services/
│   └── Platform/              # 平台服务抽象与实现
├── Helpers/                   # 基础设施工具类
└── MauiProgram.cs             # 依赖注入与启动配置
```

---

## 基础设施规则

### Repository 层规则
- 仓储类必须实现接口，接口定义在 `Repositories/Interfaces/` 目录
- 仓储实现负责具体数据访问，不把 SQL、连接串或事务细节泄漏到 ViewModel / Service
- 仓储方法必须优先采用异步形式，并支持取消令牌
- 涉及事务时，使用 `IDbTransaction` 或等价机制统一管理
- 若项目使用标记接口模式，仓储类应实现对应接口以自动注册

### 数据访问规则
- 本地数据访问使用 SQLite + Dapper 或 EF Core（以项目现有方案为准）
- 远程数据访问通过 `HttpClient` 或 `Refit` 调用 REST API
- SQL 查询必须使用参数化查询，禁止字符串拼接构造可注入 SQL
- 数据库连接字符串从配置文件读取，禁止硬编码
- 数据库连接必须正确管理生命周期，移动端注意及时释放资源
- 数据库存储字段建议使用 `snake_case`，映射通过 `Column` 特性或显式映射处理
- 网络请求必须有超时控制、错误处理和重试策略
- 移动端网络请求需考虑离线场景，关键操作应支持本地缓存或队列

<!-- include: shared/desktop/db-migration.md -->

### 配置与环境规则
- 运行路径、数据库文件路径、API 地址、环境依赖必须通过配置管理，禁止散落在代码常量中
- 敏感信息（API 密钥、令牌等）使用 `SecureStorage` 存储，禁止明文保存到 `Preferences` 或配置文件
- 本地默认配置应尽量可直接运行，同时避免把密钥、真实凭据写入仓库
- 新增配置项、命令或环境依赖时，必须同步更新相关文档
- API 基地址等环境相关配置统一在 `MauiProgram.cs` 或 `appsettings.json` 中管理

### 平台服务规则
- 平台特定功能（相机、位置、传感器、生物识别、推送通知等）必须通过接口抽象
- 接口定义在共享项目中（`Services/Platform/Interfaces/`），实现在对应 `Platforms/` 目录
- 使用 `#if ANDROID` / `#if IOS` 编译指令仅限于 `Platforms/` 目录下的文件，禁止在共享代码中使用平台编译指令
- 权限请求必须在用户触发相关操作时才发起，禁止启动时批量请求所有权限
- 平台服务变更时必须同步测试所有受影响平台

---

## 发布规则

<!-- include: shared/mobile/release.md -->

<!-- include: shared/mobile/android-release.md -->

<!-- include: shared/mobile/ios-release.md -->

<!-- include: shared/desktop/release-delivery.md -->

---

## 测试与验证规则

<!-- include: shared/desktop/repository-regression.md -->

<!-- include: shared/no-test-fallback.md -->

### 提交前最小回归
- 修改构建配置、启动命令、打包脚本或平台清单时：至少执行一次构建与启动验证
<!-- include: shared/infrastructure/pre-commit-core.md -->
- 修改平台特定代码时：在受影响平台执行验证

---
