---
rule_id: backend-agents
version: 1.12.0
last_updated: 2026-08-14
dependencies: [agents-root]
---

# 基础设施规则

## 适用范围

- 作用域：Repository、数据访问、依赖注入、平台服务、配置、构建发布与交付验证规则
- 触发场景：涉及 Repository、数据库、Riverpod 依赖注入、平台服务、配置、启动方式、打包发布时阅读

### 常见任务入口
- 改 Repository 或数据库结构：先看数据访问规则与迁移约束
- 改依赖注入、配置、连接串：先看配置规则与运行约束
- 改平台服务（相机、位置、传感器等）：先看平台服务规则
- 改构建、发布、打包流程：先看交付规则与验证要求
- 补基础设施回归：先看 `提交前最小回归`

---

## 技术栈

### 数据与配置
- 数据访问：sqflite（本地默认）/ REST API（远程服务）
- HTTP 客户端：`dio`（推荐，含拦截器）/ `http`
- 序列化：`json_serializable` / `freezed` / 手动 `fromJson` / `toJson`
- 依赖注入：Riverpod（Provider 注入）

### 平台服务
- `device_info_plus`：设备信息
- `connectivity_plus`：网络状态
- `path_provider`：文件系统路径
- `shared_preferences`：轻量存储
- `flutter_secure_storage`：安全存储
- `permission_handler`：权限管理

### 发布
- Android：AAB / APK 签名发布
- iOS：IPA 发布（需 macOS 构建环境）
- 版本、发布目录和运行命令必须可追踪

### 依赖注入规范（Riverpod）
- 使用 Riverpod Provider 配置依赖注入
- Provider 定义集中在 `lib/providers/` 目录
- Provider 生命周期：
  - `autoDispose`：页面离开后自动销毁（默认推荐）
  - 全局 Provider：不加 `autoDispose`，应用生命周期内保持
  - `family`：带参数的 Provider
- 服务注册通过 Provider 声明，不需要集中注册文件

```dart
// 推荐的 Provider 定义方式
final itemRepositoryProvider = Provider<ItemRepository>((ref) {
  return ItemRepositoryImpl(database: ref.watch(databaseProvider));
});

final itemListProvider = AsyncNotifierProvider.autoDispose<ItemListNotifier, List<Item>>(() {
  return ItemListNotifier();
});
```

如果仓库已经有真实实现，以现有代码为准，不要强行重构或替换技术栈。
技术债务与重构判断遵循根 `AGENTS.md` 的全局规则。

---

## 主动建议规则
- 发现业务逻辑放错层、Repository 与 Service 职责混杂、数据访问不一致或异常处理不一致时，应主动提醒
- 发现数据模型、存储结构、配置策略或平台服务可能影响历史数据、兼容性或权限安全时，必须先说明风险，不得直接扩大修改
- 发现可以复用既有 Repository、数据访问封装、DTO、校验器或错误处理封装时，应优先建议复用
- 不确定业务规则、权限规则、数据含义或外部接口行为时，应按根 `AGENTS.md` 的查证优先级处理，禁止按通用经验补写规则

---

## 推荐目录结构

- 基础设施目录建议聚焦在 `lib/repositories/`、`lib/services/`、`lib/config/` 下组织，优先复用现有结构，不强制迁移。

```text
lib/
├── repositories/
│   ├── interfaces/            # 仓储接口定义（abstract class）
│   └── [module]/
│       └── xxx_repository.dart
├── services/
│   └── platform/              # 平台服务抽象与实现
├── config/
│   ├── theme.dart             # 主题配置
│   ├── routes.dart            # 路由配置
│   ├── constants.dart         # 常量
│   └── env.dart               # 环境配置
├── utils/                     # 工具函数
│   ├── result.dart            # 统一结果包装
│   └── exceptions.dart        # 异常定义
├── main.dart                  # 应用入口 + ProviderScope
└── app.dart                   # MaterialApp + GoRouter
```

---

## 基础设施规则

### Repository 层规则
- Repository 类应定义清晰的公共 API（abstract class）
- Repository 实现负责具体数据访问，不把 SQL、连接串或事务细节泄漏到 Provider / Service
- Repository 方法必须优先采用异步形式（返回 `Future<T>`）
- 涉及事务时，使用 sqflite 的 `transaction` 方法统一管理
- Repository 通过 Riverpod Provider 注入

### 数据访问规则
- 本地数据访问使用 sqflite
- 远程数据访问通过 `dio` 或 `http` 调用 REST API
- SQL 查询必须使用参数化查询，禁止字符串拼接构造可注入 SQL
- 数据库连接通过 `openDatabase` 管理，移动端注意及时释放资源
- 数据库存储字段建议使用 `snake_case`，Dart 属性使用 `camelCase`，映射通过 `fromJson` / `toJson` 处理
- 网络请求必须有超时控制、错误处理和重试策略
- 移动端网络请求需考虑离线场景，关键操作应支持本地缓存或队列

