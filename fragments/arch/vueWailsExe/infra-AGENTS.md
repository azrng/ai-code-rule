---
rule_id: infra-agents
version: 1.21.0
last_updated: 2026-08-31
dependencies: [agents-root]
---

# 基础设施规则

<!-- include: shared/infra/scope.md -->

<!-- include: shared/infra/common-tasks.md -->

---

<!-- include: shared/application/tech-choice.md -->

---

## 主动建议规则
- 发现环境变量、端口、启动方式、构建脚本、部署脚本或发布链路存在不一致、不可复现或文档缺失时，应主动提醒
- 发现可以复用现有启动脚本、打包脚本、环境变量命名或发布脚本时，应优先建议复用，不新增平行部署链路
- 不确定端口占用、外部服务地址、密钥来源、安装路径或运行命令时，应按根 `AGENTS.md` 的查证优先级处理，禁止凭经验写死配置
- 涉及清理环境、删除数据、停止未知进程、修改部署配置等高风险操作时，只能提出建议，必须等待明确确认后执行

---

- 仅支持 Windows 平台发布
- 使用 `wails build -platform windows/amd64` 构建可执行文件
- 不要把密钥写入配置文件
- 发布链路要可追踪
- 清理旧版本时要保留最小回滚窗口

### 发布交付规则
- 只要本次任务涉及发布配置、版本号和构建命令，完成实现后应主动执行一次构建验证
- 若因环境限制无法执行，必须明确说明
- 发布验证至少包括：
  - 构建是否成功
  - 应用是否成功启动
  - 关键功能是否处于可用状态

---

## 任务管理机制
`TASK.md` 是唯一活动任务记录文件，较早的 `DONE` 任务统一归档到 `doc/task-archive/YYYY-MM.md`。

<!-- include: shared/infra/pre-commit-regression.md -->

<!-- include: shared/no-test-fallback.md -->
