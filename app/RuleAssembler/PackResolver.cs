using System.Text.Json.Serialization;
using System.IO.Compression;
using System.Text.Json;

namespace RuleAssembler;

// 内容包解析结果：资源来源（缓存包目录或内嵌）、生效版本与说明性输出（[pack] 行）
internal sealed record PackResolution(IResourceProvider Provider, string? Version, string Source, List<string> Logs);

// 内容包解析器：决定规则与技能内容从哪来。
// 优先级：--pack 指定包（硬失败语义）→ 本地缓存 → 程序集内嵌兜底；
// 缓存与内嵌的版本比较在 Resolve 中无条件完成（谁新用谁），与网络更新无关，
// 因此用户拒绝更新后不需要再额外比较本地内容；
// 更新检查只在交互模式启动时做一次（TryFetchUpdateOffer → ApplyUpdates，一次 latest.json
// 拉取，统一版本同时对照 exe 与生效内容两个基准，3 秒超时），命令行 / --offline / --pack 不做网络检查；
// 更新安装成功后返回 true，交互入口据此重启自身进程让新内容与新引擎即时生效。
// 单 exe 无网络永远可用：下载失败、校验失败、超时都只降级不阻塞。
internal static class PackResolver
{
    private const string LatestFile = "latest.json";
    private const string PointerFile = "current.json";
    private const string PackAsset = "ai-rule-pack.zip";

    // 默认发布源：GitHub Release latest 端点；环境变量 USE_AI_RULE_PACK_URL 可整体替换（gitee 镜像等）
    private static string BaseUrl =>
        Environment.GetEnvironmentVariable("USE_AI_RULE_PACK_URL")?.TrimEnd('/')
        ?? "https://github.com/azrng/ai-code-rule/releases/latest/download";

