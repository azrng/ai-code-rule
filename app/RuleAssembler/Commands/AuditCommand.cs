using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RuleAssembler;

// --audit 的单项审计发现：规则 / 检测 / 证据 / 受影响组合 / 建议动作五元组，
// 与报告表格一一对应；Severity 只用于控制台摘要排序，不做门禁
internal sealed record AuditFinding(
    string Rule,
    string Check,
    string Evidence,
    string Affected,
    string Suggestion,
    string Severity = "info");

// 组合级体积指标：上下文用量按“最坏路径文件闭包”建模——加载了哪些文件是确定事实，
// AI 实际阅读多少无法在引擎层验证，报告口径固定为字节
internal sealed record ComboSizeMetric(
    string CaseId,
    int RootBytes,
    int FixedSectionBytes,
    int SubruleCount,
    int SubruleBytes,
    int ContextClosureBytes,
    int DuplicateLineCount);

internal sealed record AuditReport(
    string Version,
    string GeneratedAt,
    int CaseCount,
    List<AuditFinding> Findings,
    List<ComboSizeMetric> SizeMetrics,
    List<string> Notes);

// --audit：维护者体检命令，只读规则源、只写报告文件（rule-audit-report.md / .json），
// 发现不构成失败（退出码 0）；与 --check 的守门语义互补——check 错即拦，audit 只报告。
// 仅在规则源仓库（或 --pack 指定内容）内有意义：审计对象是 fragments 全量片段与 37 个兼容样例
internal static class AuditCommand
{
    private const string MarkdownReportName = "rule-audit-report.md";
    private const string JsonReportName = "rule-audit-report.json";

    // 与守卫测试 RootTemplate_FixedBehaviorSections 同口径的固定行为规则段
    private static readonly (string Start, string? End)[] RootFixedSections =
    {
        ("## 根文件原则", "## 子规则索引"),
        ("## 全局工作规则", "## 编码硬约束"),
        ("## 编码硬约束", "## 触发式协作规则"),
        ("## 触发式协作规则", null)
    };

    // 跨文件重复检测的内容行过滤：front matter、标题、表格、代码块、列表标记、短行、
    // 目录树结构行（├──/└──/│ 切片是各子规则展示自己视角的设计形态）与指针行不参与比对
    private static bool IsComparableContentLine(string normalized)
    {
        if (normalized.Length < 12)
            return false;
        // 目录树切片行（含 ├──/└──/│ 或「src/<名>/」形态）是各子规则展示自己视角的设计，不参与比对
        if (normalized.StartsWithAny("---", "```", "|", "#", ">", "- ", "* ", "├", "└", "│", "src/appname/", "project-root/"))
            return false;
        if (normalized.EndsWith("/") && !normalized.Contains(' '))
            return false;

        return true;
    }

    // 冲突候选关键词对：同一产物文件内同时出现正反语义时列为候选，人工裁决
    private static readonly (string Positive, string Negative)[] ConflictPairs =
    {
        ("必须", "禁止"),
        ("优先", "不得"),
        ("必须", "不得"),
        ("一律", "例外"),
    };

