using System.Reflection;
using System.Text;
using Spectre.Console;

namespace RuleAssembler;

internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            // 编码设置纳入兜底：个别宿主没有标准输入句柄时 setter 会抛，裸崩不如走友好错误处理
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            // 引擎自更新残留清理：上一次「.old + 新 exe 落位」留下的旧版本文件，
            // 运行中的新 exe 不锁它，正常都能删掉；删不掉（仍被占用）静默跳过下次再试
            if (Environment.ProcessPath is { } currentExe)
                PackResolver.CleanupEngineUpdateResidue(currentExe);


            // 内容解析只决定来源（--pack 显式指定 / exe 内嵌兜底），绝不产生网络副作用；
            // 更新检查在交互模式启动时统一做一次
            var packArgs = args;
            var resolution = PackResolver.Resolve(packArgs);
            foreach (var line in resolution.Logs)
                Console.WriteLine(line);

            var resources = new ResourceStore(resolution.Provider);
            var manifest = new ManifestLoader(resources).Load();
            var skillsStore = new SkillsStore(resolution.Provider);
            var engineVersion = ResolveEngineVersion();

            // 单一内容源：内容随 exe 内嵌、与 exe 同版本构建发布（--pack 显式指定除外），
            // 不存在「引擎已换、内容未更」的部分生效状态，横幅恒显单一版本
            Console.WriteLine($"use-ai-rule {engineVersion}（{manifest.Vars.LastUpdated}）");

            // N1 启动自登记：把当前 exe 字节 + 哈希登进本机缓存（命令模式同样登记——
            // script-only 用户的目录副本也该成为同机更新源；first-wins，常态目录已完整零拷贝）
            EngineCache.RegisterCurrentEngine(engineVersion);

            // 带参模式走命令行分发（--check / --audit / --emit / --plan / --detect / --upgrade / --recipe 等），
            // 不做交互与更新检查，避免脚本调用出网络副作用
            if (StripPackArgs(ref packArgs))
                return RunCommand(packArgs, manifest, resources, skillsStore);

            // 交互路径先关 conhost QuickEdit：后续更新确认与全部菜单都是键盘链路，
            // 鼠标误点进入文本选择会挂起整个控制台输入，菜单表现为卡死
            QuickEditGuard.Disable();

            // 启动统一更新检查（仅交互模式；带参命令与 --pack 绝不联网）；
            // --offline 跳过在线拉取但仍走本机缓存回退（离线升级刚需）；
            // 安装成功即退出提示重跑，不落入后续交互
            if (resolution.Source != "pack"
                && !RunStartupUpdateCheck(engineVersion, offline: args.Contains("--offline", StringComparer.OrdinalIgnoreCase)))
                return 0;

            return InteractiveSession.Run(manifest, resources, skillsStore);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"{(ex is ArgumentException ? "参数错误" : "错误")}：{ex.Message}");
            if (ex.InnerException is not null)
                Console.Error.WriteLine($"详细信息：{ex.InnerException.Message}");

            if (!Console.IsInputRedirected)
            {
                Console.WriteLine("按 Enter 键退出。");
                Console.ReadLine();
            }

            // 退出码口径：0 成功 / 1 一般错误 / 2 参数错误，脚本可按码区分用法问题与执行失败
            return ex is ArgumentException ? 2 : 1;
        }
    }

    // 统一版本：exe 的 InformationalVersion 与内嵌内容同源（manifest.vars.version）；
    // 本地开发构建未注入时显示 dev（dev 不比任何远端版本新，不触发自更新）；
    // USE_AI_RULE_ENGINE_VERSION 仅供本地调试覆盖显示
    private static string ResolveEngineVersion()
    {
        var engineVersion = Environment.GetEnvironmentVariable("USE_AI_RULE_ENGINE_VERSION");
        if (string.IsNullOrWhiteSpace(engineVersion))
        {
            var informational = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            engineVersion = string.IsNullOrWhiteSpace(informational) ? "dev" : informational;
        }

        return engineVersion;
    }

    // 启动更新检查（仅交互模式）：在线查 latest.json（--offline 跳过），失败回退本机缓存登记的更新；
    // 弹一次确认（默认更新），安装成功即退出提示重跑——分离进程会与终端抢同一控制台输入，
    // 故不自动拉起；带旧内容继续交互会回写降级配方，绝不落入原流程。
    // 返回 false 表示已安装新版本，主流程应直接退出；stdin 重定向只报告不更新
    private static bool RunStartupUpdateCheck(string engineVersion, bool offline)
    {
        // 检查包一层转圈：latest.json 拉取最多阻塞 3 秒、本机缓存回退要哈希大文件，
        // 无反馈时启动看起来像卡死；降级说明（[update] 更新检查超时…）在动画收尾后输出，纯文本场景不插帧
        IConsoleUi startupUi = CreateUi();
        var updateLogs = new List<string>();

        PackResolver.UpdateOffer? offer;
        if (offline)
        {
            // --offline：本机缓存是唯一更新源（离线升级刚需）
            offer = startupUi.WithProgress("检查本机缓存更新…", () => EngineCache.TryFetchLocalOffer(engineVersion));
            if (offer is not null)
                Console.WriteLine($"[update] --offline 已跳过在线检查，检测到更新：{offer.Describe()}");
        }
        else
        {
            offer = startupUi.WithProgress(
                "检查更新中…",
                () => PackResolver.TryFetchUpdateOffer(engineVersion, updateLogs));
            foreach (var line in updateLogs)
                Console.WriteLine(line);

            // 在线检查失败（无网络 / 超时 / 清单不可用，统一表现为 null）→ 回退本机缓存；
            // 在线成功且已最新不查本地——远端清单是事实源，本地不会有远端不知道的版本
            if (offer is null)
            {
                offer = startupUi.WithProgress("在线检查失败，查找本机缓存更新…", () => EngineCache.TryFetchLocalOffer(engineVersion));
                if (offer is not null)
                    Console.WriteLine($"[update] 在线检查失败，本机缓存有可用更新：{offer.Describe()}");
            }
        }

        if (offer is not { HasAnyUpdate: true })
            return true;

        if (Console.IsInputRedirected)
        {
            Console.WriteLine($"[update] 检测到更新：{offer.Describe()}（非交互输入，跳过更新）");
            return true;
        }

        // 确认走统一 UI 抽象：非 ANSI 终端（CreateUi 已回退纯文本）不再被 Spectre 直连渲染叠屏
        var installed = PackResolver.ApplyUpdates(
            offer,
            o => startupUi.Confirm(
                $"发现更新：{o.Describe()}，是否更新？",
                defaultValue: true));
        if (installed)
        {
            Console.WriteLine("[update] 更新完成，本次退出。请重新运行 use-ai-rule 使新程序与新内容生效。");
            return false;
        }

        return true;
    }

    // 剥离内容包开关（--pack <路径> / --offline），返回剩余业务参数；
    // 业务命令不接受这两个开关，避免拼错被静默吞掉
    private static bool StripPackArgs(ref string[] args)
    {
        if (args.Length == 0)
            return false;

        var remaining = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].Equals("--offline", StringComparison.OrdinalIgnoreCase))
                continue;
            if (args[i].Equals("--pack", StringComparison.OrdinalIgnoreCase))
            {
                i++; // 跳过路径参数
                continue;
            }
            remaining.Add(args[i]);
        }

        args = remaining.ToArray();
        return args.Length > 0;
    }

    // 交互实现只在入口处决定一次：stdin 重定向（管道冒烟/CI）强制走纯文本编号输入；
    // 终端不支持 ANSI 光标控制时 SelectionPrompt 无法擦除上一帧（旧版 conhost 会叠屏），同样回退编号输入
    internal static IConsoleUi CreateUi()
    {
        if (Console.IsInputRedirected)
            return new PlainConsoleUi();
        try
        {
            // Ansi != true 的终端（旧版 conhost 等）无法做光标回退重绘，SelectionPrompt 会叠屏
            if (AnsiConsole.Profile.Capabilities.Ansi == true)
                return new SpectreConsoleUi();
        }
        catch (Exception)
        {
            // 能力探测失败时保守回退到纯文本交互
        }

        return new PlainConsoleUi();
    }

    // 命令行分发：只认开关与位置参数，具体执行全部委托给各命令类（每命令一类）
    private static int RunCommand(string[] args, Manifest manifest, ResourceStore resources, SkillsStore skillsStore)
    {
        if (args.Length == 1 && args[0].Equals("--check", StringComparison.OrdinalIgnoreCase))
            return CheckCommand.Run(manifest, resources, skillsStore);

        // 维护者体检：只读规则源，报告不可达 / 重复 / 冲突候选 / Skill 引用与体积指标，不拦截
        if (args.Length is 1 or 2 && args[0].Equals("--audit", StringComparison.OrdinalIgnoreCase))
        {
            if (args.Length == 2 && !args[1].Equals("--offline", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"--audit 不接受目录参数（报告固定写入当前目录）：{args[1]}", nameof(args));
            return AuditCommand.Run(manifest, resources, skillsStore);
        }

        if (args.Length is 1 or 2 && args[0].Equals("--detect", StringComparison.OrdinalIgnoreCase))
        {
            var dir = args.Length == 2 ? args[1] : Environment.CurrentDirectory;
            return DetectCommand.Run(manifest, new PlainConsoleUi(), dir);
        }

        if (args.Length >= 1 && args[0].Equals("--upgrade", StringComparison.OrdinalIgnoreCase))
            return UpgradeCommand.Run(args, manifest, resources, skillsStore);

        if (args.Length is 2 or 3 && args[0].Equals("--recipe", StringComparison.OrdinalIgnoreCase))
            return RecipeCommand.Run(args, manifest, resources, skillsStore);

        if (args.Length == 2 && args[0].Equals("--emit", StringComparison.OrdinalIgnoreCase))
            return EmitCommand.Run(args[1], manifest, resources);

        if (args.Length >= 1 && args[0].Equals("--plan", StringComparison.OrdinalIgnoreCase))
            return PlanCommand.Run(args, manifest, resources);

        if (args.Length == 1 && (args[0].Equals("--help", StringComparison.OrdinalIgnoreCase)
            || args[0].Equals("-h", StringComparison.OrdinalIgnoreCase)))
            return PrintHelp();

        throw new ArgumentException($"不支持的参数：{string.Join(" ", args)}。请直接运行，或使用 --check。", nameof(args));
    }

    private static int PrintHelp()
    {
        Console.WriteLine(InteractiveSession.Banner);
        Console.WriteLine("直接双击或不带参数运行：先选模式——生成规则文件（自动检测推荐 / 问答选择）、检测当前目录组合，或仅导入 Skills；已存在配方记录时优先生成「按配方重放」入口。");
        Console.WriteLine("规则文件落盘后可选择把内置 Skills 导入到 Codex（.agents/skills）/ ZCode（.zcode/skills）/ Claude Code（.claude/skills）/ pi（.pi/skills），追加导入、同名技能可覆盖或跳过；--recipe / --upgrade 按配方同步已记录的 Skills。");
        Console.WriteLine("--check：只校验 manifest、片段、全部兼容样例的输出契约一致性、问答决策树、组件问答决策树与内置技能，不写入文件。");
        Console.WriteLine("--audit [维护者]：审计规则源（不可达 / 重复 / 冲突候选 / Skill 引用 / 上下文体积），报告写入当前目录，不拦截。");
        Console.WriteLine("--detect [目录]：只做项目检测，打印推荐组合；证据不足时打印待确认维度与候选组合，不写入文件。");
        Console.WriteLine("--plan 维度=取值 ...：按显式维度选择解析组合内核，打印命中组件与文件清单，不写入文件。");
        Console.WriteLine("--recipe <配方.json> [目标目录]：按配方记录重放生成（缺省写入当前目录），写入配方与 .gitignore。");
        Console.WriteLine("--upgrade [目录] [--force]：按目标目录的配方记录升级规则；识别本地修改默认保留，--force 全部覆盖。");
        Console.WriteLine("--emit <目录>：把全部兼容样例的组合路径生成结果输出到指定目录，用于批量预览与产物回归。");
        Console.WriteLine("--pack <内容包.zip|目录>：使用指定内容包（zip 或已解包目录），校验失败直接报错退出。");
        Console.WriteLine("--offline：跳过在线更新检查，但仍会检查本机缓存的更新（内容随 exe 内嵌，离线永远可用）。");
        Console.WriteLine("--help：显示帮助。");
        return 0;
    }
}
