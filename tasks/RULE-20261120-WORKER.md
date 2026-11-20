# RULE-20261120-WORKER：dotnetWorker 后台常驻服务组合

## 背景与根因

起因：讨论 D:\Work\component\sso-launcher 是否需要补技术栈。排查结论分两层：

1. sso-launcher 本体（WinForms NotifyIcon 托盘、无窗体）已由 winformsExe（3.20.0）承接，
   检测器对 `UseWindowsForms` 强特征可识别，无需再动。
2. 排查中暴露真正的空白：.NET Worker Service / Windows 服务 / 后台常驻进程类项目
   （`Microsoft.NET.Sdk.Worker`、`Microsoft.Extensions.Hosting.WindowsServices`）在维度
   空间中没有形态可落——form 只有 backend-only（Web API 语义，含 Controller/HTTP 契约
   口径），会被错配到 dotnetConsole（命令解析规则误导）或 dotnet-backend（API 契约误导）。
   后台常驻是无界面长驻进程 + 无 HTTP 契约 + 安装/守护/日志落点等基建诉求，是独立形态。

## 拍板

- 新增 form 维度取值 `daemon`（后台常驻服务），排在 desktop 之后；
  选项 label「后台常驻服务」、desc 覆盖 Windows 服务 / systemd / 容器 worker。
- 新增家族 `daemon`、约束「form=daemon → frontend=none + shell=none + backend=dotnet」
  （首版仅承接 dotnet 后端，与 avalonia/wpf/winforms 的钉死口径一致；Go/Node worker
  后续按「加组件+约束+样例」接入，不新增其他配置）。
- 新增组件 `dotnetWorker`（kind=form，when 钉全 7 维）+ 兼容样例 `dotnetWorker`，
  arch/dotnetWorker/ 三片段：application（BackgroundService 作业规范、优雅停机、
  并发幂等、配置与 Options、DI）、infrastructure（数据访问后台口径、Windows 服务安装
  sc.exe/服务账户、日志落文件/事件日志、计划任务发布链路）、doc。
- 检测器强特征：csproj 含 `Microsoft.NET.Sdk.Worker` 或
  `Microsoft.Extensions.Hosting.WindowsServices`；仅引用 Microsoft.Extensions.Hosting
  （控制台模板默认）不算，避免误判。UI/包仓库主导守卫与 WebService 分支同口径：
  WPF 主程序 + Worker 辅助工程不声明方案级 daemon 证据。
- RecipeRecorder：family daemon → dotnet gitignore 模板。
- skills：grill-me when.form 补 daemon（通用工具全组合可达）。
- 版本 3.20.0 → 3.21.0（fragments 变更必须升版）。

## 验证

- --check：39 兼容样例全部通过；组件问答校验 39 样例可达无歧义；53 技能登记齐全。
- dotnet test 全量 279 全绿（含新增 4 个检测器测试：Worker SDK 命中、
  WindowsServices 包命中、纯 Hosting 引用不误判、WPF+Worker 混合保持 wpfExe）。
- --detect 实测临时 Sdk.Worker 工程：推荐组合 dotnetWorker，
  回答 form=daemon/backend=dotnet/frontend=none/shell=none 全维。
- --emit 抽查 dotnetWorker 产物：8 文件（无 ui/design-system，契约声明一致）、
  子规则索引两条、Workers/ 目录规范与日志落点条款在位。
- emit 基线：UPDATE_EMIT_BASELINE=1 刷新（版本戳 3.21.0 全量哈希轮换 +
  dotnetWorker 新增 8 产物），diff 构成核对无意外改动。