### 数据库迁移规则
- 涉及数据库结构变更时，必须同步补齐迁移脚本或等价初始化逻辑
- 迁移脚本应保持可追踪、可重复执行，并尽量说明适用版本
- 若支持回滚，回滚方式应与正向变更一起说明
- 不能确认迁移影响范围时，先说明风险，不要直接执行破坏性变更
- sqflite 迁移通过 `onCreate` / `onUpgrade` 回调管理

### 配置与环境规则
- 运行路径、数据库文件路径、API 地址、环境依赖必须通过配置管理，禁止散落在代码常量中
- 敏感信息（API 密钥、令牌等）使用 `flutter_secure_storage` 存储，禁止明文保存到 `shared_preferences` 或配置文件
- 本地默认配置应尽量可直接运行，同时避免把密钥、真实凭据写入仓库
- 新增配置项、命令或环境依赖时，必须同步更新相关文档
- API 基地址等环境相关配置统一在 `lib/config/env.dart` 中管理
- 环境变量通过 `--dart-define` 在构建时注入

### 平台服务规则
- 平台特定功能（相机、位置、传感器、生物识别、推送通知等）必须通过接口抽象
- 接口定义在共享项目中（`lib/services/platform/`），实现使用对应平台插件
- 平台特定代码使用 Flutter 插件（如 `image_picker`、`geolocator`），不直接编写 Platform Channel（除非插件不满足需求）
- 权限请求必须在用户触发相关操作时才发起，禁止启动时批量请求所有权限
- 平台服务变更时必须同步测试所有受影响平台

---

## 发布规则

### 总体要求
- 优先复用现有打包配置
- Android 发布优先使用 AAB 格式，上传 Google Play / 国内应用商店
- iOS 发布需在 macOS 环境下构建，使用 IPA 格式
- 不要把密钥、签名证书或发布凭据写入仓库
- 发布链路要可追踪
- 清理旧版本时要保留最小回滚窗口

### Android 发布规则
- 签名密钥通过安全渠道管理，禁止提交到仓库
- `AndroidManifest.xml` 中的权限声明必须与实际使用一致，不多申请无用权限
- Target SDK Version 和 Minimum SDK Version 需与目标市场要求匹配
- ProGuard / R8 混淆规则如启用，需确认反射和序列化类不被错误混淆

### iOS 发布规则
- Info.plist 中的 `NS*UsageDescription` 必须为每个使用的权限提供用户可见说明
- 最低支持版本需与目标市场要求匹配
- App Store 审核相关配置（隐私政策 URL、应用类别等）需在发布前确认

### 发布交付规则
- 只要本次任务涉及发布配置、版本号、运行命令或环境依赖，完成实现后应主动执行一次构建验证
- 发布验证至少包括：
  - `flutter build apk` / `flutter build ios` 是否成功
  - 应用是否成功启动
  - 关键功能是否处于可用状态
  - 核心访问链路是否可用
- 若因环境限制无法执行，必须明确说明

### 代码组织规范
- 一个文件只放一个主对象：Repository、数据访问实现、平台服务封装、配置类各自独立文件，文件名与主对象一致。
- 数据契约（Entity / DTO）按业务域分文件定义，不合并到一个 `models.dart` / 大杂烩文件。
- 触发拆分的信号：单文件出现多个主类、字段持续堆叠、模块跨多个不相关业务。
- 允许例外：紧耦合的常量、抽象基类分组、测试 fixture。
- 反模式：一个文件堆放所有 Repository 或所有数据模型；把多个不相关平台服务塞进同一文件。

---

## 测试与验证规则

### 仓储与数据验证
- Repository 层 SQL 查询、数据映射、事务处理发生变化时，应补充对应测试或最小化验证步骤
- 涉及数据库初始化、迁移、升级时，应至少验证一次从空环境到可运行状态的链路
- 不要让测试或验证依赖手工准备的本地状态

### 无法执行验证时
- 必须说明未执行的验证类型
- 必须说明未执行原因
- 必须说明潜在影响范围和风险

### 提交前最小回归
- 修改构建配置、启动命令、打包脚本或平台清单时：至少执行一次 `flutter build` 与启动验证
- 修改环境变量、配置项或连接串时：确认示例配置、文档和运行方式同步更新
- 修改数据库、Repository、依赖注入或外部服务集成时：至少补一项真实链路或等价验证
- 修改平台特定代码时：在受影响平台执行验证
- 若本次改动同时影响应用访问链路，应至少补一次核心链路检查

---
