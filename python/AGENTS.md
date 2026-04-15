# AGENTS.md

## 目标
职责划分如下：
- `AGENTS.md`：唯一协作规范来源，统一定义工作规则、任务机制、交付要求
- `CLAUDE.md` / `GEMINI.md`：各 Agent 的入口说明文件，仅负责提醒先读取 `AGENTS.md`，不维护平行规则内容
- `TASK.md`：当前活跃任务与少量最近完成任务的最小记录
- `TaskHistory.md`：归档历史 `DONE` 任务的最小记录

---

## 工作方法

1. 行动前先思考，写代码前先阅读现有文件并理解业务目标。
2. 输出保持简洁，但分析与落地必须完整。
3. 优先编辑现有文件，而不是无差别重写。
4. 已确认无变更风险的文件无需重复阅读，但遇到上下文缺口时必须补读。
5. 完成前尽量执行可落地的校验，无法执行时明确说明原因和影响。
6. 避免奉承性开头或冗余结尾，直接进入有价值的信息。
7. 保持解决方案简单直接，避免过度设计。
8. 用户指令始终优先于本文件内容。
9. Windows / PowerShell 场景下命令不接受 `&&`，禁止使用；需要连续执行时改为分步执行或使用 PowerShell 兼容写法。

---

## 注意事项
1. 先理解用户提出的业务目标、系统边界和调用链，再设计 API、数据模型与任务流程。
2. 所有接口、脚本、任务调度与数据处理逻辑都必须真实可执行，不能只停留在示例或伪代码层。
3. 业务逻辑、HTTP 编排、数据访问和外部集成要分层清晰，避免把复杂逻辑堆在入口文件中。
4. 涉及新增、修改、删除或异步任务的行为必须可验证，重启服务、重跑任务或重新请求后仍能看到真实结果。
5. 复杂系统优先先搭建项目骨架、配置、依赖与基础能力，再按模块逐步实现，不一次性铺开全部功能。

---

## 编码与注释规则

- 所有源码、配置、文档文件统一使用 `UTF-8` 编码。
- Windows / PowerShell 场景下命令不接受 `&&`，禁止使用；需要连续执行时改为分步执行或使用 PowerShell 兼容写法。
- 如果编写注释（含代码注释、文档注释、docstring 等），统一使用中文，不使用纯英文注释；必要的英文标识、协议字段名或框架关键字可保留原文。

---

## 一、技术栈

### 1.1 核心框架

- **Python 3.12+**，使用类型注解（type hints）
- **FastAPI** 作为 Web 框架，基于 ASGI（推荐 Uvicorn 作为服务器）
- **Pydantic V2** 用于数据验证和序列化
- **SQLAlchemy 2.0+**（异步模式）作为 ORM，配合 Alembic 进行数据库迁移（**可选**，无数据库需求的项目不使用）

### 1.2 常用库

| 用途 | 库 | 说明 |
|------|----|------|
| Web 框架 | `fastapi` | 异步高性能 API 框架 |
| ASGI 服务器 | `uvicorn[standard]` | 生产级 ASGI 服务器 |
| ORM | `sqlalchemy[asyncio]`（可选） | 异步 ORM |
| 数据库迁移 | `alembic`（可选） | 数据库版本管理 |
| 数据验证 | `pydantic` | 请求/响应模型验证 |
| Redis | `redis[hiredis]` | 缓存与消息队列 |
| HTTP 客户端 | `httpx` | 异步 HTTP 客户端 |
| 任务队列 | `celery[redis]`（可选） | 异步任务处理 |
| 日志 | `loguru` | 结构化日志 |
| 配置管理 | `pydantic-settings` | 环境变量与配置管理 |
| 测试 | `pytest` + `pytest-asyncio` + `httpx` | 异步测试支持 |
| 代码质量 | `ruff` | Linter + Formatter（替代 flake8、isort、black） |

### 1.3 包管理

使用 **uv** 作为包管理工具（替代 pip），统一管理依赖和虚拟环境。

```bash
# 初始化项目
uv init

# 添加依赖
uv add fastapi uvicorn sqlalchemy alembic

# 添加开发依赖
uv add --dev pytest pytest-asyncio ruff

# 安装所有依赖
uv sync
```

