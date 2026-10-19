@echo off
setlocal
title Building WinFixPro Portable...

echo =======================================================
echo          WinFixPro Portable Build Script
echo =======================================================
echo.

cd /d "%~dp0"

set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe

if not exist "%CSC%" (
    set CSC=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe
)

if not exist "%CSC%" (
    echo [ERROR] Could not locate csc.exe compiler in Microsoft.NET Framework directory.
    echo Please ensure .NET Framework 4.5+ is enabled on this Windows machine.
    pause
    exit /b 1
)

echo [1/2] Compiling C# source code using %CSC%...
"%CSC%" /nologo /target:winexe /optimize+ /out:"%~dp0WinFixPro.exe" /win32manifest:"%~dp0app.manifest" /win32icon:"%~dp0app.ico" /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Management.dll "%~dp0src\Theme.cs" "%~dp0src\FixEngine.cs" "%~dp0src\FixModules.cs" "%~dp0src\MainForm.cs" "%~dp0src\Program.cs"

if %ERRORLEVEL% equ 0 (
    echo.
    echo [2/2] SUCCESS! WinFixPro.exe built successfully.
    echo Portable executable is ready: %~dp0WinFixPro.exe
    echo You can copy this file to any USB drive and run on any Windows PC.
    echo.
) else (
    echo.
    echo [ERROR] Build failed with exit code %ERRORLEVEL%.
    pause
    exit /b %ERRORLEVEL%
)

endlocal
