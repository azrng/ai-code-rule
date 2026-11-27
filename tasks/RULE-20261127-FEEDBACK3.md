# RULE-20261127-FEEDBACK3 雪花 id 线上类型契约反馈批

承接日期：2026-11-27；版本 3.24.0 → 3.25.0（与同日并行会话的 Angular 组件批共享同一版本号，同一次基线刷新覆盖两批）。反馈 1 条，来自目标项目（React + .NET 分离 + AI 能力组合，reactLinkdotNet-ai 形态）`.ai-rule-feedback.md` 回传。

## 反馈判定（1/1 采纳）

### [2026-11-27] 缺少「long 主键 JSON 序列化为字符串」全局约定 —— 采纳（缺失）

- 原始场景：智能标注 / 大模型评测阶段 0 契约草案把雪花 id（`long`）写成 `long→number`，前端按 `number` 全链路实现；浏览器 E2E 联调时 `jobId` 经 `JSON.parse` 精度丢失（`…544` → `…500`），选中错误任务并级联导致结果区 / 导出 / 删除全部失败，排查耗时长。项目侧已自行修复（DTO 标 `[JsonConverter(typeof(JsonConverterLong))]` + 前端类型改 string + 契约文档补口径）。
- 判定依据：`git log -S "JsonConverterLong" / "2^53" / "精度丢失"` 全仓核对——规则层（fragments/）从未落过此口径，仅 onion-architecture 技能可选承载且措辞是「需要 long→string 时」；技能不导入即不受约束，属 B 组「基线错放技能」同型缺口。归属测试「同一栈的每个项目都希望它无条件生效」成立 → 上移规则。
- 根因核实（包源码 `C:\Work\gitee\nuget-packages` master）：`JsonConverterLong` 仅定义在 `Azrng.Core.NewtonsoftJson.ContractResolvers`；`Azrng.Core.Json`（STJ）与 `Azrng.AspNetCore.Core` 均为 `LongToStringConverter`；全部包源码无任何 MVC JSON 管线默认接线——转换器全是「要用才生效」，默认出数字，反馈根因成立。

## 落地

### 规则层（fragments，升版随 manifest 3.25.0）

- 新建 `shared/backend/id-wire-type.md`「雪花 id 线上类型」：响应 id 类 long 字段（含 `long?`）线上必须是字符串；DTO 属性保持 long + 属性标注包内置转换器（STJ 用 `Azrng.Core.Json` 的 `LongToStringConverter`，Newtonsoft 栈用 `JsonConverterLong` / `LongToStringConverter`）；不同族静默失效警示；全局注册为替代路径（写法指 onion 技能，门控句自含）；同一项目只用一种方式。
- 新建 `shared/doc/contract-id-type.md`「雪花 id 的契约类型」：CON 草案与接口说明字段表记 `string` 不记 `number`；Swashbuckle 不反映转换器、按 Swagger 校准契约时以实际响应为准。
- 挂载（include，7 个 .NET 组合，backend×7 + doc×7 产物）：
  - backend：`arch/reactLinkdotNet/backend-AGENTS.md`、`arch/vueLinkdotNet/backend-AGENTS.md`、`shared/skeletons/web-dotnet/backend-single-AGENTS.md`（react/vue Single 两样例共用）、`arch/reactLinkdotNet-ai/backend-AGENTS.md`、`arch/dotnet-layered/backend-AGENTS.md`、`arch/dotnet-core-single/backend-AGENTS.md`
  - doc：`arch/reactLinkdotNet/doc-AGENTS.md`（react/vue layered+single 四样例共用）、`arch/reactLinkdotNet-ai/doc-AGENTS.md`、`arch/dotnet-layered/doc-AGENTS.md`、`arch/dotnet-core-single/doc-AGENTS.md`
- 前端侧条款（TS 类型 string、禁 Number()/parseInt、契约未声明先按 string 建模）由并行 Angular 批新建的 `shared/frontend/contract-type-rules.md` 承载（挂 reactLinkdotNet / vueLinkdotNet / reactLinkdotNet-ai / reactOnly / vueOnly 五源，emit 传导 12 产物，含 reactLinkPython / vueLinkPython 桥接组合）——本批不在前端文件重复落同口径，避免双写。

### 技能层（skills，走技能变更日志；规则版本号随发布携带）

