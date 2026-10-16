# 属性系统与自定义控件

事实来源：官方文档（docs.avaloniaui.net「Defining properties」「Templated controls」，对应 12.x）核实后结合消费工程口径提炼；与源码疑有不符时按 SKILL.md「上游源码按需查证」回 `12.1.2` tag 核实。

## 先分清四个「模板 / 主题」概念

| 概念 | 回答的问题 | 关键写法 |
|---|---|---|
| `DataTemplate` | **数据对象**长什么样 | `DataType="vm:Xxx"` + 必须带 `x:DataType`（编译绑定） |
| `ControlTemplate` | 单个**控件**的视觉结构 | 模板内用 `TemplateBinding` 取宿主属性 |
| `ControlTheme` | 控件**类型**的默认主题（模板 + 默认 Setter，替代 WPF 隐式样式） | `x:Key="{x:Type local:Xxx}"` + `TargetType`；主题库（Semi）的写法 |
| `Styles` | **项目级**领域样式（挂 `Classes` 生效） | `<Style Selector="Button.HttpSaveButton">` |

控件形态选型（与 SKILL.md「代码生成优先级」一致）：应用组合用 `UserControl`；可复用、可被主题换肤的控件用 `TemplatedControl`；自绘 `Control.Render` 是最后手段。

## StyledProperty：可样式化 / 动画 / 继承的值

值存于属性系统（无 backing field），参与样式、动画、值优先级：

```csharp
public class StatusBadge : TemplatedControl
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<StatusBadge, string>(nameof(Text), "Ready");

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
}
```

- 静态字段命名必须是 `[属性名]Property`——XAML 靠该约定解析，写错报 `Unable to find suitable setter or adder for property`。
- `Register` 常用参数：`defaultValue`、`inherits`（沿可视树继承）、`defaultBindingMode`（如 `BindingMode.TwoWay`）、`validate` / `coerce`。
- **CLR 访问器只准 `GetValue` / `SetValue`**：样式与绑定可绕过 CLR 属性直接走属性系统，访问器里加逻辑会静默丢行为。副作用放 `OnPropertyChanged`。
- **禁止给 styled property 加 backing field**：读字段拿到的是过期值，一律走 `GetValue`。

## DirectProperty：字段支撑 / 只读 / 高性能

不参与样式与动画，支持绑定与变更通知，读值即字段访问：

```csharp
// 可写：注册必须给齐三委托（getter + setter），缺 setter 即只读属性
public static readonly DirectProperty<ResultView, int> CountProperty =
    AvaloniaProperty.RegisterDirect<ResultView, int>(
        nameof(Count), o => o.Count, (o, v) => o.Count = v);

private int count;

public int Count
{
    get => count;
    set => SetAndRaise(CountProperty, ref count, value);   // 更新字段 + 发通知，单调用
}
```

- **CLR setter 必须用 `SetAndRaise`**；对 direct 属性调 `SetValue` 直接抛 `InvalidOperationException`。
- 只读属性：注册省略 setter 委托，CLR setter 保持 `private set` + `SetAndRaise`（绑定时只能 OneWay/OneTime 读）。
- 适用：计算/派生的只读状态、集合所有权、不想被样式覆盖的背书值。

## 附加属性：在容器类上定义、挂到别的控件

```csharp
public class DimExtensions : AvaloniaObject
{
    public static readonly AttachedProperty<bool> IsDimmedProperty =
        AvaloniaProperty.RegisterAttached<DimExtensions, Control, bool>("IsDimmed");

    public static void SetIsDimmed(Control element, bool value) => element.SetValue(IsDimmedProperty, value);
    public static bool GetIsDimmed(Control element) => element.GetValue(IsDimmedProperty);

    static DimExtensions()
    {
        IsDimmedProperty.Changed.AddClassHandler<Control>((control, _) =>
            control.Opacity = GetIsDimmed(control) ? 0.5 : 1.0);
    }
}
```

XAML：`<Button local:DimExtensions.IsDimmed="True" />`。静态 `Get`/`Set` 访问器是 XAML 按名字查找的，不能省。

## 复用既有属性：AddOwner

要与既有属性共享同一属性标识（样式打到谁都生效）时用 `AddOwner`，不重复注册：

```csharp
public static readonly StyledProperty<IBrush?> BackgroundProperty =
    Border.BackgroundProperty.AddOwner<MyControl>();
```

## 响应属性变化

```csharp
protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
{
    base.OnPropertyChanged(change);   // 必须先调 base
    if (change.Property == TextProperty)
        InvalidateVisual();
}
```

12 起数据验证简化：注册属性时给 `enableDataValidation: true`（需配 `defaultBindingMode: BindingMode.TwoWay`）即自动上报校验错误，**不再需要**旧版的 `UpdateDataValidation` override；外层套 `DataValidationErrors` 展示，`:error` 伪类控制观感。

## TemplatedControl：模板部件与伪类

```csharp
[TemplatePart("PART_Button", typeof(Button), IsRequired = true)]
[TemplatePart("PART_Label", typeof(TextBlock))]
public class ToggleLabel : TemplatedControl
{
    private Button? _button;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_button is not null)
            _button.Click -= OnButtonClick;      // 模板可能重挂，先退订旧订阅

        _button = e.NameScope.Get<Button>("PART_Button");     // 缺失抛异常（必需部件）
        // e.NameScope.Find<TextBlock>("PART_Label");          // 可选部件，缺失返回 null
        _button.Click += OnButtonClick;
    }
}
```

- 部件名约定 `PART_` 前缀；`Get<T>` / `Find<T>` 类型不符时抛异常。
- 伪类：类上声明 `[PseudoClasses(":active", ":dragging")]`，代码里 `PseudoClasses.Set(":active", isActive)`；ControlTheme 内嵌套 `<Style Selector="^:active">` 让 `^` 解析到宿主。主题作者据此写状态观感，不碰 code-behind。

### 禁止 DataContext = this

自定义控件构造函数里 `DataContext = this` 会顶掉用户从可视树继承的 DataContext，外部 `{Binding ...}` 全部静默解析失败。模板内用 `TemplateBinding` 取宿主属性，DataContext 让它自然继承。

### TemplateBinding 四条边界

1. 只接受单属性，不支持路径：`{TemplateBinding Caption.Length}` 不行，改 `{Binding Caption.Length, RelativeSource={RelativeSource TemplatedParent}}`。
2. 只支持 `OneWay` / `TwoWay`（默认 OneWay；这点与 WPF 的 TemplateBinding 不同）。
3. 只能用在 `StyledElement` 上：非 StyledElement（如 `GeometryDrawing`）**静默失败**不报错，改用 RelativeSource 长写法。
4. 需要写回宿主时显式 `Mode=TwoWay`（如内嵌 Slider 回写 Value）。

### SetCurrentValue：不顶掉消费者绑定

控件内部要改一个可能被外部绑定的属性时用 `SetCurrentValue`——更新生效值但保留既有绑定；直接 `SetValue` 会顶掉绑定。

## 与裁剪 / AOT 的交界

反射式视图定位器（ViewModel→View 字符串约定解析）、运行时动态加载 XAML 都是裁剪/AOT 风险点：显式类型映射优先，确需保留时做裁剪保留声明。发布侧取舍见 references/publishing-and-single-file.md 的 Native AOT 节。
