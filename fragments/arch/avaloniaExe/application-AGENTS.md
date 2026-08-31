---
rule_id: application-agents
version: 1.22.0
last_updated: 2026-08-31
dependencies: [agents-root]
---

# 应用层规则

<!-- include: shared/application/scope.md -->

<!-- include: shared/application/common-tasks.md -->

---

## 技术栈

<!-- include: shared/application/layered-arch.md -->

### 核心库
- `CommunityToolkit.Mvvm`：`[ObservableProperty]`、`[RelayCommand]`、`IMessenger`
- `Azrng.Core`：实体基类、扩展方法、工具类、结果包装、异常体系
- `Azrng.Core.Json`：基于 `System.Text.Json` 的 JSON 序列化

<!-- include: shared/application/tech-choice.md -->

---

<!-- include: shared/application/advisory.md -->

---

## 推荐目录结构

- 应用层目录建议聚焦在 `src/AppName/ViewModels/`、`src/AppName/Services/`、`src/AppName/Models/` 下组织，优先复用现有结构，不强制迁移。

```text
src/AppName/
├── ViewModels/
│   ├── Base/
│   ├── Dialogs/
│   └── Pages/
├── Services/
│   ├── Interfaces/           # 服务接口定义
│   └── Implementations/      # 服务实现
└── Models/
    ├── Entities/             # 数据实体
    └── DTOs/                 # 数据传输对象
```

### 目录职责映射

按职责将类放到对应目录，文件命名与类名保持一致；View（`.axaml`）的组织见 `ui-AGENTS.md`。

| 目录 | 放什么类 | 命名约定 |
| --- | --- | --- |
| `ViewModels/Base/` | ViewModel 基类 | `*ViewModelBase.cs` |
| `ViewModels/Pages/`、`ViewModels/Dialogs/` | 页面 / 对话框 ViewModel（状态 + 命令编排） | `{名称}ViewModel.cs` |
| `Services/Interfaces/` | 服务接口 | `I{业务域}Service.cs` |
| `Services/Implementations/` | 服务实现 | `{业务域}Service.cs` |
| `Models/Entities/` | 数据实体，一类一文件 | `{实体名}.cs` |
| `Models/DTOs/` | DTO（视图与业务层契约），一类一文件 | `{名称}Dto.cs` |

---

<!-- include: shared/application/stage2-impl.md -->

---

## 应用层规则

<!-- include: shared/application/layering-rules.md -->

<!-- include: shared/application/viewmodel-rules.md -->

### Service 层规则
- 服务类必须实现接口，接口定义在 `Services/Interfaces/` 目录。
- 服务方法必须优先采用异步形式（返回 `Task<T>` 或 `ValueTask<T>`）。
- 服务层处理所有业务逻辑，不直接访问 Repository 以外的数据依赖。
- 服务层异常必须统一封装，抛出 Azrng 异常体系中的业务异常类型。
- 服务类实现 `ITransientDependency` / `IScopedDependency` / `ISingletonDependency` 接口，通过 `RegisterBusinessServices` 批量注册。

<!-- include: shared/application/dto-rules.md -->

<!-- include: shared/application/code-organization.md -->

<!-- include: shared/application/kiss.md -->

<!-- include: shared/application/result-wrapping.md -->

### 异常处理规范
- 业务异常继承 `BaseException` 或其子类：
  - `LogicBusinessException`：业务逻辑异常
  - `ParameterException`：参数校验异常
  - `NotFoundException`：资源不存在
  - `ForbiddenException`：禁止访问
  - `InternalServerException`：服务器内部错误
- 禁止抛出非 Azrng 体系的随意自定义异常。
- 异常处理应尽量保留可定位信息，同时对用户输出友好、可理解的提示。

<!-- include: shared/application/comment-rules.md -->

<!-- include: shared/application/anti-patterns.md -->

<!-- include: shared/application/maintainability-check.md -->

---

## 测试规则

<!-- include: shared/application/pre-commit-regression.md -->

<!-- include: shared/common/general-requirements.md -->

<!-- include: shared/application/application-layer-testing.md -->

<!-- include: shared/desktop/external-dependencies.md -->

<!-- include: shared/no-test-fallback.md -->

---
