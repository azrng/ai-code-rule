# 绑定与 MVVM

## 绑定基础

```xml
<!-- 元素绑定：Slider 值驱动文本 -->
<TextBox Text="{Binding ElementName=sdNumber1, Path=Value, UpdateSourceTrigger=PropertyChanged}"/>

<!-- DataContext 绑定（ViewModel） -->
<TextBox Text="{Binding Number1, UpdateSourceTrigger=PropertyChanged}"/>
<Button Command="{Binding CalcCommand}" Content="计算结果"/>
```

- `UpdateSourceTrigger`：`TextBox.Text` 默认 `LostFocus` 才回写源——回车前算结果的场景会「差一拍」，需要 `PropertyChanged`
- 路径不刷新先查三件事：绑定源（`DataContext`）对不对、属性是不是公有可写、有没有 `INotifyPropertyChanged`
- 转换器：`IValueConverter`（`Convert` / `ConvertBack`），Boolean→Visibility、枚举→颜色这类放转换器，不往 ViewModel 塞 UI 枚举

## INotifyPropertyChanged

最小可用实现：

```csharp
public class MainWindowViewModel : INotifyPropertyChanged
{
    private int result;
    public int Result
    {
        get => result;
        set { result = value; OnPropertyChanged(nameof(Result)); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
```

坑：
- setter 里改字段后**必须**触发通知，漏触发 = 界面永远旧值
- 通知名与属性名不一致 = 绑定失效且无异常，静默失败；有条件用 `nameof`
- 无关属性（纯字段、get-only）不实现 INPC 也不影响；只对参与绑定的属性做通知

## ICommand / RelayCommand

无 MVVM 库时的基础实现（课件 `ReplayCommand` 的修正版——原版 `CanExecute` 漏了 `return`，永远返回 true）：

```csharp
public class RelayCommand : ICommand
{
    private readonly Action<object?> execute;
    private readonly Func<object?, bool>? canExecute;

    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
        this.canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => canExecute?.Invoke(parameter) ?? true;
    public void Execute(object? parameter) => execute(parameter);
}
```

ViewModel 用法：

```csharp
public RelayCommand CalcCommand { get; }

public MainWindowViewModel() => CalcCommand = new RelayCommand(_ => CalcResult());

private void CalcResult()
{
    Result = Number1 + Number2;
}
```

项目里已有 **CommunityToolkit.Mvvm** 时优先用源生成器（省样板）：

```csharp
public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private int result;

    [RelayCommand]
    private void Calc() => Result = Number1 + Number2;
}
```

```xml
<Button Command="{Binding CalcCommand}"/>
```

## MVVM 分层口径

- **Model**：业务实体（如 `Student`），不引用 UI 命名空间
- **ViewModel**：界面状态 + 命令，引用 Model，不引用控件；一个界面一个 ViewModel，巨型界面拆子 ViewModel
- **View**（xaml + xaml.cs）：绑定与纯视图交互；`DataContext` 在 XAML 或代码后置一处指定
- 跨 ViewModel 通信：轻量用事件 / 消息类（CommunityToolkit 的 `WeakReferenceMessenger`），不搞全局静态总线
- 上位机项目：设备连接 / 采集服务放 Service 层，ViewModel 只拿服务返回的数据快照；服务回调里封送 UI（见 threading-and-dispatcher.md）

## 常见错误

- 绑定写错属性名无任何报错：WPF 绑定失败默认静默。排查用输出窗口的绑定日志，或临时设 `PresentationTraceSources.DataBinding` 跟踪
- `CalcCommand` 写成 `get => new RelayCommand(...)` 每次访问新建实例：命令字段只初始化一次（课件原始写法的 bug）
- ViewModel 引用 `System.Windows.Controls`：分层破了，把 UI 类型挡在 ViewModel 外（`Visibility` 这类用转换器换算）
- 集合绑定用 `List<T>`：增删不刷新；绑集合一律 `ObservableCollection<T>`
