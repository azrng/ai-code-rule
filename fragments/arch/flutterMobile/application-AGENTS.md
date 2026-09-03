---
rule_id: application-agents
version: 1.45.0
last_updated: 2026-09-03
dependencies: [agents-root]
---

# 应用层规则

## 适用范围

- 作用域：Riverpod Provider、Service、命令、业务编排、结果包装与应用层测试
- 触发场景：涉及业务流程、状态管理、Service 逻辑、应用服务或应用层测试时阅读

### 常见任务入口
- 改命令流程或状态切换：先看 Provider 规则与统一结果包装
- 改 Service 或业务规则：先看分层边界规则与异常处理规范
- 改 DTO、映射、结果包装：先看 DTO 与模型规则与统一结果包装
- 补应用层回归：先看 `提交前最小回归` 与测试规则

---

## 技术栈

### 分层架构
- `Page → Provider → Service → Repository → Model`
- Page 负责展示，Provider 负责状态与命令编排，Service 负责业务逻辑，Repository 负责数据访问边界，Model 负责领域数据表达

### 核心库
- `flutter_riverpod`：状态管理（`StateNotifier`、`AsyncNotifier`、`FutureProvider`、`StreamProvider`）
- `drift`：本地数据访问与实体契约（表定义 + build_runner 生成数据类）
- `go_router`：路由
- `freezed` / `json_serializable` / `dio`：仅在项目确有需要时引入，不作为默认推荐

<!-- include: shared/application/tech-choice.md -->

---

## 主动建议规则
- 发现业务逻辑放错层、Provider 与 Service 职责混杂、状态管理混乱或异常处理不一致时，应主动提醒
- 发现 Provider 状态流转、Service 逻辑、数据模型可能影响历史数据、兼容性或权限安全时，必须先说明风险，不得直接扩大修改
- 发现可以复用既有 Service、Provider、DTO、校验器或错误处理封装时，应优先建议复用
- 不确定业务规则、权限规则、数据含义或外部接口行为时，应按根 `AGENTS.md` 的查证优先级处理，禁止按通用经验补写规则

---

## 推荐目录结构

- 应用层目录建议聚焦在 `lib/providers/`（状态编排）与 `lib/data/`（服务、模型、仓储）下组织，优先复用现有结构，不强制迁移。

```text
lib/
├── providers/                 # Riverpod：DI、状态、派生数据、命令编排
│   └── xxx_provider.dart
├── data/
│   ├── services/              # 业务服务（Dart 无强制接口，建议 abstract class）
│   ├── models/                # 视图 / 展示 DTO
│   └── repositories/          # 仓储抽象与实现
└── core/utils/
    └── result.dart            # 统一结果包装
```

---

### 阶段 2 — 业务逻辑实现（业务实现角色主导）

**触发条件**：用户发出「开始业务逻辑开发」指令

**入场要求**：前端视图已完成，实体契约（drift 表定义）与视图模型（`lib/data/models/`）已明确

**工作内容**：
1. 严格按照 Model 中的类型定义实现 Provider 状态编排与 Service 层逻辑。
2. 遵循 `Provider → Service → Repository → Model` 完整分层。
3. 涉及数据访问或数据库结构变更时，结合 `backend-AGENTS.md` 同步补齐仓储实现与迁移脚本。
4. 每个关键服务方法和关键 Provider 状态变更都应能通过测试独立验证。

**字段命名约定**：
- Dart 类型 / 属性：camelCase
- JSON 序列化字段：snake_case（与 API / 数据库保持一致）
- 数据库存储字段和映射规则见 `backend-AGENTS.md`

**门控规则**：
- 核心业务逻辑测试通过后，才允许进入阶段 3。

---

## 应用层规则

### 分层边界规则
- Page 只负责展示，不承载业务逻辑
- Provider 负责状态管理、命令触发、调用 Service 与处理用户可见结果
- Service 负责业务逻辑、流程编排和规则校验，不直接操作 Widget
- Repository 是数据访问边界，具体实现细则见 `backend-AGENTS.md`
- Model / DTO 负责承载业务数据，不在其中夹带 UI 行为

### Provider 规则（Riverpod）
- Provider 中可以组织用户操作流程，但禁止直接写 SQL 或直接依赖存储细节
- 命令执行后的成功、失败、空状态必须显式反馈到界面状态（`AsyncValue.data` / `AsyncValue.error` / `AsyncValue.loading`）
- 异常在 Provider 层转换为用户友好的提示信息，不把底层异常原样暴露给用户
- 跨 Provider 协作优先使用 Riverpod 的 Provider 依赖（`ref.watch` / `ref.read`），不依赖静态全局状态
- 同一个编辑页的数据初始化只保留一条主链路

