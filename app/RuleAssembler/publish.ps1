# 构建并发布 use-ai-rule 生成器（win-x64，框架依赖模式）。
# 目标机需自装 .NET 10 运行时（https://dotnet.microsoft.com/download/dotnet/10.0）。
# 产物（exe 单产物口径，规则与技能内容全量打进 exe）：
#   dist/use-ai-rule.exe              —— win-x64 framework-dependent 单文件（体积小，跟随运行时更新）
#   dist/latest.json                  —— 更新检查清单（统一版本 + exe SHA-256），随 Release 上传
# 统一版本口径：exe 内嵌内容的 manifest.vars.version 与 exe 的 InformationalVersion 同源注入，
# banner 与更新检查基准都读它；交互启动时拉 latest.json 一次对照 exe 版本，发现更新弹确认
#（默认更新），下载或同机共享缓存命中后校验替换；无网络回退内嵌资源，单 exe 离线永远可用。
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent (Split-Path -Parent $here)

<# 计算 SHA-256（小写十六进制）。不用 Get-FileHash：部分机器 PSModulePath 被 PS7 路径
   污染后，PS 5.1 自动加载 Microsoft.PowerShell.Utility 失败，该 cmdlet 会凭空消失。 #>
function Get-Sha256 {
    param([string]$Path)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $stream = [System.IO.File]::OpenRead($Path)
        try {
            return ([System.BitConverter]::ToString($sha.ComputeHash($stream))).Replace('-', '').ToLowerInvariant()
        } finally {
            $stream.Dispose()
        }
    } finally {
        $sha.Dispose()
    }
}
$distDirectory = Join-Path $repoRoot 'dist'
New-Item -ItemType Directory -Path $distDirectory -Force | Out-Null

# 统一版本：构建 exe 前先读出 manifest.vars.version 以注入 InformationalVersion
$manifestPath = Join-Path $repoRoot 'fragments/manifest.json'
# 显式 UTF8：PS 5.1 对无 BOM 文件默认按系统 ANSI 解码，中文乱码会导致 ConvertFrom-Json 失败
$manifestJson = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
$version = $manifestJson.vars.version
if (-not $version) { throw 'manifest.vars.version 为空，无法确定发布版本' }

Write-Host "发布 win-x64（框架依赖，单文件，版本 $version）..."
dotnet publish app/RuleAssembler/RuleAssembler.csproj -c Release `
  -r win-x64 `
  --self-contained false `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:IncludeSourceRevisionInInformationalVersion=false `
  -p:InformationalVersion=$version
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish win-x64 失败，退出码：$LASTEXITCODE"
}

$publishedBinary = Join-Path $here "bin\Release\net10.0\win-x64\publish\use-ai-rule.exe"
if (-not (Test-Path -LiteralPath $publishedBinary -PathType Leaf)) {
    throw "发布完成但未找到产物：$publishedBinary"
}
$distBinary = Join-Path $distDirectory 'use-ai-rule.exe'
Copy-Item -LiteralPath $publishedBinary -Destination $distBinary -Force

# latest.json（统一版本）：version = exe 注入版本；
# engine_sha256 供确认后自更新做下载完整性校验（缺省则跳过引擎替换）；
# engine_version 是旧版 exe（2.x 引擎轨道）的兼容字段，写同值助其一次性迁移到新 exe
$latest = [ordered]@{
    version = $version
    engine_version = $version
    engine_sha256 = Get-Sha256 $distBinary
}
# 落盘 latest.json：exe 更新检查与 Release 上传都依赖此文件；UTF-8 无 BOM（PS 5.1 的 Set-Content UTF8 带 BOM 会让 exe 解析失败）
$latestJson = $latest | ConvertTo-Json
[System.IO.File]::WriteAllText((Join-Path $distDirectory 'latest.json'), $latestJson, (New-Object System.Text.UTF8Encoding($false)))

Write-Host ""
Write-Host "发布完成：$distBinary（win-x64，版本 $version）"
