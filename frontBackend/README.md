# AI Code Rule 仓库说明

本仓库定义了 AI Agent 协作开发医疗行业管理系统的统一规范、流程与技术栈。

---

## 文档结构

| 文件 | 说明 |
|------|------|
| `README.md` | 本文件，汇总所有说明文档 |
| `AGENTS.md` / `CLAUDE.md` / `GEMINI.md` | 各 AI Agent 统一工作规则（内容同步） |
| `design-system.yaml` | 设计 token 与 UI 规范 |
| `TASK.md` | 具体任务内容与进度 |

---

## 注意事项

1. 理解用户需求的业务逻辑后再做设计和开发
2. 根据功能需求设计多级菜单，管理员默认拥有所有菜单权限
3. 功能模块必须有完整可操作的功能页面
4. 所有功能至少确保能够完成基本流程操作，按钮必须可交互
5. 系统需有登录页面，默认应用图标，登录页面左上角显示应用图标和名称
6. 动效需符合医疗行业审美，界面简洁大方，操作流程清晰
7. 变更、新增、删除等业务逻辑必须真实生效，刷新页面后能看到生效内容
8. 功能模块较多时，先搭建系统框架，再按子模块逐个实现

---

## 技术栈

### 前端
- React 18 + TypeScript
- Tailwind CSS v3（唯一样式引擎）
- lucide-react（唯一图标库，尺寸 18-24px）
- ECharts + echarts-for-react（唯一图表库）
- Zustand（全局状态管理）
- React Hooks（本地组件状态）

### 后端
- Python + FastAPI + SQLAlchemy + Alembic
- 分层架构：`router → service → repository → model`

### 部署
- Docker + Docker Compose
- 数据库：PostgreSQL

### 默认端口
| 服务 | 容器内端口 | 宿主机映射端口 |
|------|------------|----------------|
| 前端（React） | 3000 | 3000 |
| 后端（FastAPI） | 8000 | 8000 |
| 数据库（PostgreSQL） | 5432 | 5432 |

- 前端 API 请求指向 `http://localhost:8000`，通过 `VITE_API_BASE_URL` 配置

---
