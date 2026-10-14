@echo off
setlocal

cd /d "%~dp0"
echo ========================================
echo   AI Rule 拼接器打包工具
echo ========================================
echo.

where dotnet >nul 2>nul
if errorlevel 1 (
    echo 错误：未找到 dotnet，请先安装 .NET SDK 10。
    echo.
    pause
    exit /b 1
)

where powershell >nul 2>nul
if errorlevel 1 (
    echo 错误：未找到 Windows PowerShell。
    echo.
    pause
    exit /b 1
)

rem 默认只打包 win-x64（self-contained 单文件，目标机无需 .NET 运行时）；
rem 传参 all 时按三平台矩阵发布（win-x64 / linux-x64 / osx-x64），
rem mac/linux 产物由 publish.ps1 复制为 dist\use-ai-rule-<rid>
set "RID_ARGS=-Rids win-x64"
if /i "%~1"=="all" set "RID_ARGS="

echo 开始打包...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0app\RuleAssembler\publish.ps1" %RID_ARGS%
set "EXIT_CODE=%ERRORLEVEL%"

echo.
if not "%EXIT_CODE%"=="0" (
    echo 打包失败，退出码：%EXIT_CODE%
    pause
    exit /b %EXIT_CODE%
)

echo 打包成功。
echo 产物目录：%~dp0dist
echo.
pause
exit /b 0
