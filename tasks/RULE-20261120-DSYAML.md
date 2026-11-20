# RULE-20261120-DSYAML：design-system.yaml 语法损坏修复与守卫

## 发现

用户要求向 D:\temp\skillsOutput 输出全量组合排查问题。`--emit` 后用 YAML 解析器
（Bun.YAML，与 YamlDotNet 独立交叉验证口径一致）扫 33 个 design-system.yaml 产物：
6 个解析失败（staticHtml、vueLinkdotNet、vueLinkdotNetSingle、vueLinkPython、
vueOnly、vueOnly-ai）。

## 根因

产物失败但 --check / emit 基线全绿——生成器把 design-system.yaml 当不透明文本拷贝，
无任何语法校验，损坏从片段一路渲染到产物无人拦截。真实源损坏 2 处（骨架模板的
`{{slot:}}` 占位不算损坏，渲染时被替换）：

1. `arch/vueLinkdotNet/design-system.yaml:437`：`description: "紧凑型，用于筛选栏、
   内联表单，      el_size: "small"` —— el_size 键行粘连进 description 且引号未闭合。
   该片段被 vueLinkdotNet / vueLinkdotNetSingle / vueLinkPython / vueOnly / vueOnly-ai
   五个样例（含 vueOnly 纯前端系）经 designSystemSource 复用，一处损坏传导 5 个产物。
2. `arch/staticHtml/design-system.yaml:560`：`separator: "/"（Bootstrap 默认分隔符）`
   —— 引号值后裸接全角括号说明，非法 YAML；同文件 :595 `container:
   "placeholder-glow" 或 "placeholder-wave"` —— 引号值后裸接「或」文本，同类损坏
   （首轮定位只命中 :560，逐段截断复扫才暴露 :595）。

对照正确结构（vueWailsExe sizes.sm / reactLinkdotNet 同段）确认修复口径；
`# 注释`写法与文件既有 separator/颜色注释口径一致。

## 修复

- vueLinkdotNet:437 拆回 `description` + `el_size: "small"` 两行（对照 vueWailsExe）。
- staticHtml:560 行内说明改 `#` 注释；:595 第二方案改 `#` 注释保留备选信息。
- 升版 3.21.0 → 3.21.1（fragments 变更必须升版）；emit 基线随版本戳全量轮换刷新。
- 防复发守卫：`test/RuleAssembler.Tests/DesignSystemYamlTests.cs`——全部兼容样例
  渲染的 .yaml 产物逐个 YamlDotNet 反序列化，失败列出「样例/文件: 原因」；
  测试项目新增 YamlDotNet 18.1.0 引用。已用注入验证守卫有效：把 :560 改回全角
  括号损坏，测试即失败（1 失败），还原后恢复通过——非恒真断言。

## 验证

- 源片段扫描：19 个 yaml，损坏 0（骨架 2 个为槽位模板，渲染时替换，产物校验覆盖）。
- 重 emit 39 样例到 D:\temp\skillsOutput：33 个产物 YAML 全部可解析，
  AGENTS.md 版本戳统一 3.21.1。
- --check 39 样例全过、53 技能登记齐全；dotnet test 280 全绿（+1 新守卫）。
- 其余排查面同步核过：include 注释残留 0、槽位占位残留 0（{{ 命中均为
  style={{}} JSX 内容）、39 个 AGENTS.md front matter 完整、子规则索引双向一致
  （引用↔实际文件无缺失无孤儿）、CLAUDE/GEMINI/GROK 指针齐全。
