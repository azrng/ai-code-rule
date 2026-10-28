---
rule_id: application-agents
version: 1.22.0
last_updated: 2026-08-31
dependencies: [agents-root]
---

# 应用规则

<!-- include: shared/application/scope.md -->

---

## 技术栈

<!-- include: shared/application/layered-arch.md -->

<!-- include: shared/application/stack-mvvm.md -->
- `Syncfusion.Maui.Toolkit`：UI 控件库（图表、下拉刷新、卡片等）
- `CommunityToolkit.Maui`：移动端扩展能力（弹窗、转换器、动画等）
- `Azrng.Core`（如项目已使用则沿用）

<!-- include: shared/application/advisory.md -->

---

## 推荐目录结构

- 应用层目录建议聚焦在 `src/AppName/ViewModels/`、`src/AppName/Services/`、`src/AppName/Models/` 下组织，优先复用现有结构，不强制迁移。

```text
src/AppName/
├── ViewModels/
│   ├── Base/
│   ├── Dialogs/
│   └── [Module]/
├── Services/
│   ├── Interfaces/            # 服务接口定义
│   └── Implementations/       # 服务实现
└── Models/
    ├── Entities/              # 数据实体
    └── DTOs/                  # 数据传输对象
```

### 目录职责映射

按职责将类放到对应目录，文件命名与类名保持一致；View（`.xaml`）的组织见 `ui-AGENTS.md`。

| 目录 | 放什么类 | 命名约定 |
| --- | --- | --- |
| `ViewModels/Base/` | ViewModel 基类 | `*ViewModelBase.cs` |
| `ViewModels/[Module]/`、`ViewModels/Dialogs/` | 模块 / 对话框 ViewModel（状态 + 命令编排） | `{名称}ViewModel.cs` |
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

<!-- include: shared/application/service-layer-dotnet.md -->

<!-- include: shared/application/dto-rules.md -->

<!-- include: shared/application/code-organization.md -->

<!-- include: shared/application/kiss.md -->

<!-- include: shared/application/result-wrapping.md -->

<!-- include: shared/application/exception-handling.md -->

### 移动端应用生命周期
- 应用生命周期事件（`OnStart`、`OnResume`、`OnSleep` / `OnStop`）中只做必要的初始化和状态保存
- 后台返回前台时，应根据业务需要刷新关键数据
- 网络状态变化时，应通过 `Connectivity.Current` 监听并给出适当提示
- 权限请求（相机、位置、存储等）必须在真正需要时才请求，禁止启动时一次性全部请求

<!-- include: shared/application/comment-rules.md -->

<!-- include: shared/application/anti-patterns.md -->

<!-- include: shared/application/maintainability-check.md -->

---

## 测试规则

<!-- include: shared/application/pre-commit-regression.md -->

<!-- include: shared/common/general-requirements.md -->

<!-- include: shared/application/application-layer-testing.md -->

<!-- include: shared/backend/dotnet-build-isolation.md -->

<!-- include: shared/desktop/external-dependencies.md -->

<!-- include: shared/no-test-fallback.md -->

---
