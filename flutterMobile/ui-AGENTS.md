---
rule_id: ui-agents
version: 1.12.0
last_updated: 2026-08-14
dependencies: [agents-root]
---

# 界面规则

## 适用范围

- 作用域：页面、布局、导航、样式、交互、动效、移动端适配、界面测试
- 触发场景：涉及 Widget、GoRouter 导航、布局、交互、样式、动效、移动端适配、UI smoke test 时阅读

### 常见任务入口
- 新增页面或布局：先看界面结构与 Widget 规则
- 改表单、弹窗、导航、交互：先看状态管理与事件处理规则
- 改样式、主题、视觉展示：先看样式规则与设计系统约束
- 改动效、过渡、动画：先看动效规则与 animation 规范
- 改移动端适配、手势、平台特定 UI：先看移动端适配规则
- 补界面回归：先看 `提交前最小回归` 与测试规则

---

## 技术栈

### UI
- Flutter + Dart
- Material 3（Flutter 内置 UI 组件库）
- Riverpod（状态管理）
- GoRouter（路由管理）

### 设计系统
- 使用 `design-system.yaml` 中定义的设计 token
- 样式必须使用 Flutter ThemeData + Material 3 Token，禁止硬编码颜色、间距、圆角
- 颜色通过 `Theme.of(context).colorScheme` 获取
- 主题定制统一通过 `ThemeData` + `ColorScheme` 配置

如果仓库已经有真实实现，以现有代码为准，不要强行重构或替换技术栈。
技术债务与重构判断遵循根 `AGENTS.md` 的全局规则。

---

## 主动建议规则
- 发现页面、组件、状态管理或请求逻辑职责混杂，导致文件膨胀、复用困难或测试困难时，应主动提醒并建议最小拆分点
- 发现样式与 `design-system.yaml`、现有主题或组件库冲突时，应先说明差异和影响，不直接引入平行视觉体系
- 发现可以复用已有 Widget、Provider、Service、工具函数时，应优先建议复用
- 不确定组件行为、路由规则、状态管理模式或设计 token 时，应按根 `AGENTS.md` 的查证优先级处理，禁止凭经验硬编码

---

## 推荐目录结构

- UI 目录建议聚焦在 `lib/pages/`、`lib/widgets/`、`lib/config/` 下组织，优先复用现有结构，不强制迁移。

```text
lib/
├── main.dart                    # 应用入口
├── app.dart                     # MaterialApp / GoRouter 配置
├── config/
│   ├── theme.dart               # ThemeData + ColorScheme 主题配置
│   ├── routes.dart              # GoRouter 路由定义
│   └── constants.dart           # 常量定义
├── pages/
│   ├── home/
│   │   ├── home_page.dart
│   │   └── home_widgets.dart    # 页面私有 Widget
│   └── [module]/
├── widgets/                     # 可复用的通用 Widget
│   ├── common/
│   └── layout/
├── providers/                   # Riverpod Provider
├── models/                      # 数据模型、DTO
├── services/                    # 业务服务
├── repositories/                # 数据仓储
└── utils/                       # 工具函数
```

---

### 阶段 1 — 视图实现（界面实现角色主导）

**触发条件**：用户发出「开始视图开发」指令

**入场要求**：阶段 0 设计文档已由用户确认

**工作内容**：
1. 按设计文档实现页面和 Widget，遵循 `design-system.yaml` 和 Material 3 样式规范。
2. 数据层使用 mock（静态 mock 数据），不依赖真实服务。
3. 同步输出数据模型文件 `lib/models/`，定义所有数据传输对象。

**产物**：
- 可运行的 Flutter 页面
- `lib/models/` 数据模型

**门控规则**：
- 用户确认页面符合设计文档预期。
- 数据模型中的类型已定稳，不再变动。
- 满足以上两点后，才允许进入阶段 2。

---

## UI 规则

### 样式规则
- 所有样式使用 Flutter ThemeData + Material 3 Token，禁止在 Widget 中直接硬编码颜色、间距、圆角
- 所有颜色通过 `Theme.of(context).colorScheme` 获取语义化 token
- 所有间距使用统一的常量或 `design-system.yaml` 中定义的 spacing scale
- 字体统一通过 `Theme.of(context).textTheme` 获取

