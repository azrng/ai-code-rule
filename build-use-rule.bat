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

echo 开始打包...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0app\RuleAssembler\publish.ps1"
set "EXIT_CODE=%ERRORLEVEL%"

echo.
if not "%EXIT_CODE%"=="0" (
    echo 打包失败，退出码：%EXIT_CODE%
    pause
    exit /b %EXIT_CODE%
)

echo 打包成功。
echo 产物：%~dp0dist\use-rule.exe
echo.
pause
exit /b 0
