# AI规则审查经验总结

## 规则文件夹清单

用于汇总仓库内当前已有的规则模板文件夹；具体修改过程与子任务状态以 `doc/rulelog/` 中的任务记录为准。

- `nextFullstack`
- `avaloniaExe`
- `dotnetBlazor`
- `dotnetConsole`
- `dotnetCoreSingle`
- `dotnetLayered`
- `python`
- `reactLinkdotNet`
- `reactTauriExe`
- `reactWailsExe`
- `vueLinkdotNet`
- `vueWailsExe`
- `wpfExe`
- `staticHtml`
- `generalProject`
- `electronDotNetExe`

---

## 规则变更记录索引

详细规则修改记录已按二级任务标题拆分到 `doc/rulelog/`；本文件只维护规则文件夹清单、记录索引与修改记录模板。

- [2026-04-16 全面审查与修复](rulelog/2026-04-16-14-25-全面审查与修复.md)
- [2026-04-16 自动提交触发规则](rulelog/2026-04-16-12-42-自动提交触发规则.md)
- [2026-04-16 审计日志记录去重](rulelog/2026-04-16-13-46-审计日志记录去重.md)
- [2026-04-18 新增通用项目规则模板](rulelog/2026-04-18-17-10-新增通用项目规则模板.md)
- [2026-04-27 提交编码与 Commit 正文规则校准](rulelog/2026-04-27-13-48-提交编码与 Commit 正文规则校准.md)
- [2026-04-29 规则审计日志拆分为 rulelog 目录](rulelog/2026-04-29-12-22-规则审计日志拆分为规则记录目录.md)
- [2026-05-19 模型行为约束与主动建议规则](rulelog/2026-05-19-10-52-模型行为约束与主动建议规则.md)
- [2026-05-19 多模板规则治理优化](rulelog/2026-05-19-12-00-avaloniaExe规则治理优化.md)
- [2026-06-10 简化 devlog 与 TASK 规则](rulelog/2026-06-10-简化devlog与TASK规则.md)
- [2026-06-27 reactLinkdotNet 规则一致性修复](rulelog/2026-06-27-reactLinkdotNet规则一致性修复.md)
- [2026-06-27 新增 electronDotNetExe 模板](rulelog/2026-06-27-新增electronDotNetExe模板.md)
- [2026-06-28 azrng-efcore 分页示例与实体基类描述修复](rulelog/2026-06-28-azrng-efcore分页与实体基类描述修复.md)
- [2026-06-28 onion-architecture 去重精简](rulelog/2026-06-28-onion-architecture去重精简.md)
- [2026-07-08 dotnetCoreSingle 任务记录范围、Docker 门控与注释约束收紧](rulelog/2026-07-08-dotnetCoreSingle任务记录Docker门控注释约束收紧.md)

---

## 修改记录模板

```markdown
## [YYYY-MM-DD]：[具体任务名称]

跟踪状态表：

| 文件夹名称 | 当前子任务状态 | 备注 |
|-----------|----------------|------|
| `[文件夹名称]` | `[未开始/进行中/已完成]` | `[备注]` |

### `[文件夹名称]`

#### 修复的问题
1. **[问题类型]**：[具体问题描述]
2. **[问题类型]**：[具体问题描述]

#### 修改的文件
- [文件1]
- [文件2]

#### 当前子任务状态
- `[未开始/进行中/已完成]`
```
