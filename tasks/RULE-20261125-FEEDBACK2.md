# RULE-20261125-FEEDBACK2：迁移脚本目录安全冲突 + 任务孤儿档滞留反馈批

承接目标项目 2 条反馈，升版 3.22.0 → 3.23.0。

## 反馈 #1（2026-11-25，冲突）：wwwroot/MigrationSql 与静态服务组合成匿名下载泄露面

- 原始证据：Rbac 项目按《代码审计.md》74 项标准审查，`Startup.cs` 模板惯例装配 `UseDefaultFiles(); UseStaticFiles();`，迁移脚本按规则置于 `wwwroot/MigrationSql/1.0.0.sql`；静态中间件在认证 / 授权之前且默认无路径限制，同构最小宿主实测 `GET /MigrationSql/1.0.0.sql` 匿名 200 拉走全库表结构、种子数据与口令哈希（审计中危缺陷 10）。项目实际无前端资源进 wwwroot，静态中间件纯模板残留。
- 判定：**采纳**。规则把迁移目录与 Web 静态根划在同一物理目录，照抄模板每次复现泄露。
- 修改明细：
  - `shared/backend/db-migration.md`：目录口径 `Service/wwwroot/MigrationSql/` → `Service/MigrationSql/`；新增「迁移脚本目录红线」（不得位于 wwwroot 或任何静态服务根之下 + 机制说明 + wwwroot 只放前端静态资源 + 存量暂不能挪的最低缓解）与「发布产物必含声明」（Web SDK 默认发布集只含 `wwwroot/**` 与配置文件，非 wwwroot 的 `.sql` 不进 build / publish 产物，宿主 csproj 必须 `<Content Include="MigrationSql/**" CopyToOutputDirectory="PreserveNewest" CopyToPublishDirectory="PreserveNewest" />`，脚本根按 `ContentRootPath` 相对解析）——反馈实测依据：默认 publish 无 MigrationSql，加 Content 声明后实测进 build 与 publish
  - `arch/dotnet-layered/backend-AGENTS.md`：目录结构表 `Service/wwwroot/MigrationSql/` → `Service/MigrationSql/`
  - `arch/dotnetBlazor/backend-AGENTS.md`：内联副本同步补红线 + 发布声明（`Web/MigrationSql/` 本就在 wwwroot 外，防挪入 + 补发布缺口）
  - `arch/reactLinkdotNet-ai/backend-AGENTS.md`：内联副本同步补两条（EF Core Migrations 场景显式排除于发布声明约束外）
  - 其余架构（dotnet-core-single、reactLinkdotNet、vueLinkdotNet、web-dotnet skeleton）经 include shared 片段自动传导
- 反向语义检索声明（门控类修改义务）：全仓 grep「静态 / 可选 / 豁免 / 跳过 / 简化」×迁移语境——①dotnetBlazor「数据库迁移规则（可选）」的可选指是否接库使用 Azrng.SqlMigration，不豁免目录红线，接库即受约束；②reactLinkdotNet-ai 双轨优先级（SQLite/EF Core 优先），选择 Azrng.SqlMigration 即受约束，EF Core Migrations 场景已在条款内显式排除；③db-migration.md「骨架必装」不要求当下存在业务 SQL，不豁免目录与发布约束；④「存量暂不能挪目录」为条款内显式缓解路径（静态根收窄 / 移除残留静态中间件），非漏洞。`dotnetBlazorWebApp/ui-AGENTS.md`「服务端只提供静态资源」指前端产物，与红线无冲突。无其他允许静态服务迁移目录的条款。
- 传导面实测：`--emit` 全样例后「迁移脚本目录红线」与「发布产物必含声明」各落地 11 个 backend-AGENTS 产物（dotnet-layered、core-single、reactLinkdotNet / -ai / Single、vueLinkdotNet / Single、dotnetBlazor / -ai、dotnetBlazorWebApp / -ai），`wwwroot/MigrationSql` 产物 0 残留。
- 回执口径：规则版本 3.23.0；Rbac 项目采纳时需同步 `Startup.cs` 脚本根路径（挪出 wwwroot）、`Rbac.Service.csproj` Content 声明、`doc/design/rbac/rbac.md` L37/L62 与 `tasks/T-001.md` 目录记载，并处置存量泄露（脚本目录迁移后 wwwroot 内旧文件删除或静态中间件移除）。

## 反馈 #2（2026-11-11，违反）：任务档滚动淘汰执行不完整留下孤儿档

- 原始证据：T404 收尾滚动删除 T397-T400.md 时发现 T394-T396.md 仍在——其 TASK.md 行已被更早会话滚动剔除出表（e39a23a 只收了 T389-T393 档案），文件未同批删除；建议置 DONE 自检追加 `ls tasks/` 与表内 ID 比对。
- 判定：**已修复，无需变更**。反馈发生时规则版本 3.3.1；两天后 `9fc7078`（2026-11-13，RULE-20261113-ROLLBACK 批，升版 3.4.1）承接同类反馈新增「任务收尾与滚动一致性」条款——置 DONE 前核对两表、表满同批滚动、`ls tasks/` 与表格反向核对、孤儿文件同批删除、逐项报出删除清单即视为已获授权。反馈建议逐项已覆盖（「先核对批次结论已被吸收」由「移入最近完成表即转只读（收尾时有价值的坑位在此窗口内吸收）」的吸收窗口覆盖）。本条属滞留未清理的待处理条目。
- 同类问题独立任务数：2（RULE-20261113-ROLLBACK 承接的滚动堆积反馈 + 本条孤儿档反馈，同一生命周期缺口，同一修复覆盖）。
- 回执口径：目标项目 `--upgrade` 至 ≥3.4.1 即获得该条款；存量孤儿档（T394-T396.md）规则只防新增不自动清理，需目标项目按新条款反向核对一次性删除（删除前确认批次结论已被提交信息 / 记忆吸收）。

## 验证

- `--check` 39 兼容样例全绿；`dotnet test` 280 测试通过；emit 基线刷新 225 项全量轮换（版本戳入哈希 + 受影响产物内容哈希），基线 diff 即本批产物审查清单。
