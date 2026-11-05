# 线程与 Dispatcher

## UI 线程模型

WPF 单 UI 线程拥有全部可视化树；控件带线程亲和性（ STA + `DispatcherObject`），非 UI 线程触碰直接抛 `InvalidOperationException: The calling thread cannot access this object because a different thread owns it`。

需要封送的典型来源：
- 串口 `DataReceived`（线程池线程）
- `Task.Run` / 后台采集线程 / `System.Threading.Timer`
- PLC 库的异步回调
- COM 组件事件（三菱 MX Component）

## 封送写法

```csharp
// 同步：等 UI 线程执行完才返回。可能死锁（UI 线程恰在等本线程），优先 BeginInvoke
Dispatcher.Invoke(() => txtPressure.Text = value.ToString());

// 异步：排队即返回，采集线程不等界面
Dispatcher.BeginInvoke(() => txtPressure.Text = value.ToString());

// 只读数据也走封送：后台线程读 TextBox.Text 同样违规
string ip = "";
Dispatcher.Invoke(() => ip = txtIpAddress.Text);
```

- 封送粒度：整段 UI 更新一次封送，别在循环里逐条 `Invoke`（每条一次线程切换，采集频率高时界面卡死）
- 采集循环内先组装数据快照，循环外（或每 tick 一次）`BeginInvoke` 批量刷界面
- `DispatcherPriority`：界面刷新用 `Background`/`Render` 优先级即可，别全用 `Send` 抢输入响应

## 采集线程正确姿势

```csharp
private CancellationTokenSource? cts;

private void StartMonitoring()
{
    cts = new CancellationTokenSource();
    var token = cts.Token;
    Task.Run(async () =>
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var value = plc.Read("DB1.DBW2");        // 后台线程做 IO
                Dispatcher.BeginInvoke(() => txtPressure.Text = value!.ToString()); // 封送刷 UI
            }
            catch (Exception ex) { /* 记日志，别让线程静默死亡 */ }
            await Task.Delay(1000, token).ConfigureAwait(false);
        }
    }, token);
}

private void StopMonitoring() => cts?.Cancel();
```

对照课件原始写法的三个 bug：
1. `Task.Delay(1000);` 不 `await`——等于没有延时，循环全速空转打爆 CPU 和 PLC
2. `while (true)` 无退出机制——关窗后线程还在跑，连接对象已释放直接异常
3. 循环里逐次 `Dispatcher.Invoke` 同步封送——UI 繁忙时后台线程被拖死

## async UI 事件

按钮事件直接 async，IO 用异步 API（S7netplus 的 `*Async`、串口的 `BaseStream.ReadAsync`）：

```csharp
private async void btnConnect_Click(object sender, RoutedEventArgs e)
{
    btnConnect.IsEnabled = false;                 // 防重入
    try
    {
        await plc.OpenAsync();                    // 连接超时不冻界面
        statusInfoTextBlock.Text = "已连接";
    }
    catch (Exception ex)
    {
        MessageBox.Show($"连接失败：{ex.Message}");
    }
    finally
    {
        btnConnect.IsEnabled = true;
    }
}
```

- `async void` 只允许事件 handler；其余一律 `async Task`
- 事件 handler 里 `await` 后的代码回到 UI 线程（SynchronizationContext），直接碰控件合法
- 库无异步 API 时：`Task.Run(() => plc.Open())` 包一层，**禁止** `.Result` / `.Wait()`（死锁 + 冻界面）

## 常见错误

- 后台线程 `MessageBox.Show`：要么封送到 UI 线程弹，要么改日志
- `Dispatcher.Invoke` 里做长活（循环保存文件）：封送过去又把 UI 线程堵死；Invoke 里只做纯 UI 操作
- `Task.Delay` 当 `Thread.Sleep` 用（不 await）：见上
- 跨线程读 `IsOpen` / 控件属性「只是读一下」：读也违规，一样封送或用状态字段 + `volatile`/锁
- 关窗不停采集线程：`Window.Closed` / `OnClosing` 里 `Cancel()` 并等待退出（或至少 `Cancel` 后再释放设备对象）
