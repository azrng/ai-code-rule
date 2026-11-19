namespace RuleAssembler;

// 目录浅扫产出的原始信号集合：只记「发现了什么」，不掺任何维度语义；
// 由 ProjectDetector.CollectEvidence 把信号翻译为维度证据
internal sealed class ScanSignals
{
    public bool HasWailsJson;
    public bool HasGoMod;
    public bool HasTauriConf;
    public bool HasNuxtConfig;
    public bool HasElectronConfig;
    public bool HasPubspec;
    public bool HasPackageJson;
    public bool HasPyproject;
    public bool HasRequirementsTxt;
    public bool HasSln;
    public bool HasRazorFiles;

    // Components/ 目录（深度 ≤3）下存在 .razor 文件：Blazor Web App 模板结构特征
    public bool RazorUnderComponents;
    public bool HasRootHtml;
    public bool HasRootHtmlOnly;
    public bool ReactInPackageJson;
    public bool VueInPackageJson;
    public bool NextInPackageJson;
    public bool NuxtInPackageJson;
    public bool ElectronInPackageJson;
    public bool HasFastapiDependency;
    public int CsprojCount;
    public List<string> CsprojContents { get; } = new();

    public bool DotNetOrNodePresent()
    {
        return CsprojCount > 0 || HasSln || HasPackageJson;
    }

    public bool PackageJsonWithoutBackend
    {
        // Electron 是桌面壳而非纯前端工程，存在 Electron 信号时不得再给 form=web 兜底证据，
        // 否则与壳证据的 form=desktop 冲突会把 form 维度打回未确认
        get
        {
            return HasPackageJson
                && !HasWailsJson
                && !HasTauriConf
                && !HasNuxtConfig
                && !NuxtInPackageJson
                && !HasElectronConfig
                && !ElectronInPackageJson
                && CsprojCount == 0
                && !NextInPackageJson;
        }
    }

    public bool OnlyStaticHtml
    {
        get { return HasRootHtmlOnly && !HasGoMod && !HasPubspec && !HasPyproject; }
    }
}
