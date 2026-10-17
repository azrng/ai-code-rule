# 构建并发布 use-ai-rule 生成器（win-x64，框架依赖模式）。
# 目标机需自装 .NET 10 运行时（https://dotnet.microsoft.com/download/dotnet/10.0）。
# 产物：
#   dist/use-ai-rule.exe              —— win-x64 framework-dependent 单文件（体积小，跟随运行时更新）
# 同时打包 merged 内容包（fragments + skills，manifest.vars.version 作包版本）：
#   dist/ai-rule-pack.zip             —— 内容包（pack.json + manifest.json + fragments/ + skills/）
#   dist/latest.json                  —— 更新检查清单（版本 + zip SHA-256），随 Release 上传
# exe 启动时拉 latest.json 比对本地缓存版本，新版本自动下载校验；无网络回退内嵌资源。
# 参数：
#   -SkipPack                   只发布 exe，不重打内容包
param(
    [switch]$SkipPack
)

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

Write-Host "发布 win-x64（框架依赖，单文件）..."
dotnet publish app/RuleAssembler/RuleAssembler.csproj -c Release `
  -r win-x64 `
  --self-contained false `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish win-x64 失败，退出码：$LASTEXITCODE"
}

$publishedBinary = Join-Path $here "bin\Release\net10.0\win-x64\publish\use-ai-rule.exe"
if (-not (Test-Path -LiteralPath $publishedBinary -PathType Leaf)) {
    throw "发布完成但未找到产物：$publishedBinary"
}
$distBinary = Join-Path $distDirectory 'use-ai-rule.exe'
Copy-Item -LiteralPath $publishedBinary -Destination $distBinary -Force

# ── merged 内容包：fragments + skills 打 zip，pack.json 记录版本与逐文件 SHA-256 ──
if (-not $SkipPack) {
    $manifestPath = Join-Path $repoRoot 'fragments/manifest.json'
    # 显式 UTF8：PS 5.1 对无 BOM 文件默认按系统 ANSI 解码，中文乱码会导致 ConvertFrom-Json 失败
    $manifestJson = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $version = $manifestJson.vars.version
    if (-not $version) { throw "manifest.vars.version 为空，无法确定内容包版本" }

    $stageDirectory = Join-Path $repoRoot 'dist/pack-stage'
    if (Test-Path $stageDirectory) { Remove-Item -Recurse -Force $stageDirectory }
    New-Item -ItemType Directory -Path $stageDirectory | Out-Null

    Copy-Item -Recurse (Join-Path $repoRoot 'fragments') (Join-Path $stageDirectory 'fragments')
    Copy-Item -Recurse (Join-Path $repoRoot 'skills') (Join-Path $stageDirectory 'skills')
    # README.md 是仓库文档不随包发布（口径与 csproj 的 Exclude 一致）
    $skillsReadme = Join-Path $stageDirectory 'skills/README.md'
    if (Test-Path $skillsReadme) { Remove-Item -Force $skillsReadme }

    # 逐文件 SHA-256 清单（相对路径用 /，哈希小写十六进制）
    $hashMap = [ordered]@{}
    foreach ($file in (Get-ChildItem -Recurse -File $stageDirectory)) {
        # PowerShell 5.1 无 System.IO.Path.GetRelativePath，用子串截取兼容 5.1 / 7
        $relative = $file.FullName.Substring($stageDirectory.Length).TrimStart('\') -replace '\\', '/'
        $hashMap[$relative] = Get-Sha256 $file.FullName
    }

    $packManifest = [ordered]@{
        kind = 'merged'
        version = $version
        schema_version = 1
        files = $hashMap
    }
    $packManifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $stageDirectory 'pack.json') -Encoding UTF8

    $packZip = Join-Path $distDirectory 'ai-rule-pack.zip'
    if (Test-Path $packZip) { Remove-Item -Force $packZip }
    Compress-Archive -Path (Join-Path $stageDirectory '*') -DestinationPath $packZip -CompressionLevel Optimal

    $latest = [ordered]@{
        version = $version
        engine_sha256 = Get-Sha256 $distBinary
        sha256 = Get-Sha256 $packZip
    }
    # 落盘 latest.json：exe 更新检查与 Release 上传都依赖此文件；UTF-8 无 BOM（PS 5.1 的 Set-Content UTF8 带 BOM 会让 exe 解析失败）
    $latestJson = $latest | ConvertTo-Json
    [System.IO.File]::WriteAllText((Join-Path $distDirectory 'latest.json'), $latestJson, (New-Object System.Text.UTF8Encoding($false)))

    Remove-Item -Recurse -Force $stageDirectory
    Write-Host ""
    Write-Host "内容包完成：$packZip（版本 $version，$($hashMap.Count) 个文件）"
} else {
    # 只出 exe 时同步清掉 dist 里残留的内容包与 latest.json，避免误传与当前 exe 不配套的发布资产
    foreach ($stale in @('ai-rule-pack.zip', 'latest.json')) {
        $stalePath = Join-Path $distDirectory $stale
        if (Test-Path -LiteralPath $stalePath) { Remove-Item -Force $stalePath }
    }
    Write-Host ""
    Write-Host "已跳过内容包打包（-SkipPack），dist 仅含 exe"
}

Write-Host ""
Write-Host "发布完成：$distBinary（win-x64）"
