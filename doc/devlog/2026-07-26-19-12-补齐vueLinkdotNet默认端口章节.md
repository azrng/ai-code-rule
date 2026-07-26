# 补齐 vueLinkdotNet 默认端口章节

## 本次目标
- 收口批量修复的唯一遗留项：`vueLinkdotNet/infra-AGENTS.md` 的「阅读摘要 / 常见任务入口」引用了不存在的「默认端口」章节。

## 核心改动
- 在「主动建议规则」与「Docker 规则」之间新增「默认端口」章节；端口与 API 基址取自该目录 `design-system.yaml` 的既有定义（前端 Vue 3000、后端 8080、PostgreSQL 5432、`VITE_API_BASE_URL` 指向 `http://localhost:8080`），并补 Redis 6379（仅启用 `Common.Cache.Redis` 时）与端口冲突处理原则。
- 注明 `design-system.yaml` 为端口与 API 基址的唯一事实来源，两处修改必须同步。

## 修改文件
- `vueLinkdotNet/infra-AGENTS.md`

## 校验情况
- 端口与环境变量名均对照 `design-system.yaml` 与 `frontend-AGENTS.md` 技术栈（Vite）核实，未凭经验编造。
- 重新读取改动段落，确认中文正常。

## 风险或遗留项
- 无。
