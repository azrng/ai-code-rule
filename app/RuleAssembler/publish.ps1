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
    $manifestJson = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
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
        $relative = ([System.IO.Path]::GetRelativePath($stageDirectory, $file.FullName)) -replace '\\', '/'
        $hashMap[$relative] = (Get-FileHash -Algorithm SHA256 $file.FullName).Hash.ToLowerInvariant()
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
        engine_sha256 = (Get-FileHash -Algorithm SHA256 $distBinary).Hash.ToLowerInvariant()
        sha256 = (Get-FileHash -Algorithm SHA256 $packZip).Hash.ToLowerInvariant()
    }

    Remove-Item -Recurse -Force $stageDirectory
    Write-Host ""
    Write-Host "内容包完成：$packZip（版本 $version，$($hashMap.Count) 个文件）"
}

Write-Host ""
Write-Host "发布完成：$distBinary（win-x64）"
