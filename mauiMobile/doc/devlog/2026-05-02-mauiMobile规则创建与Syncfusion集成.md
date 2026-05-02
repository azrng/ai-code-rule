# MAUI 移动端 AI 规则创建与 Syncfusion 集成

## 本次目标
参照 nextFullstack 规则模板，创建 MAUI 移动端 AI 开发规则目录，并将 UI 控件库更新为 Syncfusion.Maui.Toolkit。

## 核心改动
- 新建 `mauiMobile/` 目录，包含完整的 AI 协作规则体系
- 规则结构参照 avaloniaExe 模式，划分为 UI / 应用层 / 基础设施 / 文档 四个子规则
- design-system.yaml 面向移动端设计，包含触摸目标、安全区域、移动端交互模式等
- UI 控件库从 CommunityToolkit.Maui 调整为 Syncfusion.Maui.Toolkit（开源免费）

## 修改文件
- `mauiMobile/AGENTS.md`：根规则文件，全局协作规范、阶段门控、任务管理、Git 与交付
- `mauiMobile/CLAUDE.md`：Claude 入口提示
- `mauiMobile/GEMINI.md`：Gemini 入口提示
- `mauiMobile/ui-AGENTS.md`：UI 规则，包含 Syncfusion 控件优先、移动端适配、Shell 导航
- `mauiMobile/application-AGENTS.md`：应用层规则，ViewModel/Service/命令/生命周期
- `mauiMobile/infrastructure-AGENTS.md`：基础设施，SQLite/EF Core/HttpClient/平台服务/Android+iOS 发布
- `mauiMobile/doc-AGENTS.md`：文档规则，设计文档/DTO 契约/devlog/task archive
- `mauiMobile/design-system.yaml`：设计 token，移动端布局、触摸目标、交互模式
- `mauiMobile/TASK.md`：任务跟踪文件
- `mauiMobile/doc/devlog/.gitkeep`：devlog 目录占位
- `mauiMobile/doc/task-archive/.gitkeep`：任务归档目录占位

## 校验情况
- 已确认目录结构完整，共 11 个文件
- 已确认所有文件 UTF-8 编码、中文内容正常
- 未执行编译或 lint（纯规则文件，无可编译代码）

## 风险或遗留项
- Syncfusion.Maui.Toolkit 当前版本 1.0.9，仍处于 1.x 阶段，后续 API 可能变动
- 技术栈中部分假设（如 SQLite 本地默认、REST API 远程通信）需在实际项目中确认
- 未提交 Git，等待用户确认后提交
