using System.Text;

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

            Console.WriteLine(Banner);
            Console.WriteLine($"当前目录：{Environment.CurrentDirectory}");
            Console.WriteLine();

            var profile = new ConsoleWizard().SelectProfile(manifest);
            if (profile is null)
            {
                Console.WriteLine("已取消。");
                return 0;
            }

            var files = new RuleRenderer(resources).Plan(manifest, profile);
            Console.WriteLine();
            Console.WriteLine($"已选择：{profile.Label}");
            Console.WriteLine("将释放以下文件：");
            foreach (var file in files)
                Console.WriteLine($"  - {file.Output}");
            Console.WriteLine("（不释放 TASK.md / rulelog / devlog）");
            Console.WriteLine();

            Console.WriteLine();
            var written = new OutputWriter().WriteFiles(files);
            Console.WriteLine($"完成：已释放 {written} 个文件到 {Environment.CurrentDirectory}");
            return 0;
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

    private static int RunCommand(string[] args, Manifest manifest, ResourceStore resources)
    {
        if (args.Length == 1 && args[0].Equals("--check", StringComparison.OrdinalIgnoreCase))
        {
            var renderer = new RuleRenderer(resources);
            foreach (var profile in manifest.Profiles)
            {
                var files = renderer.Plan(manifest, profile);
                Console.WriteLine($"通过：{profile.Id}（{files.Count} 个文件）");
            }

            Console.WriteLine($"校验完成：{manifest.Profiles.Count} 个 Profile 全部通过。");
            return 0;
        }

        if (args.Length == 1 && (args[0].Equals("--help", StringComparison.OrdinalIgnoreCase)
            || args[0].Equals("-h", StringComparison.OrdinalIgnoreCase)))
        {
            Console.WriteLine(Banner);
            Console.WriteLine("直接双击或不带参数运行：进入交互式选择并释放规则。");
            Console.WriteLine("--check：只校验 manifest、片段和全部 Profile，不写入文件。");
            Console.WriteLine("--help：显示帮助。");
            return 0;
        }

        throw new ArgumentException($"不支持的参数：{string.Join(" ", args)}。请直接运行，或使用 --check。", nameof(args));
    }
}
