@echo off
setlocal
title Building WinFixPro Portable...

echo =======================================================
echo          WinFixPro Portable Build Script (v3.0)
echo =======================================================
echo:

cd /d "%~dp0"

rem Prioritize modern Roslyn compiler from Visual Studio Build Tools, fallback to .NET Framework
set "CSC=C:\Program Files (x86)\Microsoft Visual Studio\18\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe"
if not exist "%CSC%" set "CSC=C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe"
if not exist "%CSC%" set "CSC=C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\Roslyn\csc.exe"
if not exist "%CSC%" set "CSC=C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\Roslyn\csc.exe"
if not exist "%CSC%" set "CSC=C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe"
if not exist "%CSC%" set "CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"

if exist "%CSC%" goto :COMPILER_FOUND
echo [ERROR] Could not locate C# compiler (csc.exe).
echo Please ensure .NET Framework 4.5+ or Visual Studio Build Tools is installed.
pause
exit /b 1

:COMPILER_FOUND

echo [1/2] Compiling C# source code using %CSC%...
"%CSC%" /target:winexe /optimize+ /win32manifest:"%~dp0app.manifest" /win32icon:"%~dp0app.ico" /out:"%~dp0WinFix.exe" /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Management.dll "%~dp0src\*.cs"

if exist "%~dp0WinFix.exe" (
    echo:
    echo [2/2] SUCCESS! WinFix.exe built successfully.
    echo Portable executable is ready: %~dp0WinFix.exe
    echo You can copy this file to any USB drive and run on any Windows PC.
    echo:
) else (
    echo:
    echo [ERROR] Build failed.
    pause
    exit /b 1
)

endlocal
