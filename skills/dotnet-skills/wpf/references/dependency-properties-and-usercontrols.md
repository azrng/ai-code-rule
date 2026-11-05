# DependencyProperty 与 UserControl 自定义控件

## 什么时候写依赖属性

绑定、动画、样式生效的前提是属性走依赖系统。自定义控件（温度计、液位计、仪表盘）暴露给外界的状态一律注册 `DependencyProperty`，普通 CLR 属性绑不上去。

模板（课件温度计实战同款）：

```csharp
public static readonly DependencyProperty ValueProperty =
    DependencyProperty.Register(
        nameof(Value), typeof(double), typeof(Thermometer),
        new PropertyMetadata(0.0, OnValuePropertyChanged));

public double Value
{
    get => (double)GetValue(ValueProperty);
    set => SetValue(ValueProperty, value);
}

private static void OnValuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    => ((Thermometer)d).Refresh();   // 静态回调，转强类型实例
```

要点：
- `Register(name, type, ownerType, metadata)`：名字用 `nameof`，owner 必须是注册类自身
- 变化回调是**静态**方法，签名 `(DependencyObject, DependencyPropertyChangedEventArgs)`；实例状态靠参数 `d` 转型
- 默认值放 metadata（`new PropertyMetadata(100)`），不写在构造器里赋值（重置样式会丢）
- 只读状态用 `DependencyPropertyKey`（`RegisterReadOnly`）
- 回调里改同一属性 = 递归重入，禁止；需要级联刷新走私有 `Refresh()` 方法

## UserControl 组合式控件（课件温度计范式）

结构：`Viewbox` 包一层自适应缩放，内部 `Grid`/`Canvas` 画部件；外观用 XAML（渐变笔刷、`Border` 圆角、`DropShadowEffect`），刻度这类数量不定的元素在代码里生成。

```xml
<UserControl x:Class="Hmi.Controls.Thermometer" ...>
    <Viewbox>
        <Grid Width="100" Height="400">
            <!-- 管体：圆角矩形 + 渐变描边 -->
            <Rectangle StrokeThickness="7" RadiusX="40" RadiusY="15"/>
            <!-- 刻度画布：代码生成 -->
            <Canvas x:Name="MainCanvas" Width="75" Margin="0,70"/>
            <!-- 水银柱：高度由代码按值驱动 -->
            <Border x:Name="BorValue" Width="10" VerticalAlignment="Bottom"
                    CornerRadius="5">
                <Border.Background>
                    <LinearGradientBrush StartPoint="0,0" EndPoint="1,0">
                        <GradientStop Color="#CD3333"/>
                        <GradientStop Color="#FFC0CB" Offset="0.4"/>
                        <GradientStop Color="#CD3333" Offset="1"/>
                    </LinearGradientBrush>
                </Border.Background>
            </Border>
        </Grid>
    </Viewbox>
</UserControl>
```

刷新逻辑（值 / 量程变化、尺寸变化都进同一入口）：

```csharp
public Thermometer()
{
    InitializeComponent();
    SizeChanged += (_, _) => Refresh();
}

private void Refresh()
{
    if (MainCanvas.ActualHeight == 0) return;   // 布局未完成，ActualHeight 为 0，直接跳过

    double step = MainCanvas.ActualHeight / (Maxmum - Minmum);
    // ...按 step 画刻度 Line/TextBlock，Canvas.SetLeft/SetTop 定位...

    // 值钳制到量程后换算高度，用动画过渡
    var value = Math.Clamp(this.Value, Minmum, Maxmum);
    var height = (value - Minmum) * step + 20;
    BorValue.BeginAnimation(HeightProperty,
        new DoubleAnimation(height, TimeSpan.FromMilliseconds(500)));
}
```

要点：
- `ActualHeight == 0` 是「控件还没走完布局」的信号，此时刷新直接返回，靠 `SizeChanged` 兜底重进
- 值变化重绘量程内刻度前先 `Children.Clear()`，否则刻度叠加
- 对外只暴露依赖属性（`Minimum` / `Maximum` / `Value`），内部部件不对外
- 命名统一：控件库项目（如 `Hmi.Controls`）与使用项目分两个 csproj，控件库 `<UseWPF>true</UseWPF>` 无 `OutputType`

## UserControl vs 自定义 TemplatedControl

| | UserControl | TemplatedControl |
|---|---|---|
| 外观 | XAML 固定，使用方不能换模板 | `ControlTemplate` 可整体替换（Lookless） |
| 成本 | 低，直接组合 | 需要部件（TemplatePart）+ 模板绑定 |
| 场景 | 仪表、复合输入行等项目内控件 | 通用控件库、主题可换的控件 |

项目内做上位机画面组件选 UserControl；做要发布给多项目换肤的控件库才上 TemplatedControl。

## 常见错误

- 注册名与 CLR 包装属性名不一致：绑定按注册名找，包装属性名是另一个时数据到不了
- 在构造器里读 `ActualWidth`：恒 0；订阅 `Loaded` / `SizeChanged`
- 回调里直接 `Refresh()` 但 `MainCanvas` 为 null：回调可能在模板应用前触发，先判空
- 硬编码尺寸不缩放：套 `Viewbox` 让控件整体随分配空间缩放（注意 `Stretch` 默认 Uniform）
- 用 `Timer` 改依赖属性驱动动画：用 `DoubleAnimation` / `BeginAnimation`，走合成时钟不占 UI 线程
