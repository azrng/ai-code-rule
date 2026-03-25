# AGENTS-new.md

## 目标
职责划分如下：
- `AGENTS.md` / `CLAUDE.md` / `GEMINI.md`：各 AI Agent 统一工作规则、任务管理机制、交付要求，内容保持同步
- `design-system.yaml`：设计 token 与 UI 规范
- `TASK.md`：具体任务内容与任务进度

---

## 注意事项
1. 你需要理解用户提出的需求的业务逻辑然后再做设计和开发
2. 你需要根据设计的功能需求设计多级的菜单，管理员默认有所有菜单权限
3. 功能模块必须有完整可操作的功能页面
4. 所有的功能至少要确保能够完成基本的流程操作，功能的按钮必须都是可以交互的，界面的内容尽量多填充一些信息
5. 整体系统需要有一个登录页面，并要有默认应用图标，登录页面，登录系统左上角应该有应用图标和应用名称。
6. 系统涉及时能够有一些酷炫的动效，需要符合医疗行业的审美要求，界面简洁大方，操作流程清晰易懂。
7.  如系统内所有设计到变更、新增、删除等业务逻辑，必须能够真实生效的，而不是假的交互，确保浏览器刷新页面后能看到生效后的内容
8.  由于此系统的功能模块较多，实现的时候先把系统的框架搭起来，然后按照一个一个子模块的方式来实现，而不是一下子实现全部功能

---

## 技术栈

### 前端
- Vue 3 + TypeScript + Vite
- Element Plus 组件库
- Element Plus Icons（唯一图标库，尺寸统一 18-24px）
- Vue Router 4（路由管理）
- Pinia（全局状态管理：用户信息、权限、通知等跨组件共享状态）
- Axios（HTTP 客户端）

### 后端
- .NET 10 (2025 年已经发布正式版) + ASP.NET Core Web API
- Clean Architecture 分层架构：
  - `API`（表现层）→ `Application`（应用层）→ `Domain`（领域层）← `Infrastructure`（基础设施层）
- ORM：Entity Framework Core
- 测试框架：xUnit + Moq

### 部署
- Docker + Docker Compose
- 数据库：PostgreSQL

### 默认端口
| 服务                 | 容器内端口 | 宿主机映射端口 |
| -------------------- | ---------- | -------------- |
| 前端（Vue）          | 3000       | 3000           |
| 后端（.Net）         | 8080       | 8080           |
| 数据库（PostgreSQL） | 5432       | 5432           |

- 若宿主机端口冲突，优先调整 Compose 映射端口（宿主机侧），容器内端口保持不变
- 前端 API 请求统一指向 `http://localhost:8000`，通过环境变量 `VITE_API_BASE_URL` 配置，不允许硬编码

### 设计系统
- 使用 Element Plus 主题系统，所有视觉规范在 `design-system.yaml` 中定义
- 优先使用 Element Plus 组件和样式，禁止 inline style，禁止硬编码颜色/间距/圆角

如果仓库已经有真实实现，以现有代码为准，不要强行重构或替换技术栈。

---

## 推荐目录结构
若仓库尚未形成稳定结构，可优先参考以下组织方式；若仓库已有实现，以现状为准，不强制迁移。

