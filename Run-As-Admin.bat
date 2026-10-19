@echo off
:: WinFixPro - 1-Click Administrator Launcher
cd /d "%~dp0"
if exist "%~dp0WinFixPro.exe" (
    powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath '%~dp0WinFixPro.exe' -Verb RunAs"
) else (
    echo [ERROR] WinFixPro.exe not found! Please run build.bat first.
    pause
)
