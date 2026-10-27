---
name: browser-walkthrough
description: "浏览器自动化走查手册（Blazor / Web 界面交付走查）：独立实例隔离、组件库点击兜底顺序、真实键入、rAF 摆位判定、瞬态伪影复核。Keywords: Playwright, 走查, walkthrough, Blazor, MudBlazor, BootstrapBlazor, 截图, 浏览器验证."
---

# 浏览器自动化走查

界面交付前的浏览器走查操作手册。强制要求（必须执行走查、独立实例、以稳态 DOM 为准）由项目 `ui-AGENTS.md` 的门控句约束；本技能提供具体操作细节。

## 独立实例隔离

- 走查用独立实例：独立数据根与端口，不动开发实例的数据与配置。
- 需改运行配置时走「备份 → 注入 → 还原」，不要依赖环境变量重定向用户目录（部分应用只认配置文件路径，环境变量无效）。
- 注入的配置键用 camelCase、路径用正斜杠。

## 组件库控件操作

- 组件库封装的原生控件（MudBlazor 的 Button / Switch / Select 等）Playwright 原生点击经常整体超时：**点击超时先换兜底再怀疑应用缺陷**，兜底顺序为原生点击 → JS `evaluate` click → 对开关类补 `checked` + `change` 事件；每种兜底试过仍失败才进入缺陷排查。
- 输入一律用真实键入路径（Playwright `fill` / `type`）；JS 合成事件对 picker 类组件（日期/级联选择）无效，会静默不更新组件状态。

## 下拉摆位与渲染判定

- 下拉 popover 初始 CSS 定位在屏外（防闪烁），摆位由 `requestAnimationFrame` 调度——后台窗口 rAF 可能冻结导致下拉停在屏外：这是走查环境特性不是缺陷；判定法是手动调用摆位逻辑或真实浏览器前台验证是否立即正确落位。
- 加载窗口期的瞬态状态与 fullPage 截图拼接伪影不作为缺陷上报，以稳态 DOM 与 computed style 实测为准；疑似缺陷先等稳态或改用局部截图复核。
