# 构建并发布 use-ai-rule 生成器。
# 默认三平台 self-contained 单文件矩阵（目标机无需安装 .NET 运行时）：
#   dist/use-ai-rule.exe              —— win-x64
#   dist/use-ai-rule-linux-x64        —— linux-x64
#   dist/use-ai-rule-osx-x64          —— osx-x64
# 同时打包 merged 内容包（fragments + skills，manifest.vars.version 作包版本）：
#   dist/ai-rule-pack.zip             —— 内容包（pack.json + manifest.json + fragments/ + skills/）
#   dist/latest.json                  —— 更新检查清单（版本 + zip SHA-256），随 Release 上传
# exe 启动时拉 latest.json 比对本地缓存版本，新版本自动下载校验；无网络回退内嵌资源。
# 参数：
#   -Rids <rid[,rid...]>        指定 RID 列表，默认 win-x64,linux-x64,osx-x64
#   -FrameworkDependent         退回框架相关发布（目标机需自装 .NET 10 运行时）
#   -SkipPack                   只发布 exe，不重打内容包
# Linux/mac 下等价入口见同目录 publish.sh，两侧 RID 语义保持一致。
param(
    [string]$Rids = "win-x64,linux-x64,osx-x64",
    [switch]$FrameworkDependent,
    [switch]$SkipPack
)

$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $here

$ridList = $Rids.Split(',') | ForEach-Object { $_.Trim() } | Where-Object { $_ }
if (-not $ridList) {
    throw "未指定任何 RID：$Rids"
}

$selfContained = 'true'
if ($FrameworkDependent) {
    $selfContained = 'false'
}

$repoRoot = Split-Path -Parent (Split-Path -Parent $here)
$distDirectory = Join-Path $repoRoot 'dist'
New-Item -ItemType Directory -Path $distDirectory -Force | Out-Null

$published = @()
foreach ($rid in $ridList) {
    Write-Host ""
    Write-Host "发布 $rid（self-contained=$selfContained，单文件）..."
    dotnet publish -c Release `
      -r $rid `
      --self-contained $selfContained `
      -p:PublishSingleFile=true `
      -p:IncludeNativeLibrariesForSelfExtract=true
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish $rid 失败，退出码：$LASTEXITCODE"
    }

    $binaryName = 'use-ai-rule'
    if ($rid -like 'win-*') {
        $binaryName = 'use-ai-rule.exe'
    }
    $publishedBinary = Join-Path $here "bin\Release\net10.0\$rid\publish\$binaryName"
    if (-not (Test-Path -LiteralPath $publishedBinary -PathType Leaf)) {
        throw "发布完成但未找到产物：$publishedBinary"
    }

    # Windows 产物固定为 use-ai-rule.exe，非 Windows 带 RID 后缀
    $distName = if ($rid -like 'win-*') { 'use-ai-rule.exe' } else { "use-ai-rule-$rid" }
    $distBinary = Join-Path $distDirectory $distName
    Copy-Item -LiteralPath $publishedBinary -Destination $distBinary -Force
    $published += $distBinary
}

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
        sha256 = (Get-FileHash -Algorithm SHA256 $packZip).Hash.ToLowerInvariant()
    }
    $latest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $distDirectory 'latest.json') -Encoding UTF8

    Remove-Item -Recurse -Force $stageDirectory
    Write-Host ""
    Write-Host "内容包完成：$packZip（版本 $version，$($hashMap.Count) 个文件）"
}

Write-Host ""
Write-Host "发布完成："
foreach ($item in $published) {
    Write-Host "  $item"
}
