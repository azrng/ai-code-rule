## [2026-06-28]：onion-architecture 去重精简

跟踪状态表：

| 文件夹名称 | 当前子任务状态 | 备注 |
|-----------|----------------|------|
| `skills/onion-architecture` | `已完成` | 消除 SKILL.md 与 references 之间的自我重复，保留全部 API 内容与两文件结构，仅做去重+加交叉指针 |

### `skills/onion-architecture`

#### 修改原因
审查发现 onion-architecture skill 存在严重自我重复：SKILL.md(432行)+references(478行) 合计 910 行，同一套规则在 3-7 处反复出现——「全局返回包装」重复 6-7 次、「分页四件套」4 次、「控制器瘦」4 次、「DomainService 门槛」4 次、「Adapter」3 次。AI 需阅读大量重复内容才能定位决策信息。

前置核对：前一轮已用 nuget 源码（`D:\Gitee\nuget-packages`）逐符号核实，skill 描述的所有 API（`AddMvcResultPackFilter`/`AddMvcModelVerifyFilter`/`UseGlobalException`/`CustomResultPackFilter`/`ModelVerifyFilter`/`CustomExceptionMiddleware`/`NotFoundException.ThrowIfNull`/`CommitTransactionAsync`/`WhereIfNotNullOrWhiteSpace`/`ConfigureDefaultJson`/`IJsonSerializer`/`RegisterBusinessServices` 等）在源码中真实存在，**本次只做去重，不删任何 API 内容、不改任何 API 声明**。两文件 front matter 均无 version/last_updated 元数据，无需同步元数据。

#### 主要修改内容

**A. SKILL.md（432 → 319 行，-26%）**
1. **「全局返回包装」节与「AppService 写法」节合并**：三件套表保留 1 处，删去与之信息重叠的「业务异常→结果映射」表的重复形态，AppService/控制器代码合并精简。
2. **「领域服务 DomainService」完整代码块删除**（原 33 行，三方结对的接口/实现/AppService 调用代码）——与 references Domain 层逐字重复，改留判断门槛 + 指针 `见 references/layer-responsibilities.md → Domain 层`。
3. **「外部适配器 Adapter」完整代码块删除**（原 31 行，接口/两层实现/AppService 调用代码）——与 references Adapter 层逐字重复，改留关键点 + 指针 `见 references → Adapter 层`。
4. **「JSON 序列化」节压缩**：30 行 → 9 行，删示例代码，保留接口/实现/注册要点。
5. **「新增完整 CRUD 流程」精简**：每步一行简版（详版仍在 references 第4节，指针指向）。
6. **「依赖注入与中间件装配」删除重复 Program.cs 代码块**：与「全局包装」节的 Program.cs 重复，改留装配要点 + 自动注册代码片段 + 指向「全局包装 → Program.cs 装配」的指针。
7. **「常见反模式」表压缩**：17 行 → 6 行「最易踩 4 项」，删否定句重复项。
8. **新增迁移期偏离提示**：在反模式表后补一条「遇到存量项目未遵循约定（如用 Newtonsoft、自研 ResultFilter、`QueryableWhereIf`），按现有代码风格走、不强行改写可工作代码；新增功能才按本指南」——避免 AI 在迁移期项目强行"纠正"可工作代码。

**B. references/layer-responsibilities.md（478 → 451 行，-5%）**
1. **Application 层「返回值与错误处理」映射表删除**（原 8 行业务异常映射表）：与 SKILL 三件套表三处重复，改留一行指针指向 `SKILL.md → 全局返回包装`。
2. **Application 层迁移期说明删除**（原 `IResultModel<T>` 迁移期段）：与 SKILL 全局包装节重复，指针已覆盖。
3. **Service 层 Program.cs 装配代码块删除**（原 15 行）：与 SKILL Program.cs 重复，改留指针指向 `SKILL.md → Program.cs 装配`。
4. **跨层数据流（GET 请求旅程，27行 ASCII 图）整段保留**：可视化价值，不删。

#### 精简原则（执行时严格遵守，均已落实）
1. 不删任何 API 符号/命名空间/包名声明，只删重复的解说和代码块。
2. 每条规则全文只留 1 处：决策/门槛类留 SKILL，完整代码类留 references，交叉用指针引用。
3. 删代码必加指针，不制造断链。

#### 修改的文件
- `skills/onion-architecture/SKILL.md`
- `skills/onion-architecture/references/layer-responsibilities.md`

#### 验证结果
1. **关键 API 符号无丢失**：Grep 复查 21 个关键符号（`AddMvcResultPackFilter`/`AddMvcModelVerifyFilter`/`UseGlobalException`/`NotFoundException.ThrowIfNull`/`CommitTransactionAsync`/`WhereIfNotNullOrWhiteSpace`/`ToPageListAsync`/`GetQueryPageResult`/`IJsonSerializer`/`ConfigureDefaultJson`/`RegisterBusinessServices`/`IScopedDependency`/`IHttpHelper`/`Common.HttpClients`/`EntitiesNoTacking`/`ApplyConfigurationsFromAssembly`/`ParameterException`/`LogicBusinessException`/`ForbiddenException`/`InternalServerException` 等），两文件共 107 处匹配，全部保留。
2. **指针无断链**：SKILL 中 4 处指向 references 的指针（Domain 层 / Adapter 层 / 完整功能落点速查 / 各层职责），目标段在 references 中均真实存在；references 反向引用 SKILL「全局返回包装」「Program.cs 装配」节也存在。

#### 当前子任务状态
- `已完成`

#### 未处理项（已评估，本次不在范围内）
- 同步仓库版到 `C:\Users\Azrng\.agents\skills\` 安装目录——属于另一项任务，需用户单独确认（涉及覆盖用户本地文件）。
