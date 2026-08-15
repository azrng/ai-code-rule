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

`manifest.json` 的变量槽：内容级 `{{target-list}}` `{{subrule-index}}` `{{dev-stage}}` `{{understanding-checklist}}`（多行）`{{reading-order}}` `{{extra-sections}}`（可选整段，如 AI 家族的技术栈总览）；短语级（缺省回退纯后端家族默认值）`{{search-nouns}}` `{{contract-error-line}}` `{{root-scope-nouns}}` `{{context-scope-nouns}}` `{{high-risk-nouns}}` `{{high-risk-prefix}}` `{{task-kind-nouns}}` `{{evidence-nouns}}` `{{reuse-noun}}` `{{persistence-line}}` `{{destructive-nouns}}` `{{boundary-nouns}}` `{{rollback-scope}}` 等；追加型可空槽 `{{intro-quote}}` `{{model-behavior-extra}}` `{{verify-quote}}` 等，空值占位行自动清理。`designSystem: true` 时额外释放 `arch/<架构>/design-system.yaml` 为 `design-system.yaml`。

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

## 已接入架构（8 大类 19 架构，全部 1.12.0 基准）

| 大类 | 架构 |
| --- | --- |
| .NET 后端 | dotnet-layered（分层）、dotnet-core-single（单层） |
| Python 后端 | python-backend（FastAPI + uv） |
| Web 前后端分离 / 全栈 | reactLinkdotNet、vueLinkdotNet、reactLinkdotNet-ai（含 ai-AGENTS 与技术栈总览）、nextFullstack |
| 桌面应用 | avaloniaExe、wpfExe、dotnetBlazor、dotnetConsole |
| 跨平台混合桌面 | electronDotNetExe、reactTauriExe、reactWailsExe、vueWailsExe |
| 移动应用 | mauiMobile、flutterMobile |
| 纯前端 / 静态站点 | staticHtml |
| 通用项目 | generalProject |

新增架构：在 `fragments/arch/<id>/` 放子规则整文件，在 `manifest.json` 加一条配方即可。

## 等价性验证

19 个架构生成产物与仓库目录**全量 161 文件字节级一致**（哈希逐一比对）。各目录均已升级为 1.12.0 精简基准：根 AGENTS.md 由模板再生成，子规则保留技术栈内容、统一结构外壳（frontmatter 版本、标题去编号、删"阅读摘要"段）。