### 1.4 数据库支持（可选）

当项目需要数据库时，默认 PostgreSQL（推荐 `asyncpg` 驱动），可选：

- MySQL / MariaDB（`aiomysql`）
- SQLite（`aiosqlite`，仅开发/测试）
- SQL Server（`aioodbc`）

### 1.5 依赖注入

使用 FastAPI 原生 `Depends` 机制进行依赖注入，结合 `Annotated` 类型注解实现声明式注入。

---

## 二、项目结构

```
project/
├── app/
│   ├── __init__.py
│   ├── main.py                 # FastAPI 应用入口
│   ├── config.py               # 配置管理（pydantic-settings）
│   ├── database.py             # 数据库引擎和会话管理（可选，无数据库时移除）
│   ├── dependencies.py         # 全局依赖注入
│   ├── exceptions.py           # 自定义异常类
│   ├── middleware/              # 中间件
│   │   ├── __init__.py
│   │   ├── error_handler.py    # 全局异常处理
│   │   └── logging.py          # 请求日志中间件
│   ├── api/                    # API 路由
│   │   ├── __init__.py
│   │   ├── v1/
│   │   │   ├── __init__.py
│   │   │   └── router.py       # v1 路由注册
│   │   └── deps.py             # API 层公共依赖
│   ├── services/               # 业务逻辑层
│   │   ├── __init__.py
│   │   └── interfaces/         # 服务接口（Protocol）
│   │       └── __init__.py
│   ├── models/                 # SQLAlchemy 数据模型（可选，无数据库时移除）
│   │   ├── __init__.py
│   │   ├── base.py             # 模型基类
│   │   └── entities/           # 数据库实体
│   │       └── __init__.py
│   ├── schemas/                # Pydantic 模型（DTO）
│   │   ├── __init__.py
│   │   ├── base.py             # 基础 Schema
│   │   ├── requests/           # 请求模型
│   │   │   └── __init__.py
│   │   └── responses/          # 响应模型
│   │       └── __init__.py
│   ├── repositories/           # 数据访问层（可选，无数据库时移除）
│   │   ├── __init__.py
│   │   └── base.py             # 通用 Repository
│   ├── cache/                  # 缓存层
│   │   ├── __init__.py
│   │   └── redis.py            # Redis 缓存实现
│   └── utils/                  # 工具函数
│       ├── __init__.py
│       └── helpers.py
├── migrations/                 # Alembic 迁移文件（可选，无数据库时移除）
│   ├── env.py
│   ├── alembic.ini
│   └── versions/
├── tests/                      # 测试目录
│   ├── conftest.py             # 测试 fixtures
│   ├── unit/
│   ├── integration/
│   └── e2e/
├── doc/                        # 文档目录
│   ├── requirement.md          # 需求文档
│   ├── design/                 # 设计文档
│   │   └── 设计文档.md
│   └── devlog/                 # 开发日志
├── .env.example                # 环境变量示例
├── pyproject.toml              # 项目配置（PEP 621）
├── uv.lock                    # uv 锁文件（自动生成）
├── Dockerfile                  # Docker 构建
├── docker-compose.yml          # 容器编排
├── AGENTS.md                   # AI 代理规则
├── CLAUDE.md                   # Claude 指针
├── GEMINI.md                   # Gemini 指针
├── TASK.md                     # 当前任务记录
└── TaskHistory.md              # 历史任务归档
```

---

## 三、后端开发规则

### 3.1 API 路由层（Controller）

- **职责**：仅处理 HTTP 请求/响应，不包含业务逻辑
- 使用 `APIRouter` 组织路由，按版本隔离（`api/v1/`）
- 路由函数使用 `async def`
- 所有响应使用统一响应模型 `ResultResponse[T]`
- 通过 `Depends` 注入服务层
- 使用 Pydantic 模型进行请求体验证

```python
from fastapi import APIRouter, Depends
from app.schemas.base import ResultResponse
from app.schemas.requests import CreateUserRequest
from app.schemas.responses import UserResponse
from app.services.interfaces.user_service import IUserService
from app.api.deps import get_user_service

router = APIRouter(prefix="/users", tags=["用户管理"])

@router.post("", response_model=ResultResponse[UserResponse])
async def create_user(
    request: CreateUserRequest,
    service: IUserService = Depends(get_user_service),
):
    user = await service.create(request)
    return ResultResponse.success(data=user)
```

