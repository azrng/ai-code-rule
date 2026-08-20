# BootstrapBlazor UI 审查能力补强

- 修改时间：2026-08-20
- 修改文件夹：`skills/dotnet-skills/bootstrap-blazor`
- 修改原因：现有 skill 擅长校验 BootstrapBlazor 10.9.2 API，但缺少对表单校验、异步写入防重、页面状态、无障碍与响应式交互的审查路由，容易让“能编译”的问题漏网。
- 主要修改内容：新增按需加载的 `references/ui-audit.md`；入口加入 UI 审查模式；将 code-behind 从强制结构调整为遵循项目既有约定的默认建议；Skill 版本升至 10.9.3，BootstrapBlazor API 基线仍为 10.9.2。

| 子任务 | 状态 | 说明 |
| --- | --- | --- |
| `skills/dotnet-skills/bootstrap-blazor` | 已完成 | 审查清单与入口路由已补齐；已完成 UTF-8 回读、引用完整性与空白差异检查。内置 validator 不支持本仓库既有且必需的 `version` / `last_updated` 元数据，未据此删改元数据。 |
