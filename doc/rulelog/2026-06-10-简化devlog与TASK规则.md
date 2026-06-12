## [2026-06-10]：简化 devlog 与 TASK 规则

跟踪状态表：

| 文件夹名称 | 当前子任务状态 | 备注 |
|-----------|----------------|------|
| `dotnetLayered` | `已完成` | 已完成 devlog 移除、TASK 精简、规则更新、第一批简化（减少 62 行）、代码审查修复（恢复判断标准定义、依赖列、技术选择原则） |
| `dotnetCoreSingle` | `已完成` | 已完成 devlog 移除、TASK 精简、规则更新、补充技术选择原则（版本升级至 1.5.0） |
| `dotnetBlazor` | `已完成` | 已完成 devlog 移除、TASK 精简、规则更新、补充技术选择原则（版本升级至 1.5.0） |
| `avaloniaExe` | `已完成` | 已完成 devlog 移除、TASK 精简、规则更新（版本升级至 1.4.0） |
| `reactLinkdotNet` | `已完成` | 已完成 devlog 移除、TASK 精简、规则更新（版本升级至 1.4.0） |
| `staticHtml` | `已完成` | 已完成 devlog 移除、TASK 精简、规则更新（版本升级至 1.4.0） |
| `flutterMobile` | `已完成` | 已完成 devlog 移除、TASK 精简、规则更新（版本升级至 1.4.0） |

### 修改原因

- devlog 目录有 58 个独立变更记录文件，占用大量上下文（约 221KB）
- git 提交日志已足够清晰，包含任务、阶段、原因、变更、风险、关联、AI 等字段
- TASK.md 结构冗余，大部分文件显示"当前无任务"
- 需要简化规则，减少上下文占用，提高 AI 协作效率

### 主要修改内容

1. **移除 devlog 相关规则**
   - 删除 AGENTS.md 中所有 devlog 引用
   - 删除 doc-AGENTS.md 中 devlog 要求和 task-archive 要求
   - 简化文档目录约定

2. **精简 TASK.md 格式**
   - 只保留：任务ID、任务名称、任务状态、更新时间
   - 移除空的"当前处理中"和"待确认"段落
   - 移除"归档索引"段落
   - 只保留最近 5 条 DONE 任务，超出自动删除

3. **更新任务管理规则**
   - 将"任务归档规则"改为"任务清理规则"
   - DONE 任务超过 5 条时自动删除最早的
   - 不再使用 doc/task-archive/ 归档

4. **简化交付流程**
   - 移除 devlog 同步要求
   - 流程变为：用户提出问题 → AI 按规则修改 → 最小回归 → 提交
   - 不再创建新文档

### 修改的文件

- `dotnetLayered/AGENTS.md`
- `dotnetLayered/doc-AGENTS.md`
- `dotnetLayered/TASK.md`
- 删除 `dotnetLayered/doc/task-archive/` 目录
- `avaloniaExe/AGENTS.md`
- `avaloniaExe/doc-AGENTS.md`
- `avaloniaExe/TASK.md`
- 删除 `avaloniaExe/doc/task-archive/` 目录
- `reactLinkdotNet/AGENTS.md`
- `reactLinkdotNet/doc-AGENTS.md`
- `reactLinkdotNet/TASK.md`
- `staticHtml/AGENTS.md`
- `staticHtml/doc-AGENTS.md`
- `staticHtml/frontend-AGENTS.md`
- `staticHtml/TASK.md`
- 删除 `staticHtml/doc/task-archive/` 目录
- 删除 `staticHtml/doc/devlog/` 目录
- `flutterMobile/AGENTS.md`
- `flutterMobile/doc-AGENTS.md`
- `flutterMobile/TASK.md`
- 删除 `flutterMobile/doc/task-archive/` 目录
- 删除 `flutterMobile/doc/devlog/` 目录

### 当前子任务状态

- `已完成`
