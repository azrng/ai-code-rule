# 构建并发布 use-ai-rule 生成器。
# 默认三平台 self-contained 单文件矩阵（目标机无需安装 .NET 运行时）：
#   dist/use-ai-rule.exe              —— win-x64（兼容历史用法的别名）
#   dist/use-ai-rule-win-x64.exe      —— win-x64 规范名
#   dist/use-ai-rule-linux-x64        —— linux-x64
#   dist/use-ai-rule-osx-x64          —— osx-x64
# 参数：
#   -Rids <rid[,rid...]>        指定 RID 列表，默认 win-x64,linux-x64,osx-x64
#   -FrameworkDependent         退回框架相关发布（目标机需自装 .NET 10 运行时）
# Linux/mac 下等价入口见同目录 publish.sh，两侧 RID 语义保持一致。
param(
    [string]$Rids = "win-x64,linux-x64,osx-x64",
    [switch]$FrameworkDependent
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

    $distBinary = Join-Path $distDirectory "use-ai-rule-$rid$(if ($rid -like 'win-*') { '.exe' })"
    Copy-Item -LiteralPath $publishedBinary -Destination $distBinary -Force
    $published += $distBinary

    # 兼容历史用法：win-x64 额外复制为 dist\use-ai-rule.exe
    if ($rid -eq 'win-x64') {
        $legacyBinary = Join-Path $distDirectory 'use-ai-rule.exe'
        Copy-Item -LiteralPath $publishedBinary -Destination $legacyBinary -Force
        $published += $legacyBinary
    }
}

Write-Host ""
Write-Host "发布完成："
foreach ($item in $published) {
    Write-Host "  $item"
}