### 3.2 服务层（Service）

- **职责**：承载核心业务逻辑
- 使用 `Protocol` 定义服务接口（Python 的结构化子类型）
- 所有方法必须为 `async`
- 数据传输使用 Pydantic Schema（DTO），不直接暴露 ORM 模型
- 异常使用自定义业务异常类

```python
from typing import Protocol
from app.schemas.requests import CreateUserRequest
from app.schemas.responses import UserResponse

class IUserService(Protocol):
    async def create(self, request: CreateUserRequest) -> UserResponse: ...
    async def get_by_id(self, user_id: int) -> UserResponse: ...
    async def update(self, user_id: int, request: UpdateUserRequest) -> UserResponse: ...
    async def delete(self, user_id: int) -> None: ...
```

### 3.3 数据模型（Model）

#### ORM 模型（SQLAlchemy，可选）

- 继承统一的 `Base` 类，包含 `id`、`created_at`、`updated_at`、`is_deleted` 字段
- 使用声明式映射（Declarative Base）
- 使用 `Mapped` 类型注解
- 表名使用 snake_case

```python
from datetime import datetime
from sqlalchemy import String, Boolean
from sqlalchemy.orm import Mapped, mapped_column
from app.models.base import Base

class User(Base):
    __tablename__ = "users"

    username: Mapped[str] = mapped_column(String(50), unique=True)
    email: Mapped[str] = mapped_column(String(100))
    is_active: Mapped[bool] = mapped_column(Boolean, default=True)
```

#### Pydantic Schema（DTO）

按用途分离 Schema：
- `*Request` — 请求模型（创建/更新/查询参数）
- `*Response` — 响应模型（返回给前端的数据）
- `*Filter` — 查询过滤条件

```python
from datetime import datetime
from pydantic import BaseModel, EmailStr, Field

class CreateUserRequest(BaseModel):
    username: str = Field(..., min_length=2, max_length=50)
    email: EmailStr

class UserResponse(BaseModel):
    id: int
    username: str
    email: str
    is_active: bool
    created_at: datetime

    model_config = {"from_attributes": True}
```

### 3.4 数据访问层（Repository，可选）

- 使用 SQLAlchemy 2.0 异步查询风格（`select` / `execute`）
- 通过 `AsyncSession` 进行数据库操作
- 支持软删除（`is_deleted` 字段 + 查询过滤）
- 通用 Repository 封装基础 CRUD，特定 Repository 继承扩展

```python
from typing import TypeVar, Generic
from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession

ModelType = TypeVar("ModelType")

class BaseRepository(Generic[ModelType]):
    def __init__(self, model: type[ModelType], session: AsyncSession):
        self.model = model
        self.session = session

    async def get_by_id(self, id: int) -> ModelType | None:
        result = await self.session.execute(
            select(self.model).where(
                self.model.id == id,
                self.model.is_deleted == False
            )
        )
        return result.scalar_one_or_none()
```

### 3.5 统一响应格式

```python
from typing import TypeVar, Generic
from pydantic import BaseModel

T = TypeVar("T")

class ResultResponse(BaseModel, Generic[T]):
    code: int = 200
    message: str = "success"
    data: T | None = None

    @classmethod
    def success(cls, data: T = None, message: str = "success") -> "ResultResponse[T]":
        return cls(code=200, message=message, data=data)

    @classmethod
    def failure(cls, message: str, code: int = 400) -> "ResultResponse[T]":
        return cls(code=code, message=message, data=None)
```

### 3.6 异常处理

定义五种标准业务异常，由全局异常处理中间件统一捕获：

| 异常类 | HTTP 状态码 | 用途 |
|--------|-------------|------|
| `BusinessException` | 400 | 通用业务逻辑错误 |
| `ParameterException` | 422 | 参数校验错误 |
| `NotFoundException` | 404 | 资源未找到 |
| `ForbiddenException` | 403 | 权限不足 |
| `InternalServerException` | 500 | 服务器内部错误 |

