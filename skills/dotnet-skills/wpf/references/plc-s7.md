# 西门子 PLC（S7netplus）

包：`S7netplus`（NuGet；课件基准 0.13.0）。覆盖 S7-200 Smart 以外的 S7-300/400/1200/1500（`CpuType` 枚举选型）。

## 连接

```csharp
using S7.Net;

private Plc? plc;

// CpuType、IP、机架号 rack、插槽 slot
plc = new Plc(CpuType.S71200, "192.168.0.1", 0, 1);
await plc.OpenAsync();                       // 异步打开，UI 事件里不冻界面
if (plc.IsConnected) { /* 更新状态 */ }
```

- S7-1200 / 1500：rack 0、slot 1；S7-300/400 的 rack/slot 按硬件组态（常见 0,2）
- S7-1200/1500 需要在 TIA Portal CPU「Protection & Security」里**勾选允许访问（PERMISSIVE 或配访问权限）**，否则连接被拒；这是连不上的第一位原因，先查它再查 IP
- `Open()` / `OpenAsync()` 有 TCP 超时，放后台执行；不要在 UI 线程同步等
- 连接是长连接：重连前 `Close()` 旧对象再新建；采集线程抛异常时标记断线，由重连逻辑重建

## 地址语法（单点 Read/Write）

`Read(string address)` / `Write(string address, object value)`，地址字符串：

| 写法 | 含义 | 返回 / 写入类型 |
|---|---|---|
| `I0.0` / `Q0.1` / `M0.0` | 输入 / 输出 / 中间继电器位 | bool |
| `IB0` / `QB0` / `MB0` | 字节 | byte |
| `IW0` / `MW0` | 字（2 字节） | ushort |
| `ID0` / `MD0` | 双字（4 字节） | uint |
| `DB1.DBX0.0` | DB1 块位 | bool |
| `DB1.DBW2` | DB1 块字 | ushort |
| `DB1.DBD4` | DB1 块双字 | uint |
| `DB1.DBD4`（VarType.Real 时） | 浮点 | float |

- `Read` 返回 object，按地址类型强转（`DBW` → ushort）；直接 `.ToString()` 拼界面或当 int 用是常见错误源
- 写位值用 0/1 或 true/false：`plc.Write("M0.0", 1)`
- 对 I 区（物理输入）`Write` 通常无效（由下位机刷新），监控可以、写入别用——课件示例写 `I0.0` 仅在仿真器下成立

## 类型化读写（DataItem / ReadMultipleVars）

一次取多个不连续地址（画面刷新的正确姿势，一次报文往返）：

```csharp
var items = new List<DataItem>
{
    new() { DataType = DataType.Output,  DB = 0, StartByteAdr = 0, VarType = VarType.Bit,  Count = 1 },
    new() { DataType = DataType.Memory,  DB = 0, StartByteAdr = 0, VarType = VarType.Word, Count = 1 },
    new() { DataType = DataType.DataBlock, DB = 1, StartByteAdr = 2, VarType = VarType.Word, Count = 1 },
};

var results = await plc.ReadMultipleVarsAsync(items);
// results[i].Value 按各自 VarType 解码（Bit→bool、Word→ushort、Real→float...）
```

- `DataType`（Input/Output/Memory/DataBlock/Timer/Counter）+ `DB` 块号 + `StartByteAdr` 起始字节 + `VarType` 类型 + `Count` 个数
- 连续一片 DB 数据更省事：`plc.Read(DataType.DataBlock, db, startByte, VarType.Byte, count)` 拿 byte[] 自己按偏移解——工程上常用「整块读 + 内存解包」减少往返
- DB 号对应 TIA Portal 里「非优化访问」的块；优化访问块（默认）没有固定偏移，需在块属性里取消「优化的块访问」或改用 PLC 变量表导出的偏移

## 采集轮询

```csharp
// 周期读：后台 Task + await Task.Delay（必须 await，见 threading-and-dispatcher.md）
while (!token.IsCancellationRequested)
{
    var snapshot = await plc.ReadMultipleVarsAsync(items);   // 后台
    Dispatcher.BeginInvoke(() => UpdatePanels(snapshot));    // 一次封送刷整幅画面
    await Task.Delay(cycleMs, token);
}
```

- 轮询周期按画面刷新需求定（500–1000ms 常见），别把 PLC 当消息总线全速读
- 抛异常（网络闪断）→ 捕获、标记断线、退避重连，不让线程死循环打日志
- S7netplus 的 `Plc` 非线程安全：读写共用一个 `Plc` 实例时用 `SemaphoreSlim` 串行化，或单采集线程独占

## CPU 类型与 CpuType 选型

| 设备 | CpuType | rack/slot |
|---|---|---|
| S7-200 Smart | `S7200Smart`（走 Smart 特定处理） | 0,1 |
| S7-300 | `S7300` | 0,2（按组态） |
| S7-400 | `S7400` | 0,2（按组态） |
| S7-1200 | `S71200` | 0,1 |
| S7-1500 | `S71500` | 0,1 |
