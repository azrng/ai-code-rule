using System.Reflection;
using System.Text;

namespace RuleAssembler;

internal sealed class ResourceStore
{
    private readonly Assembly _assembly = Assembly.GetExecutingAssembly();
    private readonly string[] _resourceNames;

    public ResourceStore()
    {
        _resourceNames = _assembly.GetManifestResourceNames();
    }

    public string? Read(string relativePath)
    {
        var normalized = relativePath.Replace('\\', '/').TrimStart('/');
        var wanted = "Fragments/" + normalized;
        var name = _resourceNames.FirstOrDefault(resource =>
            string.Equals(
                resource.Replace('\\', '/').Replace("Fragments.", "Fragments/", StringComparison.OrdinalIgnoreCase),
                wanted,
                StringComparison.OrdinalIgnoreCase));

        if (name is null)
            return null;

        using var stream = _assembly.GetManifestResourceStream(name);
        if (stream is null)
            return null;

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return NormalizeLineEndings(reader.ReadToEnd());
    }

    // 片段读取口统一 LF：工作区检出可能是 CRLF（autocrlf）或 LF，必须在入口抹平，
    // 否则下游按 \n 写死的模板处理（如根模板连续空行收敛）会因行尾不同产生内容差异
    internal static string NormalizeLineEndings(string content)
    {
        return content.Replace("\r\n", "\n").Replace('\r', '\n');
    }
}
