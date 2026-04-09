# 任务清单（TASK）

> 本文件只记录具体任务内容、状态与交付要求。
> 规则解释、阶段门控与工作方式以 `AGENTS.md` 为准。

---

## 任务总表

| 任务 ID | 任务名称 | 任务目标 | 当前阶段 | 负责人 AI | 任务状态 | 优先级 | 最近更新时间 |
| ------- | -------- | -------- | -------- | --------- | -------- | ------ | ------------ |
| T001 | 维护仓库规范文件 | 统一仓库规范文件结构，明确协作规则、设计系统、任务机制与文档目录约定 | 治理 | 通用 | DONE | P1 | 2026-04-07 |
| T002 | 拆分 avaloniaExe 规则文件 | 将单文件 `AGENTS.md` 拆分为根入口与分域子规则文件，并补齐索引、阅读顺序与回顾机制 | 治理 | Codex | REVIEW | P1 | 2026-04-09 |

---

## 新任务录入模板

### 1. 总表新增一行

```md
| Txxx | 任务名称 | 任务目标 | 当前阶段 | 负责人 AI | TODO | P1 | YYYY-MM-DD |
```

### 2. 追加任务详情

```md
### Txxx 任务名称

- 任务目标：
- 当前阶段：
- 负责人 AI：
- 协作对象：
- 依赖任务：
- 影响范围：
- 验收标准：
  -
  -
- 必跑校验：
  -
  -
- 风险与说明：
  -
- 最近进展：
  - YYYY-MM-DD：
```

---

### T002 拆分 avaloniaExe 规则文件

- 任务目标：将 `avaloniaExe/AGENTS.md` 按桌面端架构拆分为根入口与 `ui`、`application`、`infrastructure`、`doc` 子规则文件，并同步补充变更记录。
- 当前阶段：治理
- 负责人 AI：Codex
- 协作对象：无
- 依赖任务：T001
- 影响范围：
  - `avaloniaExe/AGENTS.md`
  - `avaloniaExe/ui/AGENTS.md`
  - `avaloniaExe/application/AGENTS.md`
  - `avaloniaExe/infrastructure/AGENTS.md`
  - `avaloniaExe/doc/AGENTS.md`
  - `doc/devlog/`
- 验收标准：
  - 根 `AGENTS.md` 仅保留全局入口、流程、索引与交付规则。
  - `ui`、`application`、`infrastructure`、`doc` 四个子规则文件已创建并承载对应细则。
  - 已补充子规则索引、阅读顺序、验证优先级和重复错误回顾机制。
  - 已新增本次修改的 `doc/devlog/` 记录。
- 必跑校验：
  - 重新读取拆分后的规则文件，确认中文编码正常。
  - 检查是否存在重复标题、重复分隔线和明显规则缺失。
- 风险与说明：
  - 本次只调整规则模板结构，不修改其他架构目录。
  - 工作区内存在其他未提交改动，本任务不处理无关文件。
- 最近进展：
  - 2026-04-09：新增任务并开始按 `reactLinkdotNet` 试点经验拆分 `avaloniaExe` 规则结构。
  - 2026-04-09：完成根入口与 `ui`、`application`、`infrastructure`、`doc` 子规则文件拆分，并补充 devlog 与结构回读检查。
