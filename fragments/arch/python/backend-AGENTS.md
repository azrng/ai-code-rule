---
rule_id: backend-agents
version: 1.45.0
last_updated: 2026-09-03
dependencies: [agents-root]
---

# 后端规则

<!-- include: shared/backend/scope.md -->

<!-- include: shared/backend/common-tasks.md -->

---

<!-- include: shared/application/tech-choice.md -->

---

## 主动建议规则
- 发现业务逻辑放错层、路由与服务职责混杂、权限校验缺失、事务边界不清或异常处理不一致时，应主动提醒
- 发现接口契约、DTO、存储结构、缓存策略或状态流转可能影响历史数据、兼容性或权限安全时，必须先说明风险，不得直接扩大修改
- 发现可以复用既有服务、数据访问封装、DTO、校验器或错误处理封装时，应优先建议复用
- 不确定业务规则、权限规则、数据含义或外部接口行为时，应按根 `AGENTS.md` 的查证优先级处理，禁止按通用经验补写规则

---

## 技术栈

### 核心框架

- **Python 3.12+**，使用类型注解（type hints）
- **FastAPI** 作为 Web 框架，基于 ASGI（推荐 Uvicorn 作为服务器）
- **Pydantic V2** 用于数据验证和序列化
- **SQLAlchemy 2.0+**（异步模式）作为 ORM，配合 Alembic 进行数据库迁移（**可选**，无数据库需求的项目不使用）

### 常用库

| 用途 | 库 | 说明 |
|------|----|------|
| Web 框架 | `fastapi` | 异步高性能 API 框架 |
| ASGI 服务器 | `uvicorn[standard]` | 生产级 ASGI 服务器 |
| ORM | `sqlalchemy[asyncio]`（可选） | 异步 ORM |
| 数据库迁移 | `alembic`（可选） | 数据库版本管理 |
| 数据验证 | `pydantic` | 请求/响应模型验证 |
| Redis | `redis[hiredis]`（可选） | 缓存与消息队列 |
| HTTP 客户端 | `httpx` | 异步 HTTP 客户端 |
| 任务队列 | `celery[redis]`（可选） | 异步任务处理 |
| 日志 | `loguru` | 结构化日志 |
| 配置管理 | `pydantic-settings` | 环境变量与配置管理 |
| 测试 | `pytest` + `pytest-asyncio` + `httpx` | 异步测试支持 |
| 代码质量 | `ruff` | Linter + Formatter（替代 flake8、isort、black） |

### 包管理

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

### 数据库支持（可选）

当项目需要数据库时，默认 PostgreSQL（推荐 `asyncpg` 驱动），可选：

- MySQL / MariaDB（`aiomysql`）
- SQLite（`aiosqlite`，仅开发/测试）
- SQL Server（`aioodbc`）

asyncpg 时间语义约定：

- `timestamptz` 列返回带时区的 aware datetime，`timestamp` 列返回 naive datetime；同一项目内统一一种语义，禁止 naive / aware 混用后直接比较或写回数据库
- 时间字段统一一种约定（如全部 aware UTC 或全部 naive 本地时间），跨语义转换只在服务层显式完成，不在 Pydantic 序列化时隐式处理

### 依赖注入

使用 FastAPI 原生 `Depends` 机制进行依赖注入，结合 `Annotated` 类型注解实现声明式注入。

---

## 项目结构

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
│   └── design/                 # 设计文档
│       └── 设计文档.md
├── .env.example                # 环境变量示例
├── pyproject.toml              # 项目配置（PEP 621）
├── uv.lock                    # uv 锁文件（自动生成）
├── Dockerfile                  # Docker 构建
├── docker-compose.yml          # 容器编排
├── AGENTS.md                   # AI 代理规则
├── CLAUDE.md                   # Claude 指针
├── GEMINI.md                   # Gemini 指针
├── GROK.md                     # Grok 指针
└── TASK.md                     # 当前任务记录
```

---

## 阶段 1 — 后端实现

**触发条件**：用户发出「开始开发」或「开始后端开发」指令

**入场要求**：阶段 0 设计文档或接口契约已明确；若为例外情况任务，可直接进入实现

**工作内容**：
1. 先根据需求定位涉及的层级（路由、服务、数据访问、Schema），再在正确层内实现对应职责。
2. 保持依赖方向正确：路由只编排服务，服务不感知 HTTP 细节，数据访问不包含业务规则。
3. 所有增删改必须真实生效，并能通过 Swagger 或测试独立验证。
4. 涉及数据库结构变更时，同步补齐 Alembic 迁移脚本和初始化说明。

**门控规则**：
- 关键接口路径已可通过 Swagger 独立验证。
- 核心业务改动已补测试或在交付说明中解释未补原因。
- 分层职责与依赖方向未被破坏。
- 后续实现发现契约缺口时，按 `collaboration-AGENTS.md`「纠错、回退与重构」回到 Schema / 接口定义变更流程，不在实现层绕过契约。

---

## 后端开发规则

### API 路由层（Controller）

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

### 服务层（Service）

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

### 数据模型（Model）

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

### 数据访问层（Repository，可选）

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

### 统一响应格式

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

### 异常处理

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

### 缓存

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

### 数据库迁移（可选）

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

### API 文档

FastAPI 自带 Swagger UI 和 ReDoc，确保：
- 所有路由添加 `summary` 和 `description`
- 使用 `response_model` 明确响应类型
- Pydantic 模型添加 `description` 字段说明
- 按 `tags` 分组管理接口

### 命名规范

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

### 代码组织规范
- 一个文件只放一个主对象：路由处理、Service、Repository、ORM Model、Pydantic Schema、配置类各自独立文件，文件名与主对象一致。
- 数据契约（请求 / 响应 / DTO / Schema）按业务域分文件定义（`schemas/requests/`、`schemas/responses/`），不合并到一个 `models.py` / `schemas.py`。
- 触发拆分的信号：单文件出现多个主类、函数或字段持续堆叠、模块跨多个不相关业务。
- 允许例外：紧耦合的常量、Protocol 接口分组、测试 fixture。
- 反模式：一个 `models.py` 堆放所有 ORM 模型；一个 `schemas.py` 堆放所有请求 / 响应；把多个不相关 Service 塞进同一文件。
- 如果编写注释（含 docstring、行内注释等），统一使用中文，不使用纯英文注释；必要的英文标识、协议字段名或框架关键字可保留原文；不在注释里泄露密钥、token、连接串或真实生产地址。

---

## 测试规则

<!-- include: shared/backend/pre-commit-regression.md -->

### 测试框架

- **pytest** + **pytest-asyncio** 作为测试框架
- **httpx** 用于 API 集成测试（`AsyncClient`）
- 测试中使用 `aiosqlite` 内存数据库，不依赖真实数据库

### 测试分类

| 类型 | 说明 | 示例 |
|------|------|------|
| 单元测试 | Mock 外部依赖，测试单一函数/类 | Service 层逻辑测试 |
| 集成测试 | 测试多组件协作，使用内存数据库 | Repository 数据访问测试 |
| API 测试 | 通过 TestClient 测试完整请求链路 | 端到端接口测试 |

### 测试规范

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

### 验证命令

Python 验证优先使用 uv 的临时依赖执行方式，不要求预装开发依赖：

- 测试：`uv run --with pytest pytest ...`
- 代码质量检查：`uv run --with ruff ruff check ...`

<!-- include: shared/no-test-fallback.md -->

---