    // 缓存根：%LOCALAPPDATA%\use-ai-rule\packs\<version>\；
    // USE_AI_RULE_PACK_CACHE 可整体替换（测试隔离 / 双缓存并存），与 USE_AI_RULE_PACK_URL 同风格
    internal static string CacheRoot
    {
        get
        {
            var overrideRoot = Environment.GetEnvironmentVariable("USE_AI_RULE_PACK_CACHE")?.Trim();
            return string.IsNullOrEmpty(overrideRoot)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "use-ai-rule", "packs")
                : overrideRoot!;
        }
    }

    // latest.json 契约（统一版本）：version = manifest.vars.version = exe 注入版本，双产物共用；
    // sha256 = 内容包 zip 校验值，engine_sha256 = win-x64 exe 校验值。
    // 已废弃的 engine_version 字段：旧版 exe 仍读它做引擎更新判定，CI 保留写入同值兼容字段，
    // 新代码不读取（JSON 反序列化默认忽略多余字段）
    private sealed record LatestInfo(
        [property: JsonPropertyName("version")] string Version,
        [property: JsonPropertyName("engine_sha256")] string? EngineSha256,
        [property: JsonPropertyName("sha256")] string Sha256);

    private sealed record Pointer([property: JsonPropertyName("version")] string Version);

    // args 为去掉已知开关后的原始参数；interactive 表示当前启动可询问（无参数交互模式）。
    // 更新检查是惰性的：Resolve 只决定「本次用哪个来源」，绝不产生网络副作用；
    // 更新检查由 Program 在交互启动阶段调用（TryFetchUpdateOffer / ApplyUpdates），一次拉取覆盖双轨道
    public static PackResolution Resolve(string[] args, string? cacheRoot = null)
    {
        // cacheRoot 供测试注入隔离目录；生产缺省走本机缓存根，行为不变
        var cache = cacheRoot ?? CacheRoot;
        var packArg = ReadOption(args, "--pack");
        var offline = args.Contains("--offline", StringComparer.OrdinalIgnoreCase);

        // 1) 显式指定包：zip 或已解包目录，校验失败硬报错（显式指定不做静默兜底）
        if (packArg is not null)
        {
            var (root, logs) = PreparePack(packArg, validate: true, cache);
            var manifest = PackReader.Load(root);
            return new PackResolution(new FolderResourceProvider(root), manifest.Version, "pack", logs);
        }

        // 2) 离线模式 / 命令行模式：缓存命中用缓存，否则内嵌，绝不联网。
        // 缓存版本可能旧于当前 exe 内嵌内容（引擎换代先于内容包发布）：谁新用谁，
        // 避免旧缓存压过内嵌新内容导致技能登记等新特性静默失效
        var cached = FindCachedRoot(cache);
        if (cached is not null)
        {
            var manifest = PackReader.Load(cached);
            var embeddedVersion = TryGetEmbeddedPackVersion();
            if (embeddedVersion is not null && IsNewer(embeddedVersion, manifest.Version))
            {
                // 缓存收敛：把内嵌内容落位为缓存并切指针，缓存从此与内嵌同版；
                // 更新检查基准、离线加载与「下载安装的新包」走同一条加载路径
                Console.WriteLine(
                    $"[pack] 本地缓存 {manifest.Version} 旧于引擎内嵌内容 {embeddedVersion}，升位缓存…");
                var installed = TryInstallEmbedded(cache, embeddedVersion);
                if (installed is not null)
                {
                    var installedManifest = PackReader.Load(installed);
                    var upgradeLogs = offline
                        ? new List<string> { $"[pack] 离线模式：缓存已升位到 {installedManifest.Version}（来自引擎内嵌）" }
                        : new List<string> { $"[pack] 缓存已从 {manifest.Version} 升位到 {installedManifest.Version}（来自引擎内嵌）" };
                    return new PackResolution(
                        new FolderResourceProvider(installed), installedManifest.Version, "cache", upgradeLogs);
                }

                Console.WriteLine("[pack] 告警：缓存升位失败，本次使用内嵌内容");
                return new PackResolution(
                    new EmbeddedResourceProvider(), embeddedVersion, "embedded", new List<string>());
            }

            var logs = offline
                ? new List<string> { $"[pack] 离线模式：使用本地缓存 {manifest.Version}" }
                : new List<string>();
            return new PackResolution(new FolderResourceProvider(cached), manifest.Version, "cache", logs);
        }

        var offlineLog = offline
            ? new List<string> { "[pack] 无本地缓存，使用内嵌资源" }
            : new List<string>();
        return new PackResolution(new EmbeddedResourceProvider(), null, "embedded", offlineLog);
    }

    // 一次更新检查的结论：统一版本（latest.json 的 version，= manifest.vars.version = exe 注入版本）
    // 分别对照两个本地基准——引擎（当前 exe 版本）与内容包（生效内容版本）。正常情况下两者相等、
    // 两条轨道同进同退；不等只出现在上次更新部分失败的残留（如引擎换成功、包下载失败），
    // 此时各轨道独立判定，只补齐仍落后的一方。
    // 引擎轨道额外要求 engine_sha256：静默落位的二进制必须有校验值，
    // 发布源没给哈希就当引擎没有这个更新（旧发布源场景）
    internal sealed record UpdateOffer(
        string Version,
        string EngineCurrent,
        string? PackCurrent,
        string? EngineSha256,
        string? PackSha256)
    {
        public bool HasEngineUpdate => IsNewer(Version, EngineCurrent) && !string.IsNullOrWhiteSpace(EngineSha256);
        public bool HasPackUpdate => IsNewer(Version, PackCurrent) && !string.IsNullOrWhiteSpace(PackSha256);
        public bool HasAnyUpdate => HasEngineUpdate || HasPackUpdate;

        // 确认框文案：只列本次真正会更新的部分，避免用户面对「更新」二字不知道会动什么
        public string Describe()
        {
            var parts = new List<string>();
            if (HasEngineUpdate)
                parts.Add($"程序 {EngineCurrent} → {Version}（替换 exe，重启后生效）");
            if (HasPackUpdate)
                parts.Add($"内容包 {PackCurrent ?? "内嵌"} → {Version}");
            return string.Join("；", parts);
        }
    }

    // 启动时统一更新检查：一次 latest.json 拉取，统一版本同时对照 exe 与生效内容两个基准。
    // 无网络 / 超时 / 字段缺失 → 返回 null 并在 logs 给出降级说明；
    // 拉到清单但两条轨道都不需要更新 → 返回 HasAnyUpdate == false 的 offer（调用方可据此提示「已是最新」）
    public static UpdateOffer? TryFetchUpdateOffer(string engineVersion, string? packVersion, List<string> logs)
    {
        var latest = TryFetchLatest(logs);
        if (latest is null)
            return null;

        return new UpdateOffer(latest.Version, engineVersion, packVersion, latest.EngineSha256, latest.Sha256);
    }

    // 按已确认的更新结论落地安装：内容包下载校验切指针、引擎下载校验原子替换 exe。
    // 返回 true = 至少一项安装成功，调用方必须重启进程重新解析内容来源，
    // 否则会出现「提示已更新、正文却按启动时加载的旧内容生成、配方版本被回写降级」的静默退回；
    // 引擎安装成功但内容包失败同样返回 true（重启拉起的正是刚落位的新 exe）。
    // 任何单项失败只告警，不影响另一项，也不阻塞本次会话
    public static bool ApplyUpdates(UpdateOffer offer, Func<UpdateOffer, bool> confirm)
    {
        if (!offer.HasAnyUpdate || !confirm(offer))
        {
            Console.WriteLine("[update] 本次跳过更新");
            return false;
        }

        var installed = false;
        if (offer.HasPackUpdate)
        {
            try
            {
                Console.WriteLine($"[pack] 下载内容包 {offer.Version} …");
                DownloadAndInstall(new LatestInfo(offer.Version, null, offer.PackSha256!));
                Console.WriteLine($"[pack] 内容包已更新到 {offer.Version}");
                installed = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[pack] 告警：更新失败（{ex.Message}），继续使用本地内容");
            }
        }

        if (offer.HasEngineUpdate)
            installed |= TryUpdateEngine(offer.Version, offer.EngineSha256);

        return installed;
    }
    // 引擎在线自更新：下载新 exe → SHA-256 校验 → 「运行中 exe 改名 .old + 新 exe 落位」原子替换，
    // 重启后生效。确认安装路径上哈希校验是唯一完整性防线：远端资产缺失 / 被代理替换 /
    // 下载截断时拒绝落位，当前 exe 保持不动。latest.json 未携带 engine_sha256（旧发布源）视为
    // 校验不可用，同样拒绝替换并提示手动下载——落位的二进制绝不接受未校验字节。
    // Windows 对正在运行的 exe 只锁写不锁删/改名；改名失败（占用异常）清理半成品并回退提示手动下载。
    // expectedVersion / expectedSha256 传入调用方刚查到的远端统一版本与哈希，避免二次网络请求
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
            Console.WriteLine($"[engine] 下载新程序 {expectedVersion} …");
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(300) };
            var exeBytes = client.GetByteArrayAsync($"{BaseUrl}/use-ai-rule.exe").GetAwaiter().GetResult();

            var actual = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(exeBytes));
            if (!actual.Equals(expectedSha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"SHA-256 不匹配（期望 {expectedSha256[..Math.Min(16, expectedSha256.Length)]}…，实际 {actual[..16]}…）");

            var tempExe = currentExe + ".new";
            File.WriteAllBytes(tempExe, exeBytes);

            var oldExe = currentExe + ".old";
            if (File.Exists(oldExe))
                File.Delete(oldExe);
            File.Move(currentExe, oldExe);
            File.Move(tempExe, currentExe);

            Console.WriteLine($"[engine] 程序已更新到 {expectedVersion}，重新启动 exe 生效（旧版本将自动清理）");
            return true;
        }
        catch (Exception ex)
        {
            TryCleanupEngineUpdateTemp(currentExe);
            Console.WriteLine($"[engine] 告警：引擎更新失败（{ex.Message}），请到 Release 页手动下载");
            Console.WriteLine($"[engine] {BaseUrl.Replace("/releases/latest/download", "/releases/latest")}");
            return false;
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
        {
            try
            {
                if (File.Exists(stale))
                    File.Delete(stale);
            }
            catch (Exception)
            {
                // 占用中：下下次启动再试
            }
        }
    }

    // 准备包根目录：目录直接用；zip 解到注入缓存根下的临时目录（调用方校验后落位）
    private static (string Root, List<string> Logs) PreparePack(string path, bool validate, string cacheRoot)
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

        var extractRoot = Path.Combine(cacheRoot, "explicit", Guid.NewGuid().ToString("N"));
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

    private static string? FindCachedRoot(string cacheRoot)
    {
        try
        {
            var pointer = Path.Combine(cacheRoot, PointerFile);
            if (!File.Exists(pointer))
                return null;
            var version = JsonSerializer.Deserialize<Pointer>(File.ReadAllText(pointer))?.Version;
            if (string.IsNullOrWhiteSpace(version))
                return null;

            var root = Path.Combine(cacheRoot, version);
            return File.Exists(Path.Combine(root, "pack.json")) ? root : null;
        }
        catch (Exception)
        {
            // 指针损坏按无缓存处理，内嵌兜底
            return null;
        }
    }

    // 内嵌内容包版本：内嵌资源没有 pack.json，从 fragments/manifest.json 的 vars.version 读取；
    // 读取失败返回 null（视为无法比较，保持既有缓存优先行为）
    private static string? TryGetEmbeddedPackVersion()
    {
        try
        {
            var provider = new EmbeddedResourceProvider();
            var bytes = provider.ReadBytes("fragments/manifest.json");
            if (bytes is null || bytes.Length == 0)
                return null;
            using var doc = JsonDocument.Parse(bytes);
            var version = doc.RootElement
                .GetProperty("vars")
                .GetProperty("version")
                .GetString();
            return string.IsNullOrWhiteSpace(version) ? null : version;
        }
        catch (Exception)
        {
            return null;
        }
    }

    // 把引擎内嵌内容落位为缓存目录并切指针：缓存从此收敛到内嵌版本。
    // 任何失败返回 null（调用方降级为直接使用内嵌），不阻塞启动
    private static string? TryInstallEmbedded(string cacheRoot, string version)
    {
        try
        {
            var provider = new EmbeddedResourceProvider();
            var staging = Path.Combine(cacheRoot, $"{version}.staging");
            if (Directory.Exists(staging))
                Directory.Delete(staging, recursive: true);
            Directory.CreateDirectory(staging);

            var files = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var relative in provider.Names)
            {
                var bytes = provider.ReadBytes(relative);
                if (bytes is null)
                    continue;
                var target = Path.Combine(staging, relative.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.WriteAllBytes(target, bytes);
                files[relative] = Convert.ToHexString(
                    System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant();
            }

            var packManifest = new PackManifest("merged", version, 1, new Dictionary<string, string>(files));
            File.WriteAllText(Path.Combine(staging, "pack.json"), JsonSerializer.Serialize(packManifest));

            var manifest = PackReader.Load(staging);
            if (PackReader.Verify(manifest, staging).Count > 0)
            {
                Directory.Delete(staging, recursive: true);
                return null;
            }

            var targetRoot = Path.Combine(cacheRoot, version);
            if (Directory.Exists(targetRoot))
                Directory.Delete(targetRoot, recursive: true);
            Directory.Move(staging, targetRoot);
            File.WriteAllText(
                Path.Combine(cacheRoot, PointerFile), JsonSerializer.Serialize(new Pointer(version)));
            return targetRoot;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string? SafePackVersion(string root)
    {
        try
        {
            return PackReader.Load(root).Version;
        }
        catch (Exception)
        {
            return null;
        }
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
            logs.Add("[pack] 更新检查超时，使用本地版本");
            return null;
        }
    }

    // 下载 zip → 整包 SHA-256 校验 → 解压 → 逐文件校验 → 原子切指针
    private static string DownloadAndInstall(LatestInfo latest)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
        var tempZip = Path.Combine(CacheRoot, $"{latest.Version}.zip.tmp");
        Directory.CreateDirectory(CacheRoot);
        using (var response = client.GetAsync($"{BaseUrl}/{PackAsset}").GetAwaiter().GetResult())
        {
            response.EnsureSuccessStatusCode();
            File.WriteAllBytes(tempZip, response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult());
        }

        var actual = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(tempZip)));
        if (!actual.Equals(latest.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            File.Delete(tempZip);
            throw new InvalidOperationException("SHA-256 不匹配");
        }

        var targetRoot = Path.Combine(CacheRoot, latest.Version);
        var staging = targetRoot + ".staging";
        if (Directory.Exists(staging))
            Directory.Delete(staging, recursive: true);
        ZipFile.ExtractToDirectory(tempZip, staging, overwriteFiles: true);
        if (!File.Exists(Path.Combine(staging, "pack.json")))
        {
            var dirs = Directory.GetDirectories(staging);
            if (dirs.Length == 1 && File.Exists(Path.Combine(dirs[0], "pack.json")))
            {
                var nested = staging + ".nested";
                Directory.Move(dirs[0], nested);
                Directory.Delete(staging, recursive: true);
                staging = nested;
            }
        }

        var manifest = PackReader.Load(staging);
        var mismatches = PackReader.Verify(manifest, staging);
        if (mismatches.Count > 0)
        {
            Directory.Delete(staging, recursive: true);
            File.Delete(tempZip);
            throw new InvalidOperationException($"内容包 {mismatches.Count} 个文件校验失败");
        }

        if (Directory.Exists(targetRoot))
            Directory.Delete(targetRoot, recursive: true);
        Directory.Move(staging, targetRoot);
        File.Delete(tempZip);
        File.WriteAllText(Path.Combine(CacheRoot, PointerFile), JsonSerializer.Serialize(new Pointer(latest.Version)));
        return targetRoot;
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
