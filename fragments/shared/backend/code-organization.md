### 代码组织规范
- 一个文件只放一个主对象：handler、service、repository、struct（数据模型 / DTO）、配置各自独立文件，文件名与主对象一致。
- 数据契约（请求 / 响应 / DTO struct）按业务域分文件定义，不合并到一个 `models.go` / 大杂烩文件。
- 触发拆分的信号：单文件出现多个主 struct、函数或字段持续堆叠、模块跨多个不相关业务。
- 允许例外：紧耦合的常量、接口分组、测试 fixture。
- 反模式：一个 `models.go` 堆放所有 struct；把多个不相关 service 塞进同一文件。
