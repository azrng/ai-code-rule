按你现有的目录名称检索公开 Skills 仓库，下面统一使用你指定的格式：
npx skills add 仓库地址 --skill 技能名 --agent universal --copy --yes



# Flutter 界面实现-必须
npx skills add musabekisakov-imj/flutter-design-engineer --skill flutter-implementation --agent universal --copy --yes

# Flutter 动效-可选
npx skills add musabekisakov-imj/flutter-design-engineer --skill flutter-motion --agent universal --copy --yes

# Flutter 页面美化审查-可选
npx skills add kamranbekirovyz/skills --skill flutter-improve-design --agent universal --copy --yes

# UI UX Pro Max-必须
npx ui-ux-pro-max-cli init --ai universal --offline

flutter-add-widget-test，Flutter 官方提供。
它可以让 AI 使用 WidgetTester 编写组件测试，验证按钮点击、输入框、列表滚动、状态变化。
# Flutter Widget 自动化测试 可选
npx skills add flutter/agent-plugins --skill flutter-add-widget-test --agent universal --copy --yes


一、开发流程类
# 需求分析与头脑风暴
npx skills add obra/superpowers --skill brainstorming --agent universal --copy --yes

# 系统性调试
npx skills add obra/superpowers --skill systematic-debugging --agent universal --copy --yes

# Skill 创建与优化
npx skills add openai/skills --skill skill-creator --agent universal --copy --yes


二、UI 界面设计类
# 前端界面设计（Anthropic）
npx skills add anthropics/skills --skill frontend-design --agent universal --copy --yes

# 移动端界面设计（Sleek）
npx skills add designed-by-ai/skills --skill design-mobile-apps --agent universal --copy --yes



特别提醒： design-mobile-apps 依赖 Sleek 在线 API，实际设计调用涉及 SLEEK_API_KEY，不适合你之前希望完全本地复用、不额外填写 Key 的场景。
三、.NET 开发类
已经找到同名技能的公开版本，尤其是 .NET 集合中的前三个。
# ASP.NET Core
npx skills add managedcode/dotnet-skills --skill aspnet-core --agent universal --copy --yes

# Avalonia UI
npx skills add managedcode/dotnet-skills --skill avalonia --agent universal --copy --yes


四、Office 文档处理类
这四个可以从 Anthropic 的 Skills 仓库单独下载，不用安装整个 Office 合集。
# Word 文档处理
npx skills add anthropics/skills --skill docx --agent universal --copy --yes

# PDF 文件处理
npx skills add anthropics/skills --skill pdf --agent universal --copy --yes

# PowerPoint 演示文稿
npx skills add anthropics/skills --skill pptx --agent universal --copy --yes

# Excel 电子表格
npx skills add anthropics/skills --skill xlsx --agent universal --copy --yes


五、AI Agent 与自动化类
# 主动式 Agent
npx skills add halthelobster/proactive-agent --skill proactive-agent --agent universal --copy --yes

# Agent 自我改进
npx skills add pskoett/self-improving-agent --skill self-improving-agent --agent universal --copy --yes

# GitHub 操作
npx skills add openclaw/openclaw --skill github --agent universal --copy --yes

# 需求追问与方案审查
npx skills add growthx-club/build-sprint-skills --skill grill-me --agent universal --copy --yes

# 网页、文件和视频内容总结
npx skills add openclaw/openclaw --skill summarize --agent universal --copy --yes

# 个性化简历生成
npx skills add composiohq/awesome-claude-skills --skill tailored-resume-generator --agent universal --copy --yes

# 新闻摘要
npx skills add zjfls/zhoujie-claude-skills --skill news-summary --agent universal --copy --yes


六、UI 设计类剩余 Skills
# UI UX Pro Max
npx skills add nextlevelbuilder/ui-ux-pro-max-skill --skill ui-ux-pro-max --agent universal --copy --yes

# Taste Skill（安装 ID 与本地文件夹名不同）
npx skills add Leonxlnx/taste-skill --skill design-taste-frontend --agent universal --copy --yes

# 已有界面重新设计
npx skills add Leonxlnx/taste-skill --skill redesign-existing-projects --agent universal --copy --yes



其中 taste-skill 和 redesign-skill 主要侧重 Web/CSS 界面，对于 Flutter 原生 Widget 开发不建议作为必备项。
七、网络搜索类
# Tavily 搜索（需要 TAVILY_API_KEY）
npx skills add veithly/tavily-search --skill tavily-search --agent universal --copy --yes

# 网页浏览、抓取与浏览器自动化
npx skills add eze-is/web-access --skill web-access --agent universal --copy --yes


web-access 需要 Node.js 22+ 和浏览器环境，且公开安全扫描中存在警告及失败项，建议先检查其脚本再决定是否使用。
八、.NET 测试集合中的独立 Skills
你提供的 test-skills 目录下，我找到了以下 8 个独立技能的公开安装入口。
# 测试过滤语法
npx skills add managedcode/dotnet-skills --skill filter-syntax --agent universal --copy --yes

# 查找没有测试覆盖的源代码
npx skills add managedcode/dotnet-skills --skill find-untested-sources --agent universal --copy --yes

# 检测测试平台
npx skills add managedcode/dotnet-skills --skill platform-detection --agent universal --copy --yes

# 运行测试
npx skills add managedcode/dotnet-skills --skill run-tests --agent universal --copy --yes

# 测试分析扩展参考
npx skills add managedcode/dotnet-skills --skill test-analysis-extensions --agent universal --copy --yes

# 测试反模式检查
npx skills add managedcode/dotnet-skills --skill test-anti-patterns --agent universal --copy --yes

# 测试覆盖缺口分析
npx skills add managedcode/dotnet-skills --skill test-gap-analysis --agent universal --copy --yes

# 测试标签管理
npx skills add managedcode/dotnet-skills --skill test-tagging --agent universal --copy --yes

# 通用联网搜索（国内网络、无 API Key）- 推荐
npx skills add cute-omega/min-web-search-skill --skill min-web-search --agent universal --copy --yes




1. .NET 编码最佳实践
来源：dotnet-best-practices
# .NET / C# 编码最佳实践
npx skills add github/awesome-copilot --skill dotnet-best-practices --agent universal --copy --yes


它包括 XML 注释、SOLID、依赖注入、结构化日志和异常处理等规范。

但有一个缺点：它对主构造函数、MSTest、Semantic Kernel 等技术存在特定偏好。建议下载后稍作修改，明确要求优先遵守项目的 .NET 和 C# 版本，不强制引入特定库。




# 2. Web 页面自动化测试（优先）
npx skills add anthropics/skills --skill webapp-testing --agent universal --copy --yes