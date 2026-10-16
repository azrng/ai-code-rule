using System.Text.Json.Serialization;
using System.IO.Compression;
using System.Text.Json;

namespace RuleAssembler;

// 内容包解析结果：资源来源（缓存包目录或内嵌）、生效版本与说明性输出（[pack] 行）
internal sealed record PackResolution(IResourceProvider Provider, string? Version, string Source, List<string> Logs);

// 内容包解析器：决定规则与技能内容从哪来。
// 优先级：--pack 指定包（硬失败语义）→ 本地缓存 → 程序集内嵌兜底；
// 交互启动时尝试更新检查（latest.json，3 秒超时），命令行 / --offline 不做网络检查。
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

    // 缓存根：%LOCALAPPDATA%\use-ai-rule\packs\<version>\
    internal static string CacheRoot =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "use-ai-rule", "packs");

    private sealed record LatestInfo(
        [property: JsonPropertyName("version")] string Version,
        [property: JsonPropertyName("engine_version")] string? EngineVersion,
        [property: JsonPropertyName("sha256")] string Sha256);

    private sealed record Pointer([property: JsonPropertyName("version")] string Version);

    // args 为去掉已知开关后的原始参数；interactive 表示当前启动可询问（无参数交互模式）。
    // 更新检查是惰性的：Resolve 只决定「本次用哪个来源」，绝不产生网络副作用；
    // 真正的更新检查由 Program 在用户选择了会用到内容包的操作后调用 CheckForUpdates
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

        // 2) 离线模式 / 命令行模式：缓存命中用缓存，否则内嵌，绝不联网
        var cached = FindCachedRoot(cache);
        if (cached is not null)
        {
            var manifest = PackReader.Load(cached);
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

    // 惰性更新检查：进入会使用内容包的交互路径（生成规则 / 导入 Skills）前调用。
    // 拉一次 latest.json，有新版且用户同意则下载安装；任何失败静默降级不阻塞
    public static void CheckForUpdates(string? currentVersion, Func<string, bool> confirmUpgrade)
    {
        var logs = new List<string>();
        var latest = TryFetchLatest(logs);
        if (latest is null)
        {
            foreach (var line in logs)
                Console.WriteLine(line);
            return;
        }

        if (!IsNewer(latest.Version, currentVersion))
            return;

        if (!confirmUpgrade(latest.Version))
        {
            Console.WriteLine($"[pack] 本次跳过更新 {latest.Version}");
            return;
        }

        try
        {
            DownloadAndInstall(latest);
            Console.WriteLine($"[pack] 内容包已更新到 {latest.Version}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[pack] 告警：更新失败（{ex.Message}），继续使用 {currentVersion ?? "内嵌资源"}");
        }
    }

    // 引擎版本检查：只拉 latest.json 返回远端引擎版本（无网络 / 字段缺失返回 null），不下载
    public static string? TryFetchLatestEngineVersion()
    {
        var logs = new List<string>();
        var latest = TryFetchLatest(logs);
        return latest?.EngineVersion is { Length: > 0 } remote ? remote : null;
    }

    // 引擎版本比较：引擎用 2.x（run_number 单调递增），与内容包 IsNewer 的 x.y.z 语义兼容
    public static bool IsEngineNewer(string candidate, string current)
    {
        return IsNewer(candidate, current);
    }

    // 引擎在线自更新：下载新 exe → 「运行中 exe 改名 .old + 新 exe 落位」原子替换，重启后生效。
    // Windows 对正在运行的 exe 只锁写不锁删/改名；改名失败（占用异常）清理半成品并回退提示手动下载。
    // expectedLatest 传入调用方刚查到的远端版本，避免二次网络请求
    public static bool TryUpdateEngine(string currentEngineVersion, string expectedLatest)
    {
        var currentExe = Environment.ProcessPath;
        if (currentExe is null || !File.Exists(currentExe))
        {
            Console.WriteLine("[engine] 无法定位当前 exe，跳过引擎自更新");
            return false;
        }

        try
        {
            Console.WriteLine($"[engine] 下载新引擎 {expectedLatest} …");
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(300) };
            var exeBytes = client.GetByteArrayAsync($"{BaseUrl}/use-ai-rule.exe").GetAwaiter().GetResult();

            var tempExe = currentExe + ".new";
            File.WriteAllBytes(tempExe, exeBytes);

            var oldExe = currentExe + ".old";
            if (File.Exists(oldExe))
                File.Delete(oldExe);
            File.Move(currentExe, oldExe);
            File.Move(tempExe, currentExe);

            Console.WriteLine($"[engine] 引擎已更新到 {expectedLatest}，重新启动 exe 生效（旧版本将自动清理）");
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

    // 语义化版本比较：x.y.z；任一侧解析失败视为不比较（返回 false，不触发更新）
    private static bool IsNewer(string candidate, string? current)
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