    internal static int Run(Manifest manifest, ResourceStore resources, SkillsStore skillsStore)
    {
        var renderer = new RuleRenderer(resources);
        var planner = new CompositionPlanner(manifest);

        // 渲染全部兼容样例：后续检测都以产物为准（含 include 展开后的最终形态）
        var rendered = new List<(CompatibilityCase Case, List<PlannedFile> Files)>();
        foreach (var caseItem in manifest.CompatibilityCases)
        {
            var plan = planner.Plan(new Selection
            {
                Dimensions = new Dictionary<string, string>(caseItem.Selection, StringComparer.Ordinal)
            });
            rendered.Add((caseItem, renderer.Plan(manifest, plan, FeatureSelection.Defaults(manifest, plan))));
        }

        var findings = new List<AuditFinding>();
        findings.AddRange(FindUnreferencedFragments(manifest, resources, rendered));
        findings.AddRange(FindDuplicateCandidates(rendered));
        findings.AddRange(FindConflictCandidates(rendered));
        findings.AddRange(FindSkillReferenceIssues(skillsStore));
        var sizeMetrics = MeasureSizes(rendered);

        var notes = new List<string>
        {
            "上下文闭包 = 根 AGENTS.md + 产物内显式引用到的子规则文件（不含指针文件）总字节；AI 实际阅读行为不在引擎层建模。",
            "文档漂移由 EmitBaselineTests 基线守卫覆盖（214 条目 SHA-256），审计不重复校验，仅提示当前基线状态以 emit-baseline.json 是否存在为准。",
            $"体积基准：固定行为规则段与守卫测试 RootTemplate_FixedBehaviorSections 同口径；根文件中位数 {Median(sizeMetrics.Select(m => m.RootBytes))} 字节。"
        };

        var report = new AuditReport(
            manifest.Vars.Version,
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            rendered.Count,
            findings,
            sizeMetrics,
            notes);

        WriteMarkdownReport(report, MarkdownReportName);
        WriteJsonReport(report, JsonReportName);

        var high = findings.Count(f => f.Severity == "high");
        Console.WriteLine($"审计完成：{manifest.CompatibilityCases.Count} 个兼容样例，{findings.Count} 项发现（high {high} / low {findings.Count - high}）。");
        Console.WriteLine($"报告已写入 {Path.GetFullPath(MarkdownReportName)} 与 {Path.GetFullPath(JsonReportName)}");
        Console.WriteLine("审计只报告不拦截：逐项人工确认后按建议动作处理（保留 / 合并 / 下沉 / 废弃）。");
        return 0;
    }

    // 不可达片段：fragments/ 下全部 .md 对照「manifest 引用（rootSource / files.sources / pointers）+
    // include 传递闭包」，任何组合都到不了的片段列候选；结构性文件（manifest.json 自身、根骨架被引用）天然可达
    private static List<AuditFinding> FindUnreferencedFragments(
        Manifest manifest, ResourceStore resources,
        List<(CompatibilityCase Case, List<PlannedFile> Files)> rendered)
    {
        var referenced = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // manifest 直接声明的片段源
        foreach (var module in manifest.Modules)
        {
            AddIfExists(referenced, resources, module.RootSource);
            foreach (var file in module.Files)
                foreach (var source in file.GetSources())
                    AddIfExists(referenced, resources, source);
            foreach (var pointer in module.Pointers)
                AddIfExists(referenced, resources, $"pointers/{pointer}");
        }
        foreach (var component in manifest.Components)
        {
            foreach (var file in component.Files)
                foreach (var source in file.GetSources())
                    AddIfExists(referenced, resources, source);
            AddIfExists(referenced, resources, component.DesignSystemSource);
        }

        // include 传递闭包：从已引用片段出发展开全部 include 目标
        var queue = new List<string>(referenced);
        while (queue.Count > 0)
        {
            var next = new List<string>();
            foreach (var path in queue)
            {
                var content = resources.Read(path);
                if (content is null)
                    continue;
                foreach (var includePath in ExtractIncludes(content))
                    if (referenced.Add(includePath))
                        next.Add(includePath);
            }

            queue = next;
        }

        var fragmentNames = resources.ReadAllFragmentNames();
        return fragmentNames
            .Where(name => !referenced.Contains(name))
            .Select(name => new AuditFinding(
                name,
                "不可达片段",
                "未被任何 manifest 声明或 include 引用，全部有效组合不可达",
                "全部组合",
                "确认是否保留兼容用途；无兼容用途则删除",
                "high"))
            .ToList();
    }

    private static void AddIfExists(HashSet<string> referenced, ResourceStore resources, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;
        if (resources.Read(path) is not null)
            referenced.Add(path.Replace('\\', '/'));
    }

