# 动画与 Animator

## 属性类型 → Animator 对照表（源码 `src/Avalonia.Base/Animation/Animators/`，12.1.2 核实）

| 属性值类型 | Animator |
|---|---|
| double / float | DoubleAnimator / FloatAnimator |
| Color / IBrush / GradientBrush | ColorAnimator / BaseBrushAnimator / GradientBrushAnimator（纯色用 SolidColorBrushAnimator） |
| BoxShadow / BoxShadows | BoxShadowAnimator / BoxShadowsAnimator |
| Thickness / CornerRadius | ThicknessAnimator / CornerRadiusAnimator |
| Point / Rect / Size / Vector / RelativePoint / RelativeScalar | PointAnimator / RectAnimator / SizeAnimator / VectorAnimator / RelativePointAnimator / RelativeScalarAnimator |
| 整数（byte/short/int/long 及无符号） | 对应 *Animator（Byte/Int16/Int32/Int64/UInt*） |
| bool | BoolAnimator |
| decimal | DecimalAnimator |
| Transform / TransformOperations | TransformAnimator / TransformOperationsAnimator |

**没有任何 Animator 匹配的属性，动画运行时直接抛异常**，不会静默失败：

```text
InvalidOperationException: No animator registered for the property RenderTransform.
Add an animator to the Animation.Animators collection that matches this property to animate it.
```

## 旋转 spinner 安全写法（推荐，Ursa Loading.axaml 同款）

`RenderTransform` 是 Transform 类型，直接在 KeyFrame 写 `rotate(0deg)` 需要显式 Animator 注册，容易踩坑。安全做法：挂一个 `RotateTransform`，动画它的 `Angle`（double → 自动推断 DoubleAnimator）：

```xml
<Style Selector="Path.HttpSpinner">
  <Setter Property="Data" Value="M 7,1 A 6,6 0 1 1 1,7" />
  <Setter Property="Stretch" Value="None" />
  <Setter Property="RenderTransform">
    <RotateTransform />
  </Setter>
  <Style.Animations>
    <Animation IterationCount="Infinite" Duration="0:0:0.8">
      <KeyFrame Cue="0%">
        <Setter Property="RotateTransform.Angle" Value="0" />
      </KeyFrame>
      <KeyFrame Cue="100%">
        <Setter Property="RotateTransform.Angle" Value="360" />
      </KeyFrame>
    </Animation>
  </Style.Animations>
</Style>
```

要点：
- 初始角度由 KeyFrame 0% 提供，不需要额外设置初始 RenderTransform 值。
- `RotateTransform.Angle` 是附加路径写法（`RotateTransform.Angle`），Setter.Property 直接可解析。
- 静态旋转（不动画）可用字符串变换：`<Setter Property="RenderTransform" Value="rotate(180deg)" />`（Semi Expander.axaml 同款，仅 Setter 可用，**不能直接做 KeyFrame**）。

## Animation 语法速查

```xml
<Style.Animations>
  <Animation Duration="0:0:0.25" FillMode="Forward">
    <KeyFrame Cue="0%">  <Setter Property="Opacity" Value="0" /> </KeyFrame>
    <KeyFrame Cue="100%"><Setter Property="Opacity" Value="1" /> </KeyFrame>
  </Animation>
</Style.Animations>
```

- `IterationCount="Infinite"` 循环；`Duration` 是 TimeSpan。
- KeyFrame 用 `Cue="0%"` 百分比或 `KeyTime="0:0:0.1"` 绝对时间（二选一）。
- 简单过渡用 `Transitions`（属性变化时自动插值），触发式时间线才用 Animations。

## 排查动画崩溃

动画异常在**样式应用到可视树、动画首次启动时**抛出，编译期不报。看到 `No animator registered for the property X`：查对照表 → 换成有 Animator 的子属性（如 Transform → RotateTransform.Angle）。
