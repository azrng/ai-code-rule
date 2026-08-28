### 数据库访问库
- `Common.EFCore`：EF Core 基础封装（必须安装，所有 Provider 都依赖它）
- 根据项目使用的数据库，按需选择以下其中一个 Provider 包：
  - `Common.EFCore.PostgresSql`：PostgreSQL（默认推荐；注意实际 NuGet 包 id 拼写为 `PostgresSql`）
  - `Common.EFCore.MySQL`：MySQL / MariaDB
  - `Common.EFCore.SQLServer`：SQL Server
  - `Common.EFCore.SQLite`：SQLite
  - `Common.EFCore.InMemory`：内存数据库（仅用于测试）
