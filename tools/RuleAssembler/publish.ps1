# 构建并发布 use-rule.exe（框架相关单文件，需目标机有 .NET 运行时）
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $here

dotnet publish -c Release `
  -r win-x64 `
  --self-contained false `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true

Write-Host ""
Write-Host "发布完成："
$exe = Join-Path $here 'bin\Release\net10.0\win-x64\publish\use-rule.exe'
Write-Host "  $exe"
