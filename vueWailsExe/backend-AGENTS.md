# AGENTS.md

## 适用范围

- 作用域：Go/Wails 后端、绑定暴露、分层实现与后端测试规则
- 触发场景：涉及 Go Handler、Service、Repository、Model、Wails 绑定与数据库时阅读

---

## 技术栈

### 后端
- Go + Wails v3
- 标准分层架构：`Handler → Service → Repository → Model`
- ORM：GORM
- 数据库：SQLite（本地默认）

### 前后端通信
- **Wails 原生绑定**：Go 方法通过 `wails:generate` 暴露给前端 JS/TS 调用
- 禁止使用 HTTP 请求与 Go 后端通信（Wails 不是 B/S 架构）
- 如需调用外部 API，使用 Axios 通过前端发起

---

### 阶段 2 — Go 后端实现（Codex 主导）

**触发条件**：用户发出「开始后端开发」指令

**入场要求**：前端页面已完成，接口需求已明确

**工作内容**：
1. 按照接口需求实现 Go Handler/Service/Repository
2. Handler 方法需要标记 `//export` 注释以暴露给前端
3. 运行 `wails generate module` 重新生成前端绑定
4. 涉及数据库结构变更时，使用 GORM AutoMigrate

**Go 方法导出规范**：
- 需要暴露给前端的方法必须在 App 结构体上定义
- 方法首字母大写（Go 导出规则）
- 方法参数和返回值必须是可序列化类型（string、int、struct、slice 等）
- 返回 error 时，前端会收到 JS Error 对象

**门控规则**：
- 核心方法独立测试通过后，才允许进入阶段 3

---

## Go 后端规则

### 分层架构
```
Handler → Service → Repository → Model
```
- `Handler`：参数校验、调用 Service、返回结果
- `Service`：业务逻辑处理
- `Repository`：数据访问
- `Model`：GORM 实体与 DTO

### Handler 层规则
- Handler 方法定义在 App 结构体上（Wails 绑定要求）
- 只做参数校验和转发，不写业务逻辑
- 方法命名使用 PascalCase
- 参数和返回值必须是可序列化类型
- 错误通过返回 error 传递

### Service 层规则
- 服务方法接收接口（非 struct），方便测试
- 服务方法必须是幂等的，相同输入得到相同输出
- 业务错误返回自定义错误类型
- 禁止在 Service 中直接操作数据库

### Repository 层规则
- 使用 GORM 进行数据访问
- 数据库连接通过依赖注入传入
- 涉及事务时使用 `db.Transaction()`
- 复杂查询使用 GORM Scope 封装

### Model 层规则
- GORM 实体使用 struct tag 定义字段映射
- 表名使用 snake_case
- 软删除使用 `gorm.DeletedAt`
- 公共字段（ID、创建时间、更新时间）提取为 BaseModel

### 数据库规则
- 使用 GORM AutoMigrate 管理表结构
- 数据库文件默认存储在应用数据目录
- 禁止硬编码数据库连接字符串
- 所有查询必须使用参数化查询（GORM 自动处理）

### Wails 绑定规则
- 暴露给前端的方法必须有 Go doc 注释，注释内容统一使用中文
- 方法签名变更后必须运行 `wails generate module`
- 前端 `src/bindings/` 目录下的文件由 Wails 自动生成，禁止手动修改
- 方法参数不支持 chan、func、unsafe.Pointer 等类型

---

## 测试规则

### 总体要求
- 测试暂时不作为强制要求
- 若补充测试，应覆盖真实业务行为

### Go 后端测试（可选）
- 使用标准 `testing` 包
- Service 层测试 mock Repository
- Repository 层测试使用 SQLite 内存数据库

### 无法执行测试时
- 必须说明未执行的测试类型
- 必须说明未执行原因
- 必须说明潜在影响范围和风险
