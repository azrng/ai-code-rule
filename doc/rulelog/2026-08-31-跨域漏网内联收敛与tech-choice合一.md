# [2026-08-31]：跨域漏网内联收敛、tech-choice 副本合一与 infra 整文件合并，版本 1.21.0

跟踪状态表：

| 对象 | 当前子任务状态 | 备注 |
|-----------|----------------|------|
| 检测基线 | `已完成` | 对 `fragments/arch/`（955 节）× `fragments/shared/`（117 节）重跑四类检测：跨 arch 节级逐字节相同 7 组、节级近似对 206、整文件相似 12 对、arch 内联节 ≈ 既有 shared 片段（漏网）116 对；逐对读取实际差异做语义判定后再分类执行 |
| `shared/infra/docker-delivery-web.md`（新建） | `已完成` | 抽取 react/vue LinkdotNet infra 逐字节相同的 `### Docker 交付规则`（含健康检查、`.dockerignore` 行） |
| `shared/backend/code-organization-dotnet.md`（新建） | `已完成` | 抽取 react/vue LinkdotNet backend 逐字节相同的 `### 代码组织规范`（C# 风格；既有 code-organization.md 为 Go 风格，由 wails 双架构引用） |
| `shared/backend/application-layer.md`（新建） | `已完成` | 抽取 react/vue 的 `### Application 层规则`（6 条 bullet 仅顺序不同，序列比对漏检、人工比对发现；顺序收敛到 react 版：AutoMapper → 主构造函数 → 业务异常） |
| `shared/application/kiss.md` 升级 | `已完成` | 吸收 dotnet-layered 内联版的 2 条独有 bullet（单调用点判定、提交前自检），全体 kiss 引用方（react/vue/blazor/layered/core-single/electron/dotnetConsole/maui/wpf/avalonia/react-ai）同步受益；layered、react-ai 换 include（引号统一弯引号） |
| `shared/backend/redis-cache.md` 升级 | `已完成` | 吸收 core-single 的 `RedisCacheOptions` 配置细节；layered、core-single 内联版换 include（句号收敛） |
| `shared/backend/controller-layer.md`、`response-format.md` 标题条件化 | `已完成` | 吸收 blazor 的“（如提供 API 接口）”条件措辞（沿用 2026-08-30 轮 bullet 级条件化先例）；blazor 的 Controller 层规则、Domain 层规则、统一响应格式三处内联换 include（句号收敛、response 补 `/ AppService`） |
| `shared/infra/tech-choice.md` 副本删除 | `已完成` | 与 `application/tech-choice.md` 内容逐字节相同、仅标题层级差异（`##` vs `###`）；统一收敛到 `###` 版（嵌套形态，core-single infra 已是此形态），14 处 include 重指向，标题层级 `##`→`###` |
| tech-choice 内联文本清零 | `已完成` | 全仓检索发现 16 个文件 17 处“如果仓库已经有真实实现…”两行内联漏网（含 reactWails frontend 同文件重复块、electron/reactTauri backend 的拆散两半），全部换 include；`di-rules.md` 内嵌的同源 2 行一并剥离；vue LinkdotNet frontend 缺失 tech-choice，按 react 镜像补齐 |
| core-single/layered infra 整文件合并 | `已完成` | 两文件除 tech-choice 一行外逐字节相同（相似度 0.994）；layered infra 补 tech-choice 后 manifest 将 dotnetCoreSingle 的 infra source 跨架构指向 `arch/dotnet-layered/infra-AGENTS.md`（沿用 python→dotnet-layered、dotnetBlazorWebApp→dotnetBlazor 先例），删除 core-single 副本；emit 断言两 Profile 的 infra 输出逐字节一致 |
| frontend 主动建议四处收敛 | `已完成` | react/vue/electron/tauri frontend 内联 `## 主动建议规则` 均为 `shared/frontend/advisory.md` 的漂移版（vue/electron/tauri 缺“表单校验缺失”bullet、缺 schema/视觉 token 措辞，electron/tauri 另有游离“技术债务”行）→ 全部换 include 收敛到完整版 |
| doc 域漏网 | `已完成` | reactWails 的 requirement（错引号+“页面或模块清单”旧措辞）与 retro-format（短措辞）换 include；flutter 的 requirement（同上）换 include；均获得“页面、接口或模块清单”合并版措辞与规范弯引号 |
| 其余漏网换 include | `已完成` | nextFullstack infra 本地数据安全规则（逐字节相同）、wpf ui 外部依赖与数据（逐字节相同）、react-ai backend 可维护性自检（引号）、react-ai backend 统一响应格式/异常处理（include + AI 专属行保留为模板行）、react/vue Wails frontend 前端测试（可选）（标题去“（可选）”、bullet 补“前端”前缀）、vue LinkdotNet backend 依赖注入规范（内联旧版换 di-rules.md 完整版） |
| 渲染输出变化的 41 个模板 | `已完成` | version 1.21.0、last_updated 2026-08-31；manifest vars 1.20.0→1.21.0 |

## 修改原因

