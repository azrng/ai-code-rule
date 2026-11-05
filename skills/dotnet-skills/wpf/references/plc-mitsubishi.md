# 三菱 PLC（MX Component / ActUtlType）

三菱官方 COM 组件 MX Component 暴露的 `ActUtlType`（32 位）/ `ActUtlType64`（64 位）接口。**程序位数必须与组件位数一致**：x64 进程引用 `ActUtlType64`。csproj 里 COM 引用：

```xml
<ItemGroup>
  <COMReference Include="ActUtlType64Lib">
    <WrapperTool>tlbimp</WrapperTool>
    <VersionMajor>5</VersionMajor>
    <VersionMinor>0</VersionMinor>
    <Guid>719a5fab-eb1c-4b52-98bc-f8c9f6912d04</Guid>
    <Lcid>0</Lcid>
    <Isolated>false</Isolated>
    <EmbedInteropTypes>true</EmbedInteropTypes>
  </COMReference>
</ItemGroup>
```

## 前置：逻辑站号

`ActLogicalStationNumber` 不是 PLC 站号，是 MX Component「通信设置向导」里配的逻辑站号（绑定传输方式：USB / 以太网 / 串口 + 目标 PLC 型号）。**程序里的站号必须与向导配置一致**，`Open()` 返回非 0 的第一位原因就是这里没配或对不上。MX Component 的安装与向导配置不在本 skill，只覆盖程序侧。

## 连接与关闭

```csharp
using ActUtlType64Lib;

private ActUtlType64? plc;
private bool isOpen;

private bool Connect(int logicalStationNumber)
{
    plc = new ActUtlType64 { ActLogicalStationNumber = logicalStationNumber };
    int result = plc.Open();        // COM 风格：返回 0 = 成功，非 0 = 错误码
    isOpen = result == 0;
    return isOpen;
}

private void Disconnect()
{
    if (plc is not null) { plc.Close(); isOpen = false; }
}
```

- 返回值判错是惯例：`result == 0` 成功；非 0 按三菱错误码表查（手册附录），程序里至少区分「连接失败」并提示站号
- COM 对象释放：进程退出前 `Close()`；频繁重连场景让 GC 兜底即可，别手动 `Marshal.ReleaseComObject`（双释放崩溃）

## 单点读写（软元件地址字符串）

```csharp
// 读：out 参数返回
int[] data = new int[1];
int result = plc.GetDevice("D100", out data[0]);      // D100（16 位有符号）
result = plc.GetDevice("M10", out data[0]);           // 位软元件 → 0/1

// 写
plc.SetDevice("D100", 1234);
plc.SetDevice("M10", 1);
```

常用软元件：`D`（数据寄存器，16 位）、`W`（字）、`M`（内部继电器位）、`B`（链接继电器位）、`X/Y`（输入输出位）。32 位值用 `GetDeviceBlock`/`D` 寄存器成对处理（DxD+1 组成 32 位，低字在前）。

## 批量读写

```csharp
// 顺序批量读：D100 起 10 点
int[] buffer = new int[10];
int result = plc.ReadDeviceBlock("D100", 10, out buffer[0]);

// 随机批量读：地址清单（换行分隔）
string addresses = "D100\nM10\nD200";
int[] values = new int[3];
result = plc.ReadDeviceRandom(addresses, 3, out values[0]);

// 顺序批量写
int[] writeData = { 1, 2, 3 };
result = plc.WriteDeviceBlock("D100", 3, ref writeData[0]);

// 随机批量写
result = plc.WriteDeviceRandom("D100\nD200", 2, ref writeData[0]);
```

- `ReadDeviceBlock(addr, 点数, out buffer[0])`：**点数与数组长度严格一致**；out/ref 传的是首元素引用，靠点数决定 COM 侧写入多长——数组短了内存越界崩溃
- `ReadDeviceRandom` 地址字符串用 `\n`（或 `\r\n`）分隔，点数 = 地址条数；返回值顺序与地址顺序一致
- 位软元件混入随机读写时按 1 点计，值 0/1

## 监控轮询

与西门子同口径（见 references/threading-and-dispatcher.md）：后台线程轮询 + 快照 + `Dispatcher.BeginInvoke` 一次刷整幅画面；周期 500–1000ms 起。注意：
- COM 组件线程亲和：`ActUtlType64` 在哪个线程创建就在哪用；轮询线程里创建自己的实例，别跨线程共享
- `Open()` 同步返回（内部有超时），放后台线程跑避免冻 UI

## 与西门子接入的选型差异

| | 西门子（S7netplus） | 三菱（MX Component） |
|---|---|---|
| 形态 | NuGet 包，纯 .NET | COM 组件，需装 MX Component + 位数匹配 |
| 错误模型 | 异常 / IsConnected | 返回码 0 = 成功 |
| 批量读 | `ReadMultipleVars` + DataItem 类型头 | `ReadDeviceBlock`（int16 数组为主） |
| 连不上去先查 | CPU 访问保护（PUT/GET） | 逻辑站号与通信设置向导一致 |
