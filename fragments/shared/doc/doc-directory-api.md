### 文档目录约定

需求文档和设计文档按业务模块拆分，不按 Controller、Service、Repository 等技术层拆分。模块下继续按功能拆分，使同一功能的需求、设计和契约可以通过稳定 ID 互相追溯。

```text
doc/
├── index.md                         # 文档总索引，只做导航
├── requirements/
│   ├── index.md                     # 需求索引与项目级目标
│   └── <module>/<feature>.md        # 业务需求
├── design/
│   ├── index.md                     # 设计索引
│   ├── architecture.md              # 跨模块架构设计
│   └── <module>/<feature>.md        # 功能设计
└── contracts/
    └── <module>.md                  # 可选的契约索引或补充说明
```

- `doc/index.md`：列出模块、功能、文档状态、文档路径和关联关系；内容必须轻量，不能替代具体文档
- `doc/requirements/index.md`：维护项目目标、范围和模块目录；详细业务需求放在模块/功能文件中
- `doc/requirements/<module>/<feature>.md`：只描述为什么做、给谁用、做什么、业务规则、权限、边界和验收标准，不写技术实现方案
- `doc/design/index.md`：维护设计文档导航和整体设计状态
- `doc/design/architecture.md`：只记录跨模块架构、公共数据流、公共技术决策和跨模块约束
- `doc/design/<module>/<feature>.md`：只描述对应需求的实现方案、层职责、数据流、状态、接口处理、异常和技术决策；通过需求 ID 引用需求，不重复粘贴完整需求
- `doc/contracts/<module>.md`：仅在需要给多个模块提供契约索引或补充说明时使用；DTO、Schema、公开接口定义仍以代码或共享 Schema 为最终事实来源
- 新文档至少标注 `doc_id`、`doc_type`、`module`、`status`、`last_updated` 和关联文档路径；需求文档使用 `REQ-` ID，设计文档使用 `DES-` ID
- 新功能应同时创建同模块的需求文档和设计文档，例如 `requirements/user/login.md` 与 `design/user/login.md`
- 新项目或多模块项目直接使用上述结构；单模块小项目可以暂时保留单文件，但文档超过约 300 行、包含多个业务模块，或任务通常只需要其中一部分时，必须迁移拆分
- 旧项目中的 `doc/requirement.md` 和 `doc/design/设计文档.md` 视为兼容入口。迁移时按业务模块和功能切分原有章节，更新索引；迁移完成后旧文件只保留迁移说明和链接，不再追加新内容
