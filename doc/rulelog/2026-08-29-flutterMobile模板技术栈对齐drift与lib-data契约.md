# [2026-08-29]：flutterMobile 模板技术栈对齐（sqflite→drift、lib/models/→lib/data/、收敛 freezed/dio 默认推荐），版本 1.16.0

跟踪状态表：

| 对象 | 当前子任务状态 | 备注 |
|-----------|----------------|------|
| `fragments/arch/flutterMobile/backend-AGENTS.md` | `已完成` | 数据访问 sqflite→drift（DSL 查询、NativeDatabase/LazyDatabase、schemaVersion+MigrationStrategy 迁移）；网络降级为可选项；推荐目录结构改为 lib/data/ + lib/core/；平台服务接口路径与配置条目同步 |
| `fragments/arch/flutterMobile/application-AGENTS.md` | `已完成` | 核心库登记 drift/go_router、freezed/dio 改为按需引入；目录结构对齐 lib/providers + lib/data；DTO 规则改为 drift 实体 + lib/data/models/ 视图 DTO；阶段 2 入场要求同步 |
| `fragments/arch/flutterMobile/doc-AGENTS.md` | `已完成` | 接口契约唯一来源改为 lib/data/database/（drift 实体契约）+ lib/data/models/（视图 DTO）；登记 build_runner 重生成要求 |
| `fragments/arch/flutterMobile/ui-AGENTS.md` | `已完成` | 目录结构 models/services/repositories 收敛为 lib/data/；阶段 1 步骤与产物同步契约口径 |
| `fragments/arch/flutterMobile/design-system.yaml` | `已完成` | tech_stack.local_db sqflite→drift、http 标注按需；component_rules.file_placement 对齐 lib/data + lib/core + lib/features + lib/router 布局 |
| `fragments/manifest.json` | `已完成` | vars.version 1.15.0 → 1.16.0，last_updated 2026-08-29 |

## 修改原因

flutterMobile 模板仍停留在 sqflite + lib/models/ + freezed 旧栈口径，与该栈的实际演进（Riverpod + GoRouter + drift）不一致。该口径已在与模板同源的正式项目（暖仓 WarmPantry，仓库 D:\GitHub\MyItems）中完整落地验证：drift 2.20 实现数据层、实体契约由 drift 表定义与生成类承载、视图 DTO 放 lib/data/models/，静态检查与 35 项测试全绿。本次把模板收敛到经过实战验证的结构，避免新项目按旧模板起步即产生技术与文档偏差。

## 主要修改内容

- 数据访问：sqflite（openDatabase / onCreate+onUpgrade 回调 / fromJson 映射）→ drift（DSL / 参数化、NativeDatabase / LazyDatabase、schemaVersion + MigrationStrategy、生成数据类；对外 JSON 手动映射）
- 契约位置：`lib/models/`（entities/dtos 分层、freezed 推荐）→ `lib/data/database/`（drift 实体契约）+ `lib/data/models/`（视图 DTO）；表变更需重跑 build_runner
- 网络假设：dio/http 默认推荐 → 降级为「仅需要远程服务的项目」可选条目，纯本地 App 不引入
- 目录结构：repositories/services/config/utils 平铺 → `lib/data/{database,models,repositories,services}` + `lib/core/{theme,constants,utils}` + `lib/providers` + `lib/router` + `lib/features`
- 版本：四个规则文件 front matter 与 design-system meta、manifest vars 统一 bump（规则版本 1.16.0，2026-08-29）

## 验证

- 全量残留检索：`sqflite` / `lib/models` / `entities/` / `dtos/` 在 flutterMobile 目录零残留
- flutterMobile 引用的共享片段（scope/advisory/stage0-design-v2/doc-update/doc-directory/retro-format-v1/common-tasks-v2/exception-handling/general-requirements/external-dependencies/no-test-fallback/ui-general-requirements/mobile-release 系列）逐一检索，无本主题过时表述，未做改动
- 回读 backend-AGENTS.md 技术栈小节确认 UTF-8 中文与 CRLF 行尾正常
