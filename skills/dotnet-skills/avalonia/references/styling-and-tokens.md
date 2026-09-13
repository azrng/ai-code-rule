# 样式系统与 token 管线

## 选择器语法

| 选择器 | 含义 | 示例 |
|---|---|---|
| `Button` | 类型 | `<Style Selector="Button">` |
| `.class` | Classes 类 | `<Style Selector="Button.HttpSaveButton">` |
| `:pseudo` | 伪类 | `Button:pointerover` |
| 空格 | 后代 | `TabControl.HttpRequestTabs TabItem` |
| `>` | 直接子代 | `Grid > Border` |
| `^` | 原点（ControlTheme 内引用宿主） | `^:pointerover` |
| `/template/` | 进入模板 | `^ /template/ Border#PART_Root` |
| `#name` | 按名字（模板内） | `Border#PART_Track` |

## 内置伪类（源码核实，非 WPF 名）

- 输入通用（InputElement 级）：`:pointerover`、`:pressed`、`:focus`、`:focus-within`、`:focus-visible`、`:disabled`
- 控件状态：`:checked`（CheckBox/ToggleSwitch）、`:selected`（选中项）、`:indeterminate`、`:expanded`（Expander/TreeViewItem）、`:open`（下拉类）
- 特殊：`:flyout-open`（Button 的 Flyout 打开态，Button.cs `pcFlyoutOpen`）、`:pressed`（ComboBox 也用）、`:dragging`（滑块）、`:empty`（ItemsControl 空态）、`:horizontal` / `:vertical`、`:dropdownopen`（ComboBox）

注意：**没有 `:hover`**，悬停态就是 `:pointerover`。

## Styles vs ControlTheme

- 项目级领域样式（消费工程模式）：`Assets/Styles/领域Styles.axaml` 里写 `<Style Selector="...">`，控件通过 `Classes="..."` 挂类。
- ControlTheme：为控件类型定义完整模板与默认观感（替代 WPF 隐式样式），主题库（Semi）用这种。业务工程一般不改 ControlTheme，挂 Classes 即可。

## Setter 值类型严格性（启动崩溃高发区）

Setter 值在**样式附加到控件时**才实例化，类型不匹配是运行时崩溃不是编译错误：

```xml
<!-- 崩溃：Space.2 是 x:Double 标量 -->
<Setter Property="Padding" Value="{StaticResource Space.2}" />
<!-- 正确：Thickness token -->
<Setter Property="Padding" Value="{StaticResource Pad.2}" />
```

## StaticResource vs DynamicResource

- `StaticResource`：XAML 加载时一次解析；资源必然存在、不随主题变 → 结构性资源（Pad/Radius/字号）。
- `DynamicResource`：运行时绑定跟随变化 → 颜色画刷（暗/亮主题切换必须用它，消费工程约定：所有 `Brush.*` 用 DynamicResource）。

## 条件类（数据驱动视觉）

```xml
<!-- 绑定 true 时挂上 HttpResponseNoticeDanger 类 -->
<Border Classes.HttpResponseNoticeDanger="{Binding ResponseSection.IsNoticeDanger}" />
```

配合选择器 `Border.HttpResponseNoticeDanger` 定义差异观感。第三方扩展 `iri:ClassHelper.Classes` 可挂多类字符串（消费工程用于方法徽章着色）。

## token 管线（消费工程约定）

```text
design-system.yaml（token 定义）
        ↓
Assets/Styles/Colors.axaml · Spacing.axaml · Shadows.axaml（语义 token 资源）
        ↓
Assets/Styles/领域Styles.axaml（Selector + Setter 引用 token）
        ↓
Views（只挂 Classes，零内联颜色/间距）
```

调 token 的固定顺序：先改 design-system.yaml，再同步资源字典，最后检查界面引用——不在界面绕过 token 硬编码。
