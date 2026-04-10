# 扁平化 reactLinkdotNet 子规则文件结构

## 本次目标
- 将 `reactLinkdotNet` 下仅承载单个规则文件的子目录调整为扁平命名文件，降低目录层级复杂度。

## 核心改动
- 将 `frontend/AGENTS.md`、`backend/AGENTS.md`、`doc/AGENTS.md`、`infra/AGENTS.md` 分别调整为 `frontend-AGENTS.md`、`backend-AGENTS.md`、`doc-AGENTS.md`、`infra-AGENTS.md`。
- 同步更新 `reactLinkdotNet/AGENTS.md` 与 `reactLinkdotNet/TASK.md` 中的引用路径。
- 清理已不再需要的空子目录。

## 修改文件
- `reactLinkdotNet/AGENTS.md`
- `reactLinkdotNet/frontend-AGENTS.md`
- `reactLinkdotNet/backend-AGENTS.md`
- `reactLinkdotNet/doc-AGENTS.md`
- `reactLinkdotNet/infra-AGENTS.md`
- `reactLinkdotNet/TASK.md`
- `doc/devlog/2026-04-10-15-21-30-扁平化reactLinkdotNet子规则文件结构.md`

## 校验情况
- 已搜索确认规则正文中的旧路径引用已完成迁移。
- 已回读关键文件，确认中文内容与新文件名显示正常。

## 风险或遗留项
- 当前仓库中的同类模板已完成统一，后续新增模板时需沿用相同命名规范。
