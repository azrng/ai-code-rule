# AGENTS Split Plan

## 目标

在不破坏“每个架构目录可独立复制使用”这一前提下，逐步把各目录中的长篇 `AGENTS.md` 拆成更清晰的多文件结构。

本阶段只定义拆分方案，不执行各架构目录内的实际改动，待补充要求后统一实施。

## 拆分原则

1. 每个架构目录必须保持自包含，不能依赖仓库根目录的共享规则文件。
2. 每个架构目录保留一个主 `AGENTS.md` 作为入口文件。
3. 子规则优先继续使用 `AGENTS.md` 文件名，并放到明确作用域的子目录下。
4. 根 `AGENTS.md` 只保留必须全局生效的内容，避免重复堆叠前后端细节。
5. 子规则文件数量控制在 2 到 4 个为宜，避免拆得过碎。
6. 先统一拆分结构，再视情况统一压缩重复内容和表述风格。
7. 根 `AGENTS.md` 必须提供明确的子规则索引，说明各子 `AGENTS.md` 的位置、适用范围与推荐阅读顺序。
8. 对重复出现的同类错误，规则体系应支持回顾分析与增量修订，避免同样问题反复发生。

## 主文件建议保留内容

各架构目录根 `AGENTS.md` 建议保留以下内容：

- 该架构模板的目标与适用场景
- 工作方法与使用方式
- 开发流程总览
- 核心规则
- 思考与实现原则
- 任务管理机制
- Git 与提交流程
- 交付收口规范
- 常见禁止事项
- 子规则索引位置与使用说明
- 子规则阅读顺序
- 重复错误回顾与规则更新机制

## 子文件建议承载内容

按架构类型将细则拆到子目录中的 `AGENTS.md`：

- `frontend/AGENTS.md`
  - 前端技术栈
  - 页面、组件、路由、状态、样式、请求规则
  - 前端测试规则
- `backend/AGENTS.md`
  - 后端技术栈
  - 分层、控制器、服务、领域、基础设施、数据访问
  - 缓存、迁移、统一响应、异常处理
  - 后端测试规则
- `doc/AGENTS.md`
  - 设计文档阶段要求
  - `requirement` / `devlog` / `design-system.yaml` 维护规则
- `infra/AGENTS.md`
  - Docker、端口、环境变量、部署约束
- `ui/AGENTS.md`
  - 桌面 UI 或一体化前端界面规则
- `src/AGENTS.md`
  - 单体项目或不区分前后端时的实现细则

## 各架构目录初步拆分建议

### reactLinkdotNet

- `AGENTS.md`
- `frontend/AGENTS.md`
- `backend/AGENTS.md`
- `doc/AGENTS.md`
- 可选：`infra/AGENTS.md`

### vueLinkdotNet

- `AGENTS.md`
- `frontend/AGENTS.md`
- `backend/AGENTS.md`
- `doc/AGENTS.md`
- 可选：`infra/AGENTS.md`

### nextFullstack

- `AGENTS.md`
- `app/AGENTS.md`
- `server/AGENTS.md`
- `doc/AGENTS.md`
- 可选：`infra/AGENTS.md`

### dotnetBlazor

- `AGENTS.md`
- `src/AGENTS.md`
- `doc/AGENTS.md`
- 可选：`infra/AGENTS.md`

说明：该模板前后端一体，不建议硬拆为 `frontend` 和 `backend`。

### dotnetCoreSingle

- `AGENTS.md`
- `src/AGENTS.md`
- `doc/AGENTS.md`
- 可选：`infra/AGENTS.md`

### dotnetConsole

- `AGENTS.md`
- `src/AGENTS.md`
- `doc/AGENTS.md`

### dotnetLayered

- `AGENTS.md`
- `src/host/AGENTS.md`
- `src/application/AGENTS.md`
- `src/domain/AGENTS.md`
- `src/infrastructure/AGENTS.md`
- `doc/AGENTS.md`

说明：该模板按分层架构拆分最有价值。

### python

