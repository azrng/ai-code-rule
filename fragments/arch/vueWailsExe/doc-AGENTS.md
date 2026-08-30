---
rule_id: doc-agents
version: 1.17.0
last_updated: 2026-08-30
dependencies: [agents-root]
---

# 文档规则

<!-- include: shared/doc/scope.md -->

<!-- include: shared/doc/common-tasks.md -->

---

<!-- include: shared/doc/advisory.md -->

---

<!-- include: shared/doc/stage0-design.md -->

---

`design-system.yaml` 是唯一设计系统文件，包含：
- 品牌与产品风格意图
- 语义化颜色 token
- 排版 token
- 间距、圆角、阴影 token
- 布局规则
- Element Plus 主题配置映射
- 页面状态规范
- 组件使用规则
- 明确的 `do / dont` 规则

---

## 文档更新要求

---

以下内容发生变化时，必须同步更新相关文档：
- 命令、脚本或启动方式
- 环境变量
- 配置项
- 构建、发布或部署流程
- 数据库结构、迁移或初始化方式
- 对外使用方式或接口约定

优先更新已有文档，不新增重复说明。

<!-- include: shared/doc/doc-directory.md -->

<!-- include: shared/doc/requirement.md -->

<!-- include: shared/doc/retro-format.md -->
