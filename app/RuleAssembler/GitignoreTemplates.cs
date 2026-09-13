namespace RuleAssembler;

// 目标项目缺失 .gitignore 时按技术栈生成默认忽略规则。
// 内容精选自 github/gitignore 标准模板（C# 参考仓库内 daily-examples 版本，Python 等参考 mcp_server 版本），
// 每份保持精简，只覆盖构建产物、本地环境与编辑器目录，末尾统一带生成器本地产物忽略块（配方记录、生成器本体与规则效果反馈记录）。
internal static class GitignoreTemplates
{
    public const string RecipeEntry = ".ai-rule-profile.json";

    // 生成器本体：使用方式是拷贝到目标项目根目录执行，属于本地产物，不应提交。
    // 用通配覆盖全部发布名变体（use-ai-rule.exe、无扩展名产物与历史带 RID 后缀的副本），避免逐个枚举遗漏
    public const string ExeEntry = "use-ai-rule*";

    // 规则效果反馈记录：目标项目 AI 执行过程的本地登记文件，属本地产物，不随项目提交，回传规则源仓库时显式复制内容
    public const string FeedbackEntry = ".ai-rule-feedback.md";

    public static readonly string[] GeneratorEntries = { RecipeEntry, ExeEntry, FeedbackEntry };

    public const string GeneratorComment =
        "# AI rule 本地产物（use-ai-rule 生成：配方记录、生成器本体与规则效果反馈记录，勿提交）\n";

    public const string GeneratorBlock =
        GeneratorComment + RecipeEntry + "\n" + ExeEntry + "\n" + FeedbackEntry + "\n";

    public static string Get(string stackKey)
    {
        return stackKey switch
        {
            "dotnet" => DotNet,
            "python" => Python,
            "node" => Node,
            "hybrid" => Hybrid,
            "webstack" => WebStack,
            "flutter" => Flutter,
            _ => Minimal
        };
    }

    private const string CommonOs =
        "# 操作系统杂项\n" +
        ".DS_Store\n" +
        "Thumbs.db\n" +
        "desktop.ini\n";

    private const string DotNet =
        "# .NET 构建产物与本地环境\n" +
        "[Bb]in/\n" +
        "[Oo]bj/\n" +
        "[Rr]elease/\n" +
        "[Dd]ebug/\n" +
        ".vs/\n" +
        "*.user\n" +
        "*.suo\n" +
        "*.binlog\n" +
        "TestResults/\n" +
        "artifacts/\n" +
        "publish/\n" +
        "*.pubxml.user\n" +
        ".idea/\n" +
        "*.DotSettings.user\n" +
        "\n" + CommonOs + "\n" + GeneratorBlock;

    private const string Python =
        "# Python 构建产物与虚拟环境\n" +
        "__pycache__/\n" +
        "*.py[cod]\n" +
        ".venv/\n" +
        "venv/\n" +
        "dist/\n" +
        "build/\n" +
        "*.egg-info/\n" +
        ".pytest_cache/\n" +
        ".ruff_cache/\n" +
        ".mypy_cache/\n" +
        ".coverage\n" +
        "htmlcov/\n" +
        ".idea/\n" +
        "\n" + CommonOs + "\n" + GeneratorBlock;

    private const string Node =
        "# Node.js 构建产物\n" +
        "node_modules/\n" +
        "dist/\n" +
        "coverage/\n" +
        "*.log\n" +
        ".vite/\n" +
        ".idea/\n" +
        "\n" + CommonOs + "\n" + GeneratorBlock;

    // Web 前后端分离：前端 Node 产物 + .NET 后端产物都要覆盖
    private const string WebStack =
        "# Node.js 构建产物（前端）\n" +
        "node_modules/\n" +
        "dist/\n" +
        "coverage/\n" +
        "*.log\n" +
        ".vite/\n" +
        "\n" +
        "# .NET 构建产物与本地环境（后端）\n" +
        "[Bb]in/\n" +
        "[Oo]bj/\n" +
        ".vs/\n" +
        "*.user\n" +
        "TestResults/\n" +
        "publish/\n" +
        ".idea/\n" +
        "\n" + CommonOs + "\n" + GeneratorBlock;

    private const string Hybrid =
        "# 桌面混合项目构建产物（Go + Node）\n" +
        "node_modules/\n" +
        "dist/\n" +
        "coverage/\n" +
        "*.log\n" +
        "build/bin/\n" +
        "*.exe\n" +
        "*.test\n" +
        "*.out\n" +
        ".idea/\n" +
        "\n" + CommonOs + "\n" + GeneratorBlock;

    private const string Flutter =
        "# Flutter 构建产物\n" +
        ".dart_tool/\n" +
        ".flutter-plugins\n" +
        ".flutter-plugins-dependencies\n" +
        "build/\n" +
        "*.iml\n" +
        ".idea/\n" +
        "\n" + CommonOs + "\n" + GeneratorBlock;

    private const string Minimal =
        "# 通用本地文件\n" +
        "*.log\n" +
        ".idea/\n" +
        ".vscode/\n" +
        "\n" + CommonOs + "\n" + GeneratorBlock;
}
