---
rule_id: backend-agents
version: 1.26.0
last_updated: 2026-08-31
dependencies: [agents-root]
---

# 基础设施规则

## 适用范围

- 作用域：Repository、数据访问、依赖注入、平台服务、配置、构建发布与交付验证规则
- 触发场景：涉及 Repository、数据库、Riverpod 依赖注入、平台服务、配置、启动方式、打包发布时阅读

<!-- include: shared/backend/common-tasks-infra.md -->

---

## 技术栈

### 数据与配置
- 数据访问：drift（本地 SQLite，响应式查询；表定义 + build_runner 生成实体类）
- 序列化：drift 生成数据类为主，对外 JSON 等格式用手动映射；`freezed` / `json_serializable` 仅在确有需要时引入
- 网络（可选，仅需要远程服务的项目）：`dio`（推荐，含拦截器）/ `http`，必须有超时控制、错误处理和重试策略
- 依赖注入：Riverpod（Provider 注入）

### 平台服务
- `device_info_plus`：设备信息
- `connectivity_plus`：网络状态
- `path_provider`：文件系统路径
- `shared_preferences`：轻量存储
- `flutter_secure_storage`：安全存储
- `permission_handler`：权限管理

### 依赖注入规范（Riverpod）
- 使用 Riverpod Provider 配置依赖注入
- Provider 定义集中在 `lib/providers/` 目录
- Provider 生命周期：
  - `autoDispose`：页面离开后自动销毁（默认推荐）
  - 全局 Provider：不加 `autoDispose`，应用生命周期内保持
  - `family`：带参数的 Provider
- 服务注册通过 Provider 声明，不需要集中注册文件

<!-- include: shared/backend/advisory.md -->
---

## 推荐目录结构

- 基础设施目录建议按 `lib/data/`（数据访问）与 `lib/core/`（主题、常量、工具）组织，优先复用现有结构，不强制迁移。

```text
lib/
├── data/
│   ├── database/              # drift 表定义（tables.dart）与生成的实体类
│   ├── models/                # 视图 / 展示 DTO
│   ├── repositories/          # 仓储（abstract class + drift 实现）
│   └── services/              # 业务服务（含平台服务封装）
├── core/
│   ├── theme/                 # 设计 token 落地
│   ├── constants/             # 预置数据、阈值、常量
│   └── utils/                 # 工具函数（Result 包装、异常定义、格式化）
├── providers/                 # Riverpod DI、状态与命令编排
├── router/                    # GoRouter 路由定义
├── main.dart                  # 应用入口 + 目录装配 + ProviderScope
└── app.dart                   # MaterialApp.router + 主题
```

---

## 基础设施规则

### Repository 层规则
- Repository 类应定义清晰的公共 API（abstract class）
- Repository 实现负责具体数据访问，不把 SQL、连接串或事务细节泄漏到 Provider / Service
- Repository 方法必须优先采用异步形式（返回 `Future<T>`）
- 涉及事务时，使用 drift 的 `transaction` 方法统一管理
- Repository 通过 Riverpod Provider 注入

### 数据访问规则
- 本地数据访问使用 drift，查询优先使用 drift DSL / 参数化变量，禁止字符串拼接构造可注入 SQL
- 数据库连接通过 `NativeDatabase` / `LazyDatabase` 管理并集中封装，移动端注意及时释放资源
- 数据库存储字段建议使用 `snake_case`，Dart 属性使用 `camelCase`，对外 JSON 通过显式映射函数处理
- 需要远程服务的项目：网络请求必须有超时控制、错误处理和重试策略，关键操作考虑离线缓存或队列

### 数据库迁移规则
- 涉及数据库结构变更时，必须同步补齐迁移脚本或等价初始化逻辑
- 迁移脚本应保持可追踪、可重复执行，并尽量说明适用版本
- 若支持回滚，回滚方式应与正向变更一起说明
- 不能确认迁移影响范围时，先说明风险，不要直接执行破坏性变更
- drift 迁移通过 `schemaVersion` 与 `MigrationStrategy`（`onCreate` / `onUpgrade`）管理

### 配置与环境规则
- 运行路径、数据库文件路径、API 地址、环境依赖必须通过配置管理，禁止散落在代码常量中
- 敏感信息（API 密钥、令牌等）使用 `flutter_secure_storage` 存储，禁止明文保存到 `shared_preferences` 或配置文件
- 本地默认配置应尽量可直接运行，同时避免把密钥、真实凭据写入仓库
- 新增配置项、命令或环境依赖时，必须同步更新相关文档
- 需要环境配置的项目：API 基地址等统一集中管理，环境变量通过 `--dart-define` 在构建时注入
- 纯本地应用将数据库、图片、备份等目录在启动期统一装配后注入（Provider override），不散落硬编码路径

### 平台服务规则
- 平台特定功能（相机、位置、传感器、生物识别、推送通知等）必须通过接口抽象
- 接口定义在 `lib/data/services/`，实现使用对应平台插件
- 平台特定代码使用 Flutter 插件（如 `image_picker`、`geolocator`），不直接编写 Platform Channel（除非插件不满足需求）
- 权限请求必须在用户触发相关操作时才发起，禁止启动时批量请求所有权限
- 平台服务变更时必须同步测试所有受影响平台

---

## 发布规则

<!-- include: shared/mobile/release.md -->

<!-- include: shared/mobile/android-release.md -->

<!-- include: shared/mobile/ios-release.md -->

### 发布交付规则
- 只要本次任务涉及发布配置、版本号、运行命令或环境依赖，完成实现后应主动执行一次构建验证
- 发布验证至少包括：
  - `flutter build apk` / `flutter build ios` 是否成功
<!-- include: shared/desktop/release-delivery-core.md -->

### 代码组织规范
- 一个文件只放一个主对象：Repository、数据访问实现、平台服务封装、配置类各自独立文件，文件名与主对象一致。
- 数据契约（Entity / DTO）按业务域分文件定义，不合并到一个 `models.dart` / 大杂烩文件。
- 触发拆分的信号：单文件出现多个主类、字段持续堆叠、模块跨多个不相关业务。
- 允许例外：紧耦合的常量、抽象基类分组、测试 fixture。
- 反模式：一个文件堆放所有 Repository 或所有数据模型；把多个不相关平台服务塞进同一文件。

---

## 测试与验证规则

<!-- include: shared/desktop/repository-regression.md -->

<!-- include: shared/no-test-fallback.md -->

### 提交前最小回归
- 修改构建配置、启动命令、打包脚本或平台清单时：至少执行一次 `flutter build` 与启动验证
- 修改环境变量、配置项或连接串时：确认示例配置、文档和运行方式同步更新
- 修改数据库、Repository、依赖注入或外部服务集成时：至少补一项真实链路或等价验证
- 修改平台特定代码时：在受影响平台执行验证
- 若本次改动同时影响应用访问链路，应至少补一次核心链路检查

---
