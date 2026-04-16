# AI规则审查经验总结

**状态说明**：
- `未审查`：尚未进行规则审查
- `已审查待修复`：已完成问题识别，待修复
- `修复中`：正在修复已识别的问题
- `已完成`：已完成问题修复，通过验证
- `需持续优化`：已完成基本修复，仍需持续改进


## 文件夹规则修改状态跟踪

| 文件夹名称 | 最近修改时间 | 当前状态 | 审查完成度 | 备注 |
|-----------|-------------|----------|------------|------|
| `nextFullstack` | `2026-04-16` | `已完成` | `100%` | 完成首次全面审查与修复 |
| `avaloniaExe` | `2026-04-16` | `已完成` | `100%` | 已完成首轮审查与规则修复 |
| `dotnetBlazor` | - | `未审查` | `0%` | 待安排审查 |
| `dotnetConsole` | - | `未审查` | `0%` | 待安排审查 |
| `dotnetCoreSingle` | - | `未审查` | `0%` | 待安排审查 |
| `dotnetLayered` | - | `未审查` | `0%` | 待安排审查 |
| `python` | - | `未审查` | `0%` | 待安排审查 |
| `reactLinkdotNet` | - | `未审查` | `0%` | 待安排审查 |
| `reactTauriExe` | - | `未审查` | `0%` | 待安排审查 |
| `reactWailsExe` | - | `未审查` | `0%` | 待安排审查 |
| `vueLinkdotNet` | - | `未审查` | `0%` | 待安排审查 |
| `vueWailsExe` | - | `未审查` | `0%` | 待安排审查 |
| `wpfExe` | - | `未审查` | `0%` | 待安排审查 |
| `staticHtml` | - | `未审查` | `0%` | 待安排审查 |

---

## 2026-04-16：nextFullstack 文件夹规则修复

### 审查范围
- **审查文件**：AGENTS.md、frontend-AGENTS.md、backend-AGENTS.md、doc-AGENTS.md、infra-AGENTS.md、CLAUDE.md、GEMINI.md
- **审查重点**：逻辑矛盾、模糊指令、结构解析失败风险
- **审查方法**：AI工程师视角，逐行分析规则表述，识别潜在的AI理解和执行风险

### 发现的问题类别
1. **逻辑矛盾问题**：同一规则文件或不同文件之间存在相互冲突的表述
2. **模糊指令问题**：关键判断标准缺乏量化指标
3. **结构解析失败风险**：规则文件结构设计不当，可能导致AI解析错误

### 修复的问题详情

#### 1. 逻辑矛盾修复
- **组件使用范围矛盾**：统一了Zustand使用范围的表述，从两处不一致的定义改为统一的明确场景列举（侧边栏折叠、全局筛选、通知、轻量会话上下文）
- **缓存策略优先级冲突**：添加了明确的缓存策略优先级规则，TanStack Query用于客户端数据缓存，Next.js缓存用于服务端数据缓存，两者不得针对同一数据源重复缓存
- **阶段门控执行矛盾**：将"不得跳阶段"与明确跳过场景的直接矛盾，改为逻辑一致的递进关系（一般原则 → 例外情况 → 约束条件）

#### 2. 模糊指令量化
- **判断标准定义**：为以下概念添加了具体量化指标
  - 简单问题：可在5分钟内回答，无需代码修改
  - 单点低风险改动：只涉及单一文件，修改内容少于20行，不影响API契约、数据库结构或权限模型
  - 微小调整：文案修改、颜色调整、间距优化等不影响逻辑的改动，修改内容少于10行
  - 无法继续（BLOCKED）：遇到明确的技术障碍、依赖缺失、需要用户决策或外部资源
  - 开发完成（REVIEW）：代码实现完成，已通过自测，等待用户确认或代码审查
- **技术债务评估框架**：建立了4级分类（沿用现有实现、必须重构、可选重构、禁止重构）和决策流程
- **任务状态触发条件**：明确了各任务状态的具体触发场景和检查点

#### 3. 结构解析优化
- **元数据格式统一**：所有规则文件统一采用YAML front matter格式，包含以下字段：
  - rule_id：规则唯一标识符
  - version：语义化版本号
  - last_updated：最后更新日期
  - dependencies：依赖的其他规则ID列表
- **规则文件标识**：为每个规则文件添加了唯一标识（agents-root、frontend-agents、backend-agents等）
- **规则修改记录机制**：在根AGENTS.md中添加了规则修改记录要求和跨文件夹同步机制

