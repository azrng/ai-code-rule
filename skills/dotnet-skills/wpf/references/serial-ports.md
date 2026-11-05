# 串口通信（System.IO.Ports）

包：`System.IO.Ports`（.NET 6+ 需显式 PackageReference，不用框架内置）。课件基准 6.0.0。

## 参数与打开

五个参数与下位机约定一致才能通信：端口名、波特率、校验位、数据位、停止位。

```csharp
private SerialPort? serialPort;

private bool OpenPort()
{
    serialPort = new SerialPort
    {
        PortName = cmbPort.Text,          // "COM3"
        BaudRate = 9600,                  // 解析失败默认 9600，int.TryParse
        Parity = Parity.None,             // None / Odd / Even / Mark / Space
        DataBits = 8,
        StopBits = StopBits.One           // One / OnePointFive / Two
    };
    try
    {
        serialPort.Open();
        return true;
    }
    catch (UnauthorizedAccessException) { Alert("端口被占用"); return false; }
    catch (IOException ex)               { Alert($"端口不存在或 IO 错误：{ex.Message}"); return false; }
}
```

- 枚举可用端口：`SerialPort.GetPortNames()`（返回 string[]，绑到 ComboBox）；插拔不触发事件，UI 提供「刷新」按钮重查
- `Open()` 前判断 `IsOpen`，重复 Open 抛 `InvalidOperationException`
- 端口被占用（上次崩溃未释放、别的软件占用）抛 `UnauthorizedAccessException`——异常分支必须处理，串口助手类工具这是日常
- 关闭窗体 / 断开连接时 `Close()` + 置 null；`Close` 后 `Open` 同一实例前确认没有挂起的 `DataReceived`

## 发送

```csharp
// 文本
serialPort.WriteLine("AT+CSQ");
// 十六进制字节（工业设备主流）
byte[] frame = { 0x01, 0x03, 0x00, 0x00, 0x00, 0x02, 0xC4, 0x0B };
serialPort.Write(frame, 0, frame.Length);
```

UI 上让用户输入 hex 字符串时先校验再转字节：按空格拆分、每段两位 hex、`byte.Parse(s, NumberStyles.HexNumber)`；非法输入当场报错，不静默截断。

## 接收（DataReceived）

```csharp
serialPort.DataReceived += (_, _) =>
{
    try
    {
        var n = serialPort.BytesToRead;
        var buffer = new byte[n];
        serialPort.Read(buffer, 0, n);
        Dispatcher.BeginInvoke(() => AppendReceivedText(buffer));   // 线程池线程 → 封送
    }
    catch (Exception) { /* 端口可能在事件排队期间被关闭 */ }
};
```

- 事件在线程池线程触发；**只读缓冲区 + 封送**，禁止直接碰控件、禁止直接 `MessageBox`
- 一帧数据可能拆成多次事件到达（分批到达）：按协议组帧（帧头 + 长度 + 校验）在缓冲区里攒，攒齐再解析；不能假设「一次事件 = 一条完整消息」
- 事件排队期间关闭串口会抛异常：handler 内 try/catch 兜住；关闭串口前先 `-=` 退订更稳
- `ReceivedBytesThreshold` 调大可减少小包触发次数，但**不保证**一次事件收到完整帧，组帧逻辑照样要写

## 编码纪律

- 两端显式约定同一编码；`Encoding.Default` 随系统代码页（中文系统 GBK）漂移，禁止用
- ASCII 协议：`Encoding.ASCII`；中文 / GBK 设备：`Encoding.GetEncoding("GB18030")`（需 `System.Text.Encoding.CodePages` 包 + `Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)`）
- 十六进制协议（Modbus RTU、自定义帧）全程 byte[]，解析用索引切片 + 校验（CRC / LRC），显示时再转 hex 字符串
- 显示区 hex / 文本模式切换：收到的原始字节永远保留，视图层决定怎么渲染

## 串口助手类工具的功能口径

- 发送区 / 接收区分开；各自有 hex 与文本切换
- 接收区追加模式（新数据 append，时间戳可选），大数据量防 UI 卡顿：接收封送批量攒、UI 定时刷
- 断线（拔 USB 转串口）不抛托管异常但 `IsOpen` 失真：写失败时 catch 后自动关闭并置「未打开」状态
