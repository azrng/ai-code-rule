# Skills 安装清单

安装命令统一格式：

```
npx skills add <仓库地址> --skill <技能名> --agent universal --copy --yes
```

标签说明：`[必须]` 必装 · `[推荐]` 优先安装 · `[可选]` 按需安装 · `[注意]` 有额外依赖或风险，见文末注意事项。

---

## 一、Flutter 开发

```bash
# Flutter 界面实现 [必须]
npx skills add musabekisakov-imj/flutter-design-engineer --skill flutter-implementation --agent universal --copy --yes

# Flutter 动效 [可选]
npx skills add musabekisakov-imj/flutter-design-engineer --skill flutter-motion --agent universal --copy --yes

# Flutter 页面美化审查 [可选]
npx skills add kamranbekirovyz/skills --skill flutter-improve-design --agent universal --copy --yes

# Flutter Widget 自动化测试 [可选]（Flutter 官方，使用 WidgetTester 验证按钮点击、输入框、列表滚动、状态变化）
npx skills add flutter/agent-plugins --skill flutter-add-widget-test --agent universal --copy --yes
```

## 二、UI / UX 设计

```bash
# UI UX Pro Max [必须]（离线初始化安装）
npx ui-ux-pro-max-cli init --ai universal --offline

# UI UX Pro Max（skills 仓库安装方式，与上二选一）
npx skills add nextlevelbuilder/ui-ux-pro-max-skill --skill ui-ux-pro-max --agent universal --copy --yes

# 前端界面设计（Anthropic）
npx skills add anthropics/skills --skill frontend-design --agent universal --copy --yes

# 移动端界面设计（Sleek）[注意]
npx skills add designed-by-ai/skills --skill design-mobile-apps --agent universal --copy --yes

# Taste Skill（安装 ID 与本地文件夹名不同）
npx skills add Leonxlnx/taste-skill --skill design-taste-frontend --agent universal --copy --yes

# 已有界面重新设计
npx skills add Leonxlnx/taste-skill --skill redesign-existing-projects --agent universal --copy --yes
```

## 三、开发流程

```bash
# 需求分析与头脑风暴
npx skills add obra/superpowers --skill brainstorming --agent universal --copy --yes

# 系统性调试
npx skills add obra/superpowers --skill systematic-debugging --agent universal --copy --yes

# Skill 创建与优化
npx skills add openai/skills --skill skill-creator --agent universal --copy --yes

# 需求追问与方案审查
npx skills add growthx-club/build-sprint-skills --skill grill-me --agent universal --copy --yes
```

## 四、.NET 开发

```bash
# ASP.NET Core
npx skills add managedcode/dotnet-skills --skill aspnet-core --agent universal --copy --yes

# Avalonia UI
npx skills add managedcode/dotnet-skills --skill avalonia --agent universal --copy --yes

# .NET / C# 编码最佳实践 [注意]（含 XML 注释、SOLID、依赖注入、结构化日志、异常处理规范）
npx skills add github/awesome-copilot --skill dotnet-best-practices --agent universal --copy --yes
```

## 五、.NET 测试（test-skills 对应公开仓库）

```bash
# 测试过滤语法
npx skills add managedcode/dotnet-skills --skill filter-syntax --agent universal --copy --yes

# 查找没有测试覆盖的源代码
npx skills add managedcode/dotnet-skills --skill find-untested-sources --agent universal --copy --yes

# 检测测试平台
npx skills add managedcode/dotnet-skills --skill platform-detection --agent universal --copy --yes

# 运行测试
npx skills add managedcode/dotnet-skills --skill run-tests --agent universal --copy --yes

# 测试覆盖缺口分析
npx skills add managedcode/dotnet-skills --skill test-gap-analysis --agent universal --copy --yes

# 测试标签管理
npx skills add managedcode/dotnet-skills --skill test-tagging --agent universal --copy --yes
```

## 六、测试工具（Web）

```bash
# Web 页面自动化测试 [推荐]
npx skills add anthropics/skills --skill webapp-testing --agent universal --copy --yes
```

## 七、Office 文档处理（均出自 anthropics/skills，可单独安装，无需整个 Office 合集）

```bash
# Word 文档处理
npx skills add anthropics/skills --skill docx --agent universal --copy --yes

# PDF 文件处理
npx skills add anthropics/skills --skill pdf --agent universal --copy --yes

# PowerPoint 演示文稿
npx skills add anthropics/skills --skill pptx --agent universal --copy --yes

# Excel 电子表格
npx skills add anthropics/skills --skill xlsx --agent universal --copy --yes
```

## 八、AI Agent 与自动化

```bash
# 个性化简历生成
npx skills add composiohq/awesome-claude-skills --skill tailored-resume-generator --agent universal --copy --yes

# 新闻摘要
npx skills add zjfls/zhoujie-claude-skills --skill news-summary --agent universal --copy --yes
```

## 九、网络搜索与浏览

```bash
# 通用联网搜索（国内网络、无 API Key）[推荐]
npx skills add cute-omega/min-web-search-skill --skill min-web-search --agent universal --copy --yes

```

---

## 注意事项

1. **design-mobile-apps**：依赖 Sleek 在线 API，实际调用需要 `SLEEK_API_KEY`，不适合完全本地复用、不额外填写 Key 的场景。
2. **taste-skill / redesign-existing-projects**：侧重 Web/CSS 界面，Flutter 原生 Widget 开发不建议作为必备项。
5. **dotnet-best-practices**：对主构造函数、MSTest、Semantic Kernel 有特定偏好；建议下载后稍作修改，明确要求优先遵守项目自身的 .NET / C# 版本约定，不强制引入特定库。
6. **UI UX Pro Max**：`ui-ux-pro-max-cli init` 与 `skills add ui-ux-pro-max` 为同一技能的两种安装方式，任选其一即可。