```text
project-root/
├── frontend/
│   ├── public/
│   ├── src/
│   │   ├── api/               # 按业务域拆分的请求模块
│   │   ├── components/
│   │   │   ├── common/        # 通用业务组件
│   │   │   └── layout/        # 布局组件
│   │   ├── views/             # 页面级组件（按业务域拆分）
│   │   ├── router/
│   │   │   ├── index.ts       # 路由配置
│   │   │   └── routes.ts      # 路由定义
│   │   ├── stores/            # Pinia stores（按业务域拆分）
│   │   ├── types/             # TypeScript 类型定义
│   │   ├── utils/             # 纯工具函数
│   │   ├── directives/        # 自定义指令
│   │   ├── hooks/             # 组合式函数
│   │   ├── config/
│   │   │   └── element-theme.ts  # Element Plus 主题配置
│   │   ├── App.vue
│   │   └── main.ts
│   ├── Dockerfile
│   ├── package.json
│   ├── vite.config.ts
│   └── tsconfig.json
├── backend/                  # Clean Architecture 解决方案
│   ├── src/
│   │   ├── YourProject.API/                 # 表现层
│   │   │   ├── Controllers/                 # API 控制器
│   │   │   ├── Middleware/                  # 自定义中间件
│   │   │   ├── Filters/                     # 动作过滤器
│   │   │   ├── Program.cs                   # 应用入口
│   │   │   ├── appsettings.json             # 配置文件
│   │   │   └── YourProject.API.csproj
│   │   │
│   │   ├── YourProject.Application/         # 应用层
│   │   │   ├── Services/                    # 应用服务接口与实现
│   │   │   ├── DTOs/                        # 数据传输对象
│   │   │   ├── Interfaces/                  # 应用服务接口
│   │   │   ├── Mappings/                    # AutoMapper 配置
│   │   │   └── YourProject.Application.csproj
│   │   │
│   │   ├── YourProject.Domain/              # 领域层（核心业务模型）
│   │   │   ├── Entities/                    # 领域实体
│   │   │   ├── ValueObjects/                # 值对象
│   │   │   ├── Enums/                       # 枚举
│   │   │   ├── Events/                      # 领域事件
│   │   │   ├── Exceptions/                  # 领域异常
│   │   │   └── YourProject.Domain.csproj
│   │   │
│   │   └── YourProject.Infrastructure/      # 基础设施层
│   │       ├── Data/                        # 数据访问
│   │       │   ├── Context/                 # DbContext
│   │       │   ├── Configurations/          # EF Core 配置
│   │       │   ├── Migrations/              # EF Core 迁移文件
│   │       │   └── Repositories/            # 仓储实现
│   │       ├── Services/                    # 外部服务实现
│   │       └── YourProject.Infrastructure.csproj
│   ├── tests/
│   │   ├── YourProject.UnitTests/           # 单元测试
│   │   └── YourProject.IntegrationTests/    # 集成测试
│   ├── Dockerfile
│   └── backend.sln
├── doc/
│   ├── devlog/
│   ├── design/
│   └── requirement.md
├── docker-compose.yml
├── .env.example
├── .gitignore
├── AGENTS.md
├── CLAUDE.md
├── GEMINI.md
├── design-system.yaml
└── TASK.md
```

---

## 开发流程

### 总体阶段划分

所有新功能开发必须严格经历以下四个阶段，阶段之间有明确的门控条件，不满足条件不得进入下一阶段：

```
阶段 0          阶段 1          阶段 2          阶段 3
设计文档   →   前端实现   →   后端实现   →   前后端联调
（用户确认）   （Claude Code）  （Codex）       （协作）
```

**例外情况**（AI 自动判断，无需走阶段 0）：
- Bug 修复（功能行为不变，只修正错误）
- 已有页面的样式、文案微调
- 配置、环境变量、部署脚本调整
- 单个字段的增删（不涉及新页面或新业务流程）

---

### 阶段 0 — 设计文档（新功能强制前置）

**触发条件**：用户提出新功能需求

**执行方式**：由用户与 AI 对话协作产出，用户最终确认定稿

**产物**：`doc/design/设计文档.md`（全项目一份，新功能以新章节追加，不新建文件）

**文档必须包含以下四个部分**：

| 部分     | 内容要求                                                       |
| -------- | -------------------------------------------------------------- |
| 架构设计 | 模块划分、数据流向、涉及的技术决策说明                         |
| 功能需求 | 页面列表、功能点明细、业务规则、权限矩阵                       |
| 界面原型 | 每个页面的 ASCII 线框图，含关键状态（loading / empty / error） |
| 交互说明 | 操作流程、状态流转、边界场景、错误处理方式                     |

