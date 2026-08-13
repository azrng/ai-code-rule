# RuleAssembler — 交互式 AI Rule 拼接器

把一套完整 AI rule 释放到当前目录：交互选择技术大类 → 架构模板，确认后输出 AGENTS.md + 子规则 + 入口指针（CLAUDE/GEMINI/GROK），**不输出 TASK.md**。

## 工作原理

拼接器把各模板的共同内容抽成**公共片段**（`fragments/root/`、`fragments/pointers/`），按架构填入**差异片段**（`fragments/arch/<架构>/` 子规则整文件 + `manifest.json` 里的变量槽）。修改公共规则只改片段源一处，重新构建后所有架构生效——根治"改一处改一堆"。

## 片段源结构

```
fragments/
├── root/AGENTS.md            # 根 AGENTS 骨架，含 {{变量槽}}
├── pointers/                 # CLAUDE/GEMINI/GROK 指针（逐字公共）
├── arch/<架构>/*.md          # 各架构的子规则整文件（backend/doc/infra 等）
└── manifest.json             # 架构配方：变量槽取值 + 子规则清单 + 指针 + 大类
```

`manifest.json` 的变量槽：`{{version}}` `{{last_updated}}` `{{target-list}}` `{{subrule-index}}` `{{dev-stage}}` `{{understanding-checklist-backend}}` `{{contract-nouns}}` `{{search-nouns}}`。

## 构建

```powershell
./publish.ps1
# 产物：app/RuleAssembler/bin/Release/net10.0/win-x64/publish/use-rule.exe（约 260KB，框架相关）
```

框架相关单文件，目标机需有 .NET 运行时。如需自包含（零依赖、约 60MB），改 `publish.ps1` 为 `--self-contained true`。

## 使用

```powershell
# 把 use-rule.exe 拷到目标项目根目录，运行：
./use-rule.exe
# 1) 选技术大类  2) 选架构模板  3) 确认释放  → 当前目录出现完整一套规则
```

目标目录已有同名文件时会提示覆盖（y/N/all）。

## 已接入架构（Phase 1）

- dotnet-layered（.NET 分层 API 后端）
- dotnet-core-single（.NET 单层 API 后端）

其余架构（python / dotnetConsole / 桌面移动家族 / web 前后端分离 / 静态）按计划分阶段接入。新增架构：在 `fragments/arch/<id>/` 放子规则整文件，在 `manifest.json` 加一条配方即可。

## 等价性验证

dotnetLayered 生成产物与现有 `dotnetLayered/` 目录**七个文件字节级完全一致**；dotnetCoreSingle 生成产物与现有目录的差异**仅为 1.10→1.11 基准统一**（版本号、判断标准精简、重构流程精简、引号规范化），无术语错位。
