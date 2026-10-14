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
        [property: JsonPropertyName("sha256")] string Sha256);

    private sealed record Pointer([property: JsonPropertyName("version")] string Version);

    // args 为去掉已知开关后的原始参数；interactive 表示当前启动可询问（无参数交互模式）
    public static PackResolution Resolve(string[] args, bool interactive, Func<string, bool>? confirmUpgrade = null)
    {
        var packArg = ReadOption(args, "--pack");
        var offline = args.Contains("--offline", StringComparer.OrdinalIgnoreCase);

        // 1) 显式指定包：zip 或已解包目录，校验失败硬报错（显式指定不做静默兜底）
        if (packArg is not null)
        {
            var (root, logs) = PreparePack(packArg, validate: true);
            var manifest = PackReader.Load(root);
            return new PackResolution(new FolderResourceProvider(root), manifest.Version, "pack", logs);
        }

        // 2) 离线模式：缓存命中用缓存，否则内嵌，绝不联网
        if (offline)
        {
            var cached = FindCachedRoot();
            if (cached is not null)
            {
                var manifest = PackReader.Load(cached);
                var logs = new List<string> { $"[pack] 离线模式：使用本地缓存 {manifest.Version}" };
                return new PackResolution(new FolderResourceProvider(cached), manifest.Version, "cache", logs);
            }

            return new PackResolution(new EmbeddedResourceProvider(), null, "embedded",
                new List<string> { "[pack] 无本地缓存，使用内嵌资源" });
        }

        // 3) 交互启动：检查更新（失败静默降级）；命令行启动：直接用现有来源，不产生网络副作用
        var cachedRoot = FindCachedRoot();
        if (interactive)
            return ResolveInteractive(cachedRoot, confirmUpgrade);

        if (cachedRoot is not null)
        {
            var manifest = PackReader.Load(cachedRoot);
            return new PackResolution(new FolderResourceProvider(cachedRoot), manifest.Version, "cache", new List<string>());
        }

        return new PackResolution(new EmbeddedResourceProvider(), null, "embedded", new List<string>());
    }

    // 交互路径：拉 latest.json → 有新版且用户同意 → 下载安装；其余情况沿用缓存或内嵌
    private static PackResolution ResolveInteractive(string? cachedRoot, Func<string, bool>? confirmUpgrade)
    {
        var logs = new List<string>();
        var latest = TryFetchLatest(logs);
        var cachedVersion = cachedRoot is null ? null : SafePackVersion(cachedRoot);

        if (latest is not null && IsNewer(latest.Version, cachedVersion))
        {
            var agreed = confirmUpgrade?.Invoke(latest.Version) ?? true;
            if (agreed)
            {
                try
                {
                    var installed = DownloadAndInstall(latest);
                    logs.Add($"[pack] 已更新到 {latest.Version}");
                    return new PackResolution(new FolderResourceProvider(installed), latest.Version, "pack", logs);
                }
                catch (Exception ex)
                {
                    logs.Add($"[pack] 告警：更新失败（{ex.Message}），继续使用 {cachedVersion ?? "内嵌资源"}");
                }
            }
            else
            {
                logs.Add($"[pack] 本次跳过更新 {latest.Version}");
            }
        }

        if (cachedRoot is not null)
        {
            if (latest is not null && !IsNewer(latest.Version, cachedVersion))
                logs.Add($"[pack] 已是最新：{cachedVersion}");
            return new PackResolution(new FolderResourceProvider(cachedRoot), cachedVersion, "cache", logs);
        }

        return new PackResolution(new EmbeddedResourceProvider(), null, "embedded", logs);
    }

    // 准备包根目录：目录直接用；zip 解到缓存下的临时目录（调用方校验后落位）
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

        var extractRoot = Path.Combine(CacheRoot, "explicit", Guid.NewGuid().ToString("N"));
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

    private static string? FindCachedRoot()
    {
        try
        {
            var pointer = Path.Combine(CacheRoot, PointerFile);
            if (!File.Exists(pointer))
                return null;
            var version = JsonSerializer.Deserialize<Pointer>(File.ReadAllText(pointer))?.Version;
            if (string.IsNullOrWhiteSpace(version))
                return null;

            var root = Path.Combine(CacheRoot, version);
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