- `AGENTS.md`
- `src/AGENTS.md` 或 `app/AGENTS.md`
- `doc/AGENTS.md`
- 可选：`infra/AGENTS.md`

### reactWailsExe

- `AGENTS.md`
- `frontend/AGENTS.md`
- `backend/AGENTS.md`
- `doc/AGENTS.md`

### vueWailsExe

- `AGENTS.md`
- `frontend/AGENTS.md`
- `backend/AGENTS.md`
- `doc/AGENTS.md`

### wpfExe

- `AGENTS.md`
- `ui/AGENTS.md`
- `application/AGENTS.md`
- `infrastructure/AGENTS.md`
- `doc/AGENTS.md`

### avaloniaExe

- `AGENTS.md`
- `ui/AGENTS.md`
- `application/AGENTS.md`
- `infrastructure/AGENTS.md`
- `doc/AGENTS.md`

### staticHtml

- `AGENTS.md`
- `pages/AGENTS.md`
- `assets/AGENTS.md`
- `doc/AGENTS.md`

## reactLinkdotNet 试点拆分草案

后续如先拿 `reactLinkdotNet` 作为试点，建议按下列方式迁移章节：

### 根 `AGENTS.md`

- 目标
- 工作方法
- 注意事项
- 开发流程
- 使用方式
- 核心规则
- 思考与实现原则
- 编码规则
- 任务管理机制
- Git 与提交流程
- 交付收口规范
- 常见禁止事项
- 交付输出要求
- 子规则索引位置与使用说明
- 子规则阅读顺序
- 重复错误回顾与规则更新机制

建议根 `AGENTS.md` 中固定提供一段“子规则索引”，至少写清：

- 子 `AGENTS.md` 的相对路径
- 各子文件的适用范围
- 触发场景或何时必须阅读
- 推荐阅读顺序

建议采用简短清单或表格，避免 AI 需要自行猜测子规则入口位置。

### `frontend/AGENTS.md`

- 前端技术栈
- 推荐目录结构中的前端部分
- 前端规则
- 前端测试
- 设计系统文件规范中的前端相关内容

### `backend/AGENTS.md`

- 后端技术栈
- 核心库
- 依赖注入规范
- 推荐目录结构中的后端部分
- 后端规则（Clean Architecture）
- 后端测试

### `doc/AGENTS.md`

- 阶段 0 设计文档要求
- 接口契约规范
- 文档更新要求
- `requirement` 要求
- `devlog` 要求
- `design-system.yaml` 维护要求

### 重复错误回顾机制

- 当 AI 在同一类问题上连续出现两次错误时，应触发一次简短回顾分析。
- 回顾至少应说明：错误现象、根因、原规则缺口、后续如何避免再次发生。
- 回顾结论应落实到对应架构目录的 `AGENTS.md` 或子 `AGENTS.md`，而不是只停留在对话说明。
- 若本仓库规则因此发生变更，仍需按仓库约定同步新增一条 `devlog` 记录。

### 可选 `infra/AGENTS.md`

- 部署
- 默认端口
- Docker 规则
- 环境变量与联调相关约束

## 待补充事项

在正式执行拆分前，还需要你补充或确认以下方向：

- 是否统一采用子目录 `AGENTS.md` 方案，不使用 `AGENTS-frontend.md` 这类命名
- 是否所有架构都采用统一目录命名规则，例如统一使用 `frontend`、`backend`、`doc`、`infra`
- 根 `AGENTS.md` 是否只保留规则入口，还是继续保留简要技术栈概览
- `doc/AGENTS.md` 是否需要兼管 `design-system.yaml`
- 是否需要为每个架构额外生成一份“章节迁移对照表”
- 根 `AGENTS.md` 中“子规则索引”采用清单还是表格作为统一格式
- “同类错误两次”是否需要在模板中给出更明确的判定示例

## 执行顺序建议

1. 先确认拆分命名规范和目录约定。
2. 先拿 `reactLinkdotNet` 做试点拆分。
3. 试点稳定后，再批量推广到同类架构。
4. 最后统一收敛命名、目录说明和阅读顺序描述。