```python
from fastapi import Request
from fastapi.responses import JSONResponse
from app.exceptions import BusinessException

async def business_exception_handler(request: Request, exc: BusinessException) -> JSONResponse:
    return JSONResponse(
        status_code=exc.status_code,
        content={"code": exc.error_code, "message": exc.message, "data": None}
    )
```

### 3.7 缓存

- **分布式缓存**：使用 `redis.asyncio` 封装 `RedisProvider`
- **本地缓存**：使用 `cachetools` 的 `TTLCache`
- **两级缓存**：先查本地缓存，未命中再查 Redis

```python
from redis.asyncio import Redis

class RedisProvider:
    def __init__(self, redis: Redis):
        self.redis = redis

    async def get(self, key: str) -> str | None:
        return await self.redis.get(key)

    async def set(self, key: str, value: str, expire: int = 300) -> None:
        await self.redis.setex(key, expire, value)

    async def delete(self, key: str) -> None:
        await self.redis.delete(key)
```

### 3.8 数据库迁移（可选）

- 使用 **Alembic** 进行数据库版本管理
- 迁移脚本自动生成后必须检查并确认
- 每个迁移必须包含 **upgrade** 和 **downgrade**
- 生产环境执行迁移前必须备份数据库

```bash
# 生成迁移脚本
alembic revision --autogenerate -m "描述"

# 执行迁移
alembic upgrade head

# 回滚迁移
alembic downgrade -1
```

### 3.9 API 文档

FastAPI 自带 Swagger UI 和 ReDoc，确保：
- 所有路由添加 `summary` 和 `description`
- 使用 `response_model` 明确响应类型
- Pydantic 模型添加 `description` 字段说明
- 按 `tags` 分组管理接口

### 3.10 命名规范

| 场景 | 规范 | 示例 |
|------|------|------|
| 文件名 | snake_case | `user_service.py` |
| 类名 | PascalCase | `UserService` |
| 函数/方法 | snake_case | `get_user_by_id` |
| 变量 | snake_case | `user_count` |
| 常量 | UPPER_SNAKE_CASE | `MAX_RETRY_COUNT` |
| 数据库表名 | snake_case（复数） | `users`, `user_roles` |
| 数据库列名 | snake_case | `created_at`, `is_deleted` |
| API 路径 | kebab-case（小写复数） | `/api/v1/user-roles` |
| 环境变量 | UPPER_SNAKE_CASE | `DATABASE_URL` |

---

## 四、测试规则

### 4.1 测试框架

- **pytest** + **pytest-asyncio** 作为测试框架
- **httpx** 用于 API 集成测试（`AsyncClient`）
- 测试中使用 `aiosqlite` 内存数据库，不依赖真实数据库

### 4.2 测试分类

| 类型 | 说明 | 示例 |
|------|------|------|
| 单元测试 | Mock 外部依赖，测试单一函数/类 | Service 层逻辑测试 |
| 集成测试 | 测试多组件协作，使用内存数据库 | Repository 数据访问测试 |
| API 测试 | 通过 TestClient 测试完整请求链路 | 端到端接口测试 |

### 4.3 测试规范

- 测试文件命名：`test_*.py`
- 测试函数命名：`test_<功能>_<场景>_<预期结果>`
- 每个测试独立，不依赖执行顺序
- 使用 `fixtures`（conftest.py）管理测试数据
- **禁止**在测试中调用真实第三方服务（使用 mock 或录制回放）
- 覆盖率目标：核心业务逻辑 ≥ 80%

```python
import pytest
from httpx import AsyncClient, ASGITransport
from app.main import app

@pytest.fixture
async def client():
    transport = ASGITransport(app=app)
    async with AsyncClient(transport=transport, base_url="http://test") as ac:
        yield ac

async def test_create_user_success(client):
    response = await client.post("/api/v1/users", json={"username": "test", "email": "test@example.com"})
    assert response.status_code == 200
    data = response.json()
    assert data["code"] == 200
    assert data["data"]["username"] == "test"
```

---

## 五、Git 与提交规范

### 5.1 分支命名