**门控规则**：
- 用户明确确认设计文档后，才允许进入阶段 1
- AI 在此阶段只输出文档内容，不写任何实现代码

---

### 阶段 1 — 前端实现（Claude Code 主导）

**触发条件**：用户发出「开始前端开发」指令

**入场要求**：阶段 0 设计文档已由用户确认

**工作内容**：
1. 按设计文档实现页面和组件，遵循 `design-system.yaml` 和 Element Plus 规范
2. 数据层使用 mock（静态 mock 数据），不依赖真实后端接口
3. 同步输出接口说明文档，供后端开发参考

**产物**：
- 可在浏览器中运行的前端页面
- 接口需求说明（用于后端开发）

**门控规则**：
- 用户确认前端页面符合设计文档预期
- 前端页面交互逻辑已验证通过
- 满足以上两点后，才允许进入阶段 2

---

### 阶段 2 — 后端实现（Codex 主导）

**触发条件**：用户发出「开始后端开发」指令

**入场要求**：前端页面已完成，接口需求已明确

**工作内容**：
1. 按照接口需求实现 ASP.NET Core Web API 接口
2. 遵循 `Controller → Application → Domain → Infrastructure` 分层架构
3. 涉及数据库结构变更时，使用 EF Core Migrations 生成迁移脚本
4. 每个接口必须可通过 Swagger 或 Postman 独立验证

**字段命名约定**：
- C# 属性：PascalCase
- JSON 输出：camelCase（通过 System.Text.Json 配置自动转换）
- 数据库字段：snake_case（通过 EF Core 命名约定配置）

**门控规则**：
- 核心接口独立测试（Swagger / Postman）通过后，才允许进入阶段 3

---

### 阶段 3 — 前后端联调

**触发条件**：用户发出「开始联调」指令

**入场要求**：前端页面和后端接口均已完成独立验证

**工作内容**：
1. 前端替换 mock 数据，改为调用真实后端接口
2. 处理跨域配置、鉴权 token 传递、HTTP 错误码映射
3. 对齐异常处理：后端错误结构与前端 Message / Alert 展示逻辑匹配
4. 执行核心业务路径 smoke test（至少覆盖：登录 → 核心操作 → 数据回显 → 退出）

**完成标志**：核心路径 smoke test 通过，`doc/devlog/` 已补充本阶段记录

---

## 使用方式
每个 AI Agent 在开始修改前都必须：
1. 先阅读本文档，理解核心规则和流程
2. 确认当前处于哪个开发阶段，检查该阶段的入场条件是否满足
3. 再阅读 `design-system.yaml`
4. 再阅读相关模块和现有实现
5. 优先复用当前结构，再决定是否新增文件

进行前端开发时：
1. `design-system.yaml` 是颜色、间距、圆角、排版、页面状态、组件使用规则的参考依据
2. 优先使用 Element Plus 组件，样式使用 Element Plus 的 class 和 SCSS 变量
3. 图标统一使用 Element Plus Icons，尺寸 18-24px
4. 全局状态（用户、权限、通知等）使用 Pinia store
5. 组件本地状态使用 Composition API（ref、reactive、computed）

进行后端开发时：
1. 保持 Clean Architecture 分层依赖规则
2. Controller 只负责 HTTP 处理，业务逻辑放在 Application 层
3. 涉及表结构变更时使用 EF Core Migrations

进行部署改动时：
1. 优先复用现有 Dockerfile 和 Compose 链路
2. 镜像 tag 必须可追踪
3. 清理旧镜像前要保留可回滚版本

---

## 核心规则
- 先理解，再修改。
- 先复用，再新增。
- 交付必须可直接使用，不能只停留在演示层。
- 不允许新增平行配置体系。
- 不做与当前任务无关的重构。
- 所有改动都必须可说明、可验证。

---

## 前端规则