- `onion-architecture/SKILL.md`「JSON 序列化」易踩坑改硬约束口径（对齐规则层），补两个实测坑。
- `references/paging-and-json.md`：改硬约束；补逐属性标注代码示例；补两坑与 Swagger 偏差提示；保留自定义转换器句。

### 实测证据（写进条款前逐一验证，非转抄反馈）

- 包行为探针（`Azrng.Core.Json` / `Azrng.Core.NewtonsoftJson` 1.3.1，net10.0 控制台）：STJ `LongToStringConverter` 属性标注与全局注册均覆盖 `long` / `long?`，入参兼容字符串与数字，全局另覆盖集合；Newtonsoft 两个转换器属性标注均正常。
- MVC 宿主探针三模式：默认 STJ 管线里 Newtonsoft `JsonConverterLong` 属性标注**不生效也不报错**（id 照旧出数字）；`AddNewtonsoftJson` + `Converters.Add(LongToStringConverter)` 漏 `long?`（可空外键仍是数字）；`CustomContractResolver` 全覆盖（long / long? / 集合 / camelCase）。
- Swagger 探针（Swashbuckle.AspNetCore 10.0.1 最小宿主）：属性标注与全局注册模式下 swagger.json 均仍展示 `integer/int64`——契约以 Swagger 为最终事实来源的现行条款会被反向带偏，故 doc 片段补「以实际响应为准」句。`Azrng.Swashbuckle` 0.5.0 无 SchemaFilter，团队默认栈无兜底。

## 明确不挂载面（反向语义检索声明）

- Blazor 两组合（dotnetBlazor / dotnetBlazorWebApp）：前端是 .NET，无浏览器 `JSON.parse` 数值边界，不挂。
- dotnetWorker / dotnet-nuget-library / dotnetConsole：无对外 HTTP JSON 面（worker / 类库 / 控制台），不挂。
- Electron + .NET sidecar（electron-backend-dotnet）：渲染进程同为 Chromium 理论同险，但骨架无雪花 id 约定（自管 SQLite + STJ，无 Azrng 实体基类），暂不挂；后续该骨架若引入雪花 id 生成再补。
- Python / Node / Go / Rust 后端：模板无雪花 long 主键约定（Python 常见自增 / uuid，Node number 即 double），前端通用条款已覆盖消费侧，后端侧不挂。
- 全仓「豁免 / 可选 / 简化」×序列化语境检索：response-format / swagger / time-contract-rules 无任何「允许 long 出数字」的反向条款；`swagger.md` 无类型改写语义；无冲突。

## 验证

- `--check` 40 兼容样例全绿（含并行批新增 angularOnly）；`dotnet test` 293/293 全绿。
- 测试硬编码样例计数 39→40 同批修正（Unit1 / ComboRegression / EmitBaseline / RootConsistency 四处，随并行批新增样例的必改项）。
- emit 基线刷新：225 项全量版本轮换 + angularOnly 9 项新增（234 insertions / 225 deletions），无其他意外漂移；基线 diff 即本批 + Angular 批产物审查清单。
- 传导面实测（隔离缓存 `USE_AI_RULE_PACK_CACHE` 后 `--emit` 40 样例）：「雪花 id 线上类型」7 个 backend-AGENTS 产物、「雪花 id 的契约类型」7 个 doc-AGENTS 产物，与计划挂载面一致；抽查 reactLinkdotNetSingle 产物内容与标题层级正确。

## 回执口径

- 采纳，规则版本 3.25.0。反馈中的建议（CON 字段表记 string + DTO 标注转换器 + 阶段 0 / 阶段 1 同步遵守）全部落入；额外补反馈未提的三点：契约文档侧 Swagger 偏差提示、转换器与 MVC 序列化器不同族的静默失效坑、Newtonsoft 全局注册漏 `long?` 坑。
- 目标项目侧确认要点：其修复用 `[JsonConverter(typeof(JsonConverterLong))]`（Newtonsoft 族）——若该项目 MVC 未走 `AddNewtonsoftJson` 则标注无效，请核对实际管线族别（默认 STJ 应改用 `Azrng.Core.Json` 的 `LongToStringConverter`）；可空外键（`long?`）同样标注；前端由 `--upgrade` 获得「契约类型建模」条款。
- 回执后目标项目删除 `.ai-rule-feedback.md` 对应条目；下次 `--upgrade` 即获得全部三层条款。
