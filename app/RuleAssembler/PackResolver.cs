using System.Text.Json.Serialization;
using System.IO.Compression;
using System.Text.Json;

namespace RuleAssembler;

// 内容解析结果：资源来源（--pack 指定包或 exe 内嵌）、生效版本与说明性输出（[pack] 行）
internal sealed record PackResolution(IResourceProvider Provider, string? Version, string Source, List<string> Logs);

// 内容解析器：--pack 显式包（硬失败，验证用）→ exe 内嵌（常态唯一来源，内容随发布打进 exe，
// 无独立包与包缓存）。更新检查仅交互启动时一次（3 秒超时），唯一轨道是引擎自更新：
// 共享缓存命中或下载 → engine_sha256 校验 → 原子替换 exe，成功后退出提示重跑；失败只降级不阻塞。
internal static class PackResolver
{
    private const string LatestFile = "latest.json";
    private const string EngineAsset = "use-ai-rule.exe";

    // 默认发布源：GitHub Release latest 端点；环境变量 USE_AI_RULE_PACK_URL 可整体替换（gitee 镜像等）
    private static string BaseUrl =>
        Environment.GetEnvironmentVariable("USE_AI_RULE_PACK_URL")?.TrimEnd('/')
        ?? "https://github.com/azrng/ai-code-rule/releases/latest/download";