    private static readonly Regex IncludeOnlyPattern = new(
        @"^[ \t]*<!--\s*include:\s*(?<path>[^>\r\n]+?)\s*-->[ \t]*\r?$",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private static IEnumerable<string> ExtractIncludes(string content)
    {
        foreach (Match match in IncludeOnlyPattern.Matches(content))
            yield return match.Groups["path"].Value.Trim().Replace('\\', '/');
    }

    // 重复候选：同一组合的产物文件之间，归一化内容行（≥12 字符、非结构行）逐行比对，
    // 在 ≥2 个不同文件出现的同一行报候选——这是“同一条款在一条执行路径中被重复加载”的直接证据
    private static List<AuditFinding> FindDuplicateCandidates(
        List<(CompatibilityCase Case, List<PlannedFile> Files)> rendered)
    {
        var findings = new List<AuditFinding>();

        // 跨组合聚合：同一行出现在多个组合时只报一次，受影响组合列出前若干个
        var lineOwners = new Dictionary<string, List<(string CaseId, string Output)>>(StringComparer.Ordinal);
        foreach (var (caseItem, files) in rendered)
        {
            // 单组合内先去重：同一行在本组合 N 个不同文件出现才算跨文件重复
            var perCombo = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            // 只比子规则文件：根 / collaboration / 指针（CLAUDE/GEMINI/GROK）是逐入口复制的设计形态，不参与重复判定
            var subruleFiles = files.Where(f => f.Output.EndsWith("-AGENTS.md", StringComparison.OrdinalIgnoreCase)
                && !f.Output.Equals("collaboration-AGENTS.md", StringComparison.OrdinalIgnoreCase))
                .ToList();
            foreach (var file in subruleFiles)
            {
                foreach (var line in StripFrontMatter(file.Content).Split('\n'))
                {
                    var normalized = line.Trim().Replace(" ", "");
                    if (!IsComparableContentLine(normalized))
                        continue;
                    if (!perCombo.TryGetValue(normalized, out var owners))
                    {
                        owners = [];
                        perCombo[normalized] = owners;
                    }

                    owners.Add(file.Output);
                }
            }

            foreach (var (line, owners) in perCombo.Where(kv => kv.Value.Count >= 2))
            {
                if (!lineOwners.TryGetValue(line, out var comboList))
                {
                    comboList = [];
                    lineOwners[line] = comboList;
                }

                comboList.Add((caseItem.Profile, string.Join("、", owners.Order())));
            }
        }

        foreach (var (line, combos) in lineOwners)
        {
            // 纯事实短句（如目录树行）在多组合出现属正常骨架，只报内容行
            var sample = combos[0].CaseId;
            var files = combos[0].Output;
            findings.Add(new AuditFinding(
                TruncateForCell(line),
                "重复候选",
                $"「{TruncateForCell(line)}」在 {combos.Count} 个组合的同一组产物文件重复出现（{files}）",
                combos.Count <= 5
                    ? string.Join("、", combos.Select(c => c.CaseId))
                    : $"{combos.Count} 个组合",
                "检查唯一权威来源，收敛到单一片段后 include 复用",
                combos.Count >= 10 ? "high" : "low"));
        }

        return findings;
    }

    // 冲突候选：同一产物文件内、围绕同一主题词在不同行分别出现正反句式（「必须 X」与「禁止 X / X 例外」），
    // 才列为候选；同一行的正反共现（「必须 X，不得 Y」）是常见互补句式，不报。
    // 主题词取正句式的核心动词短语（必须 / 优先 / 一律 + 后续 2~6 字），跨行同词反句式即命中
    private static List<AuditFinding> FindConflictCandidates(
        List<(CompatibilityCase Case, List<PlannedFile> Files)> rendered)
    {
        var findings = new List<AuditFinding>();
        var reported = new HashSet<string>(StringComparer.Ordinal);

        foreach (var (caseItem, files) in rendered)
        {
            foreach (var file in files.Where(f => f.Output.EndsWith("-AGENTS.md", StringComparison.OrdinalIgnoreCase)
                    && !f.Output.Equals("collaboration-AGENTS.md", StringComparison.OrdinalIgnoreCase)))
            {
                // 主题词 → 正反句式证据行（去重后保留首个）
                var topics = new Dictionary<string, (string? Positive, string? Negative)>(StringComparer.Ordinal);
                foreach (var rawLine in file.Content.Split('\n'))
                {
                    var line = rawLine.Trim();
                    if (line.Length == 0 || line.StartsWithAny("|", "#", "```"))
                        continue;

                    foreach (var (positive, negative) in ConflictPairs)
                    {
                        // 同一行正反共现 = 互补句式，跳过
                        if (line.Contains(positive) && line.Contains(negative))
                            continue;

                        var positiveAt = line.IndexOf(positive, StringComparison.Ordinal);
                        if (positiveAt >= 0)
                        {
                            // 主题词：正句式标记后的 2~6 个非标点字符（粗粒度锚点，供跨行比对）
                            var topic = ExtractTopic(line, positiveAt + positive.Length);
                            if (topic is not null)
                            {
                                var entry = topics.TryGetValue(topic, out var t) ? t : (null, null);
                                entry.Positive ??= line;
                                topics[topic] = entry;
                            }
                        }

                        var negativeAt = line.IndexOf(negative, StringComparison.Ordinal);
                        if (negativeAt >= 0)
                        {
                            // 反句式的主题词取标记之前的 2~6 字（「禁止 X」词在 X 前）
                            var topic = ExtractTopicBefore(line, negativeAt);
                            if (topic is not null)
                            {
                                var entry = topics.TryGetValue(topic, out var t) ? t : (null, null);
                                entry.Negative ??= line;
                                topics[topic] = entry;
                            }
                        }
                    }
                }

                foreach (var (topic, (positiveLine, negativeLine)) in topics)
                {
                    if (positiveLine is null || negativeLine is null || positiveLine == negativeLine)
                        continue;
                    var key = caseItem.Profile + "/" + file.Output + "|" + topic;
                    if (!reported.Add(key))
                        continue;
                    findings.Add(new AuditFinding(
                        $"{caseItem.Profile}/{file.Output}",
                        "冲突候选",
                        $"主题「{topic}」正句式：{TruncateForCell(positiveLine)}；反句式：{TruncateForCell(negativeLine)}",
                        caseItem.Profile,
                        "人工裁决是否构成真实冲突（互补条款 / 例外声明不算）",
                        "low"));
                }
            }
        }

        return findings;
    }

    // 正句式主题词：从标记之后取 2~6 个连续中文 / 字母数字字符
    private static string? ExtractTopic(string line, int start)
    {
        var sb = new StringBuilder();
        for (var i = start; i < line.Length && sb.Length < 6; i++)
        {
            var ch = line[i];
            if (char.IsLetterOrDigit(ch))
                sb.Append(ch);
            else if (sb.Length >= 2)
                break;
        }

        return sb.Length >= 2 ? sb.ToString() : null;
    }

    // 反句式主题词：从标记之前回取 2~6 个连续中文 / 字母数字字符
    private static string? ExtractTopicBefore(string line, int end)
    {
        var sb = new StringBuilder();
        for (var i = end - 1; i >= 0 && sb.Length < 6; i--)
        {
            var ch = line[i];
            if (char.IsLetterOrDigit(ch))
                sb.Append(ch);
            else if (sb.Length >= 2)
                break;
        }

        return sb.Length >= 2 ? new string(sb.ToString().Reverse().ToArray()) : null;
    }

    // Skill 引用完整性：SKILL.md 引用的本地相对路径（markdown 链接）必须存在；
    // 代码围栏内的链接是「怎么写技能」的示例文本而非真实引用，跳过——skill-creator 的
    // Pattern 示例引用 FORMS.md / DOCX-JS.md 等假想文件，曾因此被整批误报为缺失
    private static List<AuditFinding> FindSkillReferenceIssues(SkillsStore skillsStore)
    {
        var findings = new List<AuditFinding>();
        foreach (var skill in skillsStore.Discover())
        {
            var files = skillsStore.ReadFiles(skill);
            var skillMd = files
                .FirstOrDefault(f => f.RelativePath.Equals("SKILL.md", StringComparison.OrdinalIgnoreCase));
            if (skillMd is null)
                continue;

            var skillContent = System.Text.Encoding.UTF8.GetString(skillMd.Content);
            var skillFiles = files
                .Select(f => f.RelativePath)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            findings.AddRange(FindSkillReferenceIssuesIn(skill.Id, skillContent, skillFiles));
        }

        return findings;
    }

    // 单技能引用缺失检测：围栏外的本地 markdown 链接目标必须存在于技能文件集
    internal static List<AuditFinding> FindSkillReferenceIssuesIn(
        string skillId, string skillContent, HashSet<string> skillFiles)
    {
        var findings = new List<AuditFinding>();
        var fences = MarkdownFences.FindSpans(skillContent);
        foreach (Match match in Regex.Matches(skillContent, @"\[[^\]]*\]\(([^)]+)"))
        {
            if (MarkdownFences.IsInside(fences, match.Index))
                continue;

            var target = match.Groups[1].Value.Trim();
            if (target.StartsWithAny("http://", "https://", "#", "mailto:"))
                continue;
            var path = target.Split('#')[0];
            if (path.Length == 0)
                continue;
            if (!skillFiles.Contains(path.Replace('\\', '/')))
                findings.Add(new AuditFinding(
                    $"skills/{skillId}/SKILL.md",
                    "Skill 引用缺失",
                    $"引用的本地文件不存在：{path}",
                    skillId,
                    "检查替代来源或修正路径；确属过时技能列入技能变更批次",
                    "low"));
        }

        return findings;
    }

    // 体积与上下文闭包：逐组合统计根文件、固定段、子规则与“显式引用闭包”字节
    private static List<ComboSizeMetric> MeasureSizes(
        List<(CompatibilityCase Case, List<PlannedFile> Files)> rendered)
    {
        var metrics = new List<ComboSizeMetric>();
        foreach (var (caseItem, files) in rendered)
        {
            var root = files.FirstOrDefault(f => f.Output.Equals("AGENTS.md", StringComparison.OrdinalIgnoreCase));
            var subrules = files
                .Where(f => f.Output.EndsWith("-AGENTS.md", StringComparison.OrdinalIgnoreCase))
                .ToList();
            var rootBytes = root is null ? 0 : Encoding.UTF8.GetByteCount(root.Content);
            var fixedBytes = root is null ? 0 : RootFixedSections.Sum(section =>
            {
                var body = ExtractSection(root.Content, section.Start, section.End);
                return body is null ? 0 : Encoding.UTF8.GetByteCount(body);
            });

            // 上下文闭包：根文件 + 根文件正文显式提到的子规则（不含 CLAUDE/GEMINI 等指针）
            var closureFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (root is not null)
            {
                foreach (var sub in subrules)
                    if (root.Content.Contains(sub.Output, StringComparison.Ordinal))
                        closureFiles.Add(sub.Output);
            }

            var closureBytes = rootBytes + subrules
                .Where(s => closureFiles.Contains(s.Output))
                .Sum(s => Encoding.UTF8.GetByteCount(s.Content));

            // 跨文件重复行数（复用重复检测的归一化口径）
            var seen = new Dictionary<string, string>(StringComparer.Ordinal);
            var duplicateLines = 0;
            foreach (var file in files.Where(f => f.Output.EndsWith(".md", StringComparison.OrdinalIgnoreCase)))
                foreach (var line in file.Content.Split('\n'))
                {
                    var normalized = line.Trim().Replace(" ", "");
                    if (!IsComparableContentLine(normalized))
                        continue;
                    if (seen.TryGetValue(normalized, out var first) && first != file.Output)
                        duplicateLines++;
                    else
                        seen[normalized] = file.Output;
                }

            metrics.Add(new ComboSizeMetric(
                caseItem.Profile, rootBytes, fixedBytes,
                subrules.Count,
                subrules.Sum(s => Encoding.UTF8.GetByteCount(s.Content)),
                closureBytes, duplicateLines));
        }

        return metrics;
    }

    private static string? ExtractSection(string content, string startHeading, string? endHeading)
    {
        var start = content.IndexOf(startHeading, StringComparison.Ordinal);
        if (start < 0)
            return null;
        var bodyStart = start + startHeading.Length;
        var end = endHeading is null ? content.Length : content.IndexOf(endHeading, bodyStart, StringComparison.Ordinal);
        return end < bodyStart ? null : content[bodyStart..end];
    }

    private static void WriteMarkdownReport(AuditReport report, string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# 规则审计报告（{report.Version}）");
        sb.AppendLine();
        sb.AppendLine($"生成时间：{report.GeneratedAt} · 样例数：{report.CaseCount} · 发现：{report.Findings.Count} 项");
        sb.AppendLine();
        sb.AppendLine("## 审计说明");
        foreach (var note in report.Notes)
            sb.AppendLine($"- {note}");
        sb.AppendLine();
        sb.AppendLine("## 发现清单");
        sb.AppendLine();
        sb.AppendLine("| 规则 | 检测 | 证据 | 受影响组合 | 建议动作 |");
        sb.AppendLine("| --- | --- | --- | --- | --- |");
        foreach (var f in report.Findings.OrderByDescending(f => f.Severity == "high").ThenBy(f => f.Check).ThenBy(f => f.Rule))
            sb.AppendLine($"| {f.Rule} | {f.Check} | {f.Evidence} | {f.Affected} | {f.Suggestion} |");
        sb.AppendLine();
        sb.AppendLine("## 上下文体积指标（字节）");
        sb.AppendLine();
        sb.AppendLine("| 组合 | 根文件 | 固定规则段 | 子规则数 | 子规则合计 | 上下文闭包 | 跨文件重复行 |");
        sb.AppendLine("| --- | --- | --- | --- | --- | --- | --- |");
        foreach (var m in report.SizeMetrics.OrderBy(m => m.RootBytes))
            sb.AppendLine($"| {m.CaseId} | {m.RootBytes} | {m.FixedSectionBytes} | {m.SubruleCount} | {m.SubruleBytes} | {m.ContextClosureBytes} | {m.DuplicateLineCount} |");
        sb.AppendLine();
        sb.AppendLine("## 预算基线（供 CI 对比）");
        sb.AppendLine();
        var rootMedian = Median(report.SizeMetrics.Select(m => m.RootBytes));
        var fixedMedian = Median(report.SizeMetrics.Select(m => m.FixedSectionBytes));
        sb.AppendLine($"- 根文件中位数：{rootMedian} 字节；固定规则段中位数：{fixedMedian} 字节");
        sb.AppendLine("- CI 建议：根固定段相对上次审计增长超过 10% 时提示审查（warning，不拦截）");

        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
    }

    private static void WriteJsonReport(AuditReport report, string path)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        File.WriteAllText(path, JsonSerializer.Serialize(report, options), new UTF8Encoding(false));
    }