### 移动端适配规则
- 页面布局必须适配不同屏幕尺寸和方向（竖屏 / 横屏）
- 使用 `LayoutBuilder` 和 `MediaQuery` 处理屏幕差异
- 触摸目标尺寸最小 44x44（iOS）/ 48x48（Android），关键操作区域需考虑拇指热区
- 安全区域处理：使用 `SafeArea` Widget 自动处理
- 列表滚动使用 `ListView.builder` / `GridView.builder`，支持懒加载
- 图片资源按平台提供合适分辨率，优先使用 SVG 矢量图
- 下拉刷新使用 `RefreshIndicator`
- 手势操作（滑动删除、捏合缩放）使用 Flutter 内置手势识别器
- 键盘遮挡输入框时使用 `SingleChildScrollView` 或 `ResizeToAvoidBottomInset` 自动调整

### Widget 规则
- 优先复用 `lib/widgets/` 下已有 Widget，禁止重复创建
- 只有确实有复用价值时才新增共享 Widget，避免为单次需求过度抽象
- 页面状态必须完整：`loading`、`empty`、`error`、`no-permission`
- 优先使用 Material 3 内置组件，不满足需求时再自定义 Widget
- 图标使用 Material Icons（`Icon(Icons.xxx)`）或项目既有素材

### 状态管理规则（Riverpod）
- 本地状态使用 `StatefulWidget` + `setState` 管理
- 跨 Widget 共享状态使用 Riverpod Provider
- Provider 定义在 `lib/providers/` 目录，按业务域拆分
- 禁止使用静态全局类存储业务状态
- 主题、语言等低频全局配置才放入 Riverpod Provider
- 页面生命周期事件中只做初始化和清理，不承载业务逻辑
- Provider 命名规范：`xxxProvider`（函数式）/ `XxxNotifier`（类式）

### 导航规则
- 使用 GoRouter 进行页面切换，路由定义集中在 `lib/config/routes.dart`
- 页面参数通过路由参数（path/query parameters）传递，不使用静态全局状态
- 模态页面使用 `context.push('/route')`
- 返回使用 `context.pop()`
- 需要历史记录时，GoRouter 默认支持
- 深度链接在 GoRouter 路由表中统一声明

### 弹窗规则
- 简单提示使用 `ScaffoldMessenger.of(context).showSnackBar`
- 确认弹窗使用 `showDialog` / `showAdaptiveDialog`
- 底部弹窗使用 `showModalBottomSheet`（动效参考 design-system.yaml 的 spring_physics）
- 弹窗内容如果复杂，应抽取为独立 Widget
- 弹窗结果通过异步返回（`await showDialog<T>`），禁止依赖隐式全局状态
- 危险操作需提供明确的二次确认

### 数据绑定规则
- 列表数据使用 `List<T>` 或 `AsyncValue<T>`（Riverpod）
- 异步数据加载使用 Riverpod 的 `AsyncNotifier` 自动管理 loading/error 状态
- 绑定路径必须可维护，禁止依赖脆弱的 Widget 查找方式

### 主题规则
- 默认使用浅色主题，深色主题仅在项目明确要求时扩展
- 若支持深色模式，所有颜色必须通过 `ColorScheme` 引用，禁止硬编码颜色值
- 主题切换通过 `ThemeMode` 控制，不自建主题切换机制
- 页面中任何视觉状态（含弹窗、Loading、Error 页面）必须同时验证浅色与深色下的可读性

---

## 动效规则

### 通用原则
- 动效参数统一参考 `design-system.yaml` 的 `animation` 区域
- 动效服务于交互反馈，不用于装饰
- 保持克制：避免过度弹跳、旋转、闪烁等分散注意力的动画
- 所有动效必须可通过 `MediaQuery.disableAnimations` 关闭（无障碍支持）

