# 构建并发布 use-ai-rule.exe（框架相关单文件，需目标机有 .NET 运行时）
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $here

dotnet publish -c Release `
  -r win-x64 `
  --self-contained false `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true

$publishedExe = Join-Path $here 'bin\Release\net10.0\win-x64\publish\use-ai-rule.exe'
if (-not (Test-Path -LiteralPath $publishedExe -PathType Leaf)) {
    throw "发布完成但未找到 EXE：$publishedExe"
}

$repoRoot = Split-Path -Parent (Split-Path -Parent $here)
$distDirectory = Join-Path $repoRoot 'dist'
$distExe = Join-Path $distDirectory 'use-ai-rule.exe'
New-Item -ItemType Directory -Path $distDirectory -Force | Out-Null
Copy-Item -LiteralPath $publishedExe -Destination $distExe -Force

Write-Host ""
Write-Host "发布完成："
Write-Host "  $distExe"
