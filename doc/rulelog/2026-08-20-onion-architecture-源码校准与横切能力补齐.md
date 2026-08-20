# onion-architecture 源码校准与横切能力补齐

- 修改时间：2026-08-20
- 修改文件夹：`skills/dotnet-skills/onion-architecture`
- 修改原因：以 `C:\Work\gitee\nuget-packages` 中 `Azrng.Core`（1.20.0）与 `Azrng.AspNetCore.Core`（1.4.0）源码为事实来源校准现有 skill，发现并修正失实项（ForbiddenException 状态码映射写反），同时补齐源码已有而 skill 未覆盖的横切能力。执行依据为本仓库 `doc/skillsDoc/onionArchitectureSkills.md` 任务书。
- 主要修改内容：
  - 失实修正：`ForbiddenException` 由 401 修正为 403（已认证但无权限），补 `UnauthorizedException` → 401（未认证）；补充 `BaseException` 派生与默认 500 兜底行为、`RetryMarkException` 不参与 HTTP 映射的说明。
  - 缺口补齐：SKILL.md 新增 `AddDefaultControllers` 一站式注册路径（`CommonMvcConfig` 三开关）、`RegisterBusinessServices` 注册规则（双重载/全业务接口注册/默认忽略 Microsoft.、System.）、分页动态排序（`GetPageSortRequest` + `OrderBy(SortContent[])`）、周边能力包速查表（六包一句话指路）、横切中间件指路（审计日志/RequestId/CORS/ICurrentUser）、反模式两行（401/403 混用、手写基础设施封装）。
  - 新增 references：`result-packing-and-exceptions.md`（包装分支全表、异常家族与状态码映射、ResultModel/ResultModelFactory/Extensions 契约、ModelVerifyFilter 错误结构与校验特性）、`program-setup-and-crosscutting.md`（注册规则、完整装配模板与中间件顺序、AuditLogOptions 全表、RequestId、CORS 家族、LongToStringConverter、ICurrentUser 自实现示例）。
  - 元数据：SKILL.md 版本 1.0.0 → 1.1.0，补 `last_updated` 字段；layer-responsibilities.md csproj 示例 Azrng.Core 1.19.0 → 1.20.0。
  - 全部 API 名称与行为均回源码逐一核实（过滤器分支 switch、异常映射 switch、注册方法签名、特性命名空间等）。
  - 验收测试（D:\temp\yangcong 六项目洋葱解决方案，InMemory provider + ProjectReference 直连源码）：dotnet build 0 错误；运行时验证三件套成功/400/404 包装、401/403 双路径、分页四件套、动态排序、CollectionNotEmpty/MinValue 校验、X-RequestId 全部符合预期。测试发现并修复两处 skill 失实：①`IBaseRepository` 无 `AddRangeAsync`，批量新增是 `AddAsync(IEnumerable<TEntity>)` 集合重载（onion-architecture 与 azrng-efcore 两个 skill 同步修正，azrng-efcore 升 1.2.1）；②`SortContent` 无无参构造，动态排序参数必须走 POST body JSON，GET query 绑定会 500（SKILL.md 与 references 已补坑说明）。

| 子任务 | 状态 | 说明 |
| --- | --- | --- |
| `skills/dotnet-skills/onion-architecture` | 已完成 | SKILL.md 修订与两个新 references 已完成；已回读检查旧表述（Forbidden→401）无残留。分层规则基线（Core/IDomain/IApplication 等）按任务书约定未改动。 |
