using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace RuleAssembler;

internal static class Program
{
    private const string Banner = "AI Rule 拼接器 — 把一套完整 AI rule 释放到当前目录";

    private static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        Console.WriteLine(Banner);
        Console.WriteLine($"当前目录：{Environment.CurrentDirectory}");
        Console.WriteLine();

        var manifest = LoadManifest();
        if (manifest is null)
        {
            Console.Error.WriteLine("错误：无法加载 manifest.json。");
            return 1;
        }

        var arch = SelectArchitecture(manifest);
        if (arch is null)
        {
            Console.WriteLine("已取消。");
            return 0;
        }

        var files = PlanFiles(manifest, arch);
        Console.WriteLine();
        Console.WriteLine("将释放以下文件：");
        foreach (var f in files)
        {
            Console.WriteLine($"  - {f.Output}");
        }
        Console.WriteLine("（不释放 TASK.md / rulelog / devlog）");
        Console.WriteLine();

        var overwriteAll = WriteFilesInteractive(files);
        if (!overwriteAll.HasValue)
        {
            Console.WriteLine("已取消。");
            return 0;
        }
        Console.WriteLine();
        Console.WriteLine($"完成：已释放 {files.Count} 个文件到 {Environment.CurrentDirectory}");
        return 0;
    }

    // ---------- manifest 模型 ----------

    private sealed class Manifest
    {
        public List<Family> Families { get; set; } = new();
        public List<Architecture> Architectures { get; set; } = new();
        public ManifestVars Vars { get; set; } = new();
    }

    private sealed class ManifestVars
    {
        [JsonPropertyName("version")] public string Version { get; set; } = "";
        [JsonPropertyName("last_updated")] public string LastUpdated { get; set; } = "";
    }

    private sealed class Family
    {
        [JsonPropertyName("id")] public string Id { get; set; } = "";
        [JsonPropertyName("label")] public string Label { get; set; } = "";
        [JsonPropertyName("desc")] public string Desc { get; set; } = "";
    }

    private sealed class Architecture
    {
        [JsonPropertyName("id")] public string Id { get; set; } = "";
        [JsonPropertyName("label")] public string Label { get; set; } = "";
        [JsonPropertyName("family")] public string Family { get; set; } = "";
        [JsonPropertyName("desc")] public string Desc { get; set; } = "";
        [JsonPropertyName("root")] public RootVars Root { get; set; } = new();
        [JsonPropertyName("subrules")] public List<Subrule> Subrules { get; set; } = new();
        [JsonPropertyName("pointers")] public List<string> Pointers { get; set; } = new();
        [JsonPropertyName("designSystem")] public bool DesignSystem { get; set; }
    }

    private sealed class RootVars
    {
        [JsonPropertyName("target-list")] public List<string> TargetList { get; set; } = new();
        [JsonPropertyName("subrule-index")] public List<string> SubruleIndex { get; set; } = new();
        [JsonPropertyName("dev-stage")] public List<string> DevStage { get; set; } = new();
        [JsonPropertyName("understanding-checklist")] public List<string> UnderstandingChecklist { get; set; } = new();
        [JsonPropertyName("search-nouns")] public string SearchNouns { get; set; } = "";
        [JsonPropertyName("extra-sections")] public List<string> ExtraSections { get; set; } = new();
        // 以下为短语级家族差异槽；未提供时回退到纯后端家族默认值
        [JsonPropertyName("root-scope-nouns")] public string RootScopeNouns { get; set; } = "后端、文档、基础设施";
        [JsonPropertyName("context-scope-nouns")] public string ContextScopeNouns { get; set; } = "服务端实现 / 文档 / 发布链路";
        [JsonPropertyName("high-risk-nouns")] public string HighRiskNouns { get; set; } = "数据库、缓存、鉴权、发布、环境变量";
        [JsonPropertyName("high-risk-prefix")] public string HighRiskPrefix { get; set; } = "";
        [JsonPropertyName("task-kind-nouns")] public string TaskKindNouns { get; set; } = "后端还是基础设施";
        [JsonPropertyName("evidence-nouns")] public string EvidenceNouns { get; set; } = "日志、数据、断点、测试";
        [JsonPropertyName("reuse-noun")] public string ReuseNoun { get; set; } = "配置";
        [JsonPropertyName("persistence-suffix")] public string PersistenceSuffix { get; set; } = "";
        [JsonPropertyName("destructive-nouns")] public string DestructiveNouns { get; set; } = "数据破坏性变更、环境清理";
        [JsonPropertyName("contract-error-line")] public string ContractErrorLine { get; set; } = "阶段 0 输出的接口契约、DTO 或字段说明在阶段 1 发现缺口时，应先回到契约变更流程，修改 DTO / 接口定义后同步 Application、Domain、EntityFramework 映射、测试和相关文档，再继续实现；不得绕过契约直接补临时字段";
        [JsonPropertyName("reading-order")] public List<string> ReadingOrder { get; set; } = new();
        // 追加型可选槽：空串占位行在输出前统一清理
        [JsonPropertyName("intro-quote")] public string IntroQuote { get; set; } = "";
        [JsonPropertyName("checklist-infra")] public string ChecklistInfra { get; set; } = "- 涉及基础设施时，已查看 Docker、环境变量、端口、启动命令、发布脚本和部署文档";
        [JsonPropertyName("model-behavior-extra")] public string ModelBehaviorExtra { get; set; } = "";
        [JsonPropertyName("verify-nouns")] public string VerifyNouns { get; set; } = "";
        [JsonPropertyName("verify-quote")] public string VerifyQuote { get; set; } = "";
        [JsonPropertyName("rollback-scope")] public string RollbackScope { get; set; } = "发布产物、容器";
        [JsonPropertyName("boundary-nouns")] public string BoundaryNouns { get; set; } = "跨层职责";
    }

    private sealed class Subrule
    {
        [JsonPropertyName("source")] public string Source { get; set; } = "";
        [JsonPropertyName("output")] public string Output { get; set; } = "";
    }

    // ---------- manifest 加载 ----------

    private static Manifest? LoadManifest()
    {
        var json = ReadResourceByPath("manifest.json");
        if (json is null) return null;
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<Manifest>(json, opts);
    }

    // ---------- 选择流程 ----------

    private static Architecture? SelectArchitecture(Manifest manifest)
    {
        var families = manifest.Families;
        Console.WriteLine("请选择技术大类：");
        for (int i = 0; i < families.Count; i++)
            Console.WriteLine($"  {i + 1}. {families[i].Label} — {families[i].Desc}");
        Console.Write($"输入编号 (1-{families.Count})，0 取消：");

        if (!int.TryParse(Console.ReadLine(), out int fIdx) || fIdx < 1 || fIdx > families.Count)
        {
            if (fIdx == 0) return null;
            Console.WriteLine("无效输入。");
            return null;
        }
        var familyId = families[fIdx - 1].Id;

        var archs = manifest.Architectures.Where(a => a.Family == familyId).ToList();
        if (archs.Count == 0)
        {
            Console.WriteLine("该大类下暂无已接入架构。");
            return null;
        }

        Console.WriteLine();
        Console.WriteLine("请选择架构模板：");
        for (int i = 0; i < archs.Count; i++)
            Console.WriteLine($"  {i + 1}. {archs[i].Label} — {archs[i].Desc}");
        Console.Write($"输入编号 (1-{archs.Count})，0 取消：");

        if (!int.TryParse(Console.ReadLine(), out int aIdx) || aIdx < 1 || aIdx > archs.Count)
        {
            if (aIdx == 0) return null;
            Console.WriteLine("无效输入。");
            return null;
        }
        return archs[aIdx - 1];
    }

    // ---------- 文件计划 ----------

    private sealed class PlannedFile
    {
        public string Output { get; set; } = "";
        public string Content { get; set; } = "";
    }

    private static List<PlannedFile> PlanFiles(Manifest manifest, Architecture arch)
    {
        var result = new List<PlannedFile>();

        // 1. 根 AGENTS.md：填模板变量
        var rootTemplate = ReadResourceByPath("root/AGENTS.md");
        if (rootTemplate is null)
            throw new InvalidOperationException("缺失片段：root/AGENTS.md");

        var filled = FillRootTemplate(rootTemplate, manifest, arch);
        var leftover = LeftoverSlots(filled);
        if (leftover.Count > 0)
            throw new InvalidOperationException($"根模板存在未填充的变量槽：{string.Join(", ", leftover)}");
        result.Add(new PlannedFile { Output = "AGENTS.md", Content = NormalizeNewlines(filled) });

        // 2. 子规则整文件（替换子规则里的 version/last_updated 与全局一致）
        foreach (var sub in arch.Subrules)
        {
            var content = ReadResourceByPath(sub.Source);
            if (content is null)
                throw new InvalidOperationException($"缺失片段：{sub.Source}");
            content = AlignVersion(content, manifest);
            result.Add(new PlannedFile { Output = sub.Output, Content = NormalizeNewlines(content) });
        }

        // 3. 指针文件（原样输出，不做行尾规范化——指针片段与目标目录文件同源同格式）
        foreach (var p in arch.Pointers)
        {
            var content = ReadResourceByPath("pointers/" + p);
            if (content is null)
                throw new InvalidOperationException($"缺失指针片段：pointers/{p}");
            result.Add(new PlannedFile { Output = p, Content = content });
        }

        // 4. 设计系统文件（可选）：manifest designSystem=true 时释放 design/design-system.yaml
        if (arch.DesignSystem)
        {
            var content = ReadResourceByPath($"arch/{arch.Id}/design-system.yaml");
            if (content is null)
                throw new InvalidOperationException($"缺失片段：arch/{arch.Id}/design-system.yaml");
            result.Add(new PlannedFile { Output = "design-system.yaml", Content = content });
        }

        return result;
    }

    // 行尾统一为 CRLF：manifest 变量槽用 \n 拼接、AlignVersion 正则替换会吃掉 \r，
    // 而仓库目标文件统一 CRLF；输出前规范化，保证字节级等价。
    private static string NormalizeNewlines(string content) =>
        content.Replace("\r\n", "\n").Replace("\n", "\r\n");

    // ---------- 模板变量填充 ----------

    private static string FillRootTemplate(string template, Manifest manifest, Architecture arch)
    {
        var vars = new Dictionary<string, string>
        {
            ["{{version}}"] = manifest.Vars.Version,
            ["{{last_updated}}"] = manifest.Vars.LastUpdated,
            ["{{target-list}}"] = string.Join("\n", arch.Root.TargetList),
            ["{{subrule-index}}"] = string.Join("\n", arch.Root.SubruleIndex),
            ["{{dev-stage}}"] = string.Join("\n", arch.Root.DevStage),
            ["{{understanding-checklist}}"] = string.Join("\n", arch.Root.UnderstandingChecklist),
            ["{{search-nouns}}"] = arch.Root.SearchNouns,
            ["{{root-scope-nouns}}"] = arch.Root.RootScopeNouns,
            ["{{context-scope-nouns}}"] = arch.Root.ContextScopeNouns,
            ["{{high-risk-nouns}}"] = arch.Root.HighRiskNouns,
            ["{{high-risk-prefix}}"] = arch.Root.HighRiskPrefix,
            ["{{task-kind-nouns}}"] = arch.Root.TaskKindNouns,
            ["{{evidence-nouns}}"] = arch.Root.EvidenceNouns,
            ["{{reuse-noun}}"] = arch.Root.ReuseNoun,
            ["{{persistence-suffix}}"] = arch.Root.PersistenceSuffix,
            ["{{destructive-nouns}}"] = arch.Root.DestructiveNouns,
            ["{{contract-error-line}}"] = arch.Root.ContractErrorLine,
            ["{{reading-order}}"] = arch.Root.ReadingOrder.Count > 0
                ? string.Join("\n", arch.Root.ReadingOrder)
                : "先读本文件确认全局规则与任务阶段 → 按任务类型命中对应子规则 → 再读相关模块与现有实现，确认复用点后动手。",
            ["{{intro-quote}}"] = arch.Root.IntroQuote,
            ["{{checklist-infra}}"] = arch.Root.ChecklistInfra,
            ["{{model-behavior-extra}}"] = arch.Root.ModelBehaviorExtra,
            ["{{verify-nouns}}"] = arch.Root.VerifyNouns,
            ["{{verify-quote}}"] = arch.Root.VerifyQuote,
            ["{{rollback-scope}}"] = arch.Root.RollbackScope,
            ["{{boundary-nouns}}"] = arch.Root.BoundaryNouns,
            // 可选槽：manifest 未提供时整段移除（连同前导换行），避免留空段落
            ["{{extra-sections}}"] = arch.Root.ExtraSections.Count > 0 ? string.Join("\n\n---\n\n", arch.Root.ExtraSections) : "",
        };

        var result = template;
        foreach (var kv in vars)
            result = result.Replace(kv.Key, kv.Value);

        // 可选槽（extra-sections / intro-quote / model-behavior-extra / verify-quote）为空时
        // 会留下空行，3+ 连续换行一律收敛为一个空行（兼容 CRLF/LF）
        result = Regex.Replace(result, @"(?:\r?\n){3,}", "\n\n");
        return result;

        return result;
    }

    // ---------- 版本对齐 ----------

    private static List<string> LeftoverSlots(string content)
    {
        var slots = new List<string>();
        var matches = Regex.Matches(content, @"\{\{[^}]+\}\}");
        foreach (Match m in matches)
            if (!slots.Contains(m.Value)) slots.Add(m.Value);
        return slots;
    }

    private static string AlignVersion(string content, Manifest manifest)
    {
        // 只替换 frontmatter（文件顶部首块 --- 之间）的 version / last_updated，
        // 避免误改正文里形如 `version: 1.0.0` 的行（如数据库迁移规则示例）。
        content = ReplaceFirstInFrontmatter(content, "version", manifest.Vars.Version);
        content = ReplaceFirstInFrontmatter(content, "last_updated", manifest.Vars.LastUpdated);
        return content;
    }

    private static string ReplaceFirstInFrontmatter(string content, string key, string value)
    {
        // frontmatter：以 --- 起始，到第二个 --- 结束。仅在该区间内替换首个 `key: ...` 行。
        if (!content.StartsWith("---", StringComparison.Ordinal))
            return content;
        var firstFenceEnd = content.IndexOf("\n---", 3, StringComparison.Ordinal);
        if (firstFenceEnd < 0) return content;
        var header = content.Substring(0, firstFenceEnd);
        var pattern = new Regex($"^{key}:\\s*.+$", RegexOptions.Multiline);
        var replaced = pattern.Replace(header, $"{key}: {value}", 1);
        return replaced + content.Substring(firstFenceEnd);
    }

    // ---------- 写出（带逐文件覆盖确认）----------

    // 返回 true=全部覆盖完成，false=全部跳过，null=用户取消（已处理零文件时不进此分支）
    private static bool? WriteFilesInteractive(List<PlannedFile> files)
    {
        bool all = false;
        var cwd = Environment.CurrentDirectory;
        // 若无任何已存在文件，先做一次整体确认
        var anyExisting = files.Any(f => File.Exists(Path.Combine(cwd, f.Output)));
        if (!anyExisting)
        {
            Console.Write("确认释放？(Y/n)：");
            if ((Console.ReadLine()?.Trim().ToLowerInvariant() ?? "y") == "n")
                return null;
        }

        foreach (var f in files)
        {
            var path = Path.Combine(cwd, f.Output);
            var exists = File.Exists(path);
            if (exists && !all)
            {
                Console.Write($"`{f.Output}` 已存在，覆盖？(y/N/all)：");
                var reply = Console.ReadLine()?.Trim().ToLowerInvariant() ?? "n";
                if (reply == "all") { all = true; }
                else if (reply != "y")
                {
                    Console.WriteLine($"  跳过 {f.Output}");
                    continue;
                }
            }
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(path, f.Content, new UTF8Encoding(false));
            Console.WriteLine($"  写入 {f.Output}");
        }
        return true;
    }

    // ---------- 嵌入资源读取 ----------

    private static string? ReadResourceByPath(string relativePath)
    {
        // 嵌入资源逻辑名：Fragments.<相对路径>（路径分隔符保持原样，/ 与 \ 均可能出现）
        // 统一把传入路径的分隔符规范化为资源名里实际用的形式，再做大小写不敏感匹配
        var asm = Assembly.GetExecutingAssembly();
        var names = asm.GetManifestResourceNames();
        var wanted = "Fragments." + relativePath.Replace('\\', '/');
        string? match = names.FirstOrDefault(n => string.Equals(n, wanted, StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            // 回退：按后缀匹配（容忍目录分隔差异）
            var suffix = relativePath.Replace('\\', '/');
            match = names.FirstOrDefault(n => n.Replace('\\', '/').EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
        }
        if (match is null) return null;
        using var stream = asm.GetManifestResourceStream(match);
        if (stream is null) return null;
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
