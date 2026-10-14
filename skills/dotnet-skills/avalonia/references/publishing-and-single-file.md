# 发布打包与单文件体积优化

适用：`dotnet publish` 产出桌面单文件（自包含 / 依赖框架）、产物体积异常排查与瘦身、发布配置变更后的验证。经验锚点：消费工程（Avalonia + net10.0-windows，win-x64，2026-09 实测，自包含 348MB → 161MB）。

## 体积构成先诊断，再动手

单文件自包含产物 = .NET 运行时（~120MB 量级）+ 原生库 + 托管 DLL（R2R 会显著放大）。先拆解再决定砍什么：

**方法 1：文件夹发布拆解**（最直观，看清每个文件多大）：

```bash
dotnet publish <proj> -c Release -r win-x64 -p:PublishSingleFile=false -o <目录>
# 按大小排序逐文件看，再对照 deps.json 判断每个 dll 是谁带进来的
```

**方法 2：审计 deps.json / project.assets.json**——定位「谁把不该带的东西拖进来」：

- `project.assets.json` 中搜包条目下的 `"frameworkReferences"`：包声明的框架引用（如 MSAL 的 `Microsoft.WindowsDesktop.App.WindowsForms`）会把**整个桌面运行时 facet**（WinForms 全家 ~25MB）拖进产物，即使你一行 WinForms 都没写
- `*.deps.json` 的 `targets` 里看 `runtimepack.Microsoft.WindowsDesktop.App...` 出现即中招
- 应用代码零使用的运行时 facet 是单文件产物最常见的水分

## 瘦身决策表（按收益排序）

| 手段 | 体积影响 | 代价 | 建议 |
|---|---|---|---|
| 关闭 `PublishReadyToRun`（R2R） | 常规省 40%~50% | 启动从 ~1.2s 变 ~3s（JIT） | 体积优先选关；启动敏感选开，**别两边都要又抱怨体积** |
| 剔除无用运行时文件 / 包 | 视情况几 MB~几十 MB | 需逐个核实真实使用面 | 见下方剔除机制 |
| 合并功能重叠的包 | 一个整包（~8MB 量级） | 改造一个功能点 | 先 grep 各包实际使用面，找「一个库能干两个的活」的合并机会 |
| `EnableCompressionInSingleFile=true` | ~50%（下载体积） | **每次启动全量解压**：实测启动 4.8s vs 1.2s、内存翻倍 | 下载渠道有 zip 外层压缩时对下载体积**零收益**（外层已压平，实测 47.3 vs 47.4MB）；取舍只剩磁盘体积 vs 启动解压开销，产物 ≤50MB 量级时解压开销可忽略（实测 ~0.2s） |
| `PublishTrimmed=true`（非 AOT） | **可能反而变大** | 裁剪后 R2R/单文件剥离机制失效，实测 285MB > 基线 174MB | 带反射/UI 库的桌面项目默认别碰；AOT 场景另说 |

功能库取舍原则：两个库如果各撑一个功能（如 PdfSharp 写 PDF 分割、PdfPig 读 PDF 文本），**不可互替，去哪个砍哪个功能**——这是产品决策，必须让用户拍板；只有「一个库已具备另一个库的能力」（如 OpenXml 与 NPOI.XWPF 都能写 Word）才做合并改造。

## 剔除单文件条目的正确机制（高频踩坑）

要把某个文件从**单文件 bundle** 里去掉，唯一有效写法——`Update` 语法打 `ExcludeFromSingleFile` 元数据，且挂钩在 `_ComputeFilesToBundle` **之前**：

```xml
<Target Name="ExcludeUnusedFilesFromSingleFile"
        BeforeTargets="_ComputeFilesToBundle"
        Condition="'$(Configuration)' == 'Release' and '$(PublishSingleFile)' == 'true'">
    <ItemGroup>
        <ResolvedFileToPublish Update="@(ResolvedFileToPublish)"
                               Condition="'%(ResolvedFileToPublish.Filename)' == 'System.Windows.Forms'">
            <ExcludeFromSingleFile>true</ExcludeFromSingleFile>
        </ResolvedFileToPublish>
        <!-- 标记后再 Remove，否则被排除的文件会散落在 exe 旁 -->
        <ResolvedFileToPublish Remove="@(ResolvedFileToPublish)"
                               Condition="'%(ResolvedFileToPublish.ExcludeFromSingleFile)' == 'true'" />
    </ItemGroup>
</Target>
```

