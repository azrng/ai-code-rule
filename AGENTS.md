# AGENTS.md

## 项目说明

本仓库用于沉淀和维护不同技术架构下的 AI 协作开发规则模板。

- `fragments/` 是唯一维护源：`arch/<id>/` 维护各架构专属片段，`shared/` 维护跨架构共享小节（子规则通过 `<!-- include: shared/... -->` 引用，改一处同步全部组合），`manifest.json` 维护模块、家族与组合事实（组件 / 约束 / 兼容样例）
- 生成器 `app/RuleAssembler/` 按维度组合把片段拼装为 `AGENTS.md`、`CLAUDE.md`、`GEMINI.md` 与 `backend-AGENTS.md`、`frontend-AGENTS.md`、`doc-AGENTS.md`、`infra-AGENTS.md`、`ui-AGENTS.md`、`application-AGENTS.md`、`infrastructure-AGENTS.md` 等子规则文件，部分组合另含 `design-system.yaml`
- 组合内核（持久口径）：manifest 的 `components` / `constraints` / `compatibilityCases` 是维度组合（形态 + 壳 + 前端 + 后端）的唯一事实来源（schema_version 3，`profiles` 节点与旧 Profile 渲染路径已删除，`--emit-all-legacy` 已下线），兼容样例携带展示元数据（label / desc / family），经典入口两级选择、检测器候选展示与配方三件套（profile_id / profile_label / family）都按样例取值；维度空间唯一事实源为 wizard.steps，问答剪枝先过 `constraints` 钳制再做结构锚定校验（非 absent 维度取值必须被存续组件表达），`--plan` 与组合解析对违反钳制或缺组件锚定的组合显式报错；`--check` 对每个样例做组合口径输出契约校验（声明 vs 产物）+ 组件问答决策树仿真 + 内置技能校验；`--emit` 按样例输出组合产物；新增技术类型 = 加组件 + 约束 + 兼容样例（含 label / desc / family），不新增其他配置
- 产物回归守卫：`test/RuleAssembler.Tests/emit-baseline.json` 固化全部兼容样例产物的 SHA-256 清单（`EmitBaselineTests` 比对，覆盖意外漂移：改错片段、include 叠加、组件传导面遗漏），规则内容变更导致基线不一致时测试失败；确认变更符合预期后必须刷新基线（`UPDATE_EMIT_BASELINE=1 dotnet test --filter EmitBaseline`）并随变更一起提交，基线文件的 diff 即本次变更的产物审查清单
- `skills/` 是 AI Skills 的唯一维护源：每个技能是一个含 `SKILL.md` 的目录（`dotnet-skills/` 等分组目录只是归档分层，技能 ID 取含 `SKILL.md` 的最外层目录名），`skills/README.md` 是仓库文档不随包发布。生成器把 `skills/` 全量嵌入发布产物，交互生成在规则文件落盘后提供「选择 Skills 导入」：多选技能、多选目标工具，按项目级目录约定整体拷贝（Codex → `.codex/skills/<技能>/`、ZCode → `.zcode/skills/<技能>/`、Claude Code → `.claude/skills/<技能>/`、pi → `.pi/skills/<技能>/`）。导入是追加语义：未选中的技能不动，目标目录已存在同名技能（含 SKILL.md）时先提示覆盖或跳过（跳过的技能保留本地版本且不记入配方；覆盖 = 同名技能目录先删后写、按内置内容整体重写，清理上游删改文件的残留，无 SKILL.md 的目录不算同名技能不删除）；`--recipe` / `--upgrade` 重放按配方同步、同名技能先删后写。勾选结果记入 `.ai-rule-profile.json` 的 `skills` / `skill_targets`
- `design-system.yaml` 的颜色、主题、字体、风格关键词是内置基线而非固定规范：规则层要求按产品品牌定制（`meta.status` 从「基线待定制」改为「已定制」），生成器对已定制的本地文件保留不覆盖；定制口径统一维护在 `shared/ui/design-system-liveness.md`，改一处同步全部界面子规则
- 通用执行约束需在各架构模板间保持一致；Windows / PowerShell 场景下统一禁止使用 `&&` 链式命令
- 通用注释口径需在各架构模板间保持一致：如果编写注释（含代码注释、文档注释、XML 注释、Go doc 注释等），统一使用中文，不使用纯英文注释
- 通用拆分口径需在各架构模板间保持一致：单个类、页面、组件、页面基类应保持职责单一，避免巨型文件和巨型类。发现文件持续膨胀、单类职责过多、阅读成本明显升高时，必须优先拆分为更小的组件、服务、辅助类或局部模块。禁止编写超大页面基类、超大工具类、超大 Service 类，禁止把多个无关职责长期堆在同一个类中。

## 规则效果反馈承接

目标项目会把规则命中 / 违反情况记录到其根目录 `.ai-rule-feedback.md`（条目约定见生成规则 `collaboration-AGENTS.md` 的「规则效果反馈」小节；该文件是待处理清单，本地产物，已由生成器加入目标项目 `.gitignore` 不随项目提交，回传时直接粘贴文件内容）。承接口径统一维护在 `doc/规则效果反馈承接.md`：收集登记 → 按其「变更归属」表归属 → 改 `fragments/` → 验证升版 → 回传处理结果，由目标项目删除已处理条目。处理反馈时遵循该文件，不临时另立口径。

其中技能（skills）类条目直接修改 `skills/` 对应技能目录并核对内容（对照上游官方源或实战项目），无需 emit 基线刷新；但由于内容包随发布统一携带版本号，技能变更后发内容包时同样递增 `vars.version`（emit 基线仍只在 fragments 变更时刷新），修正后由目标项目重新导入或 `--upgrade` 重放同步（同名技能先删后写），回执口径与规则条目一致。

发布形态为「exe 引擎 + merged 内容包」分离：`publish.ps1` / `publish.sh` 除三平台 exe 外打包 `dist/ai-rule-pack.zip`（fragments + skills，pack.json 记录版本与逐文件 SHA-256）与 `dist/latest.json`（版本 + zip SHA-256）；改 fragments / skills 后重打内容包发 Release 即可，仅生成器代码（`app/RuleAssembler/`）变更才需要重发 exe。exe 启动时经 PackResolver 决定内容来源：`--pack` 指定包 → `--offline` 本地缓存 → 交互模式更新检查（latest.json，失败降级）→ 程序集内嵌资源兜底，单 exe 无网络永远可用。

其中「冗余」反馈的判定、证据门槛与候选规则维护在 `doc/规则退役清单.md`，不得凭单次体验删除规则。

---