### Provider 类型选择
| 场景 | 推荐类型 | 示例 |
|------|---------|------|
| 简单状态 | `StateProvider` | 开关、选中项、当前页码 |
| 复杂状态逻辑 | `StateNotifierProvider` | 表单状态、列表管理 |
| 异步数据 | `FutureProvider` / `AsyncNotifierProvider` | API 请求、数据库查询 |
| 流式数据 | `StreamProvider` | 实时数据、WebSocket |
| 计算派生 | `Provider`（.autoDispose） | 基于其他 Provider 的计算值 |

### Service 层规则
- Service 类应定义清晰的公共 API（Dart 无强制接口，但建议定义 abstract class）
- Service 方法必须优先采用异步形式（返回 `Future<T>`）
- Service 层处理所有业务逻辑，不直接访问 Repository 以外的数据依赖
- Service 层异常必须统一封装，抛出项目异常体系中的业务异常类型
- Service 类通过 Riverpod Provider 注入，不直接实例化

### DTO 与模型规则
- DTO 是视图层与业务逻辑层之间的稳定契约，变更时必须同步更新相关映射和调用方
- 若仓库已有真实实体或 DTO 结构，优先沿用现状，不为模板强行改名或重组
- 数据转换规则应集中放在 Service 或明确的映射层，不散落在 Page 或 Repository 调用点
- 实体契约使用 drift 生成数据类；视图 / 展示 DTO 手写并按需实现 `copyWith`、`==`、`hashCode`

### 统一结果包装
- Service 层方法返回值统一使用项目既有的结果包装类型
- 成功响应：`Result.success(data)`
- 错误响应：`Result.failure(message, errorCode)`
- 对于仅表示操作结果的方法，也应保持统一的结果语义，不返回随意结构

```dart
// 推荐实现
sealed class Result<T> {
  const Result();
}

class Success<T> extends Result<T> {
  final T data;
  const Success(this.data);
}

class Failure<T> extends Result<T> {
  final String message;
  final String? errorCode;
  const Failure(this.message, {this.errorCode});
}
```

<!-- include: shared/application/exception-handling.md -->

### 移动端应用生命周期
- 应用生命周期事件（`AppLifecycleState`）中只做必要的初始化和状态保存
- 后台返回前台时，应根据业务需要刷新关键数据
- 网络状态变化时，应通过 `connectivity_plus` 监听并给出适当提示
- 权限请求（相机、位置、存储等）必须在真正需要时才请求，禁止启动时一次性全部请求

### 代码组织规范
- 一个文件只放一个主对象：Provider、Service、Repository、Entity、DTO 各自独立文件，文件名与主对象一致。
- DTO / Model 组织：实体契约由 drift 表定义与生成类承载（`lib/data/database/`）；视图 DTO 放 `lib/data/models/`，按业务域分文件。
- 触发拆分的信号：职责混杂、同文件出现多个主类、字段持续堆叠、跨多个不相关业务。
- 允许例外：仅服务当前文件的私有辅助类型、freezed 生成的联合类型成员、测试 fixture。
- 反模式：一个 `models.dart` 堆放所有实体 / DTO；把多个不相关 Provider 或 Service 塞进同一文件。
- 如果编写注释（含 dartdoc、行内注释等），统一使用中文，不使用纯英文注释；必要的英文标识、协议字段名或框架关键字可保留原文；不在注释里泄露密钥、token、连接串或真实生产地址。

---

## 测试规则

### 提交前最小回归
- 默认执行：`flutter test`
- Provider、命令、Service 改动：至少验证一项业务流程、状态流转或错误分支
- DTO、映射、结果包装改动：至少验证输入输出结构和边界条件
- 与数据访问、集成或配置相关的改动：至少补一项联调或等价验证，证明真实链路生效

<!-- include: shared/common/general-requirements.md -->

### 应用层测试
- Provider 状态变更、命令执行、消息发送发生变化时，应补充对应测试
- Service 层业务逻辑、数据转换、异常处理发生变化时，应补充对应测试
- 推荐使用 `flutter_test` + `mocktail` / `mockito`

<!-- include: shared/desktop/external-dependencies.md -->

<!-- include: shared/no-test-fallback.md -->

---
