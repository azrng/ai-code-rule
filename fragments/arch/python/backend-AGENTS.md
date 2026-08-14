---
rule_id: backend-agents
version: 1.12.0
last_updated: 2026-08-14
dependencies: [agents-root]
---

# 后端规则

## 适用范围

- 作用域：服务端实现、接口、服务、数据访问、缓存、异常处理与后端测试
- 触发场景：涉及接口、Service、Repository、缓存、权限、异常处理或测试时阅读

### 常见任务入口
- 改接口响应或状态码：先看接口 / 路由规则与异常处理规范
- 改业务流程或状态流转：先看 Service / 应用层规则与权限约束
- 改查询、分页、缓存、迁移：先看数据访问规则与验证要求
- 补服务端回归：先看 `提交前最小回归` 与测试规则

---

## 技术选择原则
如果仓库已经有真实实现，以现有代码为准，不要强行重构或替换技术栈。
技术债务与重构判断遵循根 `AGENTS.md` 的全局规则。

---

## 主动建议规则
- 发现业务逻辑放错层、路由与服务职责混杂、权限校验缺失、事务边界不清或异常处理不一致时，应主动提醒
- 发现接口契约、DTO、存储结构、缓存策略或状态流转可能影响历史数据、兼容性或权限安全时，必须先说明风险，不得直接扩大修改
- 发现可以复用既有服务、数据访问封装、DTO、校验器或错误处理封装时，应优先建议复用
- 不确定业务规则、权限规则、数据含义或外部接口行为时，应按根 `AGENTS.md` 的查证优先级处理，禁止按通用经验补写规则

---

## 技术栈

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
└── TASK.md                     # 当前任务记录
```

---

## 后端开发规则

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

### 代码组织规范
- 一个文件只放一个主对象：路由处理、Service、Repository、ORM Model、Pydantic Schema、配置类各自独立文件，文件名与主对象一致。
- 数据契约（请求 / 响应 / DTO / Schema）按业务域分文件定义（`schemas/requests/`、`schemas/responses/`），不合并到一个 `models.py` / `schemas.py`。
- 触发拆分的信号：单文件出现多个主类、函数或字段持续堆叠、模块跨多个不相关业务。
- 允许例外：紧耦合的常量、Protocol 接口分组、测试 fixture。
- 反模式：一个 `models.py` 堆放所有 ORM 模型；一个 `schemas.py` 堆放所有请求 / 响应；把多个不相关 Service 塞进同一文件。

---

## 测试规则

### 提交前最小回归
- 默认执行：项目现有的静态检查、类型检查或编译校验
- 业务逻辑改动：至少补一项业务规则、权限判断或状态流转验证
- 接口 / 路由 / 控制器改动：至少补一项输入输出、状态码或异常分支验证
- 数据访问、缓存、迁移改动：至少补一项查询结果、缓存行为、事务或迁移同步验证
- 若仓库暂时缺少自动化测试基础，可使用接口调用、脚本或命令行验证替代，但必须能证明核心逻辑真实生效

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