### Composition API 规范
- **必须使用** `<script setup lang="ts">` 语法糖
- 响应式数据使用 `ref`（基础类型）和 `reactive`（对象类型）
- 计算属性使用 `computed`，副作用使用 `watch` 和 `watchEffect`
- 生命周期钩子使用 `onMounted`、`onUnmounted` 等
- 可复用逻辑封装为组合式函数（`useXxx`），放在 `hooks/` 目录

### 组件通信规范
- **父传子**：使用 `defineProps` 定义，只读不可修改
- **子传父**：使用 `defineEmits` 定义事件
- **跨级传值**：优先使用 Pinia，其次使用 `provide`/`inject`
- **兄弟组件**：通过共同父组件或 Pinia 中转
- 避免直接修改 props，需要修改时使用 v-model 或 emit

### 路由管理规范
- 路由配置集中放在 `src/router/routes.ts`
- 路由守卫统一在 `src/router/index.ts` 中配置
- 路由跳转使用 `router.push` 和 `router.replace`
- 获取路由参数使用 `useRoute().params` 或 `useRoute().query`
- 路由命名使用有意义的名称（如 `UserList`、`OrderDetail`）

### Pinia 状态管理规范
- Store 按业务域拆分，放在 `src/stores/` 目录
- 使用 `defineStore` 定义 store
- State 定义数据，Getters 定义计算属性，Actions 定义修改逻辑
- 修改 state 必须通过 actions，禁止在组件中直接修改 store 状态
- 异步操作放在 actions 中，同步计算放在 getters 中

### 样式规则
- 优先使用 Element Plus 组件内置样式和 class
- 自定义样式使用 SCSS，变量来自 `design-system.yaml`
- 禁止 inline style，禁止硬编码颜色值（如 `#1A90FF`）
- 响应式设计遵循移动优先原则，必须在不同断点下测试

### 组件规则
- 优先复用 `src/components/` 下已有组件，禁止重复创建
- 只有确实有复用价值时才新增共享组件，避免为单次需求过度抽象
- 页面状态必须完整：`loading`、`empty`、`error`、`no-permission`
- 除非仓库已在使用，否则不要引入新的组件库或样式体系

### 请求规则
- 禁止在组件中直接调用后端 API，统一通过 `src/api/` 下的请求模块封装
- Axios 实例统一配置，拦截器处理 token、错误
- 业务常量不要散落在页面组件内部
- API 函数命名规范：`getXxx`、`createXxx`、`updateXxx`、`deleteXxx`

---

## 后端规则（Clean Architecture）

### 分层依赖规则
```
API → Application → Domain ← Infrastructure
                ↑_______________|
```
- `Domain` 层：无外部依赖，包含核心业务模型
- `Application` 层：依赖 Domain，定义服务接口和 DTO
- `API` 层：依赖 Application，处理 HTTP 请求
- `Infrastructure` 层：依赖 Application 和 Domain，实现具体技术细节

### Controller 层规则
- Controller 只负责 HTTP 处理，不包含业务逻辑
- 使用 `[ApiController]` 特性，自动处理 400 响应
- 使用 `[Route("api/[controller]")]` 统一路由前缀
- 注入 Application 层的服务接口，不直接依赖 Infrastructure
- 统一使用 `ActionResult<T>` 或 `IActionResult` 返回类型

### Application 层规则
- 服务类实现接口，接口定义在 `Interfaces/` 目录
- 服务方法必须是异步的（返回 `Task<T>`）
- 使用 DTO 进行数据传递，不直接暴露 Domain 实体
- 使用 AutoMapper 进行对象映射（如需要）
- 业务异常抛出继承自 `ApplicationException` 的自定义异常

### Domain 层规则
- 实体类继承自基类（包含 Id、创建时间等公共属性）
- 值对象使用 record 类型，实现值相等比较
- 领域事件继承自 `INotification`（MediatR）
- 禁止依赖外部库，保持纯净

### Infrastructure 层规则
- DbContext 实现数据上下文，配置连接和映射
- 使用 EF Core Migrations 管理数据库结构变更
- 仓储实现依赖注入，遵循仓储模式
- 外部服务（邮件、文件存储等）实现在此层