| 类型 | 格式 | 示例 |
|------|------|------|
| 功能 | `feat/<简述>` | `feat/user-management` |
| 修复 | `fix/<简述>` | `fix/login-error` |
| 重构 | `refactor/<简述>` | `refactor/auth-module` |
| 杂项 | `chore/<简述>` | `chore/update-deps` |
| 文档 | `docs/<简述>` | `docs/api-guide` |

### 5.2 提交信息格式

遵循 Conventional Commits 规范：

```
<type>(<scope>): <subject>

<body>
```

类型：`feat` | `fix` | `refactor` | `docs` | `test` | `chore` | `style` | `perf`

### 5.3 提交要求

- 每次修改后必须提交
- 提交信息使用中文描述
- 交付前自检清单：
  - [ ] 代码通过 `ruff check` 和 `ruff format`
  - [ ] 所有测试通过（`pytest`）
  - [ ] 迁移脚本已生成（如有数据库变更且使用数据库）
  - [ ] 环境变量已更新 `.env.example`
  - [ ] Docker 构建验证通过

---

## 六、Docker 规范

### 6.1 Dockerfile

- 使用多阶段构建
- 基础镜像选择 `python:3.12-slim`
- 使用 `non-root` 用户运行
- 不在镜像中包含密钥或敏感信息
- 安装依赖时使用 `--no-cache-dir`

```dockerfile
FROM python:3.12-slim AS builder
WORKDIR /app
COPY pyproject.toml ./
RUN pip install --no-cache-dir -e .
# 或使用 uv：COPY --from=ghcr.io/astral-sh/uv:latest /uv /usr/local/bin/uv
# RUN uv sync --no-dev --frozen

FROM python:3.12-slim
WORKDIR /app
COPY --from=builder /usr/local/lib/python3.12/site-packages /usr/local/lib/python3.12/site-packages
COPY --from=builder /usr/local/bin /usr/local/bin
COPY app/ ./app/
COPY migrations/ ./migrations/
EXPOSE 8000
CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "8000"]
```

### 6.2 Docker Compose

- 包含 `app`、`db`（PostgreSQL）、`redis` 服务
- 配置健康检查（`healthcheck`）
- 端口冲突时调整宿主机端口映射
- 实现后必须执行构建并验证容器启动

---

## 七、任务管理机制
`TASK.md` 与 `TaskHistory.md` 共同组成任务记录文件：`TASK.md` 只维护活跃任务与少量最近完成任务的最小记录，`TaskHistory.md` 用于归档历史 `DONE` 任务，两者均不负责承载规则解释。

### 任务状态
- `TODO`：未开始
- `DOING`：进行中
- `BLOCKED`：被阻塞
- `REVIEW`：已完成开发，等待确认
- `DONE`：已完成

### 执行规则
1. 开始工作前，先查看 `TASK.md` 是否已有对应任务。
2. 如果已有任务，优先更新原任务，不重复新建。
3. 如果没有任务，在 `TASK.md` 中新增最小任务记录。
4. 开始执行时，将任务状态更新为 `DOING`。
5. 若任务无法继续，更新为 `BLOCKED` 并写明原因。
6. 开发完成但仍需确认时，更新为 `REVIEW`。
7. 验证完成并确认交付后，更新为 `DONE`。
8. 当 `TASK.md` 中任务总数超过 30 条时，将最早完成的 `DONE` 任务迁移到 `TaskHistory.md`，直到 `TASK.md` 中任务数回落到 20 条以内。
9. 归档时仅允许迁移 `DONE` 任务，`TODO`、`DOING`、`BLOCKED`、`REVIEW` 任务必须保留在 `TASK.md`。
10. 归档任务时保留原任务 ID、任务信息与最近更新时间，并在 `TaskHistory.md` 中补充归档时间。
11. 发生任务归档时，在 `doc/devlog/` 中补充一份简短归档说明，记录迁移范围与触发原因。

### 每个任务至少包含
- 任务 ID
- 任务名称
- 任务目标
- 当前阶段
- 负责人 AI
- 任务状态
- 优先级
- 最近更新时间

