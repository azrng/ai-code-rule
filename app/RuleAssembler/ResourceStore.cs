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
        return reader.ReadToEnd();
    }
}