### 数据访问规则
- 使用 Entity Framework Core 进行数据访问
- DbContext 通过构造函数注入
- 使用 LINQ 查询，避免原生 SQL
- 涉及事务时使用 `IDbContextTransaction`
- 软删除通过全局查询过滤器实现

---

## 测试规则

### 总体要求
- 影响行为的改动应优先补充或更新测试
- 若本次改动未补测试，必须在最终说明中写明原因和风险
- 测试应覆盖真实业务行为

### 前端测试
- 前端暂时不考虑单元测试
- 但必须进行手动 smoke test 验证核心功能

### 后端测试
- 单元测试使用 xUnit + Moq
- Controller 测试：验证状态码、响应结构、参数校验
- Service 测试：验证业务逻辑分支，使用 mock 隔离依赖
- Repository 测试：使用 In-Memory 数据库或 Testcontainers
- 集成测试：使用 WebApplicationFactory 测试完整请求管道

### 外部依赖与数据
- 测试中不要真实调用第三方服务，统一使用 mock
- 测试数据应尽量最小化、可读、可重复执行
- 不要让测试依赖本地人工状态

### 无法执行测试时
- 必须说明未执行的测试类型
- 必须说明未执行原因
- 必须说明潜在影响范围和风险

---

## Git 与提交流程

### 分支命名
- `feat/<desc>`：新功能
- `fix/<desc>`：缺陷修复
- `refactor/<desc>`：无行为变更的重构
- `chore/<desc>`：工具、依赖、配置调整
- `docs/<desc>`：仅文档变更

### Commit 规范
- 提交信息优先采用 Conventional Commits：
  - `<type>(<scope>): <简短描述>`
- 示例：
  - `feat(auth): add refresh token endpoint`
  - `fix(frontend): correct table pagination reset`
  - `chore(dotnet): pin NuGet package version`

### 交付前自查
- 前端：类型检查（tsc）、构建（npm run build）
- 后端：编译（dotnet build）、测试（dotnet test）
- 涉及数据库结构变更时已确认 EF Core Migration
- 新增环境变量已同步更新相关文档
- 涉及 Docker 时已确认构建或部署链路

### 提交触发规则
- 每次修改完后都必须提交代码
- 若本次开发生成了本地运行产物、缓存、测试数据库、构建产物或依赖目录，应同步检查并更新 `.gitignore`

---

## Docker 规则

### 总体要求
- 优先复用现有 Docker 与 Compose 文件
- 生产镜像优先采用多阶段构建
- 不要把密钥写入镜像
- 部署链路要可追踪
- 清理旧镜像时要保留最小回滚窗口

### Docker 交付规则
- 只要本次任务新增或修改了 Dockerfile、`docker-compose.yml`、运行端口、环境变量或启动命令，完成实现后应主动执行一次镜像构建和容器化启动验证；若因环境限制无法执行，必须明确说明
- 容器化验证至少包括：
  - 镜像构建是否成功
  - Compose 服务是否成功启动
  - 关键服务是否处于可用状态
  - 前后端核心访问链路是否可用
- 若发现宿主机端口冲突，不要直接停止未知本地进程；优先调整 Compose 映射端口，并同步更新文档和前端配置
- 对于依赖数据库或其他基础服务的容器，Compose 中应尽量补充健康检查或等价的启动等待策略
- 新增镜像构建链路时，应同步补充 `.dockerignore`

---

## 任务管理机制
`TASK.md` 是唯一任务记录文件，用于记录具体任务内容，不负责承载规则解释。

### 任务状态
- `TODO`：未开始
- `DOING`：进行中
- `BLOCKED`：被阻塞
- `REVIEW`：已完成开发，等待确认
- `DONE`：已完成

