# AGENTS.md

## 项目说明

本仓库用于沉淀和维护不同技术架构下的 AI 协作开发规则模板。

- `fragments/` 是唯一维护源：`arch/<id>/` 维护各架构专属片段，`shared/` 维护跨架构共享小节（子规则通过 `<!-- include: shared/... -->` 引用，改一处同步全部 Profile），`manifest.json` 维护模块、家族与 Profile 配方
- 生成器 `app/RuleAssembler/` 按 Profile 把片段拼装为 `AGENTS.md`、`CLAUDE.md`、`GEMINI.md` 与 `backend-AGENTS.md`、`frontend-AGENTS.md`、`doc-AGENTS.md`、`infra-AGENTS.md`、`ui-AGENTS.md`、`application-AGENTS.md`、`infrastructure-AGENTS.md` 等子规则文件，部分 Profile 另含 `design-system.yaml`
- 组合内核（P2 阶段 2 已完成）：manifest 的 `components` / `constraints` / `compatibilityCases` 是维度组合（形态 + 壳 + 前端 + 后端）的事实来源，全部 29 个 Profile 已组件化并注册为兼容样例，`CompositionPlanner` 生成的产物必须与旧 Profile 逐字节一致，`--check` 对每个兼容样例做新旧两条渲染路径的等价比对；新增技术类型优先扩展组件而非复制 Profile，为既有组合补组件时必须先加兼容样例兜底
- 仓库根原有的 19 套架构快照目录已于 2026-08-28 删除，不再作为维护入口；新项目用 `use-ai-rule.exe`（或 `--emit`）生成规则文件到目标项目。启动时先打印当前规则版本（版本唯一事实来源为 `fragments/manifest.json` 的 `vars.version`），再检测目标项目特征给出推荐组合，也可走问答决策树（基于 manifest 的 `traits` + `wizard` 配置）或经典的「大类 → Profile」；生成成功后写入 `.ai-rule-profile.json` 配方记录，并自动把该记录与生成器本体 `use-ai-rule.exe` 加入目标项目 `.gitignore`。新增 Profile 必须同时补 `traits` 与 `wizard.options` 文案，否则 `--check` 失败
- `design-system.yaml` 的颜色、主题、字体、风格关键词是内置基线而非固定规范：规则层要求按产品品牌定制（`meta.status` 从「基线待定制」改为「已定制」），生成器对已定制的本地文件保留不覆盖；定制口径统一维护在 `shared/ui/design-system-liveness.md`，改一处同步全部界面子规则
- 通用执行约束需在各架构模板间保持一致；Windows / PowerShell 场景下统一禁止使用 `&&` 链式命令
- 通用注释口径需在各架构模板间保持一致：如果编写注释（含代码注释、文档注释、XML 注释、Go doc 注释等），统一使用中文，不使用纯英文注释
- 通用拆分口径需在各架构模板间保持一致：单个类、页面、组件、页面基类应保持职责单一，避免巨型文件和巨型类。发现文件持续膨胀、单类职责过多、阅读成本明显升高时，必须优先拆分为更小的组件、服务、辅助类或局部模块。禁止编写超大页面基类、超大工具类、超大 Service 类，禁止把多个无关职责长期堆在同一个类中。

## 规则效果反馈承接

目标项目会把规则命中 / 违反情况记录到其根目录 `.ai-rule-feedback.md`（条目约定见生成规则 `collaboration-AGENTS.md` 的「规则效果反馈」小节）。承接口径统一维护在 `doc/规则效果反馈承接.md`：收集登记 → 按 `TASK.md` §2.4 归属 → 改 `fragments/` → 验证升版 → 回执到原反馈文件。处理反馈时遵循该文件，不临时另立口径。

## 批量修改经验

当需要同时修改多个架构模板中的规则文件时，按以下方式执行，避免规则口径不一致：

1. 先统一规则口径，再开始批量改文件。
2. 先搜索全仓库中的旧表述、目录示意、文件职责描述和关键规则段落，确认影响范围。
3. 优先修改根目录 `AGENTS.md` 与 `README.md` 这类入口说明，再批量修改各架构模板，避免局部新旧规则并存。
4. 对结构固定、内容一致的模板文件可批量覆盖；对 `AGENTS.md` 这类可能存在差异的文件，优先基于关键段落定点修改，不要盲目整文件替换。
5. 批量修改后，必须再次搜索旧表述是否残留，并回读至少一个代表性模板，确认目录结构、规则说明和示例文件已同步。
6. 如果批量操作中出现编码、脚本替换失败或内容异常，优先停下来校验结果，必要时改为更稳妥的逐文件修改方式，不要继续扩大错误范围。


---
