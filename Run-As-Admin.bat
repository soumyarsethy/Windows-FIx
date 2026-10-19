@echo off
:: Launches WinFixPro.exe with elevated Administrator privileges
powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath '%~dp0WinFixPro.exe' -Verb RunAs"
