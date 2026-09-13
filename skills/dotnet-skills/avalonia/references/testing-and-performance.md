# 测试与性能

## 界面回归测试（消费工程现行模式）

轻量层：XAML 结构断言（不依赖 Headless，纯 XDocument）：

```csharp
var document = XDocument.Load(FindSourceFile("src", "ApixPress.App", "Views", "Controls", "MainWindow.axaml"));

// 断言结构契约：宿主类、绑定路径、必挂类
var host = document.Descendants()
    .Single(e => e.Name.LocalName == "ContentControl" && HasClass(e, "ProjectWorkspaceHost"));
Assert.Equal("{Binding ActiveProjectTab}", host.Attribute("Content")?.Value);
```

适合锁：结构契约（谁包含谁）、关键绑定路径、样式 token 引用（防回退内联硬编码）、伪类选择器存在性。

ViewModel 层：普通 xunit + 替身服务，覆盖命令逻辑与生命周期（Dispose / 事件退订）。

## Avalonia.Headless（大改动时推荐接入）

官方样板：`D:\SourceCode\avalonias\Avalonia.Samples\src\Avalonia.Samples\Testing\TestableApp.Headless.XUnit\`（NUnit / Appium 版本同目录），以下写法已回 12.1.2 样板核实。

测试工程包引用：

```xml
<PackageReference Include="Avalonia.Headless.XUnit" Version="12.1.2" />
```

装配（独立文件，程序集级声明）：

```csharp
using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
```

用例（官方 CalculatorTests 模式：实例化 Window + 模拟输入按键 + 断言）：

```csharp
[AvaloniaFact]
public void Should_Add_Numbers()
{
    var window = new MainWindow { DataContext = new MainWindowViewModel() };
    window.Show();

    window.FirstOperandInput.Focus();
    window.KeyTextInput("10");                                   // 模拟文本输入
    window.AddButton.Focus();
    window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);  // 模拟按键

    Assert.Equal("30", window.ResultBox.Text);
}
```

原则：不真调外部服务；测试数据最小可重复；关键状态四态（loading/empty/error/no-permission）至少各一条断言；弹层 / 焦点相关交互（如 Flyout 失活陷阱）优先用 Headless 复现再写回归。

## 性能基线（消费工程沉淀）

1. **惰性宿主**：大视图按可见性实例化（`ContentControl` + PropertyChanged 后 `host.Content ??= factory()`），切换零成本
2. **集合批量更新**：批量导入/刷新单次 `Clear+Add`（或 `ReplaceWith`），不逐条 Insert
3. **防抖重算**：每键输入路径用防抖（250ms 级），状态批次合并（`QueueWorkspaceChanges` 模式：pending 标志位 + while 循环排空）
4. **SQL 去大字段**：列表查询不带 raw_json / snapshot 大列（配合仓储层）
5. **虚拟化**：>100 行的列表必配 `VirtualizingStackPanel` + 有界高度
6. **样式资源**：循环内创建的画刷/资源提为静态 token（JSON 高亮语法着色场景）

## 交付前冒烟（样式改动必做）

```text
1. dotnet build（0 错误 0 警告）
2. dotnet test（既有断言不回退）
3. 实际启动应用，走到改动样式命中的界面
4. 关键交互各点一次：列表滚动、弹层开关、窗口切换（弹层失活陷阱高发场景）
```
