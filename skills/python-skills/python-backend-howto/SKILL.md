---
name: python-backend-howto
description: "Python 后端（FastAPI + SQLAlchemy + uv）操作手册：uv 包管理命令、分层代码模板（路由 / Protocol 服务 / ORM / Pydantic Schema / Repository / ResultResponse / 异常 handler / RedisProvider）、Alembic 迁移命令、pytest fixture 示例。Keywords: FastAPI, uv, SQLAlchemy, Pydantic, Alembic, pytest, ResultResponse, Protocol."
---

# Python 后端操作手册

分层约束与选型基线（职责边界、依赖方向、命名、异常体系表）由项目 `backend-AGENTS.md` 约束；本技能提供具体代码模板与命令序列，按需取用。

## uv 包管理

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

验证优先用 uv 临时依赖执行：`uv run --with pytest pytest ...`、`uv run --with ruff ruff check ...`。

## API 路由层（Controller）

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

## 服务层接口（Protocol）

```python
from typing import Protocol
from app.schemas.requests import CreateUserRequest, UpdateUserRequest
from app.schemas.responses import UserResponse

class IUserService(Protocol):
    async def create(self, request: CreateUserRequest) -> UserResponse: ...
    async def get_by_id(self, user_id: int) -> UserResponse: ...
    async def update(self, user_id: int, request: UpdateUserRequest) -> UserResponse: ...
    async def delete(self, user_id: int) -> None: ...
```

## ORM 模型（SQLAlchemy）

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

## Pydantic Schema（DTO）

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

## 通用 Repository 基类

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

## 统一响应格式 ResultResponse

```python
from typing import TypeVar, Generic
from pydantic import BaseModel

T = TypeVar("T")

class ResultResponse(BaseModel, Generic[T]):
    code: int = 200
    message: str = "success"
    data: T | None = None

    @classmethod
    def success(cls, data: T | None = None, message: str = "success") -> "ResultResponse[T]":
        return cls(code=200, message=message, data=data)

    @classmethod
    def failure(cls, message: str, code: int = 400) -> "ResultResponse[T]":
        return cls(code=code, message=message, data=None)
```

## 全局异常 handler

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

## RedisProvider 封装

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

## Alembic 迁移命令

```bash
# 生成迁移脚本
alembic revision --autogenerate -m "描述"

# 执行迁移
alembic upgrade head

# 回滚迁移
alembic downgrade -1
```

## pytest fixture 与 API 测试示例

```python
import pytest
import pytest_asyncio
from httpx import AsyncClient, ASGITransport
from app.main import app

# async fixture 必须用 pytest_asyncio.fixture；同步测试不能消费 async fixture
@pytest_asyncio.fixture
async def client():
    transport = ASGITransport(app=app)
    async with AsyncClient(transport=transport, base_url="http://test") as ac:
        yield ac

# 项目未配置 asyncio_mode = "auto" 时，async 测试必须显式加标记
@pytest.mark.asyncio
async def test_create_user_success(client):
    response = await client.post("/api/v1/users", json={"username": "test", "email": "test@example.com"})
    assert response.status_code == 200
    data = response.json()
    assert data["code"] == 200
    assert data["data"]["username"] == "test"
```
