---
rule_id: ui-agents
version: 1.64.0
last_updated: 2026-09-13
dependencies: [agents-root]
---

# UI 层规则

<!-- include: shared/ui/scope.md -->

<!-- include: shared/ui/common-tasks.md -->

---

## 技术栈

### UI
- .NET + Avalonia + Ursa.Avalonia + CommunityToolkit.Mvvm；Avalonia 大版本以仓库实际引用的 NuGet 包为准，规则不锚定大版本（仓库可能已高于规则编写时的基线），不主动升级，新项目先联网检索当前最新正式版
- 设计风格：Fluent Design
- 消息传递：`IMessenger`

### 版本敏感 API 查证
- Avalonia 大版本之间存在破坏性 API 变更（动画、剪贴板、阴影等写法在 11.x 与 12.x 已不同）：涉及动画（RenderTransform / 关键帧）、剪贴板（`IClipboard`）、阴影（`BoxShadow`）或平台集成等版本敏感 API 时，先按根 `AGENTS.md` 的查证优先级确认项目实际引用大版本的 API 形态（仓库既有同领域写法、NuGet 引用程序集、锁定版本源码或官方升级说明），禁止凭旧大版本经验直接书写，仍无法确认时询问用户

### 设计系统
<!-- include: shared/ui/design-system-liveness.md -->
- 使用 `design-system.yaml` 中定义的设计 token
- 样式必须使用 Avalonia 样式表（Styles / Classes），禁止把颜色、间距、圆角直接硬编码在控件属性中
- 颜色、间距、圆角必须来自样式资源，使用 `{StaticResource}` 或 `{DynamicResource}` 引用

<!-- include: shared/application/tech-choice.md -->

---

## 主动建议规则
- 发现窗口结构、页面布局、控件拆分、状态展示、样式资源、可访问性或交互一致性问题时，应主动提醒，并说明是否影响当前任务
- 发现设计稿、截图反馈或用户描述与 `design-system.yaml`、Avalonia 样式资源或 Fluent Design 约束冲突时，应先说明冲突点，再给出最小调整方案
- 涉及共享控件、全局样式、导航方案或跨 ViewModel 状态传递的建议改动，未经用户确认不得扩大到本次任务之外
- 发现可以复用已有视图、控件、样式资源或交互模式时，应优先建议复用
- 不确定交互规则、视觉规范或界面数据来源时，应按根 `AGENTS.md` 的查证优先级处理，禁止凭经验补写规则

---

## 推荐目录结构

- UI 目录建议聚焦在 `src/AppName/Views/`、`src/AppName/ViewModels/`、`src/AppName/Assets/Styles/` 下组织，优先复用现有结构，不强制迁移。

```text
src/AppName/
├── Views/
│   ├── Controls/             # 自定义控件
│   ├── Converters/           # 值转换器
│   ├── Dialogs/              # 对话框视图
│   └── Pages/                # 页面视图（按业务域拆分）
├── ViewModels/
│   ├── Base/                 # ViewModel 基类
│   ├── Dialogs/              # 对话框 ViewModel
│   └── Pages/                # 页面 ViewModel（按业务域拆分）
├── Assets/
│   ├── Styles/
│   │   ├── Fluent.axaml      # Fluent 主题
│   │   ├── Colors.axaml      # 颜色资源
│   │   ├── Fonts.axaml       # 字体资源
│   │   └── Global.axaml      # 全局样式
│   └── Images/               # 图片资源
└── App.axaml                 # 应用级资源
```

---

## 阶段 1 — 视图实现（视图实现角色主导）

**触发条件**：用户发出「开始视图开发」指令

**入场要求**：阶段 0 设计文档已由用户确认

**工作内容**：
1. 按设计文档实现页面和组件，遵循 `design-system.yaml` 和 Avalonia 样式规范。
2. 数据层使用 mock（静态 mock 数据），不依赖真实服务。
3. 同步输出接口契约文件 `src/AppName/Models/DTOs/`，定义所有数据传输对象。

**产物**：
- 可运行的 Avalonia 视图页面
- `src/AppName/Models/DTOs/` 契约类

**门控规则**：
- 用户确认视图页面符合设计文档预期。
- DTO 类中的类型已初步定稳；阶段 2 若发现契约缺口，按 `collaboration-AGENTS.md`「纠错、回退与重构」先回到契约变更流程。
- 满足以上两点后，才允许进入阶段 2。

---

## UI 规则

### 样式规则
- 所有样式使用 Avalonia Styles（`.axaml` 文件），禁止在控件中直接设置 `Background`、`Margin` 等样式属性。
- 所有颜色来自 `design-system.yaml` 中定义的语义化 token，通过样式资源引用。
- 所有间距使用统一的资源或样式类，禁止硬编码数值。
- 响应式设计遵循窗口大小变化，必须在不同窗口尺寸下测试。

### 组件规则
- 优先复用 `src/AppName/Views/Controls/` 下已有组件，禁止重复创建
<!-- include: shared/ui/component-rules-core.md -->
- 使用 Ursa.Avalonia 控件库优先，必要时使用 Avalonia 官方控件
- 图标统一使用 Avalonia 官方图形能力、Ursa 组件能力或仓库既有素材

<!-- include: shared/ui/mvvm-rules.md -->

### 导航规则
- 使用项目既有导航方案或 Ursa 导航能力进行页面切换。
- 导航逻辑封装在导航服务中，禁止在 ViewModel 中直接操作 View。
- 页面参数通过导航消息或显式参数对象传递，禁止使用静态全局状态。
- 需要历史记录时，应支持前进 / 后退。

### 对话框规则
- 使用项目既有对话框方案或 Ursa 的 Dialog / Overlay 组件实现对话框。
- 对话框内容必须使用 ViewModel，禁止在 Code-behind 编写业务逻辑。
- 对话框结果通过异步返回，禁止依赖隐式全局状态。
- 危险操作需提供明确的二次确认。

<!-- include: shared/ui/data-binding.md -->

<!-- include: shared/ui/state-management.md -->

---

## 测试规则

<!-- include: shared/ui/pre-commit-regression.md -->

<!-- include: shared/ui/general-requirements.md -->

<!-- include: shared/application/view-layer-testing.md -->
- 推荐使用 Avalonia Headless 或项目既有 UI 测试方案

<!-- include: shared/desktop/external-dependencies.md -->

<!-- include: shared/no-test-fallback.md -->

---