**全部无效的写法**（都试过，产物字节不变）：
- `ResolvedFileToPublish Remove="..."` 直接移除——SDK 内部 `_ComputeFilesToBundle` 重新收集，对 bundle 不生效
- `AfterTargets="ComputeResolvedFileToPublishList"` 挂钩时机——后续流程会重建清单
- 裸 item 元数据语法 `<ResolvedFileToPublish Include="xxx"><ExcludeFromSingleFile>...`——被当成空 Include 的新条目，真实文件根本没被打上标记
- 通过 props 文件 `CustomAfterMicrosoftCommonProps` 注入——target 不执行

注意：单文件 bundle 是有输入缓存的（`_GenerateSingleFileBundleInputCache`），改 target 后重测体积前**先删 `obj`/`bin` 强制全量发布**，否则拿旧产物字节对比会误判无效。

**文件夹发布与单文件 bundle 需要两个挂钩**（缺一会漏剔）：`_ComputeFilesToBundle` 在非单文件发布时**不存在**，只挂它会让文件夹发布的 pdb 全数留下（libSkiaSharp.pdb 84MB 实锤）。文件夹剔除用 `AfterTargets="ComputeResolvedFileToPublishList"` + `ResolvedFileToPublish Remove`；单文件剔除用上面 `_ComputeFilesToBundle` 前的 Update 标记写法。单文件 bundle 本身不含这两类 pdb（两种挂钩组合下 bundle 字节一致），文件夹发布才是滋生地。

**顺带**：`UsePlatformDetect()` 定义在 Avalonia.Desktop 包；win-x64-only 产品可直接引用 `Avalonia.Win32` + `Avalonia.Skia` + `Avalonia.HarfBuzz` 并改为 `UseWin32()+UseSkia()+UseHarfBuzz()`（省 ~4-5MB Linux/macOS 后端）。**`UseHarfBuzz()` 不能漏**：缺失时文本整形抛 `InvalidOperationException: No text shaping system configured`（编译通过、启动/测试期崩）。测试工程经 ProjectReference 传递拿平台包，其中的平台初始化也要同步。

## 包依赖侧的瘦身

- 停用功能时同步注释 `PackageReference` 并留恢复注释（改哪里、还原哪里的调用），别只删代码
- 包是否真被使用：grep 全项目命名空间引用，**一个使用点都没有才删**；只有一个使用点时评估「用自带能力的库替代」
- NuGet 包的 nuspec/assets 可能声明 `frameworkReferences` 传递运行时依赖——瘦身前先查 `project.assets.json`

## framework-dependent 单文件的原生库捆绑（2026-09-15 实测）

FDD（`--self-contained false`）单文件发布**默认不打包原生库**：libSkiaSharp、av_libglesv2（ANGLE）、e_sqlite3、libHarfBuzzSharp 全部散落 exe 旁——用户看到的就是「exe + 一堆 dll」而非单文件。单文件 bundle 的清单校验不会报错，exe 也能跑，极易漏检。

- `IncludeNativeLibrariesForSelfExtract` 对自包含 / framework-dependent **都合法且都要开**，禁止加 `Condition="'$(SelfContained)'=='true'"` 之类的条件——消费工程曾因此让 framework 版持续散装
- NETSDK1176 只约束「压缩」：`EnableCompressionInSingleFile` 确实仅自包含合法，FDD 下须关；它与原生库捆绑无关，**不要把两者写进同一个条件**
- 原生库进 bundle 后运行时解压到 `%TEMP%\.net\` 加载：34.9MB FDD 单文件实测冷启动 2.44s，与自包含版持平，无额外代价
- 验收标准：framework 发布目录里**只允许存在 exe**（ pdb 由脚本剔除），任何散落 dll 都是捆绑没生效

## 发布验证清单（改发布配置后必做）

1. `dotnet build` 0 警 0 错 + 全量测试
2. **删 obj/bin 全量重发**，记录两种产物体积：自包含（`-r win-x64`）与依赖框架（`--self-contained false -p:PublishSingleFile=true`）
3. **检查两种形态的发布目录内容**：自包含与 framework 都应只含单个 exe——目录里出现 dll 即单文件捆绑失效
4. 启动冒烟：拉起 exe，等主窗口句柄出现（JIT 版与 R2R 版启动差异大，实测记录）
5. 被剔除文件对应的功能**真实走一遍**（如剔除 Oracle 驱动后连一次 Oracle；换 Word 引擎后真导出一次）——deps.json 仍列着被剔除的文件，只有触达时才报错，必须冒烟确认剔除不破坏主路径
6. README / 架构文档里的体积与参数口径同步

## Windows 环境注意

- 产物体积对比用 `stat -c %s` 或 python `os.path.getsize`，肉眼对比 MiB 容易看错数量级
- 目标机器无 .NET 时只能交付自包含版；依赖框架版要求装机有对应桌面运行时，两个脚本分开、输出目录分开（`dist` / `dist-fdd`），别共用目录互相覆盖
