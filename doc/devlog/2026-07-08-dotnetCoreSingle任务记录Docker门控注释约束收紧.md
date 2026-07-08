# 2026-07-08 dotnetCoreSingle 任务记录范围、Docker 门控与注释约束收紧

## 修改范围
- `dotnetCoreSingle/`：`AGENTS.md`、`backend-AGENTS.md`、`infra-AGENTS.md`、`TASK.md`
- `doc/AI-Rules-Audit-Log.md`：规则变更记录索引新增一条

## 大概变更内容
针对用户用 dotnetCoreSingle 模板临时项目实测反馈的五类问题，收紧三类规则：

1. **任务记录范围**：单会话任务不再把"已完成步骤 / 待用户确认 / 阻塞项"写入 `TASK.md`，改为在最终对话输出提醒；仅跨会话交接或明确阻塞需用户决策时才入文件。
2. **Docker 创建门控**：首次创建 `Dockerfile` / `docker-compose.yml` / `.dockerignore` 等部署文件前，必须把"将新增部署文件"作为独立一项向用户确认，不得因方案骨架或阶段计划提及过就默认生成。
3. **注释约束**：接口已注释时实现方法不重复注释、不加 `/// <inheritdoc />`；默认不写实现细节型 `<remarks>`，仅 Controller action 对外契约信息可保留精简 remarks。

顺带清理两处既有重复：`backend-AGENTS.md` 技术债务评估框架改为指向根 `AGENTS.md`；`AGENTS.md` 最终输出要求去掉重复的"当前任务状态"。

## 详细记录
详见 `doc/rulelog/2026-07-08-dotnetCoreSingle任务记录Docker门控注释约束收紧.md`。
