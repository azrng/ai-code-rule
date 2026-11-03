### .NET 并行隔离
- 测试：`dotnet build <测试项目.csproj> -o artifacts/test/<任务名>` 后 `dotnet vstest <测试程序集.dll>`（该输出缺配置与 wwwroot，不能直接运行）；运行验收：`dotnet build <入口项目.csproj> -p:OutputPath=artifacts/run/<任务名>/`（`-p:OutputPath` 相对项目文件目录解析，需落仓库根时改用绝对路径，否则产物落在项目目录下的 artifacts 而非仓库根约定位置）；`dotnet publish -o` 仍写默认 `bin`，不能绕过输出锁。隔离只改输出目录（`-o` / `OutputPath`），保持项目默认 `obj` 中间目录，不自定义 `BaseIntermediateOutputPath`（会引发重复生成代码）。
- 启动隔离实例时确认 `ContentRoot` 与迁移脚本等按物理路径读取的资源指向预期；直接 `dotnet <dll>` 时 `ContentRoot` 默认为当前工作目录。
- 冒烟实例的用户目录隔离不得依赖环境变量：Windows 下 `Environment.GetFolderPath` 走 Shell Known Folder API、**不读 `APPDATA` 等环境变量**——前缀注入即便成功传入进程也不生效（bash / PowerShell 同理），实例照读真实用户目录的配置（错端口、污染真实数据根）。可靠口径是「备份真实配置文件 → 注入隔离配置（独立数据根与端口）→ 冒烟 → 还原备份」；环境变量方式不得作为隔离依据。
