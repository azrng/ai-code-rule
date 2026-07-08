## [2026-07-08]：dotnetCoreSingle 任务记录范围、Docker 门控与注释约束收紧

跟踪状态表：

| 文件夹名称 | 当前子任务状态 | 备注 |
|-----------|----------------|------|
| `dotnetCoreSingle` | `已完成` | 收紧 TASK.md 过程叙述写入范围；新增 Docker / 部署文件创建确认门控；约束 `<remarks>` 与 `/// <inheritdoc />`；顺带清理既有重复 |

### `dotnetCoreSingle`

#### 修改原因
用户用 dotnetCoreSingle 模板临时项目（D:\Gitee\Temp\api）实测后反馈五类问题：
1. TASK.md 写了"已完成步骤""待用户确认"等过程叙述（应在写代码前以阶段门控确认，而非事后堆进文件）
2. AI 未单独确认就创建了 Docker 文件
3. 阻塞项被写入 TASK.md（在最终输出提醒即可，不必入文件）
4. 实现类补了实现细节型 `<remarks>`
5. 接口已注释的实现方法仍堆 `/// <inheritdoc />`

根因：
- 任务管理规则把"已完成步骤 / 阻塞项 / 交接说明"范围写成"跨 Agent 或跨会话任务应补充"，未反向约束单会话任务，AI 对单会话也照搬三段叙述
- `infra-AGENTS.md` 缺"首次创建部署文件需单独确认"门控；根 `AGENTS.md` 高风险确认清单不含"新增部署文件"
- 代码注释规范未显式约束 `<remarks>`；原写"接口方法与实现方法都要注释"，导致实现方法堆 inheritdoc

#### 主要修改内容
- `AGENTS.md`（v1.5.0 → 1.6.0）任务管理：单 Agent 单会话任务只记 7 个字段，不写"已完成步骤 / 待用户确认 / 阻塞项"；仅在跨会话交接或明确阻塞需用户决策时才写入文件
- `AGENTS.md` 纠错机制：阻塞项 / 交接说明改为按任务管理范围约定处理（仅留交叉引用，不复述）
- `AGENTS.md` 最终输出要求：删除重复的"当前任务状态"条目（顺带清理既有重复）
- `infra-AGENTS.md`（v1.4.0 → 1.5.0）Docker 总体要求：新增"首次创建 `Dockerfile` / `docker-compose.yml` / `.dockerignore` 或其他部署文件前，必须把'将新增部署文件'作为独立一项向用户确认"，不得因骨架或计划提及过就默认生成
- `backend-AGENTS.md`（v1.5.1 → 1.6.0）代码注释规范：接口已注释时实现方法不重复、不加 `/// <inheritdoc />`；默认不写 `<remarks>`，仅 Controller action 对外契约信息可保留精简 remarks
- `backend-AGENTS.md` Swagger 规范：改为指向代码注释规范的指针，恢复"不依赖 Swagger 自动展示，但需保证可读与可维护"原意
- `backend-AGENTS.md` 技术栈：技术债务评估框架改为指向根 `AGENTS.md`（顺带清理既有重复，与 infra 写法一致）
- `TASK.md`：新增 T004 记录

#### 验证情况
- 已审查本次新增是否引入重复：Swagger 规范与代码注释规范、注释规范内部 265/266、纠错机制与任务管理三处自引入重复已消除
- 注释约束与既有"不写实现细节"原则一致，未冲突
- 带 frontmatter 的规则文件 `version` 与 `last_updated` 已同步更新

#### 范围说明
- 本次仅修订 dotnetCoreSingle；`dotnetBlazor` / `dotnetLayered` / `dotnetConsole` 等模板可能存在同类规则缺口，用户选择暂不同步，留待后续
- `dotnetConsole` 后端规则在 `application-AGENTS.md`（非 backend-AGENTS.md），将来同步时需按文件实际结构处理

#### 当前子任务状态
- `已完成`

---