### 执行规则
1. 开始工作前，先查看 `TASK.md` 是否已有对应任务
2. 如果已有任务，优先更新原任务，不重复新建
3. 如果没有任务，在 `TASK.md` 中新增最小任务记录
4. 开始执行时，将任务状态更新为 `DOING`
5. 若任务无法继续，更新为 `BLOCKED` 并写明原因
6. 开发完成但仍需确认时，更新为 `REVIEW`
7. 验证完成并确认交付后，更新为 `DONE`

### 每个任务至少包含
- 任务 ID
- 任务名称
- 任务目标
- 任务状态
- 最近更新时间

### AI Agent 要求
- 开始任务前先同步任务状态
- 开发过程中及时更新 `TASK.md`
- 范围变化时同步补充
- 每次开发完成后，都需在 `doc/devlog/` 下新增一份本次开发简短说明的 Markdown 记录

---

## 设计系统文件规范
`design-system.yaml` 是唯一设计系统文件，包含：
- 品牌与产品风格意图
- 语义化颜色 token
- 排版 token
- 间距、圆角、阴影 token
- 布局与断点规则
- Element Plus 主题配置映射
- 页面状态规范
- 组件使用规则
- 明确的 `do / dont` 规则

---

## 文档更新要求
以下内容发生变化时，必须同步更新相关文档：
- 命令或脚本
- 环境变量
- 构建或部署流程
- 任务流程
- 镜像 tag 或清理策略
- 对外使用方式或接口约定

优先更新已有文档，不新增重复说明。
每次开发完成后，必须在 `doc/devlog/` 下补充一份简短开发记录，作为本次工作的可追踪说明。

### 文档目录约定
- `doc/requirement.md`：用户编写的需求文档，AI 不应将设计说明或开发过程记录写入此文件
- `doc/design/设计文档.md`：项目设计文档，全项目一份。包含架构设计、功能需求、界面原型（ASCII）、交互说明。由用户与 AI 协作产出，用户确认后作为前后端开发的唯一需求基准。AI 可在文件中追加新功能章节，不删除已有内容，不新建平行文件
- `doc/devlog/`：每次开发完成后的简短开发日志

### devlog 要求
- 文件目录：`doc/devlog/`
- 文件格式：Markdown
- 文件命名建议：`YYYY-MM-DD-任务简述.md`
- 内容保持简短，至少包含：
  - 本次目标
  - 核心改动
  - 修改文件
  - 校验情况
  - 风险或遗留项

---

## 交付收口规范
- 当用户请求"开始开发"而非"只出方案"时，AI Agent 在实现完成后应默认继续做交付收口，而不是停在代码编写阶段
- 交付收口至少应按实际情况依次检查：
  1. 编译 / 类型检查 / 测试
  2. 构建
  3. Docker 构建与启动验证
  4. 浏览器或接口级 smoke test
  5. 文档、`TASK.md`、`doc/devlog/`、忽略文件更新
- 若存在可本地验证的部署链路，AI Agent 应优先自行验证
- 若某项验证未执行，最终输出必须明确说明未执行项、原因、影响范围和建议下一步

---

## 常见禁止事项
- 禁止在组件中直接调用后端 API，统一通过请求层封装
- 禁止在 Controller 层编写业务逻辑，应放入 Application 层
- 禁止修改 EF Core Model 后不补 Migration
- 禁止在异步代码中使用阻塞式等待（.Result / .Wait()）
- 禁止绕过既有提交校验流程，如 `git commit --no-verify`
- 禁止使用 inline style
- 禁止硬编码颜色值（如 `#1A90FF`）、间距值
- 禁止在 Domain 层依赖外部库
- 禁止使用非 Element Plus Icons 以外的图标库
- 禁止使用 Pinia 以外的状态管理库（Redux、MobX 等）
- 禁止使用 `<script>`  Options API，必须使用 Composition API

---

## 交付输出要求
最终输出优先使用中文，并至少说明：
- 本次改了什么
- 核心实现方式
- 修改了哪些文件
- 已执行和未执行的校验
- 当前任务状态
- 风险、阻塞或假设
- 是否已更新 `doc/devlog/`

---

文件结束。
