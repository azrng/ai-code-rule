## [2026-08-07]：dotnetLayered 与 dotnetBlazor 版本号统一到 1.10.0

跟踪状态表：

| 文件夹名称 | 当前子任务状态 | 备注 |
|-----------|----------------|------|
| `dotnetLayered` | `已完成` | 根 / doc / infra 由 1.9.0 / 1.8.0 统一到 1.10.0；backend 已是 1.10.0 |
| `dotnetBlazor` | `已完成` | 根 / doc / infra / ui 由 1.9.0 / 1.8.0 / 1.7.0 统一到 1.10.0；backend 已是 1.10.0 |

### 修改原因

dotnetLayered / dotnetBlazor 于 8/6 改造（当时指令为「dotnetBlazor 对齐 dotnetLayered」），版本停留在 1.8.0 / 1.9.0；8/7 对其余 8 个 .NET 模板下达「统一 1.10.0」指令时未覆盖这两个。审查发现全 10 模板版本号不齐，用户要求统一，本次补齐。

### 主要修改内容

- `dotnetLayered/AGENTS.md`（1.9.0 → 1.10.0）、`doc-AGENTS.md`（1.8.0 → 1.10.0）、`infra-AGENTS.md`（1.8.0 → 1.10.0）；`backend-AGENTS.md` 已是 1.10.0，不动
- `dotnetBlazor/AGENTS.md`（1.9.0 → 1.10.0）、`doc-AGENTS.md`（1.8.0 → 1.10.0）、`infra-AGENTS.md`（1.8.0 → 1.10.0）、`ui-AGENTS.md`（1.7.0 → 1.10.0）；`backend-AGENTS.md` 已是 1.10.0，不动
- 以上文件 `last_updated` 同步到 2026-08-07

### 修改文件

- `dotnetLayered/AGENTS.md`、`dotnetLayered/doc-AGENTS.md`、`dotnetLayered/infra-AGENTS.md`
- `dotnetBlazor/AGENTS.md`、`dotnetBlazor/doc-AGENTS.md`、`dotnetBlazor/infra-AGENTS.md`、`dotnetBlazor/ui-AGENTS.md`

### 当前子任务状态

- `已完成`

### 备注

- 至此全部 10 个 .NET 模板的规则文件版本号统一为 1.10.0。仅版本元数据变更，无规则内容改动。
