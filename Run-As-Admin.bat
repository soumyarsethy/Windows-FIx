@echo off
:: WinFixPro - 1-Click Administrator Launcher
cd /d "%~dp0"
if exist "%~dp0WinFix.exe" (
    powershell -NoProfile -NonInteractive -Command "Start-Process -FilePath '%~dp0WinFix.exe' -Verb RunAs"
    exit /b 0
)
if exist "%~dp0WinFixPro.exe" (
    powershell -NoProfile -NonInteractive -Command "Start-Process -FilePath '%~dp0WinFixPro.exe' -Verb RunAs"
    exit /b 0
)
echo [ERROR] WinFix executable not found! Please run build.bat first.
pause
