using System.Text.Json;
using System.Text.Json.Serialization;

namespace RuleAssembler;

// 引擎本地登记（N1 自登记 / N2 本地回退 / N3 sidecar 写入）：把「本机已验证过的 exe 字节 + 哈希」
// 登记为可发现的更新来源，支撑离线升级（--offline / 在线检查失败）与同机多目录副本免下载。
// 目录形态：<缓存根>\engine\<版本>\use-ai-rule.exe + meta.json{version,sha256}——
// 每版本目录自包含：单文件损坏只丢一个版本，删目录即清理，没有中心清单的并发写冲突
internal static class EngineCache
{
    private const string MetaFile = "meta.json";
    private const int KeepVersions = 3;

    // sidecar 契约：version 与目录名一致、sha256 为目录内 exe 的 SHA-256（大写十六进制）。
    // 自背书只防损坏（写中断 / 字节漂移）不防篡改——能写缓存根的攻击者直接改项目 exe 更省事，
    // 不新增攻击面；使用侧（N2 / TryReadCachedEngine）一律哈希回验，漂移字节绝不当命中
    private sealed record EngineMeta(
        [property: JsonPropertyName("version")] string Version,
        [property: JsonPropertyName("sha256")] string Sha256);

    // N1 启动自登记：把当前运行中的 exe 字节 + 哈希登进本机缓存（命令模式同样登记——
    // script-only 用户的目录副本也该成为同机更新源；常态目录已完整，读 sidear 即命中零拷贝）。
    // first-wins：目录在且 sidecar 完整、exe 在场即跳过重写，避免每次启动读写 70MB；
    // 登记缺失 / 不完整时以自身字节整体重写（运行中字节比磁盘上来源不明的缓存更可信）。
    // dev / 版本不可解析 / 定位不到 exe：不登记（与 IsNewer 口径一致，不参与任何比较）
    internal static bool RegisterCurrentEngine(string engineVersion, string? currentExePath = null)
    {
        if (!Version.TryParse(engineVersion, out _))
            return false;

        currentExePath ??= Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(currentExePath) || !File.Exists(currentExePath))
            return false;