### AI Agent 要求
- 开始任务前先同步任务状态。
- 开发过程中及时更新 `TASK.md`。
- 触发归档阈值时同步维护 `TaskHistory.md`。
- 范围变化时同步补充任务说明、验收标准和风险。
- 每次开发完成后，都需要在 `doc/devlog/` 下新增一份本次开发简短说明的 Markdown 记录。
---

## 八、文档更新要求
以下内容发生变化时，必须同步更新相关文档：
- 命令、脚本或启动方式
- 环境变量
- 配置项
- 构建、发布或部署流程
- 数据库结构、迁移或初始化方式
- 对外使用方式或接口约定

优先更新已有文档，不新增重复说明。
每次开发完成后，必须在 `doc/devlog/` 下补充一份简短开发记录。

### 文档目录约定
- `doc/requirement.md`：需求文档。若文件已存在，AI 应基于用户新增需求进行补充或修订；若文件不存在，AI 必须根据用户输入先自动生成详细初稿。该文件仅承载业务需求，不写入设计说明或开发过程记录
- `doc/design/设计文档.md`：项目设计文档，全项目一份。包含架构设计、功能需求、界面原型（如适用）与交互说明。由用户与 AI 协作产出，用户确认后作为开发的唯一需求基准。AI 可在文件中追加新功能章节，不删除已有内容，不新建平行文件
- `doc/devlog/`：每次开发完成后的简短开发日志

### requirement 要求
- 若 `doc/requirement.md` 为模板初始状态，AI 应根据用户输入将占位内容替换为项目真实需求，而不是长期保留“待补充”空壳。
- 需求文档建议固定包含以下章节：项目背景、目标与范围、用户角色、页面或模块清单、核心业务流程、关键数据与字段、权限与可见范围、边界场景与异常、非功能要求、待确认项。
- 自动生成需求文档时，优先写清业务事实、流程与边界，不提前写入技术实现方案。

### devlog 要求
- 文件目录：`doc/devlog/`
- 文件格式：Markdown
- 文件命名建议：`YYYY-MM-DD-HH-mm-ss-中文任务简述.md`
- 文件名中的任务简述必须使用中文，不使用英文 slug、拼音缩写或中英混杂描述
- 内容保持简短，至少包含：
  - 本次目标
  - 核心改动
  - 修改文件
  - 校验情况
  - 风险或遗留项
---

## 九、交付收口规范
- 当用户请求“开始开发”而非“只出方案”时，AI Agent 在实现完成后应默认继续做交付收口。
- 交付收口至少应按实际情况依次检查：
  1. 编译 / 测试
  2. 构建
  3. 发布构建与启动验证
  4. 应用级 smoke test
  5. 文档、`TASK.md`、`doc/devlog/`、忽略文件更新
- 若某项验证未执行，最终输出必须明确说明未执行项、原因、影响范围和建议下一步。

## 交付摘要

### 变更内容
- ...

### 验证状态
- [ ] 代码检查
- [ ] 单元测试
- [ ] 集成测试
- [ ] Docker 构建
- [ ] API 文档验证

### 风险与待处理
- ...

### 阻塞项
- ...
```

---

## 十、禁止事项

1. **禁止**在 API 路由函数中编写业务逻辑，必须通过服务层处理
2. **禁止**在服务层直接返回 ORM 模型，必须转换为 Pydantic Schema
3. **禁止**在异步代码中使用同步阻塞调用（如 `requests`、同步文件 I/O）
4. **禁止**跳过 Alembic 直接手动修改数据库结构（使用数据库时）
5. **禁止**直接使用 `redis.Redis` 同步客户端，必须使用 `redis.asyncio`
6. **禁止**在代码中硬编码配置值，必须通过 `pydantic-settings` 管理
7. **禁止**返回非 `ResultResponse` 格式的响应
8. **禁止**使用未定义的自定义异常
9. **禁止**提交 `print()` 调试语句（使用 `loguru` 的 `logger`）
10. **禁止**忽略类型检查警告（确保 `mypy` 或 `pyright` 通过）
11. **禁止**在测试中调用真实第三方服务
12. **禁止**跳过 Git 提交直接进行下一步操作
13. **禁止**在 Docker 镜像中使用 root 用户运行应用
14. **禁止**凭猜测修改代码排查问题，必须通过日志、断点、数据等实际证据定位原因
