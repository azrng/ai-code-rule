using System.Text;
using Spectre.Console;

namespace RuleAssembler;

internal static class Program
{
    private const string Banner = "AI Rule 拼接器 — 把一套完整 AI rule 释放到当前目录";

    private static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        try
        {
            var resources = new ResourceStore();
            var manifest = new ManifestLoader(resources).Load();

            if (args.Length > 0)
                return RunCommand(args, manifest, resources);

            return RunInteractive(manifest, resources);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"错误：{ex.Message}");
            if (ex.InnerException is not null)
                Console.Error.WriteLine($"详细信息：{ex.InnerException.Message}");

            if (!Console.IsInputRedirected)
            {
                Console.WriteLine("按 Enter 键退出。");
                Console.ReadLine();
            }

            return 1;
        }
    }

    // 交互主流程：先检测当前目录给出推荐，确认不了再落到问答决策树 / 经典选择
    private static int RunInteractive(Manifest manifest, ResourceStore resources)
    {
        Console.WriteLine(Banner);
        Console.WriteLine($"当前目录：{Environment.CurrentDirectory}");
        Console.WriteLine();

        // 交互实现只在入口处决定一次：stdin 重定向（管道冒烟/CI）强制走纯文本编号输入；
        // 终端不支持 ANSI 光标控制时 SelectionPrompt 无法擦除上一帧（旧版 conhost 会叠屏），同样回退编号输入
        IConsoleUi ui = CreateUi();
        var renderer = new RuleRenderer(resources);
        var wizard = new ConsoleWizard(ui);

        var selection = OfferEntry(manifest, renderer, wizard, ui);
        if (selection is null)
        {
            Console.WriteLine("已取消。");
            return 0;
        }

        var files = renderer.Plan(manifest, selection.Profile);
        Console.WriteLine();
        Console.WriteLine($"已选择：{selection.Profile.Label}");
        Console.WriteLine("将释放以下文件：");
        foreach (var file in files)
            Console.WriteLine($"  - {file.Output}");
        Console.WriteLine("（不释放 TASK.md / rulelog / devlog）");
        Console.WriteLine();

        // 汇总确认：detected 路径在入口已带文件预览确认过，wizard / classic 在生成前补一次
        if (selection.Mode != "detected")
        {
            var confirm = ui.Choose(
                $"确认在当前目录生成 {files.Count} 个文件？",
                "取消",
                new List<UiChoice> { new("开始生成", "") });
            if (confirm is null)
            {
                Console.WriteLine("已取消。");
                return 0;
            }
        }

        var written = new OutputWriter().WriteFiles(files);

        var recorder = new RecipeRecorder();
        var record = RecipeRecorder.Build(manifest, selection.Profile, selection.Mode, selection.Answers, files);
        recorder.Write(Environment.CurrentDirectory, record);
        var gitignoreMessage = recorder.EnsureGitignored(
            Environment.CurrentDirectory,
            RecipeRecorder.StackKeyFor(selection.Profile));

        Console.WriteLine($"完成：已释放 {written} 个文件到 {Environment.CurrentDirectory}");
        Console.WriteLine($"已写入配方记录 {RecipeRecorder.RecordFileName}（{gitignoreMessage}）");
        return 0;
    }

    private static ProfileSelection? OfferEntry(Manifest manifest, RuleRenderer renderer, ConsoleWizard wizard, IConsoleUi ui)
    {
        var detection = TryDetect(manifest, Environment.CurrentDirectory);
        if (detection is null)
        {
            while (true)
            {
                var choice = ui.Choose(
                    "请选择确定规则组合的方式：",
                    "取消",
                    new List<UiChoice>
                    {
                        new("问答选择（推荐）", "回答项目形态、前后端技术栈等几个问题"),
                        new("经典选择", "技术大类 → Profile")
                    });
                if (choice is null)
                    return null;
                if (choice == 0)
                    return wizard.SelectByWizard(manifest);
                if (choice == 1)
                    return wizard.SelectProfile(manifest);
            }
        }

        ui.Head("已检测当前目录，判断依据：");
        ui.Bullets(detection.Reasons.Take(4));
        ui.Bullets(detection.Notes.Take(2));
        ui.Line("");

        var previewFiles = renderer.Plan(manifest, manifest.Profiles.First(profile => profile.Id == detection.ProfileId));
        ui.Line($"推荐组合：{detection.ProfileLabel}");
        ui.FileTable("将生成的文件", previewFiles.Select(file => file.Output));

        while (true)
        {
            var choice = ui.Choose(
                "请确认：",
                "取消",
                new List<UiChoice>
                {
                    new("使用推荐组合并生成", ""),
                    new("问答选择（重新回答）", ""),
                    new("经典选择", "技术大类 → Profile")
                });
            if (choice is null)
                return null;
            if (choice == 0)
            {
                return new ProfileSelection(
                    manifest.Profiles.First(profile => profile.Id == detection.ProfileId),
                    "detected",
                    detection.Answers);
            }
            if (choice == 1)
                return wizard.SelectByWizard(manifest);
            if (choice == 2)
                return wizard.SelectProfile(manifest);
        }
    }

    private static IConsoleUi CreateUi()
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

    private static DetectionResult? TryDetect(Manifest manifest, string directory)
    {
        try
        {
            return new ProjectDetector(manifest).Detect(directory);
        }
        catch (Exception)
        {
            // 检测失败不阻塞交互，直接落到问答选择
            return null;
        }
    }

    private static int RunCommand(string[] args, Manifest manifest, ResourceStore resources)
    {
        if (args.Length == 1 && args[0].Equals("--check", StringComparison.OrdinalIgnoreCase))
        {
            var renderer = new RuleRenderer(resources);
            var validator = new RootConsistencyValidator();
            var contractErrors = new List<string>();
            foreach (var profile in manifest.Profiles)
            {
                var files = renderer.Plan(manifest, profile);
                var errors = validator.Validate(manifest, renderer, profile);
                if (errors.Count == 0)
                {
                    Console.WriteLine($"通过：{profile.Id}（{files.Count} 个文件）");
                    continue;
                }

                contractErrors.AddRange(errors);
                Console.WriteLine($"失败：{profile.Id}（输出契约不一致）");
            }

            if (contractErrors.Count > 0)
            {
                Console.Error.WriteLine("输出契约校验失败：");
                foreach (var error in contractErrors)
                    Console.Error.WriteLine($"- {error}");
                return 1;
            }

            var wizardErrors = new WizardEngine(manifest).SimulateAll();
            if (wizardErrors.Count > 0)
            {
                Console.Error.WriteLine("决策树校验失败：");
                foreach (var error in wizardErrors)
                    Console.Error.WriteLine($"- {error}");
                return 1;
            }

            Console.WriteLine($"决策树校验通过：全部 Profile 可达且无歧义路径。");
            Console.WriteLine($"校验完成：{manifest.Profiles.Count} 个 Profile 全部通过。");
            return 0;
        }

        if (args.Length is 1 or 2 && args[0].Equals("--detect", StringComparison.OrdinalIgnoreCase))
        {
            var dir = args.Length == 2 ? args[1] : Environment.CurrentDirectory;
            var result = new ProjectDetector(manifest).Detect(dir);
            if (result is null)
            {
                Console.WriteLine($"未能在 {dir} 识别出已接入的技术栈。");
                return 1;
            }

            Console.WriteLine($"判断依据：");
            foreach (var reason in result.Reasons)
                Console.WriteLine($"  - {reason}");
            foreach (var note in result.Notes)
                Console.WriteLine($"  - {note}");
            Console.WriteLine($"推荐组合：{result.ProfileLabel}（{result.ProfileId}）");
            Console.WriteLine("回答组合：" + string.Join("，", result.Answers.Select(pair => $"{pair.Key}={pair.Value}")));
            return 0;
        }

        if (args.Length == 2 && args[0].Equals("--emit", StringComparison.OrdinalIgnoreCase))
        {
            var outDir = Path.GetFullPath(args[1]);
            var renderer = new RuleRenderer(resources);
            var plannedProfiles = new List<(Profile Profile, List<PlannedFile> Files)>();
            foreach (var profile in manifest.Profiles)
            {
                var files = renderer.Plan(manifest, profile);
                plannedProfiles.Add((profile, files));
            }

            Directory.CreateDirectory(outDir);
            var allFiles = plannedProfiles
                .SelectMany(item => item.Files.Select(file => new PlannedFile
                {
                    Output = $"{item.Profile.Id}/{file.Output}",
                    Content = file.Content
                }))
                .ToList();
            new OutputWriter().WriteFiles(outDir, allFiles);
            foreach (var item in plannedProfiles)
                Console.WriteLine($"已输出：{item.Profile.Id}（{item.Files.Count} 个文件）");

            Console.WriteLine($"输出完成：{manifest.Profiles.Count} 个 Profile → {outDir}");
            return 0;
        }

        if (args.Length == 1 && (args[0].Equals("--help", StringComparison.OrdinalIgnoreCase)
            || args[0].Equals("-h", StringComparison.OrdinalIgnoreCase)))
        {
            Console.WriteLine(Banner);
            Console.WriteLine("直接双击或不带参数运行：自动检测当前项目给出推荐，也可进入问答选择并释放规则。");
            Console.WriteLine("--check：只校验 manifest、片段、全部 Profile 与决策树，不写入文件。");
            Console.WriteLine("--detect [目录]：只做项目检测，打印推荐组合，不写入文件。");
            Console.WriteLine("--emit <目录>：把全部 Profile 的生成结果输出到指定目录，仅用于批量预览与对比，不覆盖工作目录。");
            Console.WriteLine("--help：显示帮助。");
            return 0;
        }

        throw new ArgumentException($"不支持的参数：{string.Join(" ", args)}。请直接运行，或使用 --check。", nameof(args));
    }
}