        try
        {
            var dir = Path.Combine(PackResolver.CacheRoot, "engine", engineVersion);
            if (TryReadEngineSidecar(dir, engineVersion) is null
                || !File.Exists(Path.Combine(dir, PackResolver.EngineAsset)))
            {
                var exeBytes = File.ReadAllBytes(currentExePath);
                var sha256 = HashBytes(exeBytes);
                WriteFileAtomically(Path.Combine(dir, PackResolver.EngineAsset), exeBytes);
                WriteFileAtomically(
                    Path.Combine(dir, MetaFile),
                    JsonSerializer.SerializeToUtf8Bytes(new EngineMeta(engineVersion, sha256)));
            }

            // 清理与是否新写无关：first-wins 跳过写也要收规模，否则常态启动永远不触发清理
            PruneEngineCache(engineVersion);
            return true;
        }
        catch (Exception)
        {
            // 登记失败（磁盘 / 权限 / exe 被占用）只影响本机缓存命中率，不影响本次运行
            return false;
        }
    }

    // N2 本地回退：在缓存里找比当前版本更高且校验完整的引擎条目（--offline 升级刚需、
    // 在线检查失败的降级路径）。候选按版本从高到低逐个做 exe 哈希回验，
    // 漂移 / 丢失的目录就地剔除（自愈）后继续找次高版本；找不到返回 null
    internal static PackResolver.UpdateOffer? TryFetchLocalOffer(string engineVersion)
    {
        try
        {
            var engineRoot = Path.Combine(PackResolver.CacheRoot, "engine");
            if (!Directory.Exists(engineRoot))
                return null;

            // 候选收集：目录名可解析为版本、比当前新、sidecar 完整（version 对得上目录名且哈希非空）
            var candidates = new List<(Version V, string Dir, EngineMeta Meta)>();
            foreach (var dir in Directory.EnumerateDirectories(engineRoot))
            {
                var name = Path.GetFileName(dir);
                if (!Version.TryParse(name, out var version))
                    continue;
                if (!PackResolver.IsNewer(name, engineVersion))
                    continue;
                var meta = TryReadEngineSidecar(dir, name);
                if (meta is null)
                    continue;
                candidates.Add((version, dir, meta));
            }

            foreach (var candidate in candidates.OrderByDescending(c => c.V))
            {
                var exePath = Path.Combine(candidate.Dir, PackResolver.EngineAsset);
                if (!File.Exists(exePath)
                    || !HashFile(exePath).Equals(candidate.Meta.Sha256, StringComparison.OrdinalIgnoreCase))
                {
                    // 自愈：sidecar 在而 exe 丢失 / 哈希漂移，整目录剔除后继续次高版本
                    TryPruneEngineDir(candidate.Dir);
                    continue;
                }

                return new PackResolver.UpdateOffer(
                    Path.GetFileName(candidate.Dir), engineVersion, candidate.Meta.Sha256, FromLocalCache: true);
            }

            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    // N3 sidecar 写入：给缓存版本目录补 meta.json（下载校验通过后落位、缓存命中时自愈
    // 旧版程序留下的无 sidecar 目录）。first-wins：已完整登记不覆盖；失败静默（只影响缓存命中率）
    internal static void TryStoreEngineSidecar(string cacheRoot, string version, string sha256)
    {
        try
        {
            var dir = Path.Combine(cacheRoot, "engine", version);
            if (TryReadEngineSidecar(dir, version) is not null)
                return;

            WriteFileAtomically(
                Path.Combine(dir, MetaFile),
                JsonSerializer.SerializeToUtf8Bytes(new EngineMeta(version, sha256)));
        }
        catch (Exception)
        {
            // sidecar 写失败不影响本次更新，只让该目录暂不参与本地回退
        }
    }

    // 缓存规模守卫：登记后只保留当前版本 + 最高的 KeepVersions 个版本目录，其余整目录清理；
    // 目录名不可解析的一律不动（可能是用户文件或未来格式，删除交给显式操作）
    private static void PruneEngineCache(string currentVersion)
    {
        try
        {
            var engineRoot = Path.Combine(PackResolver.CacheRoot, "engine");
            if (!Directory.Exists(engineRoot))
                return;

            var versionDirs = new List<(Version V, string Dir)>();
            foreach (var dir in Directory.EnumerateDirectories(engineRoot))
            {
                if (Version.TryParse(Path.GetFileName(dir), out var version))
                    versionDirs.Add((version, dir));
            }

            var keep = new HashSet<string>(
                versionDirs.OrderByDescending(d => d.V).Take(KeepVersions).Select(d => d.Dir),
                StringComparer.OrdinalIgnoreCase);
            keep.Add(Path.Combine(engineRoot, currentVersion));

            foreach (var dir in versionDirs)
            {
                if (!keep.Contains(dir.Dir))
                    TryPruneEngineDir(dir.Dir);
            }
        }
        catch (Exception)
        {
            // 清理失败只让缓存多占点磁盘，下次启动再试
        }
    }

    // 读某版本目录的 sidecar 并校验口径：meta.json 可读、version 与目录名一致、sha256 非空。
    // 任何一步失败返回 null（视为未登记 / 登记不完整，由调用方决定重写或忽略）
    private static EngineMeta? TryReadEngineSidecar(string dir, string expectedVersion)
    {
        try
        {
            var path = Path.Combine(dir, MetaFile);
            if (!File.Exists(path))
                return null;

            var meta = JsonSerializer.Deserialize<EngineMeta>(File.ReadAllBytes(path));
            if (meta is null
                || !string.Equals(meta.Version, expectedVersion, StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(meta.Sha256))
                return null;

            return meta;
        }
        catch (Exception)
        {
            return null;
        }
    }

    // 整目录剔除（自愈 / 规模清理共用）：删不掉（占用 / 权限）静默，下次再试
    private static void TryPruneEngineDir(string dir)
    {
        try
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
        catch (Exception)
        {
        }
    }

    // 原子写：Guid 临时名落盘后改名，避免并发读到半截文件；失败清半成品防滞留
    private static void WriteFileAtomically(string path, byte[] bytes)
    {
        var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(temp, bytes);
            File.Move(temp, path, overwrite: true);
        }
        catch (Exception)
        {
            try
            {
                if (File.Exists(temp))
                    File.Delete(temp);
            }
            catch (Exception)
            {
            }
        }
    }

    private static string HashFile(string path)
        => HashBytes(File.ReadAllBytes(path));

    private static string HashBytes(byte[] bytes)
        => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));
}