### 修改的文件
- `AGENTS.md` - 添加判断标准定义、阶段门控修复、规则修改记录要求
- `frontend-AGENTS.md` - Zustand使用范围统一、缓存策略优先级、技术债务评估框架、量化标准
- `backend-AGENTS.md` - 技术债务评估框架
- `doc-AGENTS.md` - 元数据格式统一
- `infra-AGENTS.md` - 元数据格式统一
- `CLAUDE.md` - 元数据格式统一
- `GEMINI.md` - 元数据格式统一

---

## 2026-04-16：avaloniaExe 文件夹规则审查

### 审查范围
- **审查文件**：AGENTS.md、ui-AGENTS.md、application-AGENTS.md、infrastructure-AGENTS.md、doc-AGENTS.md、CLAUDE.md、GEMINI.md
- **审查重点**：逻辑矛盾、模糊指令、结构解析失败风险
- **审查方法**：对照 `nextFullstack` 已修复口径与当前审查基线，逐项检查元数据、阶段门控、量化标准与子规则边界表述

### 发现的问题
1. **结构解析失败风险**：所有规则文件均未采用 YAML front matter，且多个子规则文件仍以统一 H1 `# AGENTS.md` 起始，缺少 AI 解析所需的唯一标识、版本信息和依赖关系。
2. **模糊指令问题**：根 `AGENTS.md` 中“简单问题”“单点低风险改动”“BLOCKED”“REVIEW”等关键判断标准未量化，任务状态触发条件也不够具体，容易导致不同 AI 执行口径漂移。
3. **逻辑歧义问题**：`ui-AGENTS.md` 同时保留 `ViewModels/Base/` 基类目录建议，并要求“所有 ViewModel 必须继承 `ObservableObject`”，但未明确允许经公共基类间接继承，容易造成实现理解分歧。
4. **决策框架缺失**：`ui-AGENTS.md`、`application-AGENTS.md`、`infrastructure-AGENTS.md` 仅强调“不要强行重构或替换技术栈”，但缺少统一的技术债务评估框架，难以在沿用、必须重构、可选重构、禁止重构之间做出一致判断。

### 需修复的文件
- `avaloniaExe/AGENTS.md`
- `avaloniaExe/ui-AGENTS.md`
- `avaloniaExe/application-AGENTS.md`
- `avaloniaExe/infrastructure-AGENTS.md`
- `avaloniaExe/doc-AGENTS.md`
- `avaloniaExe/CLAUDE.md`
- `avaloniaExe/GEMINI.md`

### 状态更新
将状态跟踪表中 `avaloniaExe` 的状态更新为 `已审查待修复`

---

## 2026-04-16：avaloniaExe 文件夹规则修复

### 修复的问题
1. **结构解析失败风险**：为 `AGENTS.md`、`ui-AGENTS.md`、`application-AGENTS.md`、`infrastructure-AGENTS.md`、`doc-AGENTS.md`、`CLAUDE.md`、`GEMINI.md` 统一补充 YAML front matter，增加 `rule_id`、`version`、`last_updated`、`dependencies` 元数据。
2. **模糊指令问题**：在根 `AGENTS.md` 中补充“简单问题”“单点低风险改动”“微小调整”“高风险内容”“BLOCKED”“REVIEW”等量化定义，并新增任务状态触发条件说明。
3. **逻辑歧义问题**：将阶段门控调整为“完整流程 + 可跳过阶段 0 的例外场景”，并在 `ui-AGENTS.md` 中明确 ViewModel 可通过公共基类间接继承 `ObservableObject`。
4. **决策框架缺失**：在 `ui-AGENTS.md`、`application-AGENTS.md`、`infrastructure-AGENTS.md` 中统一补充技术债务评估框架与重构决策流程。

### 修改的文件
- `avaloniaExe/AGENTS.md`
- `avaloniaExe/ui-AGENTS.md`
- `avaloniaExe/application-AGENTS.md`
- `avaloniaExe/infrastructure-AGENTS.md`
- `avaloniaExe/doc-AGENTS.md`
- `avaloniaExe/CLAUDE.md`
- `avaloniaExe/GEMINI.md`

### 状态更新
将状态跟踪表中 `avaloniaExe` 的状态更新为 `已完成`

---

## 修改记录模板

```markdown
## [YYYY-MM-DD]：[文件夹名称] 规则修复

### 修复的问题
1. **[问题类型]**：[具体问题描述]
2. **[问题类型]**：[具体问题描述]

### 修改的文件
- [文件1]
- [文件2]

### 状态更新
将状态跟踪表中 `[文件夹名称]` 的状态更新为 `[新状态]`
```