接续 2026-08-27/30/31 三轮去重后，重新对 `fragments/arch/` 做节级精确/近似重复检测、整文件相似度分析，并新增“arch 内联节 vs 既有 shared 片段”漏网检测（上一轮 doc 模板的漏网问题说明该类别存在系统性缺口）。检测结果逐对读取实际差异、按语义判定真重复 / 真实变体后分类执行；另发现 shared 片段自身存在 tech-choice 逐字节双副本、以及 dotnet-core-single 与 dotnet-layered 的 infra 整文件近全同（0.994）。

## 明确不合并（真实变体，备查）

- 数据库迁移规则路径变体：react/vue（`API/MigrationSql/`）、layered（`Service/MigrationSql/`）、blazor（`Web/MigrationSql/`）、core-single（项目根 `MigrationSql/`）、react-ai（SQLite/PostgreSQL 双 Provider）——宿主项目布局真实差异
- 代码注释规范角色变体（2026-08-30 已记）：layered（IApplication/IDomain）、core-single（AppService/DomainService/Adapter）、electron（IPC Handler）、react-ai（端点+模型密钥）
- backend 主动建议规则各栈名词变体：layered/core-single/blazor/python/generalProject/reactWails/react-ai（Controller/AppService、路由与服务、Handler 与 Service 等角色词真实）；maui infra advisory 的“平台服务”（MAUI 专属）；flutter frontend advisory 的 Widget/Provider 名词
- blazor 保留变体：数据访问规则（可选）（EF 可选语义）、Redis（可选）（4 行子集）、异常处理（ErrorBoundary/SignalR）、依赖注入（`@inject`/SignalR 作用域）、Service 层规则（无 AutoMapper）；react-ai 保留变体：Redis（可选）、依赖注入（IChatClient）、提交前最小回归（端点+AI 行）、后端测试（AI mock）、路由/Zustand/代码组织（AI 增词）
- Docker 交付规则其余变体：core-single/layered（API 接口措辞，现随整文件合并单点维护）、blazor（Blazor 应用访问链路）、react-ai（模型调用链路行）、nextFullstack（Next.js 服务措辞）
- electron/reactTauri 平台语义对（IPC Handler vs Tauri Command、`.axaml` vs `.xaml` 等）：测试规则、本地能力、分层依赖、Repository 规则、异常处理、签名打包、stage0 平台边界、适用范围
- local-data-safety 变体：generalProject（安装目录/发布产物）、avalonia（桌面措辞）；wpf infrastructure 外部依赖与数据（更精确的“不可控外部服务/本地替身/手工残留数据库”措辞）
- general-requirements ×4（backend/frontend/ui/common 域尾行语义差异）、doc advisory ×3（DTO 契约/分层架构/设计系统）、doc-update 桌面版——2026-08-29/30 轮已确立的语义化变体
- avalonia/maui 桌面移动簇句号级 8 对（ViewModel 规则、MVVM、分层边界、DTO、统一结果包装、应用层测试、发布交付、数据库迁移 infra）——**留待下一轮专题收敛**（含 Pages/[Module]、axaml/xaml 少量真实差异需逐对判定量大，单独立轮保证验证质量）

## 主要修改内容

- 新建共享片段 3 个（docker-delivery-web、code-organization-dotnet、application-layer），删除重复副本 2 个（`shared/infra/tech-choice.md`、`arch/dotnet-core-single/infra-AGENTS.md`）；升级共享片段 5 个（kiss +2 bullet、redis-cache +配置细节、controller-layer/response-format 标题条件化、di-rules 剥离内嵌 tech-choice）
- 28+ 个 arch 模板内联节换 include；tech-choice 全仓统一为 `shared/application/tech-choice.md` 单一副本（44 处引用），内联文本与文件内重复块清零
- manifest：vars 1.21.0/2026-08-31；dotnetCoreSingle 的 infra subrule source 跨架构指向 dotnet-layered

## 验证

- `dotnet build` / `dotnet test`（4/4）/ `RuleAssembler --check`（20 Profile）全部通过（片段为编译期嵌入式资源，改后重建）
- 基线（HEAD worktree）与改后全量 `--emit` 内容级 diff 逐项核对：170 个输出文件一一对应（整文件合并未增减输出文件），45 个内容变化文件全部对应预期清单（tech-choice 标题层级/新增节、KISS 2 bullet 普惠、redis/响应格式/Controller 标题与措辞收敛、frontend advisory 四处对齐、doc 引号与清单措辞、wails 前端测试标题、layered 总体要求 3-bullet 完整版、Docker 块纯 include 零变化等），无意外变化
- 断言：core-single 与 layered 的 infra 输出逐字节一致；react/vue infra 均含 docker-delivery-web 片段正文
- 残留检索：被抽节原文本（react/vue 代码组织与 Application 层、layered KISS 单调用点措辞、electron/tauri/react/vue frontend 主动建议、reactWails/flutter requirement、reactWails retro、nfs 本地数据安全、wpf ui 外部依赖、tech-choice 内联文本）在 `fragments/arch/` 零残留；唯一“前后端核心访问链路”命中为 react-ai infra 自有 Docker 变体（有意保留）
