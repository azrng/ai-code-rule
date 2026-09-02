namespace RuleAssembler.Tests;

public sealed class ProjectDetectorTests : IDisposable
{
    private readonly string _root;
    private readonly Manifest _manifest;

    public ProjectDetectorTests()
    {
        _manifest = new ManifestLoader(new ResourceStore()).Load();
        _root = Path.Combine(Path.GetTempPath(), $"rule-detector-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void Detect_WailsProject_RecommendsReactWailsExe()
    {
        File.WriteAllText(Path.Combine(_root, "wails.json"), "{\"name\":\"demo\"}");
        File.WriteAllText(Path.Combine(_root, "go.mod"), "module demo\n\ngo 1.22\n");
        File.WriteAllText(
            Path.Combine(_root, "package.json"),
            "{\"dependencies\":{\"react\":\"^18.0.0\"}}");

        var result = new ProjectDetector(_manifest).Detect(_root);

        Assert.NotNull(result);
        Assert.Equal("reactWailsExe", result.ProfileId);
        Assert.Contains(result.Reasons, reason => reason.Contains("wails.json"));
    }

    [Fact]
    public void Detect_WpfProject_RecommendsWpfExe()
    {
        File.WriteAllText(
            Path.Combine(_root, "Demo.csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><OutputType>WinExe</OutputType><UseWPF>true</UseWPF></PropertyGroup></Project>");

        var result = new ProjectDetector(_manifest).Detect(_root);

        Assert.NotNull(result);
        Assert.Equal("wpfExe", result.ProfileId);
    }

    [Fact]
    public void Detect_FastapiProject_RecommendsPython()
    {
        File.WriteAllText(Path.Combine(_root, "requirements.txt"), "fastapi==0.110.0\nuvicorn\n");

        var result = new ProjectDetector(_manifest).Detect(_root);

        Assert.NotNull(result);
        Assert.Equal("python", result.ProfileId);
    }

    [Fact]
    public void Detect_EmptyDirectory_ReturnsNull()
    {
        var result = new ProjectDetector(_manifest).Detect(_root);

        Assert.Null(result);
    }
}
