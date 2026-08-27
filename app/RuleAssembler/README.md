# RuleAssembler — 交互式 AI Rule 拼接器

把一套完整 AI rule 释放到当前目录：双击启动 → 选择技术大类 → 选择 Profile → 直接释放。默认不使用命令行参数完成业务选择。

## 设计

`fragments/` 是唯一维护源，生成器把公共模块、Profile 根变量和 Profile 专属规则组合成目标文件：

```text
fragments/
├── root/AGENTS.md         # 公共根骨架，含变量槽
├── pointers/              # 公共入口指针
├── arch/<id>/             # 当前 Profile 的专属规则片段
├── manifest.json          # 模块、家族、Profile 和输出配方
└── ...                    # design-system 等能力片段
```

公共能力通过 `modules` 复用。例如 `core` 提供根入口和指针，`ai-capability` 提供 `ai-AGENTS.md`。现有技术栈专属文件暂按 Profile 保留，避免为一次模块化迁移改写大量规则正文；新增组合优先使用模块，不再复制整套 Profile。

## 构建

```powershell
./publish.ps1
```

产物位于仓库根目录 `dist/use-rule.exe`（底层发布目录仍保留在 `bin/Release/net10.0/win-x64/publish/`）。当前发布为 Windows x64、框架相关单文件，目标机需要安装对应的 .NET 10 运行时；这是体积和部署依赖之间的明确取舍。

## 使用

```text
1. 把 use-rule.exe 拷贝到目标项目根目录
2. 双击启动
3. 选择技术大类
4. 选择 Profile
5. 确认释放
```

目标目录已有同名文件时会直接覆盖。文件会先写入临时目录，全部准备成功后再提交；提交中途失败会回滚本次已经写入的文件。不会释放 `TASK.md`、`rulelog` 或 `devlog`。

维护和 CI 可以使用只读校验：

```powershell
dotnet run --project app/RuleAssembler/RuleAssembler.csproj -- --check
```

## 当前 Profile

manifest 当前包含 8 个技术大类、19 个 Profile。新增 Profile 时，在 `fragments/manifest.json` 增加配方，并在 `fragments/arch/<id>/` 增加对应片段；新增可复用能力时优先增加 `modules`，再由 Profile 通过 `moduleIds` 引用。

## 版本与格式

- 规则版本和更新时间由 `manifest.json` 的 `vars` 统一控制。
- 所有生成文件统一使用 UTF-8 无 BOM 和 manifest 指定的 LF 行尾。
- 指针文件保留自身独立的 `rule_id` 和版本元数据。
- 根目录现有的 19 套架构目录仅是历史/生成快照，不是维护入口。
