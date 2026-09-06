using System.Text;
using System.Text.Json;

namespace RuleAssembler;

// 生成成功后的收尾：写配方记录 .ai-rule-profile.json，并确保目标项目 .gitignore 忽略该记录与生成器本体。
// 配方记录用于人工核对该次生成选择了什么，也为后续按原配方升级规则留档。
internal sealed class RecipeRecorder
{
    public const string RecordFileName = GitignoreTemplates.RecipeEntry;

    public static RecipeRecord Build(
        Manifest manifest,
        Profile profile,
        string mode,
        IReadOnlyDictionary<string, string> answers,
        IReadOnlyDictionary<string, string> answerSources,
        IEnumerable<string> features,
        IReadOnlyList<PlannedFile> files)
    {
        return new RecipeRecord
        {
            ManifestVersion = manifest.Vars.Version,
            GeneratedAt = DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:sszzz"),
            Mode = mode,
            ProfileId = profile.Id,
            ProfileLabel = profile.Label,
            Family = profile.Family,
            Answers = new Dictionary<string, string>(answers, StringComparer.Ordinal),
            AnswerSources = new Dictionary<string, string>(answerSources, StringComparer.Ordinal),
            Features = features.OrderBy(id => id, StringComparer.Ordinal).ToList(),
            FileHashes = HashFiles(files),
            Files = files.Select(file => file.Output).ToList()
        };
    }

    // 组合内核路径的配方：额外记录维度选择与命中组件；唯一映射到兼容样例时回填 profile 三件套，
    // 供旧读者与升级流程继续按 Profile 语义消费
    public static RecipeRecord BuildComposition(
        Manifest manifest,
        Selection selection,
        IReadOnlyList<string> componentIds,
        string mode,
        IReadOnlyDictionary<string, string> answers,
        IReadOnlyDictionary<string, string> answerSources,
        IEnumerable<string> features,
        IReadOnlyList<PlannedFile> files)
    {
        var matches = SelectionEngine.MatchCompatibilityCases(manifest, selection);
        var profile = matches.Count == 1
            ? manifest.Profiles.FirstOrDefault(item => item.Id.Equals(matches[0], StringComparison.OrdinalIgnoreCase))
            : null;
        return new RecipeRecord
        {
            ManifestVersion = manifest.Vars.Version,
            GeneratedAt = DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:sszzz"),
            Mode = mode,
            ProfileId = profile?.Id ?? "",
            ProfileLabel = profile?.Label ?? "",
            Family = profile?.Family ?? "",
            Selection = new Dictionary<string, string>(selection.Dimensions, StringComparer.Ordinal),
            ComponentIds = componentIds.ToList(),
            Answers = new Dictionary<string, string>(answers, StringComparer.Ordinal),
            AnswerSources = new Dictionary<string, string>(answerSources, StringComparer.Ordinal),
            Features = features.OrderBy(id => id, StringComparer.Ordinal).ToList(),
            FileHashes = HashFiles(files),
            Files = files.Select(file => file.Output).ToList()
        };
    }

    // 生成内容的内容寻址指纹：路径 → SHA256（UTF-8 字节，十六进制小写），供 --upgrade 识别本地修改
    public static Dictionary<string, string> HashFiles(IReadOnlyList<PlannedFile> files)
    {
        var hashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in files)
            hashes[file.Output] = HashContent(file.Content);
        return hashes;
    }

    public static string HashContent(string content)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    // 组合路径的 gitignore 模板键：唯一映射到兼容样例时沿用其 Profile 口径，否则退到 minimal
    public static string StackKeyForComposition(Manifest manifest, CompositionPlan plan)
    {
        var matches = SelectionEngine.MatchCompatibilityCases(manifest, plan.Selection);
        var profile = matches.Count == 1
            ? manifest.Profiles.FirstOrDefault(item => item.Id.Equals(matches[0], StringComparison.OrdinalIgnoreCase))
            : null;
        return profile is not null ? StackKeyFor(profile) : "minimal";
    }

    public void Write(string rootDirectory, RecipeRecord record)
    {
        var path = Path.Combine(Path.GetFullPath(rootDirectory), RecordFileName);
        var json = JsonSerializer.Serialize(record, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
        // WriteIndented 的缩进换行随系统为 CRLF，仓库约定生成文件统一 LF
        File.WriteAllText(path, json.Replace("\r\n", "\n") + "\n", new UTF8Encoding(false));
    }

    // 返回给用户看的说明文案；.gitignore 缺失时按技术栈创建默认模板
    public string EnsureGitignored(string rootDirectory, string stackKey)
    {
        var path = Path.Combine(Path.GetFullPath(rootDirectory), ".gitignore");
        if (File.Exists(path))
        {
            var existing = File.ReadAllLines(path)
                .Select(line => line.Trim())
                .ToHashSet(StringComparer.Ordinal);
            var missing = GitignoreTemplates.GeneratorEntries
                .Where(entry => !existing.Contains(entry))
                .ToList();
            if (missing.Count == 0)
                return ".gitignore 已包含配方记录与生成器忽略规则";

            // 只追加缺失条目，已存在的忽略规则保持原样，保证重复执行幂等
            File.AppendAllText(
                path,
                "\n" + GitignoreTemplates.GeneratorComment + string.Join("\n", missing) + "\n",
                new UTF8Encoding(false));
            return "已在 .gitignore 追加忽略规则：" + string.Join("、", missing);
        }

        File.WriteAllText(path, GitignoreTemplates.Get(stackKey), new UTF8Encoding(false));
        return "已创建 .gitignore（含技术栈默认忽略规则、配方记录与生成器本体）";
    }

    // 家族 / Profile → gitignore 模板键
    public static string StackKeyFor(Profile profile)
    {
        if (profile.Id == "flutterMobile")
            return "flutter";
        return profile.Family switch
        {
            "dotnet-backend" => "dotnet",
            "python-backend" => "python",
            "web-fullstack" => "webstack",
            "hybrid-app" => "hybrid",
            "frontend-static" => "node",
            "mobile" => "dotnet",
            "desktop" => "dotnet",
            _ => "minimal"
        };
    }
}