### 页面转场
- 默认转场：fade + slight slide，200ms，Curves.easeOut
- 前进导航：slide_from_right，250ms
- 模态页面：slide_from_bottom，300ms
- 详情展开：scale_up（0.9→1.0），200ms
- 自定义转场通过 `GoRouter` 的 `pageBuilder` 或 `CustomTransitionPage` 实现

### 列表交错入场
- 卡片网格、列表数据加载后使用交错入场动画
- 每项延迟 30ms，单项动画时长 300ms，Curves.easeOutCubic
- 使用 `AnimatedList` 或手动 `AnimationController` + `Interval`

### 弹簧动画
- 底部弹窗（BottomSheet）、抽屉（Drawer）使用弹簧物理动画
- 参数：damping=25, stiffness=200-300
- Flutter API：`SpringSimulation` 或 `Curves.elasticOut`
- 禁止在文字变化、颜色变化等非位移动画中使用弹簧

### 进度条动画
- 进度条从 0 到目标值的动画填充
- 时长 1200ms，Curves.easeOut
- Flutter API：`AnimatedContainer` 或 `TweenAnimationBuilder`

### 交互反馈
- 可点击元素默认使用 `InkWell`（Material 水波纹）
- 卡片等需要更强反馈的元素，增加按压缩放（scale 0.95→1.0, 100ms）
- 实时/在线状态使用脉冲指示器（success.light 颜色，循环动画）

### 毛玻璃效果
- 顶部导航栏：`BackdropFilter` blur=10, bg white/0.8
- 底部操作栏：`BackdropFilter` blur=15, bg white/0.85
- 深色背景浮层：`BackdropFilter` blur=20, bg black/0.3
- 避免在列表滚动区域使用 `BackdropFilter`，影响性能

### 代码组织规范
- 一个文件只放一个主对象：页面、Widget、布局、路由各自独立文件，文件名与主对象一致。
- Widget 职责单一：一个 Widget 只做一件事，发现持续膨胀或多个不相关职责堆在同一文件时，应拆分为更小的子 Widget。
- 触发拆分的信号：单文件出现多个主 Widget、字段持续堆叠、跨多个不相关页面、阅读成本明显升高。
- 允许例外：仅服务当前 Widget 的私有小组件、紧耦合的样式常量、测试 fixture。
- 反模式：一个文件同时定义页面、表单、列表、弹窗等多个 Widget；把多个不相关 Widget 堆在同一文件。

---

## 测试规则

### 提交前最小回归
- 默认执行：`flutter analyze`
- 页面、导航、表单交互改动：至少补一次受影响界面或组件的 smoke test / 等价验证
- 连续两次反馈同一视觉问题未解决时，必须先做根因回顾，再改为确定布局约束，禁止继续使用视觉补丁掩盖问题；未取得真机或截图证据前，不得宣称视觉问题已修复。
- **微小调整**（文案修改、颜色调整、间距优化等不影响逻辑的改动，修改内容少于10行）：可不补自动化测试，但仍应确认受影响界面的关键状态、布局与主要交互未回退
- 仅样式或视觉改动：至少确认受影响界面的关键状态、布局与主要交互未回退
- 若影响共享 Widget、布局或状态流转，优先验证影响范围最大的界面，而不是只看局部 Widget

### 总体要求
- 影响行为的改动应优先补充或更新测试
- 若本次改动未补测试，必须在最终说明中写明原因和风险
- 测试应覆盖真实业务行为，不要只验证静态渲染

### 视图层测试
- 页面交互、表单校验、列表行为、状态展示、异常状态变化时，应补充对应测试
- 至少关注以下关键状态：`loading`、`empty`、`error`、`no-permission`
- 若涉及数据请求、筛选、提交等关键路径，应验证主要交互结果
- 推荐使用 `flutter_test` + `mocktail`

### 外部依赖与数据
- 测试中不要真实调用外部服务，统一使用 mock、stub 或测试替身
- 测试数据应尽量最小化、可读、可重复执行
- 不要让测试依赖本地人工状态或不可控外部环境

### 无法执行测试时
- 必须说明未执行的测试类型
- 必须说明未执行原因
- 必须说明潜在影响范围和风险

---