    // 引擎共享缓存根：%LOCALAPPDATA%\use-ai-rule\（引擎 exe 缓存落在 engine\<版本>\ 下）；
    // USE_AI_RULE_CACHE 可整体替换（测试隔离），与 USE_AI_RULE_PACK_URL 同风格
    internal static string CacheRoot
    {
        get
        {
            var overrideRoot = Environment.GetEnvironmentVariable("USE_AI_RULE_CACHE")?.Trim();
            return string.IsNullOrEmpty(overrideRoot)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "use-ai-rule")
                : overrideRoot!;
        }
    }

    // latest.json 契约：version = manifest.vars.version = exe 注入版本，engine_sha256 = exe 校验值；
    // engine_version 是旧版 exe 的兼容字段，CI 仍写入但新代码不读取（反序列化忽略多余字段）
    private sealed record LatestInfo(
        [property: JsonPropertyName("version")] string Version,
        [property: JsonPropertyName("engine_sha256")] string? EngineSha256);

    // args 为原始启动参数。解析是惰性的：只决定「本次用哪个来源」，绝不产生网络副作用；
    // 更新检查由 Program 在交互启动阶段调用（TryFetchUpdateOffer / ApplyUpdates）
    public static PackResolution Resolve(string[] args)
    {
        // 1) 显式指定包：zip 或已解包目录，校验失败硬报错（显式指定不做静默兜底）
        var packArg = ReadOption(args, "--pack");
        if (packArg is not null)
        {
            var (root, logs) = PreparePack(packArg, validate: true);
            var manifest = PackReader.Load(root);
            return new PackResolution(new FolderResourceProvider(root), manifest.Version, "pack", logs);
        }

        // 2) 常态唯一来源：exe 内嵌内容（与 exe 同版本构建发布，不存在独立包与包缓存）
        return new PackResolution(new EmbeddedResourceProvider(), null, "embedded", new List<string>());
    }

    // 启动时统一更新检查的结论：latest.json 的统一版本对照当前 exe 版本，
    // 引擎轨道额外要求 engine_sha256：静默落位的二进制必须有校验值，
    // 发布源没给哈希就当引擎没有这个更新（旧发布源场景）
    internal sealed record UpdateOffer(string Version, string EngineCurrent, string? EngineSha256)
    {
        public bool HasEngineUpdate => IsNewer(Version, EngineCurrent) && !string.IsNullOrWhiteSpace(EngineSha256);
        public bool HasAnyUpdate => HasEngineUpdate;

        // 确认框文案：只列本次真正会更新的部分
        public string Describe() => $"程序 {EngineCurrent} → {Version}（替换 exe，重新运行后生效）";
    }

    // 启动时统一更新检查：一次 latest.json 拉取，统一版本对照 exe 版本。
    // 无网络 / 超时 / 字段缺失 → 返回 null 并在 logs 给出降级说明；
    // 拉到清单但不需要更新 → 返回 HasAnyUpdate == false 的 offer（调用方可据此提示「已是最新」）
    public static UpdateOffer? TryFetchUpdateOffer(string engineVersion, List<string> logs)
    {
        var latest = TryFetchLatest(logs);
        if (latest is null)
            return null;

        return new UpdateOffer(latest.Version, engineVersion, latest.EngineSha256);
    }

    // 按已确认的更新结论落地安装（唯一轨道：引擎自更新）。返回 true = 安装成功，
    // 调用方必须退出并提示重跑（否则按启动时加载的旧内容生成会回写降级配方）；失败只告警不阻塞
    public static bool ApplyUpdates(UpdateOffer offer, Func<UpdateOffer, bool> confirm)
    {
        if (!offer.HasAnyUpdate || !confirm(offer))
        {
            Console.WriteLine("[update] 本次跳过更新");
            return false;
        }

        return TryUpdateEngine(offer.Version, offer.EngineSha256);
    }

    // 引擎在线自更新：共享缓存命中或下载 → SHA-256 校验 → 「运行中 exe 改名 .old + 新 exe 落位」原子替换。
    // 哈希校验是唯一完整性防线：latest.json 缺 engine_sha256（旧发布源）或校验不匹配一律拒绝落位——
    // 二进制绝不接受未校验字节；参数传调用方刚查到的远端版本与哈希，避免二次网络请求
    public static bool TryUpdateEngine(string expectedVersion, string? expectedSha256)
    {
        var currentExe = Environment.ProcessPath;
        if (currentExe is null || !File.Exists(currentExe))
        {
            Console.WriteLine("[engine] 无法定位当前 exe，跳过引擎自更新");
            return false;
        }

        if (string.IsNullOrWhiteSpace(expectedSha256))
        {
            Console.WriteLine("[engine] 告警：发布源 latest.json 缺少 engine_sha256，跳过引擎自更新（请到 Release 页手动下载）");
            Console.WriteLine($"[engine] {BaseUrl.Replace("/releases/latest/download", "/releases/latest")}");
            return false;
        }

        try
        {
            // 同机目录副本共享一份已校验 exe：第一个目录下载落缓存，其余目录哈希命中免下载；
            // 缓存以 expectedSha256 回验，等价于“已下载并校验过的字节”，不另设信任边界
            var cachedBytes = TryReadCachedEngine(CacheRoot, expectedVersion, expectedSha256);
            byte[] exeBytes;
            if (cachedBytes is not null)
            {
                Console.WriteLine($"[engine] 本机缓存命中 {expectedVersion}，跳过下载");
                exeBytes = cachedBytes;
            }
            else
            {
                Console.WriteLine($"[engine] 下载新程序 {expectedVersion} …");
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(300) };
                exeBytes = client.GetByteArrayAsync($"{BaseUrl}/{EngineAsset}").GetAwaiter().GetResult();

                var actual = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(exeBytes));
                if (!actual.Equals(expectedSha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"SHA-256 不匹配（期望 {expectedSha256[..Math.Min(16, expectedSha256.Length)]}…，实际 {actual[..16]}…）");

                TryStoreCachedEngine(CacheRoot, expectedVersion, exeBytes);
            }

            var tempExe = currentExe + ".new";
            File.WriteAllBytes(tempExe, exeBytes);

            var oldExe = currentExe + ".old";
            if (File.Exists(oldExe))
                File.Delete(oldExe);
            File.Move(currentExe, oldExe);
            File.Move(tempExe, currentExe);

            Console.WriteLine($"[engine] 程序已更新到 {expectedVersion}，重新运行 use-ai-rule 生效（旧版本将自动清理）");
            return true;
        }
        catch (Exception ex)
        {
            // 失败收尾走还原语义而非单纯清理：move2 失败时原路径已空缺、.old 是唯一可用副本，
            // 直接清残留会把用户的 exe 删没
            if (!TryAbortEngineSwap(currentExe))
            {
                Console.WriteLine("[engine] 告警：旧版本滞留为 use-ai-rule.old，请手动去掉 .old 后缀恢复后重试");
            }
            Console.WriteLine($"[engine] 告警：引擎更新失败（{ex.Message}），请到 Release 页手动下载");
            Console.WriteLine($"[engine] {BaseUrl.Replace("/releases/latest/download", "/releases/latest")}");
            return false;
        }
    }

    // 替换中断收尾：原路径空缺（move1 已把运行中 exe 改名 .old）时优先还原，再清 .new 半成品。
    // 返回 false = .old 还原失败仍滞留且原路径空缺（异常占用），.old 是唯一副本不再删除，留用户手动恢复
    internal static bool TryAbortEngineSwap(string currentExe)
    {
        var oldExe = currentExe + ".old";
        var newExe = currentExe + ".new";
        try
        {
            if (!File.Exists(currentExe) && File.Exists(oldExe))
                File.Move(oldExe, currentExe);
        }
        catch (Exception)
        {
            TryDeleteIfExists(newExe);
            return false;
        }

        TryDeleteIfExists(newExe);
        return true;
    }

    private static void TryDeleteIfExists(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception)
        {
            // 占用中：下次启动清理
        }
    }

    // 引擎共享缓存：同机所有目录副本共用 <缓存根>\engine\<版本>\use-ai-rule.exe（USE_AI_RULE_CACHE 隔离）；
    // 回读以 engine_sha256 逐字节回验，损坏删除回退下载，读写失败只降级不阻塞更新
    internal static string EngineCachePath(string cacheRoot, string version)
        => Path.Combine(cacheRoot, "engine", version, EngineAsset);

    internal static byte[]? TryReadCachedEngine(string cacheRoot, string version, string expectedSha256)
    {
        try
        {
            var path = EngineCachePath(cacheRoot, version);
            if (!File.Exists(path))
                return null;

            var bytes = File.ReadAllBytes(path);
            var actual = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));
            if (!actual.Equals(expectedSha256, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(path);
                return null;
            }

            return bytes;
        }
        catch (Exception)
        {
            return null;
        }
    }

    // 校验通过的字节落共享缓存（temp + 原子改名，避免并发读到半截文件）；失败静默，其余目录副本走下载
    internal static void TryStoreCachedEngine(string cacheRoot, string version, byte[] exeBytes)
    {
        var temp = EngineCachePath(cacheRoot, version) + ".tmp";
        try
        {
            var target = EngineCachePath(cacheRoot, version);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.WriteAllBytes(temp, exeBytes);
            File.Move(temp, target, overwrite: true);
        }
        catch (Exception)
        {
            // 缓存落盘失败（磁盘 / 权限 / 并发占用）不影响本次更新，只清半成品防滞留
            TryDeleteIfExists(temp);
        }
    }

    // 供 Program.Main 启动时调用的公开清理入口（内部委托 TryCleanupEngineUpdateTemp）
    public static void CleanupEngineUpdateResidue(string currentExe)
    {
        TryCleanupEngineUpdateTemp(currentExe);
    }
    // 引擎更新残留清理：上一次自更新留下的 .old / .new（改名成功后的常态残留、或中途失败的半成品）。
    // 只处理与当前 exe 同目录同名前缀的文件，删不掉（仍被占用）静默跳过
    private static void TryCleanupEngineUpdateTemp(string currentExe)
    {
        foreach (var stale in new[] { currentExe + ".old", currentExe + ".new" })
            TryDeleteIfExists(stale);
    }

    // 准备包根目录：目录直接用；zip 解到系统临时目录（调用方校验后使用）
    private static (string Root, List<string> Logs) PreparePack(string path, bool validate)
    {
        var logs = new List<string>();
        if (Directory.Exists(path))
        {
            if (validate)
                ValidateRoot(path);
            return (Path.GetFullPath(path), logs);
        }

        if (!File.Exists(path))
            throw new FileNotFoundException($"--pack 指定的内容包不存在：{path}");

        var extractRoot = Path.Combine(Path.GetTempPath(), "use-ai-rule-pack", Guid.NewGuid().ToString("N"));
        ZipFile.ExtractToDirectory(path, extractRoot, overwriteFiles: true);
        // zip 内可能带一层根目录：解开后找不到 pack.json 时下探一层
        if (!File.Exists(Path.Combine(extractRoot, "pack.json")))
        {
            var single = Directory.GetDirectories(extractRoot) is { Length: 1 } dirs
                && File.Exists(Path.Combine(dirs[0], "pack.json"))
                    ? dirs[0]
                    : throw new InvalidOperationException($"内容包缺少 pack.json：{path}");
            extractRoot = single;
        }

        if (validate)
            ValidateRoot(extractRoot);
        logs.Add($"[pack] 使用指定内容包 {path}");
        return (extractRoot, logs);
    }

    // 包完整性校验：pack.json 可读 + files 哈希全过
    private static void ValidateRoot(string root)
    {
        var manifest = PackReader.Load(root);
        var mismatches = PackReader.Verify(manifest, root);
        if (mismatches.Count > 0)
            throw new InvalidOperationException(
                $"内容包校验失败（{mismatches.Count} 个文件不匹配）：{string.Join("、", mismatches.Take(5))}…");
    }

    private static LatestInfo? TryFetchLatest(List<string> logs)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            using var response = client.GetAsync($"{BaseUrl}/{LatestFile}").GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
            var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var latest = JsonSerializer.Deserialize<LatestInfo>(json);
            if (latest is null || string.IsNullOrWhiteSpace(latest.Version))
                return null;
            return latest;
        }
        catch (Exception)
        {
            logs.Add("[update] 更新检查超时，使用本地版本");
            return null;
        }
    }

    // 语义化版本比较：x.y.z；任一侧解析失败视为不比较（返回 false，不触发更新）。
    // internal：RecipeReplayer 的降级守卫复用同一比较（配方版本新于当前包 → 拒绝重放）
    internal static bool IsNewer(string candidate, string? current)
    {
        if (current is null)
            return true;
        var candidateVersion = Version.TryParse(candidate, out var c) ? c : null;
        var currentVersion = Version.TryParse(current, out var cur) ? cur : null;
        return candidateVersion is not null && currentVersion is not null && candidateVersion > currentVersion;
    }

    private static string? ReadOption(string[] args, string name)
    {
        var index = Array.FindIndex(args, arg => arg.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
            return null;
        if (index + 1 >= args.Length)
            throw new ArgumentException($"{name} 需要一个参数（zip 或已解包目录）", nameof(args));
        return args[index + 1];
    }
}
