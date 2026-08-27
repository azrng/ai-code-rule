## [2026-08-27]：RuleAssembler 规则源集中化与 Profile 模块化

跟踪状态表：

| 文件夹名称 | 当前子任务状态 | 备注 |
|---|---|---|
| （仓库级工具，非某模板） | `已完成` | 保留根目录 19 套历史快照；后续只维护 `fragments/` |

### 修改原因

原有 19 套架构目录需要重复维护，且简单的架构整文件配方难以支持技术栈、角色和可选能力的组合。目标是保留双击后的交互式选择体验，同时将规则源集中到 `fragments/`，由 Profile 和可复用模块生成目标项目规则。

### 主要修改内容

- 将 manifest 从 `architectures` 升级为 `profiles`，增加 `schema_version`、统一规则版本、行尾策略和模块定义。
- 增加 `core` 公共模块，统一根入口和 CLAUDE/GEMINI/GROK 指针；增加 `ai-capability` 模块作为可复用能力示例。
- 修正 Blazor Profile 的家族归类，补充 Python 片段源目录声明。
- 将架构规则片段统一到 `1.13.0 / 2026-08-27`，生成文件统一为 UTF-8 无 BOM 和 LF。
- 将生成器拆分为 manifest 模型、资源读取、manifest 校验、Profile 选择、规则渲染和事务写出等职责。
- 保留无参数启动的交互式流程；增加 `--check` 只读校验入口，不改变双击使用方式。
- 写出前先生成临时文件，提交失败时自动回滚，避免释放半套规则。
- README 改为以 `fragments/` 和 Profile 为维护入口；根目录 19 套目录仅作历史快照。

### 验证与状态

- `dotnet build app/RuleAssembler/RuleAssembler.csproj -c Release`：通过。
- `--check`：19 个 Profile、全部片段和输出路径校验通过。
- 交互式生成验证：覆盖 19 个 Profile，均成功生成预期文件数量。
- 本轮不删除根目录 19 套快照，由后续单独处理。

### 风险与后续

- 当前 Profile 的技术栈规则正文仍按 Profile 专属文件维护；新增组合优先使用模块，后续再按稳定章节逐步抽取重复正文。
- 发布仍为 Windows x64 框架相关单文件，目标机器需要对应 .NET 10 运行时。
- 指针文件保留自身独立版本，不跟随规则主版本强制对齐。

### 修改范围

- `fragments/manifest.json`
- `fragments/arch/**/*.md`
- `app/RuleAssembler/`
- `README.md`
- 本记录文件
