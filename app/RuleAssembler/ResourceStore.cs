using System.Text;

namespace RuleAssembler;

internal sealed class ResourceStore
{
    private readonly IResourceProvider _provider;
    private readonly string[] _resourceNames;

    // 缺省走程序集内嵌（兼容既有调用与单测）；内容包模式下由 Program 注入 FolderResourceProvider
    public ResourceStore(IResourceProvider? provider = null)
    {
        _provider = provider ?? new EmbeddedResourceProvider();
        _resourceNames = _provider.Names.ToArray();
    }

    public string? Read(string relativePath)
    {
        var normalized = relativePath.Replace('\\', '/').TrimStart('/');
        // 内嵌模式下逻辑名在 Fragments. 命名空间内；内容包模式下 fragments/ 是真实目录前缀。
        // 两种来源统一先按裸路径找（内容包 fragments/ 下的片段以 fragments/xxx 传入的场景），
        // 找不到再补 fragments/ 前缀（manifest.json 等根级资源）
        var bytes = _provider.ReadBytes(normalized)
            ?? _provider.ReadBytes("fragments/" + normalized);
        if (bytes is null)
            return null;

        using var reader = new StreamReader(new MemoryStream(bytes), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return NormalizeLineEndings(reader.ReadToEnd());
    }


    // 片段读取口统一 LF：工作区检出可能是 CRLF（autocrlf）或 LF，必须在入口抹平，
    // 否则下游按 \n 写死的模板处理（如根模板连续空行收敛）会因行尾不同产生内容差异
    internal static string NormalizeLineEndings(string content)
    {
        return content.Replace("\r\n", "\n").Replace('\r', '\n');
    }
}
