# 构建并发布 use-ai-rule 生成器。
# 默认三平台 self-contained 单文件矩阵（目标机无需安装 .NET 运行时）：
#   dist/use-ai-rule.exe              —— win-x64
#   dist/use-ai-rule-linux-x64        —— linux-x64
#   dist/use-ai-rule-osx-x64          —— osx-x64
# Windows 产物统一命名 use-ai-rule.exe（与目标项目使用方式、gitignore 忽略条目一致），
# 不再额外产出 use-ai-rule-win-x64.exe 等带 RID 后缀的副本。
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

    # Windows 产物固定为 use-ai-rule.exe，非 Windows 带 RID 后缀
    $distName = if ($rid -like 'win-*') { 'use-ai-rule.exe' } else { "use-ai-rule-$rid" }
    $distBinary = Join-Path $distDirectory $distName
    Copy-Item -LiteralPath $publishedBinary -Destination $distBinary -Force
    $published += $distBinary
}

Write-Host ""
Write-Host "发布完成："
foreach ($item in $published) {
    Write-Host "  $item"
}
