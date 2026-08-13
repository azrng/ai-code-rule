## [2026-08-13]：交互式 AI Rule 拼接器（RuleAssembler Phase 1 MVP）

跟踪状态表：

| 文件夹名称 | 当前子任务状态 | 备注 |
|-----------|----------------|------|
| （仓库级工具，非某模板） | `Phase 1 已完成` | dotnet 后端家族已接入并验证等价 |

### 修改原因

痛点：改一处公共规则要在 19 个模板各自重复改一遍。目标（用户原话）：写一个工具，拷到目标目录、交互选择后，把各模板的共同点汇总成公共片段、按架构拼上差异片段，当场释放完整一套 AI rule。这能根治重复——公共片段只维护一份，改一处全部生效。

### 修改内容

- **新增 `tools/RuleAssembler/`**：C# 控制台工程（`RuleAssembler.csproj` / `Program.cs` / `publish.ps1`）。`fragments/` 整目录作为 EmbeddedResource 打包；交互式 大类→架构→确认→释放到当前目录；UTF-8 写入、覆盖保护（y/N/all）、跳过 TASK.md。发布为框架相关单文件 exe（`--self-contained false` + `PublishSingleFile`，约 260KB，目标机需 .NET）
- **新增 `fragments/` 片段源**：
  - `root/AGENTS.md`：根 AGENTS 骨架，含变量槽 `{{version}}` `{{last_updated}}` `{{target-list}}` `{{subrule-index}}` `{{dev-stage}}` `{{understanding-checklist-backend}}` `{{contract-nouns}}` `{{search-nouns}}`
  - `pointers/`：CLAUDE/GEMINI/GROK 指针（逐字公共）
  - `arch/dotnet-layered/`、`arch/dotnet-core-single/`：各架构子规则整文件（backend/doc/infra）
  - `manifest.json`：架构配方——每个架构的变量槽取值 + 子规则清单 + 指针 + 大类归类
- **接入架构**：dotnet 后端家族（dotnetLayered、dotnetCoreSingle）。以 1.11 详版为基准：任务状态触发条件统一到详版，交付验证节结构对齐
- **`.gitignore`**：忽略 `tools/RuleAssembler/bin/`、`obj/`
- **`README.md`**：使用方式节新增「一键导出」说明，指向拼接器
- **`tools/RuleAssembler/README.md`**：工具说明（原理 / 结构 / 构建 / 使用 / 已接入架构）

### 等价性验证（关键）

- **dotnetLayered**：生成产物与现有 `dotnetLayered/` 目录**七个文件（AGENTS / backend / doc / infra / CLAUDE / GEMINI / GROK）字节级完全一致**，diff 全空
- **dotnetCoreSingle**：指针完全一致；backend/doc/infra 仅版本号 1.10→1.11；root AGENTS.md 差异**全部为基准统一**（版本号、判断标准精简、重构决策流程精简、引号规范化、行序），**零术语错位**——变量槽成功保留了 core-single 自己的 AppService/数据访问/DomainService 术语
- 未释放 TASK.md（符合设计）
- 覆盖保护：目标已有同名文件时提示 y/N/all，不静默覆盖

### 设计决策

- **拼接粒度分层**：根 AGENTS 用章节级（公共骨架 + 变量槽）；子规则正文（backend/doc/infra）用文件级（每架构整文件，不拆碎——拆碎收益低、维护反升）；指针段落级公共
- **片段源作单一事实源**：未来迁移后现有 19 个模板目录不再手维护，改为生成产物。Phase 1 先保留现有目录，diff 验证等价后再决定删除
- **exe 形态**：框架相关轻量版（~260KB，需 .NET），已确认接受

### 修改文件

- 新增：`tools/RuleAssembler/{RuleAssembler.csproj,Program.cs,publish.ps1,README.md}`、`fragments/{root/AGENTS.md,manifest.json,pointers/*,arch/dotnet-layered/*,arch/dotnet-core-single/*}`
- 修改：`.gitignore`、`README.md`

### 风险

- 低：dotnetLayered 已字节级验证；dotnetCoreSingle 差异均为预期基准统一
- Phase 2/3 接入其他家族（python / dotnetConsole / 桌面移动 / web / 静态）时，需按各架构补 manifest 配方与子规则整文件；doc/infra 可能需进一步参数化（布尔开关）

### 备注

- 本次未自动提交，等待用户确认
- 现有 19 个模板目录 Phase 1 保留未动；等价性已验证，后续可决定是否删除旧目录、改为片段源为唯一来源
- AI：Claude