    private static int Median(IEnumerable<int> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        return sorted.Count == 0 ? 0 : sorted[sorted.Count / 2];
    }

    private static string TruncateForCell(string text)
    {
        var single = text.Replace("|", "\\|").Replace("\n", " ");
        return single.Length <= 60 ? single : single[..60] + "…";
    }

    // 去掉 YAML front matter（--- 起始块）：版本号等元数据行在全部产物文件都出现，不构成内容重复
    private static string StripFrontMatter(string content)
    {
        if (!content.StartsWith("---", StringComparison.Ordinal))
            return content;
        var end = content.IndexOf("\n---", 4, StringComparison.Ordinal);
        return end < 0 ? content : content[(end + 4)..];
    }

    // 测试探针：暴露内部检测器供回归测试断言（生产路径只走 Run）
    internal static List<AuditFinding> FindUnreferencedFragmentsForTest(
        Manifest manifest, ResourceStore resources,
        List<(CompatibilityCase Case, List<PlannedFile> Files)> rendered)
        => FindUnreferencedFragments(manifest, resources, rendered);

    internal static List<AuditFinding> FindDuplicateCandidatesForTest(
        List<(CompatibilityCase Case, List<PlannedFile> Files)> rendered)
        => FindDuplicateCandidates(rendered);

    internal static List<AuditFinding> FindSkillReferenceIssuesForTest(SkillsStore skillsStore)
        => FindSkillReferenceIssues(skillsStore);

    internal static List<ComboSizeMetric> MeasureSizesForTest(
        List<(CompatibilityCase Case, List<PlannedFile> Files)> rendered)
        => MeasureSizes(rendered);
}

internal static class AuditStringExtensions
{
    internal static bool StartsWithAny(this string value, params string[] prefixes)
    {
        foreach (var prefix in prefixes)
            if (value.StartsWith(prefix, StringComparison.Ordinal))
                return true;
        return false;
    }
}

// ResourceStore 辅助：枚举 fragments/ 前缀的全部 .md 片段名（两种来源统一口径）
internal static class ResourceStoreFragmentExtensions
{
    internal static List<string> ReadAllFragmentNames(this ResourceStore store)
    {
        return store.ResourceNames
            .Where(name => name.StartsWith("fragments/", StringComparison.OrdinalIgnoreCase)
                && name.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            .Select(name => name["fragments/".Length..])
            .ToList();
    }
}
